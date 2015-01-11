using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace FlexRouter.VariableSynchronization
{
    partial class ClickMethodForFs2004
    {
        //  -------------- FS2004 ---------------
        //  Взгляд в сторону/бок определяем по изменению порядка окон (при поиске)
        //  Если COCKPIT - VIEW 00 выше, чем главная панель в 2d виде (Tu154M Main Instrumental Panel), значит включем боковой вид
        //  Все вида (COCKPIT - VIEW 00, VIRTUAL COCKPIT - VIEW 00, TOWER - VIEW 00, SPOT - VIEW 00, TOP DOWN - VIEW 00 - это одно и то же окно у которого меняется имя
        //  Изменение вида можно отследить по изменению имени окна
        //  Locked окна пропадают при переключении из 2d в 3d. В 3d по-умолчанию открываются только Unlocked окна. При переключении в 2d они не скрываются.
        //  Unkocked окна при переключении видов не пропадают
        //  Если в 3d открыть окно (оно будет Unlocked) и потом сделать его Locked, то при переключении 3d вида оно пропадёт и появится в 2d
        //  -------------------------------------


        //  Если панель открыта в главном виде, то в боковом она пропадает.
        //  Если панель открыва в боковом виде, то при возврате на главный она не пропадёт, при возвращении в боковой вид панель откроется в главном виде.
        // Раз в секунду проверяем размеры окон, чтобы учитывать это при наведении мыши на кликах
        // Вряд ли кто-то будет менять размер окна и сразу же кликать/управлять

        // Взгляд в сторону/бок
        //  Tu154M Main Instrumental Panel (FSCHILD) -> Hide
        //  COCKPIT VIEW 00 (FSCHILD)-> Height Change
        //  Панели скрываются
        // Возврат взгляда на главное окно
        //  Tu154M Main Instrumental Panel (FSCHILD)-> Unhide
        //  COCKPIT VIEW 00 (FSCHILD)-> Height Change
        //  Скрытые панели восстанавливаются

        //  Если панель открыта в главном виде, то в боковом она пропадает.
        //  Если панель открыва в боковом виде, то при возврате на главный она не пропадёт, при возвращении в боковой вид панель откроется в главном виде.


        //\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\

        //  Переключение с главного вида
        //  Tu154M Main Instrumental Panel (FSCHILD) -> Hide
        //  COCKPIT - View 00 -> Height
        //  Все открытые панели пропадают
        //  Default Info Window Title -> Unhide (если уже не в этом состоянии)
        //  5 секунд пауза
        //  Default Info Window Title -> Hide

        //  Переключение на главный вид
        //  Tu154M Main Instrumental Panel (FSCHILD) -> Unhide
        //  Default Info Window Title -> Unhide (если уже не в этом состоянии)
        //  VIRTUAL COCKPIT - View 00 -> Height
        //  5 секунд пауза
        //  Default Info Window Title -> Hide

        //  Переключение вида
        //  Default Info Window Title -> Unhide (если уже не в этом состоянии)
        //  5 секунд пауза
        //  Default Info Window Title -> Hide

        //  Переключение видов между собой чаще, чем раз в 5 секунд изменением окна не отловить.
        //  При переключении видов окна принимают вид FSFLOAT и не исчезают, если не были открыты на главном виде в виде FSCHILD
        //  Если окно открыто в главном виде как FSCHILD, то после перелистывания всех видов (где оно пропадёт) и попадании к главный, окно опять откроется как FSCHILD (Hide-перебор видов-Unhide)
        //  Default Info Window Title также меняется при паузе. То есть только на нём основываться нельзя

        //\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\

        //  Определение переключения вида с главного на виртуальный и действия ///////////////////////////////////////////
        //  if(Tu154M Main Instrumental Panel (FSCHILD) -> Hide &&
        //  COCKPIT - View 00 -> Height &&
        //  Default Info Window Title -> Unhide
        //  Переключился вид. Пропавшие панели класса FSCHILD закрыл не пользователь. Открываем все. Они примут класс FSFLOAT

        //  Определение переключения вида с виртуального на главныйи действия ///////////////////////////////////////////
        //  if(Tu154M Main Instrumental Panel (FSCHILD) -> Unhide &&
        //  Default Info Window Title -> Unhide &&
        //  VIRTUAL COCKPIT - View 00 -> Height
        //  Переключился вид. Все панели закрыть и открыть, чтобы они из FSFLOAT стали FSCHILD

        //  Определение взгдяда вбок/сторону и действия ///////////////////////////////////////////
        //  if(Tu154M Main Instrumental Panel (FSCHILD) -> Hide &&
        //  COCKPIT VIEW 00 (FSCHILD)-> Height Change &&
        //  Default Info Window Title !-> Unhide
        //  Закрыть все панели и открыть как FSCHILD

        // Возврат взгляда на главное окно
        //  Tu154M Main Instrumental Panel (FSCHILD)-> Unhide &&
        //  COCKPIT VIEW 00 (FSCHILD)-> Height Change &&
        //  Default Info Window Title !-> Unhide
        //  Ничего не делаем, панели останутся открытыми

        //  Если панель пропала, но смена вида или взгляда не обнаружена, значит это пользователь открыл или закрыл панель. 
        //  Фиксируем действие пользователя. При смене вида/взгляде открытые пользователем панели не восстанавливаем.

        //                var a = System.Windows.Input.KeyFromVirtualKey(0x32);

        enum GetWindowCommand : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommand comandmd);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

//        [DllImport("user32.dll")]
//        private static extern ushort GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Virtual Keys
        /// </summary>
        public enum VirtualKeyCodes
        {
            VK_LEFTMOUSE = -5,
            VK_RIGHTMOUSE = -4,
            VK_MIDDLEMOUSE = -3,
            VK_WHEELUPMOUSE = -2,
            VK_WHEELDOWNMOUSE = -1,
            VK_UNKNOWN = 0,
            VK_LBUTTON = 0x01,   //Left mouse button
            VK_RBUTTON = 0x02,   //Right mouse button
            VK_CANCEL = 0x03,   //Control-break processing
            VK_MBUTTON = 0x04,   //Middle mouse button (three-button mouse)
            VK_BACK = 0x08,   //BACKSPACE key
            VK_TAB = 0x09,   //TAB key
            VK_CLEAR = 0x0C,   //CLEAR key
            VK_RETURN = 0x0D,   //ENTER key
            VK_SHIFT = 0x10,   //SHIFT key
            VK_CONTROL = 0x11,   //CTRL key
            VK_MENU = 0x12,   //ALT key
            VK_PAUSE = 0x13,   //PAUSE key
            VK_CAPITAL = 0x14,   //CAPS LOCK key
            VK_ESCAPE = 0x1B,   //ESC key
            VK_SPACE = 0x20,   //SPACEBAR
            VK_PRIOR = 0x21,   //PAGE UP key
            VK_NEXT = 0x22,   //PAGE DOWN key
            VK_END = 0x23,   //END key
            VK_HOME = 0x24,   //HOME key
            VK_LEFT = 0x25,   //LEFT ARROW key
            VK_UP = 0x26,   //UP ARROW key
            VK_RIGHT = 0x27,   //RIGHT ARROW key
            VK_DOWN = 0x28,   //DOWN ARROW key
            VK_SELECT = 0x29,   //SELECT key
            VK_PRINT = 0x2A,   //PRINT key
            VK_EXECUTE = 0x2B,   //EXECUTE key
            VK_SNAPSHOT = 0x2C,   //PRINT SCREEN key
            VK_INSERT = 0x2D,   //INS key
            VK_DELETE = 0x2E,   //DEL key
            VK_HELP = 0x2F,   //HELP key
            VK_0 = 0x30,   //0 key
            VK_1 = 0x31,   //1 key
            VK_2 = 0x32,   //2 key
            VK_3 = 0x33,   //3 key
            VK_4 = 0x34,   //4 key
            VK_5 = 0x35,   //5 key
            VK_6 = 0x36,    //6 key
            VK_7 = 0x37,    //7 key
            VK_8 = 0x38,   //8 key
            VK_9 = 0x39,    //9 key
            VK_A = 0x41,   //A key
            VK_B = 0x42,   //B key
            VK_C = 0x43,   //C key
            VK_D = 0x44,   //D key
            VK_E = 0x45,   //E key
            VK_F = 0x46,   //F key
            VK_G = 0x47,   //G key
            VK_H = 0x48,   //H key
            VK_I = 0x49,    //I key
            VK_J = 0x4A,   //J key
            VK_K = 0x4B,   //K key
            VK_L = 0x4C,   //L key
            VK_M = 0x4D,   //M key
            VK_N = 0x4E,    //N key
            VK_O = 0x4F,   //O key
            VK_P = 0x50,    //P key
            VK_Q = 0x51,   //Q key
            VK_R = 0x52,   //R key
            VK_S = 0x53,   //S key
            VK_T = 0x54,   //T key
            VK_U = 0x55,   //U key
            VK_V = 0x56,   //V key
            VK_W = 0x57,   //W key
            VK_X = 0x58,   //X key
            VK_Y = 0x59,   //Y key
            VK_Z = 0x5A,    //Z key
            VK_NUMPAD0 = 0x60,   //Numeric keypad 0 key
            VK_NUMPAD1 = 0x61,   //Numeric keypad 1 key
            VK_NUMPAD2 = 0x62,   //Numeric keypad 2 key
            VK_NUMPAD3 = 0x63,   //Numeric keypad 3 key
            VK_NUMPAD4 = 0x64,   //Numeric keypad 4 key
            VK_NUMPAD5 = 0x65,   //Numeric keypad 5 key
            VK_NUMPAD6 = 0x66,   //Numeric keypad 6 key
            VK_NUMPAD7 = 0x67,   //Numeric keypad 7 key
            VK_NUMPAD8 = 0x68,   //Numeric keypad 8 key
            VK_NUMPAD9 = 0x69,   //Numeric keypad 9 key
            VK_MULTIPLY = 0x6A, // *
            VK_ADD = 0x6B,      // +
            VK_SEPARATOR = 0x6C,   //Separator key
            VK_SUBTRACT = 0x6D,   //Subtract key
            VK_DECIMAL = 0x6E,   //Decimal key
            VK_DIVIDE = 0x6F,   //Divide key
            VK_F1 = 0x70,   //F1 key
            VK_F2 = 0x71,   //F2 key
            VK_F3 = 0x72,   //F3 key
            VK_F4 = 0x73,   //F4 key
            VK_F5 = 0x74,   //F5 key
            VK_F6 = 0x75,   //F6 key
            VK_F7 = 0x76,   //F7 key
            VK_F8 = 0x77,   //F8 key
            VK_F9 = 0x78,   //F9 key
            VK_F10 = 0x79,   //F10 key
            VK_F11 = 0x7A,   //F11 key
            VK_F12 = 0x7B,   //F12 key
            VK_NUMLOCK = 0x90, // NumLock
            VK_SCROLL = 0x91,   //SCROLL LOCK key
            VK_LSHIFT = 0xA0,   //Left SHIFT key
            VK_RSHIFT = 0xA1,   //Right SHIFT key
            VK_LCONTROL = 0xA2,   //Left CONTROL key
            VK_RCONTROL = 0xA3,    //Right CONTROL key
            VK_LMENU = 0xA4,      //Left MENU key
            VK_RMENU = 0xA5,   //Right MENU key
            VK_OEM3 = 0xC0,
            VK_OEM_1 = 0xBA, // ;
            VK_OEM_PLUS = 0xBB, // =
            VK_OEM_COMMA = 0xBC, // ,
            VK_OEM_MINUS = 0xBD, // -
            VK_OEM_PERIOD = 0xBE, // -
            VK_OEM_2 = 0xBF, // -
            VK_OEM_4 = 0xDB, // [
            VK_OEM_5 = 0xDC, // Near Enter \|
            VK_OEM_6 = 0xDD, // ]
            VK_OEM_7 = 0xDE, // '
            VK_OEM_102 = 0xE2, // Left \
            VK_PLAY = 0xFA,   //Play key
            VK_ZOOM = 0xFB, //Zoom key
        }

        enum Message
        {
            //MouseLeftButtonDoubleClick = 0x203,
            //MouseLeftButtonClick,
            //MouseMiddleButtonClick,
            //MouseRightButtonClick,
            MouseLeftButtonDown = 0x0201,
            MouseMiddleButtonDown = 0x0207,
            MouseRightButtonDown = 0x204,
            MouseLeftButtonUp = 0x0202,
            MouseMiddleButtonUp = 0x0208,
            MouseRightButtonUp = 0x205,
            //WM_MOUSEWHEEL = 0x20A,
            //WM_MOUSEHWHEEL = 0x20E
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, /*string */int lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, /*IntPtr i*/ int lParam);

        /// <summary>
        /// Delegate for the EnumChildWindows method
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="lParam">Caller-defined variable; we use it for a pointer to our list</param>
        /// <returns>True to continue enumerating, false to bail.</returns>
        public delegate bool EnumWindowsProc(IntPtr hwnd, /*string */ int lParam);
        /// <summary>
        /// Delegate for the EnumChildWindows method
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="lParam">Caller-defined variable; we use it for a pointer to our list</param>
        /// <returns>True to continue enumerating, false to bail.</returns>
        public delegate bool EnumWindowProc(IntPtr hWnd, /*IntPtr parameter*/int lParam);

        private static EnumWindowsProc enumWindowsProc;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = false)]
        static extern UIntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        // Or use System.Drawing.Point (Forms only)
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Point(POINT p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator POINT(Point p)
            {
                return new POINT((int) p.X, (int) p.Y);
            }
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// Flags used with the Windows API (User32.dll):GetSystemMetrics(SystemMetric smIndex)
        ///  
        /// This Enum and declaration signature was written by Gabriel T. Sharp
        /// ai_productions@verizon.net or osirisgothra@hotmail.com
        /// Obtained on pinvoke.net, please contribute your code to support the wiki!
        /// </summary>
        public enum SystemMetric
        {
            /// <summary>
            ///  Width of the screen of the primary display monitor, in pixels. This is the same values obtained by calling GetDeviceCaps as follows: GetDeviceCaps( hdcPrimaryMonitor, HORZRES).
            /// </summary>
            SM_CXSCREEN = 0,
            /// <summary>
            /// Height of the screen of the primary display monitor, in pixels. This is the same values obtained by calling GetDeviceCaps as follows: GetDeviceCaps( hdcPrimaryMonitor, VERTRES).
            /// </summary>
            SM_CYSCREEN = 1,
            /// <summary>
            /// Height of the arrow bitmap on a vertical scroll bar, in pixels.
            /// </summary>
            SM_CYVSCROLL = 20,
            /// <summary>
            /// Width of a vertical scroll bar, in pixels.
            /// </summary>
            SM_CXVSCROLL = 2,
            /// <summary>
            /// Height of a caption area, in pixels.
            /// </summary>
            SM_CYCAPTION = 4,
            /// <summary>
            /// Width of a window border, in pixels. This is equivalent to the SM_CXEDGE value for windows with the 3-D look.
            /// </summary>
            SM_CXBORDER = 5,
            /// <summary>
            /// Height of a window border, in pixels. This is equivalent to the SM_CYEDGE value for windows with the 3-D look.
            /// </summary>
            SM_CYBORDER = 6,
            /// <summary>
            /// Thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME is the height of the horizontal border and SM_CYFIXEDFRAME is the width of the vertical border.
            /// </summary>
            SM_CXDLGFRAME = 7,
            /// <summary>
            /// Thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME is the height of the horizontal border and SM_CYFIXEDFRAME is the width of the vertical border.
            /// </summary>
            SM_CYDLGFRAME = 8,
            /// <summary>
            /// Height of the thumb box in a vertical scroll bar, in pixels
            /// </summary>
            SM_CYVTHUMB = 9,
            /// <summary>
            /// Width of the thumb box in a horizontal scroll bar, in pixels.
            /// </summary>
            SM_CXHTHUMB = 10,
            /// <summary>
            /// Default width of an icon, in pixels. The LoadIcon function can load only icons with the dimensions specified by SM_CXICON and SM_CYICON
            /// </summary>
            SM_CXICON = 11,
            /// <summary>
            /// Default height of an icon, in pixels. The LoadIcon function can load only icons with the dimensions SM_CXICON and SM_CYICON.
            /// </summary>
            SM_CYICON = 12,
            /// <summary>
            /// Width of a cursor, in pixels. The system cannot create cursors of other sizes.
            /// </summary>
            SM_CXCURSOR = 13,
            /// <summary>
            /// Height of a cursor, in pixels. The system cannot create cursors of other sizes.
            /// </summary>
            SM_CYCURSOR = 14,
            /// <summary>
            /// Height of a single-line menu bar, in pixels.
            /// </summary>
            SM_CYMENU = 15,
            /// <summary>
            /// Width of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of the portion of the screen not obscured by the system taskbar or by application desktop toolbars, call the SystemParametersInfo function with the SPI_GETWORKAREA value.
            /// </summary>
            SM_CXFULLSCREEN = 16,
            /// <summary>
            /// Height of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of the portion of the screen not obscured by the system taskbar or by application desktop toolbars, call the SystemParametersInfo function with the SPI_GETWORKAREA value.
            /// </summary>
            SM_CYFULLSCREEN = 17,
            /// <summary>
            /// For double byte character set versions of the system, this is the height of the Kanji window at the bottom of the screen, in pixels
            /// </summary>
            SM_CYKANJIWINDOW = 18,
            /// <summary>
            /// Nonzero if a mouse with a wheel is installed; zero otherwise
            /// </summary>
            SM_MOUSEWHEELPRESENT = 75,
            /// <summary>
            /// Height of a horizontal scroll bar, in pixels.
            /// </summary>
            SM_CYHSCROLL = 3,
            /// <summary>
            /// Width of the arrow bitmap on a horizontal scroll bar, in pixels.
            /// </summary>
            SM_CXHSCROLL = 21,
            /// <summary>
            /// Nonzero if the debug version of User.exe is installed; zero otherwise.
            /// </summary>
            SM_DEBUG = 22,
            /// <summary>
            /// Nonzero if the left and right mouse buttons are reversed; zero otherwise.
            /// </summary>
            SM_SWAPBUTTON = 23,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            SM_RESERVED1 = 24,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            SM_RESERVED2 = 25,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            SM_RESERVED3 = 26,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            SM_RESERVED4 = 27,
            /// <summary>
            /// Minimum width of a window, in pixels.
            /// </summary>
            SM_CXMIN = 28,
            /// <summary>
            /// Minimum height of a window, in pixels.
            /// </summary>
            SM_CYMIN = 29,
            /// <summary>
            /// Width of a button in a window's caption or title bar, in pixels.
            /// </summary>
            SM_CXSIZE = 30,
            /// <summary>
            /// Height of a button in a window's caption or title bar, in pixels.
            /// </summary>
            SM_CYSIZE = 31,
            /// <summary>
            /// Thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
            /// </summary>
            SM_CXFRAME = 32,
            /// <summary>
            /// Thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
            /// </summary>
            SM_CYFRAME = 33,
            /// <summary>
            /// Minimum tracking width of a window, in pixels. The user cannot drag the window frame to a size smaller than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
            /// </summary>
            SM_CXMINTRACK = 34,
            /// <summary>
            /// Minimum tracking height of a window, in pixels. The user cannot drag the window frame to a size smaller than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message
            /// </summary>
            SM_CYMINTRACK = 35,
            /// <summary>
            /// Width of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must occur within the rectangle defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a double-click
            /// </summary>
            SM_CXDOUBLECLK = 36,
            /// <summary>
            /// Height of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must occur within the rectangle defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a double-click. (The two clicks must also occur within a specified time.)
            /// </summary>
            SM_CYDOUBLECLK = 37,
            /// <summary>
            /// Width of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING by SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CXICON
            /// </summary>
            SM_CXICONSPACING = 38,
            /// <summary>
            /// Height of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING by SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CYICON.
            /// </summary>
            SM_CYICONSPACING = 39,
            /// <summary>
            /// Nonzero if drop-down menus are right-aligned with the corresponding menu-bar item; zero if the menus are left-aligned.
            /// </summary>
            SM_MENUDROPALIGNMENT = 40,
            /// <summary>
            /// Nonzero if the Microsoft Windows for Pen computing extensions are installed; zero otherwise.
            /// </summary>
            SM_PENWINDOWS = 41,
            /// <summary>
            /// Nonzero if User32.dll supports DBCS; zero otherwise. (WinMe/95/98): Unicode
            /// </summary>
            SM_DBCSENABLED = 42,
            /// <summary>
            /// Number of buttons on mouse, or zero if no mouse is installed.
            /// </summary>
            SM_CMOUSEBUTTONS = 43,
            /// <summary>
            /// Identical Values Changed After Windows NT 4.0  
            /// </summary>
            SM_CXFIXEDFRAME = SM_CXDLGFRAME,
            /// <summary>
            /// Identical Values Changed After Windows NT 4.0
            /// </summary>
            SM_CYFIXEDFRAME = SM_CYDLGFRAME,
            /// <summary>
            /// Identical Values Changed After Windows NT 4.0
            /// </summary>
            SM_CXSIZEFRAME = SM_CXFRAME,
            /// <summary>
            /// Identical Values Changed After Windows NT 4.0
            /// </summary>
            SM_CYSIZEFRAME = SM_CYFRAME,
            /// <summary>
            /// Nonzero if security is present; zero otherwise.
            /// </summary>
            SM_SECURE = 44,
            /// <summary>
            /// Width of a 3-D border, in pixels. This is the 3-D counterpart of SM_CXBORDER
            /// </summary>
            SM_CXEDGE = 45,
            /// <summary>
            /// Height of a 3-D border, in pixels. This is the 3-D counterpart of SM_CYBORDER
            /// </summary>
            SM_CYEDGE = 46,
            /// <summary>
            /// Width of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when arranged. This value is always greater than or equal to SM_CXMINIMIZED.
            /// </summary>
            SM_CXMINSPACING = 47,
            /// <summary>
            /// Height of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when arranged. This value is always greater than or equal to SM_CYMINIMIZED.
            /// </summary>
            SM_CYMINSPACING = 48,
            /// <summary>
            /// Recommended width of a small icon, in pixels. Small icons typically appear in window captions and in small icon view
            /// </summary>
            SM_CXSMICON = 49,
            /// <summary>
            /// Recommended height of a small icon, in pixels. Small icons typically appear in window captions and in small icon view.
            /// </summary>
            SM_CYSMICON = 50,
            /// <summary>
            /// Height of a small caption, in pixels
            /// </summary>
            SM_CYSMCAPTION = 51,
            /// <summary>
            /// Width of small caption buttons, in pixels.
            /// </summary>
            SM_CXSMSIZE = 52,
            /// <summary>
            /// Height of small caption buttons, in pixels.
            /// </summary>
            SM_CYSMSIZE = 53,
            /// <summary>
            /// Width of menu bar buttons, such as the child window close button used in the multiple document interface, in pixels.
            /// </summary>
            SM_CXMENUSIZE = 54,
            /// <summary>
            /// Height of menu bar buttons, such as the child window close button used in the multiple document interface, in pixels.
            /// </summary>
            SM_CYMENUSIZE = 55,
            /// <summary>
            /// Flags specifying how the system arranged minimized windows
            /// </summary>
            SM_ARRANGE = 56,
            /// <summary>
            /// Width of a minimized window, in pixels.
            /// </summary>
            SM_CXMINIMIZED = 57,
            /// <summary>
            /// Height of a minimized window, in pixels.
            /// </summary>
            SM_CYMINIMIZED = 58,
            /// <summary>
            /// Default maximum width of a window that has a caption and sizing borders, in pixels. This metric refers to the entire desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
            /// </summary>
            SM_CXMAXTRACK = 59,
            /// <summary>
            /// Default maximum height of a window that has a caption and sizing borders, in pixels. This metric refers to the entire desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
            /// </summary>
            SM_CYMAXTRACK = 60,
            /// <summary>
            /// Default width, in pixels, of a maximized top-level window on the primary display monitor.
            /// </summary>
            SM_CXMAXIMIZED = 61,
            /// <summary>
            /// Default height, in pixels, of a maximized top-level window on the primary display monitor.
            /// </summary>
            SM_CYMAXIMIZED = 62,
            /// <summary>
            /// Least significant bit is set if a network is present; otherwise, it is cleared. The other bits are reserved for future use
            /// </summary>
            SM_NETWORK = 63,
            /// <summary>
            /// Value that specifies how the system was started: 0-normal, 1-failsafe, 2-failsafe /w net
            /// </summary>
            SM_CLEANBOOT = 67,
            /// <summary>
            /// Width of a rectangle centered on a drag point to allow for limited movement of the mouse pointer before a drag operation begins, in pixels.
            /// </summary>
            SM_CXDRAG = 68,
            /// <summary>
            /// Height of a rectangle centered on a drag point to allow for limited movement of the mouse pointer before a drag operation begins. This value is in pixels. It allows the user to click and release the mouse button easily without unintentionally starting a drag operation.
            /// </summary>
            SM_CYDRAG = 69,
            /// <summary>
            /// Nonzero if the user requires an application to present information visually in situations where it would otherwise present the information only in audible form; zero otherwise.
            /// </summary>
            SM_SHOWSOUNDS = 70,
            /// <summary>
            /// Width of the default menu check-mark bitmap, in pixels.
            /// </summary>
            SM_CXMENUCHECK = 71,
            /// <summary>
            /// Height of the default menu check-mark bitmap, in pixels.
            /// </summary>
            SM_CYMENUCHECK = 72,
            /// <summary>
            /// Nonzero if the computer has a low-end (slow) processor; zero otherwise
            /// </summary>
            SM_SLOWMACHINE = 73,
            /// <summary>
            /// Nonzero if the system is enabled for Hebrew and Arabic languages, zero if not.
            /// </summary>
            SM_MIDEASTENABLED = 74,
            /// <summary>
            /// Nonzero if a mouse is installed; zero otherwise. This value is rarely zero, because of support for virtual mice and because some systems detect the presence of the port instead of the presence of a mouse.
            /// </summary>
            SM_MOUSEPRESENT = 19,
            /// <summary>
            /// Windows 2000 (v5.0+) Coordinate of the top of the virtual screen
            /// </summary>
            SM_XVIRTUALSCREEN = 76,
            /// <summary>
            /// Windows 2000 (v5.0+) Coordinate of the left of the virtual screen
            /// </summary>
            SM_YVIRTUALSCREEN = 77,
            /// <summary>
            /// Windows 2000 (v5.0+) Width of the virtual screen
            /// </summary>
            SM_CXVIRTUALSCREEN = 78,
            /// <summary>
            /// Windows 2000 (v5.0+) Height of the virtual screen
            /// </summary>
            SM_CYVIRTUALSCREEN = 79,
            /// <summary>
            /// Number of display monitors on the desktop
            /// </summary>
            SM_CMONITORS = 80,
            /// <summary>
            /// Windows XP (v5.1+) Nonzero if all the display monitors have the same color format, zero otherwise. Note that two displays can have the same bit depth, but different color formats. For example, the red, green, and blue pixels can be encoded with different numbers of bits, or those bits can be located in different places in a pixel's color value.
            /// </summary>
            SM_SAMEDISPLAYFORMAT = 81,
            /// <summary>
            /// Windows XP (v5.1+) Nonzero if Input Method Manager/Input Method Editor features are enabled; zero otherwise
            /// </summary>
            SM_IMMENABLED = 82,
            /// <summary>
            /// Windows XP (v5.1+) Width of the left and right edges of the focus rectangle drawn by DrawFocusRect. This value is in pixels.
            /// </summary>
            SM_CXFOCUSBORDER = 83,
            /// <summary>
            /// Windows XP (v5.1+) Height of the top and bottom edges of the focus rectangle drawn by DrawFocusRect. This value is in pixels.
            /// </summary>
            SM_CYFOCUSBORDER = 84,
            /// <summary>
            /// Nonzero if the current operating system is the Windows XP Tablet PC edition, zero if not.
            /// </summary>
            SM_TABLETPC = 86,
            /// <summary>
            /// Nonzero if the current operating system is the Windows XP, Media Center Edition, zero if not.
            /// </summary>
            SM_MEDIACENTER = 87,
            /// <summary>
            /// Metrics Other
            /// </summary>
            SM_CMETRICS_OTHER = 76,
            /// <summary>
            /// Metrics Windows 2000
            /// </summary>
            SM_CMETRICS_2000 = 83,
            /// <summary>
            /// Metrics Windows NT
            /// </summary>
            SM_CMETRICS_NT = 88,
            /// <summary>
            /// Windows XP (v5.1+) This system metric is used in a Terminal Services environment. If the calling process is associated with a Terminal Services client session, the return value is nonzero. If the calling process is associated with the Terminal Server console session, the return value is zero. The console session is not necessarily the physical console - see WTSGetActiveConsoleSessionId for more information.
            /// </summary>
            SM_REMOTESESSION = 0x1000,
            /// <summary>
            /// Windows XP (v5.1+) Nonzero if the current session is shutting down; zero otherwise
            /// </summary>
            SM_SHUTTINGDOWN = 0x2000,
            /// <summary>
            /// Windows XP (v5.1+) This system metric is used in a Terminal Services environment. Its value is nonzero if the current session is remotely controlled; zero otherwise
            /// </summary>
            SM_REMOTECONTROL = 0x2001,
        }
    }
    partial class ClickMethodForFs2004
    {
        // Временно public
        public Dictionary<int, FsWindow> _simWindows = new Dictionary<int,FsWindow>();

        public int _simulatorMainWindowId = -1;
        /// <summary>
        /// Добавить окно в список окон
        /// </summary>
        /// <param name="window">Информация о добавляемом окне</param>
        /// <param name="andGenerateNewId"></param>
        /// <returns></returns>
        public int AddWindow(WindowInfo window, bool andGenerateNewId)
        {
            if (!andGenerateNewId)
            {
                if (window.Id == -1)
                    return -1;
                if (_simWindows.ContainsKey(window.Id))
                    return -1;
            }
            var newWindow = new FsWindow();
            window.Id = andGenerateNewId ? Utils.GetNewId(_simWindows) : window.Id;
            newWindow.UserInfo = window;
            _simWindows.Add(window.Id, newWindow);
            return window.Id;
        }
        public ClickMethodForFs2004()
        {
            var windowId = 0;
            var windowInfo = new WindowInfo { Name = "Microsoft Flight Simulator 2004 - A Century of Flight", Id = windowId++};
            AddWindow(windowInfo, false);
            _simulatorMainWindowId = windowInfo.Id;

            windowInfo = new WindowInfo { Name = "COCKPIT - View 00", Id = windowId++};
            AddWindow(windowInfo, false);

            windowInfo = new WindowInfo { Name = "VIRTUAL COCKPIT - View 00", Id = windowId++ };
            AddWindow(windowInfo, false);

            windowInfo = new WindowInfo { Name = "TOWER - View 00", Id = windowId++ };
            AddWindow(windowInfo, false);

            windowInfo = new WindowInfo { Name = "SPOT - View 00", Id = windowId++ };
            AddWindow(windowInfo, false);

            windowInfo = new WindowInfo { Name = "TOP DOWN - View 00", Id = windowId++ };
            AddWindow(windowInfo, false);
        }

/*        /// <summary>
        /// Add simulator and plane main windows first to initialize
        /// </summary>
        /// <param name="simulatorMainWindowId"></param>
        /// <param name="planeMainWindowId"></param>
        /// <returns></returns>
        public bool Initialize(int simulatorMainWindowId, int planeMainWindowId)
        {
            if (!_simWindows.ContainsKey(simulatorMainWindowId) || !_simWindows.ContainsKey(planeMainWindowId))
            {
                MessageBox.Show("Can't find Plane Window");
                return false;
            }
            _simulatorMainWindowId = simulatorMainWindowId;
            if (!FindMainWindowHwnd(_simulatorMainWindowId))
            {
                MessageBox.Show("Can't find Sim Window");
                return false;
            }
            MessageBox.Show("Initialized");
            return true;
        }*/
        public void FindAddedWindows()
        {
            var hwnd = GetWindow(GetDesktopWindow(), GetWindowCommand.GW_CHILD);
            FindWindowHandlesRecurse(hwnd, ref _simWindows, 0);
        }
        public void WatchWindows()
        {
            var simChangeSightEvent = Fs2004ChangeSightEvent.Nothing;
            while (true)
            {
                var a = GetSimulatorChangeSightEvent();
                FindAddedWindows();
                if (a != simChangeSightEvent)
                {
//                    Debug.Print(a.ToString());
                    simChangeSightEvent = a;
                }
//                Debug.Print("Got changes");
                foreach (var window in _simWindows)
                {
                    window.Value.SaveChanges();
                }
//                Debug.Print("Saved changes");
                Thread.Sleep(5000);
            }
        }
/*        /// <summary>
        /// Поиск хэндла главного окна симулятора
        /// </summary>
        /// <param name="windowId">ID искомого окна</param>
        /// <returns>true - окно найдено, хэндл записан в словарь окон</returns>
        private bool FindMainWindowHwnd(int windowId)
        {
            var hWnd = _simWindows[windowId].Hwnd;
            if (IsWindow(hWnd))
                return true;

            EnumWindows(EnumChildWindowsProc, windowId);
            return IsWindow(_simWindows[windowId].Hwnd);
        }

        /// <summary>
        /// Коллбэк для поиска дочерних окон
        /// </summary>
        /// <param name="hWnd">Хэндл родительского окна</param>
        /// <param name="windowId">ID искомого окна</param>
        /// <returns>true - окно не найдено</returns>
        private bool EnumChildWindowsProc(IntPtr hWnd, int windowId)
        {
            var sb = new StringBuilder(300);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (!sb.ToString().Contains(_simWindows[windowId].UserInfo.Name))
                return true;

            _simWindows[windowId].Hwnd = hWnd;
            return false;
        }*/

        /// <summary>
        /// Рекурсивный поиск окон
        /// </summary>
        /// <param name="hwnd">Хэндл родительского окна</param>
        /// <param name="data"></param>
        /// <returns>true - окно найдено</returns>
        private int FindWindowHandlesRecurse(IntPtr hwnd, ref Dictionary<int, FsWindow> data, int zFactor)
        {
            while (IsWindow(hwnd))
            {
                var winName = new StringBuilder(1000);
                GetWindowText(hwnd, winName, winName.Capacity);
                
                var winClass = new StringBuilder(256);
                GetClassName(hwnd, winClass, winClass.Capacity);

                // Найти окно по имени. Если это виртуальный вид, то имя окна меняется, остаётся неизменным только View 00
                var id = FindWindowIdByName(winName.ToString());
                if (id!=-1 && (winClass.ToString() == "FS98CHILD"||winClass.ToString() == "FS98MAIN"))
                {
                    if (data[id].Hwnd == IntPtr.Zero)
                    {
                        data[id].Hwnd = hwnd;
                        data[id].Recreate();
                    }
                    data[id].ZFactor = zFactor;
                    zFactor++;
                }
                zFactor = FindWindowHandlesRecurse(GetWindow(hwnd, GetWindowCommand.GW_CHILD), ref data, zFactor);
                hwnd = GetWindow(hwnd, GetWindowCommand.GW_HWNDNEXT);
            }
            return zFactor;
        }

        public enum Fs2004ChangeSightEvent
        {
            Nothing,                    //  Ничего не изменилось
            VirtualCockpitToMainPanel,  //  Переключение в виртуальную кабину из 2d
            MainPanelToVirtualCockpit,  //  Переключение из виртуальной кабины в 2d
            SideCockpitView,            //  Сменился внутрикабинный вид (налево, вверх, ...)
            NormalCockpitView           //  Сменился внутрикабинный вид. Взгляд на главную панель
        }

        private Fs2004ChangeSightEvent _lastChangeSightEvent = Fs2004ChangeSightEvent.Nothing;

        public Fs2004ChangeSightEvent GetSimulatorChangeSightEvent()
        {
            var mainPanelId = FindWindowIdByName("Tu-154M Main Instrument Panel");
            var mainPanelChanges = _simWindows[mainPanelId].GetChanges();
            var infoTitle = FindWindowIdByName("Default InfoWindow Title");
            var infoTitleChanges = _simWindows[infoTitle].GetChanges();
            var virtualCockpit = FindWindowIdByName("VIRTUAL COCKPIT - View 00");
            var virtualCockpitChanges = _simWindows[virtualCockpit].GetChanges();
            var cockpit = FindWindowIdByName("COCKPIT - View 00");
            var cockpitChanges = _simWindows[cockpit].GetChanges();
            var spotView = FindWindowIdByName("SPOT - View 00");
            var spotChanges = _simWindows[spotView].GetChanges();
            var towerView = FindWindowIdByName("TOWER - View 00");
            var towerChanges = _simWindows[towerView].GetChanges();
            var topDownView = FindWindowIdByName("TOP DOWN - View 00");
            var topDownChanges = _simWindows[topDownView].GetChanges();
                if((cockpitChanges&(byte)WindowChanges.Append) != 0)
                    return Fs2004ChangeSightEvent.VirtualCockpitToMainPanel;
                if ((cockpitChanges & (byte)WindowChanges.Deleted) != 0)
                    return Fs2004ChangeSightEvent.MainPanelToVirtualCockpit;

            return Fs2004ChangeSightEvent.Nothing;
        }

        /// <summary>
        /// Найти идентификатор окна по имени
        /// </summary>
        /// <param name="name">Имя окна</param>
        /// <returns>Идентификатор окна или -1, если окна с таким именем нет</returns>
        private int FindWindowIdByName(string name)
        {
            foreach (var window in _simWindows)
            {
                if (window.Value.UserInfo.Name == name)
                    return window.Key;
            }
            return -1;
        }
    }
}
