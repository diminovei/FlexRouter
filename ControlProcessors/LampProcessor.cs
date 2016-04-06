using System;
using System.Collections.Generic;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.ControlProcessors
{
    class LampProcessor : ControlProcessorBase<IBinaryOutputMethods>, IVisualizer
    {
        private bool _previousState;
        private bool _previousPowerState;

        public LampProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public override bool HasInvertMode()
        {
            return false;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(Assignment);
        } 

        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareBinaryOutput);
        }

        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return null;

            var ad = Profile.AccessDescriptor.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var lineState = ((DescriptorBinaryOutput)ad).GetLineState();
            var powerState = ad.IsPowerOn();
            if (lineState == _previousState && powerState == _previousPowerState)
                return null;
            _previousState = lineState;
            _previousPowerState = powerState;
            var ev = new LampEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware()),
                IsOn = _previousPowerState&&_previousState
            };
            return new List<ControlEventBase> {ev};
        }

        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return new ControlEventBase[0];
            var ev = new LampEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware()),
                IsOn = false
            };
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            _previousState = false;
            return new List<ControlEventBase> { ev };
        }
    }
}
