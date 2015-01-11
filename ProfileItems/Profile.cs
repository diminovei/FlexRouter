using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorsUI.Dialogues;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
using MessageBox = System.Windows.Forms.MessageBox;

namespace FlexRouter.ProfileItems
{
    public static class Profile
    {
        private static readonly Dictionary<int, Panel> PanelsStorage = new Dictionary<int, Panel>();

        private static readonly Dictionary<int, IControlProcessor> ControlProcessorsStorage =
            new Dictionary<int, IControlProcessor>();

        private static readonly Dictionary<int, DescriptorBase> AccessDescriptorsStorage =
            new Dictionary<int, DescriptorBase>();

        private static string _mainSimulatorProcess;
        private static string _moduleExtensionFilter;

        public static string GetModuleExtensionFilter()
        {
            return _moduleExtensionFilter;
        }

        public static void SetModuleExtensionFilter(string moduleExtensionFilter)
        {
            _moduleExtensionFilter = moduleExtensionFilter;
        }

        public static string GetMainManagedProcessName()
        {
            return _mainSimulatorProcess;
        }

        public static void SetMainManagedProcessName(string processName)
        {
            _mainSimulatorProcess = processName;
        }

        public static int RegisterPanel(Panel panel, bool isNewPanel)
        {
            var id = isNewPanel ? GlobalId.GetNew() : panel.Id;
            if (isNewPanel)
            {
                panel.Id = id;
                PanelsStorage.Add(id, panel);
            }
            else
                PanelsStorage[id] = panel;
            return id;
        }

        public static void RemovePanel(int panelId)
        {
            if (IsPanelInUse(panelId) || !PanelsStorage.ContainsKey(panelId))
                return;

            PanelsStorage.Remove(panelId);
        }

        public static IOrderedEnumerable<Panel> GetPanelsList()
        {
            return PanelsStorage.Values.OrderBy(panel => panel.Name);
        }

        public static Panel GetPanelById(int id)
        {
            if (!PanelsStorage.ContainsKey(id))
                return null;
            return PanelsStorage[id];
        }

        public static int GetPanelIdByName(string name)
        {
            foreach (var p in PanelsStorage)
            {
                if (p.Value.Name == name)
                    return p.Key;
            }
            return -1;
        }

        public static IVariable GetVariableById(int id)
        {
            return VariableManager.GetVariableById(id);
        }

        public static int GetVariableByPanelAndName(string panelName, string variableName)
        {
            var varList = VariableManager.GetVariablesList();
            foreach (var v in varList)
            {
                if (v.Name != variableName)
                    continue;
                var panel = GetPanelById(v.PanelId);
                if (panel.Name != panelName)
                    continue;
                return v.Id;
            }
            return -1;
        }

        public static int RegisterVariable(IVariable variable, bool isNew)
        {
            return VariableManager.RegisterVariable(variable, isNew);
        }

        public static IOrderedEnumerable<IVariable> GetSortedVariablesListByPanelId(int panelId)
        {
            var vars = VariableManager.GetVariablesList();
            return vars.Where(ad => ad.PanelId == panelId).OrderBy(ad => ad.Name);
        }

        public static int RegisterAccessDescriptor(DescriptorBase ad)
        {
            AccessDescriptorsStorage[ad.GetId()] = ad;
            return ad.GetId();
        }

        public static DescriptorBase GetAccessDesciptorById(int id)
        {
            if (!AccessDescriptorsStorage.ContainsKey(id))
                return null;
            return AccessDescriptorsStorage[id];
        }

        public static IOrderedEnumerable<DescriptorBase> GetSortedAccessDesciptorListByPanelId(int panelId)
        {
            return AccessDescriptorsStorage.Values.Where(ad => ad.GetAssignedPanelId() == panelId).OrderBy(ad => ad.GetName());
        }
        public static IOrderedEnumerable<DescriptorBase> GetSortedAccessDesciptorList()
        {
            return AccessDescriptorsStorage.Values.OrderBy(ad => ad.GetName());
        }

