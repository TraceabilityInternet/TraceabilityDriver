using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TraceabilityEngine.Util
{
    public class TEEnumUtil
    {
        /// <summary>
        /// This will return the value of the TRDescriptionAttribute.Description attribute on the given enum value. If the attribute is not found, then it will return an empty string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetEnumDescription(object value)
        {
            try
            {
                FieldInfo fi = value.GetType().GetRuntimeField(value.ToString());

                TEDescriptionAttribute[] attributes =
                    (TEDescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(TEDescriptionAttribute),
                    false);

                if (attributes != null &&
                    attributes.Length > 0)
                    return attributes[0].Description;
                else
                    return "";
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static string GetEnumDisplayName(object value)
        {
            try
            {
                FieldInfo fi = value.GetType().GetRuntimeField(value.ToString());

                TEDisplayName[] attributes =
                    (TEDisplayName[])fi.GetCustomAttributes(
                    typeof(TEDisplayName),
                    false);

                if (attributes != null &&
                    attributes.Length > 0)
                    return attributes[0].Name;
                else
                    return "";
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static string GetEnumKey(object value)
        {
            try
            {
                Type type = null;
                if (value is Type)
                {
                    type = value as Type;
                }
                else
                {
                    type = value.GetType();
                }
                FieldInfo fi = type.GetRuntimeField(value.ToString());

                if (fi == null)
                {
                    return "";
                }

                TEKey[] attributes =
                    (TEKey[])fi.GetCustomAttributes(
                    typeof(TEKey),
                    false);

                if (attributes != null &&
                    attributes.Length > 0)
                    return attributes[0].Key;
                else
                    return "";
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public static T GetEnumFromDescription<T>(string description)
        {
            T value = default(T);

            foreach (T t in GetValues<T>())
            {
                string desc = GetEnumDescription(t);
                if (desc == description)
                {
                    value = t;
                    break;
                }
            }
            return value;
        }

        public static string GetEnumFullNamespacePath(Enum value)
        {
            FieldInfo fi = value.GetType().GetRuntimeField(value.ToString());

            TEAssemblyAttribute[] attributes =
                (TEAssemblyAttribute[])fi.GetCustomAttributes(
                typeof(TEAssemblyAttribute),
                false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].FullNameSpace;
            else
                return "";
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static T InstantiateFromEnum<T>(System.Enum enumValue)
        {
            string assemblyQualifiedName = TEEnumUtil.GetEnumFullNamespacePath(enumValue);
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                throw new TEException("Failed to find Assembly Qualified Name from the Enum attributes. The enum value " +
                    "must implement [TRAssembly] attribute. Enum=" + enumValue.ToString());
            }
            Type type = enumValue.GetType().Assembly.GetType(assemblyQualifiedName);
            if (type == null)
            {
                throw new TEException("Failed to find Type by Assembly Qualified Name while instanting from Enum. " + assemblyQualifiedName);
            }
            return (T)Activator.CreateInstance(type);
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TEAssemblyAttribute : Attribute
    {

        private string m_fullNameSpace;
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DescriptionAttribute
        //     class with no parameters.
        public TEAssemblyAttribute()
        {

        }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.DescriptionAttribute
        //     class with a description.
        //
        // Parameters:
        //   description:
        //     The description text.
        public TEAssemblyAttribute(string fullNameSpace)
        {
            m_fullNameSpace = fullNameSpace;
        }

        public TEAssemblyAttribute(Type type)
        {
            m_fullNameSpace = type.FullName;
        }

        //
        // Summary:
        //     Gets the description stored in this attribute.
        //
        // Returns:
        //     The description stored in this attribute.
        public virtual string FullNameSpace
        {

            get
            {
                return (m_fullNameSpace);
            }

        }

    }

    [AttributeUsage(AttributeTargets.All)]
    public class TEDescriptionAttribute : System.Attribute
    {
        public string Description { get; set; }

        public TEDescriptionAttribute(string description)
        {
            this.Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TEDisplayName : System.Attribute
    {
        public string Name { get; set; }

        public TEDisplayName(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class TEKey : System.Attribute
    {
        public string Key { get; set; }

        public TEKey(string key)
        {
            this.Key = key;
        }
    }
}
