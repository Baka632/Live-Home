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
            Console.WriteLine("等待...");
            Console.ReadKey();
            try
            {
                await IoTService.OnOrOffBuzzer(true);
                Console.WriteLine("等待...");
                _ = Console.ReadKey();
                await IoTService.OnOrOffBuzzer(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("出现问题!");
                Console.WriteLine($"\n异常信息:{ex.Message}");
                Console.WriteLine($"\n堆栈跟踪:{ex.StackTrace}");
                Console.WriteLine($"\n内部异常:{ex.InnerException}");
                Console.WriteLine("等待...");
                _ = Console.ReadKey();
            }
        }
    }
}
