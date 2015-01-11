using System.Collections.Generic;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.ControlProcessors
{
    class LampProcessor : ControlProcessorSingleAssignmentBase<IBinaryOutputMethods>, IVisualizer
    {
        private bool _previousState;
        private bool _previousPowerState;

        public LampProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareBinaryOutput);
        }

        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;

            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var lineState = ((DescriptorBinaryOutput)ad).GetLineState();
            var powerState = ad.IsPowerOn();
            if (lineState == _previousState && powerState == _previousPowerState)
                return null;
            _previousState = lineState;
            _previousPowerState = powerState;
            var ev = new LampEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IsOn = _previousPowerState&&_previousState
            };
            // ToDo: добавить восстановление состояния, если лампа участвовала в поиске
            return new List<ControlEventBase> {ev};
        }

        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ev = new LampEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IsOn = false
            };
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            _previousState = false;
            return new List<ControlEventBase> { ev };
        }
    }
}
