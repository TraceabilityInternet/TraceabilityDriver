using OpenTraceability.GDST.MasterData;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Extensions
{
    public static class IVocabularyElementExtensions
    {
        /// <summary>
        /// Merges the source vocabulary element into the target vocabulary element, replacing null or missing properties/items in the target element with those from the source element. The source element is not modified. The target element is modified and returned.
        /// </summary>
        /// <param name="targetElement">The target vocabulary element to merge into.</param>
        /// <param name="sourceElement">The source vocabulary element to merge from.</param>
        /// <returns>The merged target vocabulary element.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static void Merge(this IVocabularyElement? targetElement, IVocabularyElement sourceElement)
        {
            if(targetElement is GDSTLocation targetGDSTLocation && sourceElement is GDSTLocation sourceGDSTLocation)
            {
                // merge logic for GDSTLocation
            }
            else if (targetElement is GDSTTradeItem targetGDSTTradeItem && sourceElement is GDSTTradeItem sourceGDSTTradeItem)
            {
                // merge logic for GDSTTradeItem
            }
            else if (targetElement is TradingParty targetTradingParty && sourceElement is TradingParty sourceTradingParty)
            {
                // merge logic for TradingParty
            }
        }
    }
}
