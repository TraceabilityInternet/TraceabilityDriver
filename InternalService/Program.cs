using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Util.Interfaces;

namespace InternalService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static void Start(string url, string directoryURL, string apiKey, IDID serviceProvider, string driverDLL, string driverClass, string connectionString)
        {
            Host.CreateDefaultBuilder(new string[] { })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSetting("APIKey", apiKey);
                    webBuilder.UseSetting("ServiceProvider", serviceProvider.ToString());
                    webBuilder.UseSetting("DriverDLL", driverDLL);
                    webBuilder.UseSetting("DriverClass", driverClass);
                    webBuilder.UseSetting("DirectoryURL", directoryURL);
                    webBuilder.UseSetting("URL", url);
                    webBuilder.UseSetting("ConnectionString", connectionString);
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
