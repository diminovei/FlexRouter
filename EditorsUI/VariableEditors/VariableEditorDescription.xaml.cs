using System;
using System.Windows;
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
            var sameVarNames = Profile.GetSameVariablesNames(_editableVariable);
            if (sameVarNames != null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageTheSameVariableIsExist) + ": " + Environment.NewLine + Environment.NewLine + sameVarNames;
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                // Bug: потенциальная бага. Удаление держится только на том, что это будет последний контрол в панели. Иначе сохранение данных в уже удалённую переменную продолжится
                Profile.VariableStorage.RemoveVariable(_editableVariable.Id);
            }
        }

        public void Localize()
        {
            _descriptionLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDescription);
        }
    }
}
