using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class CertificateType : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string FactType { get; set; }

        public static CertificateType GetFromKey(string key)
        {
            return GetCertificateTypes().Find(c => c.Key == key);
        }

        public static List<CertificateType> GetCertificateTypes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.CertificateTypes.xml");
            List<CertificateType> certTypes = new List<CertificateType>();
            foreach  (DSXML xCert in xml.ChildElements)
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
