using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestSolutionProviderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static void Start(string url)
        {
            Host.CreateDefaultBuilder(new string[] { })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSetting("URL", url);
                    webBuilder.UseEnvironment("Development");
                    webBuilder.UseUrls(new string[] { url });
                    webBuilder.UseStartup<Startup>();
                }).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
