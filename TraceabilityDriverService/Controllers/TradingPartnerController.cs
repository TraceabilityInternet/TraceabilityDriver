using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Controllers
{
    [ApiController]
    [Route("api/tradingpartner")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class TradingPartnerController
    {
        private readonly ITDConfiguration _configuration;

        public TradingPartnerController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// HTTP request to return a Trading Partner of an Account by ID.
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="tradingPartnerID"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}")]
        public async Task<ActionResult<ITEDriverTradingPartner>> Get(long accountID, long tradingPartnerID)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(accountID, tradingPartnerID);
                    return new ActionResult<ITEDriverTradingPartner>(tp);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        /// <summary>
        /// HTTP request to save a Trading Partner to an Account.
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="pglnStr"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{accountID}/{pglnStr}")]
        public async Task<ActionResult<ITEDriverTradingPartner>> Post(long accountID, string pglnStr)
        {
            try
            {
                // validation
                if(accountID == 0)
                {
                    return new BadRequestObjectResult("Account ID is not set");
                }
                if (string.IsNullOrEmpty(pglnStr))
                {
                    return new BadRequestObjectResult("PGLN is null or empty");
                }


                IPGLN pgln = IdentifierFactory.ParsePGLN(pglnStr);

                // we need to download trading partner from the directory
                using (ITEDirectoryClient client = TEClientFactory.DirectoryClient(_configuration.ServiceProviderDID, _configuration.DirectoryURL))
                {
                    ITEDirectoryAccount account = await client.GetAccountAsync(pgln);
                    if (account == null)
                    {
                        return new NotFoundResult();
                    }

                    // convert to driver trading partner and save it
                    using (ITEDriverDB driverDB = _configuration.GetDB())
                    {
                        ITEDriverTradingPartner tradingPartner = account.ToDriverTradingPartner();
                        tradingPartner.AccountID = accountID;
                        await driverDB.SaveTradingPartnerAsync(tradingPartner);

                        return new ActionResult<ITEDriverTradingPartner>(tradingPartner);
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        /// <summary>
        /// HTTP Request to delete a Trading Partner from an Account.
        /// </summary>
        /// <param name="accountID"></param>
        /// <param name="tradingPartnerID"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{accountID}/{tradingPartnerID}")]
        public async Task<IActionResult> Delete(long accountID, long tradingPartnerID)
        {
            try
            {
                // validation
                if (accountID < 1)
                {
                    return new BadRequestObjectResult("Account ID not properly set");
                }

                if (tradingPartnerID < 1)
                {
                    return new BadRequestObjectResult("Trading Partner ID not properly set");
                }

                // we will delete it if it exists, otherwise do nothing
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    await driverDB.DeleteTradingPartnerAsync(accountID, tradingPartnerID);
                }

                return new OkResult();

            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
