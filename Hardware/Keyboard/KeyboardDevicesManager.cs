using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using SlimDX.DirectInput;

namespace FlexRouter.Hardware.Keyboard
{
    internal class KeyboardDevicesManager : DeviceManagerBase
    {
        public override void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
        }
        public override void PostOutgoingEvents(ControlEventBase[] outgoingEvent)
        {
        }

        public override bool Connect()
        {
            Disconnect();
            try
            {
                // Find all the GameControl devices that are attached.
                var dinput = new DirectInput();
                foreach (var di in dinput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly))
                {
                    var keyboard = new KeyboardDevice(di, dinput);
                    Devices.Add(keyboard.GetDeviceGuid(), keyboard);
                    keyboard.Connect();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public override Capacity GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            if (cph.ModuleType != HardwareModuleType.Button)
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };

            switch (deviceSubType)
            {
                case DeviceSubType.Motherboard:
                    return new Capacity { Names = GetConnectedDevices().ToArray() };
                case DeviceSubType.Control:
                    return new Capacity {Names = Enumerable.Range(0, 255).ToArray().Select(x=>x.ToString(CultureInfo.InvariantCulture)).ToArray()};
                default:
                    return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            }
        }
    }
}
