using System.Drawing.Printing;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ProfileItems
{
    /// <summary>
    /// Панель (объединяющая сущность)
    /// </summary>
    public class Panel
    {
        public string GetNameOfProfileItemType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderPanel);
        }
        public int Id;
        public string Name;
        private string _powerFormula;
        /// <summary>
        /// Конструктор, вызывамый при загрузке панели из профиля
        /// </summary>
        /// <param name="id">идентификатор панели</param>
        private Panel(int id)
        {
            Id = id;
        }
        /// <summary>
        /// Конструктор, вызываемый при создании новой панели
        /// </summary>
        public Panel()
        {
            Id = GlobalId.GetNew();
        }

        public int GetId()
        {
            return Id;
        }
        public void SetId(int id)
        {
            Id = id;
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

        public Panel GetCopy()
        {
            var item = (Panel)MemberwiseClone();
            return item;
        }
    }
}
