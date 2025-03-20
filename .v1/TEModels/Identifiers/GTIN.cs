using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using MongoDB.Bson.Serialization;

namespace TraceabilityEngine.Models.Identifiers
{
    [DataContract]
    public class GTIN : IGTIN, IEquatable<GTIN>, IComparable<GTIN>
    {
        private string _gtinStr;

        public GTIN()
        {

        }
        public GTIN(string gtinStr)
        {
            try
            {
                string error = GTIN.DetectGTINIssue(gtinStr);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new TEException($"The GTIN {gtinStr} is invalid. {error}");
                }
                this._gtinStr = gtinStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static bool TryParse(string gtinStr, out IGTIN gtin, out string error)
        {
            try
            {
                error = GTIN.DetectGTINIssue(gtinStr);
                if (string.IsNullOrWhiteSpace(error))
                {
                    gtin = new GTIN(gtinStr);
                    return true;
                }
                else
                {
                    gtin = null;
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
            try
            {
                return $"{baseURL}/gln/{this._gtinStr}";
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }


        /// <summary>
        /// This function will analyze a GTIN and try to return feedback with why a GTIN is not valid.
        /// </summary>
        /// <param name="gtinStr">The GTIN string.</param>
        /// <returns>An error if a problem is detected, otherwise returns NULL if no problem detected and the GTIN is valid.</returns>
        public static string DetectGTINIssue(string gtinStr)
        {
            try
            {
                if (string.IsNullOrEmpty(gtinStr))
                {
                    return ("GTIN is NULL or EMPTY.");
                }
                else if (gtinStr.IsURICompatibleChars() == false)
                {
                    return ("The GTIN contains non-compatiable characters for a URI.");
                }
                else if (gtinStr.Contains(" "))
                {
                    return ("GTIN cannot contain spaces.");
                }
                else if (gtinStr.Length == 14 && gtinStr.IsOnlyDigits())
                {
                    // validate the checksum
                    char checksum = GS1.CalculateGTIN14CheckSum(gtinStr);
                    if (checksum != gtinStr.ToCharArray().Last())
                    {
                        return string.Format("The check sum did not calculate correctly. The expected check sum was {0}. " +
                            "Please make sure to validate that you typed the GTIN correctly. It's possible the check sum " +
                            "was typed correctly but another number was entered wrong.", checksum);
                    }

                    return (null);
                }
                if (gtinStr.StartsWith("urn:") && gtinStr.Contains(":product:class:"))
                {
                    return (null);
                }
                else if (gtinStr.StartsWith("urn:") && gtinStr.Contains(":idpat:sgtin"))
                {
                    string lastPiece = gtinStr.Split(':').Last().Replace(".", "");
                    if (!TEStringExtensions.IsOnlyDigits(lastPiece))
                    {
                        return ("This is supposed to be a GS1 GTIN based on the System Prefix and " +
                            "Data Type Prefix. That means the Company Prefix and Serial Numbers " +
                            "should only be digits. Found non-digit characters in the Company Prefix " +
                            "or Serial Number.");
                    }
                    else if (lastPiece.Length != 13)
                    {
                        return ("This is supposed to be a GS1 GTIN based on the System Prefix and Data Type " +
                            "Prefix. That means the Company Prefix and Serial Numbers should contain a maximum " +
                            "total of 13 digits between the two. The total number of digits when combined " +
                            "is " + lastPiece.Length + ".");
                    }

                    return (null);
                }
                else
                {
                    return ("The GTIN is not in a valid EPCIS URI format or in GS1 GTIN-14 format.");
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, "Failed to detect GTIN Issues. GTIN=" + gtinStr);
                TELogger.Log(0, Ex);
                throw;
            }
        }

        /// <summary>
        /// This function detects if the GTIN is a valid GTIN str or not.
        /// </summary>
        /// <param name="gtinStr"></param>
        /// <returns></returns>
        public static bool IsGTIN(string gtinStr)
        {
            try
            {
                if (DetectGTINIssue(gtinStr) == null)
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

        #region Overrides
        public static bool operator ==(GTIN obj1, GTIN obj2)
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
        public static bool operator !=(GTIN obj1, GTIN obj2)
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

                return this.IsEquals((GTIN)obj);
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
                return this._gtinStr;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        #endregion

        #region IEquatable<GTIN>
        public bool Equals(GTIN gtin)
        {
            try
            {
                if (Object.ReferenceEquals(null, gtin))
                {
                    return false;
                }

                if (Object.ReferenceEquals(this, gtin))
                {
                    return true;
                }

                return this.IsEquals(gtin);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        private bool IsEquals(GTIN gtin)
        {
            try
            {
                if (gtin == null) throw new ArgumentNullException(nameof(gtin));
                if (this.ToString() == gtin.ToString())
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
        public int CompareTo(GTIN gtin)
        {
            try
            {
                if (gtin == null) throw new ArgumentNullException(nameof(gtin));

                long myInt64Hash = this.ToString().GetInt64HashCode();
                long otherInt64Hash = gtin.ToString().GetInt64HashCode();

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

        #region
        public Type ValueType => typeof(GTIN);
        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                string gtinStr = context.Reader.ReadString();
                GTIN gtin = new GTIN(gtinStr);
                return gtin;
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
