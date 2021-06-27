using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.DigitalLink;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Service.Util.DB
{
    public class TEDriverDB : ITEDriverDB
    {
        private string _mongoConnectionString;
        private string _mongoDBName = "TraceabilityDriver";
        private string _tpTblName = "TradingPartner";
        private string _tpIDSequence = "TradingPartnerID";
        private string _accountTblName = "Account";
        private string _accountIDSequence = "AccountID";
        private string _digitalLinkTblName = "DigitalLink";

        public TEDriverDB(string mongoConnectionString)
        {
            _mongoConnectionString = mongoConnectionString;
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDriverTradingPartner)))
            {
                BsonClassMap.RegisterClassMap<TEDriverTradingPartner>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(SimpleSignature)))
            {
                BsonClassMap.RegisterClassMap<SimpleSignature>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDriverAccount)))
            {
                BsonClassMap.RegisterClassMap<TEDriverAccount>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(DID)))
            {
                BsonClassMap.RegisterClassMap<DID>();
            }
        }

        public async Task DeleteTradingPartnerAsync(long accountID, long tradingPartnerID)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    List<KeyValuePair<string, object>> filters = new List<KeyValuePair<string, object>>();
                    filters.Add(new KeyValuePair<string, object>("ID", tradingPartnerID));
                    filters.Add(new KeyValuePair<string, object>("AccountID", accountID));

                    await docDB.DeleteOneAsync<ITEDriverTradingPartner>(filters, _tpTblName);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverAccount> LoadAccountAsync(long accountID)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    ITEDriverAccount account = await docDB.LoadAsync<ITEDriverAccount>("ID", accountID, _accountTblName);

                    return account;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverAccount> LoadAccountAsync(IPGLN pgln)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    ITEDriverAccount account = await docDB.LoadAsync<ITEDriverAccount>("PGLN", pgln?.ToString(), _accountTblName);

                    return account;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverTradingPartner> LoadTradingPartnerAsync(long accountID, long tradingPartnerID)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    List<KeyValuePair<string, object>> filters = new List<KeyValuePair<string, object>>();
                    filters.Add(new KeyValuePair<string, object>("ID", tradingPartnerID));
                    filters.Add(new KeyValuePair<string, object>("AccountID", accountID));

                    ITEDriverTradingPartner tradingPartner = await docDB.LoadAsync<ITEDriverTradingPartner>(filters, _tpTblName);

                    return tradingPartner;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverTradingPartner> LoadTradingPartnerAsync(long accountID, IPGLN tpPGLN)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    List<KeyValuePair<string, object>> filters = new List<KeyValuePair<string, object>>();
                    filters.Add(new KeyValuePair<string, object>("PGLN", tpPGLN?.ToString()));
                    filters.Add(new KeyValuePair<string, object>("AccountID", accountID));

                    ITEDriverTradingPartner tradingPartner = await docDB.LoadAsync<ITEDriverTradingPartner>(filters, _tpTblName);

                    return tradingPartner;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task SaveAccountAsync(ITEDriverAccount account, string configURL)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    // set the ID if it has not been set
                    if (account.ID < 1)
                    {
                        account.ID = await docDB.GetNextSequenceAsync(_accountIDSequence);
                    }

                    // set the pgln if it has not been set
                    if (IPGLN.IsNullOrEmpty(account.PGLN))
                    {
                        account.PGLN = IdentifierFactory.ParsePGLN($"urn:gdst:foodontology.com:party:{account.ID}.0", out string error);
                    }

                    // set the DID if it is not set
                    if (IDID.IsNullOrEmpty(account.DID))
                    {
                        account.DID = DID.GenerateNew();
                    }

                    // if the EPCIS URL and Digital Link URL are not set, then set that for the account
                    // trying to move back into AccountController
                    // The problem is that the account ID is set based on a series from MongoDB, which is not exposed to the account controller.
                    // If we set the digital link before the ID is set, then the digital link isn't saved correctly.
                    if (string.IsNullOrWhiteSpace(account.DigitalLinkURL))
                    {
                        account.DigitalLinkURL = $"{configURL}/{account.ID}/digital_link"; // John edit
                    }

                    // try to load the existing account...
                    ITEDriverAccount existingAccount = await LoadAccountAsync(account.PGLN);
                    if (existingAccount != null)
                    {
                        account.ID = existingAccount.ID;
                        account.DID = existingAccount.DID; // Second edit of account.DID
                    }

                    // save the account
                    await docDB.SaveAsync<ITEDriverAccount>(account, "PGLN", account.PGLN?.ToString(), _accountTblName);
                }
            } 
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task SaveTradingPartnerAsync(ITEDriverTradingPartner tradingPartner)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    // if the account ID is not yet, then throw an exception
                    if (tradingPartner.AccountID < 1)
                    {
                        throw new ArgumentException("The AccountID on the trading partner must be set.");
                    }

                    // throw an exception if the PGLN is null or empty
                    if (IPGLN.IsNullOrEmpty(tradingPartner.PGLN))
                    {
                        throw new ArgumentException("The Trading Partner PGLN is NULL or Empty.");
                    }

                    // set the ID if it has not been set
                    if (tradingPartner.ID < 1)
                    {
                        tradingPartner.ID = await docDB.GetNextSequenceAsync(_tpIDSequence);
                    }

                    // try to load the existing account...
                    ITEDriverTradingPartner existingTP = await LoadTradingPartnerAsync(tradingPartner.AccountID, tradingPartner.ID);
                    if (existingTP != null)
                    {
                        tradingPartner.ID = existingTP.ID;
                        tradingPartner.DID = existingTP.DID;
                    }

                    // save the account
                    await docDB.SaveAsync<ITEDriverTradingPartner>(tradingPartner, "PGLN", tradingPartner.PGLN?.ToString(), _tpTblName);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<List<ITEDigitalLink>> LoadDigitalLinks()
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    List<ITEDigitalLink> links = await docDB.LoadAll<ITEDigitalLink>(_digitalLinkTblName);
                    return links;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task SaveDigitalLink(ITEDigitalLink link)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBName))
                {
                    await docDB.SaveAsync<ITEDigitalLink>(link, _digitalLinkTblName);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        #region
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TEDriverDB()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
