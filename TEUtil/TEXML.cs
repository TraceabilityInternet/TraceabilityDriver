using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Reflection;
using System.Globalization;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using TraceabilityEngine.Util.Extensions;

namespace TraceabilityEngine.Util
{
    [DataContract]
    public class TEXML:  IDisposable
    {
        private XmlDocument m_Doc;
        private XmlElement m_Element;
        private XmlNamespaceManager m_NSmanager;

        public static bool UseLowerCaseBooleans = false;

        /// <summary>
        /// Default Constructor for TEXML : initializes a null Doc, Element and NSmanager 
        /// </summary>
        public TEXML()
        {
            m_Doc = null;
            m_Element = null;
            m_NSmanager = null;
        }

        /// <summary>
        /// Constructor for TEXML taking a name and a dictionary of namespaces.  Initializes the TEXML XmlDocument, NSManager and Element and ties them together.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="NameSpaces"></param>
        public TEXML(string Name, Dictionary<String, String> NameSpaces)
        {
            // add namespaces from the dictionary to the NameTable property of the xml document
            foreach (KeyValuePair<string, string> kvp in NameSpaces)
            {
                Doc.NameTable.Add(kvp.Value);
            }
            // create a NSmanager using the xml document namespaces
            m_NSmanager = new XmlNamespaceManager(Doc.NameTable);
            // add each of the key/value pairs to the NSmanager namespace
            foreach (KeyValuePair<string, string> kvp in NameSpaces)
            {
                m_NSmanager.AddNamespace(kvp.Key, kvp.Value);
            }
            // create a character which we will use to split the Name parameter passed in
            char[] splitOn = new char[1];
            splitOn[0] = ':';
            // creates a string array from each of the : split parts of the Name parameter
            string[] parts = Name.Split(splitOn);
            XmlElement xEl = null;
            // check if the number of parts is equal to 2
            if (parts.Length == 2)
            {
                // create a prefix string from the first part
                string PreFix = parts[0];
                // create a local Name string from the second part
                string localName = parts[1];
                // create a url NS using the prefix/first part of the split
                string urlNS = NSManager.LookupNamespace(PreFix);
                // call CreateElement to create an element on our xml document using the prefix, local name, and url NS that we just created.
                xEl = Doc.CreateElement(PreFix, localName, urlNS);
            }
            // if the parts are greater than or less than 2
            else
            {
                // call CreateElement to create an element on our xml document using the Name parameter
                xEl = Doc.CreateElement(Name);
            }
            // Add the created element to our document by appending it to the end
            Doc.AppendChild(xEl);
            // set the Element property equal to the xml Element we created
            Element = xEl;
        }

        /// <summary>
        /// Constructor for TEXML taking a Name parameter. Initializes the TEXML XmlDocument, NSManager and Element and ties them together.
        /// </summary>
        /// <param name="name"></param>
        public TEXML(string name)
        {
            // call the CreateElement method to create an XmlElement using the name parameter
            XmlElement xEl = Doc.CreateElement(name);
            // Add the created element to our document by appending it to the end
            Doc.AppendChild(xEl);
            // set the Element property equal to the xml Element we created
            Element = xEl;
            // check if the NSManager has been created
            if (NSManager == null)
            {
                // if it has not, then we will create a new NS manager using the xml document's NameTable
                m_NSmanager = new XmlNamespaceManager(Doc.NameTable);
            }
        }

        /// <summary>
        /// Constructor for TEXML taking a XmlElement and an optional XmlNamespaceManager. Initializes the TEXML XmlDocument, NSManager and Element and ties them together.
        /// </summary>
        /// <param name="xEl"></param>
        /// <param name="NSmanager"></param>
        public TEXML(XmlElement xEl, XmlNamespaceManager NSmanager = null)
        {
            // initializes the XmlDocument
            Doc = null;
            // set the Element property equal to the xml Element parameter
            Element = xEl;
            // check if the XmlElement being passed in is null
            if (xEl != null)
            {
                // if it has value, then we set the xml Document as the OwnerDocument of our passed in XmlElement
                Doc = xEl.OwnerDocument;
                // then check if the NSManager is null
                if (NSmanager == null)
                {
                    // if it is null we will create a new NSManager from the constructor of the XmlNameSpaceManager and pass in our XmlDocuments Nametable
                    NSmanager = new XmlNamespaceManager(Doc.NameTable);
                    // Add to the NSManager the TR namespace with prefix and uri
                    NSmanager.AddNamespace("tr", "http://schema.traceregister.com");
                }
                // set the NS Manager property equal to the NSManager we created
                m_NSmanager = NSmanager;

                // check if the DocumentElement is null for our XmlDocument
                if (Doc.DocumentElement == null)
                {
                    // Add our Element to the xmlDocument.
                    Doc.AppendChild(Element);
                }
            }
        }

        /// <summary>
        /// Constructor for TEXML taking in a XmlDocument and option XmlNamespaceManager. Initializes the TEXML XmlDocument, NSManager and Element and ties them together.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <param name="NSmanager"></param>
        public TEXML(XmlDocument xDoc, XmlNamespaceManager NSmanager = null)
        {
            // set the XmlDocument property equal to the xml Document parameter
            Doc = xDoc;
            // set the Element as the DocumentElement of our passed in XmlDocument
            Element = xDoc.DocumentElement;
            // Check if the NSManager is null
            if (NSmanager == null)
            {
                // if it is null we will create a new NSManager from the constructor of the XmlNameSpaceManager and pass in our XmlDocuments Nametable
                NSmanager = new XmlNamespaceManager(Doc.NameTable);
                // Add to the NSManager the TR namespace with prefix and uri
                NSmanager.AddNamespace("tr", "http://schema.traceregister.com");
            }
            // set the NS Manager property equal to the NSManager we created
            m_NSmanager = NSmanager;
        }

        /// <summary>
        /// Constructor for TEXML taking in a XmlNode and a XmlNamespaceManager. Initializes the TEXML XmlDocument, NSManager and Element and ties them together.
        /// </summary>
        /// <param name="xNode"></param>
        /// <param name="NSmanager"></param>
        public TEXML(XmlNode xNode, XmlNamespaceManager NSmanager = null)
        {
            // Initialize the XmlDocument
            Doc = null;
            // Initialize the XmlElement
            XmlElement xEl = null;
            // check that the passed in xNode is a XmlElement type object
            if (xNode is XmlElement)
            {
                // If it is then we will cast it to a XmlElement and set the XmlElement
                xEl = (XmlElement)xNode;
            }
            // set the XmlElement property equal to the xmlElement that we created from our XmlNode parameter
            Element = xEl;
            // if the created property is not null
            if (Element != null)
            {
                // if it has value, then we set the xml Document as the OwnerDocument of our passed in XmlElement
                Doc = Element.OwnerDocument;
                if (NSmanager == null)
                {
                    // if it is null we will create a new NSManager from the constructor of the XmlNameSpaceManager and pass in our XmlDocuments Nametable
                    NSmanager = new XmlNamespaceManager(Doc.NameTable);
                    // Add to the NSManager the TR namespace with prefix and uri
                    NSmanager.AddNamespace("tr", "http://schema.traceregister.com");
                }
                // set the NS Manager property equal to the NSManager we created
                m_NSmanager = NSmanager;
                // check that the DocumentElement property (XmlElement) of our XmlDocument is null
                if (Doc.DocumentElement == null)
                {
                    // if the DocumentElement of the XmlDocument is null, then we will add the created Element to our Document
                    Doc.AppendChild(Element);
                }

            }
        }

        /// <summary>
        /// Constructor for TEXML taking in a TEXML as a parameter. Copies over the values of the passed in TEXML.
        /// </summary>
        /// <param name="xEl"></param>
        public TEXML(TEXML xEl)
        {
            Element = xEl.Element;
            Doc = xEl.Doc;
            m_NSmanager = xEl.m_NSmanager;
        }
        ~TEXML()
        {
            Dispose(false);
        }

        //IDisposable implementation. Internal call from the Dispose() method. Sets all properties to null to be garabage collected.
        private void Dispose(bool bDisp)
        {
            if (bDisp)
            {
                m_Element = null;
                if (m_Doc != null)
                {
                    m_Doc.RemoveAll();
                    m_Doc = null;
                }
                if (m_NSmanager != null)
                {
                    m_NSmanager = null;
                }
            }
        }
        // Dispose method called externally which allows the TEXML to be disposed of.
        public void Dispose()
        {
            Dispose(true);
        }

        // Static FromXElement method taking a XElement parameter. Loads/returns a TEXML element from a XElement.
        public static TEXML FromXElement(XElement el)
        {
            //Initializes a TEXML element.
            TEXML xml = null;
            try
            {
                // Create a new memory stream
                MemoryStream memoryStream = new MemoryStream();
                // outputs the parameter XElement to the memory stream
                el.Save(memoryStream);
                // Sets the streams position to the beginning
                memoryStream.Seek(0, SeekOrigin.Begin);
                // creates a new TEXML Element using the default constructor for our initializes TEXML
                xml = new TEXML();
                // Loads the memory stream into the XmlDocument property of the TEXML
                xml.Doc.Load(memoryStream);
                // sets the TEXML XmlDocument's DocumentElement property as the TEXML Element property
                xml.Element = xml.Doc.DocumentElement;
                // closes the memory stream and releases any resources associated (should ensure the stream is properly disposed still..)
                memoryStream.Close();

            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
            }
            // return the created TEXML
            return xml;
        }

        

        // Overrides the ToString() method and calls the XmlString property of TEXML which returns the TEXML Element property OuterXML as a string.
        public override string ToString()
        {
            // returns the Element.OuterXML string
            return (XmlString);
        }

        /// <summary>
        /// Creates an identical copy of the passed in TEXML by assigning the name, value, attributes and adding the source TEXML as a child of the created TEXML
        /// </summary>
        /// <param name="xmlSource"></param>
        /// <returns></returns>
        public TEXML ElementClone(TEXML xmlSource)
        {
            // Initializes a TEXML element
            TEXML xml = null;
            try
            {
                // check if the passed in TEXML Element property is not null
                if (xmlSource.Element != null)
                {
                    // creates a new XmlElement child from the passed in TEXML's Name property and appends it to the TEXML element which is returned to the intialized TEXML
                    xml = this.AddChild(xmlSource.Name);
                    // set the passed in TEXML value property as the intialized TEXML's value property
                    xml.Value = xmlSource.Value;
                    // check if the passed in TEXML Attributes property is not null
                    if (xmlSource.Attributes != null)
                    {
                        // iterates through each of the XmlAttribute on the passed TEXML collection of Attributes
                        foreach (XmlAttribute attribute in xmlSource.Attributes)
                        {
                            // each attribute is assigned to the Initialized TEXML Attribute property give a name property and value property of the passed in TEXML
                            xml.Attribute(attribute.Name, attribute.Value);
                        }
                    }
                }
            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
            }
            // returns the newly created TEXML
            return xml;
        }

