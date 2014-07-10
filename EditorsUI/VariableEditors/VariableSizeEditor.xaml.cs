using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class VariableSizeEditor : IEditor
    {
        private readonly IMemoryVariable _editableVariable;
        public VariableSizeEditor(IMemoryVariable variable)
        {
            _editableVariable = variable;
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
            _variableSize.Text = VariableSizeLocalizer.SizeBySizeType(_editableVariable.GetVariableSize());
        }

        public bool IsDataChanged()
        {
            if (_editableVariable == null)
                return true;
            return !Utils.AreStringsEqual(_variableSize.Text, VariableSizeLocalizer.SizeBySizeType(_editableVariable.GetVariableSize()));
        }

        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_variableSize.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorVariableSize);
            return new EditorFieldsErrors(emptyField);
        }

        public void Save()
        {
            _editableVariable.SetVariableSize(VariableSizeLocalizer.SizeTypeByText(_variableSize.Text));
        }

        public void Localize()
        {
            VariableSizeLocalizer.Initialize();
            VariableSizeLocalizer.LocalizeSizes(ref _variableSize);
            _variableSizeLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableSize);
        }
    }
}
