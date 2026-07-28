using OpenTraceability.GDST.MasterData;
using OpenTraceability.Interfaces;
using OpenTraceability.Models.MasterData;

namespace TraceabilityDriver.Extensions
{
    /// <summary>
    /// Merge support for master data vocabulary elements.
    /// </summary>
    public static class IVocabularyElementExtensions
    {
        /// <summary>
        /// Merges the source vocabulary element into the target vocabulary element, replacing null or missing properties/items in the target element with those from the source element. The source element is not modified. The target element is modified and returned.
        /// </summary>
        /// <remarks>
        /// This solves the same problem as <see cref="IEventExtensions.Merge"/>: the rows describing one
        /// location, trade item, or trading party can straddle sync runs, so a later partial copy must enrich
        /// the stored element instead of replacing it. The target's values always win, matching the first-wins
        /// convention used throughout the mapping chain.
        ///
        /// Identity properties are never merged - the elements are matched by identity in the first place, so
        /// <c>GLN</c>, <c>GTIN</c>, <c>PGLN</c>, <c>EPCISType</c>, <c>JsonLDType</c> and <c>Context</c> are
        /// left alone. Extra <c>KDEs</c> are not merged either, because the driver does not map them.
        ///
        /// Elements of different vocabulary types are a no-op: there is nothing meaningful to copy between,
        /// say, a location and a trade item.
        /// </remarks>
        /// <param name="targetElement">The target vocabulary element to merge into. Modified in place.</param>
        /// <param name="sourceElement">The source vocabulary element to merge from. Not modified.</param>
        public static void Merge(this IVocabularyElement targetElement, IVocabularyElement sourceElement)
        {
            if (targetElement is GDSTLocation targetGDSTLocation && sourceElement is GDSTLocation sourceGDSTLocation)
            {
                MergeLocation(targetGDSTLocation, sourceGDSTLocation);
            }
            else if (targetElement is GDSTTradeItem targetGDSTTradeItem && sourceElement is GDSTTradeItem sourceGDSTTradeItem)
            {
                MergeTradeItem(targetGDSTTradeItem, sourceGDSTTradeItem);
            }
            else if (targetElement is TradingParty targetTradingParty && sourceElement is TradingParty sourceTradingParty)
            {
                MergeTradingParty(targetTradingParty, sourceTradingParty);
            }
        }

