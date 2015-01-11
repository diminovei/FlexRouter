using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Xml;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;

namespace FlexRouter.VariableWorkerLayer
{
    public class ReadVariableResult
    {
        public double Value;
        public ProcessVariableError Error;
    }

    public enum ProcessVariableError
    {
        Ok,
        IdIsNotExist,
        CantProcessWithThisVarType,
        NotInitialized
    }

    public class InitializationState
    {
        public bool IsOk;
        public string System;
        public int ErrorCode;
        public string ErrorMessage;
    }
    public static class VariableManager
    {
        private static readonly Dictionary<int, IVariable> Variables = new Dictionary<int, IVariable>();
        private static readonly MemoryPatchMethod MemoryPatchMethodInstance = new MemoryPatchMethod();
        private static readonly FsuipcMethod FsuipcMethodInstance = new FsuipcMethod();
        private static Thread _syncThread;
        private static bool _stopThread;
        private static readonly object ThreadLocker = new object();
        private static readonly ObservableCollection<InitializationState> InitializationStatus = new ObservableCollection<InitializationState>();

        public static InitializationState[] GetInitializationStatus()
        {
            lock (ThreadLocker)
            {
                return InitializationStatus.ToArray();    
            }
        }
        public static void Initialize()
        {
            lock (ThreadLocker)
            {
                InitializationStatus.Clear();
                var iStatus  = MemoryPatchMethodInstance.Initialize(Profile.GetMainManagedProcessName(), Profile.GetModuleExtensionFilter());
                InitializationStatus.Add(iStatus);
                Problems.AddOrUpdateProblem(iStatus.System, iStatus.ErrorMessage, ProblemHideOnFixOptions.HideDescription, iStatus.IsOk);
                var fsuipcStatus = FsuipcMethodInstance.Initialize();
                InitializationStatus.Add(fsuipcStatus);
                Problems.AddOrUpdateProblem(fsuipcStatus.System, fsuipcStatus.ErrorMessage, ProblemHideOnFixOptions.HideDescription, fsuipcStatus.IsOk);
            }
        }

/*        static ~VariableManager()
        {
            if (_initialized)
                _fsuipcMethodInstance.UnInitialize();
            _initialized = false;
        }*/

        /// <summary>
        /// Стартовать процесс синхронизации переменных
        /// </summary>
        public static void Start()
        {
            if (_syncThread != null)
                if (_syncThread.IsAlive)
                    return;
            lock (ThreadLocker)
            {
                _stopThread = false;
            }
            _syncThread = new Thread(SynchronizeVariables);
            _syncThread.Start();
        }

