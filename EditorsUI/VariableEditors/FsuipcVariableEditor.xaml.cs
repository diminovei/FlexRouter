using System.Globalization;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class FsuipcVariableEditor : IEditor
    {
        private readonly FsuipcVariable _editableVariable;
        public FsuipcVariableEditor(IVariable variable)
        {
            _editableVariable = variable as FsuipcVariable;
            Initialize();
            GetDataFromVariable();
        }
        private void Initialize()
        {
            InitializeComponent();
            Localize();
        }
        
        private void GetDataFromVariable()
        {
            _offset.Text = _editableVariable.Offset.ToString("X");
            _variableSize.Text = VariableSizeLocalizer.SizeBySizeType(_editableVariable.Size);
        }

        public bool IsDataChanged()
        {
            if (_editableVariable == null)
                return true;
            return !Utils.AreStringsEqual(_offset.Text, _editableVariable.Offset.ToString("X")) || 
                    !Utils.AreStringsEqual(_variableSize.Text, VariableSizeLocalizer.SizeBySizeType(_editableVariable.Size));
        }

        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_offset.Text) || !Utils.IsHexNumber(_offset.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorVariableRelativeOffset);
            if (string.IsNullOrEmpty(_variableSize.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorVariableSize);
            return new EditorFieldsErrors(emptyField);
        }

        public void Save()
        {
            _editableVariable.Offset = int.Parse(_offset.Text, NumberStyles.HexNumber);
            _editableVariable.Size = VariableSizeLocalizer.SizeTypeByText(_variableSize.Text);
            VariableManager.RegisterVariable(_editableVariable, false);
        }

        public void Localize()
        {
            VariableSizeLocalizer.LocalizeSizes(ref _variableSize);
            _offsetLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableRelativeOffset);
            _variableSizeLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableSize);
        }
    }
}
