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
        private readonly Dictionary<Guid, Panel> _panelsStorage = new Dictionary<Guid, Panel>();

        //public delegate void OnChangeDelegate();
        //private event OnChangeDelegate OnChange;
        //public PanelStorage(OnChangeDelegate onChangeDelegate)
        //{
        //    OnChange += onChangeDelegate;
        //}
        public void StorePanel(Panel panel)
        {
            var id = panel.Id;
            _panelsStorage[id] = panel;
            //OnChange();
        }

        public Panel[] GetAllPanels()
        {
            return _panelsStorage.Values.ToArray();
        }
        public void RemovePanel(Panel panel)
        {
            if (!_panelsStorage.ContainsKey(panel.Id))
                return;
            _panelsStorage.Remove(panel.Id);
            //OnChange();
        }

        public IOrderedEnumerable<Panel> GetSortedPanelsList()
        {
            return _panelsStorage.Values.OrderBy(panel => panel.Name);
        }

        public Panel GetPanelById(Guid id)
        {
            return !_panelsStorage.ContainsKey(id) ? null : _panelsStorage[id];
        }

        public Panel GetPanelByName(string name)
        {
            return (from p in _panelsStorage where p.Value.Name == name select p.Value).FirstOrDefault();
        }

        public void Clear()
        {
            _panelsStorage.Clear();
        }
    }
}
