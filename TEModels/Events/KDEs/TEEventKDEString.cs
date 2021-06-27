using TraceabilityEngine.Interfaces.Models.Events;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TraceabilityEngine.Models.Events.KDEs
{
    public class TEEventKDEString : TEEventKDEBase, ITEEventKDE
    {
        public XElement XmlValue
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public JToken JsonValue
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Type ValueType => typeof(string);
    }
}
