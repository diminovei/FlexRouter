using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;

namespace FlexRouter.VariableWorkerLayer
{
    public class VariableManager
    {
        private readonly Dictionary<int, IVariable> _variables = new Dictionary<int, IVariable>();
        private readonly MemoryPatchMethod _memoryPatchMethodInstance = new MemoryPatchMethod();
        private readonly FsuipcMethod _fsuipcMethodInstance = new FsuipcMethod();
        private readonly List<string> _notFoundModules = new List<string>();
        private bool _resistVariableChangesFromOutsideModeOn;
        private InitializationState _memoryPatchState;
        private InitializationState _fsuipcState;

        private void InitializeMemoryPatchMethod()
        {
            if (_memoryPatchState != null)
                Problems.AddOrUpdateProblem(_memoryPatchState.System, _memoryPatchState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, true);
            _memoryPatchState = _memoryPatchMethodInstance.Initialize(Profile.GetMainManagedProcessName());
            Problems.AddOrUpdateProblem(_memoryPatchState.System, _memoryPatchState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, _memoryPatchState.IsOk);
        }

        private void InitializeFsuipcMethod()
        {
            if (_fsuipcState != null)
                Problems.AddOrUpdateProblem(_fsuipcState.System, _fsuipcState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, true);
            _fsuipcState = _fsuipcMethodInstance.Initialize();
            Problems.AddOrUpdateProblem(_fsuipcState.System, _fsuipcState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, _fsuipcState.IsOk);
        }

