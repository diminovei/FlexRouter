using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch.Microsoft.Win32.SafeHandles;

namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Класс для чтения/установки значений в памяти процесса
    /// </summary>
    public class MemoryPatchMethod
    {
        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ReadProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, ref uint lpNumberOfBytesRead);

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool WriteProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, ref uint lpNumberOfBytesWritten);

        /// <summary>
        /// Хэндл процесса симулятора
        /// </summary>
        private SafeProcessHandle _processHandle;
        /// <summary>
        /// Список найденных в процессе модулей
        /// </summary>
        private readonly Dictionary<string, ModuleInfo> _modules = new Dictionary<string, ModuleInfo>();
        /// <summary>
        /// ID процесса с которым работет класс
        /// </summary>
        private int _mainModuleProcessId;
        /// <summary>
        /// Дата и время последней попытки инициализации. Защита от слишком частых попыток
        /// </summary>
        private DateTime _lastTimeTryToInitialize = DateTime.MinValue;
        private InitializationState _lastInitStatus;
        /// <summary>
        /// Инициализация информации о модулях
        /// </summary>
        /// <param name="mainModuleName">главный модуль симулятора (fs9.exe)</param>
        /// <returns>true - simulator was found</returns>
        public InitializationState Initialize(string mainModuleName)
        {
            const string systemName = "MemoryPatcher";
            // Без этого процессор грузится на 50%, пока симулятор не загружен
            if (DateTime.Now < _lastTimeTryToInitialize + new TimeSpan(0, 0, 0, 2))
            {
                if (_lastInitStatus != null)
                    return _lastInitStatus;
                return new InitializationState
                {
                    System = systemName,
                    ErrorCode = (int)InitializationStatus.AttemptToInitializeTooOften,
                    ErrorMessage = "Attempted to initialize too often",
                    IsOk = false
                };

            }
            _lastTimeTryToInitialize = DateTime.Now;
            try
            {
                lock (_modules)
                {
                    _modules.Clear();
                    var runningProcesses = Process.GetProcesses();
                    var processesWithCorrectName = runningProcesses.Where(x => x.ProcessName.ToLower() == mainModuleName.ToLower());
                    if (processesWithCorrectName.Count() == 0)
                    {
                        _lastInitStatus = new InitializationState
                        {
                            System = systemName,
                            ErrorCode = (int)InitializationStatus.ModuleToPatchWasNotFound,
                            ErrorMessage = "Module '" + mainModuleName + "' was not found in process list",
                            IsOk = false
                        };
                        return _lastInitStatus;
                    }
                    if (processesWithCorrectName.Count() > 1)
                    {
                        _lastInitStatus = new InitializationState
                        {
                            System = systemName,
                            ErrorCode = (int)InitializationStatus.MultipleModulesFound,
                            ErrorMessage = "Multiple modules with name '" + mainModuleName + "' were found, don't know how to select correct one",
                            IsOk = false
                        };
                        return _lastInitStatus;
                    }
                    var process = processesWithCorrectName.First();
                    _mainModuleProcessId = process.Id;
                    _processHandle = SafeProcessHandle.OpenProcess(ProcessAccessFlags.VmOperation | ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VmRead | ProcessAccessFlags.VmWrite, false, _mainModuleProcessId);
                    for (var i = 0; i < process.Modules.Count; i++)
                    {
                        var info = new ModuleInfo
                        {
                            BaseAddress = process.Modules[i].BaseAddress,
                            Size = (uint)process.Modules[i].ModuleMemorySize,
                            Name = process.Modules[i].ModuleName
                        };
                        if (!_modules.ContainsKey(info.Name))
                            _modules.Add(info.Name, info);
                    }
                    process.Close();

                    _lastInitStatus = new InitializationState
                    {
                        System = systemName,
                        ErrorCode = (int)InitializationStatus.Ok,
                        ErrorMessage = "",
                        IsOk = true
                    };
                    return _lastInitStatus;
                }
            }
            catch (Exception ex)
            {
                _lastInitStatus = new InitializationState
                {
                    System = systemName,
                    ErrorCode = (int)InitializationStatus.Exception,
                    ErrorMessage = "An exception occuted: " + ex.Message,
                    IsOk = false
                };
                return _lastInitStatus;
            }
        }
        /// <summary>
        /// Получить список найденных модулей
        /// </summary>
        /// <returns></returns>
        public string[] GetListOfModulesLoadedInManagedProcess()
        {
            lock (_modules)
            {
                return _modules.Select(moduleInfo => moduleInfo.Value.Name).ToArray();
            }
        }
        /// <summary>
        /// Преобразование абсолютного смещения в относительное
        /// </summary>
        /// <param name="absoleteOffset">абсолютное смещение</param>
        /// <returns>имя модуля и относительное смещение в модуле, null - преобразование не удалось</returns>
        public ModuleAndOffset? ConvertAbsoleteToModuleOffset(uint absoleteOffset)
        {
            lock (_modules)
            {
                foreach (var info in _modules)
                {
                    if (absoleteOffset < (int)info.Value.BaseAddress || absoleteOffset > (int)info.Value.BaseAddress + info.Value.Size)
                        continue;
                    return new ModuleAndOffset
                    {
                        ModuleName = info.Value.Name,
                        Offset = absoleteOffset - (uint)info.Value.BaseAddress
                    };
                }
                return null;
            }
        }
        /// <summary>
        /// Установить значение переменной в памяти
        /// </summary>
        /// <param name="moduleName">Имя модуля, где находится переменная</param>
        /// <param name="moduleOffset">Относительное смещение от начала модуля</param>
        /// <param name="variableSize">Размер переменной</param>
        /// <param name="valueToSet">Значение, которое требуется установить</param>
        /// <returns>Результат установки значения переменной</returns>
        public ManageMemoryVariableResult SetVariableValue(string moduleName, uint moduleOffset, MemoryVariableSize variableSize, double valueToSet)
        {
            lock (_modules)
            {
                if (!_modules.ContainsKey(moduleName))
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.ModuleNotFound,
                        ErrorMessage = moduleName
                    };
                if ((int)_modules[moduleName].BaseAddress + _modules[moduleName].Size < moduleOffset)
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.OffsetIsOutOfModule
                    };
                try
                {
                    var baseOffset = (IntPtr)((int)_modules[moduleName].BaseAddress + moduleOffset);
                    var varConverter = new VariableConverter();
                    var buffer = varConverter.ValueToArray(valueToSet, variableSize);

                    uint bytesWrite = 0;
                    var res = WriteProcessMemory(_processHandle, baseOffset, buffer, (uint)buffer.Length, ref bytesWrite);
                    return new ManageMemoryVariableResult
                    {
                        Code = res ? MemoryPatchVariableErrorCode.Ok : MemoryPatchVariableErrorCode.WriteError,
                        Value = valueToSet
                    };
                }
                catch (Exception ex)
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.Unknown,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }
        /// <summary>
        /// Получить значение переменной в памяти
        /// </summary>
        /// <param name="moduleName">Имя модуля, где находится переменная</param>
        /// <param name="moduleOffset">Относительное смещение от начала модуля</param>
        /// <param name="variableSize">Размер переменной</param>
        /// <returns>Результат получения значения переменной</returns>
        public ManageMemoryVariableResult GetVariableValue(string moduleName, uint moduleOffset, MemoryVariableSize variableSize)
        {
            lock (_modules)
            {
                if (!_modules.ContainsKey(moduleName))
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.ModuleNotFound,
                        ErrorMessage = moduleName
                    };
                }
                
                if ((int) _modules[moduleName].BaseAddress + _modules[moduleName].Size < moduleOffset)
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.OffsetIsOutOfModule
                    };
                }

                try
                {
                    var baseOffset = (IntPtr)((int)_modules[moduleName].BaseAddress + moduleOffset);
                    var varConverter = new VariableConverter();
                    var buffer = new byte[varConverter.ConvertSize(variableSize)];
                    uint bytesRead = 0;
                    var readResult = ReadProcessMemory(_processHandle, baseOffset, buffer, (uint)buffer.Length, ref bytesRead);
                    if (!readResult)
                    {
                        return new ManageMemoryVariableResult
                        {
                            Code = MemoryPatchVariableErrorCode.ReadError,
                            Value = 0
                        };
                    }
                    var result = varConverter.ArrayToValue(buffer, variableSize);
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.Ok,
                        Value = result
                    };
                }
                catch (Exception ex)
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryPatchVariableErrorCode.Unknown,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }
    }
}
