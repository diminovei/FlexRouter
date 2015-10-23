using System;

namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Информация о модуле симулятора для метода MemoryPatch
    /// </summary>
    internal struct ModuleInfo
    {
        public string Name;         // Имя модуля (gau)
        public IntPtr BaseAddress;  // Адрес, куда загружен модуль
        public uint Size;            // Размер модуля в памяти
    }
}