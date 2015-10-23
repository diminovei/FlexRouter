namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Коды ошибок при установке/чтении переменной в памяти
    /// </summary>
    public enum MemoryPatchVariableErrorCode
    {
        Ok,                     // Операция прошла успешно
        ModuleNotFound,         // Модуль, в котором находится переменная не найден
        OffsetIsOutOfModule,    // Смещение выходит за размеры модуля
        WriteError,             // Не удалось установить переменную
        ReadError,              // Не удалось прочитать переменную
        OpenProcessError,       // Не удалось открыть процесс
        Unknown                 // Неизвестная ошибка
    }
}