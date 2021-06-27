using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.Events;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Mappers.EPCIS
{
    //public class EPCISXmlMapper_2_0 : ITEEventMapper
    //{
    //    public string ConvertFromEvents(List<ITEEvent> ctes, Dictionary<string, string> cbvMappings = null)
    //    {
    //        try
    //        {
    //            TEXML xml
    //        }
    //        catch (Exception Ex)
    //        {
    //            TELogger.Log(0, Ex);
    //            throw;
    //        }
    //    }

    //    private TEXML ConvertFromObject(ITEObjectEvent objEvent)
    //    {
    //        TEXML xObjectEvent = new TEXML("ObjectEvent");
    //        if (!string.IsNullOrWhiteSpace(objEvent.EventID))
    //        {
    //            xObjectEvent.AddChild("eventID", objEvent.EventID);
    //        }
    //        xObjectEvent.AddChild("eventTime", objEvent.EventTime.);
    //        xObjectEvent.AddChild("eventTimeZoneOffset", ConvertToOffset(objEvent.EventTimeOffset));
    //        xObjectEvent.AddChild("action", objEvent.Action.ToString());

    //        if (!string.IsNullOrWhiteSpace(objEvent.BusinessStep))
    //        {
    //            xObjectEvent.AddChild("bizStep", objEvent.BusinessStep);
    //        }

    //        if (objEvent.Location?.GLN != null)
    //        {
    //            TEXML xLoc = xObjectEvent.AddChild("bizLocation");
    //            xLoc.AddChild("id", objEvent.Location.GLN.ToString());
    //        }

    //        if (objEvent.ReadPoint?.ID != null)
    //        {
    //            TEXML xReadPoint = xObjectEvent.AddChild("readPoint");
    //            xReadPoint.AddChild("id", objEvent.ReadPoint.ID);
    //        }

    //        if (objEvent.SensorElementList != null && objEvent.SensorElementList.Count > 0)
    //        {

    //        }

    //        foreach (ITEEventProduct product in objEvent.Products)
    //        {

    //        }
    //    }

    //    private TEXML ConvertFromTransformation(ITETransformationEvent transformEvent)
    //    {

    //    }

    //    public List<ITEEvent> ConvertToEvents(string value)
    //    {
    //        try
    //        {

    //        }
    //        catch (Exception Ex)
    //        {
    //            TELogger.Log(0, Ex);
    //            throw;
    //        }
    //    }
    //}
}
