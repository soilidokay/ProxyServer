using Core.ProxyServer.ComponentBase;
using Core.ProxyServer.Components;
using Core.ProxyServer.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProxyServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();
            //mở cổng lắng nghe trên port 8215
            BaseListener baseListener = new BaseListener(IPAddress.Any, 8215);
            baseListener.OnAcept += new Core.ProxyServer.Deleagates.OnAceptListener(onAccept);
            baseListener.RunAsync().Wait();
        }

        //chấp nhận client và mở bắt đầu chuyển tiếp dữ liệu
        private static void onAccept(StateListener state)
        {
            _ = Task.Run(async () =>
            {
                using var client = new RemoteChannel(state.Client);
                await client.StartAsync();
            });
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
