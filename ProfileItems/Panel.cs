using System;
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
        public Guid Id;
        public string Name;
        private string _powerFormula;
        /// <summary>
        /// Конструктор, вызывамый при загрузке панели из профиля
        /// </summary>
        /// <param name="id">идентификатор панели</param>
        private Panel(Guid id)
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

        public Guid GetId()
        {
            return Id;
        }
        public void SetId(Guid id)
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
            writer.WriteAttributeString("Id", Id.ToString());
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("PowerFormula", GetPowerFormula());
            writer.WriteEndElement();
            writer.WriteString("\n");
        }
        public static Panel Load(XPathNavigator reader)
        {
            Guid id;
            if (!Guid.TryParse(reader.GetAttribute("Id", reader.NamespaceURI), out id))
            {

                // ToDo: удалить
                id = GlobalId.Register(ObjType.Panel, int.Parse(reader.GetAttribute("Id", reader.NamespaceURI)));
            }
            var panel = new Panel(id) {Name = reader.GetAttribute("Name", reader.NamespaceURI)};
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
