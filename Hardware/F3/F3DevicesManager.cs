using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting;
using System.Windows.Documents;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.F3
{
    internal class F3DevicesManager : DeviceManagerBase
    {
        public f3ioAPI.BoardInfo Boardslist;
        public override void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            if (Devices.ContainsKey(outgoingEvent.Hardware.MotherBoardId))
                Devices[outgoingEvent.Hardware.MotherBoardId].PostOutgoingEvent(outgoingEvent);
        }
        public override void PostOutgoingEvents(ControlEventBase[] outgoingEvents)
        {
            foreach (var d in Devices)
            {
                d.Value.PostOutgoingEvents(outgoingEvents);
            }
        }

        public override bool Connect()
        {
            Disconnect();
            try
            {
                for (var brd = 0; brd < Boardslist.BoardCount; brd++) //выводи весь список
                {
                    var id = F3Device.GetMotherboardId(Boardslist.Board[brd]);
                    var device = new F3Device(Boardslist.Board[brd]);
                    if(device.Connect())
                        Devices.Add(id, device);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public override void Disconnect()
        {
            base.Disconnect();
            f3ioAPI.CloseBoard();
        }

        /// <summary>
        /// Возвращает данные о том, какие значения могут принимать параметры модуль, контрол, блок в ControlProcessor
        /// </summary>
        /// <param name="cph">Железо</param>
        /// <param name="deviceSubType">Какая часть железа интересует (модуль, блок, контрол)</param>
        /// <returns>список значений, которое может принимать параметр</returns>
        public override Capacity GetCapacity(ControlProcessorHardware cph, DeviceSubType deviceSubType)
        {
            if (deviceSubType == DeviceSubType.Motherboard)
            {
                if (cph.ModuleType == HardwareModuleType.Axis
                    || cph.ModuleType == HardwareModuleType.BinaryOutput
                    || cph.ModuleType == HardwareModuleType.Button
                    || cph.ModuleType == HardwareModuleType.LedMatrixIndicator
                    || cph.ModuleType == HardwareModuleType.SteppingMotor)
                    return new Capacity { Names = GetConnectedDevices().ToArray() };
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            }
            if (!Devices.ContainsKey(cph.MotherBoardId))
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            var device = (F3Device)Devices[cph.MotherBoardId];
            var outInfo = device.OutInfo;

            var extensionDeviceCapacity = new List<int>();

            f3ioAPI.OutType type;
            switch (cph.ModuleType)
            {
                case HardwareModuleType.SteppingMotor:
                    type = f3ioAPI.OutType.oAxis;
                    break;
                case HardwareModuleType.BinaryOutput:
                    type = f3ioAPI.OutType.oLed;
                    break;
                case HardwareModuleType.LedMatrixIndicator:
                    type = f3ioAPI.OutType.oLed;
                    break;
                default:
                    return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
                    
            }
            // Выбираем все модули, которые могут управлять указанным типом железа (HardwareModuleType)
            for (var i = 0; i < outInfo.device.Length; i++)
            {
                if (outInfo.device[i].BlockCount == 0)
                    continue;
                if (outInfo.device[i].Block.Any(b => b.Type == type))
                    extensionDeviceCapacity.Add(i);
            }
            
            // Если запрашивался диапазон значений для модуля - возвращаем результат
            if (deviceSubType == DeviceSubType.ExtensionBoard)
                return new Capacity { Names = extensionDeviceCapacity.ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()};

            // Если запрашивался диапазон для блока, возвращаем все блоки, управляющие нужным типом (например, нужны блоки, управляющие светодиодами. 
            // При этом, нужно исключить блок, управляющий яркостью или шаговыми двигателями)
            if (deviceSubType == DeviceSubType.Block)
            {
                if (!extensionDeviceCapacity.Contains((int) cph.ModuleId))
                    return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
                var block = new List<int>();
                for (var i = 0; i < outInfo.device[cph.ModuleId].BlockCount; i++)
                {
                    if (outInfo.device[cph.ModuleId].Block[i].Type == type)
                        block.Add(i);
                }
                return new Capacity { Names = block.ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray() };
            }
                

            if (deviceSubType != DeviceSubType.Control)
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            // У модуля шагового двигателя шаги задаются в блоке, поэтому нет такого понятия как Control
            if (cph.ModuleType == HardwareModuleType.SteppingMotor)
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            // Параноидальная проверка. Если указанный модуль не существует
            if (!extensionDeviceCapacity.Contains((int)cph.ModuleId))
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            // Если число блоков модуля равно нулю (такого быть не должно)
            if (outInfo.device[cph.ModuleId].BlockCount == 0)
                return new Capacity { DeviceSubtypeIsNotSuitableForCurrentHardware = true };
            
            return cph.ModuleType == HardwareModuleType.LedMatrixIndicator
                ? new Capacity { Names = new[] { 0, 8 }.ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray() }
                : new Capacity { Names = Enumerable.Range(0, outInfo.device[cph.ModuleId].Block[cph.BlockId].Capacity).ToArray().Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()};
        }
    }
}
