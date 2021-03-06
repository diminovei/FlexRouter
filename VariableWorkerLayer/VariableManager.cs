﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Helpers;
using FlexRouter.Problems;
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;

namespace FlexRouter.VariableWorkerLayer
{
    public class VariableManager
    {
        private readonly Dictionary<Guid, IVariable> _variables = new Dictionary<Guid, IVariable>();
        private readonly MemoryPatchMethod _memoryPatchMethodInstance = new MemoryPatchMethod();
        private readonly FsuipcMethod _fsuipcMethodInstance = new FsuipcMethod();
        private readonly List<string> _notFoundModules = new List<string>();
        private InitializationState _memoryPatchState;
        private InitializationState _fsuipcState;

        private void InitializeMemoryPatchMethod()
        {
            if (_memoryPatchState != null)
                Problems.Problems.AddOrUpdateProblem(_memoryPatchState.System, _memoryPatchState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, true);
            _memoryPatchState = _memoryPatchMethodInstance.Initialize(Profile.GetMainManagedProcessName());
            Problems.Problems.AddOrUpdateProblem(_memoryPatchState.System, _memoryPatchState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, _memoryPatchState.IsOk);
        }
        private void InitializeFsuipcMethod()
        {
            if (_fsuipcState != null)
                Problems.Problems.AddOrUpdateProblem(_fsuipcState.System, _fsuipcState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, true);
            _fsuipcState = _fsuipcMethodInstance.Initialize();
            Problems.Problems.AddOrUpdateProblem(_fsuipcState.System, _fsuipcState.ErrorMessage, ProblemHideOnFixOptions.HideDescription, _fsuipcState.IsOk);
        }

        private void Initialize()
        {
            InitializeMemoryPatchMethod();
            InitializeFsuipcMethod();
        }
        public string[] GetListOfModulesLoadedInManagedProcess()
        {
            return _memoryPatchMethodInstance.GetListOfModulesLoadedInManagedProcess();
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
            //foreach (var variable in _variables.Values)
            //{
            //    if(!(variable is FsuipcVariable))
            //        continue;
            //    var mem = (variable as FsuipcVariable).GetValueInMemory();
            //    var toWrite = (variable as FsuipcVariable).GetValueToSet();
            //    if (mem == toWrite)
            //        (variable as FsuipcVariable).SetValueToSet(null);
            //}
            var modules = string.Empty;
            foreach (var nfm in _notFoundModules)
            {
                if (modules.Length != 0)
                    modules += ", ";
                modules += nfm;
            }

            Problems.Problems.AddOrUpdateProblem("MemoryPatcherModules", "not found - " + modules, ProblemHideOnFixOptions.HideItemAndDescription, modules.Length == 0);
        }
        /// <summary>
        /// Получить переменную по её идентификатору
        /// </summary>
        /// <param name="id">Иднтификатор переменной</param>
        /// <returns></returns>
        public IVariable GetVariableById(Guid id)
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
        public void MakeAllItemsPublic()
        {
            lock (_variables)
            {
                foreach (var item in _variables)
                {
                    (item.Value as VariableBase).SetPrivacyType(ProfileItemPrivacyType.Public);
                }
            }
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
        public void RemoveVariable(Guid variableId)
        {
            if (!_variables.ContainsKey(variableId))
                return;
            _variables.Remove(variableId);
        }
        public void Load(XPathNavigator nav, string profileMainNodeName, ProfileItemPrivacyType profileItemPrivacyType)
        {
            var navPointer = nav.Select("/" + profileMainNodeName + "/Variables/Variable");
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                IVariable variable = null;
                if (type == "FsuipcVariable")
                    variable = new FsuipcVariable();
                if (type == "MemoryPatchVariable")
                    variable = new MemoryPatchVariable();
                if (type == "FakeVariable")
                    variable = new FakeVariable();
                if (variable != null)
                {
                    variable.Load(navPointer.Current);
                    (variable as VariableBase).SetPrivacyType(profileItemPrivacyType);
                    StoreVariable(variable);
                }
            }
        }
        /// <summary>
        /// Есть ли у панели дочерние элементы
        /// </summary>
        /// <param name="panelId"></param>
        /// <returns></returns>
        public bool IsPanelInUse(Guid panelId)
        {
            return _variables.Any(v => v.Value.PanelId == panelId);
        }
        public void SaveAllVariables(XmlTextWriter writer, ProfileItemPrivacyType profileItemPrivacyType)
        {
            if (profileItemPrivacyType == ProfileItemPrivacyType.Private && ApplicationSettings.DisablePersonalProfile)
                return;
            foreach (var v in _variables)
            {
                if ((v.Value as VariableBase).GetPrivacyType() == profileItemPrivacyType || ApplicationSettings.DisablePersonalProfile)
                {
                    writer.WriteStartElement("Variable");
                    v.Value.Save(writer);
                    writer.WriteEndElement();
                    writer.WriteString("\n");
                }
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
        public ProcessVariableError WriteValue(Guid varId, double value)
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
        public ReadVariableResult ReadCachedValue(Guid varId)
        {
            return ReadValue(varId, true);
        }
        /// <summary>
        /// Прочитать значение, которое было у переменной во время предыдущей синхронизации
        /// </summary>
        /// <param name="varId"></param>
        /// <returns></returns>
        public ReadVariableResult ReadValue(Guid varId)
        {
            return ReadValue(varId, false);
        }
        private ReadVariableResult ReadValue(Guid varId, bool getCachedValue)
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
            var valueToSet = mpv.GetValueToSet();
            if (valueToSet != null && valueToSet != mpv.GetValueInMemory())
            {
                var writeVarResult = _memoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize(), (double)valueToSet);
                if (writeVarResult.Code == MemoryPatchVariableErrorCode.ModuleNotFound)
                {
                    if (!_notFoundModules.Contains(mpv.ModuleName))
                        _notFoundModules.Add(mpv.ModuleName);
                    Initialize();
                    return;
                }
            }
            var readVarResult = _memoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize());
            if (readVarResult.Code == MemoryPatchVariableErrorCode.ModuleNotFound)
            {
                if (!_notFoundModules.Contains(mpv.ModuleName))
                    _notFoundModules.Add(mpv.ModuleName);
                Initialize();
            }
            else
            {
                mpv.SetValueInMemory(readVarResult.Value);
                mpv.SetValueToSet(readVarResult.Value);
            }
                
        }

        private void SynchronizeFsuipcVariable(FsuipcVariable mpv)
        {
            //if (mpv.GetValueToSet()!=null && mpv.GetValueToSet() == mpv.GetValueInMemory())
            //    mpv.SetValueToSet(null);

            var vts = mpv.GetValueToSet();

            if (vts != null)
            {
                if (!_fsuipcMethodInstance.AddVariableToWrite(mpv))
                {
                    InitializeFsuipcMethod();
                    return;
                }
                mpv.SetValueToSet(null);
            }

            if (!_fsuipcMethodInstance.AddVariableToRead(mpv))
            {
                InitializeFsuipcMethod();
            }
        }
    }
}
