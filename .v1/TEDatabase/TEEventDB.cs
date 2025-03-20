using DSUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Models.Account;
using TraceabilityEngine.Interfaces.Models.Events;

namespace TEDatabase
{
    public class TEEventDB : ITEEventDB
    {
        private ITEAccessToken _token;
        private ITEDocumentDB _docDB;

        public TEEventDB(ITEAccessToken token)
        {
            //_docDB = token.GetDocumentDB();
        }

        public async Task<ITESaveResult> SaveAsync(List<ITEEvent> events)
        {
            try
            {
                throw new NotImplementedException();
                foreach (ITEEvent cte in events)
                {
                   await _docDB.SaveAsync<ITEEvent>(cte, "events");
                }
            }
            catch (Exception Ex)
            {
                DSLogger.Log(0, Ex);
                throw;
            }
        }
    }
}
