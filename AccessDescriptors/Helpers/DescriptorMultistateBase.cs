using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.FormulaKeeper;
using FlexRouter.Helpers;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorMultistateBase : DescriptorBase
    {
        protected List<Connector> StateDescriptors = new List<Connector>();
        protected List<Guid> UsedVariables = new List<Guid>();
        /// <summary>
        /// Перезаписать состояния после изменений в редакторе
        /// </summary>
        /// <param name="states"></param>
        public void OverwriteStates(List<Connector> states)
        {
            StateDescriptors = states;
            //ToDo: renew states in control Processor
        }
        /// <summary>
        /// Используется при изменении дескриптора из редактора
        /// </summary>
        /// <param name="formulaKeeper"></param>
        public void OverwriteFormulaKeeper(FormulaKeeper.FormulaKeeper formulaKeeper)
        {
            GlobalFormulaKeeper.Instance.Import(formulaKeeper);
        }
        public void OverwriteUsedVariables(List<Guid> variables)
        {
            UsedVariables = variables;
        }
        /// <summary>
        /// Получить все используемые переменные
        /// </summary>
        /// <returns></returns>
        public Guid[] GetAllUsedVariables()
        {
            return UsedVariables.ToArray();
        }

        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            StateDescriptors.Clear();
            _repeaterIsOn = false;
            bool.TryParse(reader.GetAttribute("RepeaterIsOn", reader.NamespaceURI), out _repeaterIsOn);
            var readerAdd = reader.Select("States/State");
            while (readerAdd.MoveNext())
            {
                var ads = new Connector
                {
                    Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI)),
                    Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI),
                    Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI))
                };
                StateDescriptors.Add(ads);
            }
            UsedVariables.Clear();
            readerAdd = reader.Select("UsedVariables/Variable");
            while (readerAdd.MoveNext())
            {
                var id = Guid.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI));
                UsedVariables.Add(id);
            }
            readerAdd = reader.Select("FormulaList/Formula");
            while (readerAdd.MoveNext())
            {
                var stateId = int.Parse(readerAdd.Current.GetAttribute("StateId", readerAdd.Current.NamespaceURI));
                var variableId = Guid.Parse(readerAdd.Current.GetAttribute("VariableId", readerAdd.Current.NamespaceURI));
                var formula = readerAdd.Current.GetAttribute("Formula", readerAdd.Current.NamespaceURI);
                SetFormula(stateId, variableId, formula);
            }
        }

        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            if (_repeaterIsOn)
                writer.WriteAttributeString("RepeaterIsOn", _repeaterIsOn.ToString());
            writer.WriteString("\n");
            writer.WriteStartElement("States");
            writer.WriteString("\n");
            foreach (var s in StateDescriptors)
            {
                writer.WriteStartElement("State");
                writer.WriteAttributeString("Id", s.Id.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Name", s.Name);
                writer.WriteAttributeString("Order", s.Order.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteString("\n");

            writer.WriteStartElement("UsedVariables");
            foreach (var v in UsedVariables)
            {
                writer.WriteStartElement("Variable");
                writer.WriteAttributeString("Id", v.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteString("\n");


            writer.WriteStartElement("FormulaList");
            foreach (var s in StateDescriptors)
            {
                foreach (var v in UsedVariables)
                {
                    var formula = GetFormula(v, s.Id);
                    if (formula == null) 
                        continue;
                    writer.WriteStartElement("Formula");
                    writer.WriteAttributeString("StateId", s.Id.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("VariableId", v.ToString());
                    writer.WriteAttributeString("Formula", formula);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
            writer.WriteString("\n");
        }

        /// <summary>
        /// Добавить состояние
        /// </summary>
        /// <param name="name">Имя состояния</param>
        public Connector AddConnector(string name)
        {
            if (StateDescriptors.Any(s => s.Name == name))
                return null;
            var nextOrder = StateDescriptors.Count == 0 ? 0 : StateDescriptors.Select(i => i.Order).OrderBy(i => i).Max() + 1;
            var nextKey = StateDescriptors.Count == 0 ? 0 : StateDescriptors.Select(i => i.Id).OrderBy(i => i).Max() + 1;
            var accessDescriptorState = new Connector
            {
                Id = nextKey,
                Name = name,
                Order = nextOrder
            };

            StateDescriptors.Add(accessDescriptorState);

            return accessDescriptorState;
        }
        /// <summary>
        /// Удалить состояние
        /// </summary>
        /// <param name="id">Идентификатор состояния</param>
        public bool RemoveConnector(int id)
        {
            var s = StateDescriptors.First(i => i.Id == id);
            if (s == null)
                return false;
            StateDescriptors.Remove(s);
            return true;
        }
        /// <summary>
        /// Добавить переменную для использования в описателе
        /// </summary>
        /// <param name="id">Идентификатор переменной</param>
        public void AddVariable(Guid id)
        {
            if (UsedVariables.Contains(id))
                return;
            UsedVariables.Add(id);
        }
        /// <summary>
        /// Удалить из использования в описателе переменную
        /// </summary>
        /// <param name="id">Идентификатор переменной</param>
        public void RemoveVariable(Guid id)
        {
            if (!UsedVariables.Contains(id))
                return;
            UsedVariables.Remove(id);
            foreach (var s in StateDescriptors)
                GlobalFormulaKeeper.Instance.RemoveFormulaByVariableIdAndOwnerId(GetId(), id, s.Id);
        }
        /// <summary>
        /// Установить формулу для переменной в определённом состоянии
        /// </summary>
        /// <param name="connectorId">идентификатор состояния</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="variableFormula">формула</param>
        public bool SetFormula(int connectorId, Guid variableId, string variableFormula)
        {
            if (!StateDescriptors.Select(i => i.Id).Contains(connectorId) || !UsedVariables.Contains(variableId))
                return false;
            GlobalFormulaKeeper.Instance.StoreVariableFormula(variableFormula, GetId(), variableId, connectorId);
            return true;
        }
        public string GetFormula(Guid variableId, int connectorId)
        {
            return GlobalFormulaKeeper.Instance.GetVariableFormulaText(GetId(), variableId, connectorId);
        }

        private bool _repeaterIsOn;

        public bool IsRepeaterOn()
        {
            return _repeaterIsOn;
        }
        public void EnableRepeater(bool enable)
        {
            _repeaterIsOn = enable;
        }
    }
}
