using System;
using System.Collections.Generic;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using SlimDX.DirectInput;

namespace FlexRouter.Hardware.Joystick
{
    internal class JoystickDevice : IHardwareDevice
    {
        /// <summary>
        /// Виртуальный модуль джойстика (ось/клавиши)
        /// </summary>
        private enum JoystickModule
        {
            Button = 1,
            Axis = 2
        };
        private readonly Queue<ControlEventBase> _incomingEvents = new Queue<ControlEventBase>();
        private readonly DeviceInstance _deviceInstance;
        private readonly DirectInput _directInput;
        private  SlimDX.DirectInput.Joystick _joystick;
        private bool[] _buttons;
        private int[] _axis;
        private readonly AutoResetEvent _joystickEvent = new AutoResetEvent(true);
        private Thread _joystickThread;
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
        /// <summary>
        /// Устанавливаемое минимальное значение, принимаемое осью джойстика
        /// </summary>
        private const int MinimumAxisValue = 0;
        /// <summary>
        /// Устанавливаемое максимальное значение, принимаемое осью джойстика
        /// </summary>
        private const int MaximumAxisValue = 1024;
        /// <summary>
        /// Если значение оси имменилось на это значение - считаем это дребезгом
        /// </summary>
        private const int AxisBounceValue = 5;
        public JoystickDevice(DeviceInstance deviceInstance, DirectInput directInput)
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
                _joystick = new SlimDX.DirectInput.Joystick(_directInput, _deviceInstance.InstanceGuid);

                foreach (var doi in _joystick.GetObjects(ObjectDeviceType.Axis))
                    _joystick.GetObjectPropertiesById((int)doi.ObjectType).SetRange(MinimumAxisValue, MaximumAxisValue);
                _joystick.Properties.AxisMode = DeviceAxisMode.Absolute;

                _joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.Nonexclusive);
                // Tell DirectX that this is a Joystick.
                //            _joystick.SetDataFormat(DeviceDataFormat.Joystick);

                _joystickThread = new Thread(GetJoystickData);
                _joystick.SetNotification(_joystickEvent);
                // Finally, acquire the device.
                _buttons = new bool[_joystick.GetObjects(ObjectDeviceType.Button).Count];
                _axis = new int[/*_joystick.GetObjects(ObjectDeviceType.Axis).Count+3*/128];
                //            PointOfView = new int[caps.NumberPointOfViews];
                _joystick.Acquire();

                _joystickThread.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Capabilities GetCapabilities()
        {
            return _joystick.Capabilities;
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
                _joystickThread.Join();
                _joystick.Unacquire();
                _joystick.Dispose();
                _directInput.Dispose();
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
        private void GetJoystickData()
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
                    if (!_joystickEvent.WaitOne(30))
                        continue;
                    _joystick.Poll();
                    var state = _joystick.GetCurrentState();
                    var buttons = state.GetButtons();

                    lock (_dumpObjectLocker)
                    {
                        for (uint i = 0; i < _buttons.Length; i++)
                        {
                            if ((buttons[i] == _buttons[i]) && _dumpMode == false)
                                continue;

                            var ev = new ButtonEvent
                            {
                                Hardware =
                                    new ControlProcessorHardware
                                    {
                                        ModuleType = HardwareModuleType.Button,
                                        MotherBoardId = GetJoystickGuid(),
                                        ModuleId = (uint)JoystickModule.Button,
                                        ControlId = i
                                    },
                                IsPressed = buttons[i]
                            };
                            _buttons[i] = buttons[i];
                            _incomingEvents.Enqueue(ev);
                        }
                        // ControlId начинается с 1
                        var axisIndex = 1;
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.X);
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.Y);
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.Z);
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.RotationX);
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.RotationY);
                        if (_axis.Length >= axisIndex)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.RotationZ);
                        var extAxis = state.GetSliders();
                        if (_axis.Length >= axisIndex && extAxis.Length >= 1)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], extAxis[0]);
                        if (_axis.Length >= axisIndex && extAxis.Length >= 2)
                            AddAxisEvent((uint)axisIndex, _axis[axisIndex++], extAxis[1]);
                        _dumpMode = false;
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Сформировать событие "изменилось положение оси" и отсеять дребезг
        /// </summary>
        /// <param name="axisIndex">индекс оси</param>
        /// <param name="oldValue">старое значение</param>
        /// <param name="newValue">новое значение</param>
        private void AddAxisEvent(uint axisIndex, int oldValue, int newValue)
        {
            lock (_dumpObjectLocker)
            {
                if (newValue <= oldValue + AxisBounceValue && newValue >= oldValue - AxisBounceValue && _dumpMode == false)
                    return;
                var ev = new AxisEvent
                {
                    Hardware =
                        new ControlProcessorHardware
                        {
                            ModuleType = HardwareModuleType.Axis,
                            MotherBoardId = GetJoystickGuid(),
                            ModuleId = (uint)JoystickModule.Axis,
                            ControlId = axisIndex
                        },
                    Position = (ushort) newValue,
                    MinimumValue = MinimumAxisValue,
                    MaximumValue = MaximumAxisValue
                };
                _axis[axisIndex] = newValue;
                _incomingEvents.Enqueue(ev);
            }
        }
        /// <summary>
        /// Получить уникальный GUID джойстика
        /// </summary>
        /// <returns></returns>
        public string GetJoystickGuid()
        {
            return _deviceInstance.ProductName + ":" + (ApplicationSettings.JoystickBindByInstanceGuid ? _deviceInstance.InstanceGuid.ToString() : _deviceInstance.ProductGuid.ToString());
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
