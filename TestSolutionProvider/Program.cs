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

namespace TestSolutionProvider
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static void Start(string url, string mapperDLLPath, string mapperClass, string dataURL, long accountID, long tradingPartnerID, string traceDiverURL, string traceDriveAPIKey)
        {
            Host.CreateDefaultBuilder(new string[] { })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSetting("AccountID", accountID.ToString());
                    webBuilder.UseSetting("TradingPartnerID", tradingPartnerID.ToString());
                    webBuilder.UseSetting("TraceabilityDriverURL", traceDiverURL);
                    webBuilder.UseSetting("TraceabilityDriverAPIKey", traceDriveAPIKey);
                    webBuilder.UseSetting("MapperDLLPath", mapperDLLPath);
                    webBuilder.UseSetting("MapperClassName", mapperClass);
                    webBuilder.UseSetting("DataURL", dataURL);
                    webBuilder.UseEnvironment("Development");
                    webBuilder.UseUrls(new string[] { url });
                    webBuilder.UseStaticWebAssets();
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
