using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    public class SoundioInput : IAudioInput, IDisposable
    {
        private readonly SoundIO api = new SoundIO();
        private SoundIOInStream instream;
        private bool stop = false;
        private RingBuffer<byte> _ringBuffer;
        private TimeSpan _bufferDuration;
        private TaskCompletionSource<object> threadSource = new TaskCompletionSource<object>();
        private ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
        
        public SoundioInput(TimeSpan? bufferDuration = null)
        {
            _bufferDuration = bufferDuration ?? TimeSpan.FromSeconds(30);
            api.ConnectBackend(SoundIOBackend.Alsa);
            api.FlushEvents();
            
            for (int i = 0; api.InputDeviceCount > i; i++)
            {
                var device = api.GetInputDevice(i);
                if (i == api.DefaultInputDeviceIndex)
                {
                    DefaultDevice = device;
                }

                Devices.Add(device);
            }
        }
        
        public SoundIODevice Device { get; private set; }

        public List<SoundIODevice> Devices { get; } = new List<SoundIODevice>();

        public SoundIODevice DefaultDevice { get; }
        
        public AudioFormat Format { get; private set; }
        
        public void Write(byte[] buffer, int offset, int count)
        {
            buffers.Enqueue(buffer);
        }

        public void SetDevice(SoundIODevice device)
        {
            Device = device;
        }

        public event EventHandler<FillEventArgs> Filled;

        public Task InitializeAsync(AudioFormat format)
        {
            return Task.Run(() => initInternal(format));
        }

        public async Task<FormatResult> SupportsFormatAsync(AudioFormat format)
        {
            return checkFormatInternal(format, out _);
        }

        public async Task StartAsync()
        {
            if (instream == null)
            {
                throw new Exception("You MUST call InitializeAsync");
            }

            stop = false;
            threadSource = new TaskCompletionSource<object>();
            instream.Start();
        }

        public async Task StopAsync()
        {
            if (instream == null)
            {
                throw new Exception("You MUST call InitializeAsync");
            }

            stop = true;
            await threadSource.Task;
            threadSource = new TaskCompletionSource<object>();
            instream.Dispose();
        }

        private void initInternal(AudioFormat format)
        {
            if (format == null)
            {
                throw new Exception("You MUST call InitializeAsync");
            }
            
            var native = Soundio.ToSoundioFormat(format);
            if (!native.HasValue)
            {
                throw new NotSupportedException("Format is not supported : " + format);
            }

            instream = Device.CreateInStream();
            instream.Format = native.Value;
            instream.SampleRate = format.SampleRate;

            
            instream.ReadCallback = ReadCallback;
            instream.OverflowCallback = () => Console.WriteLine("Overflow!");
            instream.Open();
            
            // Open後にチャンネルは設定しないと動作しない模様
            checkFormatInternal(format, out var channelLayout);
            instream.Layout = channelLayout.Value;
            
            Format = Soundio.ToManagedFormat(instream.Format, instream.SampleRate, instream.Layout.ChannelCount);
            

            var bytesPerSample = instream.BytesPerSample;
            var capacity = Format.SampleRate * Format.Channels * bytesPerSample *
                           _bufferDuration.TotalSeconds;
            _ringBuffer = new RingBuffer<byte>((uint)capacity);
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
            int writeFrames = frameCountMax;
            int framesLeft = writeFrames;
            UnionBuffer unionBuffer = new UnionBuffer();

            for (; ; ) {
                int frameCount = framesLeft;

                var areas = instream.BeginRead (ref frameCount);

                if (frameCount == 0)
                    break;

                if (areas.IsEmpty) {
                    // Due to an overflow there is a hole. Fill the ring buffer with
                    // silence for the size of the hole.
                    Console.Error.WriteLine ("Dropped {0} frames due to internal overflow", frameCount);
                } else {
                    for (int frame = 0; frame < frameCount; frame += 1) {
                        int chCount = instream.Layout.ChannelCount;
                        int copySize = instream.BytesPerSample;
                        unionBuffer.Bytes = new byte[copySize];
                        
                        fixed (byte* buffer = unionBuffer.Bytes)
                        {
                            for (int ch = 0; ch < chCount; ch += 1) {
                                var area = areas.GetArea (ch);
                                Buffer.MemoryCopy((void*)area.Pointer, buffer, copySize, copySize);
                                _ringBuffer.Enqueue(unionBuffer.Bytes);
                                area.Pointer += area.Step;
                            }
                        }
                    }
                }

                instream.EndRead ();

                framesLeft -= frameCount;
                if (framesLeft <= 0)
                    break;
            }

            int length = (int)_ringBuffer.GetLength();
            if (length >= 4096)
            {
                var buffer = new byte[length];
                _ringBuffer.Dequeue(buffer);
                Filled?.Invoke(this, new FillEventArgs(buffer, 0, length));
            }
        }

        public void Dispose()
        {
            api.Disconnect();
            api.Dispose();
            instream?.Dispose();
            Device?.RemoveReference();
        }
    }
}