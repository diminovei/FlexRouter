namespace FlexRouter.Hardware.HardwareEvents
{
    /// <summary>
    /// Входящее событие от энкодера
    /// </summary>
    public class EncoderEvent : ControlEventBase
    {
        /// <summary>
        /// Направление вращения энкодера
        /// </summary>
        public bool RotateDirection;
        /// <summary>
        /// Сколько кликов сделано в этом  направлении
        /// </summary>
        public int ClicksCount;
    }
}
