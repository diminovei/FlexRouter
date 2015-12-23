using System;
using System.Runtime.InteropServices;

public class f3ioAPI
{
    public const string DllName = "f3io.dll";
    public const Int32 _HEADER_VERSION = 5;

    public const Int32 _STBUFSIZE_ = 256;
    public const Int32 _EVBUFSIZE_ = 256;

    public const Int32 _VIRTUAL_AXIS_MAX_COUNT_ = 16;
    public const Int32 _VIRTUAL_BUTTONS_MAX_COUNT_ = 256;

    public const Int32 _DESCRIPTOR_MAX_SIZE_ = 1024;
    public const Int32 _DESCRIPTOR_MAX_CMD_COUNT_ = 256;
    public const Int32 _OUT_BLOCK_MAX_COUNT_ = 32;

    public const Int32 _EMPTY_DESCRIPTOR = 0;
    public const Int32 _CONNECTION_ERROR = -1;
    public const Int32 _INVALID_DESCRIPTOR_TYPE = -2;
    public const Int32 _INVALID_JOYSTICK_NUMBER = -3;
    public const Int32 _INVALID_DESCRIPTOR_SIZE = -4;

    public enum InterfaceInfo : int
    {
        nSPI,
        nUART,
        nVIRT,
        nALL,
        TotalMemoryInBuff,
        UsedMemoryInBuff,
    };
    public enum OutType : byte
    {
        oLed,
        oAxis,
        oControl
    };

    [StructLayout(LayoutKind.Sequential, Pack=4, CharSet = CharSet.Unicode)]
    public struct BoardInfo_
    {
        private Int32 chipsignature;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string hidname;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string sn;
        private Int16 firmwarevirsion;

