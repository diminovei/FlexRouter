using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    public abstract class ControlProcessorBase<T> : IControlProcessor where T : class
    {
        protected readonly T AccessDescriptor;

        Dictionary<Connector, object> _connectors = new Dictionary<Connector, object>();
        
        protected ControlProcessorBase(DescriptorBase accessDescriptor)
        {
            AccessDescriptor = accessDescriptor as T;
            AssignedAccessDescriptorId = accessDescriptor.GetId();
        }
        bool IControlProcessor.IsAccessDesctiptorSuitable(DescriptorBase accessDescriptor)
        {
            return IsSuitable(accessDescriptor);
        }

        private static bool IsSuitable(DescriptorBase accessDescriptor)
        {
            return accessDescriptor is T;
        }

        //private Connector[] GetConnectors()
        //{
        //    return (AccessDescriptor as DescriptorBase).GetConnectors();
        //}
        public abstract string GetName();
        
        public abstract string[] GetUsedHardwareList();

        public abstract Assignment[] GetAssignments();

        public abstract void SetAssignment(Assignment assignment);

        protected string AssignedHardwareForSingle;
        protected int AssignedAccessDescriptorId;

        public int GetAssignedAccessDescriptor()
        {
            return AssignedAccessDescriptorId;
        }
        public int GetId()
        {
            return AssignedAccessDescriptorId;
        }

        public void SetId(int id)
        {
            AssignedAccessDescriptorId = id;
        }
        public void Save(XmlTextWriter writer)
        {
            SaveHeader(writer);
            SaveAdditionals(writer);
        }

        protected void SaveHeader(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Type", GetType().Name);
            writer.WriteAttributeString("AssignedAccessDescriptorId",
                AssignedAccessDescriptorId.ToString(CultureInfo.InvariantCulture));
        }

        protected virtual void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("AssignedHardware", AssignedHardwareForSingle);
        }

        public void Load(XPathNavigator reader)
        {
            LoadHeader(reader);
            LoadAdditionals(reader);
        }

        public void LoadHeader(XPathNavigator reader)
        {
            AssignedAccessDescriptorId = int.Parse(reader.GetAttribute("AssignedAccessDescriptorId", reader.NamespaceURI));
        }

        public virtual void LoadAdditionals(XPathNavigator reader)
        {
            AssignedHardwareForSingle = reader.GetAttribute("AssignedHardware", reader.NamespaceURI);
            AssignedHardwareForSingle = ControlProcessorHardware.FixForNewVersion(AssignedHardwareForSingle);
        }
    }
}
