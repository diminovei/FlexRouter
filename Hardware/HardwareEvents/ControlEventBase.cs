using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.HardwareEvents
{
    public class ControlEventBase
    {
        /// <summary>
        /// Дамп нужен только тумблерам. Кнопкам +- он не нужен
        /// </summary>
        public bool IsSoftDumpEvent;
        /// <summary>
        /// Описание сработавшего железа
        /// </summary>
        public ControlProcessorHardware Hardware = new ControlProcessorHardware();
    }
}
