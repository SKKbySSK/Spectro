using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    [StructLayout(LayoutKind.Explicit)]
    struct UnionArray
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public float[] Floats;
    }
    
    public class SoundioInput : IAudioInput, IDisposable
    {
        private SoundIORingBuffer ringBuffer = null;
        private Thread thread;
        private SoundIOInStream stream;
        private TaskCompletionSource<object> threadSource = new TaskCompletionSource<object>();
        
        public SoundioInput(SoundIODevice device)
        {
            Device = device;
        }
        
        public SoundIODevice Device { get; }
        
        public AudioFormat Format { get; }
        
        public event ValueEventHandler<float[]> Filled;
        
        public Task InitializeAsync(AudioFormat format)
        {
            return Task.Run(() => initInternal(format));
        }

        public async Task<FormatResult> SupportsFormatAsync(AudioFormat format)
        {
            if (format == null)
            {
                throw new ArgumentNullException();
            }

            return checkFormatInternal(format, out _);
        }

        public async Task StartAsync()
        {
            if (stream == null)
            {
                throw new Exception("You MUST call InitializeAsync");
            }

            threadSource = new TaskCompletionSource<object>();
            thread.Start();
            stream.Start();
        }

        public async Task StopAsync()
        {
            if (stream == null)
            {
                throw new Exception("You MUST call InitializeAsync");
            }

            threadSource.TrySetResult(null);
            await threadSource.Task;
            threadSource = new TaskCompletionSource<object>();
            stream.Dispose();
        }

        private void initInternal(AudioFormat format)
        {
            if (format == null)
            {
                throw new ArgumentNullException();
            }
            
            var result = checkFormatInternal(format, out var layout);
            var native = Soundio.ToSoundioFormat(format);
            if (result != FormatResult.Ok || !native.HasValue || !layout.HasValue)
            {
                throw new NotSupportedException("Format is not supported : " + format);
            }

            stream = Device.CreateInStream();
            stream.Format = native.Value;
            stream.Layout = layout.Value;
            stream.SampleRate = format.SampleRate;
            stream.ReadCallback = ReadCallback;
            stream.Open();
            
            const int bufferDuration = 30;
            int capacity = (int)(bufferDuration * stream.SampleRate * stream.BytesPerFrame);

            ringBuffer = Soundio.Api.CreateRingBuffer(capacity);
            thread = new Thread(() => CopyThread(capacity));
        }

        private void CopyThread(int capacity)
        {
            var arr = new byte [capacity];
            unsafe {
                fixed (void* arrptr = arr) {
                    while (!threadSource.Task.IsCompleted)
                    {
                        Soundio.Api.FlushEvents();
                        Thread.Sleep (1000);
                        int fillBytes = ringBuffer.FillCount;
                        var readBuf = ringBuffer.ReadPointer;

                        Buffer.MemoryCopy ((void*)readBuf, arrptr, fillBytes, fillBytes);
                        var buffer = new UnionArray() { Bytes = arr };
                        Filled?.Invoke(this, new ValueEventArgs<float[]>(buffer.Floats));
                        ringBuffer.AdvanceReadPointer (fillBytes);
                    }
                }
            }
        }

        private FormatResult checkFormatInternal(AudioFormat format, out SoundIOChannelLayout? layout)
        {
            layout = null;
            if (!Device.SupportsSampleRate(format.SampleRate))
            {
                return FormatResult.UnsupportedSampleRate;
            }

            bool invalidChannel = true;
            foreach (var l in Device.Layouts)
            {
                if (l.ChannelCount == format.Channels)
                {
                    invalidChannel = false;
                    layout = l;
                    break;
                }
            }

            if (invalidChannel)
            {
                return FormatResult.UnsupportedChannel;
            }

            var nativeFormat = Soundio.ToSoundioFormat(format);
            if (nativeFormat == null || !Device.SupportsFormat(nativeFormat.Value))
            {
                return FormatResult.UnsupportedBitDepth;
            }

            return FormatResult.Ok;
        }
        
        private unsafe void ReadCallback(int frameCountMin, int frameCountMax)
        {
            var writePtr = ringBuffer.WritePointer;
            int freeBytes = ringBuffer.FreeCount;
            int freeCount = freeBytes / stream.BytesPerFrame;

            if (frameCountMin > freeCount)
                throw new InvalidOperationException ("ring buffer overflow");

            int writeFrames = Math.Min (freeCount, frameCountMax);
            int framesLeft = writeFrames;

            for (; ; ) {
                int frameCount = framesLeft;

                var areas = stream.BeginRead (ref frameCount);

                if (frameCount == 0)
                    break;

                if (areas.IsEmpty) {
                    // Due to an overflow there is a hole. Fill the ring buffer with
                    // silence for the size of the hole.
                    for (int i = 0; i < frameCount * stream.BytesPerFrame; i++)
                        Marshal.WriteByte (writePtr + i, 0);
                    Console.Error.WriteLine ("Dropped {0} frames due to internal overflow", frameCount);
                } else {
                    for (int frame = 0; frame < frameCount; frame += 1) {
                        int chCount = stream.Layout.ChannelCount;
                        int copySize = stream.BytesPerSample;
                        unsafe {
                            for (int ch = 0; ch < chCount; ch += 1) {
                                var area = areas.GetArea (ch);
                                Buffer.MemoryCopy ((void*)area.Pointer, (void*)writePtr, copySize, copySize);
                                area.Pointer += area.Step;
                                writePtr += copySize;
                            }
                        }
                    }
                }

                stream.EndRead ();

                framesLeft -= frameCount;
                if (framesLeft <= 0)
                    break;
            }

            int advanceBytes = writeFrames * stream.BytesPerFrame;
            ringBuffer.AdvanceWritePointer (advanceBytes);
        }

        public void Dispose()
        {
            ringBuffer?.Dispose();
            stream?.Dispose();
            Device.RemoveReference();
        }
    }
}