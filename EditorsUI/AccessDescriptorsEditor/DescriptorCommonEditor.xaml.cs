using System;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
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
        /// <param name="ad">Ссылка на AccessDescriptor. Если null, значит объект только создаётся</param>
        /// <param name="selectedItemPanelName">Имя выбранной панели</param>
        public DescriptorCommonEditor(DescriptorBase ad, string selectedItemPanelName)
        {
            InitializeComponent();
            _assignedAccessDescriptor = ad;
            FillPanelsList();
            var panels = Profile.GetPanelsList();
            foreach (var panel in panels)
            {
                if (panel.Id != _assignedAccessDescriptor.GetAssignedPanelId())
                    continue;
                _assignedPanel.Text = panel.Name;
            }
            _accessDescriptorName.Text = _assignedAccessDescriptor.GetName();
            _usePanelPowerFormula.IsChecked = _assignedAccessDescriptor.GetUsePanelPowerFormulaFlag();
            _powerFormula.Text = _assignedAccessDescriptor.GetPowerFormula();

            var selectedPanel = Profile.GetPanelById(_assignedAccessDescriptor.GetAssignedPanelId());
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
            _powerFormula.IsEnabled = _usePanelPowerFormula.IsChecked != true;
        }

        private void AssignedPanelDropDownOpened(object sender, EventArgs e)
        {
            FillPanelsList();
        }
        
        private void FillPanelsList()
        {
            var panels = Profile.GetPanelsList();
            _assignedPanel.Items.Clear();
            foreach (var panel in panels)
                _assignedPanel.Items.Add(panel.Name);
        }

        public void Save()
        {
//            if(_assignedAccessDescriptor.GetId() == -1)
                Profile.RegisterAccessDescriptor(_assignedAccessDescriptor);
            _assignedAccessDescriptor.SetName(_accessDescriptorName.Text);
            _assignedAccessDescriptor.SetAssignedPanelId(Profile.GetPanelIdByName(_assignedPanel.Text));
            _assignedAccessDescriptor.SetUsePanelPowerFormulaFlag(_usePanelPowerFormula.IsChecked == true);
            _assignedAccessDescriptor.SetPowerFormula(_powerFormula.Text);
        }

        public void Localize()
        {
            _editorTypeLabel.Content = _assignedAccessDescriptor.GetDescriptorName();
            _nameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorName);
            _panelLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPanelName);
            _usePanelPowerFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorUsePanelPowerFormula);
            _powerFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPowerFormula);
        }

        public bool IsDataChanged()
        {
            var panel = Profile.GetPanelById(_assignedAccessDescriptor.GetAssignedPanelId());
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
