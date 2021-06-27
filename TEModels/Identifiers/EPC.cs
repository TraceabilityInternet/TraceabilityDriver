using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Extensions;

namespace TraceabilityEngine.Models.Identifiers
{
    public class EPC : IEPC
    {
        private string _epcStr;

        public EPCType Type { get; set; }
        public IGTIN GTIN { get; set; }
        public string SerialLotNumber { get; set; }

        public EPC(string epcStr)
        {
            try
            {
                string error = EPC.DetectEPCIssue(epcStr);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new TEException($"The EPC {epcStr} is invalid. {error}");
                }
                this._epcStr = epcStr;

                // if this is a GS1 class level epc (GS1 GTIN + Lot Number)
                if (epcStr.StartsWith("urn:epc:class:lgtin:"))
                {
                    this.Type = EPCType.Class;

                    List<string> parts = epcStr.Split(":").ToList();
                    List<string> parts2 = parts.Last().Split('.').ToList();
                    parts.RemoveAt(parts.Count - 1);

                    string gtinStr = String.Join(":", parts) + ":" + parts2[0] + "." + parts2[1];
                    gtinStr = gtinStr.Replace(":class:lgtin:", ":idpat:sgtin:");
                    this.SerialLotNumber = parts2[2];
                    this.GTIN = IdentifierFactory.ParseGTIN(gtinStr);
                }
                // else if this is a GS1 instance level epc (GS1 GTIN + Serial Number)
                else if (epcStr.StartsWith("urn:epc:id:sgtin:"))
                {
                    this.Type = EPCType.Instance;

                    List<string> parts = epcStr.Split(":").ToList();
                    List<string> parts2 = parts.Last().Split('.').ToList();
                    parts.RemoveAt(parts.Count - 1);

                    string gtinStr = String.Join(":", parts) + ":" + parts2[0] + "." + parts2[1];
                    gtinStr = gtinStr.Replace(":id:sgtin:", ":idpat:sgtin:");
                    this.SerialLotNumber = parts2[2];
                    this.GTIN = IdentifierFactory.ParseGTIN(gtinStr);
                }
                // else if this is a GDST / IBM private class level identifier (GTIN + Lot Number)
                else if (epcStr.StartsWith("urn:") && epcStr.Contains(":product:lot:class:"))
                {
                    this.Type = EPCType.Class;

                    List<string> parts = epcStr.Split(":").ToList();
                    List<string> parts2 = parts.Last().Split('.').ToList();
                    parts.RemoveAt(parts.Count - 1);

                    string gtinStr = String.Join(":", parts) + ":" + parts2[0] + "." + parts2[1];
                    gtinStr = gtinStr.Replace(":product:lot:class:", ":product:class:");
                    this.SerialLotNumber = parts2[2];
                    this.GTIN = IdentifierFactory.ParseGTIN(gtinStr);
                }
                // else if this is a GDST / IBM private instance level identifier (GTIN + Serial Number) 
                else if (epcStr.StartsWith("urn:") && epcStr.Contains(":product:serial:obj:"))
                {
                    this.Type = EPCType.Instance;

                    List<string> parts = epcStr.Split(":").ToList();
                    List<string> parts2 = parts.Last().Split('.').ToList();
                    parts.RemoveAt(parts.Count - 1);

                    string gtinStr = String.Join(":", parts) + ":" + parts2[0] + "." + parts2[1];
                    gtinStr = gtinStr.Replace(":product:serial:obj:", ":product:class:");
                    this.SerialLotNumber = parts2[2];
                    this.GTIN = IdentifierFactory.ParseGTIN(gtinStr);
                }
                else if (epcStr.StartsWith("urn:sscc:"))
                {
                    this.Type = EPCType.Instance;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static string DetectEPCIssue(string epcStr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epcStr))
                {
                    return ("The EPC is a NULL or White Space string.");
                }

                if (!epcStr.IsURICompatibleChars())
                {
                    return ("The EPC contains non-compatiable characters for a URI.");
                }

                // if this is a GS1 class level epc (GS1 GTIN + Lot Number)
                if (epcStr.StartsWith("urn:epc:class:lgtin:"))
                {
                    string[] parts = epcStr.Split(":");
                    string[] parts2 = parts.Last().Split('.');

                    if (parts2.Count() < 3)
                    {
                        return $"The EPC {epcStr} is not in the right format. It doesn't contain a company prefix, item code, and lot number.";
                    }
                    else
                    {
                        return null;
                    }
                }
                // else if this is a GS1 instance level epc (GS1 GTIN + Serial Number)
                else if (epcStr.StartsWith("urn:epc:id:sgtin:"))
                {
                    string[] parts = epcStr.Split(":");
                    string[] parts2 = parts.Last().Split('.');

                    if (parts2.Count() < 3)
                    {
                        return $"The EPC {epcStr} is not in the right format. It doesn't contain a company prefix, item code, and lot number.";
                    }
                    else
                    {
                        return null;
                    }
                }
                // else if this is a GDST / IBM private class level identifier (GTIN + Lot Number)
                else if (epcStr.StartsWith("urn:") && epcStr.Contains(":product:lot:class:"))
                {
                    string[] parts = epcStr.Split(":");
                    string[] parts2 = parts.Last().Split('.');

                    if (parts2.Count() < 3)
                    {
                        return $"The EPC {epcStr} is not in the right format. It doesn't contain a company prefix, item code, and serial number.";
                    }
                    else
                    {
                        return null;
                    }
                }
                // else if this is a GDST / IBM private instance level identifier (GTIN + Serial Number) 
                else if (epcStr.StartsWith("urn:") && epcStr.Contains(":product:serial:obj:"))
                {
                    string[] parts = epcStr.Split(":");
                    string[] parts2 = parts.Last().Split('.');

                    if (parts2.Count() < 3)
                    {
                        return $"The EPC {epcStr} is not in the right format. It doesn't contain a company prefix, item code, and a serial number.";
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (epcStr.StartsWith("urn:sscc:"))
                {
                    return null;
                }
                else
                {
                    return "This EPC does not fit any of the allowed formats.";
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static bool TryParse(string epcStr, out IEPC epc, out string error)
        {
            try
            {
                error = EPC.DetectEPCIssue(epcStr);
                if (string.IsNullOrWhiteSpace(error))
                {
                    epc = new EPC(epcStr);
                    return true;
                }
                else
                {
                    epc = null;
                    return false;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(2, Ex);
                throw;
            }
        }

        public string ToDigitalLinkURL(string baseURL)
        {
            throw new NotImplementedException();
        }

        #region Overrides
        public static bool operator ==(EPC obj1, EPC obj2)
        {
            try
            {
                if (Object.ReferenceEquals(null, obj1) && Object.ReferenceEquals(null, obj2))
                {
                    return true;
                }

                if (!Object.ReferenceEquals(null, obj1) && Object.ReferenceEquals(null, obj2))
                {
                    return false;
                }

                if (Object.ReferenceEquals(null, obj1) && !Object.ReferenceEquals(null, obj2))
                {
                    return false;
                }

                return obj1.Equals(obj2);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public static bool operator !=(EPC obj1, EPC obj2)
        {
            try
            {
                if (Object.ReferenceEquals(null, obj1) && Object.ReferenceEquals(null, obj2))
                {
                    return false;
                }

                if (!Object.ReferenceEquals(null, obj1) && Object.ReferenceEquals(null, obj2))
                {
                    return true;
                }

                if (Object.ReferenceEquals(null, obj1) && !Object.ReferenceEquals(null, obj2))
                {
                    return true;
                }

                return !obj1.Equals(obj2);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public override bool Equals(object obj)
        {
            try
            {
                if (Object.ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return this.IsEquals((EPC)obj);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public override int GetHashCode()
        {
            try
            {
                int hash = this.ToString().GetInt32HashCode();
                return hash;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        public override string ToString()
        {
            try
            {
                return _epcStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        #endregion

        #region IEquatable<EPC>
        public bool Equals(EPC epc)
        {
            try
            {
                if (Object.ReferenceEquals(null, epc))
                {
                    return false;
                }

                if (Object.ReferenceEquals(this, epc))
                {
                    return true;
                }

                return this.IsEquals(epc);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        private bool IsEquals(EPC epc)
        {
            try
            {
                if (epc == null) throw new ArgumentNullException(nameof(epc));

                if (this.ToString() == epc.ToString())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        #endregion

        #region IComparable 
        public int CompareTo(EPC epc)
        {
            try
            {
                if (epc == null) throw new ArgumentNullException(nameof(epc));

                long myInt64Hash = this.ToString().GetInt64HashCode();
                long otherInt64Hash = epc.ToString().GetInt64HashCode();

                if (myInt64Hash > otherInt64Hash) return -1;
                if (myInt64Hash == otherInt64Hash) return 0;
                return 1;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        #endregion

        #region IBsonSerializer
        public Type ValueType => typeof(EPC);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                string epcStr = context.Reader.ReadString();
                EPC epc = new EPC(epcStr);
                return epc;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            try
            {
                context.Writer.WriteString(value.ToString());
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        #endregion
    }
}
