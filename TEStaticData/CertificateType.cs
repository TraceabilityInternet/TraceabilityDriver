using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class CertificateType : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string FactType { get; set; }

        public static CertificateType GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<CertificateType> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.CertificateTypes.xml");
            List<CertificateType> certTypes = new List<CertificateType>();
            foreach  (TEXML xCert in xml.ChildElements)
            {
                CertificateType certType = new CertificateType();
                certType.Key = xCert.Attribute("Key");
                certType.Name = xCert.Attribute("Name");
                certType.FactType = xCert.Attribute("FactType");
                certTypes.Add(certType);
            }
            return certTypes;
        }
    }
}
