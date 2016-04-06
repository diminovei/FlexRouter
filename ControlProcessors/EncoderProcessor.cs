using System;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    internal class EncoderProcessor : CollectorBase<IDescriptorPrevNext>, ICollector
    {
        public EncoderProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public override bool HasInvertMode()
        {
            return true;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(Assignment);
        } 

        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareEncoder);
        }

        protected override void OnNewControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as EncoderEvent;

            var direction = Connections[0].GetInverseState() ? !ev.RotateDirection : ev.RotateDirection;
            if (direction)
                AccessDescriptor.SetNextState(ev.ClicksCount);
            else
                AccessDescriptor.SetPreviousState(ev.ClicksCount);
        }

        protected override void OnTick()
        {
        }

        protected override bool IsControlEventSuitable(ControlEventBase controlEvent)
        {
            var ev = controlEvent as EncoderEvent;
            if (ev == null)
                return false;

            if (controlEvent.Hardware.GetHardwareGuid() != Connections[0].GetAssignedHardware())
                return false;
            return true;
        }

        protected override bool IsNeedToRepeatControlEventOnPowerOn()
        {
            return false;
        }
    }
}
