using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for a product on an event.
    /// </summary>
    public static class EventProductExtensions
    {
        /// <summary>
        /// Fills the target product's missing quantity from the source product.
        /// </summary>
        /// <remarks>
        /// Only the quantity can differ between two copies of the same product: the EPC is the identity the
        /// caller matched on, and the product type is fixed by which list the product sits in. A product
        /// missing from the target altogether is added by the caller through
        /// <see cref="OpenTraceability.Interfaces.IEvent.AddProduct"/>, not here.
        /// </remarks>
        /// <param name="targetProduct">The product to merge into. Modified in place.</param>
        /// <param name="sourceProduct">The product to merge from. Not modified.</param>
        public static void Merge(this EventProduct targetProduct, EventProduct sourceProduct)
        {
            if (sourceProduct.Quantity == null)
            {
                return;
            }

            // The quantity is a reference the caller cannot replace from inside MeasurementExtensions.Merge,
            // so a missing measurement is assigned here and only an existing one is merged into.
            if (targetProduct.Quantity == null)
            {
                targetProduct.Quantity = sourceProduct.Quantity;
            }
            else
            {
                targetProduct.Quantity.Merge(sourceProduct.Quantity);
            }
        }
    }
}