        private void Initialize()
        {
            InitializeMemoryPatchMethod();
            InitializeFsuipcMethod();
        }
        /// <summary>
        /// Сопротивляться ли изменению переменных извне
        /// </summary>
        /// <param name="isOn">true - сопротивляться</param>
        public void SetResistVariableChangesFromOutsideMode(bool isOn)
        {
            _resistVariableChangesFromOutsideModeOn = isOn;
        }
        /// <summary>
        /// Сопротивляться ли изменению переменных извне
        /// </summary>
        public bool IsResistVariableChangesFromOutsideModeOn()
        {
            return _resistVariableChangesFromOutsideModeOn;
        }
        public string[] GetModulesList()
        {
            return _memoryPatchMethodInstance.GetModulesList();
        }
        public ModuleAndOffset? ConvertAbsoleteOffsetToRelative(uint absoluteOffset)
        {
            return _memoryPatchMethodInstance.ConvertAbsoleteToModuleOffset(absoluteOffset);
        }
        /// <summary>
        /// Синхронизировать переменные (значения, предназначенные для записи записать, значения переменных прочитать)
        /// </summary>
        public void SynchronizeVariables()
        {
            _notFoundModules.Clear();
            _fsuipcMethodInstance.Prepare();
            foreach (var variable in _variables.Values)
            {
                if (variable is FakeVariable)
                    SynchronizeFakeVariable(variable as FakeVariable);
                if (variable is MemoryPatchVariable)
                    SynchronizeMemoryPatchVariable(variable as MemoryPatchVariable);
                if (variable is FsuipcVariable)
                    SynchronizeFsuipcVariable(variable as FsuipcVariable);
            }
            _fsuipcMethodInstance.Process();
            foreach (var variable in _variables.Values.OfType<FsuipcVariable>())
            {
                variable.SetValueInMemory(_fsuipcMethodInstance.GetValue(variable.Id));
                //if (!_resistVariableChangesFromOutsideModeOn && variable.GetValueToSet() == variable.GetPrevValueToSet())
                //{
                //    variable.SetValueToSet(variable.GetValueInMemory());
                //    variable.SetPrevValueToSet(variable.GetValueInMemory());
                //}
            }
                
            var modules = string.Empty;
            foreach (var nfm in _notFoundModules)
            {
                if (modules.Length != 0)
                    modules += ", ";
                modules += nfm;
            }

            Problems.AddOrUpdateProblem("MemoryPatcherModules", "not found - " + modules, ProblemHideOnFixOptions.HideItemAndDescription, modules.Length == 0);
        }
        /// <summary>
        /// Получить переменную по её идентификатору
        /// </summary>
        /// <param name="id">Иднтификатор переменной</param>
        /// <returns></returns>
        public IVariable GetVariableById(int id)
        {
            return !_variables.ContainsKey(id) ? null : _variables[id];
        }
        /// <summary>
        /// Получить все переменные
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IVariable> GetAllVariables()
        {
            return _variables.Select(v => v.Value);    
        }
        /// <summary>
        /// Зарегистрировать переменную
        /// </summary>
        /// <param name="variable">Переменная</param>
        public void StoreVariable(IVariable variable)
        {
            var id = variable.Id;
            _variables[id] = variable;
        }
        /// <summary>
        /// Удалить переменную
        /// </summary>
        /// <param name="variableId">Идентификатор переменной</param>
        public void RemoveVariable(int variableId)
        {
            if (!_variables.ContainsKey(variableId))
                return;
            _variables.Remove(variableId);
        }
        public void SaveAllVariables(XmlTextWriter writer)
        {
            foreach (var v in _variables)
            {
                writer.WriteStartElement("Variable");
                v.Value.Save(writer);
                writer.WriteEndElement();
                writer.WriteString("\n");
            }
        }
        /// <summary>
        /// Удалить все переменные
        /// </summary>
        public void Clear()
        {
            _variables.Clear();
        }
        /// <summary>
        /// Передать значение, которое будет записано в переменную при следующей синхронизации
        /// </summary>
        /// <param name="varId">Идентификатор переменной</param>
        /// <param name="value">значение</param>
        /// <returns>Результат</returns>
        public ProcessVariableError WriteValue(int varId, double value)
        {
            if (!_variables.ContainsKey(varId))
                return ProcessVariableError.IdIsNotExist;
            var variable = (MemoryVariableBase) _variables[varId];
            variable.SetValueToSet(value);
            if(variable is FakeVariable)
                variable.SetValueInMemory(value);
            return ProcessVariableError.Ok;
        }
        /// <summary>
        /// Прочитать значение, подготовленное к записи в переменную
        /// </summary>
        /// <param name="varId">Идентификатор переменной</param>
        /// <returns></returns>
        public ReadVariableResult ReadCachedValue(int varId)
        {
            return ReadValue(varId, true);
        }
        /// <summary>
        /// Прочитать значение, которое было у переменной во время предыдущей синхронизации
        /// </summary>
        /// <param name="varId"></param>
        /// <returns></returns>
        public ReadVariableResult ReadValue(int varId)
        {
            return ReadValue(varId, false);
        }
        private ReadVariableResult ReadValue(int varId, bool getCachedValue)
        {
            var result = new ReadVariableResult();
            if (!_variables.ContainsKey(varId))
            {
                result.Error = ProcessVariableError.IdIsNotExist;
                return result;
            }
            result.Error = ProcessVariableError.Ok;
            var valueToSet = ((MemoryVariableBase)_variables[varId]).GetValueToSet();
            var valueInMemory = ((MemoryVariableBase)_variables[varId]).GetValueInMemory();
                
            if (valueToSet == null)
                valueToSet = valueInMemory;

            if (getCachedValue)
            {
                if (valueToSet != null)
                    result.Value = (double) valueToSet;
                else
                    result.Error = ProcessVariableError.NotInitialized;
            }
            else
            {
                if (valueInMemory != null)
                    result.Value = (double) valueInMemory;
                else
                    result.Error = ProcessVariableError.NotInitialized;
            }
            return result;
        }
        private void SynchronizeFakeVariable(FakeVariable fakeVariable)
        {
            fakeVariable.SetValueInMemory(fakeVariable.GetValueToSet());
        }
        private void SynchronizeMemoryPatchVariable(MemoryPatchVariable mpv)
        {
            // Если переменная не инициализирована или нет данных для записи, переменную нужно прочитать
            if (mpv.GetValueInMemory() == null || mpv.GetValueToSet() == null)
            {
                var readVarResult = _memoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize());
                if (readVarResult.Code == MemoryPatchVariableErrorCode.ModuleNotFound)
                {
                    if (!_notFoundModules.Contains(mpv.ModuleName))
                        _notFoundModules.Add(mpv.ModuleName);
                    //mpv.SetValueToSet(null);
                    mpv.SetValueInMemory(null);
                    Initialize();
                }
                else
                    mpv.SetValueInMemory(readVarResult.Value);
            }
            else
            {
                var valueToSet = mpv.GetValueToSet();
                if (valueToSet == null) 
                    return;
                var writeVarResult = _memoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize(), (double)valueToSet);
                if (writeVarResult.Code == MemoryPatchVariableErrorCode.ModuleNotFound)
                {
                    if (!_notFoundModules.Contains(mpv.ModuleName))
                        _notFoundModules.Add(mpv.ModuleName);
                    Initialize();
                }
                else
                {
                    mpv.SetValueInMemory(writeVarResult.Value);
                    mpv.SetValueToSet(null);
                }
            }
        }
        private void SynchronizeFsuipcVariable(FsuipcVariable mpv)
        {
            if (mpv.GetValueToSet() != null)
            {
                if (
                        (_resistVariableChangesFromOutsideModeOn && mpv.GetValueToSet() != mpv.GetValueInMemory())
                        || (!_resistVariableChangesFromOutsideModeOn && mpv.GetValueToSet() != mpv.GetPrevValueToSet())
                    )
                {
                    if (_fsuipcMethodInstance.AddVariableToWrite(mpv))
                        mpv.SetPrevValueToSet(mpv.GetValueToSet());
                    else
                        InitializeFsuipcMethod();
                }
            }
            if (!_fsuipcMethodInstance.AddVariableToRead(mpv))
                InitializeFsuipcMethod();
        }
    }
}
