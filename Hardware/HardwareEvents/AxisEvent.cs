namespace FlexRouter.Hardware.HardwareEvents
{
    /// <summary>
    /// Входящее событие от энкодера
    /// </summary>
    public class AxisEvent : ControlEventBase
    {
        /// <summary>
        /// Текущая позиция бегунка сопротивления
        /// </summary>
        public ushort Position;
        /// <summary>
        /// Минимально возможное значение (зависит от реализации в железе)
        /// </summary>
        public double MinimumValue;
        /// <summary>
        /// Максимально возможное значение (зависит от реализации в железе) 
        /// </summary>
        public double MaximumValue;

    }
}
