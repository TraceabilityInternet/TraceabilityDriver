using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace UnitTests.Models.Identifiers.GS1AICodes
{
    //[TestClass]
    //public class GS1AICodeTests
    //{
    //    public GS1AICodeTests()
    //    {
    //        TEXML xml = new TEXML();
            
    //    }

    //    [TestMethod]
    //    public void  Load()
    //    {
    //        GS1AIDefinitions GS1AIDefinitions = new GS1AIDefinitions();

    //        EmbeddedResourceLoader EmbeddedResourceLoader = new EmbeddedResourceLoader();
    //        TEXML xml = EmbeddedResourceLoader.ReadXML("UnitTests.Models.Identifiers.GS1AICodes.AICodeTest.xml");
    //        int ? AICodesCount = xml["AICodesCount"].GetValueInt32();
    //        Debug.Assert(GS1AIDefinitions.AICodes.Count == AICodesCount.Value);

    //        foreach(TEXML xmlCode in xml["Codes"])
    //        {
    //            GS1AICode code = GS1AIDefinitions.FromAI(xmlCode.Attribute("ai"));
    //            Debug.Assert(code.label == xmlCode.Attribute("label"));
    //        }
    //    }
    //}
}
