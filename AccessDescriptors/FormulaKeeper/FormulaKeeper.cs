using System;
using System.Collections.Generic;
using System.Linq;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;
using SlimDX.Direct3D10;

namespace FlexRouter.AccessDescriptors.FormulaKeeper
{
    /// <summary>
    /// Класс, хранящий формулы в словаре с ключом состоящим из идентификатора состояния и переменной
    /// </summary>
    public class FormulaKeeper
    {
        private readonly Dictionary<Guid, FormulaContainer> _formulaDictionary = new Dictionary<Guid, FormulaContainer>();
        public Dictionary<Guid, FormulaContainer> Export()
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
        private Guid FindVariableFormulaId(Guid ownerId, Guid variableId, int connectorId)
        {
            foreach (var f in _formulaDictionary)
            {
                if (f.Value.VariableId != variableId || f.Value.ConnectorId != connectorId || f.Value.OwnerId != ownerId)
                    continue;
                return f.Key;
            }
            return Guid.Empty;
        }
        public void StoreVariableFormula(string formulaText, Guid ownerId, Guid variableId, int connectorId)
        {
            var formulaId = FindVariableFormulaId(ownerId, variableId, connectorId);
            if(formulaId == Guid.Empty)
                formulaId = GlobalId.GetNew();
            formulaText = ReplaceVariableTextWithId(formulaText);
            _formulaDictionary[formulaId] = (new FormulaContainer(formulaText, ownerId, variableId, connectorId));
        }
        public string GetVariableFormulaText(Guid ownerId, Guid variableId, int connectorId)
        {
            return (from f in _formulaDictionary where f.Value.VariableId == variableId && f.Value.ConnectorId == connectorId && f.Value.OwnerId == ownerId select ReplaceVariableIdWithText(f.Value.Formula)).FirstOrDefault();
        }
        public Guid StoreFormula(string formulaText, Guid ownerId)
        {
            formulaText = ReplaceVariableTextWithId(formulaText);
            var id = GlobalId.GetNew();
            _formulaDictionary.Add(id, new FormulaContainer(formulaText, ownerId));
            return id;
        }
        public Guid StoreOrChangeFormulaText(Guid formulaId, string formulaText, Guid ownerId)
        {
            if (!_formulaDictionary.ContainsKey(formulaId))
                return StoreFormula(formulaText, ownerId);
            formulaText = ReplaceVariableTextWithId(formulaText);
            _formulaDictionary[formulaId].Formula = formulaText;
            return formulaId;
        }
        /// <summary>
        /// Получить текст формулы по идентификатору
        /// </summary>
        /// <param name="formulaId">идентификатор формулы</param>
        /// <returns>текст формулы</returns>
        public string GetFormulaText(Guid formulaId)
        {
            return !_formulaDictionary.ContainsKey(formulaId) ? null : ReplaceVariableIdWithText(_formulaDictionary[formulaId].Formula);
        }
        /// <summary>
        /// Вернуть список дескрипторов и панелей в которых используется переменная по id переменной
        /// </summary>
        /// <param name="variableId">id переменной</param>
        /// <returns></returns>
        public Guid[] GetVariableOwnersByVariableId(Guid variableId)
        {
            var d = new List<Guid>();
            foreach (var fd in _formulaDictionary)
            {
                if (fd.Value.VariableId != variableId && !fd.Value.Formula.Contains("[" + variableId + "]")) 
                    continue;
                if (!d.Contains(fd.Value.OwnerId))
                    d.Add(fd.Value.OwnerId);
            }
            return d.ToArray();
        }
        /// <summary>
        /// Удалить формулу
        /// </summary>
        /// <param name="ownerId">идентификатор дескриптора</param>
        /// <param name="variableId">идентификатор переменной</param>
        /// <param name="connectorId">идентификатор соединения</param>
        public void RemoveFormulaByVariableIdAndOwnerId(Guid ownerId, Guid variableId, int connectorId)
        {
            var formulaId = FindVariableFormulaId(ownerId, variableId, connectorId);
            if (_formulaDictionary.ContainsKey(formulaId))
                _formulaDictionary.Remove(formulaId);
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
        /// <param name="ownerId">идентификатор</param>
        public void RemoveFormulasByOwnerId(Guid ownerId)
        {
            foreach (var s in _formulaDictionary.Where(x => x.Value.OwnerId == ownerId).ToList())
            {
                _formulaDictionary.Remove(s.Key);

            }
        }
        /// <summary>
        /// Заменить переменные в виде "панель.переменная" на идентификаторы переменных
        /// </summary>
        /// <param name="formula">исходная формула</param>
        /// <returns>изменённая формула</returns>
        private static string ReplaceVariableTextWithId(string formula)
        {
            var resultFormula = string.Empty;
            var token = string.Empty;
            var startToken = false;
            foreach (var t in formula)
            {
                if (startToken)
                {
                    if (t == ']')
                    {
                        var varAndPanelName = token.Split('.');
                        if (varAndPanelName.Length == 2)
                        {
                            var varId = Profile.GetVariableByPanelAndName(varAndPanelName[0], varAndPanelName[1]);
                            resultFormula += '[' + (varId == Guid.Empty ? token : varId.ToString()) + ']';
                        }
                        else
                            resultFormula += '[' + token + ']';
                        startToken = false;
                        continue;
                    }
                    token += t;
                    continue;
                }
                if (t == '[')
                {
                    startToken = true;
                    token = string.Empty;
                    continue;
                }
                resultFormula += t;
            }
            return resultFormula;
        }
        /// <summary>
        /// Заменить идентификаторы переменных на переменные в виде "панель.переменная"
        /// </summary>
        /// <param name="formula">исходная формула</param>
        /// <returns>изменённая формула</returns>
        private static string ReplaceVariableIdWithText(string formula)
        {
            var resultFormula = string.Empty;
            var token = string.Empty;
            var startToken = false;
            foreach (var t in formula)
            {
                if (startToken)
                {
                    if (t == ']')
                    {
                        startToken = false;
                        Guid id;
                        if (!Guid.TryParse(token, out id))
                        {
                            resultFormula += '[' + token + ']';
                            continue;
                        }
                        var variable = Profile.VariableStorage.GetVariableById(id);
                        var panelName = Profile.PanelStorage.GetPanelById(variable.PanelId).Name;
                        var name = variable.Name;
                        resultFormula += '[' + panelName + '.' + name + ']';
                        continue;
                    }
                    token += t;
                    continue;
                }
                if (t == '[')
                {
                    startToken = true;
                    token = string.Empty;
                    continue;
                }
                resultFormula += t;
            }
            return resultFormula;
        }
    }
}
