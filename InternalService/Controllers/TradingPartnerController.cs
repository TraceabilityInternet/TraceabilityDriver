using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace InternalService.Controllers
{
    [ApiController]
    [Route("api/tradingpartner")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradingPartnerController
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _directoryURL;
        private readonly IDID _serviceProvider;

        public TradingPartnerController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration["ConnectionString"];
            _directoryURL = _configuration["DirectoryURL"];
            _serviceProvider = DIDFactory.Parse(_configuration["ServiceProvider"]);
        }

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}")]
        public async Task<ITEDriverTradingPartner> Get(long accountID, long tradingPartnerID)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                    return tp;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpPost]
        [Route("{accountID}/{pglnStr}")]
        public async Task<IActionResult> Post(long accountID, string pglnStr)
        {
            try
            {
                IPGLN pgln = IdentifierFactory.ParsePGLN(pglnStr);

                // we need to download trading partner from the directory
                using (ITEDirectoryClient client = TEClientFactory.DirectoryClient(_serviceProvider, _directoryURL))
                {
                    ITEDirectoryAccount account = await client.GetAccountAsync(pgln);
                    if (account == null)
                    {
                        return new NotFoundResult();
                    }

                    // convert to driver trading partner and save it
                    using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                    {
                        ITEDriverTradingPartner tradingPartner = account.ToDriverTradingPartner();
                        tradingPartner.AccountID = accountID;
                        await driverDB.SaveTradingPartnerAsync(tradingPartner);

                        return new AcceptedResult("", tradingPartner);
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpDelete]
        [Route("{accountID}/{tradingPartnerID}")]
        public async Task Delete(long accountID, long tradingPartnerID)
        {
            try
            {
                // we will delete it if it exists, otherwise do nothing
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    await driverDB.DeleteTradingPartnerAsync(accountID, tradingPartnerID);
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