        /// <summary>
        /// Creates a copy of the entire tree of this.TEXML Element given an attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public TEXML DeDup(string attribute)
        {
            // Creates a new TEXML element copy from the TEXML Element this is being called from
            TEXML xmlNew = this.ElementClone();
            // iterates through from the First Child of the TEXML Element (which is the first XmlElement on the XmlNode) and checks each node immediately following after until xmlChild is null.
            for (TEXML xmlChild = this.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
            {
                // creates a duplicate of the child iterated through using the TEXML xmlChild, passed in attribute parameter and the copied/cloned TEXML Element
                DeDup(xmlChild, attribute, xmlNew);
            }
            return (xmlNew);
        }

        /// <summary>
        /// Creates a copy of an entire tree of a TEXML Element
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="attribute"></param>
        /// <param name="xmlNew"></param>
        private void DeDup(TEXML xmlNode, string attribute, TEXML xmlNew)
        {
            // checks if the passed in TEXML Element called xmlNew is null or the Element property of the TEXML Element is null
            if (xmlNew == null || xmlNew.IsNull)
            {
                // if null then a shallow copy is created from the TEXML Element called xmlNode and assign as the value of the TEXML Element called xmlNew
                xmlNew = xmlNode.ElementClone();
            }
            // else if the TEXML Element is not null
            else
            {
                // returns the  path of the TEXML Element and attribute for all of it's parents to the root
                string xmlPath = xmlNode.XPathByAttribute(attribute);
                // creates a new TEXML Element from the TEXML Element called is xmlNew at the index of the returned path string.
                TEXML xmlNewElement = xmlNew[xmlPath];
                // if the TEXML Element is null or it's Element property is null
                if (xmlNewElement == null || xmlNewElement.IsNull)
                {
                    // TEXML Element xmlNew is assigned the value of copying the TEXML Element called xmlNode
                    xmlNew = xmlNew.ElementClone(xmlNode);
                }
                // if the value of the newly created TEXML Element is not null
                else
                {
                    // assign the value of the passed in TEXML Element called xmlNew the value of the newly created TEXML Element
                    xmlNew = xmlNewElement;
                }
            }
            // continue this process recursively for each sibling of TEXML Element called xmlNode until there are no more children/siblings left
            for (TEXML xmlChild = xmlNode.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
            {
                DeDup(xmlChild, attribute, xmlNew);
            }
        }

        /// <summary>
        /// Creates a copy of the TEXML Element this is being called from by copying the Name, Value and Attributes and returns the copy.
        /// </summary>
        /// <returns></returns>
        public TEXML ElementClone()
        {
            // Initializes a new TEXML element
            TEXML xml = null;
            try
            {
                // checks if the Element property is not null (if it is null, then null is returned below)
                if (Element != null)
                {
                    // if Element isn't null, then we create a new TEXML Element from the Name of this TEXML
                    xml = new TEXML(this.Name);
                    // we copy the value of this TEXML Element to our new TEXML Element
                    xml.Value = this.Value;
                    // we check that the collection  of XmlAttribute is not null
                    if (Attributes != null)
                    {
                        // and for each of the attributes in the collection
                        foreach (XmlAttribute attribute in Attributes)
                        {
                            // we create a XmlAttribute and append it to the TEXML Element using the values of the Name and Value properties of each attribute in the collection
                            xml.Attribute(attribute.Name, attribute.Value);
                        }
                    }
                }
            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
            }
            // returns the newly created TEXML element
            return xml;
        }

        /// <summary>
        /// Creates a copy of this.TEXML using the Element property and loading a new XmlDocument from that
        /// </summary>
        /// <returns></returns>
        public TEXML Clone()
        {
            // initializes a new TEXML Element
            TEXML xml = null;
            try
            {
                // checks if the TEXML Element property is not null
                if (Element != null)
                {
                    // creates a string from the TEXML Element property's OuterXml property
                    string str = Element.OuterXml;
                    // checks that the string created is not null
                    if (str != null)
                    {
                        // creates a new TEXML Element 
                        xml = new TEXML();
                        // Loads the TEXML Element properties and values from the passed in string
                        xml.LoadFromString(str);
                    }
                }
            }
            catch (Exception se)
            {
               TELogger.Log(0,se);
            }
            // returns the copy of the newly created TEXML Element
            return xml;
        }

        /// <summary>
        /// Get the path of the Root to this TEXML Element in order
        /// </summary>
        public string XPath
        {
            get
            {
                // returns the string of a TEXML Element node and it's parents
                string str = FindXPath(Element);
                // returns the string
                return (str);
            }
        }

        /// <summary>
        /// Get the path of the Root to this TEXML Element and Attribute in order
        /// </summary>
        /// <param name="Attribute"></param>
        /// <returns></returns>
        public string XPathByAttribute(string Attribute)
        {
            // Create a string of the Root to this TEXML Element and Attribute in order
            string str = FindXPath(this, Attribute);
            // returns the string value
            return (str);
        }

        /// <summary>
        /// Creates a Deep Clone of a TEXML Element.
        /// </summary>
        public TEXML DeepClone()
        {
            // initializes a TEXML Element
            TEXML xml = null;
            try
            {
                // this is faster
                xml = TEXML.CreateFromString(this.XmlString);

                // OFFICIAL PHILIP NOTE: This was deemed too slow because of some
                //                       testing that was done in V4. I have therefore
                //                       copied over the new implemntation of this method from
                //                       V4 to V5 and implemented it here.
                // check if the TEXML Element is not null
                //if (Element != null)
                //{
                //    // Creates a duplicate of the XmlDocument and assigns the value to the XmlNode
                //    XmlNode newDocNode = Doc.Clone();
                //    // checks if the XmlNode is a XmlDocument
                //    if (newDocNode is XmlDocument)
                //    {
                //        // casts the XmlNode to a XmlDocument
                //        XmlDocument newDoc = (XmlDocument)newDocNode;
                //        // creates a new TEXML Element from the newly created XmlDocument
                //        TEXML xmlNew = new TEXML(newDoc);
                //        // assigns the value of the new TEXML Element Element property, where the XmlNode path of the root to the TEXML Element matches, to the XmlNode
                //        XmlNode NewNode = xmlNew.Element.SelectSingleNode(this.XPath);
                //        // checks that the XmlNode is null
                //        if (NewNode == null)
                //        {
                //            // throws an exception with the XPath of the XmlNode if the XmlNode is null
                //            throw new InvalidOperationException(this.XPath);
                //        }
                //        // Creates a new TEXML Element from the XmlNode and a NS Manager
                //        xml = new TEXML(NewNode, m_NSmanager);
                //    }
                //}
            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
            }
            //returns the new TEXML Element
            return xml;
        }

        /// <summary>
        /// Recursively calls each parent and increments the level value each time. Returns the value of the number of nodes this TEXML Element is away from the Root Element.
        /// </summary>
        /// <returns></returns>
        public int Level()
        {
            // initialize the level value
            int level = 0;
            // check if the TEXML Parent Element is not null
            if (Parent != null)
            {
                // set the level value to the value of the TEXML Parent Element's Level at the level value
                level = Parent.Level(level);
            }
            // return the value of the "level" (how many nodes down this Element is from the Root)
            return (level);
        }

        /// <summary>
        /// Recursively calls each Parent and increments the level to find out how many "levels" down this TEXML Element is
        /// </summary>
        /// <param name="iLevel"></param>
        /// <returns></returns>
        private int Level(int iLevel)
        {
            // initializes the level as the passed in level + 1
            int level = iLevel + 1;
            // check the TEXML Parent Element is not null
            if (Parent != null)
            {
                // assign to the level value by Recursively call the TEXML Parent Element's Level method and pass in the new level value
                level = Parent.Level(level);
            }
            // returns the value of the number of nodes this TEXML Element is away from the Root
            return (level);
        }

        // Gets the 
        public XmlNamespaceManager NSManager
        {
            get
            {
                // initalizes the XmlNameSpaceManager
                XmlNamespaceManager nsm = null;
                // initializes a TEXML Element using this.TEXML Element
                TEXML xmlCurrent = this;
                do
                {
                    // check if the TEXML Element's NS manager is not null
                    if (xmlCurrent.m_NSmanager != null)
                    {
                        // set the initialized NSM to the value of this.TEXML Element's NS Manager
                        nsm = m_NSmanager;
                        // break if current TEXML Element has a NS Manager that isn't null
                        break;
                    }
                    // sets the TEXML Element to the value of it's Parent TEXML
                    xmlCurrent = xmlCurrent.Parent;
                    // continue to loop if the TEXML Element is not null or the TEXML Element has a null NS Manager
                } while (xmlCurrent != null);
                // return the created NS Manager
                return (nsm);
            }
        }

        /// <summary>
        /// Finds the Parent Element of the TEXML Element Node
        /// </summary>
        public TEXML Parent
        {
            get
            {
                // initialize a new TEXML Element
                TEXML trXML = null;
                // check if the this.TEXML Element property is not null
                if (Element != null)
                {
                    // create a new XmlNode from the TEXML Element property
                    XmlNode xNode = (XmlNode)Element;
                    // initializes a new XmlNode
                    XmlNode xPNode;
                    // initializes a new XmlElement
                    XmlElement xPElement = null;
                    do
                    {
                        // assigns the value of the XmlNode as the value of the XmlNode's ParentNode property
                        xPNode = xNode.ParentNode;
                        // check if the XmlNode is null
                        if (xPNode == null)
                        {
                            // break if null
                            break;
                        }
                        // check if the XmlNode is a XmlElement
                        if (xPNode is XmlElement)
                        {
                            // Assigns the XmlNode parent cast to a XmlElement as the value of the XmlElement. We found our parent.
                            xPElement = (XmlElement)xPNode;
                            // break if found parent
                            break;
                        }
                        // assign the value of the XmlNode parent to the XmlNode
                        xNode = xPNode;
                        // continue to loop until a break is hit.
                    } while (true);
                    // check if the XmlElement Parent is not null
                    if (xPElement != null)
                    {
                        // creates a new TEXML Element from the XmlElement parent and NSManager property
                        trXML = new TEXML(xPElement, m_NSmanager);
                    }
                }
                //  return the newly created TEXML Element
                return (trXML);
            }
        }

        /// <summary>
        /// Creates a new XmlElement child to append the Element property of the TEXML using the passed in Name and returns the TEXML element
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public TEXML AddChild(string Name)
        {
            try
            {
                // create a new character : to split on
                char[] splitOn = new char[1];
                splitOn[0] = ':';
                // splits the passed in string Name on the character : into parts
                string[] parts = Name.Split(splitOn);
                // intializes a new XmlElement
                XmlElement xEl = null;
                // check if the number of Parts is equal to 2
                if (parts.Length == 2)
                {
                    // if there are 2 parts, set the first part as the Prefix
                    string PreFix = parts[0];
                    // sets the second part as the local name
                    string localName = parts[1];
                    // create a url NS using the prefix/first part of the split
                    string urlNS = NSManager.LookupNamespace(PreFix);
                    // call CreateElement to create an element on our xml document using the prefix, local name, and url NS that we just created.
                    xEl = Doc.CreateElement(PreFix, localName, urlNS);
                }
                // if the parts are greater than or less than 2
                else
                {
                    // call CreateElement to create an element on our xml document using the Name parameter
                    xEl = Doc.CreateElement(Name);
                }
                // Add the created element to our document by appending it to the end
                Element.AppendChild(xEl);
                // Creates a new TEXML element using the created XmlElement and NSManager
                TEXML xml = new TEXML(xEl, m_NSmanager);
                // returns the new TEXML
                return (xml);
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.AddChild: " + se.Message);
                TELogger.Log(0,se);
            }
            // else returns null
            return (null);
        }

        /// <summary>
        /// Creates a deep copy or directly appends the passed in TEXML Element to this.TEXML Element
        /// </summary>
        /// <param name="xmlChild"></param>
        /// <returns></returns>
        public TEXML AddChild(TEXML xmlChild)
        {
            try
            {
                // initializes a XmlNode
                XmlNode node;
                // checks if the passed in TEXML Element's XmlDocument property is not null and that it differs from this.TEXML Element's XmlDocument property
                if (xmlChild.Doc != null && xmlChild.Doc != Doc)
                {
                    // creates a deep copy of the passed in TEXML Element's XmlElement property from this.TEXML's XmlDocument and assigns the value to the XmlNode
                    XmlNode newNode = Doc.ImportNode(xmlChild.m_Element, true);
                    // appends the created deep copy XmlNode as a child to this.TEXML Element and assigns this value to the initialized XmlNode
                    node = Element.AppendChild(newNode);
                }
                // if XmlDocument is null or does not differ from this.TEXML Element's XmlDocument property
                else
                {
                    // assigns the value of the passed in TEXML Element's XmlElement appended to this.TEXML Element to the XmlNode
                    node = Element.AppendChild(xmlChild.m_Element);
                }
                // cast the XmlNode to a XmlElement and assign the value to the XmlElement
                XmlElement xEl = (XmlElement)node;
                // creates a new TEXML Element from the XmlElement and a NS Manager
                TEXML xml = new TEXML(xEl, m_NSmanager);
                // returns the newly created TEXML Element
                return (xml);
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.AddChild: " + se.Message);
                TELogger.Log(0,se);
            }
            // returns null if an exception is thrown
            return (null);
        }

        /// <summary>
        /// Adds a TEXML Element child to this.TEXML Element by passing in a name and value for the child Element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TEXML AddChild(string name, object value)
        {
            try
            {
                // create a new TEXML Element given a string name
                TEXML xChild = new TEXML(name);
                // if the passed in value parameter is not null
                if(value != null)
                {
                    // Assign the TEXML Element the value of the passed in value parameter as a string
                    xChild.Value = value.ToString();
                }
                // Appends the new TEXML Element as a child of this.TEXML Element
                this.AddChild(xChild);
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.AddChild: " + se.Message);
                TELogger.Log(0,se);
            }
            // return null
            return (null);
        }

        /// <summary>
        /// Creates a new TEXML root where the first TEXML element node is added immediately after the second "reference" TEXML Element node
        /// </summary>
        /// <param name="xmlNewChild"></param>
        /// <param name="xmlRefChild"></param>
        /// <returns></returns>
        public TEXML InsertChildAfter(TEXML xmlNewChild, TEXML xmlRefChild)
        {
            try
            {
                // initialize a XmlNode
                XmlNode node;
                // check that the passed in TEXML parameter's XmlDocument is not null and is not equal to this.TEXML's XmlDocument.
                if (xmlNewChild.Doc != null && xmlNewChild.Doc != Doc)
                {
                    // create a XmlNode using the passed in TEXML's XmlElement to deep copy
                    XmlNode newNode = Doc.ImportNode(xmlNewChild.m_Element, true);
                    // assign the initialized XmlNode the value of the new XmlNode after the referenced TEXML parameter's XmlElement
                    node = Element.InsertAfter(newNode, xmlRefChild.Element);
                }
                // else if TEXML parameter's XmlDocument is null or does not differ from this.TEXML's XmlDocument
                else
                {
                    // assign the initialized XmlNode the value of the passed in TEXML Element's XmlElement after the referenced TEXML parameter's XmlElement
                    node = Element.InsertAfter(xmlNewChild.m_Element, xmlRefChild.Element);
                }
                // cast the XmlNode to a XmlElement
                XmlElement xEl = (XmlElement)node;
                // Create a new TEXML from the XmlElement and NS Manager
                TEXML xml = new TEXML(xEl, m_NSmanager);
                // return the newly created TEXML
                return (xml);
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.AddChild: " + se.Message);
                TELogger.Log(0,se);
            }
            // return null if exception is thrown
            return (null);
        }

        /// <summary>
        /// Removes the specified TEXML Element from this.TEXML Element
        /// </summary>
        /// <param name="xmlChild"></param>
        public void RemoveChild(TEXML xmlChild)
        {
            try
            {
                // Create a XmlNode from removing the specified child node
                XmlNode node = Element.RemoveChild(xmlChild.m_Element);
                // returns to caller
                return;
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.RemoveChild: " + se.Message);
                TELogger.Log(0,se);
            }
            // returns to caller after exception is caught
            return;
        }

        /// <summary>
        /// Removes the specified Attribute given by it's string Name parameter
        /// </summary>
        /// <param name="Name"></param>
        public void RemoveAttribute(string Name)
        {
            try
            {
                // Removes the specified Name parameter from the TEXML Element's Attribute collection.
                Element.Attributes.RemoveNamedItem(Name);
                // iterates through each of the siblings of this.TEXML First Child
                for (TEXML xml = FirstChild; !xml.IsNull; xml = xml.NextSibling)
                {
                    // Removes each child's Attribute that matches the specified Name
                    xml.RemoveAttribute(Name);
                }
                // returns to caller
                return;
            }
            catch (Exception se)
            {
                Console.WriteLine("EXCEPTION in TEXML.RemoveAttribute: " + se.Message);
                TELogger.Log(0,se);
            }
            // returns to caller if exception is caught
            return;
        }

        /// <summary>
        /// Removes all Children from this TEXML Element
        /// </summary>
        public void ClearChildren()
        {
            // iterates through each Child Element in this.TEXML
            foreach (var item in ChildElements)
            {
                try
                {
                    // Removes each TEXML Element in the list
                    RemoveChild(item);
                }
                catch (Exception ex)
                {
                    TELogger.Log(0, ex);
                    throw new InvalidOperationException("Clear children, Remove Child failed");

                }
            }
        }

        /// <summary>
        /// Gets the XmlElement from the XmlDocument, sets the XmlElement as the value
        /// </summary>
        public XmlElement Element
        {
            get
            {
                // checks if the XmlElement is null
                if (m_Element == null)
                {
                    // checks if the XmlDocument is not null
                    if (m_Doc != null)
                    {
                        // assigns the value of the XmlDocument's DocumentElement property to the XmlElement
                        m_Element = m_Doc.DocumentElement;
                    }
                }
                // returns the XmlElement
                return (m_Element);
            }
            set
            {
                // assigns the value to the XmlElement
                m_Element = value;
            }
        }

        /// <summary>
        /// Gets the XmlDocument from the XmlElement OwnerDocument or creates a new base XmlDocument and appends it to the Base XmlDocument. Sets the XmlDocument to the value
        /// </summary>
        public XmlDocument Doc
        {
            get
            {
                // if XmlDocument is null
                if (m_Doc == null)
                {
                    // and if XmlElement is not null
                    if (m_Element != null)
                    {
                        // and if XmlElement's OwnerDocument property is not null
                        if (m_Element.OwnerDocument != null)
                        {
                            // set the XmlDocument as the XmlElement's OwnerDocument property
                            m_Doc = m_Element.OwnerDocument;
                        }
                        // else if XmlElement's OwnerDocument property is null
                        else
                        {
                            // create a new XmlDocument
                            m_Doc = new XmlDocument();
                            //Create a XmlDocument with the specified default value and assign that to the XmlNode
                            XmlNode docNode = m_Doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                            // Append the XmlNode to the XmlDocument as a child
                            m_Doc.AppendChild(docNode);

                        }
                    }
                    // else if XmlElement is null
                    else
                    {
                        // Create a new XmlDocuments
                        m_Doc = new XmlDocument();
                        XmlNode docNode = m_Doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        //Create a XmlDocument with the specified default value and assign that to the XmlNode
                        m_Doc.AppendChild(docNode);
                        // Append the XmlNode to the XmlDocument as a child
                    }
                }
                // returns the XmlDocument
                return (m_Doc);
            }
            set
            {
                // sets the XmlDocument to the value
                m_Doc = value;
            }
        }

        /// <summary>
        /// Loads a XmlDocument from the passed in FileName
        /// </summary>
        /// <param name="FileName"></param>
        public void Load(string FileName)
        {
            // check if FileName File exists
            if (File.Exists(FileName))
            {
                // Loads XmlDocument from FileName string File
                Doc.Load(FileName);
                // assigns the XmlElement the value of the XmlDocument's DocumentElement property
                m_Element = m_Doc.DocumentElement;
                // Creates a new NS Manager from the XmlDocument NameTable property
                m_NSmanager = new XmlNamespaceManager(m_Doc.NameTable);
                // adds the TR Namespace to the NS Managers collection
                m_NSmanager.AddNamespace("tr", "http://schema.traceregister.com");

            }
        }

        /// <summary>
        /// Loads a XmlDocument from the passed in Stream
        /// </summary>
        /// <param name="stream"></param>
        public void Load(Stream stream)
        {
            // Loads XmlDocument from the passed in Stream
            Doc.Load(stream);
            // assigns the XmlElement the value of the XmlDocument's DocumentElement property
            m_Element = m_Doc.DocumentElement;
            // Creates a new NS Manager from the XmlDocument NameTable property
            m_NSmanager = new XmlNamespaceManager(m_Doc.NameTable);
            // adds the TR Namespace to the NS Managers collection
            m_NSmanager.AddNamespace("tr", "http://schema.traceregister.com");
        }

        /// <summary>
        /// Loads a XmlDocument from a byte[] by creating a stream and passing in the byte[]
        /// </summary>
        /// <param name="array"></param>
        public void Load(byte[] array)
        {
            // creates a Stream from the byte[] passed in
            using (MemoryStream stream = new MemoryStream(array))
            {
                //calls the Load method that takes a stream to Load a XmlDocument
                Load(stream);
            }
        }

        /// <summary>
        /// Loads a TEXML Element from a FileName
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static TEXML LoadFromFile(string fileName)
        {
            // check if the FileName File exists
            if (File.Exists(fileName))
            {
                // creates a new TEXML Element
                var doc = new TEXML();
                // Loads the XmlElement from the FileName
                doc.Load(fileName);
                // returns the loaded XmlDocument as a XmlElement
                return doc;
            }
            // if FileName does not exists, returns null
            return null;
        }

        /// <summary>
        /// Loads a TEXML Element from a passed in string value
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static TEXML CreateFromString(string xmlString)
        {
            // creates a new TEXML Element
            var doc = new TEXML();
            // Loads the passed in xmlString into the XmlDocument
            doc.LoadFromString(xmlString);
            // returns the XmlDocument as a XmlElement
            return doc;
        }

        /// <summary>
        /// Creates a TEXML Element from a passed in string value
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static TEXML Parse(string xmlString)
        {
            try
            {
                // loads up a XmlDocument from the xmlString passed in and returns it as a TEXML Element
                return TEXML.CreateFromString(xmlString);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a XmlSchema by reading the passed in FileStream. Creates reader settings from the Schema and Reads through the XmlReader. If no exceptions are thrown validation is returned as true.
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ValidateSchema(FileStream fs, out string error)
        {
            //XmlReader reader = null;
            //string exceptionMsg = null;
            //bool bFileOk = false;
            //TextReader stringReader = null;
            try
            {
                // returns whether the Schema was validated or not when reading the FileStream parameter and reading the Schema with a XmlReader 
                return ValidateSchema((Stream)fs, out error);
                //XmlReaderSettings settings = new XmlReaderSettings();
                //XmlSchemaSet sc = new XmlSchemaSet();
                //settings.ValidationType = ValidationType.None;
                //settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                //settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
                //settings.ValidationType = ValidationType.Schema;
                //XmlSchema schema = XmlSchema.Read(fs, ValidationHandler);
                //sc.Add(schema);
                //settings.Schemas = sc;
                //stringReader = new StringReader(XmlString);
                //reader = XmlReader.Create(stringReader, settings);
                //bool bOk = true;
                //do
                //{
                //	bOk = reader.Read();
                //} while (bOk);
                //reader.Close();
                //reader.Dispose();
                //reader = null;
                //bFileOk = true;
            }
            //catch (OperationCanceledException)
            //{
            //	throw;
            //}
            //catch (FileNotFoundException NotFoundException)
            //{
            //	TELogger.Log(0, NotFoundException);
            //}
            //catch (TRXMLFileSchemaValidationFailed InvalidSchema)
            //{
            //	exceptionMsg = InvalidSchema.Message;
            //	TELogger.Log(1, InvalidSchema);
            //}
            catch (Exception ex)
            {
                error = ex.Message;
                TELogger.Log(0, ex);
                return false;
            }
            //finally
            //{
            //	if (reader != null)
            //	{
            //		reader.Close();
            //		reader.Dispose();
            //		reader = null;
            //	}
            //	if (stringReader != null)
            //	{
            //		stringReader.Close();
            //		stringReader.Dispose();
            //		stringReader = null;
            //	}
            //}

            //return (bFileOk);
        }

        /// <summary>
        /// Returns whether a Schema was validated or not by creating a FileStream from the passed in string SchemaFileName
        /// </summary>
        /// <param name="schemaFileName"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ValidateSchema(string schemaFileName, out string error)
        {
            // initialize the string parameter as a null value
            error = null;
            try
            {
                // create a FileStream from the passed in string SchemaFileName with mode Open and Access Read only
                FileStream fs = new FileStream(schemaFileName, FileMode.Open, FileAccess.Read);
                // returns whether the schema could be validated or not. 
                return ValidateSchema(fs, out error);
            }
            catch (Exception ex)
            {
                TELogger.Log(0, ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a Stream from a TEXML Element and the validates that the schema was read from beginning to end without exceptions being thrown.
        /// </summary>
        /// <param name="xSchema"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ValidateSchema(TEXML xSchema, out string error)
        {
            // initializes the string as null
            error = null;
            try
            {
                // Creates a new stream from the TEXML Element passed in as a parameter. Saves the TEXML Element to the Stream.
                Stream stream = xSchema.SaveToStream();
                // returns whether the schema could be validated or not. 
                return ValidateSchema(stream, out error);
            }
            catch (Exception ex)
            {
                TELogger.Log(0, ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ValidateSchema(Stream stream, out string error)
        {
            // initializes a XmlReader
            XmlReader reader = null;
            // sets the string value of the passed in parameter called error to null
            error = null;
            // initializes a bool to false
            bool bFileOk = false;
            // initializes a TextReader object
            TextReader stringReader = null;
            try
            {
                // Creates a new XmlReaderSettings object
                XmlReaderSettings settings = new XmlReaderSettings();
                // Creates a new XmlSchemaSet object
                XmlSchemaSet sc = new XmlSchemaSet();
                // Sets the settings of the XmlReaderSettings objects to ValidationType None, ValidationFlags ReportValidationWarnings, ValidationEventHandler subscribes to the ValidationCallback 
                settings.ValidationType = ValidationType.None;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
                // and ValidationType then to Schema
                settings.ValidationType = ValidationType.Schema;
                // creates a XmlSchema from reading the passed in Stream and using the Validation Event Handler which callsback to the ValidationCallBack method subscription
                XmlSchema schema = XmlSchema.Read(stream, ValidationHandler);
                // adds this XmlSchema to our XmlSchemaSet
                sc.Add(schema);
                // assigns the value of XmlSchemaSet to our XmlReaderSettings Schemas property
                settings.Schemas = sc;
                // Creates a new StringReader from this.TEXML's OuterElement string property
                stringReader = new StringReader(XmlString);
                // Creates a XmlReader instance using the StringReader instance and our XmlReaderSettings
                reader = XmlReader.Create(stringReader, settings);
                // sets bool to true
                bool bOk = true;
                // Reads through the XmlReader instance until no nodes are left to read
                do
                {
                    bOk = reader.Read();
                } while (bOk);
                // Closes the XmlReader instance
                reader.Close();
                // Disposes the XmlReader instance
                reader.Dispose();
                // sets XmlReader to null
                reader = null;
                // Validation complete and bool set to true
                bFileOk = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FileNotFoundException NotFoundException)
            {
                TELogger.Log(0, NotFoundException);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                TELogger.Log(0, ex);
            }
            finally
            {
                // checks if XmlReader instance is not null and then closes, disposes and sets it to null
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
                // checks if StringReader instance is not null and then closes, disposes and sets it to null
                if (stringReader != null)
                {
                    stringReader.Close();
                    stringReader.Dispose();
                    stringReader = null;
                }
            }
            // returns whether the Validation was a sucess or not
            return (bFileOk);
        }

        /// <summary>
        /// ValidationEventHandler for subscribing callbacks to and calling 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ValidationHandler(object sender, ValidationEventArgs args)
        {

        }

        /// <summary>
        /// The ValidationCallBack is the method that is fired when ValidationHandler is called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            // checks if the ValidationEventArgs has a Severity property of Warning and Logs No Validation occurred
            if (args.Severity == XmlSeverityType.Warning)
            {
                TELogger.Log(0, "Warning: Matching schema not found.  No validation occurred." + args.Message);
                throw new FileNotFoundException("Warning: Matching schema not found.  No validation occurred." + args.Message);
            }
            // else Logs  Validation error
            else
            {
                TELogger.Log(1, "\tValidation error: " + args.Message);
                throw new Exception("Validation error: " + args.Message);
            }
        }

        /// <summary>
        /// Saves the XmlDocument to the specified FileName passed in as the parameter
        /// </summary>
        /// <param name="FileName"></param>
        public void Save(string FileName)
        {
            Doc.Save(FileName);
        }

        /// <summary>
        /// Creates a new Memory Stream, saves the XmlDocument to it and returns it
        /// </summary>
        /// <returns></returns>
        public Stream SaveToStream()
        {
            // Creates a new Memory stream
            MemoryStream memStream = new MemoryStream();
            // Saves the XmlDocument to the Memory Stream
            Doc.Save(memStream);
            // Sets the cursor at the beginning of the Memory Stream
            memStream.Seek(0, SeekOrigin.Begin);
            // Returns the MemoryStream
            return (memStream);
        }

      /// <summary>
      /// Creates a XmlDocument and loads the Xml into it from the passed in string.
      /// </summary>
      /// <param name="xmlStr"></param>
        public void LoadFromString(string xmlStr)
        {
            try
            {
                // checks that the passed in string parameter is null or empty
                if (string.IsNullOrEmpty(xmlStr))
                {
                    // returns immediately if null or empty
                    return;
                }
                // loads the XmlDocuments XML using the StringReader. The StringReader reads the string passed in.
                Doc.Load(new StringReader(xmlStr));                
                // assign the value of the XmlDocument's DocumentElement property to the XmlElement
                m_Element = m_Doc.DocumentElement;
                // creates a new XmlNameSpaceManager from the XmlDocuments NameTable property
                m_NSmanager = new XmlNamespaceManager(m_Doc.NameTable);
                // adds the TR Namespace to the XmlNSManager
                m_NSmanager.AddNamespace("tr", "http://schema.traceregister.com");
            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
                TELogger.Log(0, xmlStr);
                throw;
            }
        }

        /// <summary>
        /// Gets the OuterXML value of a TEXML Element and sets the value by loading a string from a XmlDocument
        /// </summary>
        [DataMember]
        public string XmlString
        {
            get
            {
                // gets the TEXML Element's OuterXML XmlNode type property value
                if (Element != null)
                {
                    return (Element.OuterXml);
                }
                else
                {
                    return (null);
                }
            }
            set
            {
                // loads the XML from the specified value into a XmlDocument
                LoadFromString(value);
            }
        }


        /// <summary>
        /// Prints out the XML as a formatted string for display.
        /// </summary>
        public string PrintXmlString
        {
            get
            {
                string result = "";

                MemoryStream mStream = new MemoryStream();
                XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
                XmlDocument document = new XmlDocument();

                try
                {
                    // Load the XmlDocument with the XML.
                    document.LoadXml(this.XmlString);

                    writer.Formatting = Formatting.Indented;

                    // Write the XML into a formatting XmlTextWriter
                    document.WriteContentTo(writer);
                    writer.Flush();
                    mStream.Flush();

                    // Have to rewind the MemoryStream in order to read
                    // its contents.
                    mStream.Position = 0;

                    // Read MemoryStream contents into a StreamReader.
                    StreamReader sReader = new StreamReader(mStream);

                    // Extract the text from the StreamReader.
                    string formattedXml = sReader.ReadToEnd();

                    result = formattedXml;
                }
                catch (XmlException)
                {
                    // Handle the exception
                }

                mStream.Close();
                writer.Close();

                return result;
            }
        }

        /// <summary>
        /// Gets the default value for the NS
        /// </summary>
        public string DefaultNS
        {
            get
            {
                //check if NS Manager is not null
                if (NSManager != null)
                {
                    // returns ns: if not null
                    return ("ns:");
                }
                // if NS Manager is null
                else
                {
                    // returns an empty string
                    return ("");
                }
            }
        }

        // Gets the Name of this.TEXML Element's Name Property
        public string Name
        {
            get
            {
                //check if TEXML Element is not null
                if (Element != null)
                {
                    // return the Name property of XmlElement
                    return (Element.Name);
                }
                // else if TEXML is null
                else
                {
                    // returns the string "null"
                    return ("null");
                }
            }
        }

        /// <summary>
        /// Gets the first occurrence of a XmlNode with NodeType Text in this.TEXML Element and it's siblings
        /// </summary>
        public XmlNode TextNode
        {
            get
            {
                // initialize the XmlNode
                XmlNode textnode = null;
                // check if this.TEXML is not null
                if (!IsNull)
                {
                    // assign the value of this.TEXML's First Child property to XmlNode
                    XmlNode xCurNode = Element.FirstChild;
                    // iterate while XmlNode is not null
                    while (xCurNode != null)
                    {
                        // check if XmlNode's NodeType is Text
                        if (xCurNode.NodeType == XmlNodeType.Text)
                        {
                            // assign the set XmlNode value to the initalized XmlNode
                            textnode = xCurNode;
                            // break we found our TextNode
                            break;
                        }
                        // Check the next XmlNode sibling
                        xCurNode = xCurNode.NextSibling;
                    };
                }
                // return the XmlNode with NodeType Text, or a null XmlNode if none exists
                return (textnode);
            }
        }

        /// <summary>
        /// Returns the value of a XmlElement or XmlAttributes Value property from the specified string XPath passed in
        /// </summary>
        /// <param name="XPath"></param>
        /// <returns></returns>
        public string ValueFromXPath(string XPath)
        {
            // initialize the string to empty
            string value = "";
            // check if the passed in XPath string is null or empty
            if (string.IsNullOrEmpty(XPath))
            {
                // returns an empty string 
                return ("");
            }
            // check if this.TEXML Element is null
            if (Element == null)
            {
                // returns an empty string
                return ("");
            }
            // check if the passed in XPath string starts with the character '@'
            if (XPath.StartsWith("@"))
            {
                // creates a new string removing all '@' characters 
                string attributeName = XPath.Replace("@", "");
                // returns the value of this TEXML Element whose Attribute Name property matches the new string created from the XPath removal of the '@' character
                return (this.Attribute(attributeName));
            }
            // get the XmlNode that matches the passed in string parameter XPath
            XmlNode node = Element.SelectSingleNode(XPath);
            // check if the XmlNode is null
            if (node == null)
            {
                // returns an empty string if it is null
                return ("");
            }
            // check if the XmlNode is a XmlAttribute
            if (node is XmlAttribute)
            {
                // casts the XmlNode to a XmlAttribute
                XmlAttribute attrb = (XmlAttribute)node;
                // sets the initialized string as the value of the XmlAttribute's Value property
                value = attrb.Value;
            }
            // else check if the XmlNode is a XmlElement
            else if (node is XmlElement)
            {
                // creates a new TEXML Element from the XmlNode and a NS Manager
                TEXML xmlNode = new TEXML(node, m_NSmanager);
                // Finds the first occurrence of a XmlNode with NodeType TextNode, if none occurrs returns a null value
                XmlNode textNode = xmlNode.TextNode;
                // checks if the XmlNode is not null
                if (textNode != null)
                {
                    // sets the initialized string as the value of the XmlNode's Value property
                    value = textNode.Value;
                }
            }
            // returns the initialized strings new value
            return (value);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="XPath"></param>
        /// <param name="Value"></param>
        public void SetValueFromXPath(string XPath, string Value)
        {
            // check if the passed in XPath string is null or empty
            if (string.IsNullOrEmpty(XPath))
            {
                // returns immediately if null or empty
                return;
            }
            // check if this.TEXML Element is null
            if (Element == null)
            {
                // returns immediately if null
                return;
            }
            // Finds the first TEXML Element that matches the passed in XPath string
            XmlNode node = Element.SelectSingleNode(XPath);
            // check if the XmlNode is a XmlAttribute
            if (node is XmlAttribute)
            {
                // casts the XmlNode to a XmlAttribute
                XmlAttribute attrb = (XmlAttribute)node;
                // sets the passed in string Value as the value of the XmlAttribute's Value property
                attrb.Value = Value;
            }
            // else checks if the XmlNode is a XmlElement
            else if (node is XmlElement)
            {
                // Creates a new TEXML Element from the XmlNode and a NS Manager
                TEXML xmlNode = new TEXML(node, m_NSmanager);
                // Finds the first occurrence of a XmlNode with NodeType TextNode, if none occurrs returns a null value
                XmlNode textNode = xmlNode.TextNode;
                // checks if the XmlNode is not null
                if (textNode != null)
                {
                    // sets the passed in string Value as the value of the XmlNode's Value property
                    textNode.Value = Value;
                }
            }
            // returns to the caller
            return;
        }


        /// <summary>
        /// Gets the Value of a TEXML Element as a XmlNode by checking the XmlNode Value property. Returns an empty string if the XmlNode Text NodeType is null or null if the TEXML Element is null.
        /// Sets the strVal to the value being assigned to. Checks that this value has no special characters and replaces them if it does. If the value is null or empty an empty string is set.
        ///         The XmlNode.Value property is then checked and if not null, then it is set as the strVal. If it is null, then the InnerXML is set as the strVal.
        /// </summary>
        public string Value
        {
            get
            {
                // checks if this.TEXML Element is not null
                if (Element != null)
                {
                    // casts this TEXML Element to a XmlNode
                    XmlNode xNode = (XmlNode)Element;
                    // gets the first occurrence of a Text NodeType Node in TEXML
                    XmlNode text = TextNode;
                    // checks if the XmlNode is not null
                    if (text != null)
                    {
                        // returns the Text Node XmlNode's Value with characters &amp; , &lt; , &gt; , &quot; , &apos; replaced with the shorthand symbol versions
                        string strValue = text.Value.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");
                        strValue = strValue.Trim();
                        strValue = strValue.RemoveControlCharacters();
                        return (strValue);
                    }
                    // else if the XmlNode is null
                    else
                    {
                        // returns an empty string
                        return ("");
                    }
                    /*
                    if (xNode != null)
                    {
                        if (xNode.Value != null)
                        {
                            return (xNode.Value);
                        }
                        else
                        {
                            return (xNode.InnerXml);
                        }
                    }
                    else
                    {
                        return (null);
                    }
					 */
                }
                // else if this.TEXML Element is null 
                else
                {
                    // return null value string
                    return (null);
                }
            }
            set
            {
                // check if this.TEXML Element is not null
                if (Element != null)
                {
                    // casts the TEXML Element to a XmlNode
                    XmlNode xNode = (XmlNode)Element;
                    // check if XmlNode is not null
                    if (xNode != null)
                    {
                        // sets the string strVal to the value
                        string strVal = value;
                        // checks if the strVal is not null or empty
                        if (!string.IsNullOrEmpty(strVal))
                        {
                            // returns the strVal with characters &amp; , &lt; , &gt; , &quot; , &apos; replaced with the shorthand symbol versions
                            strVal = strVal.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
                        }
                        // else if strVal is null or empty
                        else
                        {
                            // sets the strVal to an empty string
                            strVal = "";
                        }
                        // checks if the XmlNode's Value property is not null
                        if (xNode.Value != null)
                        {
                            // sets the XmlNode's Value property to the value of the new strVal
                            xNode.Value = strVal;
                        }
                        // else if the XmlNode Value property is null
                        else
                        {
                            // sets the XmlNode's InnerXml property to the value of the new strVal
                            xNode.InnerXml = strVal;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a Nullable DateTime from this.TEXML Element given a format, or default to format "J"
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public DateTime? GetValueDateTime(string format = "J")
        {
            // initializes a nullable DateTime
            DateTime? dt = null;
            // sets the strValue as this.TEXML Element's Value property
            string strValue = Value;
            // check if the strValue is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    // check if the passed in format string is "J" for JSON
                    if (format == "J")
                    {
                        // uses the TEDataConvert static ParseJSONDate to return a nullable DateTime from a strValue
                        dt = TEDataConvert.ParseJSONDate(strValue);
                    }
                    // else if the passed in format is something other than "J"
                    else
                    {
                        // create a new nullable DateTime from the strVal representation using the format and InvariantCulture
                        dt = DateTime.ParseExact(strValue, format, CultureInfo.InvariantCulture);
                    }
                }
                catch
                {

                }
                // check if the Nullable DateTime is not null
                if (dt == null)
                {
                    // use the TEDataConvert to convert the strValue to a Nullable DateTime
                    dt = TEDataConvert.TRParseDateTime(strValue);
                }
            }
            // returns the Nullable DateTime
            return (dt);
        }

        /// <summary>
        /// Sets this.TEXML Element's Value property to the passed in Nullable DateTime formatted as a string with the specified format. "J" or JSON format if not specified.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="format"></param>
        public void SetValueDateTime(DateTime? dt, string format = "J")
        {
            // set an empty string
            string strValue = "";
            // check if the passed in Nullable DateTime has value
            if (dt.HasValue)
            {
                // check if the format is "J" for JSON
                if (format == "J")
                {
                    // set the strValue as the value of the Nullable DateTime's Value ToString with the specified JSON format
                    strValue = dt.Value.ToString("yyyy-MM-ddThh:mm:sszzz");
                }
                // else if the format is not JSON
                else
                {
                    // set the strValue as the value of the Nullable DateTime's Value to string with the specified format
                    strValue = dt.Value.ToString(format);
                }
            }
            // set this.TEXML Element's Value property to the strValue string created
            Value = strValue;
        }

        /// <summary>
        /// Sets this.TEXML Element's Value property to the passed in DateTime formatted as a string with the specified format. "J" or JSON format if not specified.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="format"></param>
        public void SetValueDateTime(DateTime dt, string format = "J")
        {
            // creates an empty string
            string strValue = "";
            // check if the format is "J" or JSON
            if (format == "J")
            {
                // set the strValue as the value of the DateTime's Value ToString with the specified JSON format
                strValue = dt.ToString("yyyy-MM-ddThh:mm:sszzz");
            }
            // else if the format is not JSON
            else
            {
                // set the strValue as the value of the DateTime's Value to string with the specified format
                strValue = dt.ToString(format);
            }
            // set this.TEXML Element's Value property to the strValue string created
            Value = strValue;
        }

        /// <summary>
        /// Gets the value of this.TEXML Element's Value property as a nullable double
        /// </summary>
        /// <returns></returns>
        public Double? GetValueDouble()
        {
            // initialize a nullable double
            Double? val = null;
            // set the strValue to this.TEXML Element's Value property
            string strValue = Value;
            // check if the strValue is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    // Converts the strValue to a Double and assigns the value to our nullable double
                    val = System.Convert.ToDouble(strValue);
                }
                catch
                {

                }
            }
            // returns the nullable double
            return (val);
        }

        /// <summary>
        /// Sets the value of this.TEXML Element's Value property as a nullable double using the specified format. "g" is the format used if none is specified.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="format"></param>
        public void SetValueDouble(double? val, string format = "g")
        {
            // sets the strValue to an empty string
            string strValue = "";
            // checks if the passed in nullable double Has Value
            if (val.HasValue)
            {
                // sets the strValue to the nullable doubles Value property to string with the specified format
                strValue = val.Value.ToString(format);
            }
            // returns the strValue created
            Value = strValue;
        }

        /// <summary>
        /// Sets the value of this.TEXML Element's Value property as a double using the specified format. "g" is the format used if none is specified.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="format"></param>
        public void SetValueDouble(double val, string format = "g")
        {
            // sets the strValue to an empty string
            string strValue = "";
            // sets the strValue to the double to string with the specified format
            strValue = val.ToString(format);
            // returns the strValue created
            Value = strValue;
        }

        /// <summary>
        /// Gets the value of this.TEXML Element's Value property as a nullable Int32
        /// </summary>
        /// <returns></returns>
        public Int32? GetValueInt32()
        {
            // initializes a nullable Int32
            Int32? val = null;
            // creates a new string strValue from this.TEXML Element's Value property
            string strValue = Value;
            // check if the strValue is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    // Converts the strValue to an Int32 and assigns the value to our nullable Int32
                    val = System.Convert.ToInt32(strValue);
                }
                catch
                {

                }
            }
            // returns the nullable Int32
            return (val);
        }

        /// <summary>
        ///  Sets the value of this.TEXML Element's Value property as a nullable Int32.
        /// </summary>
        /// <param name="val"></param>
        public void SetValueInt32(Int32? val)
        {
            // creates an empty string 
            string strValue = "";
            // check if the passed in nullable Int32 Has a Value
            if (val.HasValue)
            {
                // sets the strValue to the nullable Int32's Value property to string with the specified format
                strValue = val.Value.ToString();
            }
            // returns the strValue created
            Value = strValue;
        }

        /// <summary>
        ///  Sets the value of this.TEXML Element's Value property as an Int32.
        /// </summary>
        /// <param name="val"></param>
        public void SetValueInt32(Int32 val)
        {
            // creates an empty string
            string strValue = "";
            // sets the strValue to the Int32 to string 
            strValue = val.ToString();
            // returns the strValue created
            Value = strValue;
        }

        /// <summary>
        /// Gets the value of this.TEXML Element's Value property as a nullable Int64
        /// </summary>
        /// <returns></returns>
        public Int64? GetValueInt64()
        {
            // initializes the nullable Int64
            Int64? val = null;
            // creates a string from this.TEXML Element's Value property
            string strValue = Value;
            // checks if the strValue created is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    // Converts the strValue to an Int64 and assigns the value to our nullable Int32
                    val = System.Convert.ToInt64(strValue);
                }
                catch
                {

                }
            }
            // returns the nullable Int64
            return (val);
        }

        /// <summary>
        /// Sets the value of this.TEXML Element's Value property as a nullable Int64.
        /// </summary>
        /// <param name="val"></param>
        public void SetValueInt64(Int64? val)
        {
            // create an empty string
            string strValue = "";
            // check if the passed in nullable Int64 has a Value
            if (val.HasValue)
            {
                // sets the strValue to the passed in nullable Int64's Value to string 
                strValue = val.Value.ToString();
            }
            // returns the strValue created
            Value = strValue;
        }

        /// <summary>
        /// Sets the value of this.TEXML Element's Value property as a Int64.
        /// </summary>
        /// <param name="val"></param>
        public void SetValueInt64(Int64 val)
        {
            // create an empty string
            string strValue = "";
            // sets the strValue to the passed in Int64 to string 
            strValue = val.ToString();
            // returns the strValue created
            Value = strValue;
        }


        /// <summary>
        /// Returns true if TEXML Element's Value property is not null or empty and false if it is null or empty
        /// </summary>
        /// <returns></returns>
        public bool GetValueBool()
        {
            // initializes a bool
            bool val = false;
            // creates a string from this.TEXML Element's Value property
            string strValue = Value;
            // check if the string is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            {
                // check if the string To Upper case is "TRUE"
                if (strValue.ToUpper() == "TRUE")
                {
                    // return bool true
                    val = true;
                }
            }
            // if string is null or empty return bool false
            return (val);
        }

        /// <summary>
        /// Sets this.TEXML Element's Value property to the text string representation of boolean values true and false
        /// </summary>
        /// <param name="val"></param>
        public void SetValueBool(bool val)
        {
            // check if the passed in bool is true
            if (val)
            {
                // set this.TEXML Element's Value property to the text representation of true
                Value = "true";
            }
            // else the passed in bool is false
            else
            {
                // set this.TEXML Element's Value property to the text representation of false
                Value = "false";
            }
        }




        /// <summary>
        /// Gets an EnumType object from this TEXML Element's Value property
        /// </summary>
        /// <typeparam name="EnumType"></typeparam>
        /// <returns></returns>
        public EnumType GetValueEnum<EnumType>()
        {
            // initiaze an EnumType with the default value
            EnumType EnumVal = default(EnumType);
            // create a string from this.TEXML Element's Value property
            string strValue = Value;
            // check if the string is not null or empty
            if (!string.IsNullOrEmpty(strValue))
            { 
                // Parses the string into an EnumType object
                object objVal = (EnumType)Enum.Parse(typeof(EnumType), strValue);
                // check that the returned object is not null
                if (objVal != null)
                {
                    // cast the returned object to an EnumType
                    EnumVal = (EnumType)objVal;
                }
            }
            //return the EnumType object
            return (EnumVal);
        }

        /// <summary>
        /// Sets this.TEXML Element's Value property to the name of the EnumType that has the specified value 
        /// </summary>
        /// <typeparam name="EnumType"></typeparam>
        /// <param name="value"></param>
        public void SetValueEnum<EnumType>(EnumType value)
        {
            // Sets this TEXML Element's Value property to the name of the EnumType that has the specified value
            Value = Enum.GetName(value.GetType(), value);
        }

        /// <summary>
        /// A Catch All that checks the object passed in with each type of conversion offered from a type to TEXML Element's Value property
        /// </summary>
        /// <param name="val"></param>
        public void SetValue(object val)
        {
            // check if the passed in object is null
            if(val == null)
            {
                // set this.TEXML Element's Value property to an empty string
                Value = "";
            }
            // check if the value is a Nullable DateTime
            if(val is Nullable<DateTime>)
            {
                // creates a nullable DateTime
                DateTime? dtN = (DateTime?)val;
                // sets the TEXML Element's Value property to the string representation of the passed in nullable DateTime
                SetValueDateTime(dtN);
            }
            // else check if the value is a DateTime
            else if (val is DateTime)
            {
                DateTime dtN = (DateTime)val;
                // sets the TEXML Element's Value property to the string representation of the passed in DateTime
                SetValueDateTime(dtN);
            }
            // else check if the value is a Int32
            else if (val is Int32)
            {
                // sets the TEXML Element's Value property to the string representation of the passed in Int32
                SetValueInt32((Int32)val);
            }
            // else check if the value is a Nullable Int32
            else if (val is Nullable<Int32>)
            {
                // sets the TEXML Element's Value property to the string representation of the passed in nullable Int32
                SetValueInt32((Int32 ?)val);
            }
            // else check if the value is a Int64
            else if (val is Int64)
            {
                // sets the TEXML Element's Value property to the string representation of the passed in Int64
                SetValueInt64((Int64)val);
            }
            // else check if the value is a nullable Int64
            else if (val is Nullable<Int64>)
            {
                // sets the TEXML Element's Value property to the string representation of the passed in nullable Int64
                SetValueInt64((Int64 ?)val);
            }
            // else check if the value is a boolean
            else if (val is bool)
            {
                // sets the TEXML Element's Value property to the string representation of the passed in boolean
                SetValueBool((bool)val);
            }
            // else check if the value is some object
            else
            {
                // sets the TEXML Element's Value property to the string representation of the passed in object
                Value = val.ToString();
            }
        }

        /// <summary>
        /// Gets a list of strings from the "ListValues" name from this TEXML Element and all siblings with the "ListValues" name.
        /// Set the list of strings by first removing it to avoid any duplications. 
        /// Then we check if the value we are setting is not null and create TEXML and add to it the list of strings with name "Val" and Value of each string in the list.
        /// </summary>
        public List<string> StringList
        {
            get
            {
                // create a list of strings
                List<string> myList = new List<string>();
                // iterate through each child in this.TEXML's "ListValues" element/attribute and as long as it is not null, get the next sibling
                for (TEXML xml = this["ListValues"].FirstChild; !xml.IsNull; xml = xml.NextSibling)
                {
                    // add to the list of strings this TEXML Element's Value property
                    myList.Add(xml.Value);
                }
                // return the list of strings
                return (myList);
            }
            set
            {
                // initializes a TEXML Element from this.TEXML Element with the name "ListValues"
                TEXML xmlListvalues = this["ListValues"];
                // check if the TEXML is not null
                if (!xmlListvalues.IsNull)
                    // removes the specified TEXML "ListValues" named child from this.TEXML Element
                    RemoveChild(xmlListvalues);
                // Adds the child with the specified name to this. TEXML Element and assigns the value to xmlListvalues
                xmlListvalues = AddChild("ListValues");
                // initializes a TEXML Element
                TEXML xml;
                // check if the value we are attempting to set it to is not null
                if (value != null)
                {
                    // foreach str in the list of strings
                    foreach (string str in value)
                    {
                        // The initialized xml is assigned the value of the newly created TEXML named "ListValues" with a Child added called "Val"
                        xml = xmlListvalues.AddChild("Val");
                        // the initialized TEXML called xml's Value property is set to the string in the value list of strings
                        xml.Value = str;
                    }
                }
            }
        }


        public List<Double?> DoubleList
        {
            get
            {
                // create a new List of nullable Doubles
                List<Double?> myList = new List<Double?>();
                // create a string from this.TEXML Element's Value property
                string strVal = Value;
                // create a string array from splitting the string on character ','
                string[] tokens = strVal.Split(',');
                // iterate through each string in the string array
                foreach (string token in tokens)
                {
                    // check if the string is null or empty
                    if (string.IsNullOrEmpty(token))
                    {
                        // add to the list of nullable doubles a null value
                        myList.Add(null);
                    }
                    // else check if the string is not null or empty
                    else
                    {
                        try
                        {
                            // Convert the string to a Double and assign the value to a nullable double
                            double? dbl = System.Convert.ToDouble(token);
                            // add the nullable double to the list of nullable doubles
                            myList.Add(dbl);
                        }
                        catch
                        {
                            // catch a nullable slipping through and add it as a null to the list of nullable doubles.
                            myList.Add(null);
                        }
                    }
                }
                // return a list of nullable doubles
                return (myList);
            }
        }


        public void SetDataList(List<Object> lst, string Delimiter = ",", string UOM = null)
        {
            // check if the passed in list of Object is null or the count is 0
            if (lst == null || lst.Count == 0)
            {
                // creates this.TEXML Element Attribute to the name "Count" and value 0
                this.Attribute("Count", 0);
                // set this.TEXML Element's Value property to an empty string
                this.Value = "";
                // returns to the caller
                return;
            }
            // initializes a string
            string DataType = null;
            // iterates through each Object in the passed in List of Object
            foreach (Object obj in lst)
            {
                // check if object is not null
                if (obj != null)
                {
                    // check if the object is a string
                    if (obj is string)
                    {
                        // set the DataType to the name String
                        DataType = "String";
                    }
                    // check if the object is a nullable Double
                    else if (obj is Double?)
                    {
                        // set the DataType to the name Double
                        DataType = "Double";
                    }
                    // check if the object is a nullable Int32
                    else if (obj is Int32?)
                    {
                        // set the DataType to the name Integer
                        DataType = "Integer";
                    }
                    // check if the object is a nullable DateTime
                    else if (obj is DateTime?)
                    {
                        // set the DataType to the name DateTime
                        DataType = "DateTime";
                    }
                    // check if the object is a boolean
                    else if (obj is bool)
                    {
                        // set the DataType to the name bool
                        DataType = "bool";
                    }
                    break;
                }
            }
            // check if the string is null
            if (DataType == null)
            {
                throw new Exception("Invalid Datatype passed SetDataList");
            }
            // switch on the name of the string
            switch (DataType)
            {
                // check that the name is String
                case "String":
                    {
                        // create a new list of string
                        List<string> strLst = new List<string>();
                        // iterate through each Object in the List of Object
                        foreach (Object obj in lst)
                        {
                            // Add to the string list the object cast to a string
                            strLst.Add((string)obj);
                        }
                        // sets the value for the TEXML Element to a long string of the string values of each string separated by the Delimiter. 
                        //      Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
                        SetDataList(strLst, Delimiter, UOM);
                    }
                    break;
                // check that the name is Double
                case "Double":
                    {
                        // create a new list of nullable Double
                        List<Double?> dblLst = new List<Double?>();
                        foreach (Object obj in lst)
                        {
                            // Add to the nullable double list the object cast to a nullable double
                            dblLst.Add((Double?)obj);
                        }
                        // sets the value for the TEXML Element to a long string of the nullable double values of each nullable double value separated by the Delimiter. 
                        //      Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
                        SetDataList(dblLst, Delimiter, UOM);
                    }
                    break;
                // check that the name is DateTime
                case "DateTime":
                    {
                        // create a new list of nullable DateTime
                        List<DateTime?> dblLst = new List<DateTime?>();
                        foreach (Object obj in lst)
                        {
                            // Add to the nullable DateTime list the object cast to a nullable DateTime
                            dblLst.Add((DateTime?)obj);
                        }
                        // sets the value for the TEXML Element to a long string of the nullable DateTime values of each nullable DateTime value separated by the Delimiter. 
                        //      Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
                        SetDataList(dblLst, Delimiter, UOM);
                    }
                    break;
                // check that the name is Integer
                case "Integer":
                    {
                        // create a new list of nullable Int32
                        List<Int32?> intLst = new List<Int32?>();
                        foreach (Object obj in lst)
                        {
                            // Add to the nullable Int32 list the object cast to a nullable Int32
                            intLst.Add((Int32?)obj);
                        }
                        // sets the value for the TEXML Element to a long string of the nullable Int32 values of each nullable Int32 value separated by the Delimiter.  
                        //      Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
                        SetDataList(intLst, Delimiter, UOM);
                    }
                    break;

            }
        }

        /// <summary>
        /// Sets the value for the TEXML Element to a long string of the string values of each string separated by the Delimiter. 
        ///                   Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="Delimiter"></param>
        /// <param name="UOM"></param>
        public void SetDataList(List<String> lst, string Delimiter = ",", string UOM = null)
        {
            // check if the list of string passed in is null
            if (lst == null)
            {
                // create an Attribute for this TEXML Element with name "Count" and value of 0
                this.Attribute("Count", 0);
                // return to caller
                return;
            }
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in list of string's Count
            this.Attribute("Count", lst.Count);
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in Delimiter string
            this.Attribute("Delimiter", Delimiter);
            // create an Attribute for this TEXML Element with name "DataType" and value of String
            this.Attribute("DataType", "String");
            // check that the passed in UOM string is not null or empty
            if (!string.IsNullOrEmpty(UOM))
            {
                // create an Attribute for this TEXML Element with name "UOM" and value of the passed in list of string's Count
                this.Attribute("UOM", lst.Count);
            }
            // Create a new StringBuilder
            StringBuilder sb = new StringBuilder();
            // initialzes a bool to true
            bool bFirst = true;
            // initializes an empty string
            string strstr = "";
            // iterates through each string in the passed in list of string
            foreach (string str in lst)
            {
                // check the string is null or empty
                if (string.IsNullOrEmpty(str))
                {
                    // assign an empty to string to the intialized string
                    strstr = "";
                }
                // else the string is not null or empty
                else
                {
                    // assign the value of the string with a replaced delimiter to the initialized string
                    strstr = str.Replace(Delimiter, "&del&");
                }
                // check that the bool is not true, makes sure each following string after the first string contains a delimiter
                if (!bFirst)
                {
                    // Append the delimiter to the end of the stringbuilder
                    sb.Append(Delimiter);
                }
                // Append the initialized string to the end of the string Builder
                sb.Append(strstr);
                // set the boolean to include delimiter for each string following the first string
                bFirst = false;
            }
            // assign the value of the stringbuilder to a string as the value of this.TEXML Element's Value property
            Value = sb.ToString();
        }

        /// <summary>
        /// // sets the value for the TEXML Element to a long string of the nullable Int32 values of each nullable Int32 value separated by the Delimiter.  
        ///             Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="Delimiter"></param>
        /// <param name="UOM"></param>
        public void SetDataList(List<Int32?> lst, string Delimiter = ",", string UOM = null)
        {
            // check if the list of string passed in is null
            if (lst == null)
            {
                // create an Attribute for this TEXML Element with name "Count" and value of 0
                this.Attribute("Count", 0);
                // return to the caller
                return;
            }
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in list of string's Count
            this.Attribute("Count", lst.Count);
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in Delimiter string
            this.Attribute("Delimiter", Delimiter);
            // create an Attribute for this TEXML Element with name "DataType" and value of Integer
            this.Attribute("DataType", "Integer");
            // check that the passed in UOM string is not null or empty
            if (!string.IsNullOrEmpty(UOM))
            {
                // create an Attribute for this TEXML Element with name "UOM" and value of the passed in list of Integer's Count
                this.Attribute("UOM", lst.Count);
            }
            // Create a new StringBuilder
            StringBuilder sb = new StringBuilder();
            // initializes a bool to true
            bool bFirst = true;
            // initializes an empty string
            string strstr = "";
            // iterates through each nullable Int32 in the passed in list of nullable Int32
            foreach (Int32? val in lst)
            {
                // check if nullable Int32 is null
                if (val == null)
                {
                    // sets the initialized string to empty
                    strstr = "";
                }
                // else the nullable Int32 is not null or empty
                else
                {
                    // assign the value of the nullable Int32's Value Property to string as the initialized string
                    strstr = val.Value.ToString();
                }
                // check that the bool is not true, makes sure each following nullable Int32 after the first nullable Int32 contains a delimiter
                if (!bFirst)
                {
                    // Append the delimiter to the end of the stringbuilder
                    sb.Append(Delimiter);
                }
                // Append the initialized string to the end of the string Builder
                sb.Append(strstr);
                // set the boolean to include delimiter for each string following the first nullable Int32
                bFirst = false;
            }
            // assign the value of the stringbuilder to a string as the value of this.TEXML Element's Value property
            Value = sb.ToString();
        }

        /// <summary>
        ///  sets the value for the TEXML Element to a long string of the nullable double values of each nullable double value separated by the Delimiter. 
        ///            Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="Delimiter"></param>
        /// <param name="UOM"></param>
        public void SetDataList(List<double?> lst, string Delimiter = ",", string UOM = null)
        {
            // check if the list of string passed in is null
            if (lst == null)
            {
                // create an Attribute for this TEXML Element with name "Count" and value of 0
                this.Attribute("Count", 0);
                // return to the caller
                return;
            }
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in list of string's Count
            this.Attribute("Count", lst.Count);
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in Delimiter string
            this.Attribute("Delimiter", Delimiter);
            // create an Attribute for this TEXML Element with name "DataType" and value of Double
            this.Attribute("DataType", "Double");
            // check that the passed in UOM string is not null or empty
            if (!string.IsNullOrEmpty(UOM))
            {
                // create an Attribute for this TEXML Element with name "UOM" and value of the passed in list of Double's Count
                this.Attribute("UOM", lst.Count);
            }
            // Create a new StringBuilder
            StringBuilder sb = new StringBuilder();
            // initializes a bool to true
            bool bFirst = true;
            // initializes an empty string
            string strstr = "";
            // iterates through each nullable double in the passed in list of nullable double
            foreach (double? dbl in lst)
            {
                // check if nullable Double is null
                if (dbl == null)
                {
                    // sets the initialized string to empty
                    strstr = "";
                }
                // else the nullable double is not null or empty
                else
                {
                    // assign the value of the nullable double's Value Property to string formatted with "R" as the initialized string
                    strstr = dbl.Value.ToString("R");
                }
                // check that the bool is not true, makes sure each following nullable double after the first nullable double contains a delimiter
                if (!bFirst)
                {
                    // Append the delimiter to the end of the stringbuilder
                    sb.Append(Delimiter);
                }
                // Append the initialized string to the end of the string Builder
                sb.Append(strstr);
                // set the boolean to include delimiter for each string following the first nullable Double
                bFirst = false;
            }
            // assign the value of the stringbuilder to a string as the value of this.TEXML Element's Value property
            Value = sb.ToString();
        }


        /// <summary>
        ///  sets the value for the TEXML Element to a long string of the nullable DateTime values of each nullable DateTime value separated by the Delimiter. 
        ///             Sets the Attribute of this Element to the Count, Delimiter, DataType and UOM if it exists.
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="Delimiter"></param>
        /// <param name="UOM"></param>
        public void SetDataList(List<DateTime?> lst, string Delimiter = ",", string UOM = null)
        {
            // check if the list of string passed in is null
            if (lst == null)
            {
                // create an Attribute for this TEXML Element with name "Count" and value of 0
                this.Attribute("Count", 0);
                // return to the caller
                return;
            }
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in list of string's Count
            this.Attribute("Count", lst.Count);
            // create an Attribute for this TEXML Element with name "Count" and value of the passed in Delimiter string
            this.Attribute("Delimiter", Delimiter);
            // create an Attribute for this TEXML Element with name "DataType" and value of DateTime
            this.Attribute("DataType", "Double");
            // check that the passed in UOM string is not null or empty
            if (!string.IsNullOrEmpty(UOM))
            {
                // create an Attribute for this TEXML Element with name "UOM" and value of the passed in list of DateTime's Count
                this.Attribute("UOM", lst.Count);
            }
            // Create a new StringBuilder
            StringBuilder sb = new StringBuilder();
            // initializes a bool to true
            bool bFirst = true;
            // initializes an empty string
            string strstr = "";
            // iterates through each nullable DateTime in the passed in list of nullable DateTime
            foreach (DateTime? dbl in lst)
            {
                // check if nullable DateTime is null
                if (dbl == null)
                {
                    // sets the initialized string to empty
                    strstr = "";
                }
                // else the nullable DateTime is not null or empty
                else
                {
                    // assign the value of the nullable DateTime's Value Property to string formatted with "R" as the initialized string
                    strstr = dbl.Value.ToString("R");
                }
                // check that the bool is not true, makes sure each following nullable DateTime after the first nullable DateTime contains a delimiter
                if (!bFirst)
                {
                    // Append the delimiter to the end of the stringbuilder
                    sb.Append(Delimiter);
                }
                // Append the initialized string to the end of the string Builder
                sb.Append(strstr);
                // set the boolean to include delimiter for each string following the first nullable DateTime
                bFirst = false;
            }
            // assign the value of the stringbuilder to a string as the value of this.TEXML Element's Value property
            Value = sb.ToString();
        }

        public List<XmlAttribute> AttributesFromPath(string Path)
        {
            // Create a new list of TEXML
            List<XmlAttribute> xmlAttributes = new List<XmlAttribute>();
            // check the passed in Path string is null or empty
            if (string.IsNullOrEmpty(Path))
            {
                // returns a new list of TEXML if null or empty
                return (xmlAttributes);
            }

            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // initializes a XmlNodeList
                XmlNodeList list = null;
                // check if NS Manager is not null
                if (NSManager != null)
                {
                    //selects a list of nodes matching the Path string and using the NSManager to resolve prefixes in this.TEXML Element and assigns the value to the XmlNodeList
                    list = Element.SelectNodes(Path, NSManager);
                }
                // else if NS Manager is null
                else
                {
                    // XmlNodeList is assigned the value of a list of nodes matching the Path string in this.TEXML Element
                    list = Element.SelectNodes(Path);
                }
                // check the XmlNodeList is not null
                if (list != null)
                {
                    // iterate through each XmlNode in the XmlNodeList
                    foreach (XmlNode node in list)
                    {
                        //check if the XmlNode is a XmlElement
                        if (node is XmlAttribute)
                        {
                            //cast the XmlNode to a XmlElement
                            XmlAttribute attribute = (XmlAttribute)node;
                            xmlAttributes.Add(attribute);
                        }
                    }
                }
            }
            // return the list of Attributes
            return (xmlAttributes);
        }

        /// <summary>
        /// Generates a List of TEXML from this.TEXML Element using the passed XPath string
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public List<TEXML> Elements(string Path)
        {
            // Create a new list of TEXML
            List<TEXML> elements = new List<TEXML>();
            // check the passed in Path string is null or empty
            if (string.IsNullOrEmpty(Path))
            {
                // returns a new list of TEXML if null or empty
                return (elements);
            }
            
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // initializes a XmlNodeList
                XmlNodeList list = null;
                // check if NS Manager is not null
                if (NSManager != null)
                {
                    //selects a list of nodes matching the Path string and using the NSManager to resolve prefixes in this.TEXML Element and assigns the value to the XmlNodeList
                    list = Element.SelectNodes(Path, NSManager);
                }
                // else if NS Manager is null
                else
                {
                    // XmlNodeList is assigned the value of a list of nodes matching the Path string in this.TEXML Element
                    list = Element.SelectNodes(Path);
                }
                // check the XmlNodeList is not null
                if (list != null)
                {
                    // iterate through each XmlNode in the XmlNodeList
                    foreach (XmlNode node in list)
                    {
                        //check if the XmlNode is a XmlElement
                        if (node is XmlElement)
                        {
                            //cast the XmlNode to a XmlElement
                            XmlElement el = (XmlElement)node;
                            // create a new TEXML Element from the XmlElement and NS Manager
                            TEXML xmlNode = new TEXML(el, m_NSmanager);
                            // Add this TEXML Element to the list of TEXML
                            elements.Add(xmlNode);
                        }
                    }
                }
            }
            // return the list of TEXML
            return (elements);
        }

        /// <summary>
        /// Gets the first TEXML Element that matches the Path string index and creates a TEXML Element from it
        /// Sets the value of the TEXML Element at the Path index and assigns it's Parent and Siblings nodes.
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public TEXML this[string Path]
        {
            get
            {
                // creates a new TEXML Element
                TEXML trXml = new TEXML();
                // initializes a XmlNode
                XmlNode xEl = null;
                // check if this.TEXML Element is null
                if (Element == null)
                {
                    // returns the new TEXML Element
                    return (trXml);
                }
                //check if NS Manager is not null
                if (NSManager != null)
                {
                    // sets the initialized XmlNode as the first TEXML Element that matches at the Path index with prefixes resolved using the NS Manager
                    xEl = Element.SelectSingleNode(Path, NSManager);
                }
                // else if NS Manager is null
                else
                {
                    // sets the initialized XmlNode as the first TEXML Element that matches at the Path index 
                    xEl = Element.SelectSingleNode(Path);
                }
                // check if the XmlNode found is not null
                if (xEl != null)
                {
                    // check if the XmlNode is a XmlNode
                    if (xEl is XmlNode)
                    {
                        // creates a new TEXML Element from the XmlNode and NsManager
                        trXml = new TEXML((XmlNode)xEl, m_NSmanager);
                    }
                }
                // returns the TEXML Element created
                return (trXml);
            }
            set
            {
                // initializes a XmlNode
                XmlNode xEl = null;
                // checks if the NS Manager is not null
                if (NSManager != null)
                {
                    // sets the initialized XmlNode as the first TEXML Element that matches at the Path index with prefixes resolved using the NS Manager
                    xEl = Element.SelectSingleNode(Path, NSManager);
                }
                // else if NS Manager is null
                else
                {
                    // sets the initialized XmlNode as the first TEXML Element that matches at the Path index 
                    xEl = Element.SelectSingleNode(Path);
                }
                // checks if the XmlNode is not null
                if (xEl != null)
                {
                    // checks if the XmlNode is a XmlElement
                    if (xEl is XmlElement)
                    {
                        // checks if the value we are setting is a TEXML Element
                        if (value is TEXML)
                        {
                            // creates a new Parent node XmlNode for the XmlNode
                            XmlNode Parent = xEl.ParentNode;
                            // checks that the Parent XmlNode is not null
                            if (Parent != null)
                            {
                                // creates a new XmlNode sibling of the XmlNode which immediately follows the XmlNode on the Path
                                XmlNode NextSibling = xEl.NextSibling;
                                // Removes the XmlNode from the Parent
                                Parent.RemoveChild(xEl);

                                // checks if the value we are setting is not null
                                if (value != null)
                                {
                                    // creates deep copy XmlNode from this.TEXML Element's Parent nodes XmlDocument using the XmlElement of the value setter
                                    XmlNode importNode = Parent.OwnerDocument.ImportNode(value.Element, true);
                                    // checks that the created XmlNode is not null
                                    if (NextSibling != null)
                                    {
                                        // inserts the created deep copy XmlNode before the created XmlNode sibling
                                        Parent.InsertBefore(importNode, NextSibling);
                                    }
                                    // else if the Xml Node NextSibling is null
                                    else
                                    {
                                        // Append the created deep copy XmlNode to the XmlNode Parent
                                        Parent.AppendChild(importNode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get's a list of XmlAttribute Names in this.TEXML Element
        /// </summary>
        public List<string> AttributeNames
        {
            get
            {
                // create a new list of string
                List<string> strList = new List<string>();
                // check this.TEXML Element is not null
                if (Element != null)
                {
                    // iterate through each XmlAttribute in this.TEXML Element's collection of XmlAttribute
                    foreach (XmlAttribute attribute in Element.Attributes)
                    {
                        // add's the XmlAttribute Name property to the list of string
                        strList.Add(attribute.Name);
                    }
                }
                // returns the list of XmlAttribute Names
                return (strList);
            }
        }

        /// <summary>
        /// Gets a list of XmlAttribute for this.TEXML Element
        /// </summary>
        public List<XmlAttribute> Attributes
        {
            get
            {
                // create a new list of XmlAttribute
                List<XmlAttribute> list = new List<XmlAttribute>();
                // check this.TEXML Element is not null
                if (Element != null)
                {
                    // iterate through each XmlAttribute in this.TEXML Element's collection of XmlAttribute
                    foreach (XmlAttribute attribute in Element.Attributes)
                    {
                        // adds the XmlAttribute to the list of XmlAttribute
                        list.Add(attribute);
                    }
                }
                // returns the list of XmlAttribute
                return (list);
            }
        }

        /// <summary>
        /// Returns whether the XmlAttribute with the passed in Name exists in this.TEXML Element
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool HasAttribute(string Name)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // get a XmlAttribute from this.TEXML Element's Attribute collection using the specified Name index
                XmlAttribute attr = Element.Attributes[Name];
                // check the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the value of the XmlAttribute is null or empty
                    if (string.IsNullOrEmpty(attr.Value))
                    {
                        // return does not have this XmlAttribute name
                        return (false);
                    }
                    // else the value of the XmlAttribute is not null or empty
                    else
                    {
                        // return does have this XmlAttribute name
                        return (true);
                    }
                }
                // else the XmlAttribute retrieved is null
                else
                {
                    // return does not have this XmlAttribute name
                    return (false);
                }
            }
            // else this.TEXML Element is null
            else
            {
                // return does not have this XmlAttribute name
                return (false);
            }
        }

        /// <summary>
        /// Get the Value of the XmlAttribute Value property at the specified Name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public string Attribute(string Name)
        {
            // check if the TEXML Element property is not null
            if (Element != null)
            {
                // Assign the value of the TEXML Element attributes collection at the index of the passed in Name parameter to the XmlAttribute
                XmlAttribute attr = Element.Attributes[Name];
                // check if the XmlAttribute created is not null
                if (attr != null)
                {
                    // return the value of the XmlAttribute Value property
                    return (attr.Value);
                }
                // if the XmlAttribute created is null
                else
                {
                    // return null
                    return (null);
                }
            }
            // else if the TEXML Element property is null
            else
            {
                // return null
                return (null);
            }
        }

        /// <summary>
        /// Create a XmlAttribute from the passed in Name and val object
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, object val)
        {
            // check if the passed in val is an Int32
            if (val is Int32)
            {
                // Create a XmlAttribute from the passed in Name and Int32 val
                Attribute(Name, System.Convert.ToInt32(val));
            }
            // else check if the passed in val is an Int64
            else if (val is Int64)
            {
                // Create a XmlAttribute from the passed in Name and Int64 val
                Attribute(Name, System.Convert.ToInt64(val));
            }
            // else check if the passed in val is a double
            else if (val is double)
            {
                // Create a XmlAttribute from the passed in Name and double val
                Attribute(Name, System.Convert.ToDouble(val));
            }
            // else check if the passed in val is a DateTime
            else if (val is DateTime)
            {
                // Create a XmlAttribute from the passed in Name and DateTime val
                Attribute(Name, System.Convert.ToDateTime(val));
            }
            // else check if the passed in val is a boolean
            else if (val is bool)
            {
                // Create a XmlAttribute from the passed in Name and boolean val
                Attribute(Name, System.Convert.ToBoolean(val));
            }
            // else let's get the val string representation
            else
            {
                if (val == null)
                {
                    Attribute(Name, "");
                }
                else
                {
                    // Create a XmlAttribute from the passed in Name and val's string representation
                    Attribute(Name, val.ToString());
                }
            }
        }

        /// <summary>
        /// Get an ENUM value from the XML attributes.
        /// </summary>
        public T AttributeEnum<T>(string Name)
        {
            string strValue = this.Attribute(Name);
            return (T)Enum.Parse(typeof(T), strValue);
        }

        /// <summary>
        /// Set an ENUM value into the XML attributes.
        /// </summary>
        public void AttributeEnum<T>(string Name, T enumValue)
        {
            string strValue = Enum.GetName(typeof(T), enumValue);
            this.Attribute(Name, strValue);
        }

        /// <summary>
        /// Create a XmlAttribute using the passed in Name and the list of string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="strList"></param>
        public void Attribute(string name,List<string> strList)
        {
            // create a new StringBuilder object
            StringBuilder sb = new StringBuilder();
            // iterate through each string in the passed in list of string
            foreach(string str in strList)
            {
                // append the string to the stringbuilder
                sb.Append(str);
                // add a delimiter after each string
                sb.Append(";");
            }
            // Create a XmlAttribute using the passed in Name and the created StringBuilder string as the value
            Attribute(name, sb.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<string> AttributeStrList(string name)
        {
            //create a list of string
            List<string> strList = new List<string>();
            // Get the Value of this.TEXML Element's XmlAttribute at the passed in Name parameter
            string strValue = Attribute(name);
            // check if the Value retrieved is not null or empty
            if(!string.IsNullOrEmpty(strValue))
            { 
                // create a string array by splitting the Value retrieved on the ';' character delimiter
                string[] strValues = strValue.Split(';');
                // iterate through each string in the array
                foreach (string str in strValues)
                {
                    // add the string to the list of string
                    strList.Add(str);
                }
            }
            // return the list of string
            return (strList);
        }


        /// <summary>
        /// Create a XmlAttribute from the Name and Value passed in and append it to the collection of attributes on the TEXML Element node this is being called from
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        /// <param name="SkipEmpty"></param>
        public void Attribute(string Name, string val,bool SkipEmpty=false)
        {
            // we check that the Element property is not null
            if (Element != null)
            {
                // we then check whether the SkipEmpty bool has been set and also that the value passed in is not null or empty
                if(SkipEmpty && string.IsNullOrEmpty(val))
                {
                    // we return right away if either is null or empty or true (for Empty)
                    return;
                }
                // we look through our collection of XmlAttribute for the specified Name in the parameter and assign this attribute to a new XmlAttribute
                XmlAttribute attr = Element.Attributes[Name];
                // if the XmlAttribute is not null (i.e. we found an attribute with value at that Name index)
                if (attr != null)
                {
                    // we set the passed in value as our new XmlAttribute's Value property.
                    attr.Value = val;
                }
                // else if the XmlAttribute is null at that Name index
                else
                {
                    // create a split character ":"
                    char[] splitOn = new char[1];
                    splitOn[0] = ':';
                    // create an array of parts by splitting the Name parameter on the ":" character
                    string[] parts = Name.Split(splitOn);
                    // check that the Length of the array is equal to 2 and the first part does not equal the xmlns namespace tag
                    if (parts.Length == 2 && parts[0] != "xmlns")
                    {
                        // set the prefix to the first part
                        string PreFix = parts[0];
                        // set the local name to the second part
                        string localName = parts[1];
                        // create a NS using the NSManager look up of the created prefix
                        string urlNS = NSManager.LookupNamespace(PreFix);
                        // set the value of the XmlAttribute to our XmlDocument's CreateAttribute method passing in the prefix, local name and urlNS that we created
                        attr = Doc.CreateAttribute(PreFix, localName, urlNS);
                    }
                    // if the length is less than or greater than 2 or the first part of the split is the xmlns namespace tag
                    else
                    {
                        // we will create an attribute using the XmlDocument's CreateAttribute and passing in the Name parameter to assign to our XmlAttribute element
                        attr = Doc.CreateAttribute(Name);
                    }
                    // we set the passed in value to the XmlAttribute's Value property
                    attr.Value = val;
                    // we then append the new XmlAttribute to the Element node's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Creates an Attribute from the specified Name and Int32 Value. 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int32 val)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // assign the value of this. TEXML Element Attribute collection at the passed in parameter Name to the XmlAttribute
                XmlAttribute attr = Element.Attributes[Name];
                // check if the XmlAttribute is not null
                if (attr != null)
                {
                    // assign the value of the passed in Int32 to a string representation as the XmlAttribute's Value property
                    attr.Value = val.ToString();
                }
                // else if the XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name using the passed in Name parameter
                    attr = Doc.CreateAttribute(Name);
                    // assign the value of the passed in Int32 to a string representation as the XmlAttribute's Value property 
                    attr.Value = val.ToString();
                    // appends the created XmlAttribute to this.TEXML Element Attribute collection
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Creates an Attribute from the specified Name and nullable Int32 Value. 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int32 ?val)
        {
            // check if this.TEXML Element is not null 
            if (Element != null)
            {
                // assign the value of this. TEXML Element Attribute collection at the passed in parameter Name to the XmlAttribute
                XmlAttribute attr = Element.Attributes[Name];
                // check if the XmlAttribute is not null 
                if (attr != null)
                {
                    // check the passed in nullable Int32 has a value
                    if (val.HasValue)
                    {

                        // assign the value of the passed in nullable Int32 to a string representation as the XmlAttribute's Value property
                        attr.Value = val.ToString();
                    }
                    // else if the passed in nullable Int32 is null
                    else
                    {
                        // assign the value of an empty string to the XmlAttribute's Value property
                        attr.Value = "";
                    }
                }
                // else if the XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name using the passed in Name parameter
                    attr = Doc.CreateAttribute(Name);
                    // check if the passed in nullable Int32 has a value
                    if (val.HasValue)
                    {
                        // assign the value of the passed in nullable Int32 to a string representation as the XmlAttribute's Value property
                        attr.Value = val.ToString();
                    }
                    // else if the passed in nullable Int32 is null
                    else
                    {
                        // assign the value of an empty string to the XmlAttribute's Value property
                        attr.Value = "";
                    }
                    // appends the created XmlAttribute to this.TEXML Element Attribute collection
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute from the passed in name and Int16 parameters
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int16 val)
        {
            // check that this.TEXML Element is not null
            if (Element != null)
            {
                // get the XmlAttribute at the passed in name string parameter on this.TEXML Element's collection of XmlAttribute
                XmlAttribute attr = Element.Attributes[Name];
                // check that the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // set the value of the XmlAttribute as the passed in Int16 val's string representation
                    attr.Value = val.ToString();
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // set the XmlAttribute Value as the passed in Int16 val's string representation
                    attr.Value = val.ToString();
                    // append the XmlAttribute to this.TEXML Element's XmlAttribute collection
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute from the passed in name and nullable Int16 parameters
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int16 ?val)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the passed in nullable Int16 Has a Value
                    if (val.HasValue)
                    {
                        // set the Value of the created XmlAttribute as the passed in nullable Int16 val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable Int16 is null
                    else
                    {
                        // Set the Value of the created XmlAttibute as an empty string
                        attr.Value = "";
                    }
                }
                // else the created XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // check if the passed in nullable Int16 Has a Value
                    if (val.HasValue)
                    {
                        // set the Value of the created XmlAttribute as the passed in nullable Int16 val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable Int16 is null
                    else
                    {
                        // Set the Value of the created XmlAttibute as an empty string
                        attr.Value = "";
                    }
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute from the passed in name and Int16 parameters
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int64 val)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // set the Value of the created XmlAttribute as the passed in Int16 val's string representation
                    attr.Value = val.ToString();
                }
                // else the XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // set the Value of the created XmlAttribute as the passed in Int16 val's string representation
                    attr.Value = val.ToString();
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute from the passed in name and nullable Int64 parameters
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, Int64 ?val)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the passed in nullable Int64 Has a Value
                    if (val.HasValue)
                    {
                        // set the value of the XmlAttribute as the passed in Int64 val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable Int64 is null
                    else
                    {
                        // Set the Value of the created XmlAttibute as an empty string
                        attr.Value = "";
                    }
                }
                // else the created XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // check if the passed in nullable Int64 Has a Value
                    if (val.HasValue)
                    {
                        // set the Value of the created XmlAttribute as the passed in nullable Int64 val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable Int64 is null
                    else
                    {
                        // Set the Value of the created XmlAttibute as an empty string
                        attr.Value = "";
                    }
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute with the passed in name identifier and bool value. 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="bVal"></param>
        public void Attribute(string Name, bool bVal)
        {
            // check if booleans should be lower case
            if (UseLowerCaseBooleans)
            {
                // creates a XmlAttribute using the name identifier and string representation of a bool value
                Attribute(Name, bVal ? "true" : "false");
                // returns to the caller
                return;
            }
            // check the passed in bool value is true
            if (bVal == true)
            {
                // creates a XmlAttribute using the name identifier and string representation of a bool value
                Attribute(Name, "true");
            }
            // else the passed in bool value is false
            else
            {
                // creates a XmlAttribute using the name identifier and string representation of a bool value
                Attribute(Name, "false");
            }
        }

        /// <summary>
        /// Creates a XmlAttribute using the name identifier and a GUID value
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="guid"></param>
        public void Attribute(string Name, Guid guid)
        {
            // creates a XmlAttribute using the name identifier and string representation of a GUID value
            Attribute(Name, guid.ToString());
        }

        /// <summary>
        /// Create a XmlAttribute with the passed in name identifier and DateTime value with the specified format. Format defaults to yyyy-MM-ddTHH:mm:ssZ
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        /// <param name="format"></param>
        public void Attribute(string Name, DateTime val, string format = "yyyy-MM-ddTHH:mm:ssZ")
        {
            // checks if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // checks that the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // set the value of the XmlAttribute as the passed in DateTime val's string representation with the specified format
                    attr.Value = val.ToString(format);
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // set the value of the XmlAttribute as the passed in DateTime val's string representation with the specified format
                    attr.Value = val.ToString(format);
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute with the passed in name identifier and nullable DateTime value with the specified format. Format defaults to "J"
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        /// <param name="format"></param>
        public void Attribute(string Name, DateTime? val, string format = "J")
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the passed in nullable DateTime val is not null
                    if (val.HasValue)
                    {
                        // check if the passed in format is null or empty
                        if (string.IsNullOrEmpty(format))
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation
                            attr.Value = val.Value.ToString();
                        }
                        // else check if the format is "J"
                        else if (format == "J")
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation with the specified format
                            attr.Value = val.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }
                        // else the format is some other specification
                        else
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation with the specified format
                            attr.Value = val.Value.ToString(format);
                        }
                    }
                    // else the nullable DateTime is null
                    else
                    {
                        // set the value of the XmlAttribute as an empty string
                        attr.Value = "";
                    }
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // check if the passed in Nullable DateTime is null
                    if (val.HasValue)
                    {
                        // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                        attr = Doc.CreateAttribute(Name);
                        // check if the passed in format is null or empty
                        if (string.IsNullOrEmpty(format))
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation
                            attr.Value = val.Value.ToString();
                        }
                        // else check if the format is "J"
                        else if (format == "J")
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation with the specified format
                            attr.Value = val.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }
                        // else the format is some other specification
                        else
                        {
                            // set the value of the XmlAttribute as the passed in DateTime val's Value as a string representation with the specified format
                            attr.Value = val.Value.ToString(format);
                        }
                        // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                        Element.Attributes.Append(attr);
                    }
                }
            }
        }

        /// <summary>
        /// Create a XmlAttribute with the passed in name identifier and double value 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, double val)
        {
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // set the value of the XmlAttribute as the passed in double val's string representation
                    attr.Value = val.ToString();
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // set the value of the XmlAttribute as the passed in double val's string representation
                    attr.Value = val.ToString();
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }
        /// <summary>
        /// Create a XmlAttribute with the passed in name identifier and nullable double value 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="val"></param>
        public void Attribute(string Name, double ? val)
        {
            // check this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the passed in nullable double is not null
                    if (val.HasValue)
                    {
                        // set the value of the XmlAttribute as the passed in nullable double val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable double is null
                    else
                    {
                        // set the value of the XmlAttribute as an empty string
                        attr.Value = "";
                    }
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // Create a XmlAttribute with the specified XmlDocument Name parameter passed in
                    attr = Doc.CreateAttribute(Name);
                    // check if the passed in nullable double is not null
                    if (val.HasValue)
                    {
                        // set the value of the XmlAttribute as the passed in nullable double val's string representation
                        attr.Value = val.ToString();
                    }
                    // else the passed in nullable double is null
                    else
                    {
                        // set the value of the XmlAttribute as an empty string
                        attr.Value = "";
                    }
                    // append the newly created XmlAttribute to this.TEXML Element's collection of XmlAttribute
                    Element.Attributes.Append(attr);
                }
            }
        }
        
        /// <summary>
        /// Gets the boolean value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool AttributeBoolValue(string Name)
        {
            // initialize a bool 
            bool val = false;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the XmlAttribute Value property to Lowercase is true or equals 1 string values
                    if (attr.Value.ToLower() == "true" || attr.Value == "1")
                        // set the initialized bool to true
                        val = true;
                    // returns the initialized bool
                    return (val);
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // returns the initialized bool
                    return (val);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // returns the initialized bool
                return (val);
            }
        }

        /// <summary>
        /// Gets the Int32 value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Int32 AttributeInt32Value(string Name)
        {
            // initialize an Int32
            Int32 val = 0;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                if (attr != null)
                {
                    // set the Int32 value as the converted value of the XmlAttribute's Value property to an Int32
                    val = Convert.ToInt32(attr.Value);
                    // return the initialized Int32
                    return (val);
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // return the initialized Int32
                    return (val);
                }
            }
            // else this.TEXML Element is null
            else
            {
                // return the initialized Int32
                return (val);
            }
        }
        /// <summary>
        /// Gets the nullable Int32 value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Int32 ? AttributeInt32ValueEx(string Name)
        {
            // initialize a nullable Int32
            Int32? val = null;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null and the XmlAttribute's Value property is not null or empty
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                {
                    // set the nullable Int32 value as the converted value of the XmlAttribute's Value property to a nullable Int32
                    val = Convert.ToInt32(attr.Value);
                    // return the initialized nullable Int32
                    return (val);
                }
                // else the retrieved XmlAttribute is null or the XmlAttribute's Value property is null or empty
                else
                {
                    // return the initialized nullable Int32
                    return (val);
                }
            }
            // else this.TEXML Element is null
            else
            {
                // return the initialized nullable Int32
                return (val);
            }
        }
        /// <summary>
        /// Gets the Int64 value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Int64 AttributeInt64Value(string Name)
        {
            // initialize an Int64
            Int64 val = 0;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // set the Int64 value as the converted value of the XmlAttribute's Value property to an Int64
                    val = Convert.ToInt64(attr.Value);
                    // return the initialized Int64
                    return (val);
                }
                // else the retrieved XmlAttribute is null
                else
                {
                    // return the initialized Int64
                    return (val);
                }
            }
            // else this.TEXML Element is null
            else
            {
                // return the initialized Int64
                return (val);
            }
        }

        /// <summary>
        /// Gets the nullable Int64 value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Int64 ? AttributeInt64ValueEx(string Name)
        {
            // initializes a nullable Int64
            Int64? val = null;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null and the XmlAttribute's Value property is not null or empty
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                {
                    // set the nullable Int64 value as the converted value of the XmlAttribute's Value property to a nullable Int64
                    val = Convert.ToInt64(attr.Value);
                    // returns the initialized nullable Int64
                    return (val);
                }
                // else if the retrieved XmlAttribute is null
                else
                {
                    // returns the initialized nullable Int64
                    return (val);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // returns the initialized nullable Int64
                return (val);
            }
        }

        /// <summary>
        /// Gets the double value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public double AttributeDoubleValue(string Name)
        {
            //initializes a double
            double val = 0;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null 
                if (attr != null)
                {
                    try
                    {
                        // set the double value as the converted value of the XmlAttribute's Value property to a double
                        val = Convert.ToDouble(attr.Value);
                    }
                    catch(Exception ex)
                    {
                        TELogger.Log(0, "Failed to convert value to double, value=" +attr.Value);
                        TELogger.Log(0, ex);
                        throw;
                    }
                    // return the initialized double
                    return (val);
                }
                // else if the retrieved XmlAttribute is null
                else
                {
                    // return the initialized double
                    return (val);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // return the initialized double
                return (val);
            }
        }

        /// <summary>
        /// Gets the nullable double value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public double ? AttributeDoubleValueEx(string Name)
        {
            // initializes a nullable double
            double ? val = null;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null and the XmlAttribute's Value property is not null or empty
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                {
                    try
                    {
                        // set the nullable double value as the converted value of the XmlAttribute's Value property to a double
                        val = Convert.ToDouble(attr.Value);
                    }
                    // catch value not being a double an set it as null
                    catch
                    {
                        TELogger.Log(5, "Failed to convert to double, Name=" + Name + " Value" + attr.Value);
                        val = null;
                    };
                    // returns the initialized nullable double
                    return (val);
                }
                // else if the retrieved XmlAttribute is null
                else
                {
                    // returns the initialized nullable double
                    return (val);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // returns the initialized nullable double
                return (val);
            }
        }

        /// <summary>
        /// Gets the DateTime value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public DateTime AttributeDateValue(string Name)
        {
            // Instantiate a new DateTime
            DateTime dateTime = new DateTime();
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null 
                if (attr != null)
                {
                    // set the DateTime value as the converted value of the XmlAttribute's Value property to a DateTime
                    dateTime = Convert.ToDateTime(attr.Value);
                    // return the newly created DateTime
                    return (dateTime);
                }
                // else if the retrieved XmlAttribute is null
                else
                {
                    // return the newly created DateTime
                    return (dateTime);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // return the newly created DateTime
                return (dateTime);
            }
        }

        /// <summary>
        /// Gets the nullable DateTime value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public DateTime? AttributeDateValueEx(string Name, string format = "J")
        {
            // initializes a nullable DateTime
            DateTime? dateTime = null;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null
                if (attr != null)
                {
                    // check if the XmlAttribute's Value property is not null or empty
                    if (!string.IsNullOrEmpty(attr.Value))
                    {
                        try
                        {
                            // check if the passed in format is "J" for JSON
                            if (format == "J")
                            {
                                // set the nullable DateTime value as the converted value of the XmlAttribute's Value property to a DateTime with the JSON format
                                dateTime = TEDataConvert.ParseJSONDate(attr.Value);
                            }
                            // else the format is a different specification
                            else
                            {
                                // set the nullable DateTime value as the converted value of the XmlAttribute's Value property to a DateTime with the specified format and in Invariant Culture
                                dateTime = DateTime.ParseExact(attr.Value, format, CultureInfo.InvariantCulture);
                            }
                        }
                        // catch an invalid DateTime
                        catch (Exception)
                        {
                            TELogger.Log(1, "Invalid date format detected, value=" + attr.Value + " format = " + format);
                        }
                        // check if the newly created nullable DateTime is not null
                        if (dateTime == null)
                        {
                            // set the nullable DateTime value as the converted value of the XmlAttribute's Value property to a DateTime for nullable DateTimes
                            dateTime = TEDataConvert.TRParseDateTime(attr.Value);
                        }
                    }
                    // return the newly created nullable DateTime
                    return (dateTime);
                }
                // else if the retrieved XmlAttribute is null
                else
                {
                    // return the newly created nullable DateTime
                    return (dateTime);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // return the newly created nullable DateTime
                return (dateTime);
            }
        }

        /// <summary>
        /// Gets the Guid value of the Attribute with the specified name parameter
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Guid AttributeGuidValue(string Name)
        {
            // initializes a GUID
            Guid guidValue = Guid.Empty;
            // check if this.TEXML Element is not null
            if (Element != null)
            {
                // Get the XmlAttribute in this.TEXML Element's XmlAttribute collection at the specified name index
                XmlAttribute attr = Element.Attributes[Name];
                // check if the retrieved XmlAttribute is not null and the XmlAttribute's Value property is not null or empty
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                {
                    // set the Guid value as the parsed value of the XmlAttribute's Value property to a Guid
                    guidValue = Guid.Parse(attr.Value);
                    // return the initialized Guid
                    return (guidValue);
                }
                // else if the retrieved XmlAttribute is null or the XmlAttribute's Value property is null or empty
                else
                {
                    // return the initialized Guid
                    return (guidValue);
                }
            }
            // else if this.TEXML Element is null
            else
            {
                // return the initialized Guid
                return (guidValue);
            }
        }

        /// <summary>
        /// Checks if the TEXML Element property is null
        /// </summary>
        public bool IsNull
        {
            get
            {
                if (Element == null)
                {
                    return (true);
                }
                else
                {
                    return (false);
                }
            }
        }
        
        /// <summary>
        /// Gets a list of TEXML Elements that are considered to be Root Elements
        /// </summary>
        public List<TEXML> RootElements
        {
            get
            {
                // create a list of TEXML
                List<TEXML> lst = new List<TEXML>();
                // Add this.TEXML to the list
                lst.Add(this);
                // returns the list of TEXML
                return (lst);
            }
        }

        /// <summary>
        /// Gets a list of TEXML Elements within this TEXML
        /// </summary>
        public List<TEXML> ChildElements
        {
            get
            {
                // Creates a new list of TEXML 
                List<TEXML> lst = new List<TEXML>();
                // iterates through all child Nodes of the TEXML Element
                foreach (XmlNode node in Element.ChildNodes)
                {
                    // check if Node is a XmlElement
                    if (node is XmlElement)
                    {
                        // if Node is XmlElement, add a new TEXML created from the XmlNode and NS Manager to the list of TEXML
                        lst.Add(new TEXML(node, m_NSmanager));
                    }
                }
                // returns the list of TEXML
                return (lst);
            }
        }

        /// <summary>
        /// Gets the previous sibling immediately preceding this.TEXML Element
        /// </summary>
        public TEXML PreviousSibling
        {
            get
            {
                // initializes a XmlElement
                XmlElement xNext = null;

                // checks if this.TEXML Element is not null
                if (!IsNull)
                {
                    // initializes a XmlNode
                    XmlNode xNode;
                    // initializes a XmlNode with this.TEXML Element value
                    XmlNode xCurNode = Element;
                    do
                    {
                        // sets the XmlNode as the node immediately preceding the current XmlNode
                        xNode = xCurNode.PreviousSibling;
                        // checks if this previous node is null
                        if (xNode == null)
                            // breaks if null
                            break;
                        // checks if this previous node is a XmlElement
                        if (xNode is XmlElement)
                        {
                            // casts the previous node to a XmlElement and set this as the Next XmlElement
                            xNext = (XmlElement)xNode;
                            // breaks if is a XmlElement
                            break;
                        }
                        // else if the XmlNode is not a XmlElement
                        else
                        {
                            // The current XmlNode is set to the value of the previous XmlNode
                            xCurNode = xNode;
                        }
                        // continues until the loop find a null previous node or a XmlElement in the previous node
                    } while (true);
                }
                // creates a new TEXML Element from the Next(should be previous) XmlElement and NS Manager
                TEXML xmlFirstChild = new TEXML(xNext, m_NSmanager);
                // returns the newly create TEXML Element
                return (xmlFirstChild);
            }
        }

        /// <summary>
        /// Finds the next XmlElement on the XmlNode to return the next sibling of the TEXML Element this is called on
        /// </summary>
        public TEXML NextSibling
        {
            get
            {
                // initializes a new XmlElement
                XmlElement xNext = null;

                // check if TEXML Element is not null
                if (!IsNull)
                {
                    // Initialize a new XmlNode
                    XmlNode xNode;
                    // Initialize a new XmlNode with the value of the TEXML Element property
                    XmlNode xCurNode = Element;
                    do
                    {
                        // assign the XmlNode the value of the TEXML Element's next immediate node
                        xNode = xCurNode.NextSibling;
                        // check if the next immediate node was null
                        if (xNode == null)
                            // if null then break
                            break;
                        // check if the next immediate node is a XmlElement
                        if (xNode is XmlElement)
                        {
                            // create a XmlElement from the XmlNode if it is a XmlElement, this is our next sibling
                            xNext = (XmlElement)xNode;
                            // break the loop 
                            break;
                        }
                        // else XmlNode was not null and was not a XmlElement
                        else
                        {
                            // assign the XmlNode value to the XmlNode
                            xCurNode = xNode;
                        }
                        // continue to loop until a break is called
                    } while (true);
                }
                // Creates a new TEXML Element from the TEXML sibling and a NS manager
                TEXML xmlFirstChild = new TEXML(xNext, m_NSmanager);
                // returns the newly created TEXML Element sibling
                return (xmlFirstChild);
            }
        }

        /// <summary>
        /// Finds and gets the First TEXML Element child of the XmlNode
        /// </summary>
        public TEXML FirstChild
        {
            get
            {
                // initializes a XmlElement 
                XmlElement xFirstChild = null;
                // initializes a XmlNode
                XmlNode xNode = null;
                // checks that the TEXML Element property is not null
                if (Element != null)
                {
                    // sets the XmlNode as the FirstChild XmlNode property of the TEXML Element
                    xNode = Element.FirstChild;
                    do
                    {
                        //checks if the XmlNode returned is null
                        if (xNode == null)
                            // breaks if null
                            break;
                        // checks if the XmlNode returned is a XmlElement
                        if (xNode is XmlElement)
                        {
                            // assigns the XmlElement initialized earlier as the value of the XmlNode cast to a XmlElement
                            xFirstChild = (XmlElement)xNode;
                            // breaks the loop
                            break;
                        }
                        // assigns the XmlNode as the initialized XmlNode's NextSibling (the node immediately following this node)
                        xNode = xNode.NextSibling;
                        // continues to execute until the loop breaks
                    } while (true);
                }
                // creates a new TEXML element from the First Child created and the NSManager
                TEXML xmlFirstChild = new TEXML(xFirstChild, m_NSmanager);
                // returns the newly created TEXML Element
                return (xmlFirstChild);
            }
        }

        /// <summary>
        /// Returns each child in the Enumerable list of TEXML
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TEXML> GetEnumerator()
        {
            // iterates through each sibling in this.TEXML Element
            for (TEXML xml = this.FirstChild; !xml.IsNull; xml = xml.NextSibling)
            {
                // Yield each child.  
                yield return xml;
            }
        }
       
        /// <summary>
      /// Get's the next XmlCDataSection in this.TEXML or it's sibling elements
      /// </summary>
        public XmlCDataSection CData
        {
            get
            {
                // initializes a XmlCDataSection
                XmlCDataSection cdata = null;
                // initializes a XmlNode
                XmlNode xNode = null;
                // checks if this.TEXML Element is not null
                if (Element != null)
                {
                    // sets the value of the XmlNode as this.TEXML Element's first child node
                    xNode = Element.FirstChild;
                    do
                    {
                        // check if the XmlNode is null
                        if (xNode == null)
                            // break if null
                            break;
                        // check if XmlNode is a XmlCDataSection
                        if (xNode is XmlCDataSection)
                        {
                            // casts the XmlNode to a XmlCDataSection
                            cdata = (XmlCDataSection)xNode;
                            // break if found the XmlCDataSection
                            break;
                        }
                        // go to the next sibling of the XmlNode
                        xNode = xNode.NextSibling;
                        // continue until we find a null sibling or XmCDataSection
                    } while (true);
                }
                // returns the XmlCDataSection or null
                return (cdata);
            }
        }

        /// <summary>
        /// Writes the XmlCDataSection to the FileName and then removes the XmlCDataSection if necessary
        /// </summary>
        /// <param name="BasePath"></param>
        /// <param name="baseExtension"></param>
        /// <param name="bRecurse"></param>
        /// <param name="bRemoveCData"></param>
        public void ExportCDataToFile(string BasePath, string baseExtension, bool bRecurse = true, bool bRemoveCData = false)
        {
            // initializes a XmlCDataSection as this.TEXML's CData property
            XmlCDataSection cdata = this.CData;
            // check if the XmlCDataSeciton is not null
            if (cdata != null)
            {
                // gets the value of the FileName attribute as a string
                string name = Attribute("FileName");
                // checks if the FileName string is null or empty
                if (string.IsNullOrEmpty(name))
                {
                    // gets the value of the Name attribute as a string
                    name = Attribute("Name");
                    // checks if the Name string is null or empty
                    if (string.IsNullOrEmpty(name))
                    {
                        // creates a name from a New guid as a string representation
                        name = Guid.NewGuid().ToString();
                    }
                    // concatentates the passed in baseExtension to the name attribute value 
                    name += baseExtension;
                }
                // holds the BasePath string allows us to change the BasePath without commiting the changes until we finish
                string FullPath = BasePath;
                // checks if the BasePath parameter ends in the characters "\\"
                if (BasePath.EndsWith("\\"))
                {
                    // concatentates the name attribute value to the FullPath
                    FullPath += name;
                }
                // else if the BasePath parameter does not end in the characters "\\"
                else
                {
                    // adds the "\\" characters to the end of the FullPath
                    FullPath += "\\";
                    // concatentates the name attribute value to the FullPath
                    FullPath += name;
                }
                // initializes a StreamWrite using the FullPath created as the FileName
                StreamWriter sw = File.CreateText(FullPath);
                // Writes the XmlCDataSection to the Fullpath file stream 
                sw.Write(cdata.Data);
                // closes the streamwriter
                sw.Close();
                // check if passed in bool is true
                if (bRemoveCData)
                {
                    // Removes the XmlCDataSection from this.TEXML Element
                    Element.RemoveChild(cdata);
                }
            }
            // check if the passed in bool is true tp recursively continue
            if (bRecurse)
            {
                // iterates through each sibling of this.TEXML Element while TEXML Element is not null
                for (TEXML xml = this.FirstChild; !xml.IsNull; xml = xml.NextSibling)
                {
                    // recursively writes the XmlCDataSection to the FileName and then removes the XmlCDataSection if necessary
                    xml.ExportCDataToFile(BasePath, baseExtension, bRecurse, bRemoveCData);
                }
            }
        }

        /// <summary>
        /// Gets this.TEXML Element as a XElement 
        /// </summary>
        /// <returns></returns>
        public XElement GetAsXElement()
        {
            // initializes the XElement
            XElement xElement = null;
            try
            {
                // creates a new MemoryStream
                MemoryStream memoryStream = new MemoryStream();
                // saves the this.XmlDocument to the memory stream
                Doc.Save(memoryStream);
                // sets the cursor at the beginning position
                memoryStream.Seek(0, SeekOrigin.Begin);
                // creates a new XElement instance using the memory stream
                xElement = XElement.Load(memoryStream);
                // closes the memory stream
                memoryStream.Close();
            }
            catch (Exception se)
            {
                TELogger.Log(0,se);
            }
            // returns the XElement
            return xElement;
        }

        /// <summary>
        /// Gets the number of Ancestors or Parents a TEXML Element has
        /// </summary>
        /// <returns></returns>
        public int Ancestors()
        {
            // initializes an int
            int i = 0;
            // initializes a TEXML Element as this.TEXML Element's Parent property
            TEXML xml = Parent;
            // iterate through each Parent of TEXML until the Parent property is null
            while (xml != null && !xml.IsNull)
            {
                // increment the number of Parents
                ++i;
                // set the TEXML as the parent of the TEXML
                xml = xml.Parent;
            }
            // return the number of Parents
            return (i);
        }
        /// <summary>
        ///  Gets the number of siblings a TEXML Element has
        /// </summary>
        /// <returns></returns>
        public int SiblingsAfterMe()
        {
            // initializes an int
            int i = 0;
            // initializes a TEXML Element as the node immediately following this.TEXML Element'
            TEXML xml = NextSibling;
            // iterate through each Sibling of TEXML until the next Sibling is null
            while (xml != null && !xml.IsNull)
            {
                // increment the number of Siblings
                ++i;
                // set the TEXML as the sibling of the TEXML
                xml = xml.NextSibling;
            }
            // return the number of Siblings
            return (i);
        }

        /// <summary>
        /// Gets the position of this.TEXML Element relative to the Parent's Ancestors and Siblings and it's own Ancestors and Siblings
        /// </summary>
        /// <returns></returns>
        public string Position()
        {
            // initializes an empty string
            string pos = "";
            // checks if this.TEXML Element's Parent is not null 
            if (Parent != null && !Parent.IsNull)
            {
                // sets the position as the number of Parents Ancestors before and the number of Parents siblings after i.e. #-#
                pos += Parent.Ancestors() + "-" + Parent.SiblingsAfterMe();
            }
            // else if this.TEXML Element's Parent property is null
            else
            {
                // sets the positions as 0-0
                pos += "0-0";
            }
            // adds the number of this.TEXML Element's Ancestors and Siblings to the string containing the parents Ancestors and Siblings  i.e. #-#-#-# now
            pos += "-" + Ancestors() + "-" + SiblingsAfterMe();
            // returns the position as #-#-#-#
            return (pos);
        }

        /// <summary>
        /// Creates several Attributes for this.TEXML Element for Parent ID, Position, HasID and Hash Value and for each TEXML Element sibling
        /// </summary>
        /// <param name="PreferredID"></param>
        /// <param name="HasID"></param>
        /// <param name="HASHName"></param>
        public void AddHashAttributeList(string PreferredID = "ID", string HasID = "H_ID", string HASHName = "HASH")
        {
            // initializes a TEXML to this.TEXML Element
            TEXML xml = this;
            // iterate while the TEXML is not null
            while (!xml.IsNull)
            {
                // 
                xml.AddHashAttribute(PreferredID, HasID, HASHName);
                // sets the value of the TEXML to the next sibling
                xml = xml.NextSibling;
            }
        }

        /// <summary>
        /// Creates several Attributes for this.TEXML Element for Parent ID, Position, HasID and Hash Value
        /// </summary>
        /// <param name="PreferredID"></param>
        /// <param name="HasID"></param>
        /// <param name="HASHName"></param>
        public void AddHashAttribute(string PreferredID = "ID", string HasID = "H_ID", string HASHName = "HASH")
        {
            // initializes an empty string
            string md5Hash = "";
            // it is just to allow click compare button many times
            // unless, it gives an acception because ID attribute already exists
            string strVal = CalculateHashString(this, Name);
            // encodes the calculated Hash String into a Hash value that is readable
            md5Hash = Encode(strVal);

            //Gets the value of the XmlAttribute at the passed in PreferredID parameter
            string HID = Attribute(PreferredID);
            // checks if the value is null or empty
            if (string.IsNullOrEmpty(HID))
            {
                // sets the value of the string to the string representation of a new Guid
                HID = Guid.NewGuid().ToString();
                //HID = md5Hash;
            }

            //Creates a XmlAttribute for the passed in HasID parameter and newly created HID value
            Attribute(HasID, HID);
            // Creates a XmlAttribute for the passed in HASHName parameter and newly encoded hash value
            Attribute(HASHName, md5Hash);
            // initializes an empty string
            string ParentHID = "";
            // checks if this.TEXML Element's Parent property is not null
            if (Parent != null && !Parent.IsNull)
            {
                // gets the value of the TEXML Element's Parent for the HasID parameter
                ParentHID = Parent.Attribute(HasID);
                // checks if the value is null or empty
                if (string.IsNullOrEmpty(ParentHID))
                {
                    // sets the value to an empty string
                    ParentHID = "";
                }
            }
            // Creates a XmlAttribute for the "P_ID" and newly created ParentHID value
            Attribute("P_ID", ParentHID);
            // Creates a XmlAttribute for the "POS" and position of this.TEXML Element
            Attribute("POS", Position());

            // iterates through this.TEXML Element and each of it's siblings
            for (TEXML child = FirstChild; !child.IsNull; child = child.NextSibling)
            {
                // check if this.TEXML Element Name property matches the child's Name property
                if (child.Name == Name)
                {
                    // recursively calls the AddHashAttribute to add the Hash as an Attribute to the TEXML Element
                    child.AddHashAttribute(PreferredID, HasID, HASHName);
                }
            }
        }

        /// <summary>
        /// Concatenates the string value of the TEXML Element's attributes and each of it's siblings attributes 
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="BaseName"></param>
        /// <returns></returns>
        public string CalculateHashString(TEXML xml, string BaseName)
        {
            // initializes an empty string
            string strval = "";
            // checks the passed in TEXML Value property is not null
            if (xml.Value != null)
                // sets the initializes string as the Value of the passed in TEXML Element
                strval = xml.Value;
            // creates a new list of string
            List<string> attributes = new List<string>();
            // checks if the passed in TEXML Element's XmlAttribute collection is not null
            if (xml.Element.Attributes != null)
            {
                // iterates through each XmlAttribute in the collection
                foreach (XmlAttribute attrb in xml.Element.Attributes)
                {
                    // Adds the XmlAttribute Name Property to the list of string
                    attributes.Add(attrb.Name);
                }
                // sorts the list alphabetically
                attributes.Sort();
                // iterates through each attribute name string in the list of string
                foreach (string attribName in attributes)
                {
                    // concatenates the Value property of each TEXML Element at the specified name into the initialized string
                    strval += xml.Element.Attributes[attribName].Value;
                }
            }

            // iterates through this.TEXML Element and it's siblings
            for (TEXML xmlChild = xml.FirstChild; !xmlChild.IsNull; xmlChild = xmlChild.NextSibling)
            {
                // checks if the TEXML Element's Name property does not equal the passed in BaseName parameter
                if (xmlChild.Name != BaseName)
                {
                    // recursively concatenates the initialized string value with each TEXML Elements attributes added to the string
                    strval += CalculateHashString(xmlChild, BaseName);
                }
            }
            // returns the string of all Attributes for a TEXML Element and it's siblings
            return (strval);
        }

        /// <summary>
        /// Converts a string into a byte array, encodes it into a hash value and then returns the value as a readable string
        /// </summary>
        /// <param name="xel"></param>
        /// <returns></returns>
        private string Encode(string xel)
        {
            // initialize a byte array
            Byte[] originalBytes;
            // initialize another byte array
            Byte[] encodedBytes;
            // initialize a MD5
            MD5 md5;

            // creates a new MD5CryptoServiceProvider
            md5 = new MD5CryptoServiceProvider();
            // Encodes a string into the ASCII Default ANSI Code Bytes
            originalBytes = ASCIIEncoding.Default.GetBytes(xel);
            // computes the hash value for the string as a byte array
            encodedBytes = md5.ComputeHash(originalBytes);

            //Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes);
        }

        /// <summary>
        /// Creates a string from a TEXML Element node which contains each of its parent elements up to the XmlDocument
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static string FindXPath(XmlNode node)
        {
            // creates a new StringBuilder object
            StringBuilder builder = new StringBuilder();
            // iterates while the XmlNode passed in is not null
            while (node != null)
            {
                // switchs on the XmlNode's NodeType property
                switch (node.NodeType)
                {
                    // If the NodeType is an Attribute
                    case XmlNodeType.Attribute:
                        // Insert the Name of the XmlNode's Name property into the first position of the StringBuilder
                        builder.Insert(0, "/@" + node.Name);
                        // assign the value of the XmlNode as the value of the XmlNode cast to a XmlAttribute's OwnerElement property
                        node = ((XmlAttribute)node).OwnerElement;
                        // break after completing the XmlAttribute
                        break;
                    // if the NodeType is an Element
                    case XmlNodeType.Element:
                        // Finds the index of the XmlNode cast to a XmlElement
                        int index = FindElementIndex((XmlElement)node);
                        // Inserts the XmlNode's name and index at the first position of the StringBuilder
                        builder.Insert(0, "/" + node.Name + "[" + index + "]");
                        // sets the value of the XmlNode to the XmlNode's ParentNode property
                        node = node.ParentNode;
                        // break after completing the XmlElement
                        break;
                    // if the NodeType is a Document
                    case XmlNodeType.Document:
                        // returns the value of the StringBuilder as a string
                        return builder.ToString();
                    default:
                        // else throws an exception to handle that no other NodeTypes are supported
                        throw new ArgumentException("Only elements and attributes are supported");
                }
            }
            // throws  exception if Node is outside of a Document
            throw new ArgumentException("Node was not in a document");
        }

        /// <summary>
        /// Creates a string from a TEXML Element node and attribute which contains each of its parent elements up to the XmlDocument
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        static string FindXPath(TEXML node, string attribute)
        {
            // create a new StringBuilder
            StringBuilder builder = new StringBuilder();
            // initializes a new String
            string strPath;
            // iterate through the TEXML node while it is not null, and the Element property is not null
            while (node != null && !node.IsNull && node.Element != null)
            {
                // switch statement on the Node Element's NodeType (current type of the node) property
                switch (node.Element.NodeType)
                {
                    // if the node type is an Element
                    case XmlNodeType.Element:
                        // check if the node has the specified attribute passed into the parameter
                        if (node.HasAttribute(attribute))
                        {
                            // create a string from the node's name, passed in attribute, and the Value of the Attribute at the parameter attribute Name
                            strPath = string.Format("/{0}[@{1}='{2}']", node.Name, attribute, node.Attribute(attribute));
                        }
                        // else if there are no Attributes with the specified name
                        else
                        {
                            // create a string from the node's Name property
                            strPath = string.Format("/{0}", node.Name);
                        }
                        // inserts the created string into the StringBuilder object at the first position
                        builder.Insert(0, strPath);
                        // assign the TEXML Element node as the Parent value  of the TEXML Element's Parent property
                        node = node.Parent;
                        // break after creating a string and a parent from the Element type
                        break;

                    // if the node type is a Document
                    case XmlNodeType.Document:
                        // returns the created StringBuilder to a string
                        return builder.ToString();
                    default:
                        // else we throw an Exception to catch some other node type being used
                        throw new ArgumentException("Only elements and attributes are supported");
                }
            }
            // return the created StringBuilder to a string
            return builder.ToString();
        }

        /// <summary>
        /// Returns the value of the Index for a XmlElement node
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        static int FindElementIndex(XmlElement element)
        {
            // initializes a XmlNode as the passed in XmlElement parameters ParentNode
            XmlNode parentNode = element.ParentNode;
            // checks if the XmlNode is a XmlDocument
            if (parentNode is XmlDocument)
            {
                // returns 1 for root if XmlDocument
                return 1;
            }
            // initializes a XmlElement by casting the XmlNode to a XmlElement
            XmlElement parent = (XmlElement)parentNode;
            // initializes an int
            int index = 1;
            // iterates through each XmlNode in the XmlElement's ChildNodes collection
            foreach (XmlNode candidate in parent.ChildNodes)
            {
                // checks if the XmlNode is a XmlElement and if the XmlNode's Name property matches the passed in XmlElement's Name property
                if (candidate is XmlElement && candidate.Name == element.Name)
                {
                    // checks if the XmlNode is equal to the passed in parameters XmlElement
                    if (candidate == element)
                    {
                        // returns the initialized int
                        return index;
                    }
                    // increments the int if they are not the same
                    index++;
                }
            }
            throw new ArgumentException("Couldn't find element within parent");
        }
    }
}
