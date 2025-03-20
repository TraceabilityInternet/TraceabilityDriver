using DirectoryService.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Services.DirectoryService;

namespace DirectoryService
{
    public static class DirectoryServiceUtil
    {
        public static ITEDirectoryDB GetDB(string connectionString)
        {
            return new TEDirectoryMongoDB(connectionString);
        }
    }
}
