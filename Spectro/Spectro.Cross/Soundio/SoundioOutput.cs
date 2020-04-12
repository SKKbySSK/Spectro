using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SoundIOSharp;
using Spectro.Core;

namespace Spectro.Cross.Soundio
{
    public class OutputInitializationException : Exception
    {
        public OutputInitializationException(string message) : base(message)
        {
        }
        
        public OutputInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class SoundioOutput : IAudioOutput, IDisposable
    {
        private SoundIO api = new SoundIO();
        private SoundIOOutStream outstream;
        private RingBuffer<byte> _ringBuffer;
        private TimeSpan _bufferDuration;
        private IntervalClock _clock = new IntervalClock(TimeSpan.FromMilliseconds(50));

        public SoundioOutput(TimeSpan? bufferDuration = null)
        {
            _bufferDuration = bufferDuration ?? TimeSpan.FromSeconds(30);
            _clock.Tick += ClockOnTick;
            api.ConnectBackend(SoundIOBackend.Alsa);
            api.FlushEvents();
            
            for (int i = 0; api.OutputDeviceCount > i; i++)
            {
                var device = api.GetOutputDevice(i);
                if (i == api.DefaultOutputDeviceIndex)
                {
                    DefaultDevice = device;
                }

                Devices.Add(device);
            }
        }

        private async void ClockOnTick(object sender, EventArgs e)
        {
//            _clock.Tick -= ClockOnTick;
//            var usedSamples = outstream.BytesPerSample * Format.Channels * Format.SampleRate *
//                              _clock.Interval.TotalSeconds;
//
//            if (usedSamples >= 1)
//            {
//                var ev = new UnderflowEventArgs((int) usedSamples);
//                Underflow?.Invoke(this, ev);
//                if (ev.Buffer != null)
//                {
//                    Write(ev.Buffer, ev.Offset, ev.Count);
//                }
//            }
//            
//            _clock.Tick += ClockOnTick;
        }

        public event EventHandler UnderflowTimedOut;
        
        public event EventHandler<UnderflowEventArgs> Underflow;

        public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(1000);

        public SoundIODevice Device { get; private set; }

        public List<SoundIODevice> Devices { get; } = new List<SoundIODevice>();

        public SoundIODevice DefaultDevice { get; }
        
        public AudioFormat Format { get; private set; }
        
        public TimeSpan FillTimeout { get; set; } = TimeSpan.FromMilliseconds(100);
        
        public void Write(byte[] buffer, int offset, int count)
        {
            _ringBuffer.Enqueue(buffer);
        }

        public void SetDevice(SoundIODevice device, AudioFormat format)
        {
            Device = device;
            Format = format;
        }

        public void Initialize()
        {
            if (Device == null)
            {
                throw new Exception("No device is selected");
            }

            if (Device.ProbeError != 0)
            {
                throw new OutputInitializationException($"Probe Error : {Device.ProbeError}");
            }

            outstream = Device.CreateOutStream();
            outstream.WriteCallback = (min, max) => write_callback(outstream, min, max);
            outstream.UnderflowCallback = () => underflow_callback(outstream);
            outstream.SampleRate = 48000;

            var format = Soundio.ToSoundioFormat(Format);
            outstream.Format = format ?? SoundIOFormat.Invalid;
            
            if (outstream.LayoutErrorMessage != null)
            {
                var msg = outstream.LayoutErrorMessage;
                Console.WriteLine($"Channel Layout Error : {msg}");
            }
            
            outstream.Open();
            api.FlushEvents();
            
            Format = Soundio.ToManagedFormat(outstream.Format, outstream.SampleRate, outstream.Layout.ChannelCount);

            var bytesPerSample = outstream.BytesPerSample;
            var capacity = Format.SampleRate * Format.Channels * bytesPerSample *
                           _bufferDuration.TotalSeconds;
            _ringBuffer = new RingBuffer<byte>((uint)capacity);
        }

        public void Start()
        {
            if (outstream == null)
            {
                throw new Exception("SoundioOutput is not initialized");
            }
            
            outstream.Start();
            _clock.Start();
        }

        public void Stop()
        {
            if (outstream == null)
            {
                throw new Exception("SoundioOutput is not initialized");
            }

            _clock.Stop();
            outstream.Dispose();
            outstream = null;
        }

        unsafe void write_callback(SoundIOOutStream outstream, int frame_count_min, int frame_count_max)
        {
            double desiredSize = Latency.TotalSeconds * Format.SampleRate * Format.Channels;
            int frame_count = (int)Math.Max(frame_count_min, desiredSize);
            if (frame_count_max <= frame_count)
            {
                frame_count = frame_count_max;
            }
            frame_count = frame_count_max;

            var results = outstream.BeginWrite(ref frame_count);

            SoundIOChannelLayout layout = outstream.Layout;

            int readBytes = frame_count * outstream.BytesPerFrame;
            int readCount = 0;
            int read;
            Stopwatch sw = new Stopwatch();
            while (readBytes - readCount > 0 && sw.Elapsed < FillTimeout)
            {
                int bufferLength = (int)_ringBuffer.GetLength();
                if (bufferLength % outstream.BytesPerSample != 0)
                {
                    bufferLength -= outstream.BytesPerSample - (bufferLength % outstream.BytesPerSample);
                }
                
                read = (int) Math.Min(bufferLength, readBytes - readCount);
                readCount += read;
                
                byte[] buffer = new byte[read];
                _ringBuffer.Dequeue(buffer);
                
                for (var i = 0; i < buffer.Length; i += outstream.BytesPerSample * layout.ChannelCount)
                {
                    for (int channel = 0; layout.ChannelCount > channel; channel++)
                    {
                        var area = results.GetArea(channel);
                        write_sample16(area.Pointer, BitConverter.ToInt16(buffer, i));
                        area.Pointer += area.Step;
                    }
                }
            }

            outstream.EndWrite();

            if (readBytes - readCount > 0)
            {
                Task.Run(() => { UnderflowTimedOut?.Invoke(this, EventArgs.Empty); });
            }

            unsafe void write_sample16(IntPtr ptr, short sample)
            {
                short* buf = (short*)ptr;
                *buf = sample;
            }

            unsafe void write_sample8(IntPtr ptr, byte sample)
            {
                byte* buf = (byte*)ptr;
                *buf = sample;
            }
        }

        void underflow_callback(SoundIOOutStream outstream)
        {
        }

        public void Dispose()
        {
            api?.Disconnect();
            api?.Dispose();
            api = null;
            
            outstream?.Dispose();
            outstream = null;
            
            Device?.RemoveReference();
            Device = null;
        }
    }
}