        public static void RemoveVariable(int variableId)
        {
            VariableManager.RemoveVariable(variableId);
        }
        public static void RemoveAccessDescriptor(int accessDescriptorId)
        {
            if (!AccessDescriptorsStorage.ContainsKey(accessDescriptorId))
                return;

            RemoveControlProcessor(accessDescriptorId);
            AccessDescriptorsStorage.Remove(accessDescriptorId);
        }
        /// <summary>
        /// Get ControlProcessorById
        /// </summary>
        /// <param name="id">Associated AccessDescriptorId</param>
        /// <returns></returns>
        public static IControlProcessor GetControlProcessorByAccessDescriptorId(int id)
        {
            lock (ControlProcessorsStorage)
            {
                return !ControlProcessorsStorage.ContainsKey(id) ? null : ControlProcessorsStorage[id];
            }
        }

        public static void RemoveControlProcessor(int associatedAccessDescriptorId)
        {
            lock (ControlProcessorsStorage)
            {
                if (!ControlProcessorsStorage.ContainsKey(associatedAccessDescriptorId))
                    return;
                ControlProcessorsStorage.Remove(associatedAccessDescriptorId);
            }
        }

        public static int RegisterControlProcessor(IControlProcessor cp, int associatedAccessDescriptorId)
        {
            lock (ControlProcessorsStorage)
            {
                var id = associatedAccessDescriptorId;
                ControlProcessorsStorage.Add(id, cp);
                return id;
            }
        }

        public static void SendEventToControlProcessors(ControlEventBase controlEvent)
        {
            lock (ControlProcessorsStorage)
            {
                foreach (var cp in ControlProcessorsStorage)
                {
                    if (cp.Value is IVisualizer)
                        continue;
                    ((ICollector)cp.Value).ProcessControlEvent(controlEvent);
                }
            }
        }
        public static void TickControlProcessors()
        {
            lock (ControlProcessorsStorage)
            {
                foreach (var cp in ControlProcessorsStorage)
                {
                    if (cp.Value is IRepeater)
                        ((IRepeater)cp.Value).Tick();
                }
            }
        }

        public static IEnumerable<ControlEventBase> GetControlProcessorsNewEvents()
        {
            lock (ControlProcessorsStorage)
            {
                var evs = new List<ControlEventBase>();
                foreach (var cps in ControlProcessorsStorage)
                {
                    if(!(cps.Value is IVisualizer))
                        continue;
                    var range = ((IVisualizer) cps.Value).GetNewEvent();
                    if(range == null)
                        continue;
                    evs.AddRange(range);
                }
                return evs;
            }
        }

        public static IEnumerable<ControlEventBase> GetControlProcessorsClearEvents()
        {
            lock (ControlProcessorsStorage)
            {
                var evs = new List<ControlEventBase>();
                foreach (var cps in ControlProcessorsStorage)
                {
                    if (!(cps.Value is IVisualizer))
                        continue;
                    var range = ((IVisualizer)cps.Value).GetClearEvent();
                    if (range == null)
                        continue;
                    evs.AddRange(range);
                }
                return evs;
            }
        }

        private static string GenerateProfileFileName()
        {
            var folder = Utils.GetFullSubfolderPath("Profiles");
            var index = 1;
            while (true)
            {
                var fileName = "profile" + index.ToString("000") + ".ap";
                fileName = Path.Combine(folder, fileName);
                if (!File.Exists(fileName))
//                if (_profileList.All(pair => pair.Value.ToLower() != fileName.ToLower()))
                    return fileName;
                index++;
            }
        }
        private const string ProfileHeader = "FlexRouterProfile";
        private const string ProfileExtensionMask = "*.ap";
        private const string ProfileType = "Aircraft";
        private const string ProfileFolder = "Profiles";

        public static ControlProcessorHardware[] GetControlProcessorAssignments()
        {
            var modulesString = new List<string>();
            var modules = new List<ControlProcessorHardware>();
            foreach (var controlProcessor in ControlProcessorsStorage.Values)
            {
                var a = controlProcessor.GetUsedHardwareList();
                foreach (var assignment in a)
                {
                    if(string.IsNullOrEmpty(assignment))
                        continue;
                    var cph = ControlProcessorHardware.GenerateByGuid(assignment);
                    if(cph == null)
                        continue;
                    if (cph.ModuleType == HardwareModuleType.Button)
                    {
                        var module = cph.MotherBoardId + "|" + cph.ModuleType + "|" + cph.ModuleId;
                        if (!modulesString.Contains(module))
                        {
                            modules.Add(cph);
                            modulesString.Add(module);
                        }
                    }
                }
            }
            return modules.ToArray();
        }
        public static Dictionary<string, string> GetProfileList()
        {
            return Utils.GetXmlList(ProfileFolder, ProfileExtensionMask, ProfileHeader, ProfileType);
        }

