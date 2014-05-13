using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace FlexRouter
{
    class Utils
    {
        public static bool AreStringsEqual(string one, string two)
        {
            if (one == string.Empty)
                one = null;
            if (two == string.Empty)
                two = null;
            return one == two;
        }
        public static int GetNewId(IDictionary collection)
        {
            var i = 0;
            while (true)
            {
                if (!collection.Contains(i))
                    return i;
                i++;
            }
        }
        public static bool IsNumeric(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
/*            foreach (var c in text)
                if ((c < '0' || c > '9') && c != '-' && c != '.' && c != ',')
                    return false;
            return true;*/
            var regex = new Regex(@"^-?\d*\.?\d*$"); //regex that matches disallowed text
            //            var regex = new Regex(@"^-?\[0-9.]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }

        public static bool IsHexNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            int offset;
            return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
        }

        static public string GetFullSubfolderPath(string subFolder)
        {
            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var folder = Path.Combine(location, subFolder);
            return folder;
        }
        static public Dictionary<string, string> GetXmlList(string subFolder, string fileMask, string mainKey, string profileType)
        {
            var profileList = new Dictionary<string, string>();
            var folder = GetFullSubfolderPath(subFolder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileList = Directory.GetFiles(folder, fileMask);

            foreach (var file in fileList)
            {
                try
                {
                    var xp = new XPathDocument(file);
                    var nav = xp.CreateNavigator();

                    var mainKeyNav = nav.Select("/" + mainKey);
                    if (!mainKeyNav.MoveNext())
                        continue;
                    var fileProfileType = mainKeyNav.Current.GetAttribute("Type", mainKeyNav.Current.NamespaceURI);
                    if (profileType != fileProfileType)
                        continue;
                    var value = mainKeyNav.Current.GetAttribute("Name", mainKeyNav.Current.NamespaceURI);
                    profileList.Add(value, file);
                }
                catch (Exception ex)
                {
                }
            }
            return profileList;
        }
    }
}
