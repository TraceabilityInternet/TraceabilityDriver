using TraceabilityEngine.Util.StaticData;
using TraceabilityEngine.StaticData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using TraceabilityEngine.Interfaces.Models.Common;
using TraceabilityEngine.Models;
using System.Globalization;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Mappers
{
    public class GS1WebVocabMapper
    {
        public bool? GetBoolValue(JObject json, string propertyName)
        {
            bool? value = null;
            if (json[propertyName]?["@value"] != null)
            {

                value = json[propertyName]["@value"].Value<bool?>();

            }
            else
            {
                value = json[propertyName]?.Value<bool?>();
            }
            return value;
        }
       
        public static DateTime? GetDateValue(JObject json, string propertyName)
        {
            DateTime? value = null;
            if (json[propertyName]?["@value"] != null)
            {

                value = json[propertyName]["@value"].Value<DateTime?>();

            }
            else
            {
                value = json[propertyName]?.Value<DateTime?>();
            }
            return value;
        }

        public static double GetDoubleValue(JObject json, string propertyName)
        {
            try
            {
                double value = 0;
                if (json?[propertyName] != null)
                {
                    if (json?[propertyName] is JObject && json[propertyName]?["@value"] != null)
                    {
                        value = json[propertyName]["@value"].Value<double>();
                    }
                    else if (json?[propertyName] is JObject && json[propertyName]?["@id"] != null)
                    {
                        value = json[propertyName]["@id"].Value<double>();
                    }
                    else
                    {
                        value = json[propertyName]?.Value<double>() ?? 0;
                    }
                }
                return value;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public static int GetIntValue(JObject json, string propertyName)
        {
            int value = 0;
            if (json[propertyName]?["@value"] != null)
            {

                value = json[propertyName]["@value"].Value<int>();

            }
            else
            {
                value = json[propertyName]?.Value<int>() ?? 0;
            }
            return value;
        }
        public static string GetStringValue(JObject json, string propertyName)
        {
            string value = null;

            if (json?[propertyName] != null)
            {
                if (json[propertyName] is JArray)
                {
                    JArray jLanguages = json[propertyName] as JArray;
                    if (jLanguages.Count > 0)
                    {
                        JObject jLang = jLanguages.First as JObject;
                        value = jLang["@value"]?.ToString();
                    }
                }
                else
                {
                    value = json[propertyName]?.ToString();
                }
            }

            return value;
        }
        public static string GetLanguageString(JToken jt, string propertyName, string language = "en")
        {
            string value = null;
            if (jt != null & jt is JObject)
            {
                JObject json = jt as JObject;
                if (json?[propertyName] != null && json[propertyName] is JArray)
                {
                    JArray jArr = json[propertyName] as JArray;
                    foreach (JObject jLang in jArr)
                    {
                        if (jLang.Value<string>("@language") == language)
                        {
                            value = jLang["@value"]?.ToString();
                        }
                    }
                }
            }
            return value;
        }
        public static Uri GetUri(JToken jToken, string propertyName)
        {
            Uri uri = null;
            if (jToken is JObject)
            {
                JObject json = jToken as JObject;
                if (json?[propertyName] != null)
                {
                    string uriStr = null;
                    if (json[propertyName] is JObject && json[propertyName]["@id"] != null)
                    {
                        uriStr = json[propertyName].Value<string>("@id");
                    }
                    else
                    {
                        uriStr = json.Value<string>(propertyName);
                    }

                    if (!string.IsNullOrWhiteSpace(uriStr))
                    {
                        Uri.TryCreate(uriStr, UriKind.RelativeOrAbsolute, out uri);
                    }
                }
            }
            return uri;
        }
        public static TEMeasurement GetMeasurement(JObject json, string propertyName)
        {
            TEMeasurement measurement = null;
            if (json[propertyName] != null && json[propertyName] is JObject)
            {
                JObject jMeasurement = json[propertyName] as JObject;
                string unitCode = GetStringValue(jMeasurement, "unitCode");
                double value = GetDoubleValue(jMeasurement, "value");
                if (!string.IsNullOrWhiteSpace(unitCode))
                {
                    measurement = new TEMeasurement(value, unitCode);
                }
            }
            return measurement;
        }
        public static void SetBoolAsObject(JObject json, string propertyName, bool? value)
        {
            json[propertyName] = new JObject();
            json[propertyName]["@value"] = value.ToString();
            json[propertyName]["@type"] = "xsd:boolean";
        }
        public static void SetContext(JObject json)
        {
            json["@context"] = new JObject();
            SetString((JObject)json["@context"], "gs1:", "http://gs1.org/voc/");
            SetString((JObject)json["@context"], "xsd", "http://www.w3.org/2001/XMLSchema#");
            SetString((JObject)json["@context"], "@vocab", "http://gs1.org/voc/");
        }
        public static void SetDateTimeAsObject(JObject json, string propertyName, DateTime? value)
        {
            if (json != null && value != null)
            {
                json[propertyName] = new JObject();
                json[propertyName]["@value"] = value?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK", CultureInfo.InvariantCulture);
                json[propertyName]["@type"] = "xsd:dateTime";
            }
        }
        public static void SetDouble(JObject json, string propertyName, double? value)
        {
            if (value != 0)
            {
                if (json != null) //CHECK THAT DOUBLE ISN'T NULL?
                {
                    json[propertyName] = new JObject();
                    json[propertyName]["@value"] = value.ToString();
                    json[propertyName]["@type"] = "xsd.float"; //Always @type or sometimes "type"?
                }
            }
        }
        public static void SetNullDouble(JObject json, string propertyName, double? value)
        {
            if (value != null)
            {
                json[propertyName] = new JObject();
                json[propertyName]["@value"] = value.ToString(); //Set ToString();
                json[propertyName]["@type"] = "xsd.float";
            }
        }
        public static void SetIDAsArray(JObject json, string propertyName, ITEStaticData value)
        {
            if (value != null)
            {
                if (json != null && !string.IsNullOrWhiteSpace(value.Key))
                {
                    JArray jArray = new JArray();
                    JObject jID = new JObject();
                    jID["@id"] = value.Key;
                    jArray.Add(jID);
                    json[propertyName] = jArray;
                }
            }
        }
        public static void SetIDAsArray(JObject json, string propertyName, string value)
        {
            if (json != null && !string.IsNullOrWhiteSpace(value))
            {
                JArray jArray = new JArray();
                JObject jID = new JObject();
                jID["@id"] = value;
                jArray.Add(jID);
                json[propertyName] = jArray;
            }
        }
        public static void SetIDAsString(JObject json, string propertyName, string value)
        {
            if (json != null && !string.IsNullOrWhiteSpace(value))
            {
                json[propertyName] = new JObject();
                json[propertyName]["@id"] = value;
            }
        }
        public static void SetIDAsString(JObject json, string propertyName, Uri value)
        {
            if (json != null && value != null)
            {
                json[propertyName] = new JObject();
                json[propertyName]["@id"] = value;
            }
        }
        public static string GetIDAsString(JObject json, string propertyName)
        {
            string value = null;
            if (json?[propertyName] != null)
            {
                if (json[propertyName] is JArray)
                {
                    value = json[propertyName][0]["@id"].ToString();
                    return value;
                }
                else
                {
                    value = GetStringValue((JObject)json[propertyName], "@id");
                    return value;
                }
            }
            return value;
        }
        public static void SetIDAsString(JObject json, string propertyName, ITEStaticData value)
        {
            if (value != null)
            {
                if (json != null && !string.IsNullOrWhiteSpace(value.Key))
                {
                    json[propertyName] = new JObject();
                    json[propertyName]["@id"] = value.Key;
                }
            }
        }
        public static void SetString(JObject json, string propertyName, string value)
        {
            if (json != null && !string.IsNullOrWhiteSpace(value))
            {
                json[propertyName] = value;
            }
        }
        public static void SetString(JObject json, string propertyName, Uri value)
        {
            if (json != null && value != null)
            {
                json[propertyName] = value;
            }
        }
        public static void SetString(JObject json, string propertyName, ITEStaticData value)
        {
            if (value != null)
            {
                if (json != null && !string.IsNullOrWhiteSpace(value.Key))
                {
                    json[propertyName] = value.Key;
                }
            }
        }
        public static void SetInteger(JObject json, string propertyName, int? value)
        {
            if (json != null && value != null)
            {
                JObject jInt = new JObject();
                jInt["@value"] = value;
                jInt["@type"] = "xsd:integer";
                json[propertyName] = jInt;
            }
        }
        public static void SetInteger(JObject json, string propertyName, int value)
        {
            if (json != null)
            {
                JObject jInt = new JObject();
                jInt["@value"] = value;
                jInt["@type"] = "xsd:integer";
                json[propertyName] = jInt;
            }
        }
        public static void SetLanguageString(JObject json, string propertyName, string value, string language = "en")
        {
            if (json != null && !string.IsNullOrWhiteSpace(value))
            {
                if (json[propertyName] == null)
                {
                    json[propertyName] = new JArray();
                }
                JArray jArr = json[propertyName] as JArray;
                JObject jLang = new JObject();
                jLang["@value"] = value;
                jLang["@language"] = language;
                jArr.Add(jLang);
            }
        }
        public static void SetMeasurement(JObject json, string propertyName, TEMeasurement measurement)
        {
            if (json != null && measurement != null)
            {
                JObject jMeasurement = new JObject();
                SetDouble(jMeasurement, "value", measurement.Value);
                SetString(jMeasurement, "unitCode", measurement.UoM?.UNCode);
                jMeasurement["@type"] = "gs1:QuantitativeValue";

                json[propertyName] = jMeasurement;
            }
        }
        public static void SetCountryAsArray(JObject json, string propertyName, List<Country> countries)
        {
            if (countries != null)
            {
                JArray jCountries = new JArray();
                foreach (Country country in countries)
                {
                    JObject jCountry = new JObject();
                    SetString(jCountry, "@type", "gs1:Country");
                    SetString(jCountry, "countryCode", country.Abbreviation); // culture info code right property?
                    jCountries.Add(jCountry);
                }
                json[propertyName] = jCountries;
            }
        }
        public static void SetCountryAsArray(JObject json, string propertyName, Country country)
        {
            if (country != null)
            {
                JArray jCountries = new JArray();
                JObject jCountry = new JObject();
                SetString(jCountry, "@type", "gs1:Country");
                SetString(jCountry, "countryCode", country.Abbreviation); // culture info code right property?
                jCountries.Add(jCountry);
                json[propertyName] = jCountries;
            }
            
        }
        public static void SetContacts(JObject json, List<ITEContact> contacts)
        {
            if (contacts != null)
            {
                JArray jArr = new JArray();
                foreach (ITEContact contact in contacts)
                {
                    JObject jContact = new JObject();
                    jContact["@type"] = "gs1:ContactPoint";
                    SetLanguageString(jContact, "contactType", contact.ContactType);
                    SetLanguageString(jContact, "contactTitle", contact.ContactTitle);
                    SetLanguageString(jContact, "responsibility", contact.Responsibility);
                    SetString(jContact, "name", contact.Name);
                    SetString(jContact, "email", contact.Email);
                    SetString(jContact, "telephone", contact.Phone);
                    SetString(jContact, "fax", contact.Fax);

                    if (contact.Languages != null && contact.Languages.Count > 0)
                    {
                        JArray jLangs = new JArray();
                        foreach (var lang in contact.Languages)
                        {
                            jLangs.Add(lang.Key);
                        }
                        jContact["availableLanguage"] = jLangs;
                    }

                    jArr.Add(jContact);
                }
                json["contactPoint"] = jArr;
            }
        }
        public static void SetCertificates(JObject json, List<ITECertificate> certificates, string propertyName="certification")
        {
            if (certificates != null)
            {
                JArray jArr = new JArray();

                foreach (ITECertificate cert in certificates)
                {
                    JObject jCertificate = new JObject();
                    jCertificate["@type"] = "gs1:CertificationDetails";
                    SetLanguageString(jCertificate, "certificationAgency", cert.Agency);
                    SetLanguageString(jCertificate, "certificationStandard", cert.Standard);
                    SetLanguageString(jCertificate, "certificationValue", cert.Value);
                    SetDateTimeAsObject(jCertificate, "certificationAuditDate", cert.AuditDate);
                    SetDateTimeAsObject(jCertificate, "certificationStartDate", cert.StartDate);
                    SetDateTimeAsObject(jCertificate, "certificationEndDate", cert.EndDate);
                    SetString(jCertificate, "certificationIdentification", cert.Identification);
                    SetString(jCertificate, "certificationAgencyURL", cert.AgencyURL);
                    SetString(jCertificate, "certificationSubject", cert.Subject);
                    SetString(jCertificate, "certificationType", cert.CertificateType?.Key);
                    SetString(jCertificate, "certificationURI", cert.URI);
                    SetString(jCertificate, "certificationValue", cert.Value);
                    jCertificate["referencedFileDetails"] = BuildAttachment(cert.Attachment);
                    jArr.Add(jCertificate);
                }

                json[propertyName] = jArr;
            }
        }
        public static JObject BuildAttachment(ITEAttachment attachment)
        {
            if (attachment != null)
            {
                JObject jAttachment = new JObject();
                jAttachment["@type"] = "gs1:ReferencedFileDetails";
                SetString(jAttachment, "description", attachment.Description);
                SetString(jAttachment, "referencedFileName", attachment.FileName);
                SetString(jAttachment, "referencedFileURI", attachment.URI);
                SetIDAsString(jAttachment, "referencedFileURL", attachment.URL);
                SetIDAsString(jAttachment, "referencedFileType", attachment.AttachmentType);
                SetMeasurement(jAttachment, "referencedFileSize", attachment.Size);
                SetString(jAttachment, "fileLanguageCode", attachment.Language);
                SetDateTimeAsObject(jAttachment, "referencedFileEffectiveStartDateTime", attachment.StartDate);
                SetDateTimeAsObject(jAttachment, "referencedFileEffectiveEndDateTime", attachment.EndDate);
                SetInteger(jAttachment, "filePixelWidth", attachment.PixelWidth);
                SetInteger(jAttachment, "filePixelHeight", attachment.PixelHeight);
                return jAttachment;
            }
            else
            {
                return null;
            }
        }
        public static void SetAttachments(JObject json, string propertyName, List<ITEAttachment> attachments)
        {
            if (json != null && attachments != null)
            {
                JArray jArr = new JArray();
                foreach (ITEAttachment attachment in attachments)
                {
                    JObject jAttachment = BuildAttachment(attachment);
                    if (jAttachment != null)
                    {
                        jArr.Add(jAttachment);
                    }
                }
                json[propertyName] = jArr;
            }
        }
        public static void SetPhotos(JObject json, string propertyName, List<ITEPhoto> photos)
        {
            if (json != null && photos != null)
            {
                JArray jArr = new JArray();
                foreach (ITEPhoto photo in photos)
                {
                    JObject jPhoto = new JObject();
                    jPhoto["@type"] = "gs1:ReferencedFileDetails";
                    SetIDAsString(jPhoto, "referencedFileURL", photo.URL);
                    SetString(jPhoto, "referencedFileURI", photo.URI);
                    SetIDAsString(jPhoto, "referencedFileName", photo.FileName);
                    SetInteger(jPhoto, "filePixelWidth", photo.PixelWidth);
                    SetInteger(jPhoto, "filePixelHeight", photo.PixelHeight);
                    SetMeasurement(jPhoto, "referencedFileSize", photo.Size);
                    jArr.Add(jPhoto);
                }
                json[propertyName] = jArr;
            }
        }
        public static List<ITEContact> GetContacts(JObject json, string propertyName)
        {
            List<ITEContact> contacts = new List<ITEContact>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jContacts = json[propertyName] as JArray;
                foreach (JObject jContact in jContacts)
                {
                    ITEContact contact = new TEContact();
                    contact.ContactType = GetLanguageString(jContact, "contactType");
                    contact.ContactTitle = GetLanguageString(jContact, "contactTitle");
                    contact.Responsibility = GetLanguageString(jContact, "responsibility");
                    contact.Name = GetStringValue(jContact, "name");
                    contact.Email = GetStringValue(jContact, "email");
                    contact.Phone = GetStringValue(jContact, "telephone");
                    contact.Fax = GetStringValue(jContact, "fax");

                    contact.Languages = new List<TELanguage>();
                    if (jContact["availableLanguage"] != null && jContact["availableLanguage"] is JArray)
                    {
                        JArray jLanguages = jContact["availableLanguage"] as JArray;
                        foreach (JToken jt in jLanguages)
                        {
                            string key = jt.ToString();
                            TELanguage language = TELanguage.GetFromKey(key);
                            if (language != null)
                            {
                                contact.Languages.Add(language);
                            }
                        }
                    }

                    contacts.Add(contact);
                }
            }
            return contacts;
        }
        public static List<ITEPhoto> GetPhotos(JObject json, string propertyName)
        {
            List<ITEPhoto> photos = new List<ITEPhoto>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jPhotos = json[propertyName] as JArray;
                foreach (JObject jObj in jPhotos)
                {
                    ITEPhoto photo = new TEPhoto();
                    photo.URL = GetUri(jObj, "referencedFileURL");
                    photo.URI = GetUri(jObj, "referencedFileURI");
                    photo.FileName = GetIDAsString(jObj, "referencedFileName");
                    photo.PixelHeight = GetIntValue(jObj, "filePixelHeight");
                    photo.PixelWidth = GetIntValue(jObj, "filePixelWidth");
                    photo.Size = GetMeasurement(jObj, "referencedFileSize");
                    photos.Add(photo);
                }
            }
            return photos;
        }
        public static ITEAttachment GetAttachment(JObject json, string propertyName = null)
        {
            ITEAttachment attachment = null;
            JObject jAttachment = null;
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                jAttachment = json;
            }
            else
            {
                jAttachment = json[propertyName] as JObject;
            }
            if (jAttachment != null)
            {
                attachment = new TEAttachment();
                attachment.AttachmentType = AttachmentType.GetFromKey(GetIDAsString(jAttachment, "referencedFileType"));
                attachment.Description = GetStringValue(jAttachment, "description");
                attachment.URI = GetUri(jAttachment, "referencedFileURI");
                attachment.URL = GetUri(jAttachment, "referencedFileURL");
                attachment.FileName = GetStringValue(jAttachment, "referencedFileName");
                attachment.Size = GetMeasurement(jAttachment, "referencedFileSize");
                attachment.Language = TELanguage.GetFromKey(GetStringValue(jAttachment, "fileLanguageCode"));
                attachment.StartDate = GetDateValue(jAttachment, "referencedFileEffectiveStartDateTime");
                attachment.EndDate = GetDateValue(jAttachment, "referencedFileEffectiveEndDateTime");
                attachment.PixelHeight = GetIntValue(jAttachment, "filePixelWidth");
                attachment.PixelWidth = GetIntValue(jAttachment, "filePixelHeight");
            }
            return attachment;
        }
        public static List<ITEAttachment> GetAttachments(JObject json, string propertyName)
        {
            List<ITEAttachment> attachments = new List<ITEAttachment>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jArr = json[propertyName] as JArray;
                foreach (JObject jAttachment in jArr)
                {
                    ITEAttachment attachment = GetAttachment(jAttachment);
                    if (attachment != null)
                    {
                        attachments.Add(attachment);
                    }
                }
            }
            return attachments;
        }
        public static List<ITECertificate> GetCertificates(JObject json, string propertyName)
        {
            List<ITECertificate> certificates = new List<ITECertificate>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jArr = json[propertyName] as JArray;
                foreach (JObject jCert in jArr)
                {
                    ITECertificate cert = new TECertificate();
                    cert.Agency = GetLanguageString(jCert, "certificationAgency");
                    cert.Standard = GetLanguageString(jCert, "certificationStandard");
                    cert.Value = GetLanguageString(jCert, "certificationValue");
                    cert.AuditDate = GetDateValue(jCert, "certificationAuditDate");
                    cert.StartDate = GetDateValue(jCert, "certificationStartDate");
                    cert.EndDate = GetDateValue(jCert, "certificationEndDate");
                    cert.Identification = GetStringValue(jCert, "certificationIdentification");
                    cert.AgencyURL = GetUri(jCert, "certificationAgencyURL");
                    cert.Subject = GetStringValue(jCert, "certificationSubject");
                    cert.CertificateType = CertificateType.GetFromKey(GetStringValue(jCert, "certificationType"));
                    cert.URI = GetUri(jCert, "certificationURI");
                    cert.Value = GetStringValue(jCert, "certificationValue");
                    cert.Attachment = GetAttachment(jCert, "referencedFileDetails");
                    certificates.Add(cert);
                }
            }
            return certificates;
        }
        public static void SetCountry(JObject json, string propertyName, Country country)
        {
            if (json != null && country != null)
            {
                JObject jCountry = new JObject();
                jCountry["gs1:countryCode"] = country.Abbreviation;
                json[propertyName] = jCountry;
            }
        }
        public static List<Country> GetCountryFromArray(JObject json, string propertyName)
        {
            List<Country> countries = new List<Country>();
            if (json?[propertyName] != null && json[propertyName] is JArray)
            {
                JArray jArr = json[propertyName] as JArray;
                foreach (JObject jCountry in jArr)
                {
                    string abbreviation = jCountry.Value<string>("gs1:countryCode") ?? jCountry.Value<string>("countryCode");
                    if (!string.IsNullOrWhiteSpace(abbreviation))
                    {
                        Country country = Countries.FromAbbreviation(abbreviation);
                        countries.Add(country);
                    }
                }
            }
            return countries;
        }
        public static Country GetCountry(JObject json, string propertyName)
        {
            Country country = null;
            if (json?[propertyName] != null && json?[propertyName] is JObject)
            {
                JObject jCountry = json?[propertyName] as JObject;
                string abbreviation = jCountry.Value<string>("gs1:countryCode") ?? jCountry.Value<string>("countryCode");
                if (!string.IsNullOrWhiteSpace(abbreviation))
                {
                    country = Countries.FromAbbreviation(abbreviation);
                }
            }
            return country;
        }
        public static void SetAddress(JObject json, string propertyName, ITEAddress address)
        {
            if (json != null && address != null)
            {
                JObject jAddress = new JObject();

                SetLanguageString(jAddress, "streetAddress", address.Address1);
                SetLanguageString(jAddress, "streetAddress2", address.Address2);
                SetLanguageString(jAddress, "addressLocality", address.City);
                SetLanguageString(jAddress, "addressRegion", address.State);
                SetLanguageString(jAddress, "postalCode", address.ZipCode);
                SetString(jAddress, "countyCode", address.County);
                SetCountry(jAddress, "addressCountry", address.Country);

                json[propertyName] = jAddress;
            }
        }
        public static ITEAddress GetAddress(JObject json, string propertyName)
        {
            ITEAddress address = null;
            if (json?[propertyName] != null && json[propertyName] is JObject)
            {
                address = new TEAddress();

                JObject jAddress = json[propertyName] as JObject;
                address.Address1 = GetLanguageString(jAddress, "streetAddress");
                address.Address2 = GetLanguageString(jAddress, "streetAddress2");
                address.City = GetLanguageString(jAddress, "addressLocality");
                address.State = GetLanguageString(jAddress, "addressRegion");
                address.ZipCode = GetLanguageString(jAddress, "postalCode");
                address.County = GetStringValue(jAddress, "countyCode");
                address.Country = GetCountry(jAddress, "addressCountry");
            }
            return address;
        }
    }
}

