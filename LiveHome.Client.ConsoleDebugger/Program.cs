using System;
using System.Threading;
using System.Threading.Tasks;
using LiveHome.IoT;

namespace LiveHome.Client.ConsoleDebugger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    (double, double) info = await IoTService.GetEnvironmentInfo();
                    if (double.IsNaN(info.Item1) && double.IsNaN(info.Item2))
                    {
                        Console.WriteLine($"出现异常!");
                        _ = Console.ReadKey();
                        break;
                    }
                    Console.WriteLine($"温度:{info.Item1:0.#}℃");
                    Console.WriteLine($"湿度:{info.Item2:0.#}%");
                    Thread.Sleep(10000);
                }
                catch (Exception)
                {
                    Console.WriteLine($"出现异常!");
                    _ = Console.ReadKey();
                    break;
                }
            }
        }
    }
}
