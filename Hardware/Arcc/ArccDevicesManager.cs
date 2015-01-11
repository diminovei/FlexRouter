using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Helpers;
using FTD2XX_NET;

namespace FlexRouter.Hardware.Arcc
{
    internal class ArccDevicesManager : DeviceManagerBase
    {
        private volatile bool _quit = false;
        /// <summary>
        /// Префикс, который в паре с идентификатором формирует уникальный идентификатор устройства
        /// </summary>
        private const string DevicePrefix = "Arcc";

        public override void PostOutgoingEvents(ControlEventBase[] outgoingEvents)
        {
            foreach (var d in Devices)
            {
                d.Value.PostOutgoingEvents(outgoingEvents);
            }
        }

        public override void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            if (Devices.ContainsKey(outgoingEvent.Hardware.MotherBoardId))
                Devices[outgoingEvent.Hardware.MotherBoardId].PostOutgoingEvent(outgoingEvent);
       }

        public override int[] GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            // В ARCC id любой платы задаётся байтом. 0-255
            if (deviceSubType == DeviceSubType.ExtensionBoard)
                return Enumerable.Range(0, 255).ToArray();

            // ARCC не имеет подразделений типа "Block"
            if (deviceSubType == DeviceSubType.Block)
                return null;

            switch (cph.ModuleType)
            {
                case HardwareModuleType.Axis:
                        return Enumerable.Range(0, 7).ToArray();
                case HardwareModuleType.BinaryOutput:
                        return Enumerable.Range(1, 29).ToArray();
                case HardwareModuleType.Button:
                        return Enumerable.Range(1, 168).ToArray();
                case HardwareModuleType.Encoder:
                        return Enumerable.Range(1, 14).ToArray();
                default:
                    return null;
            }
        }

        public override bool Connect()
        {
            var foundDevices = new Dictionary<string, ArccDevice>();
            try
            {
                var myFtdiDevice = new FTDI();
                uint ftdiDeviceCount = 0;

                var ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return false;
                var ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
                ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return false;

                for (var i = 0; i < ftdiDeviceList.Length; i++)
                {
                    const string descriptionPattern = "ARCC";
                    if (!ftdiDeviceList[i].Description.StartsWith(descriptionPattern))
                        continue;
                    ftStatus = myFtdiDevice.OpenByIndex((uint) i);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        continue;

                    string comPort;
                    var chipId = 0;

                    ftStatus = myFtdiDevice.GetCOMPort(out comPort);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        continue;

                    myFtdiDevice.Close();
                    FTChipID.ChipID.GetDeviceChipID(i, ref chipId);
                    var id = DevicePrefix + ":" + chipId.ToString("X");
                    var device = new ArccDevice(id, comPort);
                    //var device = new ArccDevice(id, comPort);
                    //if (device.Connect())
                    //    Devices.Add(id, device);

                    foundDevices.Add(id, device);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            foreach (var arccDevice in foundDevices)
            {
                if (arccDevice.Value.Connect())
                    Devices.Add(arccDevice.Key, arccDevice.Value);
            }
            return true;
        }
        #region Методы доработаны для реализации контролируемого дампа железа Arcc
        /// <summary>
        /// Отключить все устройства
        /// </summary>
        public override void Disconnect()
        {
            // Дождаться выхода из дампа и только после этого разорвать соединение
            if (_dumpThread!=null && _dumpThread.IsAlive)
            {
                _quit = true;
                _dumpThread.Join();
            }
            foreach (var device in Devices)
                device.Value.Disconnect();
            Devices.Clear();
        }
        /// <summary>
        /// Получить входящие события (нажатие кнопки, вращение энкодера и т.д.)
        /// </summary>
        /// <returns>Массив событий</returns>
        public override ControlEventBase[] GetIncomingEvents()
        {
            var ie = new List<ControlEventBase>();
            for (var i = 0; i < Devices.Count; i++)
            {
                var incomingEvents = Devices.ElementAt(i).Value.GetIncomingEvents();
                if (incomingEvents == null)
                    continue;
                ie.AddRange(incomingEvents);
                if(_inDump)
                    RememberDumpEvents(incomingEvents);
            }
            return ie.Count == 0 ? null : ie.ToArray();
        }
        #endregion
        #region Dump
        /// <summary>
        /// Метод заполняет массив сдампленных кнопок. Получая собитие от модуля, которму отправлена команда Dump,
        /// устанавливает в true соответствующий элемент массива
        /// </summary>
        /// <param name="events">список новых событий</param>
        private void RememberDumpEvents(IEnumerable<ControlEventBase> events)
        {
            lock (_dumpSyncRoot)
            {
                foreach (var ev in events)
                {
                    if (ev.Hardware.MotherBoardId != HardwareInDump.MotherBoardId || ev.Hardware.ModuleType != HardwareInDump.ModuleType || ev.Hardware.ModuleId != HardwareInDump.ModuleId)
                        continue;
                    if (_moduleTypeInDump == ModuleTypeInDump.BinaryInput)
                        _binaryInputDumpState[ev.Hardware.ControlId-1] = true;
                    if (_moduleTypeInDump == ModuleTypeInDump.Button)
                        _buttonDumpState[ev.Hardware.ControlId-1] = true;
                }
            }
        }

        private volatile bool _inDump;
        private readonly object _dumpSyncRoot = new object();
        private Thread _dumpThread;
        public volatile ControlProcessorHardware HardwareInDump = null;
        private ControlProcessorHardware[] _allHardwareInUse;
        private enum ModuleTypeInDump
        {
            None,
            Button,
            BinaryInput
        }
        private readonly bool[] _buttonDumpState = new bool[168];
        private readonly bool[] _binaryInputDumpState = new bool[28];
        private ModuleTypeInDump _moduleTypeInDump = ModuleTypeInDump.None;

        public override void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            if (_dumpThread != null && _dumpThread.IsAlive)
                return;
            _allHardwareInUse = allHardwareInUse;
            _dumpThread = new Thread(DumpLoop){IsBackground = true};
            _dumpThread.Start();
        }
        /// <summary>
        /// Циклическая функция отправки команд дампа по одной и ожидания дампа всех кнопок
        /// </summary>
        private void DumpLoop()
        {
            _inDump = true;
            var hardwareArray = new ControlProcessorHardware[1];
            var dumpQueueHashes = new List<string>();
            foreach (var controlProcessorHardware in _allHardwareInUse)
            {
                // Если железо не Arcc
                if(!Devices.ContainsKey(controlProcessorHardware.MotherBoardId))
                    continue;
                
                // Если такая пара "материнская плата + модуль" уже обрабатывались
                var hash = controlProcessorHardware.MotherBoardId + "|" + controlProcessorHardware.ModuleId + "|" + controlProcessorHardware.ModuleType;
                if(dumpQueueHashes.Contains(hash))
                    continue;
                System.Diagnostics.Debug.Print("Dumping: " + hash);
                lock (_dumpSyncRoot)
                {
                    // Очищаем массивы сработавших событий
                    for (var i = 0; i < _binaryInputDumpState.Length; i++)
                    {
                        _binaryInputDumpState[i] = false;
                    }
                    for (var i = 0; i < _buttonDumpState.Length; i++)
                    {
                        _buttonDumpState[i] = false;
                    }
                    // Отправляем пару "материнская плата + модуль" дампиться
                    HardwareInDump = controlProcessorHardware;
                    hardwareArray[0] = controlProcessorHardware;
                    _moduleTypeInDump = controlProcessorHardware.ModuleId > 255 ? ModuleTypeInDump.BinaryInput : ModuleTypeInDump.Button;
                }
                Devices[controlProcessorHardware.MotherBoardId].Dump(hardwareArray);
                var lastUpdateTime = DateTime.Now;
                var stateCount = 0;
                // Ждём дампа всех кнопок модуля или перерыва в поставке данных и переходим к дампу следующего модуля
                while (true)
                {
                    if (_quit)
                        return;
                    lock (_dumpSyncRoot)
                    {
                        var buttonsDumpedCount = 0;
                        if (_moduleTypeInDump == ModuleTypeInDump.Button)
                        {
                            // Если все кнопки сдамплены переходим к следующему модулю
                            if (_buttonDumpState.All(b => b))
                            {
                                System.Diagnostics.Debug.Print("Dumped successfully");
                                var moduleType = _moduleTypeInDump.ToString();
                                var message = string.Format("{0} module {1}", HardwareInDump.MotherBoardId, moduleType);
                                Problems.AddOrUpdateProblem(message, "", ProblemHideOnFixOptions.HideItemAndDescription, true);
                                break;
                            }
                                
                            buttonsDumpedCount = _buttonDumpState.Count(b => b);
                        }
                        if (_moduleTypeInDump == ModuleTypeInDump.BinaryInput)
                        {
                            // Если все кнопки сдамплены переходим к следующему модулю
                            if (_binaryInputDumpState.All(b => b))
                            {
                                System.Diagnostics.Debug.Print("Dumped successfully");

                                var moduleType = _moduleTypeInDump.ToString();
                                var message = string.Format("{0} module {1}", HardwareInDump.MotherBoardId, moduleType);
                                Problems.AddOrUpdateProblem(message, "", ProblemHideOnFixOptions.HideItemAndDescription, true);
                                break;
                            }
                            buttonsDumpedCount = _binaryInputDumpState.Count(b => b);
                        }
                        // Если прошло X миллисекунд и ни одной новой кнопки не сдамплено, значит дамп окончен
                        if (buttonsDumpedCount == stateCount)
                        {
                            if (DateTime.Now - lastUpdateTime > new TimeSpan(0, 0, 0, 0, 1000))
                            {
                                var moduleType = _moduleTypeInDump.ToString();
                                var buttonsArray = _moduleTypeInDump == ModuleTypeInDump.Button
                                    ? _buttonDumpState
                                    : _binaryInputDumpState;
                                var undumpedButtonsCount = _moduleTypeInDump == ModuleTypeInDump.Button
                                    ? _buttonDumpState.Length - buttonsDumpedCount
                                    : _binaryInputDumpState.Length - buttonsDumpedCount;
                                var buttonsList = string.Empty;
                                for (var i = 0; i < buttonsArray.Length; i++)
                                {
                                    if (buttonsArray[i])
                                        continue;
                                    if (buttonsList != string.Empty)
                                        buttonsList += ", ";
                                    buttonsList += (i + 1).ToString(CultureInfo.InvariantCulture);
                                }

                                var description = string.Format("Can't dump {0} keys: {1}", undumpedButtonsCount, buttonsList);
                                var message = string.Format("{0} module {1}", HardwareInDump.MotherBoardId, moduleType);
                                Problems.AddOrUpdateProblem(message, description, ProblemHideOnFixOptions.HideItemAndDescription, false);
                                System.Diagnostics.Debug.Print("Dump failed. Dumped: " + buttonsDumpedCount);
                                break;
                            }
                        }
                        else
                        {
                            lastUpdateTime = DateTime.Now;
                            stateCount = buttonsDumpedCount;
                        }
                    }
                    Thread.Sleep(100);
                }
            }
            _inDump = false;
        }
        #endregion
    }
}
