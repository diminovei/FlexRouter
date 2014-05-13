using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public enum FormulaKeeperItemType
    {
        AccessDescriptor,
        Panel
    }
    public enum FormulaKeeperFormulaType
    {
        Power,
        GetValue,
        SetValue
    }

    static class GlobalFormulaKeeper
    {
        public static FormulaKeeper Instance = new FormulaKeeper();
    }
    /// <summary>
    /// Класс, хранящий формулы в словаре с ключом состоящим из идентификатора состояния и переменной
    /// </summary>
    public class FormulaKeeper
    {
        public class FormulaContainer
        {
            public FormulaKeeperItemType ItemType;
            public int ItemId;
            public int VariableId;
            public string Formula;

/*            public FormulaContainer(FormulaKeeperItemType itemType, int itemId, string formula)
            {
                ItemType = itemType;
                ItemId = itemId;
                Formula = formula;
                VariableId = -1

            }*/
            public FormulaContainer(FormulaKeeperItemType itemType, int itemId, int variableId, string formula)
            {
                ItemType = itemType;
                ItemId = itemId;
                Formula = formula;
                VariableId = variableId;
            }

        }
        private readonly Dictionary<string, FormulaContainer> _formulaDictionary = new Dictionary<string, FormulaContainer>();
        
//        private readonly Dictionary<string, string> _formulaDictionaryNew = new Dictionary<string, string>();

        public Dictionary<string, FormulaContainer> Export()
        {
            return _formulaDictionary;
        }
        public void Import(FormulaKeeper gfk)
        {
            var exportedData = gfk.Export();
            foreach (var data in exportedData)
            {
                _formulaDictionary[data.Key] = data.Value;
            }
        }

/*        #region New
        /// <summary>
        /// Получить формулу для панели
        /// </summary>
        /// <param name="formulaId">тип формулы</param>
        /// <param name="ownerId">идентификатор</param>
        /// <returns>формула</returns>
        public string GetFormula(int formulaId, int ownerId)
        {

            var formulaKey = GetFormulaKey(formulaId, ownerId);
            return GetFormula(formulaKey);
        }
        private string GetFormula(string formulaKey)
        {
            var formula = !_formulaDictionaryNew.ContainsKey(formulaKey) ? string.Empty : _formulaDictionaryNew[formulaKey];
            formula = ReplaceIdWithText(formula);
            return formula;
        }

        private int AddFormula(string formula, int ownerId)
        {
            var formulaId = GlobalId.GetNew();
            SetFormula(formula, ownerId, formulaId);
            return formulaId;
        }


        private static string GetFormulaKey(int formulaId, int ownerId)
        {
            return formulaId + "|" + ownerId;
        }
        private void SetFormula(string formula, int ownerId, int formulaId)
        {
            var formulaKey = GetFormulaKey(formulaId, ownerId);
            if (string.IsNullOrEmpty(formula))
            {
                Remove(formulaKey);
                return;
            }

            formula = ReplaceTextWithId(formula);
            if (_formulaDictionaryNew.ContainsKey(formulaKey))
                _formulaDictionaryNew[formulaKey] = formula;
            else
                _formulaDictionaryNew.Add(formulaKey, formula);
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="itemId">идентификатор дескриптора</param>
        private void Remove(string formulaKey)
        {
            if (_formulaDictionaryNew.ContainsKey(formulaKey))
                _formulaDictionaryNew.Remove(formulaKey);
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="itemId">идентификатор дескриптора</param>
        public void Remove(int ownerId, int formulaId)
        {
            var formulaKey = GetFormulaKey(formulaId, ownerId);
            if (_formulaDictionaryNew.ContainsKey(formulaKey))
                _formulaDictionaryNew.Remove(formulaKey);
        }
        #endregion
*/ 
        /// <summary>
        /// Получить формулу для панели
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="itemId">идентификатор</param>
        /// <returns>формула</returns>
        public string GetFormula(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int itemId)
        {
            var formulaKey = itemType.ToString() + formulaType + itemId;
            return GetFormula(formulaKey);
        }
        /// <summary>
        /// Получить формулу по идентификаторам состояния и переменной
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="descriptorId">идентификатор дескриптора</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="stateId">идентификатор состояния</param>
        /// <returns>Токенизированная формула</returns>
        public string GetFormula(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int descriptorId, int variableId, int stateId)
        {
            var formulaKey = itemType.ToString() + formulaType + descriptorId + variableId + stateId;
            return GetFormula(formulaKey);
        }

        private string GetFormula(string formulaKey)
        {
            var formula = !_formulaDictionary.ContainsKey(formulaKey) ? string.Empty : _formulaDictionary[formulaKey].Formula;
            formula = ReplaceIdWithText(formula);
            return formula;
        }

        public Dictionary<int, FormulaKeeperItemType> GetVariableLinks(int variableId)
        {
            var d = new Dictionary<int, FormulaKeeperItemType>();
            foreach (var fd in _formulaDictionary)
            {
                if (fd.Value.VariableId == variableId || fd.Value.Formula.Contains("[" + variableId + "]"))
                {
                    if(!d.ContainsKey(fd.Value.ItemId))
                        d.Add(fd.Value.ItemId, fd.Value.ItemType);
                }
            }
            return d;
        }
        private void SetFormula(string formulaKey, string formula, FormulaKeeperItemType itemType, int itemId, int variableId)
        {
/*            if (string.IsNullOrEmpty(formula))
            {
                Remove(formulaKey);
                return;
            }*/
                
            formula = ReplaceTextWithId(formula);
            if (_formulaDictionary.ContainsKey(formulaKey))
                _formulaDictionary[formulaKey].Formula = formula;
            else
                _formulaDictionary.Add(formulaKey, new FormulaContainer(itemType, itemId, variableId, formula));
        }
        /// <summary>
        /// Запомнить формулу для пары состояние и переменная
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="descriptorId">идентификатор дескриптора</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="stateId">идентификатор состояния</param>
        /// <param name="formula">формула</param>
        public void SetFormula(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int descriptorId, int variableId, int stateId, string formula)
        {
            var formulaKey = itemType.ToString() + formulaType + descriptorId + variableId + stateId;
            SetFormula(formulaKey, formula, itemType, descriptorId, variableId);
        }
        /// <summary>
        /// Запомнить формулу для пары состояние и переменная
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="itemId">идентификатор</param>
        /// <param name="formula">формула</param>
        public void SetFormula(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int itemId, string formula)
        {
            var formulaKey = itemType.ToString() + formulaType + itemId;
            SetFormula(formulaKey, formula, itemType, itemId, -1);
        }

        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="descriptorId">идентификатор дескриптора</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="stateId">идентификатор состояния</param>
        public void Remove(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int descriptorId, int variableId, int stateId)
        {
            var formulaKey = itemType.ToString() + formulaType + descriptorId + variableId + stateId;
            if (_formulaDictionary.ContainsKey(formulaKey))
                _formulaDictionary.Remove(formulaKey);
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="itemType">тип источника формулы</param>
        /// <param name="formulaType">тип формулы</param>
        /// <param name="itemId">идентификатор дескриптора</param>
        public void Remove(FormulaKeeperItemType itemType, FormulaKeeperFormulaType formulaType, int itemId)
        {
            var formulaKey = itemType.ToString() + formulaType + itemId;
            if (_formulaDictionary.ContainsKey(formulaKey))
                _formulaDictionary.Remove(formulaKey);
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="formulaKey">ключ формулы</param>
        private void Remove(string formulaKey)
        {
            if (_formulaDictionary.ContainsKey(formulaKey))
                _formulaDictionary.Remove(formulaKey);
        }
        /// <summary>
        /// Удалить все формулы
        /// </summary>
        public void ClearAll()
        {
            _formulaDictionary.Clear();
        }
        /// <summary>
        /// Удалить все формулы для указанного Item
        /// </summary>
        /// <param name="itemId">идентификатор</param>
        public void Clear(int itemId)
        {
            for (var i = _formulaDictionary.Count-1; i >= 0; i--)
            {
                if (_formulaDictionary.ElementAt(i).Value.ItemId == itemId)
                    _formulaDictionary.Remove(_formulaDictionary.Keys.ElementAt(i));
            }
        }

        private string ReplaceTextWithId(string formula)
        {
            var resultFormula = string.Empty;
            var token = string.Empty;
            var startToken = false;
            for (var i = 0; i < formula.Length; i++)
            {
                if (startToken)
                {
                    if (formula[i] == ']')
                    {
                        var varAndPanelName = token.Split('.');
                        if (varAndPanelName.Length == 2)
                        {
                            var varId = Profile.GetVariableByPanelAndName(varAndPanelName[0], varAndPanelName[1]);
                            resultFormula += '[' + (varId == -1 ? token : varId.ToString(CultureInfo.InvariantCulture)) + ']';
                        }
                        else
                            resultFormula += '[' + token + ']';
                        startToken = false;
                        continue;
                    }
                    token += formula[i];
                    continue;
                }
                if (formula[i] == '[')
                {
                    startToken = true;
                    token = string.Empty;
                    continue;
                }
                resultFormula += formula[i];
            }
            return resultFormula;
        }
        private string ReplaceIdWithText(string formula)
        {
            var resultFormula = string.Empty;
            var token = string.Empty;
            var startToken = false;
            for (var i = 0; i < formula.Length; i++)
            {
                if (startToken)
                {
                    if (formula[i] == ']')
                    {
                        startToken = false;
                        int id;
                        if (!int.TryParse(token, out id))
                        {
                            resultFormula += '[' + token + ']';
                            continue;
                        }
                        var variable = Profile.GetVariableById(id);
                        var panelName = Profile.GetPanelById(variable.PanelId).Name;
                        var name = variable.Name;
                        resultFormula += '[' + panelName + '.' + name + ']';
                        continue;
                    }
                    token += formula[i];
                    continue;
                }
                if (formula[i] == '[')
                {
                    startToken = true;
                    token = string.Empty;
                    continue;
                }
                resultFormula += formula[i];
            }
            return resultFormula;
        }
    }
}
