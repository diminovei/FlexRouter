using System;
using System.Collections.Generic;
using System.Threading;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
//using Microsoft.DirectX.DirectInput;
using SlimDX;
using SlimDX.DirectInput;
//using Device = Microsoft.DirectX.DirectInput.Device;
//using DeviceDataFormat = Microsoft.DirectX.DirectInput.DeviceDataFormat;
//using DeviceObjectInstance = Microsoft.DirectX.DirectInput.DeviceObjectInstance;
//using InputRange = Microsoft.DirectX.DirectInput.InputRange;

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
        private readonly SlimDX.DirectInput.DeviceInstance _deviceInstance;
        private readonly SlimDX.DirectInput.DirectInput _directInput;
        private  SlimDX.DirectInput.Joystick _joystick;
        private bool[] _buttons;
        private int[] _axis;
//        private int[] PointOfView;
//        private Device _joystick;
        private readonly AutoResetEvent _joystickEvent = new AutoResetEvent(true);
        private Thread _joystickThread;
        /// <summary>
        /// Объект блокировки при дампе состояния клавиш и осей
        /// </summary>
        private readonly object _threadLock = new object();
        /// <summary>
        /// Режим дампа клавиш и осей
        /// </summary>
        private DumpMode _dumpMode;
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
        public JoystickDevice(SlimDX.DirectInput.DeviceInstance deviceInstance, DirectInput directInput)
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
            _quitThread = false;
            // create a device from this controller.
            _joystick = new SlimDX.DirectInput.Joystick(_directInput, _deviceInstance.InstanceGuid);

            foreach (SlimDX.DirectInput.DeviceObjectInstance doi in _joystick.GetObjects(ObjectDeviceType.Axis))
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
        /// <summary>
        /// Отключение
        /// </summary>
        public void Disconnect()
        {
            lock (_threadLock)
            {
                _quitThread = true;
            }
            _joystickThread.Join();
            _joystick.Unacquire();
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
        }

        public void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
        }

        /// <summary>
        /// Callback, получающий информацию об изменениях в состоянии контролов джойстика
        /// </summary>
        private void GetJoystickData()
        {
            while (true)
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
                        if ((buttons[i] == _buttons[i]) && _dumpMode == DumpMode.Null)
                            continue;
                        if (_dumpMode == DumpMode.PressedKeys && !buttons[i])
                            continue;
                        if (_dumpMode == DumpMode.UnpressedKeys && buttons[i])
                            continue;

                        var ev = new ButtonEvent
                        {
                            Hardware =
                                new ControlProcessorHardware
                                {
                                    ModuleType = HardwareModuleType.Button,
                                    MotherBoardId = GetJoystickGuid(),
                                    ModuleId = (uint) JoystickModule.Button,
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
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.X);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.Y);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.Z);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.RotationX);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.RotationY);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], state.RotationZ);
                    var extAxis = state.GetSliders();
                    if (_axis.Length >= axisIndex && extAxis.Length >= 1)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], extAxis[0]);
                    if (_axis.Length >= axisIndex && extAxis.Length >= 2)
                        AddAxisEvent((uint) axisIndex, _axis[axisIndex++], extAxis[1]);
                    _dumpMode = DumpMode.Null;
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
            if (newValue <= oldValue + AxisBounceValue && newValue >= oldValue - AxisBounceValue && _dumpMode == DumpMode.Null) 
                return;
            var ev = new AxisEvent
            {
                Hardware =
                    new ControlProcessorHardware
                    {
                        ModuleType = HardwareModuleType.Axis,
                        MotherBoardId = GetJoystickGuid(),
                        ModuleId = (uint) JoystickModule.Axis,
                        ControlId = axisIndex
                    },
                Position = newValue,
                MinimumValue = MinimumAxisValue,
                MaximumValue = MaximumAxisValue
            };
            _axis[axisIndex] = newValue;
            _incomingEvents.Enqueue(ev);
        }
        /// <summary>
        /// Получить уникальный GUID джойстика
        /// </summary>
        /// <returns></returns>
        public string GetJoystickGuid()
        {
            return _deviceInstance.ProductName + ":" + /*ApplicationSettings.JoystickBindingType ? _deviceInstance.InstanceGuid.ToString() :*/
                _deviceInstance.ProductGuid;
        }
        /// <summary>
        /// Сдампить состояние кнопок и осей джойстика
        /// </summary>
        /// <param name="dumpMode"></param>
        public void Dump(DumpMode dumpMode)
        {
            lock (_dumpObjectLocker)
            {
                _dumpMode = dumpMode;
            }
        }

        public void DumpModule(ControlProcessorHardware[] hardware)
        {
            Dump(DumpMode.AllKeys);
        }
    }
