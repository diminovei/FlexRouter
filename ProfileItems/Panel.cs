using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ProfileItems
{
    public class Panel
    {
        public string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderPanel);
        }
        public int Id;
        public string Name;
        private string _powerFormula;

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
        public void Load(XPathNavigator reader)
        {
            Id = int.Parse(reader.GetAttribute("Id", reader.NamespaceURI));
            GlobalId.RegisterExisting(Id);
            Name = reader.GetAttribute("Name", reader.NamespaceURI);
            var powerFormula = reader.GetAttribute("PowerFormula", reader.NamespaceURI);
            SetPowerFormula(powerFormula);
        }
    }
}
