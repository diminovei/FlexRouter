using System;
using System.Xml;
using System.Xml.XPath;

namespace FlexRouter.VariableWorkerLayer
{
    public interface IVariable
    {
        Guid Id { get; set; }
        Guid PanelId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        void Save(XmlTextWriter writer);
        void Load(XPathNavigator reader);
        /// <summary>
        /// Получить тип переменной
        /// </summary>
        /// <returns></returns>
        string GetName();
        bool IsEqualTo(object obj);
    }
}
