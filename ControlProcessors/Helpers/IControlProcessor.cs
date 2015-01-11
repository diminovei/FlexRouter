using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.Hardware.HardwareEvents;

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
        void RenewStatesInfo(IEnumerable<AccessDescriptorState> states);
    }

    /// <summary>
    /// Интерфейс ControlProcessor'ов 
    /// </summary>
    public interface IControlProcessor
    {
        int GetId();
        /// <summary>
        /// Вернуть текстовое имя ControlProcessor
        /// </summary>
        string GetName();
        /// <summary>
        /// Нужно только для дампа отдельных модулей Arcc
        /// </summary>
        /// <returns></returns>
        string[] GetUsedHardwareList();
        /// <summary>
        /// Используется для передачи назначений в редактор
        /// </summary>
        /// <returns></returns>
        Assignment[] GetAssignments();
        /// <summary>
        /// Используется при сохранении данных из редактора в ControlProcessor. Сохранять нужно только назначение.
        /// </summary>
        /// <param name="assignment"></param>
        void SetAssignment(Assignment assignment);
        /// <summary>
        /// Может ли это ControlProcessor быть назначенным для указанного AccessDescriptor
        /// </summary>
        /// <param name="accessDescriptor">AccessDescriptor</param>
        /// <returns>true - подходит</returns>
        bool IsAccessDesctiptorSuitable(DescriptorBase accessDescriptor);

        void Save(XmlTextWriter writer);
        void Load(XPathNavigator reader);
        //ToDo: temp
        int GetAssignedAccessDescriptor();
        void SetId(int id);
    }
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

    public interface IRepeater
    {
        void Tick();
//        bool IsRepeaterOn();
//        void EnableRepeater(bool on);
    }
}
