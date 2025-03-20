using FDModels.Locations;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.FoodProfiles
{
    public class FoodProfile
    {
        public string UUID { get; set; }
        public Address AnonymizedAddress { get; set; }
        public List<FoodProfileCategory> FoodCategories { get; set; }
    }
}
