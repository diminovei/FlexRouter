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
        public string PowerFormula;
        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("Panel");
            writer.WriteAttributeString("Id", Id.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("PowerFormula", PowerFormula);
            writer.WriteEndElement();
            writer.WriteString("\n");
        }
        public void Load(XPathNavigator reader)
        {
            Id = int.Parse(reader.GetAttribute("Id", reader.NamespaceURI));
            GlobalId.RegisterExisting(Id);
            Name = reader.GetAttribute("Name", reader.NamespaceURI);
            PowerFormula = reader.GetAttribute("PowerFormula", reader.NamespaceURI);
        }
    }
}
