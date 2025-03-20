using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductAllergensAndAdditives : ITEProductAllergensAndAdditives
    {
        public string AllergenSpecificationAgency { get; set; }
        public string AllergenSpecificationName { get; set; }
        public string AllergenSpecificationStatement { get; set; }
        public List<ITEProductAllergen> AllergensList { get; set; } = new List<ITEProductAllergen>();
        public List<ITEProductAdditive> Additives { get; set; } = new List<ITEProductAdditive>();
    }
}
