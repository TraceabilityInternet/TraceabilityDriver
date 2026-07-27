using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTraceability.Mappers;
using OpenTraceability.Models.Events;

namespace TraceabilityDriver.Tests
{
    /// <summary>
    /// Deep-compares two EPCIS documents and reports every difference as a human-readable message.
    /// </summary>
    /// <remarks>
    /// Both documents are serialized through the same OpenTraceability JSON mapper so representational
    /// noise cancels out, then compared as JSON. Events are matched by a semantic key (event type,
    /// business step, action, business location, and the full set of EPCs on the event) rather than by
    /// event id, because the driver generates deterministic hash-based event ids that never match the
    /// golden document's ids. The properties eventID, recordTime, and eventTime are ignored everywhere.
    /// Arrays are compared as multisets because element order (e.g. source lists, certificates) is not
    /// semantically meaningful.
    /// </remarks>
    public static class FullDataComparer
    {
        private static readonly HashSet<string> _ignoredProperties = new HashSet<string>() { "eventID", "recordTime", "eventTime" };

        // Note - Claude - 7/24/2026: The SDK's Address.Address2 property carries an
        // OpenTraceabilityMasterData attribute but no OpenTraceabilityJson attribute, so
        // streetAddressTwo is dropped when master data round-trips through the GS1 web vocab
        // masterdata endpoint. Excluded here until the SDK adds the JSON mapping.
        private static readonly HashSet<string> _ignoredMasterDataAttributes = new HashSet<string>() { "urn:epcglobal:cbv:mda#streetAddressTwo" };

        /// <summary>
        /// Compares the two documents and returns one message per difference, or an empty list when the
        /// documents are equivalent.
        /// </summary>
        /// <param name="expected">The golden document.</param>
        /// <param name="actual">The document produced by the traceback.</param>
        /// <returns>The differences found, empty when the documents match.</returns>
        public static List<string> CompareDocuments(EPCISDocument expected, EPCISDocument actual)
        {
            List<string> diffs = new List<string>();

            // Schema validation is skipped: the comparison is field-by-field, and the traceback result
            // is a merged document that legitimately lacks header fields such as creationDate.
            JObject expectedJson = JObject.Parse(OpenTraceabilityMappers.EPCISDocument.JSON.Map(expected, checkSchema: false));
            JObject actualJson = JObject.Parse(OpenTraceabilityMappers.EPCISDocument.JSON.Map(actual, checkSchema: false));

            CompareEvents(expectedJson, actualJson, diffs);
            CompareMasterData(expectedJson, actualJson, diffs);

            return diffs;
        }

        /// <summary>
        /// Matches the events of both documents by semantic key and deep-compares each matched pair.
        /// </summary>
        private static void CompareEvents(JObject expectedJson, JObject actualJson, List<string> diffs)
        {
            List<JObject> expectedEvents = GetEvents(expectedJson);
            List<JObject> actualEvents = GetEvents(actualJson);

            // Match greedily by key; keys are unique across the golden document (verified), so a
            // dictionary keyed lookup with removal gives a stable one-to-one matching.
            Dictionary<string, JObject> actualByKey = new Dictionary<string, JObject>();
            foreach (JObject actualEvent in actualEvents)
            {
                string key = BuildEventKey(actualEvent);
                if (!actualByKey.TryAdd(key, actualEvent))
                {
                    diffs.Add($"Duplicate actual event key: {key}");
                }
            }

            foreach (JObject expectedEvent in expectedEvents)
            {
                string key = BuildEventKey(expectedEvent);
                if (actualByKey.TryGetValue(key, out JObject? actualEvent))
                {
                    actualByKey.Remove(key);
                    CompareTokens($"event[{key}]", expectedEvent, actualEvent, diffs);
                }
                else
                {
                    diffs.Add($"Expected event not found in the traceback result: {key}");
                }
            }

            foreach (string key in actualByKey.Keys)
            {
                diffs.Add($"Unexpected extra event in the traceback result: {key}");
            }
        }

