using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Интерфейс ControlProcessor'ов 
    /// </summary>
    public interface IVisualizer
    {
        /// <summary>
        /// Получить от роутера новое событие для визуализатора (индикатора, лампы, ...)
        /// </summary>
        /// <returns></returns>
        IEnumerable<ControlEventBase> GetNewEvent();
        /// <summary>
        /// Получить от роутера событие "очистки" для визуализатора (индиктор, лампа, ...). Нужно гасить визуализаторы при выключении роутера
        /// </summary>
        /// <returns></returns>
        IEnumerable<ControlEventBase> GetClearEvent();
    }
}