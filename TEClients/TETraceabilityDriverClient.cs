using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Mappers.EPCIS;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.ObjectPooling;

namespace TraceabilityEngine.Clients
{
    public class TETraceabilityDriverClient : ITEInternalClient
    {
        private HttpClient _client;
        string _url;
        string _apiKey;
        
        public TETraceabilityDriverClient(string url, string apiKey)
        {
            _apiKey = apiKey;
            _client = new HttpClient();
            _url = url.TrimEnd('/');
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        }

        #pragma warning disable 1998
        public async Task<ITEDriverAccount> GetAccountAsync(long id)
        {
            throw new NotImplementedException();
        }

        public async Task<ITEDriverAccount> SaveAccountAsync(ITEDriverAccount account)
        {
            try
            {
                if (account == null) throw new ArgumentNullException(nameof(account));

                string jsonStr = account.ToJson();
                StringContent content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_url}/api/account/", content);
                if (response.IsSuccessStatusCode == false)
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to save the account.");
                }
                else
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    ITEDriverAccount accountReturned = TEDriverFactory.CreateAccount(responseStr);
                    return accountReturned;
                }
            }
            catch(Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverTradingPartner> GetTradingPartnerAsync(long accountID, long tradingPartnerID)
        {
            try
            {
                var response = await _client.GetAsync($"{_url}/api/tradingpartner/{accountID}/{tradingPartnerID}");
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception("Failed to get the trading partner.");
                }
                else
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    ITEDriverTradingPartner tp = TEDriverFactory.CreateTradingPartner(responseStr);
                    return tp;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<ITEDriverTradingPartner> AddTradingPartnerAsync(long accountID, IPGLN pgln)
        {
            try
            {
                var response = await _client.PostAsync($"{_url}/api/tradingpartner/{accountID}/{pgln}", new StringContent(""));
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception("Failed to add the trading partner.");
                }
                else
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    ITEDriverTradingPartner tp = TEDriverFactory.CreateTradingPartner(responseStr);
                    return tp;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task DeleteTradingPartnerAsync(long accountID, long tradingPartnerID)
        {
            try
            {
                var response = await _client.DeleteAsync($"{_url}/api/tradingpartner/{accountID}/{tradingPartnerID}");
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception("Failed to save the account.");
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<string> GetTradeItemAsync(long accountID, long tradingPartnerID, string gtin)
        {
            try
            {
                string url = $"{_url}/api/tradeitems/{accountID}/{tradingPartnerID}/{gtin}";
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode == false)
                {
                    string errorStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to get the trade item.");
                }
                string responseStr = await response.Content.ReadAsStringAsync();
                return responseStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<string> GetLocationAsync(long accountID, long tradingPartnerID, string gln)
        {
            try
            {
                string url = $"{_url}/api/location/{accountID}/{tradingPartnerID}/{gln}";
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode == false)
                {
                    string errorStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to get the location.");
                }
                string responseStr = await response.Content.ReadAsStringAsync();
                return responseStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<string> GetTradingPartyAsync(long accountID, long tradingPartnerID, string pgln)
        {
            try
            {
                string url = $"{_url}/api/tradingparty/{accountID}/{tradingPartnerID}/{pgln}";
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode == false)
                {
                    string errorStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to get the trading party.");
                }
                string responseStr = await response.Content.ReadAsStringAsync();
                return responseStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<string> GetEventsAsync(long accountID, long tradingPartnerID, string epc)
        {
            try
            {
                string url = $"{_url.TrimEnd('/')}/api/events/{accountID}/{tradingPartnerID}/{epc}";
                HttpResponseMessage response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to register the account.");
                }
                else
                {
                    string localFormatEvents = await response.Content.ReadAsStringAsync();
                    return localFormatEvents;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public async Task<string> GetEventsAsync(long accountID, long tradingPartnerID, string epc, DateTime minEventTime, DateTime maxEventTime)
        {
            try
            {
                string url = $"{_url.TrimEnd('/')}/api/events/{accountID}/{tradingPartnerID}/{epc}?minEventTime={minEventTime.ToString()}&maxEventTime={maxEventTime.ToString()}";
                HttpResponseMessage response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string responseStr = await response.Content.ReadAsStringAsync();
                    throw new Exception("Failed to register the account.");
                }
                else
                {
                    string localFormatEvents = await response.Content.ReadAsStringAsync();
                    return localFormatEvents;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        //private string GetAuth(IDID accountDID, IPGLN accountPGLN, IPGLN tpPGLN, string subject)
        //{
        //    string authKey = _tpPGLN + "|" + _accountPGLN + "|" + subject;
        //    ISimpleSignature signature = _did.Sign(authKey, DateTime.UtcNow.ToString());
        //    return "Bearer " + Convert.ToBase64String(Encoding.UTF8.GetBytes(signature.ToString()));
        //}

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (_client != null)
                    {
                        _client.Dispose();
                        _client = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TEInternalClient()
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
