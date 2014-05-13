using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;
using FlexRouter.VariableSynchronization;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
using SlimDX.Direct3D11;

namespace FlexRouter
{
    internal static class Profile
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

        public static int RegisterAccessDescriptor(DescriptorBase ad, bool isNew)
        {
 /*           int id;
            if (isNew)
            {
                id = GlobalId.GetNew();
                ad.SetId(id);
            }
            else
                id = ad.GetId();
                        AccessDescriptorsStorage.Add(id, ad);
            return id;*/


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
//            return (from item in _storageItems where item.Value.AccessDescriptor.GetAssignedPanelId() == panelId select item.Value.AccessDescriptor).OrderBy(ad => ad.GetId());
            return
                AccessDescriptorsStorage.Values.Where(ad => ad.GetAssignedPanelId() == panelId)
                                        .OrderBy(ad => ad.GetName());
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

        public static ControlEventBase[] GetControlProcessorsNewEvents()
        {
            lock (ControlProcessorsStorage)
            {
                return
                    ControlProcessorsStorage.Values.Where(cp => cp is IVisualizer)
                        .Select(cp => ((IVisualizer) cp).GetNewEvent())
                        .Where(ev => ev != null)
                        .ToArray();
            }
        }

        public static ControlEventBase[] GetControlProcessorsClearEvents()
        {
            lock (ControlProcessorsStorage)
            {
                return
                    ControlProcessorsStorage.Values.Where(cp => cp is IVisualizer)
                        .Select(cp => ((IVisualizer) cp).GetClearEvent())
                        .Where(ev => ev != null)
                        .ToArray();
            }
        }

        public static void Load()
        {
            _moduleExtensionFilter = ".gau";
            _mainSimulatorProcess = "fs9";

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var folder = Path.Combine(location, @"Profiles\profile001.ap");

            LoadProfile(folder);
//            TestInit();
        }

        private static string GenerateProfileFileName()
        {
            var folder = Utils.GetFullSubfolderPath("Profiles");
            var index = 1;
            while (true)
            {
                var fileName = "profile" + index.ToString("000") + ".ap";
                fileName = Path.Combine(folder, fileName);
                if (_profileList.All(pair => pair.Value.ToLower() != fileName.ToLower()))
                    return fileName;
                index++;
            }
        }

        private static Dictionary<string, string> _profileList = new Dictionary<string, string>();
        //ToDo: временно
        private const string ProfileName = "First";
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
                var a = controlProcessor.GetAssignments();
                foreach (var assignment in a)
                {
                    if(string.IsNullOrEmpty(assignment.AssignedItem))
                        continue;
                    var cph = ControlProcessorHardware.GenerateByGuid(assignment.AssignedItem);
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
        public static string[] GetProfileList()
        {
            _profileList = Utils.GetXmlList(ProfileFolder, ProfileExtensionMask, ProfileHeader, ProfileType);
            return _profileList.Keys.ToArray();
        }

/*        private static void SaveSerialized()
        {
        
            using (var x = new XmlSerializer(typeof (YourClass)))
            {
                var fs = new FileStream(@"C:\YourFile.xml"), FileMode.OpenOrCreate);
                x.Serialize(fs, yourInstance);
                fs.Close();
            }

        private static void LoadSerialized()
        {

            var x = new XmlSerializer(typeof(YourClass));
            var fs = new FileStream(@"C:\YourFile.xml"), FileMode.Open);
            var fromFile = x.Deserialize(fs) as YourClass;
            fs.Close();
        }*/
        public static void SaveProfile()
        {
            try
            {
                if (!_profileList.ContainsKey(ProfileName))
                    _profileList.Add(ProfileName, GenerateProfileFileName());
 
                using (var sw = new StringWriter())
                {
                    using (var writer = new XmlTextWriter(sw /*ProfileList[profileName], Encoding.Unicode*/))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;
                        writer.WriteStartDocument();
                            writer.WriteStartElement(ProfileHeader);
                            writer.WriteAttributeString("Type", ProfileType);
                            writer.WriteAttributeString("Name", ProfileName);
//                            writer.WriteString("\n");
                                writer.WriteStartElement(ProfileType);

                                    // Panels
                                    //writer.WriteString("\n");
                                    writer.WriteStartElement("Panels");
                                    //writer.WriteString("\n");
                                    foreach (var panel in PanelsStorage)
                                        panel.Value.Save(writer);                            
                                    writer.WriteEndElement();
                                    //writer.WriteString("\n");
                                    
                                    // Variables
                                    writer.WriteStartElement("Variables");
                                    //writer.WriteString("\n");
                                    VariableManager.Save(writer);
                                    writer.WriteEndElement();
                                    //writer.WriteString("\n");

                                    // AccessDescriptors
                                    writer.WriteStartElement("AccessDescriptors");
                                    //writer.WriteString("\n");
                                    foreach (var ads in AccessDescriptorsStorage)
                                    {
                                        writer.WriteStartElement("AccessDescriptor");
                                        ads.Value.Save(writer);
                                        writer.WriteEndElement();
                                        //writer.WriteString("\n");
                                    }
                                    writer.WriteEndElement();
                                    //writer.WriteString("\n");

                                    // ControlProcessors
                                    writer.WriteStartElement("ControlProcessors");
                                    //writer.WriteString("\n");
                                    foreach (var cp in ControlProcessorsStorage)
                                    {
                                        writer.WriteStartElement("ControlProcessor");
                                        cp.Value.Save(writer);
                                        writer.WriteEndElement();
                                        //writer.WriteString("\n");
                                    }
                                    writer.WriteEndElement();
                                    //writer.WriteString("\n");

                                writer.WriteEndElement();

                        
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    if (File.Exists(_profileList[ProfileName]))
                        File.Copy(_profileList[ProfileName], _profileList[ProfileName] + ".bak", true);
                    using (var swToDisk = new StreamWriter(_profileList[ProfileName], false, Encoding.Unicode))
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
        /// <summary>
        /// Загрузка профиля
        /// </summary>
        /// <param name="profileName">Имя профиля, прописанное в теге ArccRouterProfile, аттрибут Name</param>
        /// <returns>Успешно ли прошла загрузка</returns>
        public static bool LoadProfile(string profileName)
        {
            ControlProcessorsStorage.Clear();
            AccessDescriptorsStorage.Clear();
            // Очистить VariableStorage
            var xp = new XPathDocument(profileName);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/FlexRouterProfile/Aircraft/Panels/Panel");

            while (navPointer.MoveNext())
            {
                var panel = new Panel();
                panel.Load(navPointer.Current);
                PanelsStorage.Add(panel.Id, panel);
            }

            
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
                DescriptorBase ad = null;
                if (type == "DescriptorBinaryOutput")
                    ad = new DescriptorBinaryOutput();
                if (type == "DescriptorIndicator")
                    ad = new DescriptorIndicator();
                if (type == "DescriptorRange")
                    ad = new DescriptorRange();
                if (type == "DescriptorValue")
                    ad = new DescriptorValue();
                if (type == "RangeUnion")
                    ad = new RangeUnion();
                if (ad != null)
                {
                    ad.Load(navPointer.Current);
                    RegisterAccessDescriptor(ad, false);
                }
            }

            navPointer = nav.Select("/FlexRouterProfile/Aircraft/ControlProcessors/ControlProcessor");
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                var id = int.Parse(navPointer.Current.GetAttribute("AssignedAccessDescriptorId", navPointer.Current.NamespaceURI));
                var accessDescriptor = GetAccessDesciptorById(id);
                IControlProcessor cp = null;
                if (type == "AxisRangeProcessor")
                    cp = new AxisRangeProcessor(accessDescriptor);
                if (type == "LampProcessor")
                    cp = new LampProcessor(accessDescriptor);
                if (type == "IndicatorProcessor")
                    cp = new IndicatorProcessor(accessDescriptor);
                if (type == "EncoderProcessor")
                    cp = new EncoderProcessor(accessDescriptor);
                if (type == "ButtonProcessor")
                    cp = new ButtonProcessor(accessDescriptor);
                if (type == "ButtonPlusMinusProcessor")
                    cp = new ButtonPlusMinusProcessor(accessDescriptor);
                if (type == "ButtonBinaryInputProcessor")
                    cp = new ButtonBinaryInputProcessor(accessDescriptor);
                if (cp != null)
                {
                    cp.Load(navPointer.Current);
                    var cpId = RegisterControlProcessor((IControlProcessor)cp, id);
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
/*            lock (ControlProcessorsStorage)
            {
                lock (AccessDescriptorsStorage)
                {
                    for (int i = AccessDescriptorsStorage.Count-1; i >= 0; i--)
                    {
                        var newId = GlobalId.GetNew();
                        for (int j = ControlProcessorsStorage.Count - 1; j >= 0; j--)
                        {
                            if (ControlProcessorsStorage.ElementAt(j).Value.GetAssignedAccessDescriptor() != AccessDescriptorsStorage.ElementAt(i).Value.GetId())
                                continue;
                            var cp = ControlProcessorsStorage.ElementAt(j);
                            ControlProcessorsStorage.Remove(ControlProcessorsStorage.ElementAt(j).Key);
                            ControlProcessorsStorage.Add(newId, cp.Value);
                            ControlProcessorsStorage[newId].SetId(newId);

                        }
                        var add2 = AccessDescriptorsStorage.ElementAt(i);
                        AccessDescriptorsStorage.Remove(AccessDescriptorsStorage.ElementAt(i).Key);
                        AccessDescriptorsStorage.Add(newId, add2.Value);
                        AccessDescriptorsStorage[newId].SetId(newId);
                    }
                }
            }*/
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
    }
}
