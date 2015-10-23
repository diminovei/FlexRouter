using System;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorCommonEditor.xaml
    /// </summary>
    public partial class DescriptorCommonEditor : UserControl, IEditor
    {
        private readonly DescriptorBase _assignedAccessDescriptor;
        /// <summary>
        /// Показать AccessDescriptorEditor
        /// </summary>
        /// <param name="ad">Ссылка на AccessDescriptor</param>
        /// <param name="selectedItemPanelName">Имя выбранной панели</param>
        public DescriptorCommonEditor(DescriptorBase ad, string selectedItemPanelName)
        {
            InitializeComponent();
            FillPanelsList();

            _assignedAccessDescriptor = ad;
            _accessDescriptorName.Text = _assignedAccessDescriptor.GetName();
            _usePanelPowerFormula.IsChecked = _assignedAccessDescriptor.GetUsePanelPowerFormulaFlag();
            _powerFormula.Text = _assignedAccessDescriptor.GetPowerFormula();

            // Если в AccessDescriptor не указана панель - берём текущую, выделенную в дереве и устанавливаем её
            // в качестве панели AccessDescriptor (для ускорения заполнения полей)
            var selectedPanel = Profile.PanelStorage.GetPanelById(_assignedAccessDescriptor.GetAssignedPanelId());
            if (selectedPanel != null)
                _assignedPanel.Text = selectedPanel.Name;
            else
            {
                if (selectedItemPanelName != null)
                    _assignedPanel.Text = selectedItemPanelName;
            }

            Localize();
        }
        private void UsePanelPowerFormulaClick(object sender, RoutedEventArgs e)
        {
//            _powerFormula.IsEnabled = _usePanelPowerFormula.IsChecked != true;
        }
        private void AssignedPanelDropDownOpened(object sender, EventArgs e)
        {
            FillPanelsList();
        }
        private void FillPanelsList()
        {
            _assignedPanel.Items.Clear();
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            foreach (var panel in panels)
                _assignedPanel.Items.Add(panel.Name);
        }
        public void Save()
        {
            GlobalFormulaKeeper.Instance.RemoveFormulasByOwnerId(_assignedAccessDescriptor.GetId());
            Profile.RegisterAccessDescriptor(_assignedAccessDescriptor);
            _assignedAccessDescriptor.SetName(_accessDescriptorName.Text);
            _assignedAccessDescriptor.SetAssignedPanelId(Profile.PanelStorage.GetPanelByName(_assignedPanel.Text).Id);
            _assignedAccessDescriptor.SetUsePanelPowerFormulaFlag(_usePanelPowerFormula.IsChecked == true);
            _assignedAccessDescriptor.SetPowerFormula(_powerFormula.Text);
        }
        public void Localize()
        {
            _editorTypeLabel.Content = _assignedAccessDescriptor.GetDescriptorType();
            _nameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorName);
            _panelLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPanelName);
            _usePanelPowerFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorUsePanelPowerFormula);
            _powerFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPowerFormula);
        }
        public bool IsDataChanged()
        {
            var panel = Profile.PanelStorage.GetPanelById(_assignedAccessDescriptor.GetAssignedPanelId());
            var panelName = panel != null ? panel.Name : null;
            
            return !Utils.AreStringsEqual(_accessDescriptorName.Text, _assignedAccessDescriptor.GetName()) ||
                   !Utils.AreStringsEqual(_assignedPanel.Text, panelName) ||
                   _usePanelPowerFormula.IsChecked != _assignedAccessDescriptor.GetUsePanelPowerFormulaFlag() ||
                   !Utils.AreStringsEqual(_powerFormula.Text, _assignedAccessDescriptor.GetPowerFormula());
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_accessDescriptorName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName);
            if(_accessDescriptorName.Text.Contains("."))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName) + " (" + LanguageManager.GetPhrase(Phrases.EditorBadSymbols) + ")";
            if (string.IsNullOrEmpty(_assignedPanel.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorPanelName);
            return new EditorFieldsErrors(emptyField);
        }
        private void _accessDescriptorName_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = e.Text ==".";
        }
    }
}
