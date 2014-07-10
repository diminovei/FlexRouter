using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        //public static bool MinimizeToTrayOnStart { get; set; }
        //public static uint RepeaterFirstPause { get; set; }
        //public static uint RepeaterNextPause { get; set; }
        //public static bool ShowAxisInfo { get; set; }
        //public static bool JoystickBindingType { get; set; }

        //public static readonly uint DefaultRepeaterFirstPause = 10;
        //public static readonly uint DefaultRepeaterNextPause = 0;

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

                //MinimizeToTrayOnStart =
                //    settingsNav.Current.GetAttribute("MinimizeToTrayOnStart", settingsNav.Current.NamespaceURI)
                //        .ToLower()
                //        .Contains("true");
                //JoystickBindingType =
                //    settingsNav.Current.GetAttribute("JoystickBindingType", settingsNav.Current.NamespaceURI)
                //        .ToLower()
                //        .Contains("true");
                //ShowAxisInfo =
                //    settingsNav.Current.GetAttribute("ShowAxisInfo", settingsNav.Current.NamespaceURI)
                //        .ToLower()
                //        .Contains("true");
                //uint repeater;
                //RepeaterFirstPause =
                //    uint.TryParse(
                //        settingsNav.Current.GetAttribute("RepeaterFirstPause", settingsNav.Current.NamespaceURI),
                //        out repeater)
                //        ? repeater
                //        : DefaultRepeaterFirstPause;
                //RepeaterNextPause =
                //    uint.TryParse(
                //        settingsNav.Current.GetAttribute("RepeaterNextPause", settingsNav.Current.NamespaceURI),
                //        out repeater)
                //        ? repeater
                //        : DefaultRepeaterNextPause;
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
            
            
            
            
/*            var writer = new XmlTextWriter(SettingsLocation, Encoding.Unicode);
            writer.WriteStartDocument();
            writer.WriteStartElement("FlexRouterProfile");
            writer.WriteAttributeString("Type", "Settings");
            writer.WriteAttributeString("Name", "Settings");
            writer.WriteStartElement("Settings");
            writer.WriteAttributeString("DefaultProfile", DefaultProfile);
            writer.WriteAttributeString("DefaultLanguage", DefaultLanguage);
            writer.WriteAttributeString("ControlsSynchronizationIsOff", ControlsSynchronizationIsOff.ToString());
            //writer.WriteAttributeString("JoystickBindingType", MinimizeToTrayOnStart.ToString());
            //writer.WriteAttributeString("ShowAxisInfo", ShowAxisInfo.ToString());
            //writer.WriteAttributeString("RepeaterFirstPause", RepeaterFirstPause.ToString());
            //writer.WriteAttributeString("RepeaterNextPause", RepeaterNextPause.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();*/
        }
    }
}
