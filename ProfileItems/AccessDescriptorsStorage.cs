using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.Helpers;

namespace FlexRouter.ProfileItems
{
    public class AccessDescriptorsStorage
    {
        private readonly ControlProcessorsStorage _controlProcessorsStorage;
        public AccessDescriptorsStorage(ControlProcessorsStorage controlProcessorsStorage)
        {
            _controlProcessorsStorage = controlProcessorsStorage;
        }
        private readonly Dictionary<Guid, DescriptorBase> _storage = new Dictionary<Guid, DescriptorBase>();
        public Guid RegisterAccessDescriptor(DescriptorBase ad)
        {
            _storage[ad.GetId()] = ad;
            return ad.GetId();
        }
        public void InitializeAccessDescriptors()
        {
            lock (_storage)
            {
                foreach (var ad in _storage)
                {
                    ad.Value.Initialize();
                }
            }
        }
        public DescriptorBase GetAccessDesciptorById(Guid id)
        {
            if (!_storage.ContainsKey(id))
                return null;
            return _storage[id];
        }
        public IOrderedEnumerable<DescriptorBase> GetSortedAccessDesciptorListByPanelId(Guid panelId)
        {
            return _storage.Values.Where(ad => ad.GetAssignedPanelId() == panelId).OrderBy(ad => ad.GetName());
        }
        public IOrderedEnumerable<DescriptorBase> GetSortedAccessDesciptorList()
        {
            return _storage.Values.OrderBy(ad => ad.GetName());
        }
        public void RemoveAccessDescriptor(Guid accessDescriptorId)
        {
            if (!_storage.ContainsKey(accessDescriptorId))
                return;
            _storage.Remove(accessDescriptorId);
        }

        public void MakeAllItemsPublic()
        {
            lock (_storage)
            {
                foreach (var item in _storage)
                {
                    item.Value.SetPrivacyType(ProfileItemPrivacyType.Public);
                }
            }
        }
        public DescriptorBase[] GetAll()
        {
            return _storage.Values.ToArray();
        }
        public void Clear()
        {
            lock (_storage)
            {
                _storage.Clear();
            }
        }
        /// <summary>
        /// Есть ли у панели дочерние элементы
        /// </summary>
        /// <param name="panelId"></param>
        /// <returns></returns>
        public bool IsPanelInUse(Guid panelId)
        {
            return _storage.Any(descriptorBase => descriptorBase.Value.GetAssignedPanelId() == panelId);
        }

        public void Save(XmlTextWriter writer, ProfileItemPrivacyType profileItemPrivacyType, bool disablePrivateProfile)
        {
            writer.WriteStartElement("AccessDescriptors");
            foreach (var ads in _storage)
            {
                if (ads.Value.GetPrivacyType() == profileItemPrivacyType || disablePrivateProfile)
                {
                    writer.WriteStartElement("AccessDescriptor");
                    ads.Value.Save(writer);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        public void Load(XPathNavigator nav, string profileMainNodeName, ProfileItemPrivacyType profileItemPrivacyType)
        {
            var navPointer = nav.Select("/" + profileMainNodeName + "/AccessDescriptors/AccessDescriptor");
            while (navPointer.MoveNext())
            {
                var type = navPointer.Current.GetAttribute("Type", navPointer.Current.NamespaceURI);
                var ad = Utils.FindAndCreate<DescriptorBase>(type);

                if (ad != null)
                {
                    ad.Load(navPointer.Current);
                    ad.SetPrivacyType(profileItemPrivacyType);
                    RegisterAccessDescriptor(ad);
                }
            }
            
        }
    }
}
