using System;
using System.Collections.Generic;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.F3
{
    class F3Device : IHardwareDevice
    {
        /// <summary>
        /// Антидребезг осей
        /// </summary>
        private readonly AxisDebouncer _axisDebouncer = new AxisDebouncer();
        /// <summary>
        /// Структура, описывающая материнскую плату F3/L3
        /// </summary>
        private f3ioAPI.BoardInfo_ _motherBoard;
        /// <summary>
        /// Указатель на инстанс материнской платы в библиотеке, работающей с F3/L3
        /// </summary>
        private IntPtr _motherBoardHandle;
        /// <summary>
        /// Очередь входящих сообщений
        /// </summary>
        private readonly Queue<ControlEventBase> _incomingEvents = new Queue<ControlEventBase>();
        /// <summary>
        /// Очередь исходящих сообщений
        /// </summary>
        private readonly Queue<ControlEventBase> _outgoingEvents = new Queue<ControlEventBase>();
        /// <summary>
        /// Флаг, указывающий потокам класса на необходимость завершения
        /// </summary>
        private volatile bool _quit;
        /// <summary>
        /// Поток, обрабатывающий входящие и исходящие события
        /// </summary>
        private readonly Thread _processEventsThread;
        /// <summary>
        /// Получить имя материнской платы
        /// </summary>
        /// <param name="motherBoard">структура с информацией о материнской плате</param>
        /// <returns></returns>
        public static string GetMotherboardId(f3ioAPI.BoardInfo_ motherBoard)
        {
            return motherBoard.Name + ":" + motherBoard.SN;
        }
        public F3Device(f3ioAPI.BoardInfo_ motherBoard)
        {
            _motherBoard = motherBoard;
            _processEventsThread = new Thread(ProcessEventsLoop) { IsBackground = true };
        }
        /// <summary>
        /// Количество модулей, подключенных к материнской плате F3/L3 (включая материнскую плату)
        /// </summary>
        public int DeviceCount;
        /// <summary>
        /// Общая информация обо всех модулях (имена и т.д)
        /// </summary>
        public f3ioAPI.DeviceGeneral[] GenInfo;
        /// <summary>
        /// Информация о модулях, отправляющий события (кнопки, оси)
        /// </summary>
        public f3ioAPI.DeviceIN[] InInfo;
        /// <summary>
        /// Информация о модулях, принимающих события (светодиоды, двигатели)
        /// В отличие от других элементов, аут представлен не в виде массива, т.к. информациолнный массив уже определен в классе f3ioAPI.DeviceOUT
        /// </summary>
        public f3ioAPI.DeviceOUT OutInfo;
        /// <summary>
        /// Соединиться с материнской платой и получить информацию о подключенных модулях
        /// </summary>
        /// <returns>true - успешно, false - не удалось соединиться</returns>
        public bool Connect()
        {
            try
            {
                //********************************** открываем устройство ********************************//
                if (!f3ioAPI.OpenBoard(_motherBoard.HIDname))
                    return false;
                _motherBoardHandle = f3ioAPI.mgetHandle();
                // загрузка конфигурации (8- число попыток , в случае неудачиной блокировки конфигурации
                if (!f3ioAPI.OpenConfiguration(8))
                    return false;
                DeviceCount = f3ioAPI.getMainInterfaceInfo(f3ioAPI.InterfaceInfo.nALL);
                    //получаем (из конфигурации) максимально возможное кол-во подключенных к контроллеру устройств, включая сам контроллер.
                GenInfo = new f3ioAPI.DeviceGeneral[DeviceCount];
                InInfo = new f3ioAPI.DeviceIN[DeviceCount];
                OutInfo = new f3ioAPI.DeviceOUT();

                for (var dev = 0; dev < DeviceCount; dev++) //обходим в цикле все подключения
                {
                    if (!f3ioAPI.connectionEnumerated((byte) dev)) 
                        continue;
                    GenInfo[dev].GetInfo(dev); //получаем общую информацию устройства
                    InInfo[dev].GetInfo(dev); //получаем информации о функциях ввода устройства
                }

                OutInfo.GetInfo(); //информацию по АУТу получаем скопом, по всем устройствам сразу
                
                f3ioAPI.CloseConfiguration(); //в обязательном порядке освобождаем конфигурацию

                _processEventsThread.Start();
                return true;
            }
            catch (Exception)
            {
                f3ioAPI.CloseConfiguration(); //в обязательном порядке освобождаем конфигурацию
                return false;
            }
        }
        /// <summary>
        /// Завершить работу с железом
        /// </summary>
        public void Disconnect()
        {
            _quit = true;
            _processEventsThread.Join();
            SendData();
        }
        /// <summary>
        /// Получить все новые входящие события, присланные железом
        /// </summary>
        /// <returns></returns>
        public ControlEventBase[] GetIncomingEvents()
        {
            if (_incomingEvents.Count == 0)
                return null;
            var ie = new List<ControlEventBase>();
            lock (_incomingEvents)
            {
                while (_incomingEvents.Count > 0)
                    ie.Add(_incomingEvents.Dequeue());
            }
            return ie.ToArray(); 
        }
        /// <summary>
        /// Отправить железу событие
        /// </summary>
        /// <param name="outgoingEvent">описание исходящего события</param>
        public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            lock (_outgoingEvents)
            {
               _outgoingEvents.Enqueue(outgoingEvent);
            }
        }
        /// <summary>
        /// Отправить железу массив событий
        /// </summary>
        /// <param name="outgoingEvents">массиы исходящих событий</param>
        public void PostOutgoingEvents(ControlEventBase[] outgoingEvents)
        {
            lock (_outgoingEvents)
            {
                foreach (var ev in outgoingEvents)
                {
                    if (ev.Hardware.MotherBoardId != GetMotherboardId(_motherBoard))
                        continue;
                    _outgoingEvents.Enqueue(ev);
                }
            }
        }
        /// <summary>
        /// Сдампить состояние всех кнопок
        /// </summary>
        /// <param name="allHardwareInUse">используемое в профиле железо</param>
        public void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            _keyFilter.Reset();
        }
        /// <summary>
        /// Sets a bit at a specified index in a signed integer
        /// </summary>
        /// <param name="value">The number to use</param>
        /// <param name="index">The bit index in the integer</param>
        /// <param name="bitValue">Bit value (on/off)</param>
        /// <returns>Returns the new value</returns>
        public static int SetBit(int value, int index, bool bitValue)
        {
            if (index < 0 || index >= sizeof (int)*8)
                throw new ArgumentOutOfRangeException();
            return bitValue ? value | (1 << index) : value & ~(1 << index);
        }
        /// <summary>
        /// ToDO:экспериментальный глобальных объект лочки. На случай, если f3io.dll не потокобезопасная
        /// </summary>
        public static object F3GlobalSyncRoot = new object();
        /// <summary>
        /// Циклическая функция для работы с железом (отправка и получение событий)
        /// </summary>
        private void ProcessEventsLoop()
        {
            while (true)
            {
                if (_quit)
                    return;
                lock (F3GlobalSyncRoot)
                {
                    for (uint i = 0; i < InInfo.Length; i++)
                    {
                        if (InInfo[i].AxisCount <= 0 && InInfo[i].ButtonsCount <= 0) 
                            continue;
                        InInfo[i].getData(_motherBoardHandle);
                        if (InInfo[i].AxisCount > 0)
                            ProcessAxisEvents(i);
                        if(InInfo[i].ButtonsCount > 0)
                            ProcessButtonsEvents(i);
                    }
                //if (_outgoingEvents.Count == 0)
                //{
                //    Thread.Sleep(2);
                //    continue;
                //}
                    SendData();
                }
                Thread.Sleep(100);
            }
        }

        private void SendData()
        {
            lock (_outgoingEvents)
            {
                while (_outgoingEvents.Count != 0)
                {
                    var ev = _outgoingEvents.Dequeue();
                    switch (ev.Hardware.ModuleType)
                    {
                        case HardwareModuleType.Indicator:
                        case HardwareModuleType.BinaryOutput:
                        {
                            var hEvent = ev as LampEvent;
                            if (hEvent == null)
                                break;
                            var currentData = OutInfo.device[ev.Hardware.ModuleId].Block[(int) (ev.Hardware.BlockId)].Data;
                            var bitToChange = (int) (ev.Hardware.ControlId);
                            OutInfo.device[ev.Hardware.ModuleId].Block[(int) (ev.Hardware.BlockId)].Data = (short) SetBit(currentData, bitToChange, hEvent.IsOn);
                            break;
                        }
                        case HardwareModuleType.SteppingMotor:
                        {
                            var hEvent = ev as SteppingMotorEvent;
                            if (hEvent == null)
                                break;
                            OutInfo.device[ev.Hardware.ModuleId].Block[(int) (ev.Hardware.BlockId)].Data = hEvent.Position;
                            break;
                        }
                    }
                }
                for (uint i = 0; i < OutInfo.device.Length; i++)
                {
                    if (OutInfo.device[i].BlockCount == 0)
                        continue;
                    OutInfo.device[i].SendData(_motherBoardHandle);
               }
            }
        }
        /// <summary>
        /// Обраборать входящие сообытия от кнопок
        /// </summary>
        private void ProcessButtonsEvents()
        {
            // для того чтобы не читать массивы состояния кнопок по всем подклбченным устройствам
            // можно воспользоваться чтением буфера "кнопочных" собыйтий
            // буфер достаточно короткий по размеру, для того чтобы быть переданным за 1 кадр ЮСБ передачи
            // при этом буфер содержит до 30 событий типа "Вкл" и "Выкл"
            // перед использованием буфера как правило производится полное чтение состояния всех кнопок
            // далее их состояние отслеживается с помощью буфера событий
            // В случае, если буфер читается недостаточно часто, может наступить его переполнение (свофство Overflow)
            // при этом затираются самые старые события. 
            // Как правило при этом необходимо выполнить полное чтение состояния всех кнопок (метод GetData())
            // Сам буфер (в контроллере) очищается при каждом его чтении.
            // таким образом, чтобы предотвратить его переполнение, нужно правильно подобрать (эксперементально) интервал его чтения
            // так чтобы между 2мя чтениями немогло произойти > 30 событий
            //_buttonEvents.getEvents(_motherBoardHandle);

//            lock (F3GlobalSyncRoot)
//            {
                var buttonEvents = new f3ioAPI.ButtonEvents();
                buttonEvents.getEvents(_motherBoardHandle);
                for (var i = 0; i < buttonEvents.EventsCount; i++)
                {
                    var ev = new ButtonEvent
                    {
                        Hardware =
                            new ControlProcessorHardware
                            {
                                ModuleType = HardwareModuleType.Button,
                                MotherBoardId = GetMotherboardId(_motherBoard),
                                ModuleId = buttonEvents.EventsData[i].nDev,
                                ControlId = buttonEvents.EventsData[i].nButt
                            },
                        IsPressed = buttonEvents.EventsData[i].State
                    };
                    System.Diagnostics.Debug.Print("St: {0}, Btn: {1}, Mod: {2}, Mb: {3}", ev.IsPressed, ev.Hardware.ControlId, ev.Hardware.ModuleId, GetMotherboardId(_motherBoard));
                    lock (_incomingEvents)
                        _incomingEvents.Enqueue(ev);
                }
//            }
        }
        private readonly KeysFilter _keyFilter = new KeysFilter();
        /// <summary>
        /// Обраборать входящие сообытия от кнопок
        /// </summary>
        /// <param name="deviceIndex">индекс устройства, с которого нужно обрабатывать события</param>
        private void ProcessButtonsEvents(uint deviceIndex)
        {
            for (uint i = 0; i < InInfo[deviceIndex].ButtonsCount; i++)
            {
                var ev = new ButtonEvent
                {
                    Hardware =
                        new ControlProcessorHardware
                        {
                            ModuleType = HardwareModuleType.Button,
                            MotherBoardId = GetMotherboardId(_motherBoard),
                            ModuleId = deviceIndex,
                            ControlId = i
                        },
                    IsPressed = InInfo[deviceIndex].butt[i]
                };
                if (!_keyFilter.IsNeedToProcessControlEvent(ev)) 
                    continue;
                System.Diagnostics.Debug.Print("St: {0}, Btn: {1}, Mod: {2}, Mb: {3}", ev.IsPressed, ev.Hardware.ControlId, ev.Hardware.ModuleId, ev.Hardware.MotherBoardId);
                lock (_incomingEvents)
                    _incomingEvents.Enqueue(ev);
            }
        }
        ///// <summary>
        ///// Последние состояния осей. Используется для подавления помех (фильтрация мелкого дребезга)
        ///// </summary>
        //private readonly Dictionary<string, ushort> _previousAxisValues = new Dictionary<string, ushort>();
        /// <summary>
        /// Обраборать входящие сообытия от осей
        /// </summary>
        /// <param name="deviceIndex">индекс устройства, с которого нужно обрабатывать события</param>
        private void ProcessAxisEvents(uint deviceIndex)
        {
            //const int threshold = 2;
            for (uint ax = 0; ax < InInfo[deviceIndex].AxisCount; ax++)
            {
                var controlEvent = new AxisEvent
                {
                    Hardware = new ControlProcessorHardware
                    {
                        ModuleType = HardwareModuleType.Axis,
                        MotherBoardId = GetMotherboardId(_motherBoard),
                        ModuleId = deviceIndex,
                        ControlId = ax
                    },
                    Position = InInfo[deviceIndex].axis[ax].value,
                    MinimumValue = 0,
                    MaximumValue = InInfo[deviceIndex].axis[ax].range
                };
                // 0.2% - это примерно 2 из 1000.
                if(!_axisDebouncer.IsNeedToProcessAxisEvent(controlEvent, 0.2))
                    continue;
                //var key = controlEvent.Hardware.GetHardwareGuid();
                //if (!_previousAxisValues.ContainsKey(key))
                //    _previousAxisValues.Add(key, controlEvent.Position);
                //// Фильтрация дребезга оси и пропуск значений, уже отправленных роутеру
                //if (controlEvent.Position >= _previousAxisValues[key] - threshold && controlEvent.Position <= _previousAxisValues[key] + threshold) 
                //    continue;

                lock (_incomingEvents)
                    _incomingEvents.Enqueue(controlEvent);
            }
        }
    }
}
