using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors.Helpers
{
    static class GlobalFormulaKeeper
    {
        public static FormulaKeeper Instance = new FormulaKeeper();
    }
    /// <summary>
    /// Класс, хранящий формулы в словаре с ключом состоящим из идентификатора состояния и переменной
    /// </summary>
    public class FormulaKeeper
    {
        public class FormulaContainer2
        {
            public string Formula;
            public int OwnerId;
            public int VariableId;
            public int StateId;
            public FormulaContainer2(string formula, int ownerId)
            {
                OwnerId = ownerId;
                Formula = formula;
                VariableId = -1;
                StateId = -1;
            }
            public FormulaContainer2(string formula, int ownerId, int variableId, int stateId)
            {
                OwnerId = ownerId;
                Formula = formula;
                VariableId = variableId;
                StateId = stateId;
            }
        }
        
        private readonly Dictionary<int, FormulaContainer2> _formulaDictionaryNew = new Dictionary<int, FormulaContainer2>();

        public Dictionary<int, FormulaContainer2> Export()
        {
            return _formulaDictionaryNew;
        }
        public void Import(FormulaKeeper gfk)
        {
            var exportedData = gfk.Export();
            foreach (var data in exportedData)
            {
                _formulaDictionaryNew[data.Key] = data.Value;
            }
        }
        #region NewMethods

        private int FindVariableFormulaId(int ownerId, int variableId, int stateId)
        {
            foreach (var f in _formulaDictionaryNew)
            {
                if (f.Value.VariableId != variableId || f.Value.StateId != stateId || f.Value.OwnerId != ownerId)
                    continue;
                return f.Key;
            }
            return -1;
        }
        public void StoreVariableFormula(string formulaText, int ownerId, int variableId, int stateId)
        {
            var formulaId = FindVariableFormulaId(ownerId, variableId, stateId);
            if(formulaId == -1)
                formulaId = GlobalId.GetNew();
            formulaText = ReplaceTextWithId(formulaText);
            _formulaDictionaryNew[formulaId] = (new FormulaContainer2(formulaText, ownerId, variableId, stateId));
        }

        public string GetVariableFormulaText(int ownerId, int variableId, int stateId)
        {
            return (from f in _formulaDictionaryNew where f.Value.VariableId == variableId && f.Value.StateId == stateId && f.Value.OwnerId == ownerId select ReplaceIdWithText(f.Value.Formula)).FirstOrDefault();
        }

        public int StoreFormula(string formulaText, int ownerId)
        {
            formulaText = ReplaceTextWithId(formulaText);
            var id = GlobalId.GetNew();
            _formulaDictionaryNew.Add(id, new FormulaContainer2(formulaText, ownerId));
            return id;
        }

        public void ChangeFormulaText(int formulaId, string formulaText)
        {
            if (!_formulaDictionaryNew.ContainsKey(formulaId))
                return;
            formulaText = ReplaceTextWithId(formulaText);
            _formulaDictionaryNew[formulaId].Formula = formulaText;
        }

        public string GetFormulaText(int formulaId)
        {
            return !_formulaDictionaryNew.ContainsKey(formulaId) ? null : ReplaceIdWithText(_formulaDictionaryNew[formulaId].Formula);
        }
        #endregion
        /// <summary>
        /// Вернуть список дескрипторов и панелей в которых используется переменная по id переменной
        /// </summary>
        /// <param name="variableId">id переменной</param>
        /// <returns></returns>
        public int[] GetVariableOwnersByVariableId(int variableId)
        {
            var d = new List<int>();
            foreach (var fd in _formulaDictionaryNew)
            {
                if (fd.Value.VariableId == variableId || fd.Value.Formula.Contains("[" + variableId + "]"))
                {
                    if (!d.Contains(fd.Value.OwnerId))
                        d.Add(fd.Value.OwnerId);
                }
            }
            return d.ToArray();
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="ownerId">идентификатор дескриптора</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="stateId">идентификатор состояния</param>
        public void RemoveVariableFormula(int ownerId, int variableId, int stateId)
        {
            var formulaId = FindVariableFormulaId(ownerId, variableId, stateId);
            if (_formulaDictionaryNew.ContainsKey(formulaId))
                _formulaDictionaryNew.Remove(formulaId);
        }

        /// <summary>
        /// Удалить все формулы
        /// </summary>
        public void ClearAll()
        {
            _formulaDictionaryNew.Clear();
        }
        /// <summary>
        /// Удалить все формулы для указанного Item
        /// </summary>
        /// <param name="itemId">идентификатор</param>
        public void RemoveFormulasByOwnerId(int itemId)
        {
            for (var i = _formulaDictionaryNew.Count-1; i >= 0; i--)
            {
                if (_formulaDictionaryNew.ElementAt(i).Value.OwnerId == itemId)
                    _formulaDictionaryNew.Remove(_formulaDictionaryNew.Keys.ElementAt(i));
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
