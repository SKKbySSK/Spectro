using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using rpi_ws281x;
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
////The default settings uses a frequency of 800000 Hz and the DMA channel 10.
//            var settings = Settings.CreateDefaultSettings();
//
////Use 16 LEDs and GPIO Pin 18.
////Set brightness to maximum (255)
////Use Unknown as strip type. Then the type will be set in the native assembly.
//            var controller =
//                settings.AddController(3, Pin.Gpio18, StripType.WS, ControllerType.PWM0, 255, false);
//
//            using (var rpi = new WS281x(settings))
//            {
//                //Set the color of the first LED of controller 0 to blue
//                controller.SetLED(0, System.Drawing.Color.Blue);
//                //Set the color of the second LED of controller 0 to red
//                controller.SetLED(1, System.Drawing.Color.Red);
//                //Set the color of the second LED of controller 0 to red
//                controller.SetLED(2, System.Drawing.Color.Green);
//                rpi.Render();
//                Thread.Sleep(10000);
//            }
//
//            
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
