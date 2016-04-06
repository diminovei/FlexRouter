using System;
using System.Drawing;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.VariableWorkerLayer
{
    public abstract class VariableBase : ProfileItemPrivacy, IVariable, ITreeItem, IITemWithId
    {
        public Guid Id { get; set; }
        public Guid PanelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        protected VariableBase()
        {
            Id = GlobalId.GetNew();
            PanelId = Guid.Empty;
        }
        
        public void Save(XmlTextWriter writer)
        {
            SaveHeader(writer);
            SaveAdditionals(writer);
        }

        public void SaveHeader(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Type", GetType().Name);
            writer.WriteAttributeString("Id", Id.ToString());
            writer.WriteAttributeString("PanelId", PanelId.ToString());
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
            Guid id;
            if (!Guid.TryParse(reader.GetAttribute("Id", reader.NamespaceURI), out id))
            {
                // ToDo: удалить
                id = GlobalId.Register(ObjType.Variable, int.Parse(reader.GetAttribute("Id", reader.NamespaceURI)));
            }
            Id = id;

            Guid panelId;
            if (!Guid.TryParse(reader.GetAttribute("PanelId", reader.NamespaceURI), out panelId))
            {
                panelId = GlobalId.GetByOldId(ObjType.Panel, int.Parse(reader.GetAttribute("PanelId", reader.NamespaceURI)));
            }
            PanelId = panelId;
            Name = reader.GetAttribute("Name", reader.NamespaceURI);
            Description = reader.GetAttribute("Description", reader.NamespaceURI);
        }
        public virtual void LoadAdditionals(XPathNavigator reader)
        {
        }

        public IVariable GetCopy()
        {
            var variable = (IVariable)MemberwiseClone();
            return variable;
        }

        public abstract string GetName();
        public abstract Bitmap GetIcon();
        public abstract bool IsEqualTo(object obj);

        public Guid GetId()
        {
            return Id;
        }

        public void SetId(Guid id)
        {
            Id = id;
        }
    }
}
