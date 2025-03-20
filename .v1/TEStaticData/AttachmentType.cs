using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class AttachmentType : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string FactType { get; set; }

        public static AttachmentType GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<AttachmentType> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.AttachmentTypes.xml");
            List<AttachmentType> types = new List<AttachmentType>();
            foreach  (TEXML xAttachmentType in xml.ChildElements)
            {
                AttachmentType type = new AttachmentType();
                type.Key = xAttachmentType.Attribute("Key");
                type.Name = xAttachmentType.Attribute("Name");
                type.FactType = xAttachmentType.Attribute("FactType");
                types.Add(type);
            }
            return types;
        }
    }
}
