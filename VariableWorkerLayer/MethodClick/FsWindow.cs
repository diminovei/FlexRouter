using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace FlexRouter.VariableWorkerLayer.MethodClick
{
    partial class FsWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
    }
    
    partial class FsWindow
    {
        public FsWindow()
        {
            Recreate();
        }
        //  Поля, заполняемые пользователем
        public WindowInfo UserInfo;
        //  Поля, заполняемые роутером
        public string Class;                    //  Класс окна FS98CHILD или FS98FLOAT
        public IntPtr Hwnd;                     //  Хэндл окна
        public Rect Coordinares;                //  Координаты окна
        public bool IsVisible;                  //  Состояние окна (скрыто/не скрыто)
        public string ParentWindowClassName;    //  Класс родительского окна
        public bool IsLocked;                   //  Окно "залочено"?
        private bool _isAppend;                  //  Устанавливается при создании окна, после сохранения изменений удаляется
        private bool _isDeleted;                  //  Устанавливается при создании окна, после сохранения изменений удаляется
        public int ZFactor;
        public void Recreate()
        {
            _isAppend = true;
            _isDeleted = false;
//            if(UserInfo!=null)
//                Debug.Print("Append: " + UserInfo.Name);
        }
        public byte GetChanges()
        {
            return GetWindowChanges(false);
        }
        public byte SaveChanges()
        {
            return GetWindowChanges(true);
        }
        private byte GetWindowChanges(bool andSave)
        {
            byte changes = 0;
            if (Hwnd != IntPtr.Zero)
            {
                var sb = new StringBuilder(300);
                GetWindowText(Hwnd, sb, sb.Capacity);
                if (sb.ToString() != UserInfo.Name)
                    Hwnd = IntPtr.Zero;
            }

            if (!IsWindow(Hwnd))
            {
                if (!_isDeleted)
                {
//                    Debug.Print("Deleted: " + UserInfo.Name);
                    changes |= (byte)WindowChanges.Deleted;
                    _isDeleted = true;
                    ZFactor = -1;
                    return changes;
                }
                changes |= (byte)WindowChanges.NoChanges;
                return changes;
            }
            if(_isAppend)
                changes |= (byte)WindowChanges.Append;
            var isVisible = IsWindowVisible(Hwnd);
            if (IsVisible && !isVisible)
                changes |= (byte)WindowChanges.Hide;
            if (!IsVisible && isVisible)
                changes |= (byte)WindowChanges.Unhide;

            var parentHwnd = GetParent(Hwnd);
            var parentWindowClass = new StringBuilder(256);
            GetClassName(parentHwnd, parentWindowClass, parentWindowClass.Capacity);

            if (ParentWindowClassName != parentWindowClass.ToString())
            { 
                if (!IsLocked && parentWindowClass.ToString() == "FS98MAIN")
                    changes |= (byte)WindowChanges.Locked;
                if (IsLocked && parentWindowClass.ToString() == "FS98FLOAT")
                    changes |= (byte)WindowChanges.Unlocked;
            }
            var coordinates = CountWindowCoordinates();
            if (coordinates.X != Coordinares.X ||
                coordinates.Y != Coordinares.Y ||
                coordinates.Width != Coordinares.Width ||
                coordinates.Height != Coordinares.Height)
                changes |= (byte)WindowChanges.Size;
            if(andSave)
            {
                IsVisible = isVisible;
                ParentWindowClassName = parentWindowClass.ToString();
                Coordinares = coordinates;
                IsLocked = parentWindowClass.ToString() == "FS98MAIN";
                _isAppend = false;
            }
            return changes;
        }

        /// <summary>
        /// Рассчитать координаты окна
        /// </summary>
        private Rect CountWindowCoordinates()
        {
            RECT rect;
            GetWindowRect(Hwnd, out rect);
            
//            var parentHwnd = GetParent(Hwnd);

//            var pt = new Point(0, 0);
//            ClientToScreen(parentHwnd, ref pt);

            return new Rect
            {
  //              X = (rect.Left - pt.X),
//                Y = (rect.Top - pt.Y),
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top
            };
            /*            _simWindows[windowId].X = (int) (rect.Left - pt.X);
                        _simWindows[windowId].Y = (int) (rect.Top - pt.Y);

                        _simWindows[windowId].Width = rect.Right - rect.Left;
                        _simWindows[windowId].Height = rect.Bottom - rect.Top;*/

            /*            var parentHwnd = GetParent(_simWindows[windowId].Hwnd);
                        var windowClass = new StringBuilder(256);
                        GetClassName(parentHwnd, windowClass, windowClass.Capacity);
                        var isUnlocked = windowClass.ToString() == "FS98FLOAT";
                        if (!isUnlocked)
                            wi.RouterY = GetSystemMetrics(SystemMetric.SM_CYSCREEN) - pt.Y;
                        else
                            wi.RouterY = GetSystemMetrics(SystemMetric.SM_CYSCREEN);*/
        }

    }
}
