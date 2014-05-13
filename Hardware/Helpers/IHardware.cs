using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    interface IHardware
    {
        bool Connect();
        void Disconnect();
        IEnumerable<string> GetConnectedDevices();
        ControlEventBase[] GetIncomingEvents();
        void PostOutgoingEvent(ControlEventBase outgoingEvent);
        void Dump(DumpMode dumpMode);
        void DumpModule(ControlProcessorHardware[] hardware);
    }

}
