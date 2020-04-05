using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace Spectro.Core
{
    public class Analyzer
    {
        private readonly Complex[] fftResult;
        private readonly double[] window;
        
        public Analyzer(int size, int sampleRate)
        {
            Size = size;
            SampleRate = sampleRate;
            FrequencyResolution = sampleRate / (double)size;
            fftResult = new Complex[size];
            window = Window.HammingPeriodic(size);
        }
        
        public int Size { get; }
        
        public int SampleRate { get; }
        
        public double FrequencyResolution { get; }
        
        public unsafe void Fft(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < Size)
            {
                throw new ArgumentException();
            }

            int size = Size;
            fixed (byte* buf = buffer)
            fixed (double* win = window)
            fixed (Complex* res = fftResult)
            {
                for (int i = offset; size > i; i++)
                {
                    res[i] = new Complex(buf[i] * win[i], 0);
                }
            }

            MathNet.Numerics.IntegralTransforms.Fourier.Forward(fftResult);
        }

        public int GetIndex(double freq)
        {
            var index = (int)Math.Round(freq / FrequencyResolution);
            return Math.Min(index, Size);
        }

        public double GetFrequency(int index)
        {
            return index * FrequencyResolution;
        }

        public unsafe double[] GetPowerSpectrum(int offset, int endIndex)
        {
            var spectrum = new double[endIndex + 1 - offset];
            if (endIndex > Size || offset > endIndex)
            {
                throw new IndexOutOfRangeException();
            }
            
            fixed (Complex* res = fftResult)
            {
                Complex val;
                for (int i = offset; endIndex >= i; i++)
                {
                    val = res[i];
                    spectrum[i - offset] = val.Magnitude;
                }
            }

            return spectrum;
        }

        public double[] GetPowerSpectrum(int offset = 0)
        {
            return GetPowerSpectrum(offset, Size - 1);
        }

        /// <summary>
        /// Calculate dBFS (Decibels relative to full scale)
        /// </summary>
        /// <returns></returns>
        public double[] GetDBFS(int offset, int endIndex)
        {
            var power = GetPowerSpectrum(offset, endIndex);

            for (int i = 0; power.Length > i; i++)
            {
                // https://www.kvraudio.com/forum/viewtopic.php?t=276092
                power[i] = 20 * Math.Log10(2 * power[i] / Size);
            }

            return power;
        }

        /// <summary>
        /// Calculate dBFS (Decibels relative to full scale)
        /// </summary>
        /// <returns></returns>
        public double[] GetDBFS(int offset)
        {
            return GetDBFS(offset, Size - 1);
        }

        public double[] GetDB(int offset, int endIndex)
        {
            var power = GetPowerSpectrum(offset, endIndex);

            for (int i = 0; power.Length > i; i++)
            {
                power[i] = 20 * Math.Log10(power[i]);
            }

            return power;
        }

        public double[] GetDB(int offset)
        {
            return GetDB(offset, Size - 1);
        }
    }
}
