using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ProfileItems
{
    public class Panel
    {
        public string GetNameOfProfileItemType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderPanel);
        }
        public int Id;
        public string Name;
        private string _powerFormula;

        private Panel(int id)
        {
            Id = id;
        }
        public Panel()
        {
            Id = GlobalId.GetNew();
        }
        public string GetPowerFormula()
        {
            return _powerFormula;
        }
        public void SetPowerFormula(string powerFormula)
        {
            _powerFormula = powerFormula;
        }
        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Panel");
            writer.WriteAttributeString("Id", Id.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("PowerFormula", GetPowerFormula());
            writer.WriteEndElement();
            writer.WriteString("\n");
        }
        public static Panel Load(XPathNavigator reader)
        {
            var id = int.Parse(reader.GetAttribute("Id", reader.NamespaceURI));
            var panel = new Panel(id);
            GlobalId.RegisterExisting(id);
            panel.Name = reader.GetAttribute("Name", reader.NamespaceURI);
            var powerFormula = reader.GetAttribute("PowerFormula", reader.NamespaceURI);
            panel.SetPowerFormula(powerFormula);
            return panel;
        }
    }
}
