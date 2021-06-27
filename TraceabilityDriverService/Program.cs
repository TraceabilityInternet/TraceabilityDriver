using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityDriverService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static void Start(ITDConfiguration configuration)
        {
            Host.CreateDefaultBuilder(new string[] { })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSetting("EventURLTemplate", configuration.EventURLTemplate);
                    webBuilder.UseSetting("TradeItemURLTemplate", configuration.TradeItemURLTemplate);
                    webBuilder.UseSetting("LocationURLTemplate", configuration.LocationURLTemplate);
                    webBuilder.UseSetting("TradingPartnerURLTemplate", configuration.TradingPartnerURLTemplate);
                    webBuilder.UseSetting("RequiresTradingPartnerAuthorization", configuration.RequiresTradingPartnerAuthorization.ToString());
                    webBuilder.UseSetting("URL", configuration.URL);
                    webBuilder.UseSetting("DirectoryURL", configuration.DirectoryURL);
                    webBuilder.UseSetting("ConnectionString", configuration.ConnectionString);
                    webBuilder.UseSetting("ServiceProviderDID", configuration.ServiceProviderDID?.ToString());
                    webBuilder.UseSetting("ServiceProviderPGLN", configuration.ServiceProviderPGLN?.ToString());
                    webBuilder.UseSetting("APIKey", configuration.APIKey);
                    webBuilder.UseSetting("MapperDLLPath", configuration.MapperDLLPath);
                    webBuilder.UseSetting("MapperClassName", configuration.MapperClassName);
                    webBuilder.UseEnvironment("Development");
                    webBuilder.UseUrls(configuration.URL);
                    webBuilder.UseStartup<Startup>();
                }).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
