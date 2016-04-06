using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.FormulaKeeper;
using FlexRouter.EditorsUI.Dialogues;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.ProfileItems
{
    public static class Profile
    {
        public static PanelStorage PanelStorage = new PanelStorage();
        public static VariableManager VariableStorage = new VariableManager();
        public static ControlProcessorsStorage ControlProcessor = new ControlProcessorsStorage();
        public static AccessDescriptorsStorage AccessDescriptor = new AccessDescriptorsStorage(ControlProcessor);

        /// <summary>
        /// Есть ли у панели дочерние элементы
        /// </summary>
        /// <param name="panelId"></param>
        /// <returns></returns>
        public static bool IsPanelInUse(Guid panelId)
        {
            return AccessDescriptor.IsPanelInUse(panelId) || VariableStorage.IsPanelInUse(panelId);
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
        public static string GetVariableAndPanelNameById(Guid id)
        {
            var variable = VariableStorage.GetVariableById(id);
            var variableName = variable.Name;
            var panel = PanelStorage.GetPanelById(variable.PanelId);
            return panel.Name + "." + variableName;
        }
        public static Guid GetVariableByPanelAndName(string panelName, string variableName)
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
            return Guid.Empty;
        }
        public static IOrderedEnumerable<IVariable> GetSortedVariablesListByPanelId(Guid panelId)
        {
            var vars = VariableStorage.GetAllVariables();
            return vars.Where(ad => ad.PanelId == panelId).OrderBy(ad => ad.Name);
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
        private const string ProfileFolder = "Profiles";
        private const string ProfileAssignmentsFolder = "Profiles.Personal";

        private const string AssignmentsExtension = "apa";
        private const string PrivateProfileExtension = "app";
        private static Guid _currentProfileId = Guid.Empty;
        private static string _mainSimulatorProcess;
        public static string GetMainManagedProcessName()
        {
            return _mainSimulatorProcess;
        }
        public static void SetMainManagedProcessName(string processName)
        {
            _mainSimulatorProcess = processName;
        }

        public static Dictionary<string, string> GetProfileList(string profileType)
        {
            return Utils.GetXmlList(ProfileFolder, ProfileExtensionMask, ProfileHeader, profileType);
        }
        public static string GetPrivateProfilePath(string profileType, string fileNameOnly)
        {
            var profileList = Utils.GetXmlList(ProfileAssignmentsFolder, PrivateProfileExtension, ProfileHeader, ProfileItemPrivacyType.Private.ToString());
            return profileList.FirstOrDefault(x => x.Value.Contains(fileNameOnly)).Value;
        }
        private static string _currentProfileName;
        private static string _currentProfilePath;
        private static string _currentPersonalProfilePath;

        public static bool LoadProfileByName(string profileName)
        {
            var profileList = GetProfileList(ProfileItemPrivacyType.Public.ToString());
            if (!profileList.ContainsKey(profileName))
                return false;
            _currentProfilePath = profileList[profileName];
            var loadResult = Load(_currentProfilePath, ProfileItemPrivacyType.Public);

            _currentPersonalProfilePath = _currentProfilePath.Replace(@"\" + ProfileFolder + @"\", @"\" + ProfileAssignmentsFolder + @"\") + "p";
            if (File.Exists(_currentPersonalProfilePath))
                Load(_currentPersonalProfilePath, ProfileItemPrivacyType.Private);

            var assignmentsProfilePath = _currentProfilePath.Replace(@"\" + ProfileFolder + @"\", @"\" + ProfileAssignmentsFolder + @"\") + "a";
            ControlProcessor.Load(assignmentsProfilePath, ProfileHeader, AccessDescriptor.GetAll());
            AccessDescriptor.InitializeAccessDescriptors();
            return loadResult;
        }

        public static void RemoveAccessDescriptor(Guid id)
        {
            ControlProcessor.RemoveControlProcessor(id);
            AccessDescriptor.RemoveAccessDescriptor(id);
        }
        /// <summary>
        /// Загрузка профиля
        /// </summary>
        /// <param name="profilePath">Путь к профилю</param>
        /// <param name="profileItemPrivacyType"></param>
        /// <returns>Успешно ли прошла загрузка</returns>
        public static bool Load(string profilePath, ProfileItemPrivacyType profileItemPrivacyType)
        {
            if(profileItemPrivacyType == ProfileItemPrivacyType.Public)
                Clear();
            // костыль для FS9
            var xp = new XPathDocument(profilePath);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/" + ProfileHeader);
            navPointer.MoveNext();
            _currentProfileName = navPointer.Current.GetAttribute("Name", navPointer.Current.NamespaceURI);
            if (!Guid.TryParse(navPointer.Current.GetAttribute("Id", navPointer.Current.NamespaceURI), out _currentProfileId))
            {
                _currentProfileId = GlobalId.GetNew();
            }
            if (profileItemPrivacyType == ProfileItemPrivacyType.Public)
                _currentProfilePath = profilePath;
            else
                _currentPersonalProfilePath = profilePath;
            navPointer = nav.Select("/" + ProfileHeader + "/Panels/Panel");
            while (navPointer.MoveNext())
            {
                var panel = Panel.Load(navPointer.Current);
                panel.SetPrivacyType(profileItemPrivacyType);
                PanelStorage.StorePanel(panel);
            }

            navPointer = nav.Select("/" + ProfileHeader + "/Variables");
            navPointer.MoveNext();
            _mainSimulatorProcess = navPointer.Current.GetAttribute("ProcessToManage", navPointer.Current.NamespaceURI);
            VariableStorage.Load(nav, ProfileHeader, profileItemPrivacyType);

            AccessDescriptor.Load(nav, ProfileHeader, profileItemPrivacyType);
            return true;
        }

        public static void MakeAllItemsPublic()
        {
            PanelStorage.MakeAllItemsPublic();
            AccessDescriptor.MakeAllItemsPublic();
            VariableStorage.MakeAllItemsPublic();
        }
        public static void Save(bool disablePrivateProfile)
        {
            Save(_currentProfileName, _currentProfilePath, ProfileItemPrivacyType.Public, disablePrivateProfile);
            Save(_currentProfileName, _currentPersonalProfilePath, ProfileItemPrivacyType.Private, disablePrivateProfile);
        }
        public static void SaveAs(string path)
        {
            Save(_currentProfileName, path, ProfileItemPrivacyType.Public, true);
        }
        private static void Save(string profileName, string profilePath, ProfileItemPrivacyType profileItemPrivacyType, bool disablePrivateProfile)
        {
            if (disablePrivateProfile && profileItemPrivacyType == ProfileItemPrivacyType.Private)
                return;

            var privateProfilePath = Utils.GetFullSubfolderPath(ProfileAssignmentsFolder);
            if (!File.Exists(privateProfilePath))
                Directory.CreateDirectory(privateProfilePath);
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;
                    writer.WriteStartDocument();
                    // Заголовок
                    writer.WriteStartElement(ProfileHeader);
                    writer.WriteAttributeString("Type", profileItemPrivacyType.ToString());
                    writer.WriteAttributeString("Name", profileName);
                    writer.WriteAttributeString("Id", _currentProfileId.ToString());

                    // Панели
                    writer.WriteStartElement("Panels");
                    var panels = PanelStorage.GetAllPanels();
                    foreach (var panel in panels)
                    {
                        if (panel.GetPrivacyType() == profileItemPrivacyType || disablePrivateProfile)
                            panel.Save(writer);
                    }
                    writer.WriteEndElement();
                                    
                    // Переменные
                    writer.WriteStartElement("Variables");
                    writer.WriteAttributeString("ProcessToManage", _mainSimulatorProcess);
                    VariableStorage.SaveAllVariables(writer, profileItemPrivacyType);
                    writer.WriteEndElement();

                    // AccessDescriptors
                    AccessDescriptor.Save(writer, profileItemPrivacyType, disablePrivateProfile);
                    // Закрываем заголовок
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
            if (profileItemPrivacyType == ProfileItemPrivacyType.Public)
            {
                var assignmentsPath = GetAssignmentPathForProfile(profilePath);
                ControlProcessor.Save(assignmentsPath, ProfileHeader, _currentProfileId);
            }
        }
        private static string GetAssignmentPathForProfile(string profilePath)
        {
            return Path.Combine(Utils.GetFullSubfolderPath(ProfileAssignmentsFolder), Path.GetFileNameWithoutExtension(profilePath) + "." + AssignmentsExtension);
        }
        public static void Clear()
        {
            ControlProcessor.Clear();
            AccessDescriptor.Clear();
            VariableStorage.Clear();
            PanelStorage.Clear();
            GlobalId.Clear();
            GlobalFormulaKeeper.Instance.ClearAll();
            _mainSimulatorProcess = string.Empty;
            _currentProfileName = null;
            _currentProfilePath = null;
            _currentPersonalProfilePath = null;
        }
        public static bool IsProfileLoaded()
        {
            return _currentProfileName != null;
        }
        public static string GetLoadedProfileName()
        {
            return _currentProfileName;
        }
        public static string CreateNewProfile(bool disablePrivateProfile)
        {
            var profileList = GetProfileList(ProfileItemPrivacyType.Public.ToString());
        loop:
            var it = new ProfileEditor();
            if (it.ShowDialog() != true)
                return null;
            var profileName = it.GetProfileName();
            var mainProcessName = it.GetMainProcessName();
            if(profileList.ContainsKey(profileName))
            {
                MessageBox.Show(LanguageManager.GetPhrase(Phrases.SettingsMessageProfileNameIsAlreadyExist),
                                LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                goto loop;
            }
            Clear();
            _currentProfileName = profileName;
            _mainSimulatorProcess = mainProcessName;
            _currentProfilePath = GenerateProfileFileName();
            _currentPersonalProfilePath = _currentProfilePath.Replace(@"\" + ProfileFolder + @"\", @"\" + ProfileAssignmentsFolder + @"\") + "p";
            _currentProfileId = GlobalId.GetNew();
            Save(disablePrivateProfile);
            return profileName;
        }
        public static string RenameProfile(bool disablePrivateProfile)
        {
            var profileList = GetProfileList(ProfileItemPrivacyType.Public.ToString());
        loop:
            var it = new InputString(LanguageManager.GetPhrase(Phrases.SettingsMessageInputProfileNewName), _currentProfileName);
            if (it.ShowDialog() != true)
                return null;
            var profileName = it.GetText();
            if (profileList.ContainsKey(profileName))
            {
                MessageBox.Show(LanguageManager.GetPhrase(Phrases.SettingsMessageProfileNameIsAlreadyExist),
                                LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                goto loop;
            }
            _currentProfileName = profileName;
            Save(disablePrivateProfile);
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
