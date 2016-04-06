using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlexRouter.Hardware.Arcc;
using FlexRouter.Hardware.F3;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Hardware.Joystick;
using FlexRouter.Hardware.Keyboard;

namespace FlexRouter.Hardware
{
    static class HardwareManager
    {
        static private readonly List<IHardware> Hardwares = new List<IHardware>();
        /// <summary>
        /// В конструкторе добавляются классы железа, которое будет обрабатываться роутром
        /// </summary>
        static HardwareManager()
        {
            _searchTimer = new Timer(_ => OnTimedEvent(null, null), null, 500, 500);
            Hardwares.Add(new ArccDevicesManager());
            Hardwares.Add(new JoystickDevicesManager());
            Hardwares.Add(new KeyboardDevicesManager());
            Hardwares.Add(new F3DevicesManager());
        }
        #region Поиск компонентов
        /// <summary>
        /// Таймер для смены фазы поиска компонентов
        /// </summary>
// ReSharper disable once NotAccessedField.Local
        private static Timer _searchTimer;
        /// <summary>
        /// Guid компонента, который находится в поиске (индикатор, светодиод).
        /// Когда компонент в поиске, вывод на него от роутера игнорируется, компонент моргает, чтобы пользователь понимал, какой контрол он выбрал.
        /// </summary>
        private static string _contolInSearchGuid;
        /// <summary>
        /// Фаза поиска компоента: "погасить"-"показать"
        /// </summary>
        private static bool _searchPhase;
        /// <summary>
        /// Установить Guid компонента, участвующего в поиске
        /// </summary>
        /// <param name="hardwareGuid"></param>
        public static void SetComponentToSearch(string hardwareGuid)
        {
            StopComponentSearch();
            _contolInSearchGuid = hardwareGuid;
        }
        /// <summary>
        /// Прекратить поиск
        /// </summary>
        public static void StopComponentSearch()
        {
            if (string.IsNullOrEmpty(_contolInSearchGuid))
                return;
            ControlEventBase ev = null;
            if (SoftDumpCache.ContainsKey(_contolInSearchGuid))
                ev = SoftDumpCache[_contolInSearchGuid];
            if (ev == null)
            {
                var hardware = ControlProcessorHardware.GenerateByGuid(_contolInSearchGuid);
                if (hardware.ModuleType == HardwareModuleType.BinaryOutput)
                {
                    ev = new LampEvent
                    {
                        Hardware = hardware,
                        IsOn = false
                    };
                }
                if (hardware.ModuleType == HardwareModuleType.Indicator || hardware.ModuleType == HardwareModuleType.LedMatrixIndicator)
                {
                    ev = new IndicatorEvent
                    {
                        Hardware = hardware,
                        IndicatorText = "       "
                    };
                }
            }
            PostOutgoingSearchEvent(ev);
            _contolInSearchGuid = string.Empty;
        }
        /// <summary>
        /// Событие таймера при котором меняется фаза поиска (погасить контрол/включить контрол)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnTimedEvent(object sender, EventArgs e)
        {
            _searchPhase = !_searchPhase;
            if (string.IsNullOrEmpty(_contolInSearchGuid))
                return;
            var hardware = ControlProcessorHardware.GenerateByGuid(_contolInSearchGuid);
            if (hardware.ModuleType == HardwareModuleType.BinaryOutput)
            {
                var ev = new LampEvent
                {
                    Hardware = hardware,
                    IsOn = _searchPhase
                };
                PostOutgoingSearchEvent(ev);
            }
            if (hardware.ModuleType == HardwareModuleType.Indicator)
            {
                var ev = new IndicatorEvent
                {
                    Hardware = hardware,
                    IndicatorText = _searchPhase ? "-------" : "       "
                };
                PostOutgoingSearchEvent(ev);
            }
        }
        #endregion
        /// <summary>
        /// Подключить все устройства
        /// </summary>
        /// <returns></returns>
        static public bool Connect()
        {
            System.Diagnostics.Debug.Print("Connect");
            var result = true;
            foreach (var hardware in Hardwares)
            {
                if (!hardware.Connect())
                    result = false;
            }
            System.Diagnostics.Debug.Print("Connected");
            return result;
        }
        /// <summary>
        /// Отключить все устройства
        /// </summary>
        static public void Disconnect()
        {
            foreach (var hardware in Hardwares)
                hardware.Disconnect();
            
            IncomingEvents.Clear();
            SoftDumpCache.Clear();
        }
        static public void Dump(ControlProcessorHardware[] hardware)
        {
            foreach (var item in Hardwares)
                item.Dump(hardware);
        }
        /// <summary>
        /// Получить список подключенных устройств
        /// </summary>
        /// <returns>Список в формате id, имя</returns>
        static public string[] GetConnectedDevices()
        {
            var allDevices = new List<string>();
            foreach (var hardware in Hardwares)
            {
                var devices = hardware.GetConnectedDevices();
                if (devices != null)
                    allDevices.AddRange(devices);
            }
            return allDevices.ToArray();
        }