        public Int32 ChipSignature
        {
            get { return chipsignature; }
        }
        public Int16 FirmwareVirsion
        {
            get { return firmwarevirsion; }
        }
        public string HIDname
        {
            get { return hidname; }
        }
        public string Name
        {
            get { return name; }
        }
        public string SN
        {
            get { return sn; }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BoardInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public BoardInfo_[] Board;
        public int BoardCount
        {
            get
            {
                getBoardList(ref this, 0);
                return getBoardList(ref this, (Int16)this.Board.Length);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct DeviceGeneral
    {
        private byte nDevice;
        private Int32 insrumentid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string devicename;
        public Int32 InsrumentID
        {
            get { return insrumentid; }
        }
        public string DeviceName
        {
            get { return devicename; }
        }
        public bool GetInfo(int nDevice)
        {
            return getDeviceGeneralInfo(ref this, (byte)nDevice);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Axis
    {
        public UInt16 value;
        public UInt16 range;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DeviceIN
    {
        private UInt16 __addr___;
        private byte nDevice;
        private byte axiscount;
        private byte buttonscount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = _VIRTUAL_AXIS_MAX_COUNT_)]
        public Axis[] axis;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = _VIRTUAL_BUTTONS_MAX_COUNT_)]
        public bool[] butt;
        public byte AxisCount
        {
            get { return axiscount; }
        }
        public byte ButtonsCount
        {
            get { return buttonscount; }
        }
        public bool GetInfo(int nDevice)
        {
            return getDeviceInInfo(ref this, (byte)nDevice);
        }
        public bool getData()
        {
            return getInData(ref this, 1);
        }
        public bool getData(IntPtr handlr)
        {
            return mgetInData(handlr, ref this, 1);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    protected struct OutData
    {
        public Int16 actual;
        public Int16 previous;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OutBlock
    {
        private OutType oType;
        private sbyte capacity;
        private OutData data;
        public OutType Type
        {
            get 
            {
                switch (oType)
                {
                    case OutType.oAxis:
                    case OutType.oControl:
                    case OutType.oLed:
                        return oType;
                }
                return OutType.oLed; 
            }
        }
        public int Capacity
        {
            get 
            {
                int result = (int)capacity;
                switch (Type)
                {
                    case OutType.oLed: // от 1 до 16
                        if (result < 0)
                        {
                            result = -result;
                        }
                        if (result == 0)
                        {
                            result = 1;
                        }
                        else if (result > 16)
                        {
                            result = 16;
                        }
                        break;
                    default: //-8 (диапазон от -127 до +128), -16 (диапазон от -32767 до +32768), +8 (диапазон от 0 до +255), +16 (диапазон от 0 до +65535),
                        if (result < 0)
                        {
                            if (result < -8)
                            {
                                result = -16;
                            }
                            else
                            {
                                result = -8;
                            }
                        }
                        else if (result > 8)
                        {
                            result = 16;
                        }
                        else
                        {
                            result = 8;
                        }
                        break;
                }
                return result; 
            }
        }
        public Int16 Data
        {
            get { return data.actual; }
            set { data.actual = value; }
        }
        public Int16 InitData
        {
            set { data.actual = value; data.previous = value; }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DeviceOUT_
    {
        private byte ndevice;
        private byte blockcount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = _OUT_BLOCK_MAX_COUNT_)]
        public OutBlock[] Block;
        public bool SendData()
        {
            return sendOutPackets(ref this, 0, 0);
        }
        public bool SendData(IntPtr handle)
        {
            return msendOutPackets(handle, ref this, 0, 0);
        }
        public int BlockCount
        {
            get { return (int)blockcount; }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DeviceOUT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 156)]
        public DeviceOUT_[] device;
        public bool GetInfo()
        {
            getDeviceOutInfo(ref this, 0);
            for (int i = 1; i < device.Length; i++)
            {
                getDeviceOutInfo(ref device[i], (byte)i);
            }
            return true;
        }
        public bool SendData()
        {
            return sendOutPackets(ref this, 0, device.Length);
        }
        public bool SendData(IntPtr handle)
        {
            return msendOutPackets(handle, ref this, 0, device.Length);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct TextLed
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string name;
        public string Name
        {
            get { return name; }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct TextBlock
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = _STBUFSIZE_)]
        private string name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public TextLed[] Led;
        public string Name
        {
            get { return name; }
        }
    }
    public struct OutText
    {
        public TextBlock[] Block;
        public bool LoadText(byte nDev)
        {
            if (!initTextBlock(nDev))
            {
                return false;
            }
            if (Block == null)
            {
                Block = new TextBlock[_OUT_BLOCK_MAX_COUNT_];
            }
            for (int i = 0; i < Block.Length; i++)
            {
                if (!getOutTextBlock(ref Block[i], (byte)i))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ButtonEventsData
    {
        public byte nDev;
        public byte nButt;
        public bool State;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ButtonEvents
    {
        private byte len;
        private byte eventscount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = _EVBUFSIZE_)]
        public ButtonEventsData[] EventsData;
        public byte EventsCount
        {
            get { if (eventscount > len) { return len; } return eventscount; }
        }
        public bool Overflow
        {
            get { return eventscount > len; }
        }
        public bool getEvents()
        {
            return getVirtButtQueue(ref this);
        }
        public bool getEvents(IntPtr handle)
        {
            return mgetVirtButtQueue(handle, ref this);
        }
    }



    // *************** Общие функции *************** //
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern Int32 getDllVersion();

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern Int16 getBoardList(ref BoardInfo list, Int16 listlen);


    // *************** Основные функции работы с выбранным контроллером *************** //
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern bool OpenBoard(String HidName);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool CloseBoard();

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenConfiguration(Int32 nRetries);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool CloseConfiguration();

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern Int32 getMainInterfaceInfo(InterfaceInfo info);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getDeviceGeneralInfo(ref DeviceGeneral GeneralInfo, byte ndev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getDeviceInInfo(ref DeviceIN InInfo, byte ndev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getDeviceOutInfo(ref DeviceOUT packetStruct, byte ndev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getDeviceOutInfo(ref DeviceOUT_ packetStruct, byte ndev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getInData(ref DeviceIN InData, Int32 deviceCount);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool sendOutPackets(ref DeviceOUT packets, Int32 StartPaket, Int32 FinalPaket);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool sendOutPackets(ref DeviceOUT_ packets, Int32 StartPaket, Int32 FinalPaket);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getVirtButtQueue(ref ButtonEvents eventsdata);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool DevReset(byte nDev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool GlobalReset();

    [DllImport(f3ioAPI.DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool connectionEstablished(byte nDev);

    [DllImport(f3ioAPI.DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool connectionEnumerated(byte nDev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool initTextBlock(byte nDev);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool getOutTextBlock(ref TextBlock outtext, byte nBlock);

    //


    // *************** Дополнительные функции работы в мультиконтроллерном режиме *************** //
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern IntPtr mOpenBoard(String HidName);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr mgetHandle();

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern void msetHandle(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool mCloseBoard(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool mgetInData(IntPtr handle, ref DeviceIN InData, Int32 deviceCount);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool msendOutPackets(IntPtr handle, ref DeviceOUT packets, Int32 StartPaket, Int32 FinalPaket);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool msendOutPackets(IntPtr handle, ref DeviceOUT_ packets, Int32 StartPaket, Int32 FinalPaket);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    private static extern bool mgetVirtButtQueue(IntPtr handle, ref ButtonEvents eventsdata);
}
