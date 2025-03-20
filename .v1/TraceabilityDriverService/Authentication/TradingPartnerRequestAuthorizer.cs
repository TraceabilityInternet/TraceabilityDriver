using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Authentication
{
    public static class TradingPartnerRequestAuthorizer
    {
        public static async Task<bool> Authorize(HttpRequest request, string subject, ITEDriverDB driverDB, long account_id)
        {
            try
            {
                // the requesting party should provide their PGLN in the request header 'x-pgln'
                string requester_pglnStr = request.Headers["x-pgln"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(requester_pglnStr))
                {
                    // fail authorization if the 'x-pgln' header is not provided.
                    return false;
                }
                IPGLN requester_pgln = IdentifierFactory.ParsePGLN(requester_pglnStr);

                // load the trading partner and the account
                ITEDriverAccount account = await driverDB.LoadAccountAsync(account_id);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(account.ID, requester_pgln);

                if (account == null)
                {
                    return false;
                }

                // verify that the trading partner is added to the account
                if (tp == null)
                {
                    return false;
                }

                // check if this trading partner has an API Access Key and if that matches the 
                if (!string.IsNullOrWhiteSpace(tp.APIAccessKey))
                {
                    string apiKey = request.Headers["x-api-key"].FirstOrDefault()?.Split(' ').LastOrDefault();
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        if (apiKey == tp.APIAccessKey)
                        {
                            // if the api key matches, then assume they have been verified successfully
                            return true;
                        }
                    }
                }

                // otherwise, we are going to use the Public / Private Key authentication using the DIDs

                // grab the auth header
                string authHeader = request.Headers["Authorization"].FirstOrDefault()?.Split(' ').LastOrDefault();
                if (string.IsNullOrWhiteSpace(requester_pglnStr))
                {
                    // fail authorization if the 'Authorization' header is not provided.
                    return false;
                }
                string decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader));
                ISimpleSignature signature = SimpleSignatureFactory.Parse(decrypted);

                // determine the account the request is intended for
                IPGLN accountPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[0]);

                // determine the trading partner that sent the request
                IPGLN tradingPartnerPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[1]);

                // determine the signature can be verified using the public key of the trading partner
                string signatureSubject = signature.Value.Split("|")[2];

                account = await driverDB.LoadAccountAsync(accountPGLN);
                tp = await driverDB.LoadTradingPartnerAsync(account.ID, tradingPartnerPGLN);

                // verify the account
                if (account == null)
                {
                    return false;
                }

                // verify that the trading partner is added to the account
                if (tp == null)
                {
                    return false;
                }

                // verify that the signature can be verified using the DID of the trading partner
                if (!tp.PublicDID.Verify(signature))
                {
                    return false;
                }

                if (signatureSubject != subject)
                {
                    return false;
                }

                if (account_id != account.ID)
                {
                    return false;
                }

                if (!tradingPartnerPGLN.Equals(requester_pgln))
                {
                    return false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static async Task<long> GetTradingPartnerID(HttpRequest request, ITEDriverDB driverDB, long account_id)
        {
            try
            {
                // the requesting party should provide their PGLN in the request header 'x-pgln'
                string requester_pglnStr = request.Headers["x-pgln"].FirstOrDefault();
                IPGLN requester_pgln = IdentifierFactory.ParsePGLN(requester_pglnStr);

                ITEDriverAccount account = await driverDB.LoadAccountAsync(account_id);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(account.ID, requester_pgln);

                return tp?.ID ?? 0;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static string GenerateAuthHeader(string subject, ITEDriverAccount account, ITEDriverTradingPartner tradingPartner)
        {
            try
            {
                string signatureValue = tradingPartner.PGLN + "|" + account.PGLN + "|" + subject;
                ISimpleSignature signature = account.DID.Sign(signatureValue, DateTime.UtcNow.ToString());
                string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(signature.ToString()));
                return authHeader;
            } 
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
