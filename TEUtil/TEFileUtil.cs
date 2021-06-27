using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TraceabilityEngine.Util
{
    /// <summary>
    /// Enum for the Possible Directories.
    /// </summary>
    public enum Directories
    {
        Base,
        Processing,
        Processed,
        Failed,
    };

    /// <summary>
    /// Utility Class with functions to help with File Management
    /// </summary>
    static public class TEFileUtil
    {
        private readonly static Object _locker = new object();
        public static string BaseDirectory = null;
        
        /// <summary>
        /// Get the path of the currently executing assembly;
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(codeBase))
                {
                    codeBase = BaseDirectory;
                }
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// Get's the directory path.
        /// </summary>
        /// <param name="dir">What Specific Directory in the Main Directory to Get.</param>
        /// <returns></returns>
        /// 
        static public string GetDirectory(Directories dir)
        {
            //string dirName = (string) Application.Current.Properties["DirectoryPath"];
            string dirName = BaseDirectory;
            if (dir == Directories.Processing)
            {
                dirName += "\\Processing";
            }
            else if (dir == Directories.Processed)
            {
                dirName += "\\Processed";
            }

            else if (dir == Directories.Failed)
            {
                dirName += "\\Failed";
            }
            return (dirName);
        }

        /// <summary>
        /// Returns a FileInfo Object based off of a FileName and the Directory of the Main Directory
        /// it should be in.
        /// </summary>
        /// <param name="FileName">Name of the file.</param>
        /// <param name="dir">What Local directory it is in.</param>
        /// <returns></returns>
        static public FileInfo GetFileInfo(string FileName, Directories dir)
        {
            string filePath = String.Format("{0}\\{1}", GetDirectory(dir), FileName);

            if (File.Exists(filePath))
            {
                return new FileInfo(filePath);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        static public string MoveFileTo(string fileName, string targetDir, Int64? version = null)
        {
            string target = "";
            lock (_locker)
            {
                if (Directory.Exists(targetDir) == false)
                {
                    Directory.CreateDirectory(targetDir);
                }
                FileInfo fi = new FileInfo(fileName);
                if (version.HasValue)
                {
                    string newName = fi.Name;
                    newName = newName.Replace(fi.Extension, "");
                    newName = newName + "_" + version.Value;
                    newName = newName + fi.Extension;
                    //target = targetDir + "\\" + fi.Name + "_"+version.Value;
                    target = targetDir + "\\" + newName;
                }
                else
                {
                    target = targetDir + "\\" + fi.Name;
                }

                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                System.IO.File.Move(fileName, target);
            }
            return target;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        static public string MoveFileTo(string fileName, string targetDir, string appendToFileName, Int64 version)
        {
            string target = "";
            lock (_locker)
            {
                if (Directory.Exists(targetDir) == false)
                {
                    Directory.CreateDirectory(targetDir);
                }
                FileInfo fi = new FileInfo(fileName);
                if (!string.IsNullOrEmpty(appendToFileName))
                {
                    string newName = fi.Name;
                    newName = newName.Replace(fi.Extension, "");
                    newName = newName + "_" + appendToFileName;
                    newName = newName + version;
                    newName = newName + fi.Extension;
                    target = targetDir + "\\" + newName;
                }
                else
                {
                    target = targetDir + "\\" + fi.Name;
                }

                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                System.IO.File.Move(fileName, target);
            }
            return target;
        }

        public static string RemoveIllegals(string filepath)
        {
            Regex illegals = new Regex(@"[<, >, :, "", /, \\, |, \?, \*]");
            return illegals.Replace(filepath, "_");
        }

        /*
        private static readonly string m_TestServerFileRootPath = @"C:\TRTFS\Testing";
        private static Dictionary<string, TEXML> m_CachedXMLFiles;

        public static TEXML GetServerXmlFile(string xmlFileName)
        {
            try
            {
                if(xmlFileName == null)
                {
                    throw new ArgumentNullException("Argument 'xmlFileName' == null.");
                }

                if(m_CachedXMLFiles == null)
                {
                    m_CachedXMLFiles = new Dictionary<string, TEXML>();
                }

                if(m_CachedXMLFiles.Keys.Contains(xmlFileName) && m_CachedXMLFiles[xmlFileName] != null)
                {
                    return m_CachedXMLFiles[xmlFileName];
                }

                if(!Directory.Exists(m_TestServerFileRootPath))
                {
                    Directory.CreateDirectory(m_TestServerFileRootPath);
                }

                string fullFilePath = Path.Combine(m_TestServerFileRootPath, xmlFileName);

                if(!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException("Could not find the xml data file. - " + fullFilePath);
                }

                using (StreamReader reader = new StreamReader(fullFilePath))
                {
                    m_CachedXMLFiles[xmlFileName] = TEXML.CreateFromString(reader.ReadToEnd());
                }

                return m_CachedXMLFiles[xmlFileName];
            }
            catch(Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
		*/
    }
}
