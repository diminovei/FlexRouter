using System;
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
    class ButtonProcessor : ControlProcessorBase<IDescriptorMultistateWithDefault>, ICollector, IRepeater
    {
        private int _lastStateId = -1;
        private bool _lastStatePeriod;
        public ButtonProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public override bool HasInvertMode()
        {
            return true;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(AssignmentForButton);
        } 

        /// <summary>
        /// Эмуляция "Toggle" будет работать для всех кнопок сразу
        /// </summary>
        private bool _emulateToggle;


        /// <summary>
        /// Идентификатор AccessDescriptor'а, которым управляет ControlProcessor
        /// </summary>
        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareButton);
        }
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("EmulateToggle", _emulateToggle.ToString());
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
        public override void LoadAdditionals(XPathNavigator reader)
        {
            _emulateToggle = bool.Parse(reader.GetAttribute("EmulateToggle", reader.NamespaceURI));
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
        public void SetEmulateToggleMode(bool on)
        {
            _emulateToggle = on;
        }
        public bool GetEmulateToggleMode()
        {
            return _emulateToggle;
        }
        public void ProcessControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as ButtonEvent;
            if (ev == null)
                return;
            // AD при изменении состава State'ов нотифицирует об этом CP
            // Проверить, существует ли всё ещё такой ID контрола в AccessDescriptor
            // Как получить все состояния при загрузке ControlProcessor? Вызвать функцию в AccessDescriptor?
            var hardwareId = controlEvent.Hardware.GetHardwareGuid();
            var button = (AssignmentForButton) Connections.FirstOrDefault(hw => hw.GetAssignedHardware() == hardwareId);
            if (button == null)
                return;

            if (!((DescriptorBase)AccessDescriptor).IsPowerOn())
                return;

            var direction = button.GetInverseState() ? !ev.IsPressed : ev.IsPressed;
            if (_emulateToggle)
            {
                var action = button.Toggle(direction);
                if (action == ToggleState.MakeOn)
                {
                    AccessDescriptor.SetState(button.GetConnector().Id);
                    _lastStateId = button.GetConnector().Id;
                    button.IsOn = true;
                }
                    
                if (action == ToggleState.MakeOff)
                {
                    button.IsOn = false;

                }
            }
            else
            {
                if (direction)
                {
                    AccessDescriptor.SetState(button.GetConnector().Id);
                    _lastStateId = button.GetConnector().Id;
                    button.IsOn = true;
                }
                else
                {
                    button.IsOn = false;
                    _lastStateId = -1;
                }
            }
            // Для дампа кнопок с первого раза (без AllKeys, затем PressedKeysOnly) нужно игнорировать события отжатых кнопок, если одна из кнопок уже нажата
            if (Connections.Any(bi => ((AssignmentForButton)bi).IsOn)) 
                return;
            _lastStateId = -1;
            AccessDescriptor.SetDefaultState();
        }

        public void Tick()
        {
            if (!(AccessDescriptor is IRepeaterInDescriptor))
                return;
            var repeaterIsOn = ((DescriptorMultistateBase) AccessDescriptor).IsRepeaterOn();
            if (!repeaterIsOn || _lastStateId == -1)
                return;

            if (_lastStatePeriod == false)
            {
                AccessDescriptor.SetDefaultState();
            }
            else
            {
                AccessDescriptor.SetState(_lastStateId);
            }
            _lastStatePeriod = !_lastStatePeriod;
        }
    }
}
