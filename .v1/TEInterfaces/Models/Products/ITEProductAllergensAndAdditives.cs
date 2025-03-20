using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductAllergensAndAdditives
    {
        string AllergenSpecificationAgency { get; set; }
        string AllergenSpecificationName { get; set; }
        string AllergenSpecificationStatement { get; set; }
        List<ITEProductAllergen> AllergensList { get; set; }
        List<ITEProductAdditive> Additives { get; set; }
    }
}
