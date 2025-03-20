using System;
using TraceabilityEngine.Interfaces.DB;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Database
{
    public class TEDatabaseFactory : ITEDatabaseFactory
    { 

        public ITEAccountDB GetAccountDB()
        {
            throw new NotImplementedException();
        }

        public ITEAttachmentDB GetAttachmentDB()
        {
            throw new NotImplementedException();
        }

        public ITEDocumentDB GetDocumentDB()
        {
            throw new NotImplementedException();
        }

        public ITEEventDB GetEvent()
        {
            throw new NotImplementedException();
        }

        public ITELocationDB GetLocationDB()
        {
            throw new NotImplementedException();
        }

        public ITEProductDB GetProductDB()
        {
            throw new NotImplementedException();
        }
    }
}
