using System.Collections.Generic;
using System.Linq;
using FlexRouter.ProfileItems;

namespace FlexRouter
{
    public class PanelStorage
    {
        private readonly Dictionary<int, Panel> _panelsStorage = new Dictionary<int, Panel>();

        public void RegisterOrUpdaterPanel(Panel panel)
        {
            var id = panel.Id;
            _panelsStorage[id] = panel;
        }

        public void RemovePanel(int panelId)
        {
            if (!_panelsStorage.ContainsKey(panelId))
                return;

            _panelsStorage.Remove(panelId);
        }

        public IOrderedEnumerable<Panel> GetPanelsList()
        {
            return _panelsStorage.Values.OrderBy(panel => panel.Name);
        }

        public Panel GetPanelById(int id)
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
