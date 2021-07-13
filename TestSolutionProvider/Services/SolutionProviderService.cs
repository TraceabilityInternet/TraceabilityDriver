using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TestDriver;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Service.Util;

namespace TestSolutionProvider.Services
{
    public class SolutionProviderService : ISolutionProviderService
    {
        private HttpClient _client;

        public long AccountID { get; set; }
        public long TradingPartnerID { get; set; }
        public string TraceabilityDriverURL { get; set; }
        public string TraceabilityDriverAPIKey { get; set; }
        public string DataURL { get; set; }
        public ITETraceabilityMapper Mapper { get; set; }

        public SolutionProviderService(IConfiguration configuration, IHttpContextAccessor accessor)
        {
            string mapperDLLPath = configuration.GetValue<string>("MapperDLLPath");
            string mapperClassName = configuration.GetValue<string>("MapperClassName");
            if (!string.IsNullOrWhiteSpace(mapperDLLPath) && !string.IsNullOrWhiteSpace(mapperClassName))
            {
                this.Mapper = DriverUtil.LoadMapper(mapperDLLPath, mapperClassName);
            }
            this.DataURL = configuration.GetValue<string>("DataURL");
            this.AccountID = configuration.GetValue<long>("AccountID");
            this.TradingPartnerID = configuration.GetValue<long>("TradingPartnerID");
            this.TraceabilityDriverURL = configuration.GetValue<string>("TraceabilityDriverURL");
            this.TraceabilityDriverAPIKey = configuration.GetValue<string>("TraceabilityDriverAPIKey");

#if DEBUG
            this.DataURL = this.DataURL ?? "xml";
            if (this.Mapper == null)
            {
                this.Mapper = new XmlTestDriver();
            }
#endif


            // we need to get the address of the web application to use for talking to the controllers.
            var url = accessor.HttpContext.Request.Scheme + "://" + accessor.HttpContext.Request.Host + accessor.HttpContext.Request.PathBase;
            this._client = new HttpClient();
            this._client.BaseAddress = new Uri(url);
        }

        public async Task<List<ITEEvent>> GetEventsAsync()
        {
            string dataStr = await GetData("events");
            var events = this.Mapper.MapToGS1Events(dataStr, null);
            return events;
        }

        public async Task<List<ITEProduct>> GetProductsAsync()
        {
            string dataStr = await GetData("tradeitems");
            var tradeitems = this.Mapper.MapToGS1TradeItems(dataStr);
            return tradeitems;
        }

        public async Task<List<ITELocation>> GetLocationsAsync()
        {
            string dataStr = await GetData("locations");
            var locations = this.Mapper.MapToGS1Locations(dataStr);
            return locations;
        }

        public async Task<List<ITETradingParty>> GetTradingPartiesAsync()
        {
            string dataStr = await GetData("tradingparties");
            var tradingparties = this.Mapper.MapToGS1TradingPartners(dataStr);
            return tradingparties;
        }

        public async Task<string> GetData(string path)
        {
            string url = $"{this.DataURL}/{path}";
            HttpResponseMessage response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();
                throw new Exception("Failed to register the account.");
            }
            else
            {
                string localFormatEvents = await response.Content.ReadAsStringAsync();
                return localFormatEvents;
            }
        }

        public async Task<string> GetRawData(string identifier)
        {
            string url = $"{this.DataURL}/raw/{identifier}";
            HttpResponseMessage response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string responseStr = await response.Content.ReadAsStringAsync();
                return "Failed to get the raw data...";
            }
            else
            {
                string rawData = await response.Content.ReadAsStringAsync();
                return rawData;
            }
        }

        public async Task RequestData(string epc)
        {
            using (var client = TEClientFactory.InternalClient(this.TraceabilityDriverURL, this.TraceabilityDriverAPIKey))
            {
                string localFormat = await client.GetEventsAsync(this.AccountID, this.TradingPartnerID, epc);
                await SaveData(localFormat, "event");

                // now we are going to request the GTINs
                List<ITEEvent> events = Mapper.MapToGS1Events(localFormat, null);
                foreach (var cte in events)
                {
                    // request the gtins
                    foreach (var piRef in cte.Products)
                    {
                        IGTIN gtin = piRef.EPC.GTIN;
                        string localGTINs = await client.GetTradeItemAsync(this.AccountID, this.TradingPartnerID, gtin.ToString());
                        await SaveData(localGTINs, "tradeitem");
                    }

                    // request the trading parties
                    string localTP = await client.GetTradingPartyAsync(this.AccountID, this.TradingPartnerID, cte.DataOwner?.ToString());
                    await SaveData(localTP, "tradingparty");
                    localTP = await client.GetTradingPartyAsync(this.AccountID, this.TradingPartnerID, cte.Owner?.ToString());
                    await SaveData(localTP, "tradingparty");

                    // request the location 
                    string locationData = await client.GetLocationAsync(this.AccountID, this.TradingPartnerID, cte.Location.GLN?.ToString());
                    await SaveData(locationData, "location");
                }
            }
        }

        private async Task SaveData(string data, string type)
        {
            // now we are going to save the data...
            string url = $"{this.DataURL}/save/{type}";
            var content = new StringContent(data, Encoding.UTF8, "text/plain");
            HttpResponseMessage response = await _client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                string responseError = await response.Content.ReadAsStringAsync();
                throw new Exception("Failed to request EPC data.");
            }
        }
    }
}
