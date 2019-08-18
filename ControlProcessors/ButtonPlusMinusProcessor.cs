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
    class ButtonPlusMinusProcessor : CollectorBase<IDescriptorPrevNext>, ICollector, IRepeater
    {
        public ButtonPlusMinusProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public override bool HasInvertMode()
        {
            return true;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(Assignment);
        } 

        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareButtonPlusMinus);
        }
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteStartElement("Connectors");
            foreach (var c in Connections)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", c.GetConnector().Id.ToString(CultureInfo.InvariantCulture));
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
            Connections.Clear();
            var readerAdd = reader.Select("Connectors/Connector");
            while (readerAdd.MoveNext())
            {
                var c = new Connector();
                c.Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI));
                c.Order = c.Id;
                c.Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI);
                var a = new AssignmentForButton();
                a.SetInverseState(bool.Parse(readerAdd.Current.GetAttribute("Invert", readerAdd.Current.NamespaceURI)));
                a.SetAssignedHardware(ControlProcessorHardware.FixForNewVersion(readerAdd.Current.GetAttribute("AssignedHardware", readerAdd.Current.NamespaceURI)));
                a.SetConnector(c);
                Connections.Add(a);
            }
        }
        internal enum LastState
        {
            None,
            Prev,
            Next
        };
        private LastState _lastState = LastState.None;
        private int _repeatsPerTimeCounter;
        private readonly int[] _repeatsPerTime =
        {
            0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 3, 3, 5, 5, 5, 5, 5, 5, 5, 10
        };
        private void SetRepeaterState(LastState repeaterState)
        {
            _lastState = repeaterState;
            if (repeaterState == LastState.None)
                _repeatsPerTimeCounter = 0;
        }
        protected override void OnNewControlEvent(ControlEventBase controlEvent)
        {
            var ev = (ButtonEvent)controlEvent;

            // AD при изменении состава State'ов нотифицирует об этом CP
            // Проверить, существует ли всё ещё такой ID контрола в AccessDescriptor
            // Как получить все состояния при загрузке ControlProcessor? Вызвать функцию в AccessDescriptor?
            var hardwareId = controlEvent.Hardware.GetHardwareGuid();
            var button = Connections.FirstOrDefault(hw => hw.GetAssignedHardware() == hardwareId);

            var direction = button.GetInverseState() ? !ev.IsPressed : ev.IsPressed;
            if (!direction)
            {
                SetRepeaterState(LastState.None);
                return;
            }

            if (button.GetConnector().Id == 1)
            {
                AccessDescriptor.SetNextState(1);
                SetRepeaterState(LastState.Next);
            }

            else
            {
                AccessDescriptor.SetPreviousState(1);
                SetRepeaterState(LastState.Prev);
            }
        }

        protected override void OnTick()
        {
            if (!(AccessDescriptor is IRepeaterInDescriptor))
                return;
            bool repeaterIsOn = ((IRepeaterInDescriptor)AccessDescriptor).IsRepeaterOn();
            if (!repeaterIsOn || _lastState == LastState.None)
                return;
            if (_repeatsPerTime[_repeatsPerTimeCounter] == 0)
            {
                _repeatsPerTimeCounter++;
                return;
            }

            if (_lastState == LastState.Next)
            {
                AccessDescriptor.SetNextState(_repeatsPerTime[_repeatsPerTimeCounter]);
            }
            else
            {
                AccessDescriptor.SetPreviousState(_repeatsPerTime[_repeatsPerTimeCounter]);
            }
            if (_repeatsPerTimeCounter < _repeatsPerTime.Length - 1)
                _repeatsPerTimeCounter++;
        }

        protected override bool IsControlEventSuitable(ControlEventBase controlEvent)
        {
            var ev = controlEvent as ButtonEvent;
            if (ev == null)
                return false;

            if (controlEvent.IsSoftDumpEvent)
                return false;
            // AD при изменении состава State'ов нотифицирует об этом CP
            // Проверить, существует ли всё ещё такой ID контрола в AccessDescriptor
            // Как получить все состояния при загрузке ControlProcessor? Вызвать функцию в AccessDescriptor?
            var hardwareId = controlEvent.Hardware.GetHardwareGuid();
            var button = Connections.FirstOrDefault(hw => hw.GetAssignedHardware() == hardwareId);
            if (button == null)
                return false;
            return true;
        }

        protected override bool IsNeedToRepeatControlEventOnPowerOn()
        {
            return false;
        }

        public void RenewStatesInfo(IEnumerable<Connector> states)
        {
            throw new NotImplementedException();
        }
    }
}
