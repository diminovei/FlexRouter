using System;
using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FTD2XX_NET;

namespace FlexRouter.Hardware.Arcc
{
    internal class ArccDevicesManager : DeviceManagerBase
    {
        /// <summary>
        /// Префикс, который в паре с идентификатором формирует уникальный идентификатор устройства
        /// </summary>
        private const string DevicePrefix = "Arcc";

        public override void PostOutgoingEvent(ControlEventBase outgoingEvent)
        {
            if (Devices.ContainsKey(outgoingEvent.Hardware.MotherBoardId))
                Devices[outgoingEvent.Hardware.MotherBoardId].PostOutgoingEvent(outgoingEvent);
       }

        public override bool Connect()
        {
            Disconnect();
            try
            {
                var foundDevices = new Dictionary<string, ArccDevice>();
                var myFtdiDevice = new FTDI();
                uint ftdiDeviceCount = 0;

                var ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return false;
                var ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
                ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return false;

                for (var i = 0; i < ftdiDeviceList.Length; i++)
                {
                    const string descriptionPattern = "ARCC";
                    if (!ftdiDeviceList[i].Description.StartsWith(descriptionPattern))
                        continue;
                    ftStatus = myFtdiDevice.OpenByIndex((uint) i);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        continue;

                    string comPort;
                    var chipId = 0;

                    ftStatus = myFtdiDevice.GetCOMPort(out comPort);
                    if (ftStatus != FTDI.FT_STATUS.FT_OK)
                        continue;

                    myFtdiDevice.Close();
                    FTChipID.ChipID.GetDeviceChipID(i, ref chipId);
                    var id = DevicePrefix + ":" + chipId.ToString("X");
                    var device = new ArccDevice(id, chipId, comPort);
                    foundDevices.Add(id, device);
                }
                foreach (var arccDevice in foundDevices)
                {
                    if (arccDevice.Value.Connect())
                        Devices.Add(arccDevice.Key, arccDevice.Value);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
        public override void DumpModule(ControlProcessorHardware[] hardware)
        {
            foreach (var item in hardware)
                foreach (var device in Devices.Values)
                    if (((ArccDevice)device).Id == item.MotherBoardId)
                        device.DumpModule(hardware);
        }
    }
}
