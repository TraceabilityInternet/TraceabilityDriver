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
    /// <summary>
    /// This is used for managing accounts that are controlled by the owner of the traceability driver.
    /// </summary>
    [ApiController]
    [Route("api/account")]
    [Authorize(AuthenticationSchemes = "APIKeyAuthenticationHandler")]
    public class AccountController
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _directoryURL;
        private readonly IDID _serviceProvider;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration["ConnectionString"];
            _directoryURL = _configuration["DirectoryURL"];
            _serviceProvider = DIDFactory.Parse(_configuration["ServiceProvider"]);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ITEDriverAccount> Get(string id)
        {
            try
            {
                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    if (long.TryParse(id, out long longID))
                    {
                        // load by the id
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(longID);
                        return account;
                    }
                    else
                    {
                        // load by the PGLN
                        IPGLN pgln = IdentifierFactory.ParsePGLN(id);
                        ITEDriverAccount account = await driverDB.LoadAccountAsync(pgln);
                        return account;
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
                if (account == null) throw new ArgumentNullException(nameof(account));

                if (IDID.IsNullOrEmpty(account.DID))
                {
                    account.DID = DID.GenerateNew();
                }

                using (ITEDriverDB driverDB = InternalServiceUtil.GetDB(_connectionString))
                {
                    await driverDB.SaveAccountAsync(account);
                }

                // now we are going to register the account with the directory
                using (ITEDirectoryClient client = TEClientFactory.DirectoryClient(_serviceProvider, _directoryURL))
                {
                    ITEDirectoryNewAccount newAccount = account.ToDirectoryAccount(_serviceProvider);
                    await client.RegisterAccountAsync(newAccount);
                }

                return new AcceptedResult("", account);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
