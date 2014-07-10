using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Xml;
using FlexRouter.Hardware;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;
using FlexRouter.VariableSynchronization;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;

namespace FlexRouter.VariableWorkerLayer
{
    // При чтении понимать, обновилось ли значение, успешно ли прошло, если нет, повторить, но знать, что повторяем
    // При чтении/записи понимать, успешно ли прошло, если нет, повторить, но знать, что повторяем
    // При ничего не деланьи иногда перечитывать переменную и если она обновилась - знать об этом, но как быть, если ничего не изменилось, а показания ещё не считывались ни разу?
    public class ReadVariableResult
    {
        public double Value;
        public ProcessVariableError Error;
    }

    internal enum VariableState
    {
        Read,
        Write,
        Check,
        Idle
    }

    internal class VariableExtended
    {
        public IVariable Variable;
        private VariableState _state;
        private int _attemptCounter;

        public VariableState GetState()
        {
            return _state;
        }

        public void SetState(VariableState state)
        {
            _state = state;
            _attemptCounter = 0;
        }

        public void LeaveCurrentState()
        {
            _attemptCounter++;
        }
    }

    public enum ProcessVariableError
    {
        Ok,
        IdIsNotExist,
        CantProcessWithThisVarType
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
        private static bool _initialized;
        private static readonly Dictionary<int, VariableExtended> Variables = new Dictionary<int, VariableExtended>();
        private static readonly MemoryPatchMethod MemoryPatchMethodInstance = new MemoryPatchMethod();
        private static readonly FsuipcMethod FsuipcMethodInstance = new FsuipcMethod();
        private static Thread _syncThread;
        private static bool _stopThread;
        private static readonly object ThreadLocker = new object();
        private static ObservableCollection<InitializationState> _initializationStatus = new ObservableCollection<InitializationState>();

