using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace DirectoryService.Controllers
{
    [ApiController]
    [Route("directory")]
    public class DirectoryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public DirectoryController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration["ConnectionString"];
        }

        [HttpGet]
        public string Index()
        {
            return "Hello!";
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAccount([FromBody] ITEDirectoryNewAccount account)
        {
            try
            {
                if (ITEDirectoryAccount.IsNullOrEmpty(account)) throw new ArgumentNullException("The account is null or empty. Please make sure the PGLN and the DID is populated.");

                // verify the service provider is registered
                using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(_connectionString))
                {
                    if (!(await dirDB.IsValidServiceProviderAsync(account.ServiceProviderPGLN)))
                    {
                        return new UnauthorizedResult();
                    }

                    ITEDirectoryServiceProvider serviceProvider = await dirDB.LoadServiceProvider(account.ServiceProviderPGLN);
                    if (serviceProvider == null)
                    {
                        return new UnauthorizedResult();
                    }

                    // set the DID
                    account.ServiceProviderDID = serviceProvider.DID;

                    // verify that the account's public key can verify the account signature
                    if (!account.VerifySignature())
                    {
                        return new UnauthorizedResult();
                    }

                    // save the account
                    await dirDB.SaveAccountAsync(account);
                }

                return new AcceptedResult();
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        [HttpGet]
        [Route("account/{pglnStr}")]
        public async Task<ITEDirectoryAccount> GetAccount([FromRoute] string pglnStr)
        {
            IPGLN pgln = IdentifierFactory.ParsePGLN(pglnStr);
            if (IPGLN.IsNullOrEmpty(pgln)) throw new ArgumentNullException(nameof(pgln));

            using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(_connectionString))
            {
                ITEDirectoryAccount account = await dirDB.LoadAccountAsync(pgln);
                return account;
            }
        }

        [HttpGet]
        [Route("search/{searchTerm}")]
        public async Task<ActionResult<List<ITEDirectorySearchResult>>> SearchAccounts(string searchTerm)
        {
            try
            {
                string authHeader = this.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    return new BadRequestObjectResult("Authorization Header must be provided.");
                }

                string headerDecrypted = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader));
                ISimpleSignature signature = SimpleSignatureFactory.Parse(headerDecrypted);

                if (signature == null)
                {
                    return new BadRequestObjectResult("Invalid authorization header.");
                }

                string[] parts = signature.Value.Split('|');
                if (parts.Length != 2)
                {
                    return new BadRequestObjectResult("Invalid authorization header.");
                }

                IPGLN serviceProviderPGLN = IdentifierFactory.ParsePGLN(parts[0], out string error);
                if (serviceProviderPGLN == null)
                {
                    return new BadRequestObjectResult("Invalid authorization header.");
                }
                string searchTermFromHeader = parts[1];

                if (searchTermFromHeader != searchTerm)
                {
                    return new BadRequestObjectResult("Invalid authorization header.");
                }

                using (ITEDirectoryDB dirDB = DirectoryServiceUtil.GetDB(_connectionString))
                {
                    var sp = await dirDB.LoadServiceProvider(serviceProviderPGLN);

                    if (sp == null)
                    {
                        return new BadRequestObjectResult("Invalid authorization header.");
                    }

                    if (!sp.DID.Verify(signature))
                    {
                        return new BadRequestObjectResult("Invalid authorization header.");
                    }


                }

                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