        /// <summary>
        /// Compares the master data vocabularies element-by-element and attribute-by-attribute.
        /// </summary>
        private static void CompareMasterData(JObject expectedJson, JObject actualJson, List<string> diffs)
        {
            Dictionary<string, JObject> expectedElements = GetMasterDataElements(expectedJson);
            Dictionary<string, JObject> actualElements = GetMasterDataElements(actualJson);

            foreach (KeyValuePair<string, JObject> expectedElement in expectedElements)
            {
                if (actualElements.TryGetValue(expectedElement.Key, out JObject? actualElement))
                {
                    CompareAttributeLists(expectedElement.Key, expectedElement.Value, actualElement, diffs);
                }
                else
                {
                    diffs.Add($"Expected master data element not found: {expectedElement.Key}");
                }
            }

            foreach (string key in actualElements.Keys.Where(k => !expectedElements.ContainsKey(k)))
            {
                diffs.Add($"Unexpected extra master data element: {key}");
            }
        }

        /// <summary>
        /// Compares the attributes array of two vocabulary elements as a multiset of (id, value) pairs,
        /// which tolerates attribute ordering and repeated attribute ids (e.g. productClassification).
        /// </summary>
        private static void CompareAttributeLists(string elementId, JObject expectedElement, JObject actualElement, List<string> diffs)
        {
            List<string> expectedAttributes = (expectedElement["attributes"] as JArray ?? new JArray()).Where(a => !IsIgnoredAttribute(a)).Select(a => Canonicalize(a)).ToList();
            List<string> actualAttributes = (actualElement["attributes"] as JArray ?? new JArray()).Where(a => !IsIgnoredAttribute(a)).Select(a => Canonicalize(a)).ToList();

            foreach (string attribute in expectedAttributes)
            {
                if (!actualAttributes.Remove(attribute))
                {
                    diffs.Add($"Master data element {elementId}: missing attribute {attribute}");
                }
            }

            foreach (string attribute in actualAttributes)
            {
                diffs.Add($"Master data element {elementId}: unexpected attribute {attribute}");
            }
        }

        /// <summary>
        /// Returns TRUE when the master data attribute is excluded from comparison.
        /// </summary>
        private static bool IsIgnoredAttribute(JToken attribute)
        {
            return _ignoredMasterDataAttributes.Contains(attribute["id"]?.Value<string>() ?? string.Empty);
        }

        /// <summary>
        /// Recursively compares two JSON tokens, reporting missing, unexpected, and mismatched values
        /// with their full JSON path. Ignored properties are skipped, and arrays compare as multisets.
        /// </summary>
        private static void CompareTokens(string path, JToken expected, JToken actual, List<string> diffs)
        {
            if (expected is JObject expectedObject && actual is JObject actualObject)
            {
                foreach (string name in expectedObject.Properties().Select(p => p.Name).Union(actualObject.Properties().Select(p => p.Name)))
                {
                    if (_ignoredProperties.Contains(name))
                    {
                        continue;
                    }

                    JToken? expectedValue = expectedObject[name];
                    JToken? actualValue = actualObject[name];

                    if (expectedValue == null)
                    {
                        diffs.Add($"UNEXPECTED {path}.{name} = {Canonicalize(actualValue!)}");
                    }
                    else if (actualValue == null)
                    {
                        diffs.Add($"MISSING {path}.{name} (expected: {Canonicalize(expectedValue)})");
                    }
                    else
                    {
                        CompareTokens($"{path}.{name}", expectedValue, actualValue, diffs);
                    }
                }
            }
            else if (expected is JArray expectedArray && actual is JArray actualArray)
            {
                // Arrays compare as multisets of canonicalized items so element ordering differences
                // (source lists, certificates, classifications) do not produce false diffs.
                List<string> remaining = actualArray.Select(a => Canonicalize(a)).ToList();

                foreach (JToken expectedItem in expectedArray)
                {
                    string canonical = Canonicalize(expectedItem);
                    if (!remaining.Remove(canonical))
                    {
                        diffs.Add($"MISSING {path}[] item (expected: {canonical})");
                    }
                }

                foreach (string canonical in remaining)
                {
                    diffs.Add($"UNEXPECTED {path}[] item: {canonical}");
                }
            }
            else if (!JToken.DeepEquals(expected, actual))
            {
                diffs.Add($"VALUE MISMATCH at {path}: expected '{expected.ToString(Formatting.None)}' actual '{actual.ToString(Formatting.None)}'");
            }
        }

