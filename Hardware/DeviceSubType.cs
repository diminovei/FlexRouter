namespace FlexRouter.Hardware
{
    /// <summary>
    /// Подтип железа. Используется для получения описания железа (Capacity)
    /// </summary>
    internal enum DeviceSubType
    {
        Motherboard,        // Материнская плата
        ExtensionBoard,     // Число плат расширения (модуль)
        Block,              // Число логических объединений контролов (блок)
        Control,            // Число контролов (кнопка, ось, ...)
    }
}