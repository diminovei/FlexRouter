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
        public static PanelStorage PanelStorage = new PanelStorage();
        public static VariableManager VariableStorage = new VariableManager();

        /// <summary>
        /// Есть ли у панели дочерние элементы
        /// </summary>
        /// <param name="panelId"></param>
        /// <returns></returns>
        public static bool IsPanelInUse(int panelId)
        {
            return AccessDescriptorsStorage.Any(descriptorBase => descriptorBase.Value.GetAssignedPanelId() == panelId) ||
                VariableStorage.GetAllVariables().Any(variable => variable.PanelId == panelId);
        }

        private static readonly Dictionary<int, IControlProcessor> ControlProcessorsStorage = new Dictionary<int, IControlProcessor>();

        private static readonly Dictionary<int, DescriptorBase> AccessDescriptorsStorage = new Dictionary<int, DescriptorBase>();

        private static string _mainSimulatorProcess;

        public static string GetMainManagedProcessName()
        {
            return _mainSimulatorProcess;
        }

        public static void SetMainManagedProcessName(string processName)
        {
            _mainSimulatorProcess = processName;
        }

        /// <summary>
        /// GetSameVariablesNames
        /// </summary>
        /// <param name="variable"></param>
        /// <returns>null - такие же переменные не существуют</returns>
        public static string GetSameVariablesNames(IVariable variable)
        {
            var variables = VariableStorage.GetAllVariables();
            var varNames = variables.Where(v => v.IsEqualTo(variable) && v.Id != variable.Id).Aggregate(string.Empty, (current, v) => current + (GetVariableAndPanelNameById(v.Id) + Environment.NewLine));
            return string.IsNullOrEmpty(varNames) ? null : varNames;
        }

        public static string GetVariableAndPanelNameById(int id)
        {
            var variable = VariableStorage.GetVariableById(id);
            var variableName = variable.Name;
            var panel = PanelStorage.GetPanelById(variable.PanelId);
            return panel.Name + "." + variableName;
        }

        public static int GetVariableByPanelAndName(string panelName, string variableName)
        {
            var varList = VariableStorage.GetAllVariables();
            foreach (var v in varList)
            {
                if (v.Name != variableName)
                    continue;
                var panel = PanelStorage.GetPanelById(v.PanelId);
                if (panel.Name != panelName)
                    continue;
                return v.Id;
            }
            return -1;
        }

        public static IOrderedEnumerable<IVariable> GetSortedVariablesListByPanelId(int panelId)
        {
            var vars = VariableStorage.GetAllVariables();
            return vars.Where(ad => ad.PanelId == panelId).OrderBy(ad => ad.Name);
        }

        public static int RegisterAccessDescriptor(DescriptorBase ad)
        {
            AccessDescriptorsStorage[ad.GetId()] = ad;
            return ad.GetId();
        }

        public static void InitializeAccessDescriptors()
        {
            //lock (AccessDescriptorsStorage)
            //{
                foreach (var ad in AccessDescriptorsStorage)
                {
                    ad.Value.Initialize();
                }
//            }
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
            //lock (ControlProcessorsStorage)
            //{
                return !ControlProcessorsStorage.ContainsKey(id) ? null : ControlProcessorsStorage[id];
//            }
        }

        public static void RemoveControlProcessor(int associatedAccessDescriptorId)
        {
            //lock (ControlProcessorsStorage)
            //{
                if (!ControlProcessorsStorage.ContainsKey(associatedAccessDescriptorId))
                    return;
                ControlProcessorsStorage.Remove(associatedAccessDescriptorId);
//            }
        }

        public static int RegisterControlProcessor(IControlProcessor cp, int associatedAccessDescriptorId)
        {
            //lock (ControlProcessorsStorage)
            //{
                var id = associatedAccessDescriptorId;
                ControlProcessorsStorage.Add(id, cp);
                return id;
//            }
        }

        public static void SendEventToControlProcessors(ControlEventBase controlEvent)
        {
            //lock (ControlProcessorsStorage)
            //{
                foreach (var cp in ControlProcessorsStorage)
                {
                    if (cp.Value is IVisualizer)
                        continue;
                    ((ICollector)cp.Value).ProcessControlEvent(controlEvent);
                }
            //}
        }
        public static void TickControlProcessors()
        {
            //lock (ControlProcessorsStorage)
            //{
                foreach (var cp in ControlProcessorsStorage)
                {
                    var value = cp.Value as IRepeater;
                    if (value != null)
                        value.Tick();
                }
            //}
        }
        public static ControlProcessorHardware[] GetControlProcessorAssignments()
        {
            var modulesString = new List<string>();
            var modules = new List<ControlProcessorHardware>();
            foreach (var controlProcessor in ControlProcessorsStorage.Values)
            {
                var a = controlProcessor.GetUsedHardwareList();
                foreach (var assignment in a)
                {
                    if (string.IsNullOrEmpty(assignment))
                        continue;
                    var cph = ControlProcessorHardware.GenerateByGuid(assignment);
                    if (cph == null)
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

        public static IEnumerable<ControlEventBase> GetControlProcessorsNewEvents()
        {
            //lock (ControlProcessorsStorage)
            //{
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
            //}
        }

        public static IEnumerable<ControlEventBase> GetControlProcessorsClearEvents()
        {
            //lock (ControlProcessorsStorage)
            //{
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
            //}
        }

        public static void UpdateControlProcessorsAssignments()
        {
            foreach (var cp in ControlProcessorsStorage)
            {
                cp.Value.OnAssignmentsChanged();
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
        private const string ProfileAssignmentsFolder = "Profiles.Assignments";
        private const string ProfileTypeForAssignments = "Assignments";
        private const string AssignmentsExtension = "apa";

        public static Dictionary<string, string> GetProfileList()
        {
            return Utils.GetXmlList(ProfileFolder, ProfileExtensionMask, ProfileHeader, ProfileType);
        }

        public static Dictionary<string, string> GetProfileAssignmentsList()
        {
            return Utils.GetXmlList(ProfileAssignmentsFolder, "*."+AssignmentsExtension, ProfileHeader, ProfileTypeForAssignments);
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
                                var panels = PanelStorage.GetAllPanels();
                                foreach (var panel in panels)
                                    panel.Save(writer);                            
                                writer.WriteEndElement();
                                    
                                // Variables
                                writer.WriteStartElement("Variables");
                                writer.WriteAttributeString("ProcessToManage", _mainSimulatorProcess);
                                VariableStorage.SaveAllVariables(writer);
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

                                //// ControlProcessors
                                //writer.WriteStartElement("ControlProcessors");
                                //foreach (var cp in ControlProcessorsStorage)
                                //{
                                //    writer.WriteStartElement("ControlProcessor");
                                //    cp.Value.Save(writer);
                                //    writer.WriteEndElement();
                                //}
                                //writer.WriteEndElement();

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
            var assignmentsPath = GetAssignmentPathForProfile(profilePath);
            SaveAssignments(profileName, assignmentsPath);
        }

        private static string GetAssignmentPathForProfile(string profilePath)
        {
            return Path.Combine(Utils.GetFullSubfolderPath(ProfileAssignmentsFolder), Path.GetFileNameWithoutExtension(profilePath) + "." + AssignmentsExtension);
        }

        private static void SaveAssignments(string profileName, string profilePath)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;
                    writer.WriteStartDocument();
                    writer.WriteStartElement(ProfileHeader);
                    writer.WriteAttributeString("Type", ProfileTypeForAssignments);
                    writer.WriteAttributeString("Name", profileName);
                    writer.WriteStartElement(ProfileType);

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
        public static string GetProfileName(string path)
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            return Utils.GetXmlList(folder, name, ProfileHeader, ProfileType).Keys.FirstOrDefault();
        }
        /// <summary>
        /// Загрузка профиля
        /// </summary>
        /// <param name="profilePath">Путь к профилю</param>
        /// <returns>Успешно ли прошла загрузка</returns>
        public static bool LoadProfile(string profilePath)
        {
            Clear();
            // костыль для FS9
            var xp = new XPathDocument(profilePath);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/FlexRouterProfile");
            navPointer.MoveNext();
            _currentProfileName = navPointer.Current.GetAttribute("Name", navPointer.Current.NamespaceURI);

            _currentProfilePath = profilePath;
            navPointer = nav.Select("/FlexRouterProfile/Aircraft/Panels/Panel");
            while (navPointer.MoveNext())
            {
                var panel = Panel.Load(navPointer.Current);
                PanelStorage.StorePanel(panel);
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
                    variable = new FsuipcVariable();
                if (type == "MemoryPatchVariable")
                    variable = new MemoryPatchVariable();
                if (type == "FakeVariable")
                    variable = new FakeVariable();
                if (variable != null)
                {
                    variable.Load(navPointer.Current);
                    VariableStorage.StoreVariable(variable);
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

            var assignmentFileName = Path.GetFileNameWithoutExtension(profilePath) + "." + AssignmentsExtension;

            var assignmenProfiles = GetProfileAssignmentsList();
            if (assignmenProfiles.Values.FirstOrDefault(x => x.EndsWith(assignmentFileName)) != null)
            {
                var assignmentsPath = GetAssignmentPathForProfile(profilePath);
                LoadAssignments(assignmentsPath);
            }
            else
                LoadAssignments(profilePath);

            foreach (var descriptorBase in AccessDescriptorsStorage)
                descriptorBase.Value.Initialize();
            return true;
        }
        /// <summary>
        /// Загрузить ControlProcessor's
        /// </summary>
        /// <param name="controlProcessorsProfilePath"></param>
        private static void LoadAssignments(string controlProcessorsProfilePath)
        {
            var xp = new XPathDocument(controlProcessorsProfilePath);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/FlexRouterProfile/Aircraft/ControlProcessors/ControlProcessor");
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

                if (cp != null)
                {
                    cp.Load(navPointer.Current);
                    var cpId = RegisterControlProcessor(cp, id);
                    var ad = AccessDescriptorsStorage[id] as DescriptorMultistateBase;
                    if (ad == null) continue;

                    var controlProcessor = ControlProcessorsStorage[cpId];
                    if (controlProcessor != null)
                        controlProcessor.OnAssignmentsChanged();
                }
            }
            if (cpLoadErrorsCounter != 0)
            {
                var message = LanguageManager.GetPhrase(Phrases.SettingsMessageNotLoadedControlProcrssorsCount) + ": " + cpLoadErrorsCounter;
                MessageBox.Show(message, LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public static void Clear()
        {
            ControlProcessorsStorage.Clear();
            AccessDescriptorsStorage.Clear();
            VariableStorage.Clear();
            PanelStorage.Clear();
            GlobalFormulaKeeper.Instance.ClearAll();
            GlobalId.ClearAll();
            _mainSimulatorProcess = string.Empty;
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
            var assignmentsPath = GetAssignmentPathForProfile(_currentProfilePath);
            if (File.Exists(assignmentsPath))
                File.Delete(assignmentsPath);

            if (File.Exists(_currentProfilePath))
                File.Delete(_currentProfilePath);
        }
    }
}
