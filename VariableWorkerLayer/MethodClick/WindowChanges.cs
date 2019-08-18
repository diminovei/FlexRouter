using System;

namespace FlexRouter.VariableWorkerLayer.MethodClick
{
    /// <summary>
    /// Какие изменения произошли с окном (Битовое поле)
    /// </summary>
    [Flags]
    public enum WindowChanges : byte
    {
        NoChanges = 0,
        Append = 1,
        Deleted = 2,
        Hide = 4,
        Unhide = 8,
        Size = 16,
        Locked = 32,
        Unlocked = 64
    };
}