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
        ///     Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        public ControlEventBase[] GetIncomingEvents()
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

        public void Dump(DumpMode dumpMode)
        {
            foreach (var device in Devices)
                device.Value.Dump(dumpMode);
        }

        public virtual void DumpModule(ControlProcessorHardware[] hardware)
        {
            Dump(DumpMode.AllKeys);
        }

        /// <summary>
        ///     Подключить все устройства
        /// </summary>
        /// <returns></returns>
        public abstract bool Connect();

        /// <summary>
        ///     Отключить все устройства
        /// </summary>
        public void Disconnect()
        {
            foreach (var device in Devices)
                device.Value.Disconnect();
            Devices.Clear();
        }
    }
}