        /// <summary>
        /// Получить информацию о возможностях устройства
        /// </summary>
        /// <param name="cph"></param>
        /// <param name="deviceSubType"></param>
        public static Capacity GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            if (deviceSubType != DeviceSubType.Motherboard)
            {
                foreach (var hardware in Hardwares.Where(hardware => hardware.GetConnectedDevices().Contains(cph.MotherBoardId)))
                    return hardware.GetCapacity(cph, deviceSubType);
            }
            var capacity = new Capacity();
            foreach (var c in Hardwares.Select(hardware => hardware.GetCapacity(cph, deviceSubType)))
                capacity.Add(c);
            return capacity;
        }

        /// <summary>
        /// Получить входящие событиея (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        static public IEnumerable<ControlEventBase> GetIncomingEvents()
        {
            var incomingEventsToReturn = new List<ControlEventBase>();
            for (var i = 0; i < Hardwares.Count; i++)
            {
                var incomingEvents = Hardwares.ElementAt(i).GetIncomingEvents();
                if (incomingEvents == null)
                    continue;
                foreach (var ie in incomingEvents)
                {
                    if(ie == null)
                        continue;
                    incomingEventsToReturn.Add(ie);
                    SoftDumpCache[ie.Hardware.GetHardwareGuid()] = ie;
                }
            }
            return incomingEventsToReturn;
        }
        /// <summary>
        /// Отправить на устройство исходящее событие
        /// </summary>
        /// <param name="outgoingEvent">Событие</param>
        static public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            if (outgoingEvent.Hardware.GetHardwareGuid() == _contolInSearchGuid)
                return;
            SoftDumpCache[outgoingEvent.Hardware.GetHardwareGuid()] = outgoingEvent;
            foreach (var hardware in Hardwares)
                hardware.PostOutgoingEvent(outgoingEvent);
        }
        static public void PostOutgoingEvents(List<ControlEventBase> outgoingEvents)
        {
            var oe = new List<ControlEventBase>();
            foreach (var ev in outgoingEvents)
            {
                if (ev.Hardware.GetHardwareGuid() == _contolInSearchGuid)
                    continue;

                SoftDumpCache[ev.Hardware.GetHardwareGuid()] = ev;
                oe.Add(ev);
            }
            foreach (var hardware in Hardwares)
                hardware.PostOutgoingEvents(oe.ToArray());
        }
        /// <summary>
        /// Отправить на устройство исходящее событие
        /// </summary>
        /// <param name="outgoingEvent">Событие</param>
        static private void PostOutgoingSearchEvent(ControlEventBase outgoingEvent)
        {
            foreach (var hardware in Hardwares)
                hardware.PostOutgoingEvent(outgoingEvent);
        }
        #region Код, запоминающий состояния всех контролов и позволяющий отправить фэйковое сообщение, "сдампить" контролы
        private static readonly Queue<ControlEventBase> IncomingEvents = new Queue<ControlEventBase>();
        public static void ResendLastControlEvent(string hardwareGuid)
        {
            var controlEvent = SoftDumpCache.FirstOrDefault(x => x.Key == hardwareGuid).Value;
            if (controlEvent == null)
                return;
            if (controlEvent.Hardware.ModuleType != HardwareModuleType.Axis && controlEvent.Hardware.ModuleType != HardwareModuleType.Button)
                return;
            lock (IncomingEvents)
            {
                IncomingEvents.Enqueue(controlEvent);
            }
        }
        /// <summary>
        /// Мягкий дамп нужен для того, чтобы привести виртуальные тумблеры в соответствие с железячными сразу после загрузки самолёта
        /// А также поддерживать их в согласованном состоянии в случае, если пользователь переключает их в симуляторе мышью
        /// Сюда попадают только кнопки и оси
        /// Мягкий дамп делается раз в N секунд
        /// </summary>
        private static readonly Dictionary<string, ControlEventBase> SoftDumpCache = new Dictionary<string, ControlEventBase>();
        /// <summary>
        /// Получить актуальное состояние всех кнопок и осей
        /// </summary>
        /// <returns>Список событий</returns>
        public static ControlEventBase[] SoftDump()
        {
            lock (SoftDumpCache)
            {
                var eventsArray = new List<ControlEventBase>();
                eventsArray.AddRange(SoftDumpCache.Values.Where(ev =>ev.Hardware.ModuleType == HardwareModuleType.Button || ev.Hardware.ModuleType == HardwareModuleType.Axis));
                return eventsArray.ToArray();
            }
        }
        #endregion
    }
}
