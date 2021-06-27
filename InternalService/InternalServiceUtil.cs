using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Service.Util.DB;
using TraceabilityEngine.Util.Interfaces;

namespace InternalService
{
    public static class InternalServiceUtil
    {
        public static ITEDriverDB GetDB(string connectionString)
        {
            return new TEDriverDB(connectionString);
        }
    }
}
