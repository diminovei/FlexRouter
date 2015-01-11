using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorMultistateBase : DescriptorBase
    {
        protected List<AccessDescriptorState> StateDescriptors = new List<AccessDescriptorState>();
        protected List<int> UsedVariables = new List<int>();
        /// <summary>
        /// Перезаписать состояния после изменений в редакторе
        /// </summary>
        /// <param name="states"></param>
        public void OverwriteStates(List<AccessDescriptorState> states)
        {
            StateDescriptors = states;
            //ToDo: renew states in control Processor
        }
        /// <summary>
        /// Используется при изменении дескриптора из редактора
        /// </summary>
        /// <param name="formulaKeeper"></param>
        public void OverwriteFormulaKeeper(FormulaKeeper formulaKeeper)
        {
            //ToDo: если оставить здесь, то не будет сохраняться формула питания
//            GlobalFormulaKeeper.Instance.RemoveFormulasByOwnerId(GetId());
            GlobalFormulaKeeper.Instance.Import(formulaKeeper);
        }
        public void OverwriteUsedVariables(List<int> variables)
        {
            UsedVariables = variables;
        }
        /// <summary>
        /// Получить все используемые переменные
        /// </summary>
        /// <returns></returns>
        public int[] GetAllUsedVariables()
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
                var ads = new AccessDescriptorState();
                ads.Id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI));
                ads.Name = readerAdd.Current.GetAttribute("Name", readerAdd.Current.NamespaceURI);
                ads.Order = int.Parse(readerAdd.Current.GetAttribute("Order", readerAdd.Current.NamespaceURI));
                StateDescriptors.Add(ads);
            }
            UsedVariables.Clear();
            readerAdd = reader.Select("UsedVariables/Variable");
            while (readerAdd.MoveNext())
            {
                var id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI));
                UsedVariables.Add(id);
            }
            readerAdd = reader.Select("FormulaList/Formula");
            while (readerAdd.MoveNext())
            {
                var stateId = int.Parse(readerAdd.Current.GetAttribute("StateId", readerAdd.Current.NamespaceURI));
                var variableId = int.Parse(readerAdd.Current.GetAttribute("VariableId", readerAdd.Current.NamespaceURI));
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
                writer.WriteAttributeString("Id", v.ToString(CultureInfo.InvariantCulture));
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
                    writer.WriteAttributeString("VariableId", v.ToString(CultureInfo.InvariantCulture));
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
        public AccessDescriptorState AddState(string name)
        {
            if (StateDescriptors.Any(s => s.Name == name))
                return null;
            var nextOrder = StateDescriptors.Count == 0 ? 0 : StateDescriptors.Select(i => i.Order).OrderBy(i => i).Max() + 1;
            var nextKey = StateDescriptors.Count == 0 ? 0 : StateDescriptors.Select(i => i.Id).OrderBy(i => i).Max() + 1;
            var accessDescriptorState = new AccessDescriptorState
            {
                Id = nextKey,
                Name = name,
                Order = nextOrder
            };

            StateDescriptors.Add(accessDescriptorState);

            if (Profile.GetControlProcessorByAccessDescriptorId(GetId()) != null)
            {
                // ToDo: Обновить данные в ControlProcessor
                //          var cp = Profile.GetControlProcessorByAccessDescriptorId(_assignedControlProcessor);
                //            cp.RenewStatesInfo(_states.Values.ToArray());
            }
            return accessDescriptorState;
        }
        /// <summary>
        /// Удалить состояние
        /// </summary>
        /// <param name="id">Идентификатор состояния</param>
        public bool RemoveState(int id)
        {
            var s = StateDescriptors.First(i => i.Id == id);
            if (s == null)
                return false;
            StateDescriptors.Remove(s);
            // ToDo перебрать все Order и выстроить их в ряд, чтобы не было пропусков
            return true;
        }
        /// <summary>
        /// Добавить переменную для использования в описателе
        /// </summary>
        /// <param name="id">Идентификатор переменной</param>
        public void AddVariable(int id)
        {
            if (UsedVariables.Contains(id))
                return;
            UsedVariables.Add(id);
        }
        /// <summary>
        /// Удалить из использования в описателе переменную
        /// </summary>
        /// <param name="id">Идентификатор переменной</param>
        public void RemoveVariable(int id)
        {
            if (!UsedVariables.Contains(id))
                return;
            UsedVariables.Remove(id);
            foreach (var s in StateDescriptors)
                GlobalFormulaKeeper.Instance.RemoveVariableFormula(GetId(), id, s.Id);
        }
        /// <summary>
        /// Получить все состояния
        /// </summary>
        /// <returns></returns>
        public AccessDescriptorState[] GetStateDescriptors()
        {
            return StateDescriptors.ToArray();
        }
        /// <summary>
        /// Установить формулу для переменной в определённом состоянии
        /// </summary>
        /// <param name="stateId">идентификатор состояния</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="variableFormula">формула</param>
        public bool SetFormula(int stateId, int variableId, string variableFormula)
        {
            if (!StateDescriptors.Select(i => i.Id).Contains(stateId) || !UsedVariables.Contains(variableId))
                return false;
            GlobalFormulaKeeper.Instance.StoreVariableFormula(variableFormula, GetId(), variableId, stateId);
            return true;
        }
        public string GetFormula(int variableId, int stateId)
        {
            return GlobalFormulaKeeper.Instance.GetVariableFormulaText(GetId(), variableId, stateId);
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
