using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;

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

        public ControlEventBase GetNewEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var lineState = ((DescriptorBinaryOutput) ad).GetLineState();
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
            return ev;
        }

        public ControlEventBase GetClearEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;
            var ev = new LampEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                IsOn = false
            };
            return ev;
        }
    }
}
