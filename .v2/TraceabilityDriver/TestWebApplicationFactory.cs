using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraceabilityDriver;

namespace TraceabilityDriver;

public class TestWebApplicationFactory
{
    public IWebHost CreateHostBuilder(IConfiguration config, string url)
    {
        var webhost = WebHost.CreateDefaultBuilder(args: new string[] { })
                                .UseStartup<Startup>()
                                .UseEnvironment("Test")
                                .UseConfiguration(config)
                                .UseKestrel()
                                .UseUrls(url)
                                .Build();

        webhost.Start();
        return webhost;
    }
}
