namespace FlexRouter.Hardware.HardwareEvents
{
    /// <summary>
    /// Исходящее событие установки яркости
    /// </summary>
    class BrightnessEvent : ControlEventBase
    {
        /// <summary>
        /// Яркость, которую нужно установить
        /// </summary>
        public short Position;

    }
}
