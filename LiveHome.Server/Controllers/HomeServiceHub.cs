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
        public async Task SendGasInfoToAll(bool message)
        {
            Console.WriteLine($"{DateTime.Now} > [HomeServiceHub:Hub]正在发送可燃气体信息...");
            await Clients.All.SendAsync("ReceiveCombustibleGasInfo", message);
        }

        public async Task SendEnvironmentInfoToAll(string message)
        {
            Console.WriteLine($"{DateTime.Now} > [HomeServiceHub:Hub]正在发送环境信息...");
            await Clients.All.SendAsync("ReceiveEnvironmentInfo", message);
        }

        public async Task<string> GetEnvironmentInfo()
        {
            try
            {
                (double, double) values = await IoTService.GetEnvironmentInfo();
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
            Console.WriteLine($"{DateTime.Now}  > [HomeServiceHub:Hub]用户{Context.UserIdentifier}已经连接");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (exception is null)
            {
                Console.WriteLine($"{DateTime.Now} > [HomeServiceHub:Hub]用户{Context.UserIdentifier}断开了连接");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} > [HomeServiceHub:Hub]用户{Context.UserIdentifier}意外的断开了连接");
                Console.WriteLine($"详细信息:{exception.Message}");
                return base.OnDisconnectedAsync(null);
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
