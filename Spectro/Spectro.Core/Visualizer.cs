using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Spectro.Core
{
    public class VisualizerConfig
    {
        public VisualizerConfig(IAudioInput input, IVisualizingOutput output, IAudioOutput passthroughOutput = null)
        {
            Input = input;
            Output = output;
            PassthroughOutput = passthroughOutput;
        }

        public IAudioInput Input { get; }
        
        public IVisualizingOutput Output { get; }
        
        public IAudioOutput PassthroughOutput { get; set; }
    }
    
    public class Visualizer
    {
        private readonly Analyzer _analyzer;
        private byte[] _buffer;
        private BufferSink<byte> _sink;
        private int _bufferIndex = 0;
        
        public Visualizer(int fftLength, int sampleRate, VisualizerConfig config, Clock clock = null)
        {
            _analyzer = new Analyzer(fftLength, sampleRate);

            var inFormat = config.Input.Format;
            _sink = new BufferSink<byte>(fftLength * inFormat.BitDepth / 8 * inFormat.Channels);
            
            int bytesPerSample = config.Input.Format.BitDepth / 8;
            _buffer = new byte[_analyzer.Size * bytesPerSample];
            
            Config = config;
            
            Clock = clock ?? new IntervalClock(TimeSpan.FromMilliseconds(10));
            Clock.Tick += ClockOnTick;
            
            config.Input.Filled += InputOnFilled;
            config.PassthroughOutput.Underflow += PassthroughOutputOnUnderflow;
            config.PassthroughOutput.UnderflowTimedOut += PassthroughOutputOnUnderflowTimedOut;
        }

        private void PassthroughOutputOnUnderflowTimedOut(object sender, EventArgs e)
        {
            Console.WriteLine("Timed out");
        }

        private void PassthroughOutputOnUnderflow(object sender, UnderflowEventArgs e)
        {
            var size = e.Size ?? _analyzer.Size * Config.PassthroughOutput.Format.BitDepth / 8;
            var buffer = _sink.Pop(size);
            Config.PassthroughOutput.Write(buffer, 0, buffer.Length);
        }

        private void InputOnFilled(object sender, FillEventArgs e)
        {
            if (e.Count == 0)
            {
                return;
            }

            try
            {
                Array.Copy(e.Buffer, e.Offset, _buffer, _bufferIndex, e.Count);
                _bufferIndex += e.Count;

                if (_bufferIndex == _buffer.Length)
                {
                    _sink.Push(_buffer, false);
                    _bufferIndex = 0;
                    int bytesPerSample = Config.Input.Format.BitDepth / 8;
                    _buffer = new byte[_analyzer.Size * bytesPerSample];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error!" + ex);
            }
        }

        public Clock Clock { get; }
        
        public VisualizerConfig Config { get; }

        private async void ClockOnTick(object sender, EventArgs e)
        {
            Clock.Tick -= ClockOnTick;
            try
            {
                await AnalyzeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error!" + ex);
            }
            Clock.Tick += ClockOnTick;
        }

        public bool Ready { get; private set; } = false;

        public bool AutoStart { get; set; } = true;

        public async Task AnalyzeAsync(byte[] buffer)
        {
            _analyzer.Fft(buffer, 0, Config.Input.Format.BitDepth);
            //await Config.Output.UpdateAsync(_analyzer);
        }

        public async Task AnalyzeAsync()
        {
//            byte[] buffer;
//            while (buffers.TryDequeue(out buffer))
//            {
//                await AnalyzeAsync(buffer);
//            }
        }
    }
}
