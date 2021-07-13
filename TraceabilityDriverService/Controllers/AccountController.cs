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
    [Route("api/account")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class AccountController
    {
        private readonly ITDConfiguration _configuration;

        public AccountController(ITDConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// An HTTP GET request that loads an Account from the MongoDB based on its account_ID or its PGLN.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<ITEDriverAccount>> Get(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    // What to return here?
                    return new BadRequestObjectResult("Account ID is not properly set.");
                }

                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    if (long.TryParse(id, out long longID))
                    {
                        // load by the id
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(longID);
                        return new ActionResult<ITEDriverAccount>(account);
                    }
                    else
                    {
                        // load by the PGLN
                        IPGLN pgln = IdentifierFactory.ParsePGLN(id);
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(pgln);
                        return new ActionResult<ITEDriverAccount>(account);
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
        /// An HTTP POST that registers an Account with the Directory and saves the Account to the MongoDB.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<ITEDriverAccount>> Post([FromBody] ITEDriverAccount account)
        {
            try
            {
                string configURL = _configuration.URL;

                // validation, consider adding an accountID check?
                if (account == null)
                {
                    return new BadRequestObjectResult("Account was not properly set.");
                }

                if (IPGLN.IsNullOrEmpty(account.PGLN))
                {
                    return new BadRequestObjectResult("The account is required to have a PGLN. If youa re probiding a PGLN, ensure it is in the correct format.");
                }

                if(string.IsNullOrEmpty(account.Name))
                {
                    return new BadRequestObjectResult("The account must have a name.");
                }

                if (IDID.IsNullOrEmpty(account.DID))
                {
                    account.DID = DID.GenerateNew(); // first setting of DID.
                }


                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    await driverDB.SaveAccountAsync(account, configURL);
                }

                // now we are going to register the account with the directory
                using (ITEDirectoryClient client = TEClientFactory.DirectoryClient(_configuration.ServiceProviderDID, _configuration.DirectoryURL))
                {
                    ITEDirectoryNewAccount newAccount = account.ToDirectoryAccount(_configuration.ServiceProviderDID, _configuration.ServiceProviderPGLN);
                    await client.RegisterAccountAsync(newAccount);
                }

                //return new AcceptedResult("", account);
                return new ActionResult<ITEDriverAccount>(account);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
