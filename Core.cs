﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.MessagesToMainForm;
using FlexRouter.ProfileItems;

namespace FlexRouter
{
    class Core
    {
        enum Mode
        {
            Work,
            Pause,
            Stop
        }
        private volatile Mode _mode = Mode.Stop;
        private Thread _routerCoreThread;
        private volatile bool _isDumpedOnce;
        public bool Pause()
        {
            return Stop(true);
        }
        public bool IsWorking()
        {
            return _routerCoreThread != null && _routerCoreThread.IsAlive;
        }
        public bool Start()
        {
            if (IsWorking())
                return false;
            Messenger.AddMessage(MessageToMainForm.ClearConnectedDevicesList);
            if (_mode == Mode.Stop)
            {
                HardwareManager.Connect();
                var devices = HardwareManager.GetConnectedDevices();
                foreach (var device in devices)
                    Messenger.AddMessage(MessageToMainForm.ChangeConnectedDevice, device);
            }
            else
                Profile.AccessDescriptor.InitializeAccessDescriptors();
            // Запускаем роутер после дампа, чтобы не получилось, что индикаторы и лампы не зажигаются, хотя фактически режимы включены
            _mode = Mode.Work;
            _routerCoreThread = new Thread(ThreadLoop) { IsBackground = true };
            _routerCoreThread.Start();
            Messenger.AddMessage(MessageToMainForm.RouterStarted);
            if (_mode == Mode.Stop)
            {
                Profile.AccessDescriptor.InitializeAccessDescriptors();
                Dump();
            }
            return true;
        }
        public bool Stop()
        {
            return Stop(false);
        }
        private bool Stop(bool pause)
        {
            if (!IsWorking())
                return false;
            _mode = pause ? Mode.Pause : Mode.Stop;
            _routerCoreThread.Join();
            if (_mode == Mode.Stop)
            {
                ShutDownOutputHardware();
                HardwareManager.Disconnect();
                Messenger.AddMessage(MessageToMainForm.RouterStopped);
            }
            else
            {
                Messenger.AddMessage(MessageToMainForm.RouterPaused);
            }
            return true;
        }
        private void ThreadLoop()
        {
            var currentEventCounter = 0;
            const int softDumpPortion = 10;
            var stopwatch = new Stopwatch();
            while (true)
            {
                if (_mode == Mode.Stop || _mode == Mode.Pause)
                    return;
                stopwatch.Start();
                Profile.VariableStorage.SynchronizeVariables();
                Work();
                if (!ApplicationSettings.ControlsSynchronizationIsOff)
                {
                    var eventsCache = HardwareManager.SoftDump();
                    if (eventsCache != null)
                    {
                        var softDumpEvents = eventsCache.Skip(currentEventCounter).Take(softDumpPortion).ToArray();
                        if (softDumpEvents.Length != 0)
                        {
                            currentEventCounter += softDumpPortion;
                            foreach (var controlEvent in softDumpEvents)
                            {
                                controlEvent.IsSoftDumpEvent = true;
                                Profile.ControlProcessor.SendEvent(controlEvent);
                            }
                                
                        }
                        else
                            currentEventCounter = 0;
                    }
                }
                stopwatch.Stop();
                const int delay = 200;
                if (stopwatch.ElapsedMilliseconds < delay)
                    Thread.Sleep(delay - (int)stopwatch.ElapsedMilliseconds);
            }
        }
        private void Work()
        {
            if (!_isDumpedOnce)
            {
                Dump();
                _isDumpedOnce = true;
            }

            var events = HardwareManager.GetIncomingEvents();

            // Обрабатываем все события
            foreach (var controlEvent in events)
            {
                Profile.ControlProcessor.SendEvent(controlEvent);
                //if(controlEvent.Hardware.ModuleType!=HardwareModuleType.Axis)
                    Messenger.AddMessage(MessageToMainForm.NewHardwareEvent, controlEvent);
            }
            var outgoing = new List<ControlEventBase>();
            var outputWasOn = new List<string>();

            var newOutgoingEvents = Profile.ControlProcessor.GetNewEvents();
            foreach (var newOutgoingEvent in newOutgoingEvents)
            {
                // В том случае, когда на один индикатор назначены 2 деcкриптора. Переключение "коммутатором"
                // более ранний по ID дескриптор установил текст при переключении коммутатора, а более поздний выключил питание. В итоге индикатор пуст
                if (newOutgoingEvent.Hardware.ModuleType == HardwareModuleType.LedMatrixIndicator || newOutgoingEvent.Hardware.ModuleType == HardwareModuleType.Indicator || newOutgoingEvent.Hardware.ModuleType == HardwareModuleType.BinaryOutput)
                {
                    var isPowerOff = false;

                    var indicatorEvent = newOutgoingEvent as IndicatorEvent;
                    if (indicatorEvent != null && string.IsNullOrWhiteSpace(indicatorEvent.IndicatorText)) 
                        isPowerOff = true;
                    
                    var lampEvent = newOutgoingEvent as LampEvent;
                    if (lampEvent != null && !lampEvent.IsOn) 
                        isPowerOff = true;

                    var hwGuid = newOutgoingEvent.Hardware.GetHardwareGuid();
                    if (isPowerOff && outputWasOn.Contains(hwGuid))
                        continue;

                    if(!isPowerOff && !outputWasOn.Contains(hwGuid))
                        outputWasOn.Add(hwGuid);
                }
                outgoing.Add(newOutgoingEvent);
            }
            outputWasOn.Clear();
            HardwareManager.PostOutgoingEvents(outgoing);
            Profile.ControlProcessor.Tick();
        }
        /// <summary>
        /// Гасим все лампы и индикаторы, присутствующие в профиле
        /// </summary>
        private void ShutDownOutputHardware()
        {
            var clearEvents = Profile.ControlProcessor.GetShutDownEventsForAllControlProcessors();
                HardwareManager.PostOutgoingEvents(clearEvents.ToList());
        }
        public void Dump()
        {
            var assignments = Profile.ControlProcessor.GetAllAssignedHardwares();
            Debug.Print("Dump command");
            HardwareManager.Dump(assignments);
        }
    }
}
