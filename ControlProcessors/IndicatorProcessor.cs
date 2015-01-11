using System.Collections.Generic;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.ControlProcessors
{
    class IndicatorProcessor : ControlProcessorSingleAssignmentBase<IIndicatorMethods>, IVisualizer
    {
        private string _previousIndicatorText;

        public IndicatorProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
        }

        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var text = ((IIndicatorMethods) ad).GetIndicatorText();
            if (text == _previousIndicatorText)
                return null;
            _previousIndicatorText = text;
            var ev = new IndicatorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IndicatorText = text,
            };
            // ToDo: добавить восстановление текста, если индикатор участвовал в поиске
            return new List<ControlEventBase> { ev };
        }

        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ev = new IndicatorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IndicatorText = "",
            };
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            _previousIndicatorText = "";
            return new List<ControlEventBase> { ev };
        }
    }
}
