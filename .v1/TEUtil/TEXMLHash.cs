using Force.Crc32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TraceabilityEngine.Util
{
    public static class TEXMLHash
    {
		static public void UpdateHash(TEXML xml)
		{
			CalculateDeepHashableString(xml, null);
		}

		/// <summary>
		/// The routine works from bottom up;
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="sb"></param>
		static private void CalculateDeepHashableString(TEXML xml, StringBuilder sbParent)
		{
			TEXML xmlChild;
			StringBuilder sbElement = new StringBuilder();
			for (xmlChild = xml.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
			{
				CalculateDeepHashableString(xmlChild, sbElement);
			}
			//we are now at a leaf node walking up;
			string str = CalculateHashableString(xml);
			sbElement.Append(str);
			string strFull = sbElement.ToString();
			//string md5Hash = Encode(str);
			//string md5HashDeep = Encode(strFull);
			int iCRC = (int)Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(str));
			int iCRCFull = (int)Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(strFull));
			xml.Attribute("CRC", iCRC);
			xml.Attribute("CRCFull", iCRCFull);
			//We need to add our stringbuilder to the parten string builder
			if (sbParent != null)
				sbParent.Append(strFull);
		}

		static private string CalculateHashableString(TEXML xml)
		{
			string strVal = xml.Name;
			if (xml.Value != null)
			{
				strVal += xml.Value;
			}
			XmlCDataSection cdata = xml.CData;
			if (cdata != null)
			{
				strVal += cdata.Value;
			}

			if (xml.Element != null)
			{
				if (xml.Element.HasAttributes == true)
				{
					List<string> attributes = new List<string>();
					foreach (XmlAttribute atrb in xml.Element.Attributes)
					{
						if ((atrb != null) && (atrb.Value != null))
						{
							if (atrb.Name != "CRC" && atrb.Name != "CRCFull")
							{
								attributes.Add(atrb.Name);
							}
						}
					}
					if (attributes.Count > 0)
					{
						attributes.Sort();
						foreach (string attName in attributes)
						{
							string val = xml.Element.Attributes[attName].Value;
							strVal += val;
						}
					}
				}
			}
			return (strVal);
		}
	}
}