        /// <summary>
        /// Fills the target location's unset properties from the source location.
        /// </summary>
        private static void MergeLocation(GDSTLocation target, GDSTLocation source)
        {
            if (target.OwningParty == null && source.OwningParty != null)
            {
                target.OwningParty = source.OwningParty;
            }

            if (target.InformationProvider == null && source.InformationProvider != null)
            {
                target.InformationProvider = source.InformationProvider;
            }

            if (string.IsNullOrEmpty(target.Contact) && !string.IsNullOrEmpty(source.Contact))
            {
                target.Contact = source.Contact;
            }

            if (string.IsNullOrEmpty(target.Email) && !string.IsNullOrEmpty(source.Email))
            {
                target.Email = source.Email;
            }

            if (string.IsNullOrEmpty(target.Phone) && !string.IsNullOrEmpty(source.Phone))
            {
                target.Phone = source.Phone;
            }

            if (string.IsNullOrEmpty(target.UnloadingPort) && !string.IsNullOrEmpty(source.UnloadingPort))
            {
                target.UnloadingPort = source.UnloadingPort;
            }

            // The geo properties back onto each other on the model (a geo fence derives the coordinates and
            // vice versa), so each one is only taken when the target resolves neither.
            if (string.IsNullOrEmpty(target.GeoLocation) && !string.IsNullOrEmpty(source.GeoLocation))
            {
                target.GeoLocation = source.GeoLocation;
            }

            if (string.IsNullOrEmpty(target.GeoFence) && !string.IsNullOrEmpty(source.GeoFence))
            {
                target.GeoFence = source.GeoFence;
            }

            // Reference-typed members that the helper extensions cannot create for us are assigned when the
            // target has none and merged into otherwise.
            if (target.Name == null)
            {
                target.Name = source.Name;
            }
            else
            {
                target.Name.Merge(source.Name);
            }

            if (target.Address == null)
            {
                target.Address = source.Address;
            }
            else
            {
                target.Address.Merge(source.Address);
            }

            if (target.CertificationList == null)
            {
                target.CertificationList = source.CertificationList;
            }
            else
            {
                target.CertificationList.Merge(source.CertificationList);
            }

            target.LocationClassification.Merge(source.LocationClassification);

            if (target.VesselFlagState == null && source.VesselFlagState != null)
            {
                target.VesselFlagState = source.VesselFlagState;
            }

            if (string.IsNullOrEmpty(target.VesselID) && !string.IsNullOrEmpty(source.VesselID))
            {
                target.VesselID = source.VesselID;
            }

            if (string.IsNullOrEmpty(target.VesselName) && !string.IsNullOrEmpty(source.VesselName))
            {
                target.VesselName = source.VesselName;
            }

            if (string.IsNullOrEmpty(target.IMONumber) && !string.IsNullOrEmpty(source.IMONumber))
            {
                target.IMONumber = source.IMONumber;
            }

            if (string.IsNullOrEmpty(target.VesselPublicRegistry) && !string.IsNullOrEmpty(source.VesselPublicRegistry))
            {
                target.VesselPublicRegistry = source.VesselPublicRegistry;
            }

            if (string.IsNullOrEmpty(target.SatelliteTrackingAuthority) && !string.IsNullOrEmpty(source.SatelliteTrackingAuthority))
            {
                target.SatelliteTrackingAuthority = source.SatelliteTrackingAuthority;
            }
        }

        /// <summary>
        /// Fills the target trade item's unset properties from the source trade item.
        /// </summary>
        private static void MergeTradeItem(GDSTTradeItem target, GDSTTradeItem source)
        {
            if (string.IsNullOrEmpty(target.TradeItemConditionCode) && !string.IsNullOrEmpty(source.TradeItemConditionCode))
            {
                target.TradeItemConditionCode = source.TradeItemConditionCode;
            }

            if (target.OwningParty == null && source.OwningParty != null)
            {
                target.OwningParty = source.OwningParty;
            }

            if (target.InformationProvider == null && source.InformationProvider != null)
            {
                target.InformationProvider = source.InformationProvider;
            }

            if (target.ShortDescription == null)
            {
                target.ShortDescription = source.ShortDescription;
            }
            else
            {
                target.ShortDescription.Merge(source.ShortDescription);
            }

            if (target.FisherySpeciesScientificName == null)
            {
                target.FisherySpeciesScientificName = source.FisherySpeciesScientificName;
            }
            else
            {
                target.FisherySpeciesScientificName.Merge(source.FisherySpeciesScientificName);
            }

            if (target.FisherySpeciesCode == null)
            {
                target.FisherySpeciesCode = source.FisherySpeciesCode;
            }
            else
            {
                target.FisherySpeciesCode.Merge(source.FisherySpeciesCode);
            }

            target.ProductClassification.Merge(source.ProductClassification);
        }

        /// <summary>
        /// Fills the target trading party's unset properties from the source trading party.
        /// </summary>
        private static void MergeTradingParty(TradingParty target, TradingParty source)
        {
            if (target.OwningParty == null && source.OwningParty != null)
            {
                target.OwningParty = source.OwningParty;
            }

            if (target.InformationProvider == null && source.InformationProvider != null)
            {
                target.InformationProvider = source.InformationProvider;
            }

            if (string.IsNullOrEmpty(target.IFTP) && !string.IsNullOrEmpty(source.IFTP))
            {
                target.IFTP = source.IFTP;
            }

            if (target.Name == null)
            {
                target.Name = source.Name;
            }
            else
            {
                target.Name.Merge(source.Name);
            }

            if (target.Address == null)
            {
                target.Address = source.Address;
            }
            else
            {
                target.Address.Merge(source.Address);
            }
        }
    }
}