        public static InitializationState[] GetInitializationStatus()
        {
            lock (ThreadLocker)
            {
                return _initializationStatus.ToArray();    
            }
        }
        public static void Initialize()
        {
            lock (ThreadLocker)
            {
                _initializationStatus.Clear();
                var iStatus  = MemoryPatchMethodInstance.Initialize(Profile.GetMainManagedProcessName(), Profile.GetModuleExtensionFilter());
                _initializationStatus.Add(iStatus);
                Problems.AddOrUpdateProblem(iStatus.System, iStatus.ErrorMessage, ProblemHideOnFixOptions.HideDescription, iStatus.IsOk);
                var fsuipcStatus = FsuipcMethodInstance.Initialize();
                _initializationStatus.Add(fsuipcStatus);
                Problems.AddOrUpdateProblem(fsuipcStatus.System, fsuipcStatus.ErrorMessage, ProblemHideOnFixOptions.HideDescription, fsuipcStatus.IsOk);
                _initialized = true;
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
                    for (var i = 0; i < Variables.Count; i++)
                    {
                        SynchronizeMemoryPatchVariable(Variables.ElementAt(i).Key);
                        SynchronizeFsuipcVariable(Variables.ElementAt(i).Key);
                    }
                    FsuipcMethodInstance.Process();
                    for (var i = 0; i < Variables.Count; i++)
                    {
                        GetFsuipcVariableValue(Variables.ElementAt(i).Key);
                    }
                }
                string modules = string.Empty;
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
                return !Variables.ContainsKey(id) ? null : Variables[id].Variable;
            }
        }
        public static IEnumerable<IVariable> GetVariablesList()
        {
            lock (Variables)
            {
                return Variables.Select(v => v.Value.Variable).ToArray();    
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
                var varExtended = new VariableExtended {Variable = variable};
                varExtended.SetState(VariableState.Read);
                Variables.Add(id, varExtended);
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
                    v.Value.Variable.Save(writer);
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

                if (Variables[varId].Variable is FsuipcVariable)
                {
                    ((FsuipcVariable)Variables[varId].Variable).ValueToSet = value;
                    Variables[varId].SetState(VariableState.Write);
                }
                    

                if (Variables[varId].Variable is MemoryPatchVariable)
                {
                    ((MemoryPatchVariable)Variables[varId].Variable).ValueToSet = value;
                    Variables[varId].SetState(VariableState.Write);
                }

                if (Variables[varId].Variable is FakeVariable)
                {
                    ((FakeVariable)Variables[varId].Variable).ValueToSet = value;
                    Variables[varId].SetState(VariableState.Write);
                }
                return ProcessVariableError.Ok;
            }
        }
        public static ReadVariableResult ReadValue(int varId)
        {
            lock (ThreadLocker)
            {
                var result = new ReadVariableResult();
                if (!Variables.ContainsKey(varId))
                {
                    result.Error = ProcessVariableError.IdIsNotExist;
                    return result;
                }

/*                if (_variables[varId].Variable is ClickVariable)
                {
                    result.Error = ProcessVariableError.CantProcessWithThisVarType;
                    return result;
                }*/
                result.Error = ProcessVariableError.Ok;
                if (Variables[varId].Variable is FsuipcVariable)
                    result.Value = ((FsuipcVariable) Variables[varId].Variable).ValueInMemory; // ValueToSet;

                if (Variables[varId].Variable is MemoryPatchVariable)
                    result.Value = ((MemoryPatchVariable) Variables[varId].Variable).ValueInMemory;//ValueToSet;
                if (Variables[varId].Variable is FakeVariable)
                    result.Value = ((FakeVariable)Variables[varId].Variable).ValueToSet;
                return result;
            }
        }
/*        private static void SynchronizeMemoryPatchVariable(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is MemoryPatchVariable))
                    return;
                var mpv = (MemoryPatchVariable)Variables[id].Variable;
                var mpvExt = Variables[id];
                // Последовательность состояний при записи: Write->Check->Read
                if (mpvExt.GetState() == VariableState.Read || mpvExt.GetState() == VariableState.Check)
                {
                    var readVarResult = MemoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize());
                    if (readVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось прочитать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (readVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                        {
                            if (!NotFoundModules.Contains(mpv.ModuleName))
                                NotFoundModules.Add(mpv.ModuleName);
                            Initialize();
                        }
                    }
                    else
                    {
                        // Если чтение прошло успешно
                        // Если установлено состояние "проверка" и значение в памяти не соотоветствует, тому, что записывали, пробуем записать ещё раз
                        if (mpvExt.GetState() == VariableState.Check && readVarResult.Value != ((MemoryPatchVariable)Variables[id].Variable).ValueToSet)
                            mpvExt.SetState(VariableState.Write);
                        else
                        {
                            // Устанавливает состояние в стандартное "чтение"
                            mpvExt.SetState(VariableState.Read);
                            ((MemoryPatchVariable)Variables[id].Variable).ValueInMemory = readVarResult.Value;
                        }
                    }
                }
                if (mpvExt.GetState() == VariableState.Write)
                {
                    var writeVarResult = MemoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize(), mpv.ValueToSet);

                    if (writeVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось записать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (writeVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                        {
                            if (!NotFoundModules.Contains(mpv.ModuleName))
                                NotFoundModules.Add(mpv.ModuleName);
                            Initialize();
                        }

                    }
                    else
                        // Если запись прошла успешно, устанавливаем состояние в "проверка"
                        mpvExt.SetState(VariableState.Check);
                }
                //if (mpvExt.GetState() == VariableState.Check)
                //if(!MemoryPatchVariablesOnCheckState.Contains(id))
                //    MemoryPatchVariablesOnCheckState.Add(id);
            }
        }*/
        private static void SynchronizeMemoryPatchVariable(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is MemoryPatchVariable))
                    return;
                var mpv = (MemoryPatchVariable)Variables[id].Variable;
                var mpvExt = Variables[id];
                // Последовательность состояний при записи: Write->Check->Read
                if (mpvExt.GetState() == VariableState.Write)
                {
                    var writeVarResult = MemoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize(),
                                                                             mpv.ValueToSet);
                    if (writeVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось записать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (writeVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                        {
                            if (!NotFoundModules.Contains(mpv.ModuleName))
                                NotFoundModules.Add(mpv.ModuleName);
                            Initialize();
                        }
                    }
                    else
                        // Если запись прошла успешно, устанавливаем состояние в "проверка"
                        mpvExt.SetState(VariableState.Check);

                }
                if (mpvExt.GetState() == VariableState.Read || mpvExt.GetState() == VariableState.Check)
                {
                    var readVarResult = MemoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.GetVariableSize());
                    if (readVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось прочитать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (readVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                        {
                            if (!NotFoundModules.Contains(mpv.ModuleName))
                                NotFoundModules.Add(mpv.ModuleName);
                            Initialize();
                        }
                    }
                    else
                    {
                        // Если чтение прошло успешно
                        // Если установлено состояние "проверка" и значение в памяти не соотоветствует, тому, что записывали, пробуем записать ещё раз
                        if (mpvExt.GetState() == VariableState.Check &&
                            readVarResult.Value != ((MemoryPatchVariable)Variables[id].Variable).ValueToSet)
                            mpvExt.SetState(VariableState.Write);
                        else
                        {
                            // Устанавливает состояние в стандартное "чтение"
                            mpvExt.SetState(VariableState.Read);
                            ((MemoryPatchVariable)Variables[id].Variable).ValueInMemory = readVarResult.Value;
                        }
                    }
                }
            }
        }

