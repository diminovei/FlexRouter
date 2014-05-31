using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    internal class EncoderProcessor : ControlProcessorSingleAssignmentBaseWithInversion<IDescriptorPrevNext>, ICollector
    {
        public EncoderProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareEncoder);
        }

        public void ProcessControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as EncoderEvent;
            if (ev == null)
                return;

            if (controlEvent.Hardware.GetHardwareGuid() != AssignedHardwareForSingle)
                return;

            if (!((DescriptorBase)AccessDescriptor).IsPowerOn())
                return;

            var direction = GetInversion() ? !ev.RotateDirection : ev.RotateDirection;
            if (direction)
                AccessDescriptor.SetNextState(ev.ClicksCount);
            else
                AccessDescriptor.SetPreviousState(ev.ClicksCount);
        }
    }
}
