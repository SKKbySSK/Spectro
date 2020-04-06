using System;
using System.Threading.Tasks;

namespace Spectro.Cross
{
    public class Program
    {
        public static async Task Main()
        {
            var devices = Soundio.Soundio.GetInputDevices();
            int i = 0;
            foreach (var device in devices)
            {
                Console.WriteLine($"[{i++}] {device.Name}");
            }

            Console.ReadLine();
        }
    }
}
