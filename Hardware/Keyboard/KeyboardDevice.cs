using System;
using System.Collections.Generic;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using SlimDX.DirectInput;

namespace FlexRouter.Hardware.Keyboard
{
    internal class KeyboardDevice : IHardwareDevice
    {
        private enum Modifiers
        {
            Control = 1,
            Alt = 2,
            Shift = 4,
        }

        protected class KeyboardButtonState
        {
            public bool IsPressed;
            public int ModifiersMask;

            public KeyboardButtonState()
            {
                IsPressed = false;
                ModifiersMask = 0;
            }
        }
        private SlimDX.DirectInput.Keyboard _device;
        private readonly Queue<ControlEventBase> _incomingEvents = new Queue<ControlEventBase>();
        private readonly DeviceInstance _deviceInstance;
        private readonly DirectInput _directInput;
        private KeyboardButtonState[] _buttons;
        private readonly AutoResetEvent _event = new AutoResetEvent(true);
        private Thread _thread;
        /// <summary>
        /// Объект блокировки при дампе состояния клавиш и осей
        /// </summary>
        private readonly object _threadLock = new object();
        /// <summary>
        /// Режим дампа клавиш и осей
        /// </summary>
        private bool _dumpMode;
        /// <summary>
        /// Объект блокировки при дампе состояния клавиш и осей
        /// </summary>
        private readonly object _dumpObjectLocker = new object();
        /// <summary>
        /// При установке в true сингализирует о необходимости окончить работу
        /// </summary>
        private bool _quitThread;
        public KeyboardDevice(DeviceInstance deviceInstance, DirectInput directInput)
        {
            _deviceInstance = deviceInstance;
            _directInput = directInput;
        }

        /// <summary>
        /// Найти все устройства типа джойстик
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            try
            {
                _quitThread = false;
                // create a device from this controller.
                _device = new SlimDX.DirectInput.Keyboard(_directInput);

                _device.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);

                _thread = new Thread(GetDeviceData);
                _device.SetNotification(_event);

                // Finally, acquire the device.
                _buttons = new KeyboardButtonState[_device.GetObjects(ObjectDeviceType.Button).Count];
                for (var i = 0; i < _buttons.Length; i++)
                    _buttons[i] = new KeyboardButtonState();

                _device.Acquire();

                _thread.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Отключение
        /// </summary>
        public void Disconnect()
        {
            try
            {
                lock (_threadLock)
                {
                    _quitThread = true;
                }
                _thread.Join();
                _device.Unacquire();
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// Получить все новые события, пришедшие от джойстика
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
            //lock (_incomingEvents)
            //{
            //    return _incomingEvents.ToArray();
            //}
        }

        public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
        }
        public void PostOutgoingEvents(ControlEventBase[] outgoingEvent)
        {
        }
        /// <summary>
        /// Callback, получающий информацию об изменениях в состоянии контролов джойстика
        /// </summary>
        private void GetDeviceData()
        {
            while (true)
            {
                try
                {
                    lock (_threadLock)
                    {
                        if (_quitThread)
                            break;
                    }
                    if (!_event.WaitOne(30))
                        continue;
                    _device.Poll();
                    var state = _device.GetCurrentState();

                    lock (_dumpObjectLocker)
                    {
                        var modifiers = 0;
                        if (state.IsPressed(Key.LeftShift) || state.IsPressed(Key.RightShift))
                            modifiers |= (int) Modifiers.Shift;
                        if (state.IsPressed(Key.LeftAlt) || state.IsPressed(Key.RightAlt))
                            modifiers |= (int) Modifiers.Alt;
                        if (state.IsPressed(Key.LeftControl) || state.IsPressed(Key.RightControl))
                            modifiers |= (int) Modifiers.Control;

                        for (var i = 0; i < _buttons.Length; i++)
                        {
                            if (state.IsPressed((Key) i))
                            {
                                // Если кнопка нажата и модификаторы не изменились и не нужно дампить - не запоминаем
                                if (_buttons[i].IsPressed && modifiers == _buttons[i].ModifiersMask && _dumpMode == false)
                                    continue;
                            }
                            else
                            {
                                // Если кнопка нажата и модификаторы не изменились и не нужно дампить - не запоминаем
                                if (!_buttons[i].IsPressed && _dumpMode == false)
                                    continue;
                            }
                            _buttons[i].ModifiersMask = modifiers;
                            _buttons[i].IsPressed = state.IsPressed((Key) i);
                            var ev = new ButtonEvent
                            {
                                Hardware =
                                    new ControlProcessorHardware
                                    {
                                        ModuleType = HardwareModuleType.Button,
                                        MotherBoardId = GetDeviceGuid(),
                                        ModuleId = (uint) _buttons[i].ModifiersMask,
                                        ControlId = (uint) i
                                    },
                                IsPressed = _buttons[i].IsPressed
                            };
                            _incomingEvents.Enqueue(ev);
                        }
                        _dumpMode = false;
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// Получить уникальный GUID джойстика
        /// </summary>
        /// <returns></returns>
        public string GetDeviceGuid()
        {
            return _deviceInstance.ProductName + ":" + _deviceInstance.ProductGuid;
        }
                /// <summary>
        /// Сдампить клавиши, оси, ...
        /// </summary>
        /// <param name="allHardwareInUse">Все используемые в профиле контролы для железа, не понимающего общей команды Dump и дампящего помодульно (ARCC)</param>
        public void Dump(ControlProcessorHardware[] allHardwareInUse)
        {
            lock (_dumpObjectLocker)
            {
                _dumpMode = true;
            }
        }
    }
}
