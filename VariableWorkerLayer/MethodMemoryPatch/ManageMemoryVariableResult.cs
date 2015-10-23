namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Результат установки значения переменной или получения значения переменной в памяти
    /// </summary>
    public struct ManageMemoryVariableResult
    {
        public MemoryPatchVariableErrorCode Code;  // Код ошибки
        public string ErrorMessage;                 // Текст ошибки/исключения
        public double Value;                        // Полученое/установленное значение
    }
}