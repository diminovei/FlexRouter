using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorValue : DescriptorMultistateBase, IDescriptorMultistateWithDefault, IDefautValueAbility, IRepeaterInDescriptor
    {
        public override string GetDescriptorType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryMultistate);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.Button;
        }

        private int _defaultStateId = -1;
        /// <summary>
        /// Установить состояние по-умолчанию. -1 - нет установленного по-умолчанию состояния
        /// </summary>
        /// <param name="id"></param>
        public void AssignDefaultStateId(int id)
        {
            _defaultStateId = id;
        }
        /// <summary>
        /// Отменить установку состояния по-умолчанию
        /// </summary>
        public void UnAssignDefaultStateId()
        {
            _defaultStateId = -1;
        }
        /// <summary>
        /// Установить значение переменных в соответствие с указанной для этого формулой
        /// </summary>
        /// <param name="stateId"></param>
        public void SetState(int stateId)
        {
            if (StateDescriptors.Count(sd => sd.Id == stateId) == 0)
                return;
            if (!IsPowerOn() || stateId == -1)
                return;
            foreach (var varId in UsedVariables)
            {
                var formula = GlobalFormulaKeeper.Instance.GetVariableFormulaText(GetId(), varId, stateId);
                if (string.IsNullOrEmpty(formula))
                    return;
                var formulaResult = CalculatorE.ComputeFormula(formula);
                if (formulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.DoubleResult)
                    Profile.VariableStorage.WriteValue(varId, formulaResult.CalculatedDoubleValue);
            }
        }
        /// <summary>
        /// Установить значение по-умолчанию
        /// </summary>
        public void SetDefaultState()
        {
            if (_defaultStateId!=-1)
                SetState(_defaultStateId);
        }
        /// <summary>
        /// Получить id значения по-умолчанию
        /// </summary>
        public int GetDefaultStateId()
        {
            return _defaultStateId;
        }
        /// <summary>
        /// Получить все состояния
        /// </summary>
        /// <returns></returns>
        public override Connector[] GetConnectors(object controlProcessor)
        {
            return StateDescriptors.Where(x=>x.Id!=GetDefaultStateId()).ToArray();
        }
        /// <summary>
        /// Сохранить дополнительные параметры
        /// </summary>
        /// <param name="writer">Стрим для записи XML</param>
        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            if (_defaultStateId == -1)
                return;
            writer.WriteStartElement("DefaultState");
            writer.WriteAttributeString("Id", _defaultStateId.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }
        /// <summary>
        /// Загрузить дополнительные параметры
        /// </summary>
        /// <param name="reader">Итератор узла XML</param>
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            var readerAdd = reader.SelectSingleNode("DefaultState");
            if (readerAdd == null)
                return;
            int.TryParse(readerAdd.GetAttribute("Id", readerAdd.NamespaceURI), NumberStyles.Number, CultureInfo.InvariantCulture, out _defaultStateId);
        }
    }
}
