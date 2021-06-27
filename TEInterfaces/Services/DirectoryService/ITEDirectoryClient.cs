using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectoryClient : IDisposable
    {
        Task RegisterAccountAsync(ITEDirectoryNewAccount account);
        Task<ITEDirectoryAccount> GetAccountAsync(IPGLN pgln);
    }
}
