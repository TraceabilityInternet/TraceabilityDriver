using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class AttachmentType : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string FactType { get; set; }

        public static AttachmentType GetFromKey(string key)
        {
            return GetAttachmentTypes().Find(c => c.Key == key);
        }

        public static List<AttachmentType> GetAttachmentTypes()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.AttachmentTypes.xml");
            List<AttachmentType> types = new List<AttachmentType>();
            foreach  (DSXML xAttachmentType in xml.ChildElements)
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
