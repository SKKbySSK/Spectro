using System;
using System.IO;
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

        private static FileStream fs = new FileStream("test.raw", FileMode.Create);
        
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

            AudioFormat format = new AudioFormat(44100, 2);

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
                if (ReadYesNo("Continue? [yes/no] : "))
                {
                    var sampleRate = ReadInt("Sample rate : ");
                    format = new AudioFormat(sampleRate);
                }
            }

            await input.InitializeAsync(format);
            Console.WriteLine("Device initialized");

            input.Filled += InputOnFilled;
            await input.StartAsync();

            Console.ReadKey();
            await input.StopAsync();
            input.Filled -= InputOnFilled;

            fs.Dispose();
            input.Dispose();
        }

        private static async void InputOnFilled(object sender, FillEventArgs e)
        {
            await fs.WriteAsync(e.Buffer.Bytes, e.Offset, e.Count);
        }

        public static int ReadInt(string message, string errorMessage = "Try Again [yes/no] : ")
        {
            int num;
            Console.Write(message);
            while (!int.TryParse(Console.ReadLine(), out num))
            {
                Console.Write(errorMessage);
            }

            return num;
        }

        public static bool ReadYesNo(string message, string errorMessage = "Try Again : ")
        {
            Console.Write(message);
            string res = Console.ReadLine()?.ToLower() ?? "";
            while (res != "yes" && res != "no")
            {
                Console.Write(errorMessage);
                res = Console.ReadLine()?.ToLower() ?? "";
            }

            return res == "yes";
        }
    }
}
