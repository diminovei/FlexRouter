using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorFormulaEditor.xaml
    /// </summary>
    public partial class DescriptorDecPointEditor : IEditor
    {
        private readonly DescriptorIndicator _assignedAccessDescriptor;
        public DescriptorDecPointEditor(DescriptorIndicator assignedAccessDescriptor)
        {
            InitializeComponent();
            Localize();
            _assignedAccessDescriptor = assignedAccessDescriptor;
            _digitsAfterPoint.Text = _assignedAccessDescriptor.GetNumberOfDigitsAfterPoint().ToString(CultureInfo.InvariantCulture);
            _totalDigits.Text = _assignedAccessDescriptor.GetNumberOfDigits().ToString(CultureInfo.InvariantCulture);
        }

        public void Save()
        {
            var digits = byte.Parse(_digitsAfterPoint.Text);
            _assignedAccessDescriptor.SetNumberOfDigitsAfterPoint(digits);
            
            var totalDigits = byte.Parse(_totalDigits.Text);
            _assignedAccessDescriptor.SetNumberOfDigits(totalDigits);
        }

        public void Localize()
        {
            _digitsAfterPointLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDigitsAfterPoint);
            _totalDigitsLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDigitsTotalNumber);
        }

        public bool IsDataChanged()
        {
            return byte.Parse(_digitsAfterPoint.Text)!=_assignedAccessDescriptor.GetNumberOfDigitsAfterPoint()
                || byte.Parse(_totalDigits.Text) != _assignedAccessDescriptor.GetNumberOfDigits();
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_totalDigits.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorDigitsTotalNumber);
            if (string.IsNullOrEmpty(_digitsAfterPoint.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorDigitsAfterPoint);

            return new EditorFieldsErrors(emptyField);
        }

        private void DigitsAfterPointPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 2;
        }

        private bool IsNumeric(string text)
        {
            var regex = new Regex("^[0-9]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }
    }
}
