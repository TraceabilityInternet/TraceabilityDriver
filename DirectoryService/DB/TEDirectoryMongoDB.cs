using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Databases.Mongo.Serializers;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace DirectoryService.DB
{
    public class TEDirectoryMongoDB : ITEDirectoryDB
    {
        string _mongoConnectionString;
        string _mongoDBname = "TEDirectory";
        string _serviceProviderTbl = "ServiceProvider";
        string _accountTbl = "Account";
        string _adminTbl = "Admin";

        public TEDirectoryMongoDB(string mongoConnectionString)
        {
            _mongoConnectionString = mongoConnectionString;
            if (!BsonClassMap.IsClassMapRegistered(typeof(PGLN)))
            {
                BsonClassMap.RegisterClassMap<PGLN>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(DID)))
            {
                BsonClassMap.RegisterClassMap<DID>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDirectoryServiceProvider)))
            {
                BsonClassMap.RegisterClassMap<TEDirectoryServiceProvider>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(SimpleSignature)))
            {
                BsonClassMap.RegisterClassMap<SimpleSignature>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDirectoryAccount)))
            {
                BsonClassMap.RegisterClassMap<TEDirectoryAccount>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDirectoryNewAccount)))
            {
                BsonClassMap.RegisterClassMap<TEDirectoryNewAccount>();
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(TEDirectoryAdmin)))
            {
                BsonClassMap.RegisterClassMap<TEDirectoryAdmin>();
            }
        }

        public async Task<bool> IsValidServiceProviderAsync(IPGLN pgln)
        {
            try
            {
                if (IPGLN.IsNullOrEmpty(pgln)) throw new ArgumentNullException(nameof(pgln));

                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    ITEDirectoryServiceProvider serviceProvider = await docDB.LoadAsync<ITEDirectoryServiceProvider>("PGLN", pgln?.ToString(), _serviceProviderTbl);
                    if (serviceProvider != null)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<List<ITEDirectoryServiceProvider>> LoadAllServiceProviders()
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    List<ITEDirectoryServiceProvider> serviceProviders = await docDB.LoadAll<ITEDirectoryServiceProvider>(_serviceProviderTbl);
                    foreach (var sp in serviceProviders)
                    {
                        sp.DID.PrivateKey = null;
                    }
                    return serviceProviders;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDirectoryServiceProvider> LoadServiceProvider(IPGLN pgln)
        {
            try
            {
                if (pgln == null) throw new ArgumentNullException(nameof(pgln));

                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    ITEDirectoryServiceProvider sp = await docDB.LoadAsync<ITEDirectoryServiceProvider>("PGLN", pgln?.ToString(), _serviceProviderTbl);
                    return sp;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDirectoryAccount> LoadAccountAsync(IPGLN pgln)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    ITEDirectoryAccount account = await docDB.LoadAsync<ITEDirectoryAccount>("PGLN", pgln?.ToString(), _accountTbl);
                    return account;
                }
            }
            catch (Exception Ex)
            {
                var maps = BsonClassMap.GetRegisteredClassMaps();
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public async Task<List<ITEDirectoryAccount>> LoadAllAccounts()
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    List<ITEDirectoryAccount> accounts = await docDB.LoadAll<ITEDirectoryAccount>(_accountTbl);
                    foreach (var acc in accounts)
                    {
                        acc.DID.PrivateKey = null;
                    }
                    return accounts;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task RemoveAccountAsync(IPGLN pgln)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    await docDB.DeleteOneAsync<ITEDirectoryAccount>("PGLN", pgln, _accountTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task RemoveServiceProviderAsync(IDID did)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    await docDB.DeleteOneAsync<ITEDirectoryServiceProvider>("DID.ID", did.ID, _serviceProviderTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task SaveAccountAsync(ITEDirectoryNewAccount account)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    ITEDirectoryAccount existingAccount = await LoadAccountAsync(account.PGLN);
                    if (existingAccount != null)
                    {
                        account.ID = existingAccount.ID;
                        account.ObjectID = existingAccount.ObjectID;
                    }
                    else
                    {
                        account.ID = await docDB.GetNextSequenceAsync("account");
                    }

                    await docDB.SaveAsync<ITEDirectoryAccount>(account.ToAccount(), _accountTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        // Overloading SaveAccountAsync to work with ITEDirectoryAccounts as well
        public async Task SaveAccountAsync(ITEDirectoryAccount account)
        {
            try
            {
                // generate a new did if it doesn't have one set
                if (IDID.IsNullOrEmpty(account.DID))
                {
                    account.DID = DID.GenerateNew();
                }

                // generate a pgln is it doesn't have one set
                if (IPGLN.IsNullOrEmpty(account.PGLN))
                {
                    // Double check the string here...
                    account.PGLN = IdentifierFactory.ParsePGLN($"urn:traceabilityinternet:party:Account.{Guid.NewGuid().ToString()}");
                }

                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    await docDB.SaveAsync<ITEDirectoryAccount>(account, "PGLN", account.PGLN?.ToString(), _serviceProviderTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }



        public async Task SaveServiceProviderAsync(ITEDirectoryServiceProvider serviceProvider)
        {
            try
            {
                // generate a new did if it doesn't have one set
                if (IDID.IsNullOrEmpty(serviceProvider.DID))
                {
                    serviceProvider.DID = DID.GenerateNew();
                }

                // generate a pgln is it doesn't have one set
                if (IPGLN.IsNullOrEmpty(serviceProvider.PGLN))
                {
                    serviceProvider.PGLN = IdentifierFactory.ParsePGLN($"urn:traceabilityinternet:party:ServiceProvider.{Guid.NewGuid().ToString()}");
                }

                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    await docDB.SaveAsync<ITEDirectoryServiceProvider>(serviceProvider, "PGLN", serviceProvider.PGLN?.ToString(), _serviceProviderTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task SaveAdmin(ITEDirectoryAdmin admin)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    await docDB.SaveAsync<ITEDirectoryAdmin>(admin, "Email", admin.Email, _adminTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDirectoryAdmin> LoadAdmin(string email)
        {
            try
            {
                using (ITEDocumentDB docDB = new TEMongoDatabase(_mongoConnectionString, _mongoDBname))
                {
                    return await docDB.LoadAsync<ITEDirectoryAdmin>("Email", email, _adminTbl);
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        #region IDisposable
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
        // ~TEDirectoryDB()
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
