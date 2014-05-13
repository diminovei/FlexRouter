using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    interface IHardwareDevice
    {
        bool Connect();
        void Disconnect();
        ControlEventBase[] GetIncomingEvents();
        void PostOutgoingEvent(ControlEventBase outgoingEvent);
        void Dump(DumpMode dumpMode);
        void DumpModule(ControlProcessorHardware[] hardware);
    }
}
