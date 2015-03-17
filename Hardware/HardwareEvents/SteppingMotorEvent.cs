namespace FlexRouter.Hardware.HardwareEvents
{
    /// <summary>
    /// Исходящее событие шаговому двигателю
    /// </summary>
    class SteppingMotorEvent : ControlEventBase
    {
        /// <summary>
        /// Позиция, в которую нужно установить шаговый двигатель
        /// </summary>
        public short Position;
    }
}
