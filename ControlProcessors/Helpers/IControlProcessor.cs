using System;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.AssignedHardware;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Интерфейс ControlProcessor'ов 
    /// </summary>
    public interface IControlProcessor
    {
        /// <summary>
        /// Возможно ли инвертировать контрол? Нужно для редакторов
        /// </summary>
        /// <returns></returns>
        bool HasInvertMode();
        Guid GetId();
        /// <summary>
        /// Вернуть текстовое имя ControlProcessor
        /// </summary>
        string GetDescription();
        /// <summary>
        /// Нужно только для дампа отдельных модулей Arcc
        /// </summary>
        /// <returns></returns>
        string[] GetUsedHardwareList();
        /// <summary>
        /// Используется для передачи назначений в редактор
        /// </summary>
        /// <returns></returns>
        IAssignment[] GetAssignments();
        /// <summary>
        /// Используется при сохранении данных из редактора в ControlProcessor. Сохранять нужно только назначение.
        /// </summary>
        /// <param name="assignment"></param>
        void SetAssignment(IAssignment assignment);
        /// <summary>
        /// Может ли это ControlProcessor быть назначенным для указанного AccessDescriptor
        /// </summary>
        /// <param name="accessDescriptor">AccessDescriptor</param>
        /// <returns>true - подходит</returns>
        bool IsAccessDesctiptorSuitable(DescriptorBase accessDescriptor);
        /// <summary>
        /// Должен вызываться при изменении состава коннекторов к AccessDescriptor (по-старому, состояний)
        /// </summary>
        void OnAssignmentsChanged();
        void Save(XmlTextWriter writer);
        void Load(XPathNavigator reader);
    }
}
