using DirectoryService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;

namespace DirectoryService.Services
{
    public class DBService : IDBService
    {
        private AccountService _accountService;

        public DBService (AccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<List<ITEDirectoryServiceProvider>> GetAll()
        {
            using (ITEDirectoryDB db = DirectoryServiceUtil.GetDB(_accountService.ConnectionString))
            {
                return await db.LoadAllServiceProviders();
            }
        }
        public async Task<List<ITEDirectoryAccount>> LoadAccountList()
        {
            using (ITEDirectoryDB db = DirectoryServiceUtil.GetDB(_accountService.ConnectionString))
            {
                return await db.LoadAllAccounts();
            }
        }
    }
}