        /// <summary>
        /// Остановить процесс синхронизации переменных
        /// </summary>
        public static void Stop()
        {
            if (_syncThread == null)
                return;
            if (!_syncThread.IsAlive)
                return;
            lock (ThreadLocker)
            {
                _stopThread = true;
            }
            _syncThread.Join();
        }
        public static string[] GetModulesList()
        {
            return MemoryPatchMethodInstance.GetModulesList();
        }
        public static ModuleAndOffset? ConvertAbsoleteOffsetToRelative(uint absoluteOffset)
        {
            return MemoryPatchMethodInstance.ConvertAbsoleteToModuleOffset(absoluteOffset);
        }
        private static readonly List<string> NotFoundModules = new List<string>();
        private static void SynchronizeVariables()
        {
            while (true)
            {
                lock (ThreadLocker)
                {
                    if (_stopThread)
                        return;

                    NotFoundModules.Clear();
                    foreach (var variable in Variables.Values)
                    {
                        if (variable is FakeVariable)
                            SynchronizeFakeVariable(variable as FakeVariable);
                        if (variable is MemoryPatchVariable)
                            SynchronizeMemoryPatchVariable(variable as MemoryPatchVariable);
                        if (variable is FsuipcVariable)
                            SynchronizeFsuipcVariable(variable as FsuipcVariable);
                    }
                    FsuipcMethodInstance.Process();
                    foreach (var variable in Variables.Values.OfType<FsuipcVariable>())
                    {
                        GetFsuipcVariableValue(variable);
                    }
                }
                var modules = string.Empty;
                foreach (var nfm in NotFoundModules)
                {
                    if (modules.Length != 0)
                        modules += ", ";
                    modules += nfm;
                }

                Problems.AddOrUpdateProblem("MemoryPatcherModules", "not found - " + modules, ProblemHideOnFixOptions.HideItemAndDescription, modules.Length == 0);
                Thread.Sleep(100);
            }
        }
        public static IVariable GetVariableById(int id)
        {
            lock (Variables)
            {
                return !Variables.ContainsKey(id) ? null : Variables[id];
            }
        }
        public static IEnumerable<IVariable> GetVariablesList()
        {
            lock (Variables)
            {
                return Variables.Select(v => v.Value);    
            }
        }
        /// <summary>
        /// Зарегистрировать переменную
        /// </summary>
        /// <param name="variable">Переменная</param>
        /// <param name="generateNewId">Нужно ли генерировать новый идентификатор?</param>
        /// <returns>идентификатор переменной или -1, если переменная с таким Id уже есть</returns>
        public static int RegisterVariable(IVariable variable, bool generateNewId)
        {
            lock (Variables)
            {
                var id = generateNewId ? Utils.GetNewId(Variables) : ((VariableBase) variable).Id;
                if (generateNewId)
                    ((VariableBase) variable).Id = id;

                if (Variables.ContainsKey(id))
                    return -1;
                Variables.Add(id, variable);
                return id;
            }
        }
        public static void RemoveVariable(int variableId)
        {
            lock (Variables)
            {
                if (!Variables.ContainsKey(variableId))
                    return;
                Variables.Remove(variableId);
            }
        }
        public static void Save(XmlTextWriter writer)
        {
            lock (Variables)
            {
                foreach (var v in Variables)
                {
                    writer.WriteStartElement("Variable");
                    v.Value.Save(writer);
                    writer.WriteEndElement();
                    writer.WriteString("\n");
                }
            }
        }
        public static void Clear()
        {
            lock (Variables)
            {
                Variables.Clear();
            }
        }
        public static ProcessVariableError WriteValue(int varId, double value)
        {
            lock (ThreadLocker)
            {
                if (!Variables.ContainsKey(varId))
                    return ProcessVariableError.IdIsNotExist;
                var variable = (MemoryVariableBase) Variables[varId];
                variable.SetValueToSet(value);
                if(variable is FakeVariable)
                    variable.SetValueInMemory(value);
                return ProcessVariableError.Ok;
            }
        }
        public static ReadVariableResult ReadCachedValue(int varId)
        {
            return ReadValue(varId, true);
        }
        public static ReadVariableResult ReadValue(int varId)
        {
            return ReadValue(varId, false);
        }
        private static ReadVariableResult ReadValue(int varId, bool getCachedValue)
        {
            lock (ThreadLocker)
            {
                var result = new ReadVariableResult();
                if (!Variables.ContainsKey(varId))
                {
                    result.Error = ProcessVariableError.IdIsNotExist;
                    return result;
                }
                result.Error = ProcessVariableError.Ok;
                var valueToSet = ((MemoryVariableBase)Variables[varId]).GetValueToSet();
                var valueInMemory = ((MemoryVariableBase)Variables[varId]).GetValueInMemory();
                
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
        }
        private static void SynchronizeFakeVariable(FakeVariable fakeVariable)
        {
            fakeVariable.SetValueInMemory(fakeVariable.GetValueToSet());
        }
        private static void SynchronizeMemoryPatchVariable(MemoryPatchVariable mpv)
        {
            lock (Variables)
            {
                // Если переменная не инициализирована или нет данных для записи, переменную нужно прочитать
                if (mpv.GetValueInMemory() == null || mpv.GetValueToSet() == null)
                {
                    var readVarResult = MemoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize());
                    if (readVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                    {
                        if (!NotFoundModules.Contains(mpv.ModuleName))
                            NotFoundModules.Add(mpv.ModuleName);
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
                    var writeVarResult = MemoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize(), (double)valueToSet);
                    if (writeVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                    {
                        if (!NotFoundModules.Contains(mpv.ModuleName))
                            NotFoundModules.Add(mpv.ModuleName);
                        Initialize();
                    }
                    else
                    {
                        mpv.SetValueInMemory(writeVarResult.Value);
                        mpv.SetValueToSet(null);
                    }
                }
            }
        }
        private static void SynchronizeFsuipcVariable(FsuipcVariable mpv)
        {
            lock (Variables)
            {
                // Если переменная не инициализирована или нет данных для записи, переменную нужно прочитать
                if (mpv.GetValueInMemory() == null || mpv.GetValueToSet() == null)
                {
                    if (FsuipcMethodInstance.AddVariableToRead(mpv))
                        return;
                    mpv.SetValueInMemory(null);
                    Initialize();
                }
                else
                {
                    var valueToSet = mpv.GetValueToSet();
                    if (valueToSet == null)
                        return;

                    if (!FsuipcMethodInstance.AddVariableToWrite(mpv))
                        Initialize();
                    else
                    {
                        FsuipcMethodInstance.AddVariableToRead(mpv);
                        mpv.SetValueInMemory(mpv.GetValueToSet());
                        //ToDo: тут не правильно. Передаём инстанс класса, а потом в нём же зануляем значение.
//                        mpv.SetValueToSet(null);
                    }
                }
            }
        }
        private static void GetFsuipcVariableValue(FsuipcVariable fsuipcVariable)
        {
            lock (Variables)
            {
                fsuipcVariable.SetValueInMemory(FsuipcMethodInstance.GetValue(fsuipcVariable.Id));
            }
        }
    }
}
