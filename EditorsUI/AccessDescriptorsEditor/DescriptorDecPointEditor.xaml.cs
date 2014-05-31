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
        }

        public void Save()
        {
            var digits = byte.Parse(_digitsAfterPoint.Text);
            _assignedAccessDescriptor.SetNumberOfDigitsAfterPoint(digits);
        }

        public void Localize()
        {
            _digitsAfterPointLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDigitsAfterPoint);
        }

        public bool IsDataChanged()
        {
            return byte.Parse(_digitsAfterPoint.Text)!=_assignedAccessDescriptor.GetNumberOfDigitsAfterPoint();
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
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
