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
    class ButtonBinaryInputProcessor : ControlProcessorBase<IDescriptorMultistate>, ICollector, IControlProcessorMultistate
    {
       
        // ToDo: можно сделать Dictionary
        private class AccessDescriptorStateAssignment
        {
            internal AccessDescriptorState State = new AccessDescriptorState();
            internal string Assignment;
            public Assignment GetAsAssignment()
            {
                var assignment = new Assignment
                    {
                        AssignedItem = Assignment,
                        Inverse = false,
                        StateId = State.Id,
                        StateName = State.Name
                    };
                return assignment;
            }
        }

        public void SetUsedHardwareWithStates(SortedDictionary<string, bool> usedHardwareWithStates)
        {
            _usedHardware.Clear();
            foreach (var usedHardwareWithState in usedHardwareWithStates)
            {
                _usedHardware.Add(usedHardwareWithState.Key, usedHardwareWithState.Value);
            }
        }
        public SortedDictionary<string, bool> GetUsedHardwareWithStates()
        {
            return _usedHardware;
        }
        /// <summary>
        /// Сопоставление железа коду (ControlProcessorHardware - железо, bool - RotateDirection)
        /// Словарь [кнопка, состояние]. Все состояния дают код, на который срабатывает переключение
        /// </summary>
        private readonly SortedDictionary<string, bool> _usedHardware = new SortedDictionary<string, bool>();
        /// <summary>
        /// Словарь [код, stateId]. Если в словаре есть код, который сейчас набран кнопками - включаем указанный StateId
        /// </summary>
        private readonly List<AccessDescriptorStateAssignment> _stateAssignments = new List<AccessDescriptorStateAssignment>();

        
        public ButtonBinaryInputProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        public override string GetName()
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
            var activatedState = _stateAssignments.FirstOrDefault(accessDescriptorStateAssignment => accessDescriptorStateAssignment.Assignment == code);
            return activatedState == null ? -1 : activatedState.State.Id;
        }

        public override string[] GetUsedHardwareList()
        {
            return _usedHardware.Keys.ToArray();
        }

        public override Assignment[] GetAssignments()
        {
            return _stateAssignments.OrderBy(x => x.State.Order).Select(stateAssignment => stateAssignment.GetAsAssignment()).ToArray();
        }

        public override void SetAssignment(Assignment assignment)
        {
            foreach (var t in _stateAssignments.Where(t => t.State.Id == assignment.StateId))
            {
                t.Assignment = assignment.AssignedItem;
            }
        }

        public void RenewStatesInfo(IEnumerable<AccessDescriptorState> states)
        {
            // Обновляем изменившиеся состояния
            // Добавляем появившиеся состояния
            // ToDo: не забыть сохранить из AccessDescriptor
            var accessDescriptorStates = states as AccessDescriptorState[] ?? states.ToArray();
            foreach (var s in accessDescriptorStates)
            {
                var found = false;
                foreach (var ah in _stateAssignments)
                {
                    if (ah.State.Id != s.Id)
                        continue;
                    ah.State.Order = s.Order;
                    ah.State.Name = s.Name;
                    found = true;
                    break;
                }
                if (found)
                    continue;
                var sa = new AccessDescriptorStateAssignment {Assignment = string.Empty, State = s};
                _stateAssignments.Add(sa);
            }
            // Ищем лишние назначения (State удалён) и удаляем их.
            // ToDo: не забыть сохранить из AccessDescriptor
            for (var i = _stateAssignments.Count - 1; i >= 0; i--)
            {
                if(accessDescriptorStates.All(s => _stateAssignments[i].State.Id != s.Id))
                    _stateAssignments.RemoveAt(i);
            }
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
            foreach (var buttonInfo in _stateAssignments)
            {
                writer.WriteStartElement("Connector");
                writer.WriteAttributeString("Id", buttonInfo.State.Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Order", buttonInfo.State.Order.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", buttonInfo.State.Name);
                writer.WriteAttributeString("AssignedCode", buttonInfo.Assignment);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            _usedHardware.Clear();
            var readerAdd = reader.Select("UsedHardware/Hardware");
            while (readerAdd.MoveNext())
            {
                var id = readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI);
                _usedHardware.Add(id, false);
            }
            readerAdd = reader.Select("Connectors/Connector");
            while (readerAdd.MoveNext())
            {
                var item = new AccessDescriptorStateAssignment
                {
                    State =
                    {
                        Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                        Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI)),
                        Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI)
                    },
                    Assignment = readerAdd.Current.GetAttribute("AssignedCode", readerAdd.Current.NamespaceURI)
                };
                _stateAssignments.Add(item);
            }
        }
    }
}
