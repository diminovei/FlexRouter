using System;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.ProfileItems;

namespace FlexRouter.EditorsUI.Dialogues
{
    public enum SelectedType
    {
        Variable,
        AccessDescriptor
    }
    /// <summary>
    /// Interaction logic for VariableSelector.xaml
    /// </summary>
    public partial class Selector
    {
        private Guid _selectedItemId = Guid.Empty;
        private readonly Guid _selectedPanelId = Guid.Empty;
        private readonly SelectedType _selectedType;
        public Selector(SelectedType selectedType)
        {
            InitializeComponent();
            _selectedType = selectedType;
            ShowTree(selectedType);
        }
        public Selector(SelectedType selectedType, string panelName)
        {
            InitializeComponent();
            _selectedPanelId = Profile.PanelStorage.GetPanelByName(panelName).Id;
            _selectedType = selectedType;
            ShowTree(selectedType);
        }

        private void ShowTree(SelectedType selectedType)
        {
            if (selectedType == SelectedType.Variable)
                ShowVariableTree();
            if (selectedType == SelectedType.AccessDescriptor)
                ShowAccessDecsriptorTree();

        }
        private void ShowVariableTree()
        {
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            _tree.Items.Clear();
            foreach (var panel in panels)
            {
                var treeRootItem = new TreeViewItem { Tag = panel.Id, Name = TreeItemType.Panel.ToString(), Header = panel.Name };

                var ad = Profile.GetSortedVariablesListByPanelId(panel.Id);
                foreach (var adesc in ad)
                {
                    var treeAdItem = new TreeViewItem { Tag = adesc.Id, Name = TreeItemType.AccessDescriptor.ToString(), Header = adesc.Name};
                    treeRootItem.Items.Add(treeAdItem);
                }
                _tree.Items.Add(treeRootItem);
            }
        }
        private void ShowAccessDecsriptorTree()
        {
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            _tree.Items.Clear();
//            var adAll = Profile.GetSortedAccessDesciptorList();
            foreach (var panel in panels)
            {
                if(_selectedPanelId!=Guid.Empty && panel.Id != _selectedPanelId)
                    continue;
                var treeRootItem = new TreeViewItem { Tag = panel.Id, Name = TreeItemType.Panel.ToString(), Header = panel.Name };

                var ad = Profile.GetSortedAccessDesciptorListByPanelId(panel.Id);
                /*foreach (var adesc in ad)
                {
                    if (adesc.IsDependent())
                        continue;
                    var treeAdItem = new TreeViewItem { Tag = adesc, Name = TreeItemType.AccessDescriptor.ToString(), Header = adesc.GetName() };
                    treeRootItem.Items.Add(treeAdItem);
                    foreach (var a in adAll)
                    {
                        if (!a.IsDependent())
                            continue;
                        if (a.GetDependency().GetId() != adesc.GetId())
                            continue;
                        var treeDependentItem = new TreeViewItem { Tag = a, Name = TreeItemType.AccessDescriptor.ToString(), Header = Profile.GetPanelById(a.GetAssignedPanelId()).Name + "." + a.GetName() };
                        treeAdItem.Items.Add(treeDependentItem);
                    }
                }
                _tree.Items.Add(treeRootItem);*/

                 
                foreach (var adesc in ad)
                {
                    var treeAdItem = new TreeViewItem { Tag = adesc.GetId(), Name = TreeItemType.AccessDescriptor.ToString(), Header = adesc.GetName() };
                    treeRootItem.Items.Add(treeAdItem);
                }
                _tree.Items.Add(treeRootItem);
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            _selectedItemId = Guid.Empty;
            Close();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Возвращает id выбранной переменной
        /// </summary>
        /// <returns>id выбранной переменной. -1, если ничего не выбрано</returns>
        public Guid GetSelectedItemId()
        {
            return _selectedItemId;
        }

        private void VariablesTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
            if (_tree.SelectedItem == null)
            {
                _selectedItemId = Guid.Empty;
                return;
            }
                

            if (((TreeViewItem)_tree.SelectedItem).Name == TreeItemType.Panel.ToString())
            {
                _description.Text = string.Empty;
                _selectedItemId = Guid.Empty;
                return;
            }
            if (_selectedType == SelectedType.Variable)
            {
                var selectedVariable = Profile.VariableStorage.GetVariableById((Guid)((TreeViewItem)_tree.SelectedItem).Tag);
                _description.Text = selectedVariable.Description;
                _selectedItemId = selectedVariable.Id;
            }
            if (_selectedType == SelectedType.AccessDescriptor)
            {
                var selectedDescriptor = Profile.GetAccessDesciptorById((Guid)((TreeViewItem)_tree.SelectedItem).Tag);
                _description.Text = string.Empty;
                _selectedItemId = selectedDescriptor.GetId();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
                OkClick(null, null);
            }
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                CancelClick(null, null);
            }

        }
    }
}
