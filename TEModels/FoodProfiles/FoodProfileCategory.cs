using FDModels.FoodCategories;
using FDModels.TradeItems;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.FoodProfiles
{
    public class FoodProfileCategory
    {
        public FoodCategory FoodCategory { get; set; }
        public List<TradeItemClaim> RequiredClaims { get; set; }
    }
}
