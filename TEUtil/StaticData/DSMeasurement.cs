using TraceabilityEngine.Util.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace TraceabilityEngine.Util.StaticData
{
    [DataContract]
    public class TEMeasurement : IComparable<TEMeasurement>, IComparable
    {
        protected double? _value;
        protected UOM _uom;
        //protected static UOMS _uoms = null;

        static TEMeasurement()
        {

        }

        public static TEMeasurement EmptyMeasurement()
        {
            TEMeasurement empty = new TEMeasurement();
            empty._value = null;
            empty._uom = null;
            return (empty);
        }

        public static bool IsNullOrEmpty(TEMeasurement measurement)
        {
            if (measurement == null)
            {
                return (true);
            }
            else if (UOM.IsNullOrEmpty(measurement.UoM))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        public TEMeasurement()
        {
            _value = 0;
            _uom = new UOM();
        }

        public TEMeasurement(TEXML xmlElement)
        {
            if (!xmlElement.IsNull)
            {
                _value = xmlElement.AttributeDoubleValueEx("Value");
                _uom = UOM.ParseFromName(xmlElement.Attribute("UoM"));
            }
            else
            {
                _value = null;
                _uom = new UOM();
            }
        }
        public TEMeasurement(TEMeasurement copyFrom)
        {
            if (copyFrom != null)
            {
                _value = copyFrom.Value;
                _uom = new UOM(copyFrom.UoM);
            }
            else
            {
                _value = null;
                _uom = new UOM();
            }
        }

        public TEMeasurement(double? value, UOM unitCode)
        {
            _value = value;
            _uom = unitCode;
        }

        public TEMeasurement(double? value, string unitCode)
        {
            _value = value;
            _uom = UOM.ParseFromName(unitCode);
        }

        public TEMeasurement(Int32 value)
        {
            _value = value;
            _uom = null;
        }

        public bool IsNullOrEmpty()
        {

            if (_value.HasValue && _uom != null && !string.IsNullOrEmpty(this.UoM.Key))
            {
                return (false);
            }
            return (true);
        }

        public bool ShouldSerializeValue()
        {
            return true;
        }

        public void Add(TEMeasurement measurement)
        {
            try
            {
                if (measurement != null)
                {
                    Value += measurement.Value;
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static TEMeasurement operator +(TEMeasurement left, TEMeasurement right)
        {
            if (left.UoM.UnitDimension != right.UoM.UnitDimension)
            {
                throw new TEException($"All operands must be of the same unit dimension. Left UoM = ${left.UoM.UNCode} | Right UoM = ${right.UoM.UNCode}.");
            }
            if (left.IsNullOrEmpty() || right.IsNullOrEmpty())
            {
                return new TEMeasurement(null, left.UoM);
            }
            double? rightValue = right.UoM.Convert(right.Value, left.UoM);
            double? sum = left.Value.Value + rightValue.Value;
            return (new TEMeasurement(sum, left.UoM));
        }

        public static TEMeasurement operator -(TEMeasurement left, TEMeasurement right)
        {
            if (left.UoM.UnitDimension != right.UoM.UnitDimension)
            {
                throw new TEException("All operands must be of the same unit dimension");
            }
            if (left.IsNullOrEmpty() || right.IsNullOrEmpty())
            {
                return new TEMeasurement(null, left.UoM);
            }
            double? rightValue = right.UoM.Convert(right.Value, left.UoM);
            double? diff = left.Value.Value - rightValue.Value;
            return (new TEMeasurement(diff, left.UoM));
        }

        public static TEMeasurement operator *(TEMeasurement left, double factor)
        {
            if (left.IsNullOrEmpty())
            {
                return new TEMeasurement(null, left.UoM);
            }
            double newValue = left.Value.Value * factor;
            return (new TEMeasurement(newValue, left.UoM));
        }

        public TEMeasurement ToBase()
        {
            // if the UoM is NULL or EMPTY then we just return this
            if (String.IsNullOrWhiteSpace(this.UoM?.UNCode)) return this;

            // otherwise then we convert to base
            TEMeasurement trBase = new TEMeasurement();
            trBase.UoM = UOMS.GetBase(this.UoM);

            // lets make sure we looked up the base UoM
            if (String.IsNullOrWhiteSpace(this.UoM?.UNCode)) throw new NullReferenceException("Failed to look up base UoM. UNCode=" + this.UoM.UNCode);

            trBase.Value = UoM.Convert(this.Value, trBase.UoM);
            trBase.Value = trBase.Value.Round();
            return (trBase);
        }

        public TEMeasurement ToSystem(UnitSystem unitSystem)
        {
            TEMeasurement trBase = new TEMeasurement();
            trBase.UoM = unitSystem.GetSystemUOM(this.UoM);
            trBase.Value = UoM.Convert(this.Value, trBase.UoM);
            trBase.Value = trBase.Value.Round();
            return (trBase);
        }

        public TEMeasurement ToSystem(UnitSystem unitSystem, string subGroup)
        {
            TEMeasurement trBase = new TEMeasurement();
            trBase.UoM = unitSystem.GetSystemUOM(this.UoM, subGroup);
            trBase.Value = UoM.Convert(this.Value, trBase.UoM);
            trBase.Value = trBase.Value.Round();
            return (trBase);
        }

        public TEMeasurement ConvertTo(string uomStr)
        {
            if (string.IsNullOrEmpty(uomStr))
            {
                return (this);
            }
            TEMeasurement trBase = new TEMeasurement();
            trBase.UoM = UOMS.GetUOMFromUNCode(uomStr);
            if (trBase.UoM == null)
            {
                return (this);
            }
            trBase.Value = UoM.Convert(this.Value, trBase.UoM);
            trBase.Value = trBase.Value.Round();
            return (trBase);
        }

        public TEMeasurement ConvertTo(UOM uom)
        {
            if (uom == null)
            {
                return (this);
            }
            TEMeasurement trBase = new TEMeasurement();
            trBase.UoM = uom;
            trBase.Value = UoM.Convert(this.Value, trBase.UoM);
            trBase.Value = trBase.Value.Round();
            return (trBase);
        }

        [DataMember]
        public double? Value
        {
            get
            {
                return (_value);
            }
            set
            {
                _value = value;
            }
        }

        [DataMember]
        [DefaultValue(null)]
        public UOM UoM
        {
            get
            {
                return (_uom);
            }
            set
            {
                _uom = value;
            }
        }

        public override string ToString()
        {
            try
            {
                if (this.IsNullOrEmpty())
                {
                    return ("");
                }

                string str = this.Value.TEToString();
                str += " " + this.UoM.UNCode;
                return str;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string ToStringEx()
        {
            try
            {
                if (this.IsNullOrEmpty())
                {
                    return ("");
                }

                string str = this.Value.TEToString();
                str += " " + this.UoM.Abbreviation;
                return str;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public string GetUniquenessKey(int iVersion)
        {
            try
            {
                if (this.Value == null && this.UoM == null)
                {
                    return "";
                }
                else
                {
                    return (this.ToBase().ToString()).Trim();
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static TEMeasurement Parse(string strValue)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(strValue))
                {
                    TEMeasurement emptyMeasurement = new TEMeasurement();
                    return (emptyMeasurement);
                }

                strValue = strValue.Trim();
                string[] strParts = strValue.Split(' ');
                if (strParts.Count() != 2)
                {
                    throw new TEException("Invalid Measurment string encountered, value=" + strValue + ". String must have a value and the UOM UN Code.");
                }
                string numberStr = strParts[0];
                string uomStr = strParts[1];


                numberStr = numberStr.Trim();
                uomStr = uomStr.Trim();

                List<UOM> uoms = UOMS.UOMList;

                double dblValue = double.Parse(numberStr);
                UOM uom = UOMS.GetUOMFromUNCode(uomStr);
                if (uom == null)
                {
                    uom = uoms.Find(u => u.Abbreviation.ToLower() == uomStr.ToLower() || uomStr.ToLower() == u.Name.ToLower());
                }

                if (uom == null && !String.IsNullOrWhiteSpace(uomStr))
                {
                    throw new Exception(String.Format("Failed to recognize UoM while parsing a TRMeasurement from a string. String={0}, Value={1}, UoM={2}", strValue, numberStr, uomStr));
                }

                TEMeasurement measurement = new TEMeasurement();
                measurement.Value = dblValue;
                measurement.UoM = uom;
                return measurement;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        /// <summary>
        /// Tries to parse the Measurement.
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static TEMeasurement TryParse(string strValue)
        {
            TEMeasurement measure = null;
            try
            {
                if (!string.IsNullOrEmpty(strValue))
                {
                    measure = TEMeasurement.Parse(strValue);
                }
            }
            catch (Exception Ex)
            {
#if DEBUG
                TELogger.Log(5, "Failed to parse a measurement. strValue=" + strValue);
                TELogger.Log(5, Ex);
#endif
            }
            return measure;
        }

        /// <summary>
        /// This method will reflect through an object looking for all TRMeasurement properties
        /// in an effort to convert all of them to the default unit system. This is useful for when
        /// editing an object, we can run it through this method so that when it's loaded into the 
        /// editor it will be in the default unit system for that account.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="unitSystem"></param>
        public static void ConvertMeasurementsToUnitSystem(object obj, UnitSystem unitSystem)
        {
            try
            {
                if (obj is IList)
                {
                    foreach (object value in (IList)obj)
                    {
                        ConvertMeasurementsToUnitSystem(value, unitSystem);
                    }
                }
                else
                {
                    foreach (PropertyInfo pInfo in obj.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == 0))
                    {
                        if (pInfo.PropertyType != typeof(string)
                            && !pInfo.PropertyType.IsValueType
                            && pInfo.GetCustomAttribute(typeof(DataMemberAttribute)) != null)
                        {
                            object propValue = pInfo.GetValue(obj);
                            if (propValue != null)
                            {
                                if (propValue is TEMeasurement)
                                {
                                    TEMeasurement measurement = (TEMeasurement)propValue;
                                    if (!TEMeasurement.IsNullOrEmpty(measurement))
                                    {
                                        string subGroup = "Medium";
                                        TEMeasurementAttribute measureAttribute = pInfo.GetCustomAttribute<TEMeasurementAttribute>();
                                        if (measureAttribute != null)
                                        {
                                            subGroup = measureAttribute.SubGroup;
                                        }
                                        measurement = measurement.ToSystem(unitSystem, subGroup);
                                        pInfo.SetValue(obj, measurement);
                                    }
                                }
                                else if (propValue is IList)
                                {
                                    foreach (object value in (IList)propValue)
                                    {
                                        if (value != null)
                                        {
                                            ConvertMeasurementsToUnitSystem(value, unitSystem);
                                        }
                                    }
                                }
                                else if (!pInfo.PropertyType.IsValueType && pInfo.PropertyType != typeof(string))
                                {
                                    ConvertMeasurementsToUnitSystem(propValue, unitSystem);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static bool operator ==(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return (true);
            }
            if (ReferenceEquals(lhs, null))
            {
                return (false);
            }
            int iCompare = lhs.CompareTo(rhs);
            if (iCompare == 0)
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        public static bool operator !=(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (lhs == rhs)
            {
                return (false);
            }
            else
            {
                return (true);
            }
        }

        public static bool operator >(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return (false);
            }
            if (ReferenceEquals(lhs, null))
            {
                return (true);
            }

            int iCompare = lhs.CompareTo(rhs);
            if (iCompare == 1)
            {
                return (true);
            }
            else
            {
                return (false);
            }


        }

        public static bool operator <(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return (false);
            }
            if (ReferenceEquals(lhs, null))
            {
                return (true);
            }

            int iCompare = lhs.CompareTo(rhs);
            if (iCompare == -1)
            {
                return (true);
            }
            else
            {
                return (false);
            }


        }

        public static bool operator <=(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return (true);
            }
            if (ReferenceEquals(lhs, null))
            {
                return (false);
            }
            int iCompare = lhs.CompareTo(rhs);
            if (iCompare != 1)
            {
                return (true);
            }
            else
            {
                return (false);
            }


        }

        public static bool operator >=(TEMeasurement lhs, TEMeasurement rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return (true);
            }
            if (ReferenceEquals(lhs, null))
            {
                return (false);
            }
            int iCompare = lhs.CompareTo(rhs);
            if (iCompare != -1)
            {
                return (true);
            }
            else
            {
                return (false);
            }


        }
        public override bool Equals(object obj)
        {
            if (!ReferenceEquals(obj, null))
            {
                if (obj is TEMeasurement)
                {
                    TEMeasurement other = (TEMeasurement)obj;
                    if (Value == other.Value && UoM == other.UoM)
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }
        public int CompareTo(object obj)
        {
            if (!ReferenceEquals(obj, null))
            {
                return (1);
            }
            else if (obj is TEMeasurement)
            {
                TEMeasurement other = (TEMeasurement)obj;
                return (CompareTo(other));
            }
            else
            {
                throw new Exception("object is not of type TRMeasurement");
            }
        }
        public int CompareTo(TEMeasurement other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            //If this instance has a value;
            if (this.Value.HasValue)
            {
                if (!other.Value.HasValue)
                {
                    return 1;
                }
                TEMeasurement thisBase = this.ToBase();
                TEMeasurement otherBase = other.ToBase();
                if (thisBase.Value == otherBase.Value)
                {
                    return (0);
                }
                else if (thisBase.Value < otherBase.Value)
                {
                    return (-1);
                }
                else
                {
                    return (1);
                }
            }
            else if (other.Value.HasValue)
            {
                return (-1);
            }
            else
            {
                return (0);
            }
        }

        public override int GetHashCode()
        {
            return (Value.GetHashCode() + (UoM?.GetHashCode() ?? 0));
        }

        public TEXML ToXML(TEXML xmlParent, string Name)
        {
            TEXML xmlElement = xmlParent.AddChild(Name);
            xmlElement.Attribute("Value", Value);
            xmlElement.Attribute("UoM", UoM.UNCode);
            xmlElement.Attribute("UoMAbbrev", UoM.Abbreviation);
            return (xmlElement);
        }

        public static TEMeasurement FromXML(TEXML xmlElement)
        {
            if (xmlElement != null && !xmlElement.IsNull)
            {
                double? val = xmlElement.AttributeDoubleValueEx("Value");
                string strUOM = xmlElement.Attribute("UoM");
                UOM uom = UOM.LookUpFromUNCode(strUOM);
                return new TEMeasurement(val, uom);
            }
            else
            {
                return (null);
            }
        }


    }

    public class TEMeasurementAttribute : System.Attribute
    {
        public string SubGroup { get; set; }

    }
}