        private static string _currentProfileName;
        private static string _currentProfilePath;

        public static void SaveCurrentProfile()
        {
            SaveProfile(_currentProfileName, _currentProfilePath);
        }
        public static void SaveProfileAs(string path)
        {
            SaveProfile(_currentProfileName, path);
        }
        private static void SaveProfile(string profileName, string profilePath)
        {
            try
            {
                using (var sw = new StringWriter())
                {
                    using (var writer = new XmlTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;
                        writer.WriteStartDocument();
                            writer.WriteStartElement(ProfileHeader);
                            writer.WriteAttributeString("Type", ProfileType);
                            writer.WriteAttributeString("Name", profileName);
                                writer.WriteStartElement(ProfileType);

                                    // Panels
                                    writer.WriteStartElement("Panels");
                                    foreach (var panel in PanelsStorage)
                                        panel.Value.Save(writer);                            
                                    writer.WriteEndElement();
                                    
                                    // Variables
                                    writer.WriteStartElement("Variables");
                                    writer.WriteAttributeString("ProcessToManage", _mainSimulatorProcess);
                                    VariableManager.Save(writer);
                                    writer.WriteEndElement();

                                    // AccessDescriptors
                                    writer.WriteStartElement("AccessDescriptors");
                                    foreach (var ads in AccessDescriptorsStorage)
                                    {
                                        writer.WriteStartElement("AccessDescriptor");
                                        ads.Value.Save(writer);
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();

                                    // ControlProcessors
                                    writer.WriteStartElement("ControlProcessors");
                                    foreach (var cp in ControlProcessorsStorage)
                                    {
                                        writer.WriteStartElement("ControlProcessor");
                                        cp.Value.Save(writer);
                                        writer.WriteEndElement();
                                    }
                                    writer.WriteEndElement();

                                writer.WriteEndElement();

                        
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    if (File.Exists(profilePath))
                        File.Copy(profilePath, profilePath + ".bak", true);
                    using (var swToDisk = new StreamWriter(profilePath, false, Encoding.Unicode))
                    {
                        var parsedXml = XDocument.Parse(sw.ToString());
                        swToDisk.Write(parsedXml.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
        }

/*        public static string GenerateProfileFileName()
        {
            var profileList = GetProfileList();
            var folder = Utils.GetFullSubfolderPath("Profiles");
            var index = 1;
            while (true)
            {
                var fileName = "profile" + index.ToString("000") + ".ap";
                fileName = Path.Combine(folder, fileName);
                if (profileList.All(pair => pair.Value.ToLower() != fileName.ToLower()))
                    return fileName;
                index++;
            }
        }*/

        private static string CheckAndCorrectProfileFileName(string name)
        {
            var profileList = GetProfileList();
            if (!profileList.Keys.Contains(name))
                return name;
            var index = 1;
            while (true)
            {
                if (!profileList.Keys.Contains(name + index))
                    return name+index;
                index++;
            }
        }
        /// <summary>
        /// Загрузка профиля
        /// </summary>
        /// <param name="profilePath">Путь к профилю</param>
        /// <returns>Успешно ли прошла загрузка</returns>
        public static bool LoadProfile(string profilePath)
        {
            return LoadOrMergeProfile(profilePath, profilePath);
        }
        /// <summary>
        /// Слияние профилей
        /// </summary>
        /// <param name="profileName">Путь к профилю, из которого будет браться всё, кроме ControlProcrssor</param>
        /// <param name="controlProcessorsProfile">Путь к профилю, из которого будут браться ControlProcrssor'ы</param>
        /// <returns>Успешно ли прошла загрузка</returns>
        public static bool MergeAssignmentsWithProfile(string profileName, string controlProcessorsProfile)
        {
            return LoadOrMergeProfile(profileName, controlProcessorsProfile);
        }

        public static string GetProfileName(string path)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            return Utils.GetXmlList(folder, name, ProfileHeader, ProfileType).Keys.FirstOrDefault();
        }
        /// <summary>
        /// Загрузка профиля или слияние профилей
        /// При загрузке оба параметра одинаковы, при слиянии указываются различны профили
        /// </summary>
        /// <param name="profilePath">Путь к профилю, из которого будет браться всё, кроме ControlProcrssor</param>
        /// <param name="controlProcessorsProfilePath">Путь к профилю, из которого будут браться ControlProcrssor'ы</param>
        /// <returns>Успешно ли прошла загрузка</returns>
        private static bool LoadOrMergeProfile(string profilePath, string controlProcessorsProfilePath)
        {
            Clear();
            // костыль для FS9
            // _moduleExtensionFilter = ".gau";
            _moduleExtensionFilter = "";

            var xp = new XPathDocument(profilePath);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/FlexRouterProfile");
            navPointer.MoveNext();
            _currentProfileName = navPointer.Current.GetAttribute("Name", navPointer.Current.NamespaceURI);
            _currentProfilePath = controlProcessorsProfilePath ?? profilePath;
            navPointer = nav.Select("/FlexRouterProfile/Aircraft/Panels/Panel");
            while (navPointer.MoveNext())
            {
                var panel = new Panel();
                panel.Load(navPointer.Current);
                PanelsStorage.Add(panel.Id, panel);
            }

            navPointer = nav.Select("/FlexRouterProfile/Aircraft/Variables");
            navPointer.MoveNext();
            _mainSimulatorProcess = navPointer.Current.GetAttribute("ProcessToManage", navPointer.Current.NamespaceURI);
            
            navPointer = nav.Select("/FlexRouterProfile/Aircraft/Variables/Variable");
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                IVariable variable = null;
                if (type == "FsuipcVariable")
                {
                    variable = new FsuipcVariable();
                }
                if (type == "MemoryPatchVariable")
                {
                    variable = new MemoryPatchVariable();
                }
                if (type == "FakeVariable")
                {
                    variable = new FakeVariable();
                }
                if (variable != null)
                {
                    variable.Load(navPointer.Current);
                    RegisterVariable(variable, false);
                }
            }
            navPointer = nav.Select("/FlexRouterProfile/Aircraft/AccessDescriptors/AccessDescriptor");
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                var ad = Utils.FindAndCreate<DescriptorBase>(type);

                if (ad != null)
                {
                    ad.Load(navPointer.Current);
                    RegisterAccessDescriptor(ad);
                }
            }
            xp = new XPathDocument(controlProcessorsProfilePath);
            nav = xp.CreateNavigator();
            navPointer = nav.Select("/FlexRouterProfile/Aircraft/ControlProcessors/ControlProcessor");
            var cpLoadErrorsCounter = 0;
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                var id = int.Parse(navPointer.Current.GetAttribute("AssignedAccessDescriptorId", navPointer.Current.NamespaceURI));
                if (!AccessDescriptorsStorage.ContainsKey(id))
                {
                    cpLoadErrorsCounter++;
                    continue;
                }
                var accessDescriptor = GetAccessDesciptorById(id);

                Object[] args = { accessDescriptor };
                var cp = Utils.FindAndCreate<IControlProcessor>(type, args);

                //IControlProcessor cp = null;
                //if (type == "AxisRangeProcessor")
                //    cp = new AxisRangeProcessor(accessDescriptor);
                //if (type == "LampProcessor")
                //    cp = new LampProcessor(accessDescriptor);
                //if (type == "IndicatorProcessor")
                //    cp = new IndicatorProcessor(accessDescriptor);
                //if (type == "LedMatrixIndicatorProcessor")
                //    cp = new LedMatrixIndicatorProcessor(accessDescriptor);
                //if (type == "EncoderProcessor")
                //    cp = new EncoderProcessor(accessDescriptor);
                //if (type == "ButtonProcessor")
                //    cp = new ButtonProcessor(accessDescriptor);
                //if (type == "ButtonPlusMinusProcessor")
                //    cp = new ButtonPlusMinusProcessor(accessDescriptor);
                //if (type == "ButtonBinaryInputProcessor")
                //    cp = new ButtonBinaryInputProcessor(accessDescriptor);
                if (cp != null)
                {
                    cp.Load(navPointer.Current);
                    var cpId = RegisterControlProcessor(cp, id);
                    var ad = AccessDescriptorsStorage[id] as DescriptorMultistateBase;
                    if (ad!=null)
                    {
                        var states = ad.GetStateDescriptors();
                        var controlProcessor = ControlProcessorsStorage[cpId] as IControlProcessorMultistate;
                        if(controlProcessor!=null)
                            controlProcessor.RenewStatesInfo(states);
                    }
                }
            }
            // Если был импорт профиля
            if (profilePath != controlProcessorsProfilePath)
            {
                if (cpLoadErrorsCounter != 0)
                {
                    var message = LanguageManager.GetPhrase(Phrases.SettingsMessageNotLoadedControlProcrssorsCount) + ": " + cpLoadErrorsCounter;
                    MessageBox.Show(message, LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                SaveCurrentProfile();
            }
            foreach (var descriptorBase in AccessDescriptorsStorage)
                descriptorBase.Value.Initialize();
            return true;
        }

        public static void InitializeAccessDescriptors()
        {
            lock (AccessDescriptorsStorage)
            {
                foreach (var ad in AccessDescriptorsStorage)
                {
                    ad.Value.Initialize();
                }
            }
        }

        public static bool IsPanelInUse(int panelId)
        {
            return AccessDescriptorsStorage.Any(descriptorBase => descriptorBase.Value.GetAssignedPanelId() == panelId) ||
                VariableManager.GetVariablesList().Any(variable => variable.PanelId == panelId);
        }

        public static void Clear()
        {
            ControlProcessorsStorage.Clear();
            AccessDescriptorsStorage.Clear();
            VariableManager.Clear();
            PanelsStorage.Clear();
            GlobalFormulaKeeper.Instance.ClearAll();
            _mainSimulatorProcess = string.Empty;
            _moduleExtensionFilter = string.Empty;
            _currentProfileName = null;
            _currentProfilePath = null;
        }

        public static bool IsProfileLoaded()
        {
            return _currentProfileName != null;
        }

        public static string GetLoadedProfileName()
        {
            return _currentProfileName;
        }

        public static string CreateNewProfile()
        {
            var profileList = GetProfileList();
        loop:
            var it = new ProfileEditor();
            if (it.ShowDialog() != true)
                return null;
            var profileName = it.GetProfileName();
            var mainProcessName = it.GetMainProcessName();
            if(profileList.ContainsKey(profileName))
            {
                System.Windows.MessageBox.Show(LanguageManager.GetPhrase(Phrases.SettingsMessageProfileNameIsAlreadyExist),
                                LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                goto loop;
            }
            Clear();
            _currentProfileName = profileName;
            _mainSimulatorProcess = mainProcessName;
            _currentProfilePath = GenerateProfileFileName();
            SaveCurrentProfile();
            return profileName;
        }
        
        public static string RenameProfile()
        {
            var profileList = GetProfileList();
        loop:
            var it = new InputString(LanguageManager.GetPhrase(Phrases.SettingsMessageInputProfileNewName), _currentProfileName);
            if (it.ShowDialog() != true)
                return null;
            var profileName = it.GetText();
            if (profileList.ContainsKey(profileName))
            {
                System.Windows.MessageBox.Show(LanguageManager.GetPhrase(Phrases.SettingsMessageProfileNameIsAlreadyExist),
                                LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                goto loop;
            }
            _currentProfileName = profileName;
            SaveCurrentProfile();
            return profileName;
        }
        public static void RemoveCurrentProfile()
        {
            Clear();
            if (File.Exists(_currentProfilePath))
                File.Delete(_currentProfilePath);
        }

        public static string Import(string path)
        {
            return ImportProfile(false, path);
        }

        public static string ImportAndSaveAssignments(string path)
        {
            return ImportProfile(true, path);
        }

        private static string ImportProfile(bool andSaveAssignments, string pathToProfileToImport)
        {
            var profileName = GetProfileName(pathToProfileToImport);
            var importedProfileName = CheckAndCorrectProfileFileName(profileName);
            var importedProfilePath = GenerateProfileFileName();
            if(andSaveAssignments)
                MergeAssignmentsWithProfile(pathToProfileToImport, _currentProfilePath);
            else
                LoadProfile(pathToProfileToImport);
            SaveProfile(importedProfileName, importedProfilePath);
            return importedProfileName;
        }
    }
}
