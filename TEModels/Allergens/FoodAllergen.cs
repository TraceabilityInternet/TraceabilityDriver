using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FDModels.Allergens
{
    [JsonObject]
    public class FoodAllergen
    {
        [Key]
        [JsonProperty("id")]
        public string URI { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string Description { get; set; }
    }
}
