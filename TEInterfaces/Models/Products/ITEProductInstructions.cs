using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityEngine.Interfaces.Models.Products
{
    public interface ITEProductInstructions
    {
        string ConsumerSafetyInformation { get; set; }
        string ConsumerStorageInformation { get; set; }
        int SupplierSpecifiedMinimumConsumerStorageDays { get; set; }
        string ConsumerUsageInstructions { get; set; }
        string ConsumerPackageDisclaimer { get; set; }
        string WarningInformation { get; set; }
        string UserManualURL { get; set; }
        string ConsumerHandlingInfoURL { get; set; }
        string RecipeWebsiteURL { get; set; }
        string AudioFileURL { get; set; }
        string CustomerSupportCenter { get; set; }
    }
}
