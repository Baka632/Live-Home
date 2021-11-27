using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using LiveHome.IoT;
using LiveHome.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace LiveHome.Server.Controllers
{
    public class HomeServiceHub : Hub
    {
        static HomeServiceHub()
        {
            Log("HomeServiceHub:类型对象构造器", "\n「随风飘荡」\n年轻的欣特莱雅在寻找自己今后的人生道路，在年龄与阅历增长之后，她兀然发现，自己早已没了选择的权力，只能随着时代的潮流不断飘荡。");
        }

        public async Task SendGasInfoToAll(bool message)
        {
            Log("HomeServiceHub:Hub", "正在发送可燃气体信息...");
            await Clients.All.SendAsync("ReceiveCombustibleGasInfo", message);
        }

        public async Task SendEnvironmentInfoToAll(string message)
        {
            Log("HomeServiceHub:Hub", "正在发送环境信息...");
            await Clients.All.SendAsync("ReceiveEnvironmentInfo", message);
        }

        public async Task<string> GetEnvironmentInfo()
        {
            try
            {
                (double, double) values = await IoTService.GetEnvironmentInfo();
                if (double.IsNaN(values.Item1) && double.IsNaN(values.Item2))
                {
                    values.Item1 = IoTService.LastSuccessEnvInfo.Item1;
                    values.Item2 = IoTService.LastSuccessEnvInfo.Item2;
                }
                return JsonSerializer.Serialize(values.AsEnvironmentInfoStruct());
            }
            catch (Exception ex)
            {
#if DEBUG
                return JsonSerializer.Serialize(new EnvironmentInfo() { Temperature = 11451.4, RelativeHumidity = 81.0 });
#endif
                throw new HubException($"暂时无法获取温度信息,因为服务器出现了{ex.GetType().FullName}异常");
            }
        }
        
        public async Task<bool> GetCombustibleGasInfo()
        {
            try
            {
                bool value = await IoTService.DetectCombustibleGas();
                return value;
            }
            catch (Exception ex)
            {
#if DEBUG
                return true;
#endif
                throw new HubException($"暂时无法获取信息,因为服务器出现了{ex.GetType().FullName}异常");
            }
        }


        public override Task OnConnectedAsync()
        {
            Log("HomeServiceHub:Hub", $"用户{Context.UserIdentifier}已经连接");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (exception is null)
            {
                Log("HomeServiceHub:Hub", $"用户{Context.UserIdentifier}断开了连接");
            }
            else
            {
                Log("HomeServiceHub:Hub", $"用户{Context.UserIdentifier}意外的断开了连接\n详细信息:{exception.Message}");
                return base.OnDisconnectedAsync(null);
            }
            return base.OnDisconnectedAsync(exception);
        }

        private static void Log(string sender, string message)
        {
            Console.WriteLine($"\n{DateTime.Now} > [{sender}]{message}");
        }
    }
}
