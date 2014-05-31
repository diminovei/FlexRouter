using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    class ButtonPlusMinusProcessor : ControlProcessorMuitistateBase<IDescriptorPrevNext>, ICollector, IControlProcessorMultistate, IRepeater
    {
        public ButtonPlusMinusProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
            PrepareAssignmentsList();
        }

        readonly List<Assignment>_assignments = new List<Assignment>();

        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareButtonPlusMinus);
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
            var button = _assignments.FirstOrDefault(hw => hw.AssignedItem == hardwareId);
            if (button == null)
                return;
            if (!((DescriptorBase)AccessDescriptor).IsPowerOn())
                return;

            var direction = button.Inverse ? !ev.IsPressed : ev.IsPressed;
            if (!direction)
            {
                SetRepeaterState(LastState.None);
                return;
            }
                

            if (button.StateId == 1)
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
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("RepeaterIsOn", _repeaterIsOn.ToString());
            writer.WriteStartElement("Connectors");
            foreach (var buttonInfo in _assignments)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", buttonInfo.StateId.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", buttonInfo.StateName);
                writer.WriteAttributeString("Invert", buttonInfo.Inverse.ToString());
                writer.WriteAttributeString("AssignedHardware", buttonInfo.AssignedItem);
                writer.WriteEndElement();
                writer.WriteString("\n");
            }
            writer.WriteEndElement();
            writer.WriteString("\n");
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            _repeaterIsOn = bool.Parse(reader.GetAttribute("RepeaterIsOn", reader.NamespaceURI));
            AssignedHardware.Clear();
            var readerAdd = reader.Select("Connectors/Connector");
            while (readerAdd.MoveNext())
            {
                var item = new Assignment
                {
                    StateId = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                    StateName = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI),
                    Inverse = bool.Parse(readerAdd.Current.GetAttribute("Invert", readerAdd.Current.NamespaceURI)),
                    AssignedItem = readerAdd.Current.GetAttribute("AssignedHardware", readerAdd.Current.NamespaceURI)
                };
                _assignments[item.StateId] = item;
            }
        }
        public override Assignment[] GetAssignments()
        {
            return _assignments.ToArray();
        }

        public override void SetAssignment(Assignment assignment)
        {
            foreach (var t in _assignments.Where(t => t.StateId == assignment.StateId))
            {
                t.AssignedItem = assignment.AssignedItem;
            }
        }
        private void PrepareAssignmentsList()
        {
            var a = new Assignment
            {
                StateId = 0,
                StateName = "-",
                Inverse = false,
                AssignedItem = AssignedHardwareForSingle
            };
            _assignments.Add(a);
            var b = new Assignment
            {
                StateId = 1,
                StateName = "+",
                Inverse = false,
                AssignedItem = AssignedHardwareForSingle
            };
            _assignments.Add(b);
        }

        internal enum LastState
        {
            None,
            Prev,
            Next
        };
        private bool _repeaterIsOn;
        private LastState _lastState = LastState.None;
        private int _repeatsPerTimeCounter = 0;
        private int[] _repeatsPerTime =
        {
            0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 3, 3, 5, 5, 5, 5, 5, 5, 5, 10
        };
        private void SetRepeaterState(LastState repeaterState)
        {
            _lastState = repeaterState;
            if (repeaterState == LastState.None)
                _repeatsPerTimeCounter = 0;
        }
        public bool IsRepeaterOn()
        {
            return _repeaterIsOn;
        }
        public void EnableRepeater(bool on)
        {
            _repeaterIsOn = on;
            if (on)
            {
                _repeatsPerTimeCounter = 0;
                SetRepeaterState(LastState.None);
            }
                
        }
        public void Tick()
        {
            if (!_repeaterIsOn || _lastState == LastState.None)
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
    }
}
