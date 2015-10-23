using System.Collections.Generic;
using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Интерфейс для ControlProcessor с многими состояниями
    /// </summary>
    public interface IControlProcessorMultistate
    {
        /// <summary>
        /// Список состояний изменился, нужно удалить назначения для несуществующих состояний, добавить новые и обновить существующие
        /// </summary>
        /// <param name="states"></param>
        void RenewStatesInfo(IEnumerable<Connector> states);
    }
}