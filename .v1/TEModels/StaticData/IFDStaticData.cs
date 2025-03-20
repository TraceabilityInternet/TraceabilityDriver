using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public interface IFDStaticData
    {
        string Key { get; set; }
        string Name { get; set; }
    }
}
