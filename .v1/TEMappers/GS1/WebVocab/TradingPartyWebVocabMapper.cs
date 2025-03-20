using TraceabilityEngine.Util;
using TraceabilityEngine.Mappers;
using TraceabilityEngine.Models.TradingParty;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TraceabilityEngine.Interfaces.Mappers;
using TraceabilityEngine.Interfaces.Models.TradingParty;
using TraceabilityEngine.StaticData;
using TraceabilityEngine.Models.Identifiers;

namespace TraceabilityEngine.Mappers
{
    public class TradingPartyWebVocabMapper : GS1WebVocabMapper, ITETradingPartyMapper
    {
        public string ConvertFromTradingParty(ITETradingParty tp)
        {
            try
            {
                if (tp == null) throw new ArgumentNullException(nameof(tp));
                JObject json = new JObject();

                // handle the serialization here...
                SetContext(json);
                SetString(json, "@type", "gs1:Organization");
                SetString(json, "@id", tp.OrganizationWebURI);
                SetString(json, "globalLocationNumber", tp.PGLN?.ToString());
                SetLanguageString(json, "organizationName", tp.Name);
                SetLanguageString(json, "department", tp.Department);
                SetString(json, "additionalOrganizationIdentificationTypeValue", tp.AdditionalOrganizationType);
                SetString(json, "organizationRole", tp.OrganizationRole);
                SetAddress(json, "address", tp.Address);
                SetPhotos(json, "image", tp.Logo);
                SetCertificates(json, tp.Certificates, "certification");
                SetContacts(json, tp.Contacts);

                return json.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public ITETradingParty ConvertToTradingParty(string jsonStr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonStr)) throw new ArgumentNullException(nameof(jsonStr));
                JObject json = JObject.Parse(jsonStr);

                // handle the serialization here...
                SetContext(json);
                SetString(json, "@type", "gs1:Organization");

                ITETradingParty tp = new TETradingParty();
                tp.OrganizationWebURI = GetUri(json, "@id");
                tp.Name = GetLanguageString(json, "organizationName");
                tp.PGLN = IdentifierFactory.ParsePGLN(GetStringValue(json, "globalLocationNumber"), out string error);
                tp.Department = GetLanguageString(json, "department");
                tp.AdditionalOrganizationType = GetStringValue(json, "additionalOrganizationIdentificationTypeValue");
                tp.OrganizationRole = OrganizationRole.GetFromKey(GetStringValue(json, "organizationRole"));
                tp.Address = GetAddress(json, "address");
                tp.Logo = GetPhotos(json, "image");
                tp.Certificates = GetCertificates(json, "certification");
                tp.Contacts = GetContacts(json, "contactPoint");
                return tp;
            }
            catch (Exception Ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                TELogger.Log(0, Ex);
                throw;
            }
        }
    }
}
