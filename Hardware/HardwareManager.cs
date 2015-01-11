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
        /// Таймер для смены фазы поиска компонентов
        /// </summary>
        private static Timer _timer;

//        private static readonly object ObjectToLock = new object();
        static HardwareManager()
        {
            _timer = new Timer(_ => OnTimedEvent(null, null), null, 500, 500);
            AddHardwareClass(new ArccDevicesManager());
            AddHardwareClass(new JoystickDevicesManager());
            AddHardwareClass(new KeyboardDevicesManager());
            AddHardwareClass(new F3DevicesManager());
        }
        #region Поиск компонентов
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
            var ev = GetLastControlEvent(_contolInSearchGuid);
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
                if (hardware.ModuleType == HardwareModuleType.Indicator)
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
        static private void AddHardwareClass(IHardware hardware)
        {
            lock(hardware)
            {
                Hardwares.Add(hardware);
            }
        }
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
        public static int[] GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            foreach (var hardware in Hardwares)
            {
                if (hardware.GetConnectedDevices().Contains(cph.MotherBoardId))
                    return hardware.GetCapacity(cph, deviceSubType);
            }
            return null;
        }

        /// <summary>
        /// Получить входящие событиея (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        static public ControlEventBase[] GetIncomingEvents()
        {
            var ie = new List<ControlEventBase>();
            for (var i = 0; i < Hardwares.Count; i++)
            {
                var incomingEvents = Hardwares.ElementAt(i).GetIncomingEvents();
                if (incomingEvents == null)
                    continue;
                ie.AddRange(incomingEvents);

            }
            // Именно здесь, чтобы не добавлять FakeEvent в FakeEvent. Получение данных из FakeEvent позже
            foreach (var ev in ie)
            {
                SetLastControlEvent(ev);
                if (ev.Hardware.ModuleType == HardwareModuleType.Button || ev.Hardware.ModuleType == HardwareModuleType.Axis)
                    SoftDumpCache[ev.Hardware.GetHardwareGuid()] = ev;
            }
            
            // Получение данных из FakeEvent
            var fakeEvents = GetFakeIncomingEvents();
            ie.AddRange(fakeEvents);
            
            return ie.Count == 0 ? new ControlEventBase[0] : ie.ToArray();
        }
        /// <summary>
        /// Отправить на устройство исходящее событие
        /// </summary>
        /// <param name="outgoingEvent">Событие</param>
        static public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            if (outgoingEvent.Hardware.GetHardwareGuid() == _contolInSearchGuid)
                return;
            SetLastControlEvent(outgoingEvent);
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
                SetLastControlEvent(ev);
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
        private static readonly Dictionary<string, ControlEventBase> LastControlEvents = new Dictionary<string, ControlEventBase>();
        /// <summary>
        /// Мягкий дамп нужен для того, чтобы привести виртуальные тумблеры в соответствие с железячными сразу после загрузки самолёта
        /// А также поддерживать их в согласованном состоянии в случае, если пользователь переключает их в симуляторе мышью
        /// Сюда попадают только кнопки и оси
        /// Мягкий дамп делается раз в N секунд
        /// </summary>
        private static readonly Dictionary<string, ControlEventBase> SoftDumpCache = new Dictionary<string, ControlEventBase>();
        static private void SetLastControlEvent(ControlEventBase controlEvent)
        {
            LastControlEvents[controlEvent.Hardware.GetHardwareGuid()] = controlEvent;
        }
        static private ControlEventBase GetLastControlEvent(string controlGuid)
        {
            if (string.IsNullOrEmpty(controlGuid))
                return null;
            return !LastControlEvents.ContainsKey(controlGuid) ? null : LastControlEvents[controlGuid];
        }

        public static void ResendLastControlEvent(string controlGuid)
        {
            var controlEvent = GetLastControlEvent(controlGuid);
            if (controlEvent == null)
                return;
            if (controlEvent.Hardware.ModuleType != HardwareModuleType.Axis &&
                controlEvent.Hardware.ModuleType != HardwareModuleType.Button)
                return;
            lock (IncomingEvents)
            {
                IncomingEvents.Enqueue(controlEvent);
            }
        }

        /// <summary>
        /// Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        public static ControlEventBase[] GetFakeIncomingEvents()
        {
            var ie = new List<ControlEventBase>();
            lock (IncomingEvents)
            {
                while (IncomingEvents.Count > 0)
                    ie.Add(IncomingEvents.Dequeue());
            }
            return ie.ToArray();
        }
        /// <summary>
        /// Получить актуальное состояние всех кнопок и осей
        /// </summary>
        /// <returns>Список событий</returns>
        public static ControlEventBase[] SoftDump()
        {
            var eventsArray = new List<ControlEventBase>();
            eventsArray.AddRange(SoftDumpCache.Values);
            return eventsArray.ToArray();
        }
        #endregion
    }
}
