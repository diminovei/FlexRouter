namespace FlexRouter.Hardware.Helpers
{
    /// <summary>
    /// Поддерживаемые типы модулей железа
    /// </summary>
    public enum HardwareModuleType
    {
        Unknown = -1,
        Axis,
        Button,
        Encoder,
        BinaryOutput,
        Indicator,
        SteppingMotor,
        LedMatrixIndicator
    }
}
