using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableSynchronization;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class VariableEditorDescription : IEditor
    {
        private readonly IVariable _editableVariable;
        public VariableEditorDescription(IVariable variable)
        {
            _editableVariable = variable;
            InitializeComponent();
            Localize();
            _description.Text = _editableVariable.Description;
        }
        public bool IsDataChanged()
        {
            if (_editableVariable == null)
                return true;
            return !Utils.AreStringsEqual(_description.Text, _editableVariable.Description);
        }

        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        public void Save()
        {
            _editableVariable.Description = _description.Text;
        }

        public void Localize()
        {
            _descriptionLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDescription);
        }
    }
}
