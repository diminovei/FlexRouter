using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.Arcc
{
    /// <summary>
    /// Типы модулей и варианты их использования
    /// Внимание! При использовании значений как int и как enum!
    /// </summary>
    public enum ArccModuleType
    {
/*        BinaryInput = -3,
        ButtonAsEncoder = -2,
        ButtonRepeater = -1,*/
        Axis = 1,
        Button = 2,
        Encoder = 3,
        LinearOutput = 4,
        Indicator = 5,
//        BinaryInput = 8,
    }
    public class ArccDevice : IHardwareDevice
    {
        enum AxisCommand
        {
            EnableDisableAxis = 1
            //  Byte - mean (5 bytes length)
            //  01 - group
            //  XX - Id
            //  XX - Command
            //  XX - Axis number
            //  XX - 0 - Disable, 1 - enable
            // ---------------------------------
            //  Byte - mean (18 bytes length)
            //  01 - group
            //  XX - Id
            //  XX XX - Axis 8
            //  XX XX - Axis 7
            //  XX XX - Axis 6
            //  XX XX - Axis 5
            //  XX XX - Axis 4
            //  XX XX - Axis 3
            //  XX XX - Axis 2
            //  XX XX - Axis 1
        }
        enum BinaryInputCommand
        {
            SetUpFilter = 1,
            DumpAllLines = 2
            //  Byte - mean (5 bytes length)
            //  08 - group
            //  XX - Id
            //  XX - Command
            //  XX - Any data
            //  XX - Any data
        }
        enum IndicatorCommand
        {
            SetText = 1,
            SetBrightness = 2,
            BatteryOff = 3,
            BatteryOn = 4,
            SaveBrightnessToEeprom = 5
        }
        enum LinearOutputCommand
        {
            SetLampState = 1,
            CheckLampsAllOn = 5,
            CheckLampsAllOff = 6,
            StopCheckLamps = 7
        }
        enum ButtonCommand
        {
            DumpPressedKeysOnly = 1,
            DumpUnpressedKeysOnly = 2,
            DumpAllKeys = 3,
            SetDumpInterval = 4
        }
        internal class OutputData
        {
            internal byte[] Buffer;
            internal OutputData(byte bufferLength)
            {
                Buffer = new byte[bufferLength];
            }
        }

        readonly Dictionary<uint, string> _binaryInputModulesState = new Dictionary<uint, string>();
        private IEnumerable<ControlEventBase> OnNewBinaryInputPacket(uint moduleId, string buttonsState)
        {
            var events = new List<ControlEventBase>();
            var needToDump = false; // Когда данные поступают в первый раз, нужно сдампить все линии, потому что, когда все линии установлены в 0 не ясно, какая кнопка сработала
            //  Однако, если выключить тумблер при назначении и все линии встанут в 0, будет неверно определён сработавший тумблер (ситуация редкая, только если ни один тумблер с этой платы не назначен
            if(!_binaryInputModulesState.ContainsKey(moduleId))
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
                                     Hardware = new ControlProcessorHardware {ModuleType = HardwareModuleType.Button, ModuleId = moduleId, ControlId = (uint)i, MotherBoardId = Id},
                                     IsPressed = buttonsState[i] != '0',
                                 };
                    events.Add(ev);
                }
            }
            _binaryInputModulesState[moduleId] = buttonsState;
            return events.ToArray();
        }

        private readonly object _quitSyncRoot = new object();
        private bool _quit;
        private readonly Thread _sendDataThread;

        public int ChipId { get; private set; }
        public string ComPort { get; private set; }
        public string Id { get; private set; }

        private SerialPort _port;
        private readonly Queue<ControlEventBase> _incomingEvents = new Queue<ControlEventBase>();
        private readonly Queue<ControlEventBase> _outgoingEvents = new Queue<ControlEventBase>();
        private readonly Dictionary<uint, ushort[]> _axisValues = new Dictionary<uint, ushort[]>();
        /// <summary>
        /// Здесь сохраняются остатки пришедших данных, не кратные размеру пакета. Учитываются при следующем чтении данных из устройства
        /// </summary>
        private readonly List<byte> _prevBufferTail = new List<byte>();
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="id">Идентификатор устройства для использования в роутере</param>
        /// <param name="chipId">Идентификатор устройства, данный ему производителем</param>
        /// <param name="comPort">Com-порт, с которым соединено устройство</param>
        public ArccDevice(string id, int chipId, string comPort)
        {
            Id = id;
            ChipId = chipId;
            ComPort = comPort;
            _sendDataThread = new Thread(SendDataLoop){IsBackground = true};
        }
        /// <summary>
        /// Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
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
        public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            lock (_outgoingEvents)
            {
                _outgoingEvents.Enqueue(outgoingEvent);
            }
        }

        public void Dump(DumpMode dumpMode)
        {
            
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
                               PortName = ComPort,
                               Handshake = Handshake.None,
                               Parity = Parity.None,
                               DataBits = 8,
                               StopBits = StopBits.One,
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
                _port.Open();
                _port.DtrEnable = true;
                Thread.Sleep(1000);
                _port.DtrEnable = false;
            }
            catch (Exception)
            {
                return false;
            }
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
        /// Функция (callback) приёма данных от устройства
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                lock (_incomingEvents)
                {
                    var bytesToRead = _port.BytesToRead;
                    if (bytesToRead == 0)
                        return;
                    var buffer = new byte[bytesToRead+_prevBufferTail.Count];
                    _prevBufferTail.CopyTo(buffer);
                    _port.Read(buffer, _prevBufferTail.Count, bytesToRead);
                    if (bytesToRead < 18)
                        return;
                    const int blockLength = 18;
                    var block = bytesToRead / blockLength;
                    for (var i = 0; i < block; i++)
                    {
                        var blockOffset = i == 0 ? i : i * blockLength;
                        if (buffer[blockOffset] == (byte)ArccModuleType.Button)
                        {
                            var controlEvent = new ButtonEvent
                                                   {
                                                       Hardware = new ControlProcessorHardware{ModuleType = HardwareModuleType.Button, ControlId = buffer[blockOffset + 16], ModuleId = buffer[blockOffset + 1], MotherBoardId = Id},
                                                       IsPressed = (buffer[blockOffset + 17] == 1),
                                                   };
                            _incomingEvents.Enqueue(controlEvent);
                        }
                        // Модуль бинарного ввода
                        if (buffer[blockOffset] == 8) // 8 - группа модулей бинарного ввода
                        {
                            var s1 = Convert.ToString(buffer[14], 2).PadLeft(8, '0');
                            var s2 = Convert.ToString(buffer[15], 2).PadLeft(8, '0');
                            var s3 = Convert.ToString(buffer[16], 2).PadLeft(8, '0');
                            var s4 = Convert.ToString(buffer[17], 2).PadLeft(8, '0');

                            var ss = s1 + s2 + s3 + s4;
                            var charArray = ss.ToCharArray();
                            Array.Reverse(charArray);
                            ss = new string(charArray);
                            
                            var events = OnNewBinaryInputPacket(buffer[blockOffset + 1], ss);
                            foreach (var ev in events)
                                _incomingEvents.Enqueue(ev);
                        }

                        if (buffer[blockOffset] == (byte)ArccModuleType.Encoder)
                        {
                            var controlEvent = new EncoderEvent
                                                   {
                                                       Hardware = new ControlProcessorHardware{ModuleType = HardwareModuleType.Encoder, ControlId = buffer[blockOffset + 15], ModuleId = buffer[blockOffset + 1], MotherBoardId = Id},
                                                       RotateDirection = (buffer[blockOffset + 17] == 1),
                                                       ClicksCount = buffer[blockOffset + 16],
                                                   };
                            _incomingEvents.Enqueue(controlEvent);
                        }
                        if (buffer[blockOffset] == (byte)ArccModuleType.Axis)
                        {
                            var controlEvent = new AxisEvent
                                                   {
                                                       Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Axis, ModuleId = buffer[blockOffset + 1], MotherBoardId = Id},
                                                   };
                            if (!_axisValues.ContainsKey(controlEvent.Hardware.ModuleId))
                                _axisValues.Add(controlEvent.Hardware.ModuleId, new ushort[8]);

                            const int threshold = 1;
                            for (var j = 7; j >= 0; j--)
                            {
                                var value = buffer[blockOffset + 2 + j * 2] * 256 + buffer[blockOffset + 2 + j * 2 + 1];

                                if(_axisValues[controlEvent.Hardware.ModuleId][j] == value)
                                    continue;
                                // Фильтрация дребезга оси
                                if (value >= _axisValues[controlEvent.Hardware.ModuleId][j] - threshold &&
                                    value <= _axisValues[controlEvent.Hardware.ModuleId][j] + threshold) continue;
                                
                                // Тест на дребезг пройден
                                controlEvent.Hardware.ControlId = (uint)((uint)8 - j);
                                // ToDo: а нужно ли?
                                //                                controlEvent.Direction = value > _axisValues[controlEvent.Hardware.ModuleId][j];
                                controlEvent.Position = value;
                                controlEvent.MinimumValue = 0;
                                controlEvent.MaximumValue = 1023;

                                _incomingEvents.Enqueue(controlEvent);
                                _axisValues[controlEvent.Hardware.ModuleId][j] = (ushort)value;
                            }
                        }
                    }
                    _prevBufferTail.Clear();
                    if(bytesToRead % blockLength!=0)
                    {
                        for (var j = block*blockLength; j < bytesToRead; j++)
                            _prevBufferTail.Add(buffer[j]);
                    }
                }
            }
            catch (Exception)
            {
                Disconnect();
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
            lock (_outgoingEvents)
            {
                while (_outgoingEvents.Count != 0)
                {
                    var ev = _outgoingEvents.Dequeue();

                    OutputData outData = null;
                    switch (ev.Hardware.ModuleType)
                    {
                        case HardwareModuleType.BinaryOutput:
                        {
                            var hEvent = ev as LampEvent;
                            if (hEvent == null)
                                break;
                            outData = new OutputData(5);
                            outData.Buffer[0] = (byte)GetArccModuleId(ev.Hardware.ModuleType);
                            outData.Buffer[1] = (byte)ev.Hardware.ModuleId;
                            outData.Buffer[2] = (byte)LinearOutputCommand.SetLampState;
                            outData.Buffer[3] = (byte)ev.Hardware.ControlId;
                            outData.Buffer[4] = (byte)(hEvent.IsOn ? 1 : 0);
                            break;
                            }
                        
                        case HardwareModuleType.Indicator:
                        {
                            // ToDo: костыль, чтобы работал поиск.
                            if (ev.Hardware.ControlId != 0)
                                break;
                                var hEvent = ev as IndicatorEvent;
                                if (hEvent == null)
                                    break;
                                outData = new OutputData(10);
                                outData.Buffer = StringToIndicatorBuffer(hEvent.IndicatorText);
                                outData.Buffer[0] = (byte)GetArccModuleId(ev.Hardware.ModuleType);
                                outData.Buffer[1] = (byte)ev.Hardware.ModuleId;
                                outData.Buffer[2] = (byte)IndicatorCommand.SetText;
                                break;
                            }
                        case HardwareModuleType.Button:
                            {
/*                                var hEvent = ev as ButtonEvent;
                                if (hEvent == null)
                                    break;
                                outData = new OutputData(5);
                                outData.Buffer[0] = 8;//ev.ModuleType; // 8 - Группа модулей бинарного ввода. Всегда дампим вместе с клавишами, 
                                                                        //  чтобы не усложнять код и не вводить отдельный тип модулей
                                outData.Buffer[1] = (byte)ev.Hardware.ModuleId;
                                outData.Buffer[2] = 2; // 1- настройка фильтра, 2 - дамп
                                outData.Buffer[3] = 0;
                                outData.Buffer[4] = 0;
                                _port.Write(outData.Buffer, 0, outData.Buffer.Length);
                                Thread.Sleep(1000);
                                
                                outData = new OutputData(5);
                                outData.Buffer[0] = (byte)GetArccModuleId(ev.Hardware.ModuleType);
                                outData.Buffer[1] = (byte)ev.Hardware.ModuleId;
                                //ToDo: DumpKeys
                                outData.Buffer[2] = ev.Command;
                                outData.Buffer[3] = 0;
                                outData.Buffer[4] = 0;*/
                                break;
                            }
                    }
                    if (outData != null)
                        _port.Write(outData.Buffer, 0, outData.Buffer.Length);
                    Thread.Sleep(2);
                }
            }
        }
        public void DumpModule(ControlProcessorHardware[] hardware)
        {
            lock (_outgoingEvents)
            {
                foreach (var hw in hardware)
                {
                    var outData = new OutputData(5);
                    outData.Buffer[0] = 8;//ev.ModuleType; // 8 - Группа модулей бинарного ввода. Всегда дампим вместе с клавишами, 
                    //  чтобы не усложнять код и не вводить отдельный тип модулей
                    outData.Buffer[1] = (byte) hw.ModuleId;
                    outData.Buffer[2] = (byte)BinaryInputCommand.DumpAllLines; // 1- настройка фильтра, 2 - дамп
                    outData.Buffer[3] = 0;
                    outData.Buffer[4] = 0;
                    _port.Write(outData.Buffer, 0, outData.Buffer.Length);
                    Thread.Sleep(1000);

                    outData = new OutputData(5);
                    outData.Buffer[0] = 2;
                    outData.Buffer[1] = (byte) hw.ModuleId;
                    outData.Buffer[2] = (byte)ButtonCommand.DumpAllKeys;
                    outData.Buffer[3] = 0;
                    outData.Buffer[4] = 0;

                    _port.Write(outData.Buffer, 0, outData.Buffer.Length);
                    Thread.Sleep(1000);

                    outData = new OutputData(5);
                    outData.Buffer[0] = 2;
                    outData.Buffer[1] = (byte) hw.ModuleId;
                    outData.Buffer[2] = (byte)ButtonCommand.DumpPressedKeysOnly;
                    outData.Buffer[3] = 0;
                    outData.Buffer[4] = 0;

                    _port.Write(outData.Buffer, 0, outData.Buffer.Length);
                    Thread.Sleep(2);
                }
            }
        }
        private ArccModuleType GetArccModuleId(HardwareModuleType moduleType)
        {
            if(moduleType == HardwareModuleType.BinaryOutput)
                return ArccModuleType.LinearOutput;
            if(moduleType == HardwareModuleType.Indicator)
                return ArccModuleType.Indicator;
            if(moduleType == HardwareModuleType.Button)
                return ArccModuleType.Button;
            return ArccModuleType.Indicator;
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
    }
}
