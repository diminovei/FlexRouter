using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using SlimDX.DirectInput;

namespace FlexRouter.Hardware.Joystick
{
    internal class JoystickDevicesManager : DeviceManagerBase
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
            catch(ArgumentException ex)
            {
                var message = LanguageManager.GetPhrase(Phrases.SettingsHardwareGuidConflict);
                var header = LanguageManager.GetPhrase(Phrases.MainFormName) + " - " + LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public override Capacity GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            if (deviceSubType == DeviceSubType.Motherboard && (cph.ModuleType == HardwareModuleType.Button || cph.ModuleType == HardwareModuleType.Axis))
                return new Capacity { Names = GetConnectedDevices().ToArray() };
            if (cph.ModuleType == HardwareModuleType.Axis && deviceSubType == DeviceSubType.Control)
                return new Capacity {Names = Enumerable.Range(0, ((JoystickDevice) Devices[cph.MotherBoardId]).GetCapabilities().AxesCount).ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()};
            if (cph.ModuleType == HardwareModuleType.Button && deviceSubType == DeviceSubType.Control)
                return new Capacity { Names = Enumerable.Range(0, ((JoystickDevice) Devices[cph.MotherBoardId]).GetCapabilities().ButtonCount).ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()};
            return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
        }
    }
}
