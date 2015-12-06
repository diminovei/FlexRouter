using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.EditorsUI.Helpers
{
    interface IControlProcessorEditor
    {
        void OnNewControlEvent(ControlEventBase controlEvent);

    }
}
