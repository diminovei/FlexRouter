using System;
using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using SlimDX.DirectInput;

namespace FlexRouter.Hardware.Joystick
{
    internal class JoystickDevicesManager : DeviceManagerBase
    {
        public override void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
        }

        public override bool Connect()
        {
            Disconnect();
            try
            {
                // Find all the GameControl devices that are attached.
                var dinput = new DirectInput();
                foreach (
                    var di in
                        dinput.GetDevices(DeviceClass.GameController,
                            DeviceEnumerationFlags.AttachedOnly))
                {
                    var joy = new JoystickDevice(di, dinput);
                    Devices.Add(joy.GetJoystickGuid(), joy);
                    joy.Connect();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
