using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    interface IHardwareDevice
    {
        bool Connect();
        void Disconnect();
        ControlEventBase[] GetIncomingEvents();
        void PostOutgoingEvent(ControlEventBase outgoingEvent);
        void PostOutgoingEvents(ControlEventBase[] outgoingEvents);
        /// <summary>
        /// Сдампить клавиши, оси, ...
        /// </summary>
        /// <param name="allHardwareInUse">Все используемые в профиле контролы для железа, не понимающего общей команды Dump и дампящего помодульно (ARCC)</param>
        void Dump(ControlProcessorHardware[] allHardwareInUse);
    }
}
