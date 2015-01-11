using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace FlexRouter.VariableSynchronization
{
    /// <summary>
    /// Описание окна для процессора макросов
    /// </summary>
    public class MacroProcessorWindowInfo
    {
        /// <summary>
        /// Идентификатор окна
        /// </summary>
        public int Id;
        /// <summary>
        /// Хэндл окна
        /// </summary>
        public IntPtr Hwnd;
        /// <summary>
        /// Текущие размеры окна. Нужна для того, чтобы пересчитывать координаты щелчка мышью. Ведь сниматься они могли при одном размере, а воспроизводиться могут в другом
        /// </summary>
        public Rect Size;
    }
    /// <summary>
    /// Поддерживаемые действия мышью
    /// </summary>
    public enum MouseAction
    {
        MouseLeftClick,
        MouseRightClick,
        MouseLeftDoubleClick,
        MouseRightDoubleClick,
        MouseMiddleClick,
        MouseMiddleDoubleClick,
    }
    /// <summary>
    /// Интерфейс действия клавиатурой или мышью
    /// </summary>
    public interface IMacroAction
    {
        string GetActionNameAsText();
    }
    /// <summary>
    /// Мышиное событие в макросе
    /// </summary>
    public class MouseEvent : IMacroAction
    {
        /// <summary>
        /// Мышиное событие
        /// </summary>
        public MouseAction Action;
        /// <summary>
        /// X координата места, куда нужно кликнуть. При расчётах нужно пересчитывать под текущий размер окна
        /// </summary>
        public int MouseX;
        /// <summary>
        /// Y координата места, куда нужно кликнуть. При расчётах нужно пересчитывать под текущий размер окна
        /// </summary>
        public int MouseY;
        /// <summary>
        /// Ширина окна в момент получения координат X, Y
        /// </summary>
        public int WindowWidth;
        /// <summary>
        /// Высота окна в момент получения координат X, Y
        /// </summary>
        public int WindowHeight;
        public string GetActionNameAsText()
        {
            return Enum.GetName(typeof(MouseAction), Action);
        }
    }
    /// <summary>
    /// Клавиатурное событие в макросе
    /// </summary>
    public class KeyboardEvent : IMacroAction
    {
        /// <summary>
        /// Клавиша для симуляции нажатия
        /// </summary>
        public KeyEventArgs Key;
        public string GetActionNameAsText()
        {
            return Key.KeyCode.ToString();
        }
    }
    /// <summary>
    /// Набор связанных действий в макросе. Все клавиши по очереди нажимаются, а потом отжимаются в обратном порядке
    /// </summary>
    public class MacroToken
    {
        /// <summary>
        /// Действия макроса. Выполняются в прямом порядке с действием KeyDown, а потом в обратном порядке с действием KeyUp и игнорированием действий мышью
        /// </summary>
        public IMacroAction[] Actions;
        /// <summary>
        /// Идентификатор окна, над которым производятся действия
        /// </summary>
        public int WindowId;
    }
    /// <summary>
    /// Результат выполнения макроса
    /// </summary>
    public enum PlayMacroResult
    {
        Ok,                                 //  Порядок
        ActivateMainApplicationWindowError, //  Не удалось сделать активным главное окно
        WindowNotFound,                     //  Не удалось найти окно по идентификатору. Возможно, оно отсутствует (другой самолёт? Другая версия панели?)
        UnknownMacroType                    //  Неизвестный тип действий макроса

    }
    /// <summary>
    /// Класс для исполнения макросов
    /// </summary>
    class ClickMacroProcessor
    {
        /// <summary>
        /// Направление для клавиши нажата/отжата
        /// </summary>
        internal enum KeyDirection
        {
            Down = 0,
            Up = 2
        }
        /// <summary>
        /// Сообщения, посылаемые окну при выполнении мышиного макроса
        /// </summary>
        internal enum MouseMessage
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(Keys vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Идентификатор главного окна приложения
        /// </summary>
        private int _mainApplicationWindowId;
        /// <summary>
        /// Список идентификаторов и хэндлов окон для выполнения макроса 
        /// </summary>
        private readonly Dictionary<int, MacroProcessorWindowInfo> _windows = new Dictionary<int, MacroProcessorWindowInfo>();
        /// <summary>
        /// Обновить список окон для работы макросов
        /// </summary>
        /// <param name="windowInfos">Список окон</param>
        /// <param name="mainApplicationWindowId">ID главного окна приложения для того, чтобы делать его активным перед проигрыванием макроса</param>
        /// <returns>false - главное оконо отсутствует среди списка окон</returns>
        public bool RenewWindowsInfo(MacroProcessorWindowInfo[] windowInfos, int mainApplicationWindowId)
        {
            if (windowInfos.All(wi => wi.Id != mainApplicationWindowId))
                return false;
            lock (_windows)
            {
                _windows.Clear();
                foreach (var wi in windowInfos)
                {
                    _windows.Add(wi.Id, wi);
                }
                _mainApplicationWindowId = mainApplicationWindowId;
            }
            return true;
        }
        /// <summary>
        /// Выполнить макрос
        /// </summary>
        /// <param name="macroToken">Список действий</param>
        /// <returns>Результат выполнения макроса</returns>
        public PlayMacroResult PlayMacro(MacroToken[] macroToken)
        {
            //  Делаем основное окно приложения активным
            //  Запоминаем состояние Shift, Ctrl, Alt
            //  Проходимся по токенам
            //  В каждом токене проходимся по Action в прямом порядке, выполняя их. Для клавиатурных делаем KeyDown
            //  В каждом токене проходимся только по клавиатурным Action в обратном порядке, и делаем для всех клавиатурных KeyUp
            //  Восстанавливаем состояние Shift, Ctrl, Alt

            if (!MakeMainApplicationWindowActive())
                return PlayMacroResult.ActivateMainApplicationWindowError;
            var shiftIsDown = IsKeyDown(Keys.ShiftKey);
            var ctrlIsDown = IsKeyDown(Keys.ControlKey);
//            var altIsDown = IsKeyDown(System.Windows.Forms.Keys.Alt);

            foreach (var token in macroToken)
            {
                if (!_windows.ContainsKey(token.WindowId))
                    return PlayMacroResult.WindowNotFound;
                for (var i = 0; i < token.Actions.Length; i++)
                {
                    if (token.Actions[i].GetType() == typeof (MouseEvent))
                    {
                        PlayMouseAction(token, (MouseEvent) token.Actions[i]);
                        continue;
                    }
                    if (token.Actions[i].GetType() == typeof(KeyboardEvent))
                    {
                        keybd_event((byte)((KeyboardEvent)token.Actions[i]).Key.KeyValue, 0, (uint)KeyDirection.Down, 0);
                        continue;
                    }
                    return PlayMacroResult.UnknownMacroType;   
                }

                for (var i = token.Actions.Length-1; i >= 0; i--)
                {
                    if (token.Actions[i].GetType() == typeof(MouseEvent))
                    {
                        continue;
                    }
                    if (token.Actions[i].GetType() == typeof(KeyboardEvent))
                    {
                        keybd_event((byte)((KeyboardEvent)token.Actions[i]).Key.KeyValue, 0, (uint)KeyDirection.Up, 0);
                        continue;
                    }
                    return PlayMacroResult.UnknownMacroType;
                }

            }
            if(shiftIsDown && !IsKeyDown(Keys.ShiftKey))
                keybd_event((byte)Keys.ShiftKey, 0, 0, 0);
            if(ctrlIsDown && !IsKeyDown(Keys.ControlKey))
                keybd_event((byte)Keys.ControlKey, 0, 0, 0);
 //           if(altIsDown && !IsKeyDown(System.Windows.Forms.Keys.Alt))
//                keybd_event((ubyte) Keys.Alt, 0, 0, 0);
            return PlayMacroResult.Ok;
        }
/*        /// <summary>
        /// MakeLParam Macro
        /// </summary>
        private static int MakeLParam(int LoWord, int HiWord)
        {
            return ((HiWord << 16) | (LoWord & 0xffff));
        }*/
        /// <summary>
        /// Выполнить мышиное событие
        /// </summary>
        /// <param name="token"></param>
        /// <param name="action"></param>
        private void PlayMouseAction(MacroToken token, MouseEvent action)
        {
            var hwnd = _windows[token.WindowId].Hwnd;
            var x = (int)(_windows[token.WindowId].Size.Width / action.WindowWidth * action.MouseX);
            var y = (int)(_windows[token.WindowId].Size.Height / action.WindowHeight * action.MouseY);
//            var coords1 = MakeLParam(x, y);
            var coords = x | (y << 16);
            if (action.Action == MouseAction.MouseLeftClick)
            {
                SendMessage(hwnd, (int)MouseMessage.MouseLeftButtonDown, 1, coords);
                Thread.Sleep(1);
                SendMessage(hwnd, (int)MouseMessage.MouseLeftButtonUp, 0, coords);
            }
            if (action.Action == MouseAction.MouseRightClick)
            {
                SendMessage(hwnd, (int)MouseMessage.MouseRightButtonDown, 1, coords);
                Thread.Sleep(1);
                SendMessage(hwnd, (int)MouseMessage.MouseRightButtonUp, 0, coords);
            }
            if (action.Action == MouseAction.MouseMiddleClick)
            {
                SendMessage(hwnd, (int)MouseMessage.MouseMiddleButtonDown, 1, coords);
                Thread.Sleep(1);
                SendMessage(hwnd, (int)MouseMessage.MouseMiddleButtonUp, 0, coords);
            }
        }
        /// <summary>
        /// Сделать главное окно приложения активным, если оно не активно
        /// </summary>
        /// <returns>false - произошло исключение</returns>
        private bool MakeMainApplicationWindowActive()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd != _windows[_mainApplicationWindowId].Hwnd)
                    if (!SetForegroundWindow(_windows[_mainApplicationWindowId].Hwnd))
                        return false;
                return true;
            }
            catch (Exception)
            {
                return false;                
            }
        }
        /// <summary>
        /// Нажата ли клавиша
        /// </summary>
        /// <param name="vKey">Скан-код клавиши</param>
        /// <returns>true - нажата</returns>
        private bool IsKeyDown(Keys vKey)
        {
            return 0 != (GetAsyncKeyState(vKey) & 0x8000);
        }
    }
}
