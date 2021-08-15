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

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                using (ITEDriverDB driverDB = _configuration.GetDB())
                {
                    if (long.TryParse(id, out long longID))
                    {
                        // load by the id
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(longID);
                        return new OkObjectResult(account);
                    }
                    else
                    {
                        // load by the PGLN
                        IPGLN pgln = IdentifierFactory.ParsePGLN(id);
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(pgln);
                        return new OkObjectResult(account);
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
        public async Task<IActionResult> Post([FromBody] ITEDriverAccount account)
        {
            try
            {
                string configURL = _configuration.URL;

                if (account == null)
                {
                    return new BadRequestObjectResult("The account is not provided.");
                }

                if (string.IsNullOrWhiteSpace(account.Name))
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

                // now we are going to register the account with the directory service if we have it configured
                if (!string.IsNullOrWhiteSpace(_configuration.DirectoryURL))
                {
                    using (ITEDirectoryClient client = TEClientFactory.DirectoryClient(_configuration.ServiceProviderDID, _configuration.DirectoryURL))
                    {
                        ITEDirectoryNewAccount newAccount = account.ToDirectoryAccount(_configuration.ServiceProviderDID, _configuration.ServiceProviderPGLN);
                        await client.RegisterAccountAsync(newAccount);
                    }
                }

                return new OkObjectResult(account);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
