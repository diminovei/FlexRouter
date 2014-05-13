using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;

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

        public ControlEventBase GetNewEvent()
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
            return ev;
        }

        public ControlEventBase GetClearEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ev = new IndicatorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IndicatorText = "",
            };
            return ev;
        }
    }
}
