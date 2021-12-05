using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using Iot.Device.Ssd13xx.Commands.Ssd1306Commands;
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
                await IoTService.GetEnvironmentInfo();
                await IoTService.DetectCombustibleGas();
                await IoTService.WriteEnviromentInfoToLCD();
                await IoTService.LightOnOrOffLED(true, 16);
                Console.WriteLine("等待...");
                _ = Console.ReadKey();
                await IoTService.LightOnOrOffLED(false, 16);
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

        private static void CaptureMe()
        {
            
        }
    }
}
