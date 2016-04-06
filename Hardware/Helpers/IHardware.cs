using System;
using System.Collections.Generic;
using System.Linq;
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
        Capacity GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType);
    }

    //interface ICapacity
    //{
        
    //}

    public enum CapacityResult
    {
        Ok,
        DeviceSubtypeIsNotSuitableForCurrentHardware,
        DeviceSubtypeIsNotConnected
    }

    public class Capacity
    {
        public string[] Names = {};
        public bool DeviceSubtypeIsNotSuitableForCurrentHardware;

        public void Add(Capacity capacity)
        {
            Names = Names.Concat(capacity.Names).ToArray();
            if (capacity.DeviceSubtypeIsNotSuitableForCurrentHardware && Names.Length == 0)
                DeviceSubtypeIsNotSuitableForCurrentHardware = true;
            if (!capacity.DeviceSubtypeIsNotSuitableForCurrentHardware && Names.Length != 0)
                DeviceSubtypeIsNotSuitableForCurrentHardware = false;
        }
    }

    //public class CapacityForMotherboard : ICapacity
    //{
    //    public string[] Motherboards;
    //}
    //public class CapacityForNotMotherboard : ICapacity
    //{
    //    public int[] Capacity;
    //}
}
