﻿using System.Collections.Generic;
using System.Linq;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    abstract class ControlProcessorMuitistateBase <T> : ControlProcessorBase<T> where T : class
    {
        protected ControlProcessorMuitistateBase(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        protected readonly List<ButtonInfo> _assignedHardware = new List<ButtonInfo>();
        public List<ButtonInfo> GetAssignedHardware()
        {
            return _assignedHardware;
        }

        public override Assignment[] GetAssignments()
        {
            var assignment = new List<Assignment>();
            var descriptor = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            foreach (var ah in _assignedHardware)
            {
                if (descriptor is DescriptorValue)
                {
                    if (ah.Id == ((DescriptorValue)descriptor).GetDefaultStateId())
                        continue;
                }
                var a = new Assignment
                {
                    StateId = ah.Id,
                    StateName = ah.Name,
                    AssignedItem = ah.AssignedHardware,
                    Inverse = ah.Invert
                };
                assignment.Add(a);
            }
            return assignment.ToArray();
        }
        public override void SetAssignment(Assignment assignment)
        {
            foreach (var ah in _assignedHardware)
            {
                if (ah.Id != assignment.StateId)
                    continue;
                ah.AssignedHardware = assignment.AssignedItem;
                ah.Invert = assignment.Inverse;
            }
        }
        public void SetInvertMode(int stateId, bool on)
        {
            foreach (var ah in _assignedHardware)
            {
                if (ah.Id != stateId)
                    continue;
                ah.Invert = on;
                return;
            }
        }
        public void AssignHardware(int stateId, string hardwareGuid)
        {
            var hw = _assignedHardware.FirstOrDefault(ah => ah.Id == stateId);
            if (hw != null)
                hw.AssignedHardware = hardwareGuid;
        }
        /// <summary>
        /// После редактирования AccessDescriptor обновить информацию о состояниях
        /// </summary>
        public void RenewStatesInfo(IEnumerable<AccessDescriptorState> states)
        {
            // Ищем новые State'ы и добавляем, если такие есть
            // ToDo: не забыть сохранить из AccessDescriptor
            foreach (var s in states)
            {
                var found = false;
                foreach (var ah in _assignedHardware)
                {
                    if (!ah.CompareState(s))
                        continue;
                    ah.Order = s.Order;
                    ah.Name = s.Name;
                    found = true;
                    break;
                }
                if (found)
                    continue;
                var bi = new ButtonInfo {Id = s.Id, Name = s.Name, Order = s.Order, AssignedHardware = string.Empty};
                _assignedHardware.Add(bi);
            }
            // Ищем лишние назначения (State удалён) и удаляем их.
            // ToDo: не забыть сохранить из AccessDescriptor
            for (var i = _assignedHardware.Count - 1; i >= 0; i--)
            {
                if (!states.Any(s => _assignedHardware[i].Id == s.Id))
                    _assignedHardware.RemoveAt(i);
            }
        }
    }
}