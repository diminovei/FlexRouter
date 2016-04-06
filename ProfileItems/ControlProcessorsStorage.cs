using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.ProfileItems
{
    public class ControlProcessorsStorage
    {
        private const string ProfileType = "Assignments";
        private readonly Dictionary<Guid, IControlProcessor> _storage = new Dictionary<Guid, IControlProcessor>();
        /// <summary>
        /// GetControlProcessor ControlProcessorById
        /// </summary>
        /// <param name="assignedAccessDescriptorId">Associated AccessDescriptorId</param>
        /// <returns></returns>
        public IControlProcessor GetControlProcessor(Guid assignedAccessDescriptorId)
        {
            lock (_storage)
            {
                return !_storage.ContainsKey(assignedAccessDescriptorId) ? null : _storage[assignedAccessDescriptorId];
            }
        }
        public void RemoveControlProcessor(Guid associatedAccessDescriptorId)
        {
            lock (_storage)
            {
                if (!_storage.ContainsKey(associatedAccessDescriptorId))
                    return;
                _storage.Remove(associatedAccessDescriptorId);
            }
        }
        public Guid AddControlProcessor(IControlProcessor cp, Guid associatedAccessDescriptorId)
        {
            lock (_storage)
            {
                var id = associatedAccessDescriptorId;
                _storage.Add(id, cp);
                return id;
            }
        }
        public void SendEvent(ControlEventBase controlEvent)
        {
            lock (_storage)
            {
                foreach (var cp in _storage)
                {
                    if (cp.Value is IVisualizer)
                        continue;
                    ((ICollector)cp.Value).ProcessControlEvent(controlEvent);
                }
            }
        }
        public void Tick()
        {
            lock (_storage)
            {
                foreach (var cp in _storage)
                {
                    //var value = cp.Value as IRepeater;
                    var value = cp.Value as ICollector;
                    if (value != null)
                        value.Tick();
                }
            }
        }
        public ControlProcessorHardware[] GetAllAssignedHardwares()
        {
            var modulesString = new List<string>();
            var modules = new List<ControlProcessorHardware>();
            foreach (var controlProcessor in _storage.Values)
            {
                var a = controlProcessor.GetUsedHardwareList().Distinct();
                foreach (var assignment in a)
                {
                    if (string.IsNullOrEmpty(assignment))
                        continue;
                    
                    var cph = ControlProcessorHardware.GenerateByGuid(assignment);
                    if (cph == null)
                        continue;
                    
                    if (cph.ModuleType != HardwareModuleType.Button) 
                        continue;
                    
                    var module = cph.MotherBoardId + "|" + cph.ModuleType + "|" + cph.ModuleId;
                    
                    if (modulesString.Contains(module)) 
                        continue;
                    
                    modules.Add(cph);
                    modulesString.Add(module);
                }
            }
            return modules.ToArray();
        }
        public IEnumerable<ControlEventBase> GetNewEvents()
        {
            return GetEvents(false);
        }
        private IEnumerable<ControlEventBase> GetEvents(bool isForShutDown)
        {
            lock (_storage)
            {
                var evs = new List<ControlEventBase>();
                foreach (var cps in _storage)
                {
                    if (!(cps.Value is IVisualizer))
                        continue;
                    var range = isForShutDown ? (cps.Value as IVisualizer).GetClearEvent() : (cps.Value as IVisualizer).GetNewEvent();
                    if (range == null)
                        continue;
                    evs.AddRange(range);
                }
                return evs;
            }
        }
        public IEnumerable<ControlEventBase> GetShutDownEventsForAllControlProcessors()
        {
            return GetEvents(true);
        }
        public List<ControlEventBase> GetShutDownEvents(Guid assignedAccessDescriptorId)
        {
            var shutDownEvents = new List<ControlEventBase>();
            lock (_storage)
            {
                foreach (var cp in _storage)
                {
                    if(!(cp.Value is IVisualizer))
                        continue;
                    if(cp.Value.GetAssignedAccessDescriptorId() != assignedAccessDescriptorId)
                        continue;
                    shutDownEvents.AddRange((cp.Value as IVisualizer).GetClearEvent());
                }
            }
            return shutDownEvents;
        }
        public void UpdateAssignments()
        {
            foreach (var cp in _storage)
            {
                cp.Value.OnAssignmentsChanged();
            }
        }
        public void Clear()
        {
            lock (_storage)
            {
                _storage.Clear();
            }
        }
        public void Save(string profilePath, string profileMainNodeName, Guid profileId)
        {
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;
                    writer.WriteStartDocument();
                    writer.WriteStartElement(profileMainNodeName);
                    writer.WriteAttributeString("Type", ProfileType);
                    writer.WriteAttributeString("Id", profileId.ToString());

                    writer.WriteStartElement("ControlProcessors");
                    foreach (var cp in _storage)
                    {
                        writer.WriteStartElement("ControlProcessor");
                        cp.Value.Save(writer);
                        writer.WriteEndElement();
                    }
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
        /// <summary>
        /// Загрузить ControlProcessor's
        /// </summary>
        public void Load(string profilePath, string profileMainNodeName, IEnumerable<DescriptorBase> accessDescriptors)
        {
            if (!File.Exists(profilePath))
                return;
            // ToDo: удалить
            //            GlobalId.Save();

            var xp = new XPathDocument(profilePath);
            var nav = xp.CreateNavigator();
            var navPointer = nav.Select("/" + profileMainNodeName + "/ControlProcessors/ControlProcessor");
            var cpLoadErrorsCounter = 0;
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);

                Guid id;
                if (!Guid.TryParse(navPointer.Current.GetAttribute("AssignedAccessDescriptorId", navPointer.Current.NamespaceURI), out id))
                {
                    // ToDo: удалить
                    id = GlobalId.GetByOldId(ObjType.AccessDescriptor, int.Parse(navPointer.Current.GetAttribute("AssignedAccessDescriptorId", navPointer.Current.NamespaceURI)));
                }
                var accessDescriptor = accessDescriptors.FirstOrDefault(x => x.GetId() == id);
                if (accessDescriptor == null)
                {
                    cpLoadErrorsCounter++;
                    continue;
                }

                Object[] args = { accessDescriptor };
                var cp = Utils.FindAndCreate<IControlProcessor>(type, args);

                if (cp != null)
                {
                    cp.Load(navPointer.Current);
                    var cpId = AddControlProcessor(cp, id);
                    //var ad = accessDescriptor as DescriptorMultistateBase;
                    //if (ad == null) 
                    //    continue;

                    var controlProcessor = GetControlProcessor(cpId);
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
    }
}
