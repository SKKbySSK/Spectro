using System;

namespace Spectro.Core
{
    public class Visualizer
    {
        private Analyzer analyzer;
        private BufferSink<byte> sink;
        
        public Visualizer(int fftLength, int sampleRate)
        {
            analyzer = new Analyzer(fftLength, sampleRate);
            sink = new BufferSink<byte>(fftLength);
            sink.Filled += SinkOnFilled;
        }

        private void SinkOnFilled(object sender, EventArgs e)
        {
            Ready = true;
        }

        public bool Ready { get; private set; } = false;

        public void Push(byte[] buffer)
        {
            sink.Push(buffer, false);
        }

        public void Analyze()
        {
            if (!sink.IsFilled)
            {
                return;
            }

            var buffer = sink.Pop();
            analyzer.Fft(buffer, 0);
        }
    }
}