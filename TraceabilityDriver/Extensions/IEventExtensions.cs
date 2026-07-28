using OpenTraceability.GDST.Events;
using OpenTraceability.Interfaces;

namespace TraceabilityDriver.Extensions
{
    public static class IEventExtensions
    {
        /// <summary>
        /// Merges the source event into the target event, replacing null or missing properties/items in the target event with those from the source event. The source event is not modified. The target event is modified and returned.
        /// </summary>
        /// <param name="sourceEvent">The source event to merge from.</param>
        /// <param name="targetEvent">The target event to merge into.</param>
        /// <returns>The merged target event.</returns>
        public static void Merge(this IEvent targetEvent, IEvent sourceEvent)
        {
            if (targetEvent.EventTime == null && sourceEvent.EventTime != null)
            {
                targetEvent.EventTime = sourceEvent.EventTime;
            }

            if (targetEvent.EventTimeZoneOffset == null && sourceEvent.EventTimeZoneOffset != null)
            {
                targetEvent.EventTimeZoneOffset = sourceEvent.EventTimeZoneOffset;
            }

            if (targetEvent.Action == null && sourceEvent.Action != null)
            {
                targetEvent.Action = sourceEvent.Action;
            }

            if (targetEvent.ReadPoint == null && sourceEvent.ReadPoint != null)
            {
                targetEvent.ReadPoint = sourceEvent.ReadPoint;
            }

            if (targetEvent.Disposition == null && sourceEvent.Disposition != null)
            {
                targetEvent.Disposition = sourceEvent.Disposition;
            }

            if (string.IsNullOrEmpty(targetEvent.CertificationInfo) && !string.IsNullOrEmpty(sourceEvent.CertificationInfo))
            {
                targetEvent.CertificationInfo = sourceEvent.CertificationInfo;
            }

            if (targetEvent.ErrorDeclaration == null && sourceEvent.ErrorDeclaration != null)
            {
                targetEvent.ErrorDeclaration = sourceEvent.ErrorDeclaration;
            }

            if (targetEvent.PersistentDisposition == null && sourceEvent.PersistentDisposition != null)
            {
                targetEvent.PersistentDisposition = sourceEvent.PersistentDisposition;
            }

            if (targetEvent.RecordTime == null && sourceEvent.RecordTime != null)
            {
                targetEvent.RecordTime = sourceEvent.RecordTime;
            }

            if (targetEvent.Location == null && sourceEvent.Location != null)
            {
                targetEvent.Location = sourceEvent.Location;
            }

            // sensor element lists excluded for now since we don't map them for the traceability driver

            targetEvent.BizTransactionList.Merge(sourceEvent.BizTransactionList);
            targetEvent.SourceList.Merge(sourceEvent.SourceList);
            targetEvent.DestinationList.Merge(sourceEvent.DestinationList);

            foreach (var sourceProduct in sourceEvent.Products)
            {
                var targetProduct = targetEvent.Products.FirstOrDefault(p => p.EPC.Equals(sourceProduct.EPC));
                if (targetProduct == null)
                {
                    targetEvent.AddProduct(sourceProduct);
                }
                else
                {
                    targetProduct.Merge(sourceProduct);
                }
            }

            if (targetEvent is IGDSTEvent targetGDSTEvent && sourceEvent is IGDSTEvent sourceGDSTEvent)
            {
                if (targetGDSTEvent.InformationProvider == null && sourceGDSTEvent.InformationProvider != null)
                {
                    targetGDSTEvent.InformationProvider = sourceGDSTEvent.InformationProvider;
                }
            }

            if (targetEvent is IGDSTProductOwnerEvent targetGDSTProductOwnerEvent && sourceEvent is IGDSTProductOwnerEvent sourceGDSTProductOwnerEvent)
            {
                if (targetGDSTProductOwnerEvent.ProductOwner == null && sourceGDSTProductOwnerEvent.ProductOwner != null)
                {
                    targetGDSTProductOwnerEvent.ProductOwner = sourceGDSTProductOwnerEvent.ProductOwner;
                }
            }

            if (targetEvent is IGDSTILMDEvent targetGDSTILMDEvent && sourceEvent is IGDSTILMDEvent sourceGDSTILMDEvent)
            {
                targetGDSTILMDEvent.ILMD.Merge(sourceGDSTILMDEvent.ILMD);
            }

            if (targetEvent is GDSTCommissionEvent targetCommissionEvent && sourceEvent is GDSTCommissionEvent sourceCommissionEvent)
            {
                if (string.IsNullOrEmpty(targetCommissionEvent.HumanWelfarePolicy) && !string.IsNullOrEmpty(sourceCommissionEvent.HumanWelfarePolicy))
                {
                    targetCommissionEvent.HumanWelfarePolicy = sourceCommissionEvent.HumanWelfarePolicy;
                }
            }

            // decommission events have no additional requirements.

            if (targetEvent is GDSTShippingEvent targetShippingEvent && sourceEvent is GDSTShippingEvent sourceShippingEvent)
            {
                if (string.IsNullOrEmpty(targetShippingEvent.UnloadingPort) && !string.IsNullOrEmpty(sourceShippingEvent.UnloadingPort))
                {
                    targetShippingEvent.UnloadingPort = sourceShippingEvent.UnloadingPort;
                }
            }

            if (targetEvent is GDSTReceivingEvent targetReceivingEvent && sourceEvent is GDSTReceivingEvent sourceReceivingEvent)
            {
                if (string.IsNullOrEmpty(targetReceivingEvent.UnloadingPort) && !string.IsNullOrEmpty(sourceReceivingEvent.UnloadingPort))
                {
                    targetReceivingEvent.UnloadingPort = sourceReceivingEvent.UnloadingPort;
                }
            }

            // aggregation events have no additional requirements.

            // disaggregation events have no additional requirements.

            if (targetEvent is GDSTTransformationEvent targetTransformationEvent && sourceEvent is GDSTTransformationEvent sourceTransformationEvent)
            {
                if (string.IsNullOrEmpty(targetTransformationEvent.HumanWelfarePolicy) && !string.IsNullOrEmpty(sourceTransformationEvent.HumanWelfarePolicy))
                {
                    targetTransformationEvent.HumanWelfarePolicy = sourceTransformationEvent.HumanWelfarePolicy;
                }
            }
        }
    }
}
