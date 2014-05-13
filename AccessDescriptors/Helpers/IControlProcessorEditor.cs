using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.AccessDescriptors.Helpers
{
    interface IControlProcessorEditor
    {
        void OnNewControlEvent(ControlEventBase controlEvent);

    }
}
