using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Xml;
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

    /*    // Варианты оптимизации:
        //  Устанавливаемые значения - устанавливаем последнее указанное (или копим. В зависимости от настройки)
        //  Макросы только копим.
        public enum VariableTypes
        {
            BitManager,         //  Управление битами   (SetBit, ResetBit) управление в одной переменной только одним битом?
    //        BcdRange,         //  ВСD с диапазоном    Управление не получится. Нужна формула для Fsuipc
            Range,              //  Указан диапазон значений
            ValuesList,         //  Указан список значений
            ValuesListMacro,    //  Указан список значений
            NextPrevMacro,      //  Только с функциями следующий/предыдущий
            Formula,            //  Управление через формулу (в формулах поддержка преобразования BCD)
        }

        enum ValueState
        {
            Ok,
            Error
        }

        public enum VariableCapacity
        {
            SeparateAndPrevNextStates,  // Есть SetPositionInRange, SetDefaultState, SetNextState, SetPrevState
            PrevNextStatesOnly,         //  SetNextState, SetPrevState
        }
        public class ClickState
        {
            public int Id;
            public string Name;
            public string TokenizedMacro;                   // Токенизированный макрос
            public string TokenizedMacroInPolishNotation;   // Токенизированный макрос в польской нотации. Остаётся только заменить переменные значениями и считать
        }
        public class ClickVariableWithStates : VariableBase
        {
            public int Id;
            public int WindowId;
            private List<ClickState> ClickStates = new List<ClickState>();
        
            public void SetNextState() {}
            public void SetPrevState() {}
            public void SetDefaultState() {}
            public void SetPositionInRange(int stateId) {}
            public int DefaultStateId;              // Идентификатор состяния по-умолчанию
            public int CurrentStateId;              // Идентификатор состяния по-умолчанию
            public bool LoopStates;                 // При достижении последнего состояния и команде NextState переходить на нулевой?

        }

        public class ClickVariableWithNextPrevStates : VariableBase
        {
            public int Id;
            public int WindowId;
            private ClickState NextState;
            private ClickState PrevState;
        
            public void SetNextState() {}
            public void SetPrevState() {}
            public void SetDefaultState() {}
            public void SetPositionInRange(int stateId) {}
            public int DefaultStateId;              // Идентификатор состяния по-умолчанию
            public int CurrentStateId;              // Идентификатор состяния по-умолчанию
            public bool LoopStates;                 // При достижении последнего состояния и команде NextState переходить на нулевой?

        }

        struct ClickerVariable
        {
            public string Name;
        }*/

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

        public static ObservableCollection<InitializationState> GetInitializationStatus()
        {
            return _initializationStatus;
        }
        public static void Initialize()
        {
            _initializationStatus.Clear();
            _initializationStatus.Add(MemoryPatchMethodInstance.Initialize(Profile.GetMainManagedProcessName(), Profile.GetModuleExtensionFilter()));
            _initializationStatus.Add(FsuipcMethodInstance.Initialize());
            _initialized = true;
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
        private static void SynchronizeVariables()
        {
            while (true)
            {
                lock (ThreadLocker)
                {
                    if (_stopThread)
                        return;
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
/*
        public static ProcessVariableError PlayMacro(int varId, MacroToken[] macroToken)
        {
            if (!_variables.ContainsKey(varId))
                return ProcessVariableError.IdIsNotExist;
//            if (!(_variables[varId].Variable is ClickVariable))
//                return ProcessVariableError.CantProcessWithThisVarType;
            // Добавление статуса в переменную, прочее
            return ProcessVariableError.Ok;
        }
*/
        public static ProcessVariableError WriteValue(int varId, double value)
        {
            lock (ThreadLocker)
            {
                if (!Variables.ContainsKey(varId))
                    return ProcessVariableError.IdIsNotExist;
/*                if (_variables[varId].Variable is ClickVariable)
                    return ProcessVariableError.CantProcessWithThisVarType;*/

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
                    var writeVarResult = MemoryPatchMethodInstance.SetVariableValue(mpv.ModuleName, mpv.Offset, mpv.Size,
                                                                             mpv.ValueToSet);
                    if (writeVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось записать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (writeVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                            Initialize();
                    }
                    else
                        // Если запись прошла успешно, устанавливаем состояние в "проверка"
                        mpvExt.SetState(VariableState.Check);

                }
                if (mpvExt.GetState() == VariableState.Read || mpvExt.GetState() == VariableState.Check)
                {
                    var readVarResult = MemoryPatchMethodInstance.GetVariableValue(mpv.ModuleName, mpv.Offset, mpv.Size);
                    if (readVarResult.Code != MemoryVariableGetSetErrorCode.Ok)
                    {
                        mpvExt.LeaveCurrentState();
                        // Если не удалось прочитать, то возможно, модуль ещё не загружен в память. Переинициализируем
                        if (readVarResult.Code == MemoryVariableGetSetErrorCode.ModuleNotFound)
                            Initialize();
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
            /*
            // Последовательность состояний при записи: Write->Check->Read
            if (mpvExt.GetState() == VariableState.Write)
            {
                if (!_fsuipcMethodInstance.AddVariableToWrite(mpv))
                    mpvExt.LeaveCurrentState();
                else
                    // Если запись прошла успешно, устанавливаем состояние в "проверка"
                    mpvExt.SetState(VariableState.Check);

            }
            if (mpvExt.GetState() == VariableState.Read || mpvExt.GetState() == VariableState.Check)
            {
                if (!_fsuipcMethodInstance.AddVariableToRead(mpv))
                    mpvExt.LeaveCurrentState();
                else
                {
                    // Если чтение прошло успешно
                    // Если установлено состояние "проверка" и значение в памяти не соотоветствует, тому, что записывали, пробуем записать ещё раз
                    if (mpvExt.GetState() == VariableState.Check &&
                        readVarResult.Value != ((MemoryPatchVariable)_variables[id].Variable).ValueToSet)
                        mpvExt.SetState(VariableState.Write);
                    else
                    {
                        // Устанавливает состояние в стандартное "чтение"
                        mpvExt.SetState(VariableState.Read);
                        ((MemoryPatchVariable)_variables[id].Variable).ValueInMemory = readVarResult.Value;
                    }
                }
            }*/
        }
    }
}
