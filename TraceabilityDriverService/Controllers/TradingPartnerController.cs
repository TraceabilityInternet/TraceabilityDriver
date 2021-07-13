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

        [HttpGet]
        [Route("{accountID}/{tradingPartnerID}")]
        public async Task<ITEDriverTradingPartner> Get(long accountID, long tradingPartnerID)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
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

        //[HttpGet]
        //[Route("{accountID}/search/{name}")]
        //public async Task<ITEDriverTradingPartnerSearchResult> Search(long accountID, string name)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
        //        {
        //            return new BadRequestObjectResult("Must provide a name of at least 3 characters long.");
        //        }

        //        throw new NotImplementedException();
        //    }
        //    catch (Exception Ex)
        //    {
        //        TELogger.Log(0, Ex);
        //        throw;
        //    }
        //}

        [HttpPost]
        [Route("{accountID}/{pglnStr}")]
        public async Task<IActionResult> Post(long accountID, string pglnStr)
        {
            try
            {
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

        [HttpPost]
        [Route("{accountID}/add_manually")]
        public async Task<IActionResult> Post(long accountID, [FromBody] ITEDriverTradingPartner tradingPartner)
        {
            try
            {
                // convert to driver trading partner and save it
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    // validate the account
                    if (await driverDB.LoadAccountAsync(accountID) == null)
                    {
                        return new NotFoundResult();
                    }
                    
                    // validate the trading partner
                    if (tradingPartner == null)
                    {
                        return new BadRequestResult();
                    }
                    else if (IPGLN.IsNullOrEmpty(tradingPartner.PGLN) 
                        || IDID.IsNullOrEmpty(tradingPartner.DID) 
                        || string.IsNullOrWhiteSpace(tradingPartner.DigitalLinkURL) 
                        || string.IsNullOrWhiteSpace(tradingPartner.Name))
                    {
                        return new BadRequestResult();
                    }

                    tradingPartner.AccountID = accountID;
                    await driverDB.SaveTradingPartnerAsync(tradingPartner);
                    return new AcceptedResult("", tradingPartner);
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
                using (ITEDriverDB driverDB = _configuration.GetDB())
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
