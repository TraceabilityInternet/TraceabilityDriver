using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Interfaces.Services.DirectoryService;
using TraceabilityEngine.Models.Directory;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.ObjectPooling;

namespace TraceabilityEngine.Clients
{
    /// <summary>
    /// This is a class used for communicating with a Directory Service.
    /// </summary>
    public class TEDirectoryClient : ITEDirectoryClient
    {
        LimitedPoolItem<HttpClient> _item;
        HttpClient _client;
        string _url;
        IDID _did;

        /// <summary>
        /// This constructs an instance of the TEDirectoryClient.
        /// </summary>
        /// <param name="did">The DID of the entity that wishes to communicate with the Directory Service. This DID must be registed on the Directory Service as an authorized user.</param>
        /// <param name="url">The URL of the Directory Service.</param>
        public TEDirectoryClient(IDID did, string url)
        {
            if (IDID.IsNullOrEmpty(did))
            {
                throw new ArgumentNullException(nameof(did));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            _item = TraceabilityEngine.Util.Net.HttpUtil.ClientPool.Get();
            _client = _item.Value;
            _url = url;
            _did = did;
        }

        public async Task<ITEDirectoryAccount> GetAccountAsync(IPGLN pgln)
        {
            try
            {
                if (IPGLN.IsNullOrEmpty(pgln))
                {
                    throw new ArgumentNullException(nameof(pgln));
                }

                string url = $"{_url.TrimEnd('/')}/directory/account/{pgln}";
                HttpResponseMessage response = await _client.GetAsync(url);
                string jsonStr = await response.Content.ReadAsStringAsync();
                ITEDirectoryAccount account = new TEDirectoryAccount();
                account.FromJson(jsonStr);

                return account;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task RegisterAccountAsync(ITEDirectoryNewAccount account)
        {
            try
            {
                string json = account.ToJson();
                string url = $"{_url.TrimEnd('/')}/directory/register";
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to register the account.");
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
                    if (_item != null)
                    {
                        _item.Dispose();
                        _item = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TEDirectoryClient()
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
