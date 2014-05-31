using System.Collections.Generic;
using System.Threading;
using FlexRouter.Hardware;
using FlexRouter.Hardware.Arcc;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter
{
    class Core
    {
        private Thread _routerCoreThread;
        private readonly object _routerCoreThreadLocker = new object();
        private bool _routerCoreStopCommand;    // При установке флага в true работа потока должна прекратиться
        
        private bool _routerCorePauseCommand;    // При установке флага в true работа потока должна прекратиться
        private bool _routerCoreIsPaused;
        readonly List<string> _outputWasOn = new List<string>();
        private bool _isPaused;
        public bool IsWorking()
        {
            return _routerCoreThread != null && _routerCoreThread.IsAlive;
        }
        public void Lock()
        {
            if (!IsWorking())
                return;
            lock (_routerCoreThreadLocker)
            {
                _routerCorePauseCommand = true;
            }
            while (true)
            {
                lock (_routerCoreThreadLocker)
                {
                    if (_routerCoreIsPaused)
                        break;
                }
            }
        }
        public void Unlock()
        {
            if (!IsWorking())
                return;
            lock (_routerCoreThreadLocker)
            {
                _routerCorePauseCommand = false;
                _routerCoreIsPaused = false;
            }
        }
        public bool Start()
        {
            if(IsWorking())
                return false;
            Messenger.AddMessage(MessageToMainForm.ClearConnectedDevicesList);
            if (!_isPaused)
            {
                HardwareManager.Connect();
                var devices = HardwareManager.GetConnectedDevices();
                foreach (var device in devices)
                    Messenger.AddMessage(MessageToMainForm.AddConnectedDevice, device);
            }
            Profile.InitializeAccessDescriptors();
            Dump();
            // Запускаем роутер после дампа, чтобы не получилось, что индикаторы и лампы не зажигаются, хотя фактически режимы включены
            _routerCoreThread = new Thread(ThreadLoop) { IsBackground = true };
            _routerCoreThread.Start();
            Messenger.AddMessage(MessageToMainForm.RouterStarted);
            return true;
        }
        public bool Stop(bool pause)
        {
            if (!IsWorking())
                return false;
            lock (_routerCoreThreadLocker)
            {
                _routerCoreStopCommand = true;
                _routerCoreIsPaused = false;
            }
            _routerCoreThread.Join();
            _routerCoreStopCommand = false;
            ShutDownOutputHardware();
            if (!pause)
                HardwareManager.Disconnect();
            else
                _isPaused = true;
            Messenger.AddMessage(MessageToMainForm.RouterStopped);
            return true;
        }
        private void ThreadLoop()
        {
            while (true)
            {
                lock (_routerCoreThreadLocker)
                {
                    if (_routerCoreStopCommand)
                        return;
                    if (_routerCorePauseCommand)
                    {
                        if (!_routerCoreIsPaused)
                        {
                            _routerCoreIsPaused = true;
                        }
                            
                        Thread.Sleep(100);
                        continue;
                    }
                }
                Work();
                Thread.Sleep(100);
            }
        }
        private void Work()
        {
            var events = HardwareManager.GetIncomingEvents();
//            if (events == null)
//                return;
            // Обрабатываем все события
            if (events != null)
            {
                foreach (var controlEvent in events)
                {
                    Profile.SendEventToControlProcessors(controlEvent);
                    //                Messenger.AddMessage(MessageToMainForm.ShowEvent, string.Format("{0}:{1}", controlEvent.Hardware.GetHardwareGuid(), controlEvent.RotateDirection ? ">>>" : "<<<"));
                    if(controlEvent.Hardware.ModuleType != HardwareModuleType.Axis)
                        Messenger.AddMessage(MessageToMainForm.NewHardwareEvent, controlEvent);
                    //                var a = controlEvent.Hardware.GetHardwareGuid();
                    //                SendEventInfoToMainForm(controlEvent);
                    //                ProcessControlEvent(controlEvent);
                }
            }

            var newOutgoingEvents = Profile.GetControlProcessorsNewEvents();
            foreach (var newOutgoingEvent in newOutgoingEvents)
            {
                // В том случае, когда на один индикатор назначены 2 дексриптора. Переключение "коммутатором"
                // более ранний по ID дескриптор установил текст при переключении коммутатора, а более поздний выключил питание. В итоге индикатор пуст
                if (newOutgoingEvent.Hardware.ModuleType == HardwareModuleType.Indicator || newOutgoingEvent.Hardware.ModuleType == HardwareModuleType.BinaryOutput)
                {
                    var isPowerOff = false;
                    var hwGuid = newOutgoingEvent.Hardware.GetHardwareGuid();

                    var indicatorEvent = newOutgoingEvent as IndicatorEvent;
                    if (indicatorEvent != null && indicatorEvent.IndicatorText == string.Empty) 
                        isPowerOff = true;
                    var lampEvent = newOutgoingEvent as LampEvent;
                    if (lampEvent != null && !lampEvent.IsOn) 
                        isPowerOff = true;
                    if (isPowerOff && _outputWasOn.Contains(hwGuid))
                        continue;

                    if(!isPowerOff && !_outputWasOn.Contains(hwGuid))
                        _outputWasOn.Add(hwGuid);
                }
                HardwareManager.PostOutgoingEvent(newOutgoingEvent);
            }
            _outputWasOn.Clear();
            Profile.TickControlProcessors();
        }
        /// <summary>
        /// Гасим все лампы и индикаторы, присутствующие в профиле
        /// </summary>
        public void ShutDownOutputHardware()
        {
            var clearEvents = Profile.GetControlProcessorsClearEvents();
            foreach (var clearEvent in clearEvents)
                HardwareManager.PostOutgoingEvent(clearEvent);
        }

        private void Dump()
        {
            var assignments = Profile.GetControlProcessorAssignments();
            HardwareManager.Dump(DumpMode.AllKeys);
            HardwareManager.DumpModule(assignments);
        }
/*        private void DumpKeys()
        {
            var modules = new List<string>();
            foreach (var processor in Profile.ControlProcessors.ToArray())
            {
                var hw = processor.GetHardwareUsed();
                var moduleType = processor.GetUsedHardwareModuleType();
                if (moduleType != ArccModuleType.Button && moduleType != ArccModuleType.ButtonAsEncoder && moduleType != ArccModuleType.ButtonRepeater)
                    continue;

                var module = hw.MotherBoardId + ":" + hw.ModuleId;
                if (modules.Contains(module))
                    continue;
                modules.Add(module);
                var dev = new VisualizerEvent
                {
                    Hardware = new ControlProcessorHardware { MotherBoardId = (hw.MotherBoardId), ModuleId = hw.ModuleId, ModuleType = ArccModuleType.Button },
                    Command = ((byte)ButtonCommand.DumpAllKeys) // DumpAllKeys
                };
                HardwareManager.PostOutgoingEvent(dev);
                var dev1 = new VisualizerEvent
                {
                    Hardware = new ControlProcessorHardware { MotherBoardId = (hw.MotherBoardId), ModuleId = hw.ModuleId, ModuleType = ArccModuleType.Button },
                    Command = ((byte)ButtonCommand.DumpPressedKeysOnly)
                };
                HardwareManager.PostOutgoingEvent(dev1);
            }
            HardwareManager.Dump(DumpMode.AllKeys);
            HardwareManager.Dump(DumpMode.PressedKeys);
            IsNeedToDumpKeys = false;
        }*/
    }
}
