using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveHome.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"\n{DateTime.Now} > [Live Home Server:入口点方法]\n「画中人 WR-8 大梦 行动后」\n夕:人未必自由。我所画之真真假假，皆是对自我的诘问。\n嵯峨:人生当自由。\n夕:错，你我皆是画中人。");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
