using System;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Service.Util
{
    public static class TradingPartnerRequestAuthorizer
    {
        public static async Task<bool> Authorize(string authHeader, string subject, ITEDriverDB driverDB, long account_id)
        {
            try
            {
                string decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader));
                ISimpleSignature signature = SimpleSignatureFactory.Parse(decrypted);

                // [ ] 1. determine the account the request is intended for
                IPGLN accountPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[0]);

                // [ ] 2. determine the trading partner that sent the request
                IPGLN tradingPartnerPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[1]);

                // [ ] 3. determine the signature can be verified using the public key of the trading partner
                string signatureSubject = signature.Value.Split("|")[2];

                // [ ] 4. load the trading partner and the account
                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountPGLN);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(account.ID, tradingPartnerPGLN);

                if (account == null)
                {
                    return false;
                }

                // [ ] 5. verify that the trading partner is added to the account
                if (tp == null)
                {
                    return false;
                }

                // [ ] 6. verify that the signature can be verified using the DID of the trading partner
                if (!tp.DID.Verify(signature))
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

                return true;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static async Task<long> GetTradingPartnerID(string authHeader, ITEDriverDB driverDB)
        {
            try
            {
                string decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader));
                ISimpleSignature signature = SimpleSignatureFactory.Parse(decrypted);

                //// I think these need to be switched
                //IPGLN accountPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[0]);
                //IPGLN tradingPartnerPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[1]);

                IPGLN accountPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[1]);
                IPGLN tradingPartnerPGLN = IdentifierFactory.ParsePGLN(signature.Value.Split("|")[0]);

                ITEDriverAccount account = await driverDB.LoadAccountAsync(accountPGLN);
                ITEDriverTradingPartner tp = await driverDB.LoadTradingPartnerAsync(account.ID, tradingPartnerPGLN);

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
