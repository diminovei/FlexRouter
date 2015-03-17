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
        /// Значение-метка для потоков, установленная в true говорит о том, что пора завершаться
        /// </summary>
        private volatile bool _quit;
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
        /// Класс, преобразующий массив данных от устройства в события, понятные роутеру и наоборот
        /// </summary>
        private readonly ArccHardwareDataProcessor _arccHardwareDataProcessor;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="motherboardId">Идентификатор устройства для использования в роутере</param>
        /// <param name="comPort">Com-порт, с которым соединено устройство</param>
        public ArccDevice(string motherboardId, string comPort)
        {
            MotherboardId = motherboardId;
            _arccHardwareDataProcessor = new ArccHardwareDataProcessor(MotherboardId);
            _comPort = comPort;
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
            _quit = true;
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
            var bytesToRead = _port.BytesToRead;
            if (bytesToRead == 0)
                return;
            lock (_incomingEvents)
            {
                var buffer = new byte[bytesToRead];
                _port.Read(buffer, 0, bytesToRead);
                
                var events = _arccHardwareDataProcessor.ProcessDataFromSerialPort(buffer);

                foreach (var ev in events)
                    _incomingEvents.Enqueue(ev);
            }
        }
        /// <summary>
        /// Циклическая функция отправки данных устройству
        /// </summary>
        private void SendDataLoop()
        {
            while (true)
            {
                if (_quit)
                    return;

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
                    var outDataBuffer = _arccHardwareDataProcessor.ConvertEventToByteArrayForHardware(ev);
                    if (outDataBuffer != null)
                        _port.Write(outDataBuffer, 0, outDataBuffer.Length);
                    Thread.Sleep(2);
                }
            }
        }
        /// <summary>
        /// Дамп всех клавиш
        /// </summary>
        /// <param name="allHardwareInUse">массив содержит упоминание всех модулей, которые нужно сдампить</param>
        public void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            // Если не сделать эту задержку, то дамп может не произойти. Почему - не понятно.
            Thread.Sleep(1000);
            var dumpEvents = new ControlEventBase[1];
            var dumpEvent = new DumpEvent { Hardware = allHardwareInUse[0] };
            dumpEvents[0] = dumpEvent;
            PostOutgoingEvents(dumpEvents);
            System.Diagnostics.Debug.Print("Dump: " + MotherboardId);
        }
    }
}
