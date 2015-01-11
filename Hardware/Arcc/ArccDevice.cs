using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.Arcc
{
    public class ArccDevice : IHardwareDevice
    {
        /// <summary>
        /// Для того, чтобы не вводить новую сущность и логику, BinaryInput воспринимается, как 28 кнопок.
        /// Максимальный Id модуля в ARCC - 255, так как в пакете занимает 1 байт.
        /// Для того, чтобы внутри этого класса отличать модуль бинарного ввода от модуля кнопок при получении данных и при дампе, ModuleId увеличивается на 256
        /// Таким образом, даже если Id двух разных модулей кнопок и бинарного ввода пересекаются, обрабатываться они будут корректно
        /// </summary>
        private const uint IncreaseModuleIdForBinaryInput = 256;
        /// <summary>
        /// Объект для синхронизации потоков
        /// </summary>
        private readonly object _quitSyncRoot = new object();
        /// <summary>
        /// Значение-метка для потоков, установленная в true говорит о том, что пора завершаться
        /// </summary>
        private bool _quit;
        /// <summary>
        /// Нить для разгребания очереди исходящих сообщений (периодической отсылки данных на лампы, индикаторы, ...)
        /// </summary>
        private readonly Thread _sendDataThread;
        /// <summary>
        /// Имя Com-порта, с которым соединена материнская плата ARCC
        /// </summary>
        private readonly string _comPort;
        /// <summary>
        /// id материнской платы
        /// </summary>
        public string MotherboardId { get; private set; }
        /// <summary>
        /// Объект Com-порт
        /// </summary>
        private SerialPort _port;
        /// <summary>
        /// Очередь входящих событий (кнопки, энкодеры, ...)
        /// </summary>
        private readonly Queue<ControlEventBase> _incomingEvents = new Queue<ControlEventBase>();
        /// <summary>
        /// Очередь исходящих событий (индикаторы, лампы, ...)
        /// </summary>
        private readonly Queue<ControlEventBase> _outgoingEvents = new Queue<ControlEventBase>();
        /// <summary>
        /// Последние состояния осей. Используется для подавления помех (фильтрация мелкого дребезга)
        /// </summary>
        private readonly Dictionary<uint, ushort[]> _axisValues = new Dictionary<uint, ushort[]>();
        /// <summary>
        /// Здесь сохраняются остатки пришедших данных, не кратные размеру пакета. Учитываются при следующем чтении данных из устройства
        /// </summary>
        private byte[] _prevBufferTail = new byte[0];
        /// <summary>
        /// В этом словаре запоминаются состояния всех кнопок модулей бинарного ввода, чтобы можно было понять, что изменилось с прошлого пакета данных от модуля
        /// </summary>
        readonly Dictionary<uint, string> _binaryInputModulesState = new Dictionary<uint, string>();
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="motherboardId">Идентификатор устройства для использования в роутере</param>
        /// <param name="comPort">Com-порт, с которым соединено устройство</param>
        public ArccDevice(string motherboardId, string comPort)
        {
            MotherboardId = motherboardId;
            _comPort = comPort;
            _sendDataThread = new Thread(SendDataLoop){IsBackground = true};
            //_dumpWatcherThread = new Thread(DumpLoop) { IsBackground = true };
        }
        /// <summary>
        /// Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        public ControlEventBase[] GetIncomingEvents()
        {
            if (_incomingEvents.Count == 0)
                return null;
            lock (_incomingEvents)
            {
                var copy = _incomingEvents.ToArray();
                _incomingEvents.Clear();
                return copy;
            }
        }

        public void PostOutgoingEvents(ControlEventBase[] outgoingEvents)
        {
            lock (_outgoingEvents)
            {
                foreach (var ev in outgoingEvents)
                {
                    if(ev.Hardware.MotherBoardId!=MotherboardId)
                        continue;
                    _outgoingEvents.Enqueue(ev);
                }    
            }
        }
        public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            lock (_outgoingEvents)
            {
                _outgoingEvents.Enqueue(outgoingEvent);
            }
        }
        /// <summary>
        /// Соединиться с устройством
        /// </summary>
        /// <returns>true - соединение прошло успешно</returns>
        public bool Connect()
        {
            if (_port == null)
            {
                _port = new SerialPort
                           {
                               BaudRate = 1250000,
                               PortName = _comPort,
                               Handshake = Handshake.None,
                               Parity = Parity.None,
                               DataBits = 8,
                               StopBits = StopBits.One,
                               RtsEnable = false,
                           };
                //(Проверка False, Bits = None)
                _port.DataReceived += SerialPortDataReceived;
                //                Port.BreakState
                //                Port.CDHolding
                //                Port.CtsHolding
                //                Port.DsrHolding
                //                Port.ParityReplace
                //                Port.PinChanged
                //                Port.RtsEnable
                //Контроль Dtr = Disable; (контроль чётности)
                //Контроль Rts = Disable;
                // FlowControl = None;
                // OutCtsFlow = False;
                // OutDsrFlow = False;
                // SyncMethod = ThreadSync; (бывает WindowSync, None)
                // Отключить все реакции, кроме Cts
                // После считывания буфера его нужно чистить (входной и выходной)
            }
            try
            {
                _isFirstPacket = true;
                _port.Open();
                _port.DtrEnable = true;
                Thread.Sleep(1000);
                _port.DtrEnable = false;
            }
            catch (Exception)
            {
                return false;
            }
            // Пока CtsHolding не установлен в true - устройство не готово к работе
            while (true)
            {
                if (_port.CtsHolding)
                    break;
                Thread.Sleep(100);
            }
            _quit = false;
            _sendDataThread.Start();
            return true;
        }
        /// <summary>
        /// Разорвать соединение с устройством
        /// </summary>
        public void Disconnect()
        {
            lock (_quitSyncRoot)
            {
                _quit = true;
            }
            _sendDataThread.Join();
            SendData();
            _port.DtrEnable = true;
            Thread.Sleep(1000);
            _port.DtrEnable = false;
            _port.Close();
            _port = null;
        }
        /// <summary>
        /// Переменная, обозначающая, что это первый пакет данных, принимаемый из порта
        /// </summary>
        private bool _isFirstPacket = true;
        /// <summary>
        /// Функция (callback) приёма данных от устройства
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var bytesToRead = _port.BytesToRead;
            if (bytesToRead == 0)
                return;
            lock (_incomingEvents)
            {
                    
                // Если это первый пакет, то локальная переменная isFirstPacketLocal будет проинициализирована в true только один раз, затем всегда false
                // В первом пакете иногда приходит, а иногда не приходит 1 байт со значением 0. Для отлова этой непостоянной ситуации и служит эта переменная
                var isFirstPacketLocal = _isFirstPacket;
                _isFirstPacket = false;
                    
                // Сначала в буфер помещаем "хвост", оставшийся от предыдущих пакетов, затем вновь пришедшие данные.
                // Хвост - остаток, не кратный 18 байтам (стандартная длина пакетов от модулей)
                var buffer = new byte[_prevBufferTail.Length + bytesToRead];
                _prevBufferTail.CopyTo(buffer, 0);
                _port.Read(buffer, _prevBufferTail.Length, bytesToRead);
                    
                // Если это первое получение данных их COM-порта с момента инициализации и самый первый байт равен нулю, то это не стандартный пакет ARCC, потому что первым байтом должен идти ID модуля. 0 - не является ID модуля.
                // 0 нужно проигнорировать. Так (вроде бы) железо сообщает о том, что готово к работе, но этот 0 приходит не всегда. Чаще при первом включении после подключения железа к USB.
                if (isFirstPacketLocal && buffer[0] == 0)
                {
                    if (buffer.Length == 1)
                        return;
                    var newArray = new byte[buffer.Length - 1];
                    Array.Copy(buffer, 1, newArray, 0, buffer.Length - 1);
                    buffer = newArray;
                }

                // Обрабатываем все имеющиеся в буфере пакеты. Длина каждого 18 байт
                const int blockLength = 18;
                var blocksReceived = buffer.Length / blockLength;
                for (var i = 0; i < blocksReceived; i++)
                {
                    var blockOffset = i == 0 ? i : i * blockLength;
                    if (buffer[blockOffset] == (byte) ArccModuleType.Button)
                        ProcessButtonEvent(buffer, blockOffset);
                    // Модуль бинарного ввода
                    if (buffer[blockOffset] == (byte)ArccModuleType.BinaryInput) // 8 - группа модулей бинарного ввода
                        ProcessBinaryInputEvent(buffer, blockOffset);

                    if (buffer[blockOffset] == (byte)ArccModuleType.Encoder)
                        ProcessEncoderEvent(buffer, blockOffset);

                    if (buffer[blockOffset] == (byte)ArccModuleType.Axis)
                        ProcessAxisEvent(buffer, blockOffset);
                }
                // Если размер данных в буфере кратен 18, значит мы обработали все пакеты
                if (buffer.Length%blockLength == 0)
                {
                    _prevBufferTail = new byte[0]; 
                    return;
                }
                // Если в буфере остались необработанные данные менее 18 байт, это значит, что остальная часть придёт в следущий раз
                // Сохраним остатки во временном буфере и в следующий раз склеим сохранённые данные с вновь пришедшими
                _prevBufferTail = new byte[buffer.Length % blockLength];
                Array.Copy(buffer, blocksReceived * blockLength, _prevBufferTail, 0, buffer.Length % blockLength);
            }
        }
        /// <summary>
        /// Обработать и добавить событие от модуля осей
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        private void ProcessAxisEvent(byte[] buffer, int blockOffset)
        {
            var controlEvent = new AxisEvent { Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Axis, ModuleId = buffer[blockOffset + 1], MotherBoardId = MotherboardId } };
            if (!_axisValues.ContainsKey(controlEvent.Hardware.ModuleId))
                _axisValues.Add(controlEvent.Hardware.ModuleId, new ushort[8]);

            const int threshold = 1;
            for (var j = 7; j >= 0; j--)
            {
                var value = buffer[blockOffset + 2 + j * 2] * 256 + buffer[blockOffset + 2 + j * 2 + 1];

                //if (_axisValues[controlEvent.Hardware.ModuleId][j] == value)
                //    continue;
                // Фильтрация дребезга оси
                if (value >= _axisValues[controlEvent.Hardware.ModuleId][j] - threshold &&
                    value <= _axisValues[controlEvent.Hardware.ModuleId][j] + threshold) continue;

                // Тест на дребезг пройден
                controlEvent.Hardware.ControlId = (uint)((uint)8 - j);
                controlEvent.Position = (ushort) value;
                controlEvent.MinimumValue = 0;
                controlEvent.MaximumValue = 1023;

                _incomingEvents.Enqueue(controlEvent);
                _axisValues[controlEvent.Hardware.ModuleId][j] = (ushort)value;
            }
        }
        /// <summary>
        /// Обработать и добавить событие от модуля энкодеров
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        private void ProcessEncoderEvent(byte[] buffer, int blockOffset)
        {
            var controlEvent = new EncoderEvent
            {
                Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Encoder, ControlId = buffer[blockOffset + 15], ModuleId = buffer[blockOffset + 1], MotherBoardId = MotherboardId },
                RotateDirection = (buffer[blockOffset + 17] == 1),
                ClicksCount = buffer[blockOffset + 16],
            };
            _incomingEvents.Enqueue(controlEvent);
        }
        /// <summary>
        /// Обработать и добавить событие от модуля бинарного ввода
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        private void ProcessBinaryInputEvent(byte[] buffer, int blockOffset)
        {
            var buttonsStateByte1 = Convert.ToString(buffer[14], 2).PadLeft(8, '0');
            var buttonsStateByte2 = Convert.ToString(buffer[15], 2).PadLeft(8, '0');
            var buttonsStateByte3 = Convert.ToString(buffer[16], 2).PadLeft(8, '0');
            var buttonsStateByte4 = Convert.ToString(buffer[17], 2).PadLeft(8, '0');

            var buttonsState = buttonsStateByte1 + buttonsStateByte2 + buttonsStateByte3 + buttonsStateByte4;
            var charArray = buttonsState.ToCharArray();
            Array.Reverse(charArray);
            buttonsState = new string(charArray);

            var events = OnNewBinaryInputPacket(buffer[blockOffset + 1] + IncreaseModuleIdForBinaryInput, buttonsState);
            foreach (var ev in events)
                _incomingEvents.Enqueue(ev);
        }
        /// <summary>
        /// В этом методе данные от модуля бинарного ввода преобразуются в события нажатия кнопки от виртуального модуля, id которого на 256 больше, чем на самом деле
        /// Это нужно для того, чтобы не поддерживать ещё один тип плат. Фактически модуль бинарного ввода - это модуль, обрабатывающий нажатия кнопок
        /// </summary>
        /// <param name="moduleId">id модуля, от которого пришли данные</param>
        /// <param name="buttonsState">состояния кнопок модуля бинарного ввода</param>
        /// <returns></returns>
        private IEnumerable<ControlEventBase> OnNewBinaryInputPacket(uint moduleId, string buttonsState)
        {
            var events = new List<ControlEventBase>();
            // Когда данные поступают в первый раз, нужно сдампить все линии, потому что, когда все линии установлены в 0 не ясно, какая кнопка сработала
            // Однако, если выключить тумблер при назначении и все линии встанут в 0, будет неверно определён сработавший тумблер (ситуация редкая, только если ни один тумблер с этой платы не назначен)
            var needToDump = false;
            if (!_binaryInputModulesState.ContainsKey(moduleId))
            {
                needToDump = true;
                _binaryInputModulesState.Add(moduleId, Convert.ToString(0, 2).PadRight(32, '0'));
            }

            var oldButtonsState = _binaryInputModulesState[moduleId];

            for (var i = /*ButtonsState.Length - 1*/27; i >= 0; i--)
            {
                if (oldButtonsState[i] != buttonsState[i] || needToDump)
                {
                    var ev = new ButtonEvent
                    {
                        Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Button, ModuleId = moduleId, ControlId = (uint)i, MotherBoardId = MotherboardId },
                        IsPressed = buttonsState[i] != '0',
                    };
                    events.Add(ev);
                }
            }
            _binaryInputModulesState[moduleId] = buttonsState;
            return events.ToArray();
        }
        /// <summary>
        /// Обработать и добавить событие от модуля кнопок
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        private void ProcessButtonEvent(byte[] buffer, int blockOffset)
        {
            // blockOffset + 15 == 1 - Начат дамп кнопок
            // blockOffset + 15 == 2 - Завершён дамп кнопок (не совпадает с приходящими событиями, видимо, сообщает о том, что внутри железа дамп завершён)
            if (buffer[blockOffset + 15] != 1 && buffer[blockOffset + 15] != 2 && buffer[blockOffset + 16] != 0)
            {
                var controlEvent = new ButtonEvent
                {
                    Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Button, ControlId = buffer[blockOffset + 16], ModuleId = buffer[blockOffset + 1], MotherBoardId = MotherboardId },
                    IsPressed = (buffer[blockOffset + 17] == 1),
                };
                _incomingEvents.Enqueue(controlEvent);
            }
        }
        /// <summary>
        /// Циклическая функция отправки данных устройству
        /// </summary>
        private void SendDataLoop()
        {
            while (true)
            {
                lock (_quitSyncRoot)
                {
                    if (_quit)
                        return;
                }

                if (_outgoingEvents.Count == 0)
                {
                    Thread.Sleep(2);
                    continue;
                }
                SendData();
            }
        }
        /// <summary>
        /// Отправить накопившиеся события железу
        /// </summary>
        private void SendData()
        {
            try
            {
                lock (_outgoingEvents)
                {
                    while (_outgoingEvents.Count != 0)
                    {
                        var ev = _outgoingEvents.Dequeue();

                        byte[] outDataBuffer = null;
                        // Отправка события включить линию модуля бинарного вывода
                        if (ev.Hardware.ModuleType == HardwareModuleType.BinaryOutput && ev is LampEvent)
                        {
                            var hEvent = ev as LampEvent;
                            outDataBuffer = new byte[5];
                            outDataBuffer[0] = (byte)ArccModuleType.LinearOutput;
                            outDataBuffer[1] = (byte)ev.Hardware.ModuleId;
                            outDataBuffer[2] = (byte)ArccLinearOutputCommand.SetLampState;
                            outDataBuffer[3] = (byte)ev.Hardware.ControlId;
                            outDataBuffer[4] = (byte)(hEvent.IsOn ? 1 : 0);
                        }
                        // Отправка события установить на индикаторе текст
                        // ToDo: костыль, чтобы работал поиск: ev.Hardware.ControlId != 0
                        if (ev.Hardware.ModuleType == HardwareModuleType.Indicator && ev is IndicatorEvent && ev.Hardware.ControlId == 0)
                        {
                                var hEvent = ev as IndicatorEvent;
                                // Размер буфера - 10 байт
                                outDataBuffer = StringToIndicatorBuffer(hEvent.IndicatorText);
                                outDataBuffer[0] = (byte) ArccModuleType.Indicator;
                                outDataBuffer[1] = (byte) ev.Hardware.ModuleId;
                                outDataBuffer[2] = (byte) ArccIndicatorCommand.SetText;
                        }
                        // Отправка события сдампить состояние кнопок модуля клавиатуры
                        if (ev.Hardware.ModuleType == HardwareModuleType.Button && ev is DumpEvent)
                        {
                            outDataBuffer = new byte[5];
                            outDataBuffer[0] = (byte)ArccModuleType.Button;
                            outDataBuffer[1] = (byte)ev.Hardware.ModuleId;
                            outDataBuffer[2] = (byte)ArccButtonCommand.DumpAllKeys;//command;
                            outDataBuffer[3] = 0;
                            outDataBuffer[4] = 0;
                        }
                        // Отправка события сдампить состояние кнопок модуля бинарного ввода
                        if (ev.Hardware.ModuleType == HardwareModuleType.Button && ev is DumpEvent && ev.Hardware.ModuleId > 255)
                        {
                            outDataBuffer = new byte[5];
                            outDataBuffer[0] = (byte)ArccModuleType.BinaryInput; // 8 - Группа модулей бинарного ввода. Всегда дампим вместе с клавишами, 
                            outDataBuffer[1] = (byte)(ev.Hardware.ModuleId - IncreaseModuleIdForBinaryInput);
                            outDataBuffer[2] = (byte)ArccBinaryInputCommand.DumpAllLines; // 1- настройка фильтра, 2 - дамп
                            outDataBuffer[3] = 0;
                            outDataBuffer[4] = 0;
                        }
                        if (outDataBuffer != null)
                            _port.Write(outDataBuffer, 0, outDataBuffer.Length);
                        Thread.Sleep(2);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// Преобразование текста в массив байт для передачи индикатору. Возвращаемый буфер больше текста и соответствует длине и формату пакета, передаваемого индикатору
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Коды для передачи в индикатор</returns>
        private static byte[] StringToIndicatorBuffer(string text)
        {
            const string signs = "01234567890123456789 FDC-_^Iro.E";

            var result = new byte[10];
            for (var i = 0; i < result.Length; i++)
                result[i] = 20;

            var bufferPos = 9;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (bufferPos == 2)
                    break;

                var symbol = text[i];
                if (symbol == ',')
                    symbol = '.';
                var pos = signs.IndexOf(symbol);
                if (pos == -1)
                {
                    result[bufferPos] = 20;
                    bufferPos--;
                    continue;
                }
                if (i > 0 && symbol == '.' && text[i - 1] >= '0' && text[i - 1] <= '9')
                {
                    pos = signs.IndexOf(text[i - 1], 10);
                    i -= 1;
                }

                result[bufferPos] = (byte)pos;

                bufferPos--;
            }
            return result;
        }
        /// <summary>
        /// Дамп всех клавиш
        /// </summary>
        /// <param name="allHardwareInUse">массив содержит упоминание всех модулей, которые нужно сдампить</param>
        public void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            var dumpEvents = new ControlEventBase[1];
            var dumpEvent = new DumpEvent { Hardware = allHardwareInUse[0] };
            dumpEvents[0] = dumpEvent;
            PostOutgoingEvents(dumpEvents);
            System.Diagnostics.Debug.Print("Dump: " + MotherboardId);
        }
    }
}
