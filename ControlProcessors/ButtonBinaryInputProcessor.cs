using System.Collections.Generic;
using System.Linq;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.ControlProcessors
{
    class ButtonBinaryInputProcessor : ControlProcessorBase<IDescriptorMultistate>, ICollector, IControlProcessorMultistate
    {
        // ToDo: можно сделать Dictionary
        private class AccessDescriptorStateAssignment
        {
            internal AccessDescriptorState State;
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
        /// <summary>
        /// Флаг включения режима сбора назначенного железа (все кнопки, которые будут участвовать в формировании кода, включающего состояние в описателе доступа)
        /// </summary>
        private bool _collectingAssignedHardwareModeOn;
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
            foreach (var s in states)
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
                if(!states.Any(s => _stateAssignments[i].State.Id == s.Id))
                    _stateAssignments.RemoveAt(i);
            }
        }

        public void SetInitializeUsedHardwareModeOn(bool isOn)
        {
            if(isOn)
                _usedHardware.Clear();
            _collectingAssignedHardwareModeOn = isOn;
        }

        public void ProcessControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as ButtonEvent;
            if (ev == null)
                return;
            var hw = controlEvent.Hardware.GetHardwareGuid();
            
            // Если режим сбора используемого железа - состояния не переключаем
/*            if (_collectingAssignedHardwareModeOn)
            {
                if (_usedHardware.ContainsKey(hw))
                    _usedHardware[hw] = ev.IsPressed;
                else
                    _usedHardware.Add(hw, ev.IsPressed);
                return;
            }*/
            // Если режим сбора используемого железа - состояния не переключаем
            if (_collectingAssignedHardwareModeOn)
            {
                _usedHardware[hw] = ev.IsPressed;
                return;
            }

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
    }
}
