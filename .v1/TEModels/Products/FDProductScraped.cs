using DSUtil.StaticData;
using FDModels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace FDModels.Products
{
    /// <summary>
    /// This is a model used when scraping products off the internet.
    /// </summary>
    [DataContract]
    public class FDProductScraped : FDProduct
    {
        [DataMember]
        public List<string> Categories { get; set; } = new List<string>();

        [DataMember]
        public List<string> Claims { get; set; } = new List<string>();

        public void FixClaims()
        {
            // try to detect organic
            if (this.Name.ToLower().Contains("organic") && !this.Claims.Contains("organic"))
            {
                this.Claims.Add("organic");                
            }

            // if the categories contain "eggs"
            if (this.Categories.Contains("eggs"))
            {
                if (DetectsPhrase("pasture raised", this.Name, this.Description))
                {
                    this.Claims.Add("pasture raised");
                }
                if (DetectsPhrase("cage free", this.Name, this.Description))
                {
                    this.Claims.Add("cage free");
                }
                if (DetectsPhrase("free range", this.Name, this.Description))
                {
                    this.Claims.Add("free range");
                }
            }

            if (DetectsPhrase("extra virgin olive oil", this.Name))
            {
                this.Claims.Add("extra virgin olive oil");
            }
        }

        private bool DetectsPhrase(string phrase, params string[] values)
        {
            foreach (string val in values)
            {
                string valTransformed = val.ToLower().Replace("-", " ").Replace("_", " ");
                if (valTransformed.Contains(phrase.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
