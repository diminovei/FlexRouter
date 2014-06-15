using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FlexRouter.VariableSynchronization;

namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Коды ошибок при установке/чтении переменной в памяти
    /// </summary>
    public enum MemoryVariableGetSetErrorCode
    {
        Ok,                     // Операция прошла успешно
        ModuleNotFound,         // Модуль, в котором находится переменная не найден
        OffsetIsOutOfModule,    // Смещение выходит за размеры модуля
        SetError,               // Не удалось установить переменную
        Unknown                 // Неизвестная ошибка
    }
    /// <summary>
    /// Результат установки значения переменной или получения значения переменной в памяти
    /// </summary>
    public struct ManageMemoryVariableResult
    {
        public MemoryVariableGetSetErrorCode Code;  // Код ошибки
        public string ErrorMessage;                 // Текст ошибки/исключения
        public double Value;                        // Полученое/установленное значение
    }
    /// <summary>
    /// Возвращаемое значение для преобразования абсолютного смещения в относительное
    /// </summary>
    public struct ModuleAndOffset
    {
        public string ModuleName;   // Имя модуля
        public uint Offset;         // Смещение в модуле
    }
    /// <summary>
    /// Класс для чтения/установки значений в памяти процесса
    /// </summary>
    public class MemoryPatchMethod
    {
        #region Imports
        [DllImport("Kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, ref uint lpNumberOfBytesRead);

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, ref uint lpNumberOfBytesWritten);

        [DllImport("Kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        static extern bool CloseHandle(IntPtr hObject);


        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VmOperation = 0x00000008,
            VmRead = 0x00000010,
            VmWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
        #endregion
        /// <summary>
        /// Информация о модуле симулятора для метода MemoryPatch
        /// </summary>
        internal struct ModuleInfo
        {
            public string Name;         // Имя модуля (gau)
            public IntPtr BaseAddress;  // Адрес, куда загружен модуль
            public uint Size;            // Размер модуля в памяти
        }
        private readonly Dictionary<string, ModuleInfo> _modules = new Dictionary<string, ModuleInfo>();
        private int _mainModuleProcessId;
        private string _moduleExtensionFilter;
        private DateTime _lastTimeTryToInitialize = DateTime.Now;
/*        /// <summary>
        /// Загружен ли модуль (для проверки загружен ли самолёт)
        /// </summary>
        /// <param name="mainModuleName"></param>
        /// <param name="moduleToCheckName"></param>
        /// <returns></returns>
        public bool CheckModulePresence(string mainModuleName, string moduleToCheckName)
        {
            if (_modules.ContainsKey(moduleToCheckName))
                return true;
            return Initialize(mainModuleName, _moduleExtensionFilter) && _modules.ContainsKey(moduleToCheckName);
        }*/

        public enum InitializationStatus
        {
            Ok,
            AttemptToInitializeTooOften,
            ModuleToPatchWasNotFound,
            Exception
        }
        /// <summary>
        /// Инициализация информации о модулях
        /// </summary>
        /// <param name="mainModuleName">главный модуль симулятора (fs9.exe)</param>
        /// <param name="moduleExtensionFilter">расширение, которое соответствует приборам</param>
        /// <returns>true - simulator was found</returns>
        public InitializationState Initialize(string mainModuleName, string moduleExtensionFilter)
        {
            const string systemName = "MemoryPatcher";
            // Без этого процессор грузится на 50%, пока симулятор не загружен
            if (DateTime.Now < _lastTimeTryToInitialize + new TimeSpan(0, 0, 0, 2))
                return new InitializationState
                {
                    System = systemName,
                    ErrorCode = (int)InitializationStatus.AttemptToInitializeTooOften,
                    ErrorMessage = "Attempted to initialize too often",
                    IsOk = false
                };
            _lastTimeTryToInitialize = DateTime.Now;
            try
            {
                lock (_modules)
                {
                    _moduleExtensionFilter = moduleExtensionFilter;
                    _modules.Clear();
                    var runningProcesses = Process.GetProcesses();
                    foreach (var process in runningProcesses)
                    {
                        if (process.ProcessName != mainModuleName)
                            continue;
                        _mainModuleProcessId = process.Id;
                        for (var i = 0; i < process.Modules.Count; i++)
                        {
                            if (!process.Modules[i].ModuleName.ToLower().EndsWith(_moduleExtensionFilter.ToLower()))
                                continue;
                            var info = new ModuleInfo
                            {
                                BaseAddress = process.Modules[i].BaseAddress,
                                Size = (uint)process.Modules[i].ModuleMemorySize,
                                Name = process.Modules[i].ModuleName
                            };
                            _modules.Add(info.Name, info);
                        }
                        process.Close();
                        return new InitializationState
                        {
                            System = systemName,
                            ErrorCode = (int)InitializationStatus.Ok,
                            ErrorMessage = "",
                            IsOk = true
                        };
                    }
                    return new InitializationState
                    {
                        System = systemName,
                        ErrorCode = (int)InitializationStatus.ModuleToPatchWasNotFound,
                        ErrorMessage = "Module '" + mainModuleName + "' was not found in process list",
                        IsOk = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new InitializationState
                {
                    System = systemName,
                    ErrorCode = (int)InitializationStatus.Exception,
                    ErrorMessage = "An exception occuted: " + ex.Message,
                    IsOk = false
                };
            }
        }
        public string[] GetModulesList()
        {
            lock (_modules)
            {
                var modules = new List<string>();
                foreach (var moduleInfo in _modules)
                {
                    modules.Add(moduleInfo.Value.Name);
                }
                return modules.ToArray();
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
                        Code = MemoryVariableGetSetErrorCode.ModuleNotFound,
                        ErrorMessage = moduleName
                    };
                if ((int)_modules[moduleName].BaseAddress + _modules[moduleName].Size < moduleOffset)
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.OffsetIsOutOfModule
                    };
                try
                {
                    var baseOffset = (IntPtr)((int)_modules[moduleName].BaseAddress + moduleOffset);
                    var hProcess =
                        OpenProcess(
                            ProcessAccessFlags.VmOperation | ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VmRead |
                            ProcessAccessFlags.VmWrite, false, _mainModuleProcessId);

                    var varConverter = new VariableConverter();
                    var buffer = varConverter.ValueToArray(valueToSet, variableSize);

                    uint bytesWrite = 0;
                    var res = WriteProcessMemory(hProcess, baseOffset, buffer, (uint)buffer.Length /*(uint)variableSize*/,
                                                 ref bytesWrite);
                    CloseHandle(hProcess);
                    return new ManageMemoryVariableResult
                    {
                        Code = res ? MemoryVariableGetSetErrorCode.Ok : MemoryVariableGetSetErrorCode.SetError,
                        Value = valueToSet
                    };
                }
                catch (Exception ex)
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.Unknown,
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
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.ModuleNotFound,
                        ErrorMessage = moduleName
                    };
                if ((int)_modules[moduleName].BaseAddress + _modules[moduleName].Size < moduleOffset)
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.OffsetIsOutOfModule
                    };
                try
                {
                    var baseOffset = (IntPtr)((int)_modules[moduleName].BaseAddress + moduleOffset);
                    var hProcess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VmRead | ProcessAccessFlags.VmWrite, false, _mainModuleProcessId);
                    var varConverter = new VariableConverter();
                    var buffer = new byte[varConverter.ConvertSize(variableSize)];
                    uint bytesRead = 0;
                    ReadProcessMemory(hProcess, baseOffset, buffer, (uint)buffer.Length, ref bytesRead);
                    CloseHandle(hProcess);
                    var result = varConverter.ArrayToValue(buffer, variableSize);
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.Ok,
                        Value = result
                    };
                }
                catch (Exception ex)
                {
                    return new ManageMemoryVariableResult
                    {
                        Code = MemoryVariableGetSetErrorCode.Unknown,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }
    }
}
