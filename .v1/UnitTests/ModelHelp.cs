using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.StaticData;
using TraceabilityEngine.Util.StaticData;

namespace UnitTests
{
    public class ModelHelp
    {
        public static List<T> BuildData<T>(int numberOfItems)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < numberOfItems; i++)
            {
                T obj = (T)BuildObject(typeof(T));
                list.Add(obj);
            }
            return list;
        }

        private static object BuildObject(Type t)
        {
            if (t.IsInterface)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.IsInterface && type.IsAssignableTo(t))
                        {
                            t = type;
                            break;
                        }
                    }
                }
            }

            object obj = Activator.CreateInstance(t);

            // build each property
            foreach (PropertyInfo pInfo in t.GetProperties())
            {
                if (pInfo.SetMethod == null)
                {
                    continue;
                }

                object propObj = BuildType(pInfo.PropertyType);
                if (propObj != null)
                {
                    pInfo.SetValue(obj, propObj);
                }
            }

            return obj;
        }

        private static object BuildType(Type t)
        {
            if (t == typeof(string))
            {
                return "some_value";
            }
            else if (t == typeof(DateTime?) || t == typeof(DateTime))
            {
                return DateTime.Now;
            }
            else if (t == typeof(int) || t == typeof(int?))
            {
                return 1;
            }
            else if (t == typeof(double) || t == typeof(double?))
            {
                return 1.0;
            }
            else if (t == typeof(bool) || t == typeof(bool?))
            {
                return true;
            }
            else if (t == typeof(Uri))
            {
                return new Uri("https://www.google.com");
            }
            else if (t == typeof(Species))
            {
                return SpeciesList.GetSpeciesByAlphaCode("YFT");
            }
            else if (t == typeof(IGTIN))
            {
                IGTIN gtin = IdentifierFactory.ParseGTIN("urn:epc:idpat:sgtin:08600031303.00");
                return gtin;
            }
            else if (t == typeof(IPGLN))
            {
                IPGLN pgln = IdentifierFactory.ParsePGLN("urn:epc:id:sgln:08600031303.0");
                return pgln;
            }
            else if (t == typeof(IGLN))
            {
                IGLN gln = IdentifierFactory.ParseGLN("urn:epc:id:sgln:08600031303.0");
                return gln;
            }
            else if (t == typeof(Uri))
            {
                return new Uri("https://www.google.com");
            }
            else if (t == typeof(TEMeasurement))
            {
                TEMeasurement measurement = new TEMeasurement(1, "KGM");
                return measurement;
            }
            else if (t == typeof(Country))
            {
                Country country = Countries.FromCountryIso(840);
                return country;
            }
            else if (t.IsAssignableTo(typeof(ITEStaticData)))
            {
                // find the method that will return a list of the static data
                foreach (MethodInfo method in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (method.Name == "GetList")
                    {
                        IList staticList = (IList)method.Invoke(null, null);
                        if (staticList != null && staticList.Count > 0)
                        {
                            return staticList[0];
                        }
                    }
                }
            }
            else if (t.IsAssignableTo(typeof(IList)))
            {
                // we need to create an instance of the list
                IList theList = (IList)Activator.CreateInstance(t);
                Type itemType = t.GenericTypeArguments.FirstOrDefault();
                object itemObj = BuildType(itemType);
                theList.Add(itemObj);
                return theList;
            }
            else
            {
                object propObj = BuildObject(t);
                return propObj;
            }

            return null;
        }
    }
}