        /// <summary>
        /// Builds a semantic matching key for an event from its type, business step, action, business
        /// location, and every EPC referenced by the event, ignoring event ids and times.
        /// </summary>
        private static string BuildEventKey(JObject epcisEvent)
        {
            List<string> epcs = new List<string>();

            epcs.AddRange(GetStringArray(epcisEvent, "epcList"));
            epcs.AddRange(GetStringArray(epcisEvent, "inputEPCList"));
            epcs.AddRange(GetStringArray(epcisEvent, "outputEPCList"));
            epcs.AddRange(GetStringArray(epcisEvent, "childEPCs"));
            epcs.AddRange(GetQuantityEpcs(epcisEvent, "quantityList"));
            epcs.AddRange(GetQuantityEpcs(epcisEvent, "inputQuantityList"));
            epcs.AddRange(GetQuantityEpcs(epcisEvent, "outputQuantityList"));
            epcs.AddRange(GetQuantityEpcs(epcisEvent, "childQuantityList"));

            string? parentId = epcisEvent["parentID"]?.Value<string>();
            if (parentId != null)
            {
                epcs.Add(parentId);
            }

            epcs.Sort(StringComparer.Ordinal);

            string type = epcisEvent["type"]?.Value<string>() ?? string.Empty;
            string bizStep = epcisEvent["bizStep"]?.Value<string>() ?? string.Empty;
            string action = epcisEvent["action"]?.Value<string>() ?? string.Empty;
            string bizLocation = epcisEvent["bizLocation"]?["id"]?.Value<string>() ?? string.Empty;

            return $"{type}|{bizStep}|{action}|{bizLocation}|{string.Join(",", epcs)}";
        }

        /// <summary>
        /// Reads a string array property from the event, returning an empty sequence when absent.
        /// </summary>
        private static IEnumerable<string> GetStringArray(JObject epcisEvent, string propertyName)
        {
            return (epcisEvent[propertyName] as JArray ?? new JArray()).Select(t => t.Value<string>() ?? string.Empty);
        }

        /// <summary>
        /// Reads the epcClass values from a quantity list property, returning an empty sequence when absent.
        /// </summary>
        private static IEnumerable<string> GetQuantityEpcs(JObject epcisEvent, string propertyName)
        {
            return (epcisEvent[propertyName] as JArray ?? new JArray()).Select(t => t["epcClass"]?.Value<string>() ?? string.Empty);
        }

        /// <summary>
        /// Reads the event list from the document JSON.
        /// </summary>
        private static List<JObject> GetEvents(JObject documentJson)
        {
            return (documentJson["epcisBody"]?["eventList"] as JArray ?? new JArray()).OfType<JObject>().ToList();
        }

        /// <summary>
        /// Indexes the vocabulary elements of the document by (vocabulary type, element id).
        /// </summary>
        private static Dictionary<string, JObject> GetMasterDataElements(JObject documentJson)
        {
            Dictionary<string, JObject> elements = new Dictionary<string, JObject>();

            JArray vocabularies = documentJson["epcisHeader"]?["epcisMasterData"]?["vocabularyList"] as JArray ?? new JArray();
            foreach (JObject vocabulary in vocabularies.OfType<JObject>())
            {
                string vocabType = vocabulary["type"]?.Value<string>() ?? string.Empty;
                foreach (JObject element in (vocabulary["vocabularyElementList"] as JArray ?? new JArray()).OfType<JObject>())
                {
                    string id = element["id"]?.Value<string>() ?? string.Empty;
                    elements[$"{vocabType}|{id}"] = element;
                }
            }

            return elements;
        }

        /// <summary>
        /// Produces a canonical single-line representation of a token: ignored properties removed and
        /// object properties sorted by name, so equivalent tokens compare equal as strings.
        /// </summary>
        private static string Canonicalize(JToken token)
        {
            return CanonicalizeToken(token).ToString(Formatting.None);
        }

        /// <summary>
        /// Recursively rebuilds a token with sorted object properties and without ignored properties.
        /// </summary>
        private static JToken CanonicalizeToken(JToken token)
        {
            if (token is JObject obj)
            {
                JObject result = new JObject();
                foreach (JProperty property in obj.Properties().Where(p => !_ignoredProperties.Contains(p.Name)).OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    result[property.Name] = CanonicalizeToken(property.Value);
                }
                return result;
            }

            if (token is JArray array)
            {
                // Array items are themselves canonicalized and sorted so multiset-equal arrays produce
                // the same canonical string regardless of element order.
                JArray result = new JArray();
                foreach (JToken item in array.Select(CanonicalizeToken).OrderBy(t => t.ToString(Formatting.None), StringComparer.Ordinal))
                {
                    result.Add(item);
                }
                return result;
            }

            return token.DeepClone();
        }
    }
}
