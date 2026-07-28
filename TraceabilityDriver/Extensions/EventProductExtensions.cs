using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    public static class EventProductExtensions
    {
        public static void Merge(this EventProduct targetProduct, EventProduct sourceProduct)
        {
            if (targetProduct == null && sourceProduct != null)
            {
                targetProduct = new EventProduct(sourceProduct.EPC)
                {
                    Quantity = sourceProduct.Quantity,
                    Type = sourceProduct.Type,
                };
            }
            else if (targetProduct != null && sourceProduct != null)
            {
                targetProduct.Quantity.Merge(sourceProduct.Quantity);
            }
        }
    }
}
