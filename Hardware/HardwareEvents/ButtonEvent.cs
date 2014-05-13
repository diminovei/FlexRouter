namespace FlexRouter.Hardware.HardwareEvents
{
    /// <summary>
    /// Входящее событие от кнопки или модуля бинарного ввода
    /// </summary>
    public class ButtonEvent : ControlEventBase
    {
        /// <summary>
        /// Кнопка нажата?
        /// </summary>
        public bool IsPressed;
    }
}
