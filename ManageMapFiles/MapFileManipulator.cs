using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FlexRouter.ManageMapFiles
{
    class MapFileManipulator
    {
        /// <summary>
        /// Получить список уникальных имён модулей, используемых в профиле для MemoryPatchVariable
        /// </summary>
        /// <returns>Массив неповторяющихся имён модулей</returns>
        public static string[] GetListOfModulesUsedInProfile()
        {
            var variables = Profile.VariableStorage.GetAllVariables();
            var modules = variables.Where(x => x is MemoryPatchVariable).Select(y => ((MemoryPatchVariable)y).ModuleName).Distinct().ToArray();
            return modules;
        }
        private static LinkerInfo[] LoadMapFile(string mapPath)
        {
            var linkerInfo = new List<LinkerInfo>();
            var map = Path.GetFileNameWithoutExtension(mapPath);

            ulong imageBase = 0;
            var filestream = new FileStream(mapPath,
                                          FileMode.Open,
                                          FileAccess.Read,
                                          FileShare.Read);
            var file = new StreamReader(filestream, Encoding.UTF8, true, 128);

            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Replace("  ", " ").Replace(" ", ";");
                var lines = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 3)
                    continue;

                if (lines[1] == "___ImageBase")
                    imageBase = ulong.Parse(lines[2], NumberStyles.HexNumber);

                if (!lines[1].Contains("@"))
                    continue;
                bool isFunction = false;
                foreach (var l in lines)
                {
                    if (l != "f")
                        continue;
                    isFunction = true;
                    break;
                }
                if (isFunction)
                    continue;
                var currentAddress = (uint)(ulong.Parse(lines[2], NumberStyles.HexNumber) - imageBase);
                if (lines[1].Contains("@@"))
                    lines[1] = lines[1].Remove(lines[1].IndexOf("@@"));

                linkerInfo.Add(new LinkerInfo { ObjName = lines[lines.Length - 1], Name = lines[1], Offset = currentAddress, ModuleName = map });
            }
            return linkerInfo.ToArray();
        }
        /// <summary>
        /// Для переменных, используемых в профиле, получить имена и смещения от переменной в выбранном map-файле
        /// </summary>
        /// <param name="variableModuleName">модуль, для которого производится поиск</param>
        /// <returns></returns>
        public static VariableNameFromMapFile[] GetVariableNamesFromMapFile(string mapFilePath, string variableModuleName)
        {
            var linkerInfos = LoadMapFile(mapFilePath);

            var VariableNameFromMapFileArray = new List<VariableNameFromMapFile>();

            var variables = Profile.VariableStorage.GetAllVariables();

            var memoryPathVariablesArray = variables.Where(x => x is MemoryPatchVariable && (x as MemoryPatchVariable).ModuleName.ToLower() == variableModuleName.ToLower()).ToArray();

            foreach (MemoryPatchVariable memoryPatchVariable in memoryPathVariablesArray)
            {
                var closestVariableBelow = linkerInfos.OrderBy(x => x.Offset).Last(y => y.Offset <= memoryPatchVariable.Offset);
                var n = new VariableNameFromMapFile
                {
                    VarId = memoryPatchVariable.GetId(),
                    NameInMapFile = closestVariableBelow.Name,
                    DistanceToClosestVarBelow = memoryPatchVariable.Offset - closestVariableBelow.Offset
                };
                VariableNameFromMapFileArray.Add(n);
            }
            return VariableNameFromMapFileArray.ToArray();
        }
        /// <summary>
        /// Для переменных, используемых в профиле, получить новые смещения из выбранного map-файла
        /// </summary>
        /// <param name="variableModuleName">модуль, для которого производится поиск</param>
        /// <returns></returns>
        public static VariableOffsetFromMapFile[] GetVariableOffsetFromMapFile(string mapFileName, string variableModuleName)
        {
            var VariableOffsetFromMapFileArray = new List<VariableOffsetFromMapFile>();

            var linkerInfos = LoadMapFile(mapFileName);

            var variables = Profile.VariableStorage.GetAllVariables();
            var memoryPathVariablesArray = variables.Where(x => x is MemoryPatchVariable && (x as MemoryPatchVariable).ModuleName.ToLower() == variableModuleName.ToLower()).ToArray();

            foreach (MemoryPatchVariable memoryPatchVariable in memoryPathVariablesArray)
            {
                var varInfo = memoryPatchVariable.NameInMapFile.Split('+');
                if (varInfo.Length != 2)
                    continue;
                var name = varInfo[0];
                var distance = uint.Parse(varInfo[1].Replace("0x", ""), NumberStyles.HexNumber);

                var v = linkerInfos.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (v == null)
                    continue;

                var n = new VariableOffsetFromMapFile
                {
                    VarId = memoryPatchVariable.GetId(),
                    Offset = v.Offset + distance
                };
                VariableOffsetFromMapFileArray.Add(n);
            }
            return VariableOffsetFromMapFileArray.ToArray();
        }
    }
}
