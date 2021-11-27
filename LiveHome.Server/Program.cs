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
            Console.WriteLine($"\n{DateTime.Now} > [Live Home Server:��ڵ㷽��]\n�������� WR-8 ���� �ж���\nϦ:��δ�����ɡ�������֮����ټ٣����Ƕ����ҵ�ڵ�ʡ�\n�϶�:���������ɡ�\nϦ:�����ҽ��ǻ����ˡ�");
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
