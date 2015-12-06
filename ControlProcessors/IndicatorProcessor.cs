using System;
using System.Collections.Generic;
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
    class IndicatorProcessor : ControlProcessorBase<IIndicatorMethods>, IVisualizer
    {
        private string _previousIndicatorText;

        public IndicatorProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        protected override Type GetAssignmentsType()
        {
            return typeof(Assignment);
        } 

        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
        }
        public override bool HasInvertMode()
        {
            return false;
        }

        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return null;
            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var text = ((IIndicatorMethods) ad).GetIndicatorText();
            if (text == _previousIndicatorText)
                return null;
            _previousIndicatorText = text;
            var ev = new IndicatorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware()),
                IndicatorText = text,
            };
            return new List<ControlEventBase> { ev };
        }

        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return null;
            var ev = new IndicatorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware()),
                IndicatorText = "",
            };
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            _previousIndicatorText = "";
            return new List<ControlEventBase> { ev };
        }
    }
}
