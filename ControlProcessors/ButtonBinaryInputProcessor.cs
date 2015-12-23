using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    class ButtonBinaryInputProcessor : ControlProcessorBase<IDescriptorMultistateWithDefault>, ICollector
    {
        public ButtonBinaryInputProcessor(DescriptorBase accessDescriptor)
            : base(accessDescriptor)
        {
        }

        public override bool HasInvertMode()
        {
            return false;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(AssignmentForButton);
        } 

        /// <summary>
        /// Содержит список участвующего в формировании кода железа, а также текущие состояния кнопок
        /// </summary>
        private readonly SortedDictionary<string, bool> _usedHardware = new SortedDictionary<string, bool>();
        public void SetInvolvedHardwareWithCurrentStates(SortedDictionary<string, bool> usedHardwareWithStates)
        {
            _usedHardware.Clear();
            foreach (var usedHardwareWithState in usedHardwareWithStates)
            {
                _usedHardware.Add(usedHardwareWithState.Key, usedHardwareWithState.Value);
            }
        }
        public SortedDictionary<string, bool> GetInvolvedHardwareWithCurrentStates()
        {
            return _usedHardware;
        }
        public override string[] GetUsedHardwareList()
        {
            return _usedHardware.Keys.ToArray();
        }

        ///// <summary>
        ///// Словарь [код, stateId]. Если в словаре есть код, который сейчас набран кнопками - включаем указанный ConnectorId
        ///// </summary>
        //private readonly List<Assignment> _stateAssignments = new List<Assignment>();
        
        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareBinaryInput);
        }

        /// <summary>
        /// Получить id состояния, которое нужно включить в AccessDescriptor
        /// </summary>
        /// <returns>id состояния. -1 - код не совпал ни с одним назначением</returns>
        private int GetActivatedStateId()
        {
            // Собираем код            
            var code = _usedHardware.Aggregate(string.Empty, (current, h) => current + (h.Value ? "1" : "0"));
            var activatedState = Connections.FirstOrDefault(accessDescriptorStateAssignment => accessDescriptorStateAssignment.GetAssignedHardware() == code);
            return activatedState == null ? -1 : activatedState.GetConnector().Id;
        }
        
        public void ProcessControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as ButtonEvent;
            if (ev == null)
                return;
            var hw = controlEvent.Hardware.GetHardwareGuid();
            
            // Если такое железо не назначено - прекращаем обработку
            if (!_usedHardware.ContainsKey(hw))
                return;

            _usedHardware[hw] = ev.IsPressed;

            if (!((DescriptorBase)AccessDescriptor).IsPowerOn())
                return;

            var stateId = GetActivatedStateId();
            if (stateId != -1)
            {
                AccessDescriptor.SetState(stateId);
            }
            else
            {
                AccessDescriptor.SetDefaultState();
            }
        }

        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteStartElement("UsedHardware");
            foreach (var b in _usedHardware)
            {
                writer.WriteStartElement("Hardware");
                writer.WriteAttributeString("Id", b.Key);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Connectors");
            foreach (var c in Connections)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", c.GetConnector().Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Order", c.GetConnector().Order.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", c.GetConnector().Name);
                writer.WriteAttributeString("AssignedCode", c.GetAssignedHardware());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            _usedHardware.Clear();
            Connections.Clear();
            var readerAdd = reader.Select("UsedHardware/Hardware");
            while (readerAdd.MoveNext())
            {
                var id = readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI);
                id = ControlProcessorHardware.FixForNewVersion(id);
                _usedHardware.Add(id, false);
            }
            readerAdd = reader.Select("Connectors/Connector");
            while (readerAdd.MoveNext())
            {
                var c = new Connector
                {
                    Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                    Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI)),
                    Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI),
                };
                var item = new Assignment();
                item.SetConnector(c);
                item.SetAssignedHardware(readerAdd.Current.GetAttribute("AssignedCode", readerAdd.Current.NamespaceURI));
                Connections.Add(item);
            }
        }
    }
}
