using System;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class VariableEditorHeader : IEditor
    {
        private readonly IVariable _editableVariable;
        private readonly Phrases _variableType;
        public VariableEditorHeader(IVariable variable, Phrases variableType, string selectedItemPanelName)
        {
            _editableVariable = variable;
            _variableType = variableType;
            InitializeComponent();
            Localize();
            FillPanelsList();
            _variableName.Text = _editableVariable.Name;
            var panel = Profile.PanelStorage.GetPanelById(_editableVariable.PanelId);
            if(panel != null)
                _panel.Text = panel.Name;
            else
            {
                if (selectedItemPanelName != null)
                    _panel.Text = selectedItemPanelName;
            }
        }
        public bool IsDataChanged()
        {
            return !Utils.AreStringsEqual(_variableName.Text, _editableVariable.Name) ||
                   !Utils.AreStringsEqual(_panel.Text, Profile.PanelStorage.GetPanelById(_editableVariable.PanelId).Name);
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_variableName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName);
            if (_variableName.Text.Contains("."))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName) + " (" + LanguageManager.GetPhrase(Phrases.EditorBadSymbols) + ")";
            if (string.IsNullOrEmpty(_panel.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorPanelName);
            return new EditorFieldsErrors(emptyField);
        }
        public void Save()
        {
            _editableVariable.Name = _variableName.Text;
            _editableVariable.PanelId = Profile.PanelStorage.GetPanelByName(_panel.Text).Id;
            Profile.VariableStorage.StoreVariable(_editableVariable);
        }
        public void Localize()
        {
            _editorTypeLabel.Content = LanguageManager.GetPhrase(_variableType);
            _nameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorName);
            _panelLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPanelName);
        }
        private void PanelDropDownOpened(object sender, EventArgs e)
        {
            FillPanelsList();
        }
        private void FillPanelsList()
        {
            var currentText = _panel.Text;
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            _panel.Items.Clear();
            foreach (var p in panels)
                _panel.Items.Add(p.Name);
            if (!_panel.Items.Contains(currentText))
                _panel.Items.Add(currentText);
            _panel.Text = currentText;
        }
        private void _variableName_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = e.Text == ".";
        }
    }
}
