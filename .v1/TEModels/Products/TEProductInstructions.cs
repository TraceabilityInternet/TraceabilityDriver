using TraceabilityEngine.Interfaces.Models.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Models.Products
{
    public class TEProductInstructions : ITEProductInstructions
    {
        public string ConsumerSafetyInformation { get; set; }
        public string ConsumerStorageInformation { get; set; }
        public int SupplierSpecifiedMinimumConsumerStorageDays { get; set; }
        public string ConsumerUsageInstructions { get; set; }
        public string ConsumerPackageDisclaimer { get; set; }
        public string WarningInformation { get; set; }
        public string UserManualURL { get; set; }
        public string ConsumerHandlingInfoURL { get; set; }
        public string RecipeWebsiteURL { get; set; }
        public string AudioFileURL { get; set; }
        public string CustomerSupportCenter { get; set; }
    }
}
