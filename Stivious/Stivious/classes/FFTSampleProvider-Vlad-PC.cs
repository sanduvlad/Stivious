using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;

namespace Stivious.classes
{
    class FFTSampleProvider : ISampleProvider
    {
        private ISampleProvider source;

        public event EventHandler<FFT_EventArgs> FFT_Calculated;
        public event EventHandler<FFT_WaveForm> FFT_WaveForm;

        private Complex[] fftBuffer = new Complex[1024];
        private FFT_EventArgs fftArgs;
        private FFT_WaveForm waveArgs;
        private int fftPos;
        private int wavePos;

        private int waveRate = 0;

        private double[] waveFormValue = new double[1];

        public FFTSampleProvider(ISampleProvider source)
        {
            this.source = source;

            fftArgs = new FFT_EventArgs(fftBuffer);

            waveArgs = new FFT_WaveForm(waveFormValue);
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return source.WaveFormat;
            }
        }

        public void Add(float value)
        {
            if (FFT_Calculated != null)
            {
                fftBuffer[fftPos].X = (float)(value * Blackman_NuttallWindow(fftPos, 1024));
                //fftBuffer[fftPos].X = value;
                fftBuffer[fftPos].Y = 0;
                fftPos++;

                

                if (fftPos >= 1024)
                {
                    fftPos = 0;
                    FastFourierTransform.FFT(true, 10, fftBuffer);
                    FFT_Calculated(this, fftArgs);
                }
            }

            if (FFT_WaveForm != null)
            {
                waveFormValue[0] = value;
                FFT_WaveForm(this, waveArgs);
            }
        }

        private double Blackman_NuttallWindow(double n, int N)
        {
            double a0 = 0.3635819, a1 = 0.4891775, a2 = 0.1365995, a3 = 0.0106411;

            return a0 - a1 * Math.Cos((2 * Math.PI * n) / (N - 1)) + a2 * Math.Cos((4 * Math.PI * n) / (N - 1)) - a3 * Math.Cos((6 * Math.PI * n) / (N - 1));
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int sampleRead = source.Read(buffer, offset, count);
            
            for (int i = 0; i < sampleRead; i = i +  source.WaveFormat.Channels)
            {
                Add(buffer[i + offset]);
            }

            return sampleRead;
        }
    }

    public class FFT_EventArgs : EventArgs
    {
        public Complex[] Result { get; private set; }
        public FFT_EventArgs(Complex[] result)
        {
            Result = result;
        }
    }

    public class FFT_WaveForm :EventArgs
    {
        public double[] Value { get; private set; }
        public FFT_WaveForm(double[] value)
        {
            Value = value;
        }
    }

    public class FFT_ChangeTimeRemaining : EventArgs
    {
        public TimeSpan Time { get; private set; }
        public FFT_ChangeTimeRemaining(TimeSpan time)
        {
            Time = time;
        }
    }
}
