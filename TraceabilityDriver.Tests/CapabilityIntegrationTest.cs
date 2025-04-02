using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// A full integration test that stands up the TraceabilityDriver and then executes the capability
    /// test against it to ensure that the capability test is functioning correctly.
    /// </summary>
    public class CapabilityIntegrationTest
    {
        IWebHost _webHost = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Test.json")
                .Build();

            _webHost = new TestWebApplicationFactory().CreateHostBuilder(config, "http://localhost:5111");
            _webHost.Start();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_webHost != null)
            {
                _webHost.StopAsync().Wait();
                _webHost.Dispose();
            }
        }

        [Test]
        public void TestCapability()
        {

        }
    }
}
