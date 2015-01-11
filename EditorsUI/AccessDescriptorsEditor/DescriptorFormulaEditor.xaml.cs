using System.Windows.Controls;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorFormulaEditor.xaml
    /// </summary>
    public partial class DescriptorFormulaEditor : UserControl, IEditor
    {
        private readonly DescriptorOutputBase _assignedAccessDescriptor;
        public DescriptorFormulaEditor(DescriptorOutputBase assignedAccessDescriptor)
        {
            InitializeComponent();
            Localize();
            _assignedAccessDescriptor = assignedAccessDescriptor;
            _formula.Text = _assignedAccessDescriptor.GetFormula();
        }

        public void Save()
        {
            _assignedAccessDescriptor.SetFormula(_formula.Text);
        }

        public void Localize()
        {
            _formulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorOutputFormula);
        }

        public bool IsDataChanged()
        {
            return !Utils.AreStringsEqual(_assignedAccessDescriptor.GetFormula(), _formula.Text);
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            // ToDo: проверить формулу
            return new EditorFieldsErrors(emptyField);
        }
    }
}