/*        private static void SynchronizeFsuipcVariable(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is FsuipcVariable))
                    return;
                var mpv = (FsuipcVariable)Variables[id].Variable;
                var mpvExt = Variables[id];

                if (mpvExt.GetState() == VariableState.Read || mpvExt.GetState() == VariableState.Check)
                    if (!FsuipcMethodInstance.AddVariableToRead(mpv))
                        mpvExt.LeaveCurrentState();

                if (mpvExt.GetState() == VariableState.Write)
                    if (FsuipcMethodInstance.AddVariableToWrite(mpv))
                        mpvExt.SetState(VariableState.Check);
                    else
                        mpvExt.LeaveCurrentState();
            }
        }*/
        private static void SynchronizeFsuipcVariable(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is FsuipcVariable))
                    return;
                var mpv = (FsuipcVariable)Variables[id].Variable;
                var mpvExt = Variables[id];
                if (mpvExt.GetState() == VariableState.Write)
                    if (FsuipcMethodInstance.AddVariableToWrite(mpv))
                        mpvExt.SetState(VariableState.Read);
                    else
                        mpvExt.LeaveCurrentState();

                if (mpvExt.GetState() == VariableState.Read)
                    if (!FsuipcMethodInstance.AddVariableToRead(mpv))
                        mpvExt.LeaveCurrentState();
            }
        }
/*        private static void GetFsuipcVariableValue(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is FsuipcVariable))
                    return;
                var mpv = (FsuipcVariable)Variables[id].Variable;
                var mpvExt = Variables[id];
                if (mpvExt.GetState() != VariableState.Read && mpvExt.GetState() != VariableState.Check)
                    return;
                mpv.ValueInMemory = FsuipcMethodInstance.GetValue(id);

                if (mpvExt.GetState() == VariableState.Check && mpv.ValueInMemory != mpv.ValueToSet)
                    mpvExt.SetState(VariableState.Write);
            }
        }*/
        private static void GetFsuipcVariableValue(int id)
        {
            lock (Variables)
            {
                if (!(Variables[id].Variable is FsuipcVariable))
                    return;
                var mpv = (FsuipcVariable)Variables[id].Variable;
                var mpvExt = Variables[id];
                if (mpvExt.GetState() == VariableState.Read)
                    mpv.ValueInMemory = FsuipcMethodInstance.GetValue(id);
            }
        }
    }
}
