using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;

namespace DirectoryService.Services.Interfaces
{
    public interface IDBService
    {
        Task<List<ITEDirectoryServiceProvider>> GetAll();


        Task <List<ITEDirectoryAccount>> LoadAccountList();


    }
}
