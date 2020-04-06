using System;
using System.Threading.Tasks;
using Spectro.Core;
using Spectro.Cross.Soundio;

namespace Spectro.Cross
{
    public class Program
    {
        private static int[] priortizedSampleRate =
        {
            48000,
            44100,
        };
        
        public static async Task Main()
        {
            var devices = Soundio.Soundio.GetInputDevices();
            Console.WriteLine("Input devices");
            for (var i = 0; devices.Length > i; i++)
            {
                Console.WriteLine($"[{i}] {devices[i].Name}");
            }

            var deviceIndex = ReadInt("Input device index : ");
            var inputDevice = devices[deviceIndex];
            
            for (var i = 0; devices.Length > i; i++)
            {
                if (i != deviceIndex)
                {
                    devices[i].RemoveReference();
                }
            }
            
            var input = new SoundioInput(inputDevice);

            AudioFormat format = null;

            for (int i = 0; priortizedSampleRate.Length > i; i++)
            {
                var f = new AudioFormat(priortizedSampleRate[i]);
                var result = await input.SupportsFormatAsync(f);
                if (result == FormatResult.Ok)
                {
                    format = f;
                    break;
                }
            }

            if (format == null)
            {
                Console.WriteLine("There is no supported format available");
                return;
            }

            await input.InitializeAsync(format);
            Console.WriteLine("Device initialized");
        }

        public static int ReadInt(string message, string errorMessage = "Try Again : ")
        {
            int num;
            Console.Write(message);
            while (!int.TryParse(Console.ReadLine(), out num))
            {
                Console.Write(errorMessage);
            }

            return num;
        }
    }
}
