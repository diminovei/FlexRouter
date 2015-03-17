using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Интерфейс ControlProcessor'ов 
    /// </summary>
    public interface ICollector
    {
        /// <summary>
        /// Обработать событие, поступившее от железа
        /// </summary>
        /// <param name="controlEvent"></param>
        void ProcessControlEvent(ControlEventBase controlEvent);
    }
}