/*    internal class JoystickDevice
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
        private byte[] _buttons;
        private int[] _axis;
        //        private int[] PointOfView;
        private Device _joystick;
        private readonly AutoResetEvent _joystickEvent = new AutoResetEvent(true);
        private Thread _joystickThread;
        /// <summary>
        /// Объект блокировки при дампе состояния клавиш и осей
        /// </summary>
        private readonly object _threadLock = new object();
        /// <summary>
        /// Режим дампа клавиш и осей
        /// </summary>
        private DumpMode _dumpMode;
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
        public JoystickDevice(DeviceInstance deviceInstance)
        {
            _deviceInstance = deviceInstance;
        }
        /// <summary>
        /// Найти все устройства типа джойстик
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            _quitThread = false;
            // create a device from this controller.
            _joystick = new Device(_deviceInstance.InstanceGuid);

            foreach (DeviceObjectInstance doi in _joystick.Objects)
            {
                if ((doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                {
                    _joystick.Properties.SetRange(ParameterHow.ById, doi.ObjectId, new InputRange(MinimumAxisValue, MaximumAxisValue));
                }
            }
            //            var ig = DeviceInstance.InstanceGuid;
            //            Debug.Print("Name: {0}, Guid: {1}, InstanceName: {2}, InstanceGuid: {3} ", DeviceInstance.ProductName, DeviceInstance.ProductGuid, DeviceInstance.InstanceName, DeviceInstance.InstanceGuid);

            _joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
            // Tell DirectX that this is a Joystick.
            _joystick.SetDataFormat(DeviceDataFormat.Joystick);

            _joystickThread = new Thread(GetJoystickData);
            _joystick.SetEventNotification(_joystickEvent);

            // Finally, acquire the device.
            var caps = _joystick.Caps;
            _buttons = new byte[caps.NumberButtons];
            _axis = new int[caps.NumberAxes+2]; //_axis = new int[12];
 * 
            //            PointOfView = new int[caps.NumberPointOfViews];
            _joystick.Acquire();

            _joystickThread.Start();
            return true;
        }
        /// <summary>
        /// Отключение
        /// </summary>
        public void Disconnect()
        {
            lock (_threadLock)
            {
                _quitThread = true;
            }
            _joystickThread.Join();
            _joystick.Unacquire();
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
        }
        /// <summary>
        /// Callback, получающий информацию об изменениях в состоянии контролов джойстика
        /// </summary>
        private void GetJoystickData()
        {
            while (true)
            {
                if (_quitThread)
                    break;
                if (!_joystickEvent.WaitOne(30))
                    continue;
                _joystick.Poll();
                var state = _joystick.CurrentJoystickState;
                var buttons = state.GetButtons();

                lock (_dumpObjectLocker)
                {
                    for (uint i = 0; i < _buttons.Length; i++)
                    {
                        if ((buttons[i] == _buttons[i]) && _dumpMode == DumpMode.Null)
                            continue;
                        if (_dumpMode == DumpMode.PressedKeys && buttons[i] < 128)
                            continue;
                        if (_dumpMode == DumpMode.UnpressedKeys && buttons[i] >= 128)
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
                            IsPressed = buttons[i] >= 128
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
                        AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.Rx);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.Ry);
                    if (_axis.Length >= axisIndex)
                        AddAxisEvent((uint)axisIndex, _axis[axisIndex++], state.Rz);
                    var extAxis = state.GetSlider();
                    if (_axis.Length >= axisIndex && extAxis.Length >= 1)
                        AddAxisEvent((uint)axisIndex, _axis[axisIndex++], extAxis[0]);
                    if (_axis.Length >= axisIndex && extAxis.Length >= 2)
                        AddAxisEvent((uint)axisIndex, _axis[axisIndex++], extAxis[1]);
                    _dumpMode = DumpMode.Null;
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
            if (newValue <= oldValue + AxisBounceValue && newValue >= oldValue - AxisBounceValue && _dumpMode == DumpMode.Null)
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
                Position = newValue,
                MinimumValue = MinimumAxisValue,
                MaximumValue = MaximumAxisValue
            };
            _axis[axisIndex] = newValue;
            _incomingEvents.Enqueue(ev);
        }
        /// <summary>
        /// Получить уникальный GUID джойстика
        /// </summary>
        /// <returns></returns>
        public string GetJoystickGuid()
        {
            return _deviceInstance.ProductName + ":" + ApplicationSettings.JoystickBindingType ? _deviceInstance.InstanceGuid.ToString() :
                _deviceInstance.ProductGuid;
        }
        /// <summary>
        /// Сдампить состояние кнопок и осей джойстика
        /// </summary>
        /// <param name="dumpMode"></param>
        public void Dump(DumpMode dumpMode)
        {
            lock (_dumpObjectLocker)
            {
                _dumpMode = dumpMode;
            }
        }
    }*/
}
