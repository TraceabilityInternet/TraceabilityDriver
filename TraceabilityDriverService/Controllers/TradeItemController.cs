using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/tradeitems")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradeItemController : ControllerBase
    {
        private readonly ITDConfiguration _configuration;

        public TradeItemController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}/{gtin}")]
        public async Task<string> Get(long accountID, long tradingPartnerID, string gtin)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    ITEDriverAccount account = await driverDB.LoadAccountAsync(accountID);
                    ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                    string authHeader = TradingPartnerRequestAuthorizer.GenerateAuthHeader(gtin, account, tp);
                    string url = tp.DigitalLinkURL + $"/gtin/{gtin}?linkType=gs1:masterData";

                    using (var item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                    {
                        HttpClient client = item.Value;
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", authHeader);
                        var response = await client.GetAsync(url);
                        string linkJson = await response.Content.ReadAsStringAsync();
                        JArray jArr = JArray.Parse(linkJson); // Edited Jobject to Jarray
                        string masterDataURL = jArr[0].Value<string>("link"); // "url" to "link", added [0] index

                        // now we have the link to the master data
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", authHeader);
                        var response2 = await client.GetAsync(masterDataURL); // Edited url to masterDataURL
                        string gs1Format = await response2.Content.ReadAsStringAsync();
                        ITEProductMapper mapper = new ProductWebVocabMapper();
                        ITEProduct product = mapper.ConvertToProduct(gs1Format);
                        string localFormat = _configuration.Mapper.MapToLocalTradeItems(new List<ITEProduct>() { product });
                        return localFormat;
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
