using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.DB.DocumentDB;

namespace TraceabilityEngine.Interfaces.Services.DirectoryService
{
    public interface ITEDirectoryAdmin : ITEDocumentObject
    {
        string Email { get; set; }
        string EncryptedPassword { get; set; }
    }
}
