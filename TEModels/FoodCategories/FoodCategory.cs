using System;
using System.Collections.Generic;
using System.Text;

namespace FDModels.FoodCategories
{
    public class FoodCategory
    {
        public string URI { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TradeItemClaim> Claims { get; set; }
        public List<TradeItem> TradeItems { get; set; }
    }
}
