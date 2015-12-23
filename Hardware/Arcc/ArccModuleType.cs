namespace FlexRouter.Hardware.Arcc
{
    /// <summary>
    /// Типы модулей и варианты их использования
    /// Внимание! При использовании значений как int и как enum!
    /// </summary>
    public enum ArccModuleType
    {
        Axis = 1,
        Button = 2,
        Encoder = 3,
        LinearOutput = 4,
        Indicator = 5,
        BinaryInput = 8,
    }
}