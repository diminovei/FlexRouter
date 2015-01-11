using System;
using System.Collections.Generic;
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
        public override int[] GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            return (cph.ModuleType == HardwareModuleType.Button && deviceSubType == DeviceSubType.Control) ? Enumerable.Range(0, 255).ToArray() : null;
        }
    }
}
