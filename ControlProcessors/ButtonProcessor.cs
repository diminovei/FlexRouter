using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    class ButtonProcessor : ControlProcessorMuitistateBase<IDescriptorMultistate>, ICollector, IControlProcessorMultistate, IRepeater
    {
        private int _lastStateId = -1;
        private bool _lastStatePeriod;
        public ButtonProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        /// <summary>
        /// Эмуляция "Toggle" будет работать для всех кнопок сразу
        /// </summary>
        private bool _emulateToggle;

        /// <summary>
        /// Идентификатор AccessDescriptor'а, которым управляет ControlProcessor
        /// </summary>
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareButton);
        }
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("EmulateToggle", _emulateToggle.ToString());
            writer.WriteStartElement("Connectors");
            foreach (var buttonInfo in AssignedHardware)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", buttonInfo.Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Order", buttonInfo.Order.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", buttonInfo.Name);
                writer.WriteAttributeString("Invert", buttonInfo.Invert.ToString());
                writer.WriteAttributeString("AssignedHardware", buttonInfo.AssignedHardware);
                writer.WriteEndElement();
                writer.WriteString("\n");
            }
            writer.WriteEndElement();
            writer.WriteString("\n");
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            _emulateToggle = bool.Parse(reader.GetAttribute("EmulateToggle", reader.NamespaceURI));
            AssignedHardware.Clear();
            var readerAdd = reader.Select("Connectors/Connector");
            while (readerAdd.MoveNext())
            {
                var item = new ButtonInfo
                {
                    Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                    Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI)),
                    Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI),
                    Invert = bool.Parse(readerAdd.Current.GetAttribute("Invert", readerAdd.Current.NamespaceURI)),
                    AssignedHardware = ControlProcessorHardware.FixForNewVersion(readerAdd.Current.GetAttribute("AssignedHardware", readerAdd.Current.NamespaceURI))
                };
                AssignedHardware.Add(item);
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
            var button = AssignedHardware.FirstOrDefault(hw => hw.AssignedHardware == hardwareId);
            if (button == null)
                return;

            if (!((DescriptorBase)AccessDescriptor).IsPowerOn())
                return;

            var direction = button.Invert ? !ev.IsPressed : ev.IsPressed;
            if (_emulateToggle)
            {
                var action = button.Toggle(direction);
                if (action == ToggleState.MakeOn)
                {
                    AccessDescriptor.SetState(button.Id);
                    _lastStateId = button.Id;
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
                    AccessDescriptor.SetState(button.Id);
                    _lastStateId = button.Id;
                    button.IsOn = true;
                }
                else
                {
                    button.IsOn = false;
                    _lastStateId = -1;
                }
            }
            // Для дампа кнопок с первого раза (без AllKeys, затем PressedKeysOnly) нужно игнорировать события отжатых кнопок, если одна из кнопок уже нажата
            if (AssignedHardware.Any(bi => bi.IsOn)) 
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
