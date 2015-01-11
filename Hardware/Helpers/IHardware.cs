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
        void PostOutgoingEvents(ControlEventBase[] outgoingEvents);
        /// <summary>
        /// Сдампить клавиши, оси, ...
        /// </summary>
        /// <param name="allHardwareInUse">Все используемые в профиле контролы для железа, не понимающего общей команды Dump и дампящего помодульно (ARCC)</param>
        void Dump(ControlProcessorHardware[] allHardwareInUse);
        /// <summary>
        /// Получить информацию о возможностях модуля или контролов
        /// </summary>
        /// <param name="cph">Заполненная структура с информацией о модуле</param>
        /// <param name="deviceSubType">Тип модуля или контролов, о которых нужно получить информацию</param>
        /// <returns>Список идентификаторов модулей или линий Null - модуль или линии не поддерживаются.</returns>
        int[] GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType);
    }

}
