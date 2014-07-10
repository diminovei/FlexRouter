using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class MemoryPatchVariableEditor : IEditor
    {
        private readonly MemoryPatchVariable _editableVariable;
        public MemoryPatchVariableEditor(IVariable variable)
        {
            _editableVariable = variable as MemoryPatchVariable;
            Initialize();
            GetDataFromVariable();
        }
        private void Initialize()
        {
            InitializeComponent();
            Localize();
            FillModulesList();
        }
        private void GetDataFromVariable()
        {
            if (!_moduleName.Items.Contains(_editableVariable.ModuleName))
                _moduleName.Items.Add(_editableVariable.ModuleName);
            _moduleName.Text = _editableVariable.ModuleName;
            _relativeOffset.Text = _editableVariable.Offset.ToString("X");
        }

        public bool IsDataChanged()
        {
            if (_editableVariable == null)
                return true;
            return !Utils.AreStringsEqual(_moduleName.Text, _editableVariable.ModuleName) ||
                   !Utils.AreStringsEqual(_relativeOffset.Text, _editableVariable.Offset.ToString("X"));
        }

        /// <summary>
        /// Корректно ли заполнены поля в форме редактора
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_relativeOffset.Text) || !Utils.IsHexNumber(_relativeOffset.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorVariableRelativeOffset);
            if (string.IsNullOrEmpty(_moduleName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorVariableModule);
            return new EditorFieldsErrors(emptyField);
        }

        public void Save()
        {
            _editableVariable.ModuleName = _moduleName.Text;
            _editableVariable.Offset = uint.Parse(_relativeOffset.Text, NumberStyles.HexNumber);
            VariableManager.RegisterVariable(_editableVariable, false);
        }

        public void Localize()
        {
            _absoluteOffsetLabel.Content = LanguageManager.GetPhrase(Phrases.EditorAbsoluteOffset);
            _convertOffset.Content = LanguageManager.GetPhrase(Phrases.EditorConvertAbsoluteOffsetToRelative);
            _variableModuleLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableModule);
            _offsetLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableRelativeOffset);
        }

        private void ConvertOffsetClick(object sender, RoutedEventArgs e)
        {
            uint offset;
            if (!uint.TryParse(_absoluteOffset.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out offset))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageAbsoluteIsNotAhexNumber);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var result = VariableManager.ConvertAbsoleteOffsetToRelative(offset);
            if(result == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageAbsoluteOffsetIsOutOfModule);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            if (!_moduleName.Items.Contains(result.Value.ModuleName))
                _moduleName.Items.Add(result.Value.ModuleName);
            _moduleName.Text = result.Value.ModuleName;
            _relativeOffset.Text = result.Value.Offset.ToString("X");
        }

        private void ModuleNameDropDownOpened(object sender, EventArgs e)
        {
            FillModulesList();
        }
        
        private void FillModulesList()
        {
            var currentText = _moduleName.Text;
            var modules = VariableManager.GetModulesList().OrderBy(x => x);
            _moduleName.Items.Clear();
            foreach (var m in modules)
                _moduleName.Items.Add(m);
            if (!_moduleName.Items.Contains(currentText))
                _moduleName.Items.Add(currentText);
            _moduleName.Text = currentText;
        }
    }
}
