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
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.ObjectPooling;

namespace InternalService.Controllers
{
    [ApiController]
    [Route("{accountID}/masterdata")]
    public class MasterDataController : ControllerBase
    {
        private IConfiguration _configuration;
        private string _connectionString;
        private string _urlTemplate;
        private ITETraceabilityDriver _driver;
        bool _requiresAuthorization;

        public MasterDataController(IConfiguration configuration)
        {
            _configuration = configuration;
            _urlTemplate = _configuration.GetValue<string>("TradeItemURLTemplate");
            _connectionString = _configuration.GetValue<string>("ConnectionString");
            _requiresAuthorization = _configuration.GetValue<bool>("RequiresAuthorization");

            string dllPath = _configuration.GetValue<string>("DriverDLLPath");
            string className = _configuration.GetValue<string>("DriverClassName");
            _driver = DriverUtil.Load(dllPath, className);
        }

        [HttpGet]
        [Route("tradeitem/{gtin}")]
        public async Task<IActionResult> GetTradeItem(string gtin)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_requiresAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, gtin, driverDB))
                    {
                        // query the configured url for the epc
                        string url = _urlTemplate.Replace("{gtin}", gtin);
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITEProductMapper mapper = new ProductWebVocabMapper();
                            List<ITEProduct> products = _driver.MapToGS1TradeItems(localData);
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
        public async Task<IActionResult> GetLocation(string gln)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_requiresAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, gln, driverDB))
                    {
                        // query the configured url for the epc
                        string url = _urlTemplate.Replace("{gln}", gln);
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITEProductMapper mapper = new ProductWebVocabMapper();
                            List<ITEProduct> products = _driver.MapToGS1TradeItems(localData);
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
        [Route("tradingparty/{pgln}")]
        public async Task<IActionResult> GetTradingParty(string pgln)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    string authHeader = Request?.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    if (!_requiresAuthorization || await TradingPartnerRequestAuthorizer.Authorize(authHeader, gtin, driverDB))
                    {
                        // query the configured url for the epc
                        string url = _urlTemplate.Replace("{gtin}", gtin);
                        using (LimitedPoolItem<HttpClient> item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get())
                        {
                            HttpClient client = item.Value;
                            var response = await client.GetAsync(url);
                            string localData = await response.Content.ReadAsStringAsync();

                            // pass the results through the configured mapper
                            ITEProductMapper mapper = new ProductWebVocabMapper();
                            List<ITEProduct> products = _driver.MapToGS1TradeItems(localData);
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
    }
}
