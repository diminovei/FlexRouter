using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
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
}
