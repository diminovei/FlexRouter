using System.Collections.Generic;
using System.Linq;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    internal abstract class DeviceManagerBase : IHardware
    {
        /// <summary>
        ///     Список подключенных устройств
        /// </summary>
        protected readonly Dictionary<string, IHardwareDevice> Devices = new Dictionary<string, IHardwareDevice>();

        /// <summary>
        ///     Получить список подключенных устройств
        /// </summary>
        /// <returns>Список в формате id, имя</returns>
        public IEnumerable<string> GetConnectedDevices()
        {
            return Devices.Keys.ToArray();
        }

        /// <summary>
        /// Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        public virtual ControlEventBase[] GetIncomingEvents()
        {
            var ie = new List<ControlEventBase>();
            for (int i = 0; i < Devices.Count; i++)
            {
                ControlEventBase[] incomingEvents = Devices.ElementAt(i).Value.GetIncomingEvents();
                if (incomingEvents == null)
                    continue;
                ie.AddRange(incomingEvents);
            }
            return ie.Count == 0 ? null : ie.ToArray();
        }

        /// <summary>
        ///     Отправить на устройство исходящее событие
        /// </summary>
        /// <param name="outgoingEvent">Событие</param>
        public abstract void PostOutgoingEvent(ControlEventBase outgoingEvent);

        public abstract void PostOutgoingEvents(ControlEventBase[] outgoingEvents);

        public virtual void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            foreach (var device in Devices)
                device.Value.Dump(allHardwareInUse);
        }

        public abstract int[] GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType);

        /// <summary>
        ///     Подключить все устройства
        /// </summary>
        /// <returns></returns>
        public abstract bool Connect();

        /// <summary>
        ///     Отключить все устройства
        /// </summary>
        public virtual void Disconnect()
        {
            foreach (var device in Devices)
                device.Value.Disconnect();
            Devices.Clear();
        }
    }
}
