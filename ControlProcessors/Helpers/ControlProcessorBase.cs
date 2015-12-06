using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    public abstract class ControlProcessorBase<T> : IControlProcessor where T : class
    {
        protected readonly T AccessDescriptor;

        public abstract bool HasInvertMode();
        protected ControlProcessorBase(DescriptorBase accessDescriptor)
        {
            AccessDescriptor = accessDescriptor as T;
            AssignedAccessDescriptorId = accessDescriptor.GetId();
            FillConnectors(accessDescriptor);
        }
        private void FillConnectors(DescriptorBase accessDescriptor)
        {
            var conn = accessDescriptor.GetConnectors(this);
            foreach (var c in conn)
            {
                var assignment = (IAssignment) Activator.CreateInstance(GetAssignmentsType());
                assignment.SetConnector(c);
                Connections.Add(assignment);
            }
        }

        bool IControlProcessor.IsAccessDesctiptorSuitable(DescriptorBase accessDescriptor)
        {
            return IsSuitable(accessDescriptor);
        }

        private static bool IsSuitable(DescriptorBase accessDescriptor)
        {
            return accessDescriptor is T;
        }

        public abstract string GetDescription();
        protected abstract Type GetAssignmentsType();
        /// <summary>
        /// Соединения коннекторов с железом
        /// </summary>
        protected List<IAssignment> Connections = new List<IAssignment>();
        public IAssignment[] GetAssignments()
        {
            return Connections.ToArray();
        }
        public void SetAssignment(IAssignment assignment)
        {
            for (var i = 0; i < Connections.Count; i++)
            {
                if (Connections[i].GetConnector().Id != assignment.GetConnector().Id)
                    continue;
                Connections[i] = assignment;
            }
        }
        /// <summary>
        /// После редактирования AccessDescriptor обновилась информацию о состояниях
        /// </summary>
        public void OnAssignmentsChanged()
        {
            var connectors = (AccessDescriptor as DescriptorBase).GetConnectors(this);
            var newConnections = new List<IAssignment>();
            foreach (var newConnector in connectors)
            {
                var found = false;
                foreach (var oldConnection in Connections)
                {
                    if(oldConnection.GetConnector().Id!=newConnector.Id)
                        continue;
                    oldConnection.SetConnector(newConnector);
                    newConnections.Add(oldConnection);
                    found = true;
                    break;
                }
                if (!found)
                {
                    var assignment = (IAssignment) Activator.CreateInstance(GetAssignmentsType());
                    assignment.SetConnector(newConnector);
                    newConnections.Add(assignment);
                }
            }
            Connections = newConnections;
        }
        public virtual string[] GetUsedHardwareList()
        {
            return Connections.Select(c => c.GetAssignedHardware()).ToArray();
        }
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
            writer.WriteStartElement("Connectors");
            foreach (var c in Connections)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", c.GetConnector().Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Order", c.GetConnector().Order.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", c.GetConnector().Name);
                writer.WriteAttributeString("Invert", c.GetInverseState().ToString());
                writer.WriteAttributeString("AssignedHardware", c.GetAssignedHardware());
                writer.WriteEndElement();
                writer.WriteString("\n");
            }
            writer.WriteEndElement();
            writer.WriteString("\n");
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
            // Фикс для старой версии профиля
            Connections[0].SetAssignedHardware(reader.GetAttribute("AssignedHardware", reader.NamespaceURI));
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
            {
                Connections.Clear();
                var readerAdd = reader.Select("Connectors/Connector");
                while (readerAdd.MoveNext())
                {
                    var c = new Connector
                    {
                        Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                        Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI)),
                        Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI)
                    };
                    var a = new AssignmentForButton();
                    a.SetInverseState(bool.Parse(readerAdd.Current.GetAttribute("Invert", readerAdd.Current.NamespaceURI)));
                    a.SetAssignedHardware(ControlProcessorHardware.FixForNewVersion(readerAdd.Current.GetAttribute("AssignedHardware", readerAdd.Current.NamespaceURI)));
                    a.SetConnector(c);
                    Connections.Add(a);
                }
            }
        }
    }
}
