using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FlexRouter
{
    /// <summary>
    /// Класс хранения настроек приложения
    /// </summary>
    internal static class ApplicationSettings
    {
        private static readonly string SettingsLocation;

        static ApplicationSettings()
        {
            SettingsLocation = Assembly.GetEntryAssembly().Location + ".ini";
        }

        public static string DefaultProfile { get; set; }
        public static string DefaultLanguage { get; set; }
        public static bool ControlsSynchronizationIsOff { get; set; }
        public static bool JoystickBindByInstanceGuid { get; set; }

        public static bool LoadSettings()
        {
            if (!File.Exists(SettingsLocation))
                return true;

            try
            {
                var xp = new XPathDocument(SettingsLocation);
                var nav = xp.CreateNavigator();

                var headerNav = nav.Select("/FlexRouterProfile");
                headerNav.MoveNext();
                var type = headerNav.Current.GetAttribute("Type", headerNav.Current.NamespaceURI);
                if (type != "Settings")
                    return false;

                var settingsNav = headerNav.Current.SelectChildren("Settings", headerNav.Current.NamespaceURI);
                settingsNav.MoveNext();

                DefaultProfile = settingsNav.Current.GetAttribute("DefaultProfile", settingsNav.Current.NamespaceURI);
                DefaultLanguage = settingsNav.Current.GetAttribute("DefaultLanguage", settingsNav.Current.NamespaceURI);
                ControlsSynchronizationIsOff = settingsNav.Current.GetAttribute("ControlsSynchronizationIsOff", settingsNav.Current.NamespaceURI).ToLower().Contains("true");
                JoystickBindByInstanceGuid = settingsNav.Current.GetAttribute("JoystickBindByInstanceGuid", settingsNav.Current.NamespaceURI).ToLower().Contains("true");
                
                //RepeaterFirstPause =
                //    uint.TryParse(
                //        settingsNav.Current.GetAttribute("RepeaterFirstPause", settingsNav.Current.NamespaceURI),
                //        out repeater)
                //        ? repeater
                //        : DefaultRepeaterFirstPause;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void SaveSettings()
        {
            try
            {
                using (var sw = new StringWriter())
                {
                    using (var writer = new XmlTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;
                        writer.WriteStartDocument();
                        writer.WriteStartElement("FlexRouterProfile");
                        writer.WriteAttributeString("Type", "Settings");
                        writer.WriteAttributeString("Name", "Settings");
                        writer.WriteStartElement("Settings");
                        writer.WriteAttributeString("DefaultProfile", DefaultProfile);
                        writer.WriteAttributeString("DefaultLanguage", DefaultLanguage);
                        writer.WriteAttributeString("ControlsSynchronizationIsOff", ControlsSynchronizationIsOff.ToString());
                        writer.WriteAttributeString("JoystickBindByInstanceGuid", JoystickBindByInstanceGuid.ToString());
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    if (File.Exists(SettingsLocation))
                        File.Copy(SettingsLocation, SettingsLocation + ".bak", true);
                    using (var swToDisk = new StreamWriter(SettingsLocation, false, Encoding.Unicode))
                    {
                        var parsedXml = XDocument.Parse(sw.ToString());
                        swToDisk.Write(parsedXml.ToString());
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
