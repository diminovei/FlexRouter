using System.Globalization;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Helpers;
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
        }
        public bool IsDataChanged()
        {
            return !Utils.AreStringsEqual(_offset.Text, _editableVariable.Offset.ToString("X"));
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
            return new EditorFieldsErrors(emptyField);
        }
        public void Save()
        {
            _editableVariable.Offset = int.Parse(_offset.Text, NumberStyles.HexNumber);
        }
        public void Localize()
        {
            _offsetLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableRelativeOffset);
        }
    }
}
