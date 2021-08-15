using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriverService.Authentication;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Locations;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.ObjectPooling;

namespace TraceabilityDriverService.Controllers
{
    /// <summary>
    /// This is the API that receives external requests. This API requires that external requests follow the public/private key 
    /// security that is provided. Requests are then forwarded to the solution provider using the URL templates configured. 
    /// The SolutionProviderAPIKey is added to the Authorization header for forwarded requests.
    /// </summary>
    [ApiController]
    [Route("{account_id}/masterdata")]
    public class MasterDataController : ControllerBase
    {
        private ITDConfiguration _configuration;

        public MasterDataController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("tradeitem/{gtin}")]
        public async Task<IActionResult> GetTradeItem(string gtin, long account_id)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_configuration.RequiresTradingPartnerAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, gtin, driverDB, account_id))
                    {
                        long tradingpartner_id = await TradingPartnerRequestAuthorizer.GetTradingPartnerID(authHeader, driverDB);

                        // query the configured url for the trade item
                        string url = _configuration.TradeItemURLTemplate.Replace("{gtin}", gtin)
                                                                        .Replace("{tradingpartner_id}", tradingpartner_id.ToString())
                                                                        .Replace("{account_id}", account_id.ToString());

                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            client.DefaultRequestHeaders.Clear();
                            if (!string.IsNullOrWhiteSpace(_configuration.SolutionProviderAPIKey))
                            {
                                client.DefaultRequestHeaders.Add("Authorization", "Basic " + _configuration.SolutionProviderAPIKey);
                            }
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITEProductMapper mapper = new ProductWebVocabMapper();
                            List<ITEProduct> products = _configuration.Mapper.MapToGS1TradeItems(localData);
                            if (products.Count > 0)
                            {
                                string gs1Events = mapper.ConvertFromProduct(products.FirstOrDefault());
                                return new OkObjectResult(gs1Events);
                            }
                            else
                            {
                                return new NotFoundResult();
                            }
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("location/{gln}")]
        public async Task<IActionResult> GetLocation(string gln, long account_id) // added account_id
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_configuration.RequiresTradingPartnerAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, gln, driverDB, account_id))
                    {
                        long tradingpartner_id = await TradingPartnerRequestAuthorizer.GetTradingPartnerID(authHeader, driverDB);

                        // query the configured url for the epc
                        string url = _configuration.LocationURLTemplate.Replace("{gln}", gln)
                                                                       .Replace("{tradingpartner_id}", tradingpartner_id.ToString())
                                                                       .Replace("{account_id}", account_id.ToString());
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            client.DefaultRequestHeaders.Clear();
                            if (!string.IsNullOrWhiteSpace(_configuration.SolutionProviderAPIKey))
                            {
                                client.DefaultRequestHeaders.Add("Authorization", "Basic " + _configuration.SolutionProviderAPIKey);
                            }
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITELocationMapper mapper = new LocationWebVocabMapper();
                            List<ITELocation> locations = _configuration.Mapper.MapToGS1Locations(localData);
                            if (locations.Count > 0)
                            {
                                string gs1Events = mapper.ConvertFromLocation(locations.FirstOrDefault());
                                return new OkObjectResult(gs1Events);
                            }
                            else
                            {
                                return new NotFoundResult();
                            }
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("tradingparty/{pgln}")]
        public async Task<IActionResult> GetTradingParty(string pgln, long account_id)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_configuration.RequiresTradingPartnerAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, pgln, driverDB, account_id))
                    {
                        long tradingpartner_id = await TradingPartnerRequestAuthorizer.GetTradingPartnerID(authHeader, driverDB);

                        // query the configured url for the epc
                        string url = _configuration.TradingPartyURLTemplate.Replace("{pgln}", pgln).Replace("{account_id}", account_id.ToString()).Replace("{tradingpartner_id}", tradingpartner_id.ToString());
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            client.DefaultRequestHeaders.Clear();
                            if (!string.IsNullOrWhiteSpace(_configuration.SolutionProviderAPIKey))
                            {
                                client.DefaultRequestHeaders.Add("Authorization", "Basic " + _configuration.SolutionProviderAPIKey);
                            }
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITETradingPartyMapper mapper = new TradingPartyWebVocabMapper();
                            List<ITETradingParty> tps = _configuration.Mapper.MapToGS1TradingPartners(localData);
                            if (tps.Count > 0)
                            {
                                string gs1Events = mapper.ConvertFromTradingParty(tps.FirstOrDefault());
                                return new OkObjectResult(gs1Events);
                            }
                            else
                            {
                                return new NotFoundResult();
                            }
                        }
                    }
                    else
                    {
                        return new UnauthorizedResult();
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
