using TraceabilityEngine.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.StaticData
{
    public class ProductForm : ITEStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductForm GetFromKey(string key)
        {
            return GetList().Find(c => c.Key == key);
        }

        public static List<ProductForm> GetList()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            TEXML xml = loader.ReadXML("TEStaticData.Data.ProductForms.xml");
            List<ProductForm> productForms = new List<ProductForm>();
            foreach  (TEXML xProductForm in xml.ChildElements)
            {
                ProductForm productForm = new ProductForm();
                productForm.Key = xProductForm.Attribute("Key");
                productForm.Name = xProductForm.Attribute("Name");
                productForms.Add(productForm);
            }
            return productForms;
        }
    }
}
