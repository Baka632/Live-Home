using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using LiveHome.IoT;
using LiveHome.Server.Controllers;
using LiveHome.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace LiveHome.Server
{
    public class Startup
    {
        private readonly Timer Timer = new(15000) { AutoReset = true };
        private IHubContext<HomeServiceHub> hubContext;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Timer.Elapsed += Timer_Elapsed;
            Timer.Start();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log("LiveHomeServer:计时器", "滴答");
            Log("LiveHomeServer:计时器", "正在发送可燃气体信息...");
            try
            {
                bool isGasDetected = await IoTService.DetectCombustibleGas();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveCombustibleGasInfo", isGasDetected);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveCombustibleGasInfo", true);
                }
#endif
#if !DEBUG
                throw new HubException($"暂时无法获取信息,因为服务器出现了{ex.GetType().FullName}异常");
#endif
            }

            try
            {
                Log("LiveHomeServer:计时器", "正在发送环境信息...");
                EnvironmentInfo envInfo = (await IoTService.GetEnvironmentInfo()).AsEnvironmentInfoStruct();
                if (hubContext != null)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveEnvironmentInfo", JsonSerializer.Serialize(envInfo));
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                if (hubContext != null)
                {
                    Random random = new Random();
                    await hubContext.Clients.All.SendAsync("ReceiveEnvironmentInfo", JsonSerializer.Serialize(new EnvironmentInfo() { Temperature = 22.5, RelativeHumidity = 67.2 }));
                }
                return;
#endif
                //Make server happy
                //throw new HubException($"暂时无法获取温度信息,因为服务器出现了{ex.GetType().FullName}异常");
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddControllers();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LiveHome.Server", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LiveHome.Server v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                IHubContext<HomeServiceHub> hubContext1 = context.RequestServices
                                        .GetRequiredService<IHubContext<HomeServiceHub>>();
                hubContext = hubContext1;

                if (next != null)
                {
                    await next.Invoke();
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<HomeServiceHub>("/homeHub");
            });
        }

        private static void Log(string sender, string message)
        {
            Console.WriteLine($"\n{DateTime.Now} > [{sender}]{message}");
        }
    }
}
