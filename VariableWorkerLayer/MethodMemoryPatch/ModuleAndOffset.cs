namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Возвращаемое значение для преобразования абсолютного смещения в относительное
    /// </summary>
    public struct ModuleAndOffset
    {
        public string ModuleName;   // Имя модуля
        public uint Offset;         // Смещение в модуле
    }
}