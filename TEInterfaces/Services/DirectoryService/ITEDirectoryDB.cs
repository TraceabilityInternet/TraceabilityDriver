using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util.Interfaces;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectoryDB : IDisposable
    {
        Task SaveServiceProviderAsync(ITEDirectoryServiceProvider serviceProvider);
        Task RemoveServiceProviderAsync(IDID did);
        Task<bool> IsValidServiceProviderAsync(IPGLN pgln);
        Task<List<ITEDirectoryServiceProvider>> LoadAllServiceProviders();
        Task<ITEDirectoryServiceProvider> LoadServiceProvider(IPGLN pgln);

        Task SaveAccountAsync(ITEDirectoryNewAccount account);
        // Overloading SaveAccountAsync to work with ITEDirectoryAccount as well
        Task SaveAccountAsync(ITEDirectoryAccount account);
        Task<ITEDirectoryAccount> LoadAccountAsync(IPGLN pgln);
        Task<List<ITEDirectoryAccount>> LoadAllAccounts();
        Task RemoveAccountAsync(IPGLN pgln);

        Task SaveAdmin(ITEDirectoryAdmin user);
        Task<ITEDirectoryAdmin> LoadAdmin(string username);
    }
}
