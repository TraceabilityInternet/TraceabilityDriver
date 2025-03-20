using DSUtil;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.StaticData
{
    public class ProductForm : IFDStaticData
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public static ProductForm GetFromKey(string key)
        {
            return GetProductForms().Find(c => c.Key == key);
        }

        public static List<ProductForm> GetProductForms()
        {
            EmbeddedResourceLoader loader = new EmbeddedResourceLoader();
            DSXML xml = loader.ReadXML("FDModels.StaticData.Data.ProductForms.xml");
            List<ProductForm> productForms = new List<ProductForm>();
            foreach  (DSXML xProductForm in xml.ChildElements)
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
