using System.Drawing;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.VariableWorkerLayer
{
    public abstract class VariableBase : IVariable, ITreeItem
    {
        public int Id { get; set; }
        public int PanelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        protected VariableBase()
        {
            Id = GlobalId.GetNew();
            PanelId = -1;
        }
        
        public void Save(XmlTextWriter writer)
        {
            SaveHeader(writer);
            SaveAdditionals(writer);
        }

        public void SaveHeader(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Type", GetType().Name);
            writer.WriteAttributeString("Id", Id.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("PanelId", PanelId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Description", Description);
        }
        public virtual void SaveAdditionals(XmlTextWriter writer)
        {

        }

        public void Load(XPathNavigator reader)
        {
            LoadHeader(reader);
            LoadAdditionals(reader);
        }

        public void LoadHeader(XPathNavigator reader)
        {
            Id = int.Parse(reader.GetAttribute("Id", reader.NamespaceURI));
            GlobalId.RegisterExisting(Id);
            PanelId = int.Parse(reader.GetAttribute("PanelId", reader.NamespaceURI));
            Name = reader.GetAttribute("Name", reader.NamespaceURI);
            Description = reader.GetAttribute("Description", reader.NamespaceURI);
        }
        public virtual void LoadAdditionals(XPathNavigator reader)
        {
        }

        public IVariable GetCopy()
        {
            var variable = (IVariable)MemberwiseClone();
            //variable.Id = GlobalId.GetNew();
            return variable;
        }

        public abstract string GetName();
        public abstract Bitmap GetIcon();
        public abstract bool IsEqualTo(object obj);
    }
}
