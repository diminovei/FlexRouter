using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexRouter.ProfileItems
{
    /// <summary>
    /// Класс для хранения панелей 
    /// </summary>
    public class PanelStorage
    {
        private readonly Dictionary<Guid, Panel> _storage = new Dictionary<Guid, Panel>();

        //public delegate void OnChangeDelegate();
        //private event OnChangeDelegate OnChange;
        //public PanelStorage(OnChangeDelegate onChangeDelegate)
        //{
        //    OnChange += onChangeDelegate;
        //}
        public void StorePanel(Panel panel)
        {
            var id = panel.Id;
            _storage[id] = panel;
            //OnChange();
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

        public Panel[] GetAllPanels()
        {
            return _storage.Values.ToArray();
        }
        public void RemovePanel(Panel panel)
        {
            if (!_storage.ContainsKey(panel.Id))
                return;
            _storage.Remove(panel.Id);
            //OnChange();
        }

        public IOrderedEnumerable<Panel> GetSortedPanelsList()
        {
            return _storage.Values.OrderBy(panel => panel.Name);
        }

        public Panel GetPanelById(Guid id)
        {
            return !_storage.ContainsKey(id) ? null : _storage[id];
        }

        public Panel GetPanelByName(string name)
        {
            return (from p in _storage where p.Value.Name == name select p.Value).FirstOrDefault();
        }

        public void Clear()
        {
            _storage.Clear();
        }
    }
}
