using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorPanels
{
    /// <summary>
    /// Interaction logic for DescriptorRangeEditor.xaml
    /// </summary>
    public partial class DescriptorRangeEditor : UserControl, IEditor
    {
//        CultureInfo ci = new CultureInfo("en-US");

        private readonly DescriptorRange _assignedAccessDescriptor;
        public DescriptorRangeEditor(DescriptorRange assignedAccessDescriptor)
        {
            InitializeComponent();
            _assignedAccessDescriptor = assignedAccessDescriptor;
            _getValueFormula.Text = _assignedAccessDescriptor.GetReceiveValueFormula();
            _minimum.Text = _assignedAccessDescriptor.MinimumValue.ToString(CultureInfo.InvariantCulture);
            _maximum.Text = _assignedAccessDescriptor.MaximumValue.ToString(CultureInfo.InvariantCulture);
            _enableDefaultValue.IsChecked = _assignedAccessDescriptor.EnableDefaultValue;
            _defaultValue.Text = _assignedAccessDescriptor.EnableDefaultValue ? _assignedAccessDescriptor.DefaultValue.ToString(CultureInfo.InvariantCulture) : string.Empty;
            _step.Text = _assignedAccessDescriptor.Step.ToString(CultureInfo.InvariantCulture);
            _cyclic.IsChecked = _assignedAccessDescriptor.IsLooped;
            _defaultValue.IsReadOnly = !_assignedAccessDescriptor.EnableDefaultValue;
            Localize();
        }

        public void Save()
        {
            _assignedAccessDescriptor.SetReceiveValueFormula(_getValueFormula.Text);

            //Double.TryParse(_minimum.Text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _assignedAccessDescriptor.MinimumValue);
            //Double.TryParse(_maximum.Text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _assignedAccessDescriptor.MaximumValue);
            //Double.TryParse(_step.Text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _assignedAccessDescriptor.Step);

            _assignedAccessDescriptor.MinimumValue = double.Parse(_minimum.Text, CultureInfo.InvariantCulture);
            _assignedAccessDescriptor.MaximumValue = double.Parse(_maximum.Text, CultureInfo.InvariantCulture);
            _assignedAccessDescriptor.Step = double.Parse(_step.Text, CultureInfo.InvariantCulture);
            _assignedAccessDescriptor.IsLooped = _cyclic.IsChecked == true;
            _assignedAccessDescriptor.EnableDefaultValue = _enableDefaultValue.IsChecked == true;
            if (_enableDefaultValue.IsChecked == true)
                //Double.TryParse(_defaultValue.Text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _assignedAccessDescriptor.DefaultValue);
                _assignedAccessDescriptor.DefaultValue = double.Parse(_defaultValue.Text);
        }

        public void Localize()
        {
            _getValueFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeGetValueFormula);
            _minimumLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeMinimumValue);
            _maximumLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeMaximumValue);
            _stepLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeStep);
            _cyclicLabel.Content = LanguageManager.GetPhrase(Phrases.EditorLoopRange);
            _defaultValueLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeDefaultValue);
            _enableDefaultValue.Content = LanguageManager.GetPhrase(Phrases.EditorRangeUseDefaultValue);
        }

        public bool IsDataChanged()
        {
            if (!Utils.IsNumeric(_minimum.Text) || !Utils.IsNumeric(_maximum.Text) || !Utils.IsNumeric(_step.Text))
                return true;
            if (_enableDefaultValue.IsChecked == true)
            {
                if (!Utils.IsNumeric(_defaultValue.Text))
                    return true;
                if (double.Parse(_defaultValue.Text, CultureInfo.InvariantCulture) != _assignedAccessDescriptor.DefaultValue)
                    return true;
            }
            if (double.Parse(_minimum.Text, CultureInfo.InvariantCulture) != _assignedAccessDescriptor.MinimumValue)
                return true;
            if (double.Parse(_maximum.Text, CultureInfo.InvariantCulture) != _assignedAccessDescriptor.MaximumValue)
                return true;
            if (double.Parse(_step.Text, CultureInfo.InvariantCulture) != _assignedAccessDescriptor.Step)
                return true;
            if (_cyclic.IsChecked != _assignedAccessDescriptor.IsLooped)
                return true;
            if (!Utils.AreStringsEqual(_getValueFormula.Text,_assignedAccessDescriptor.GetReceiveValueFormula()))
                return true;
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (_enableDefaultValue.IsChecked == true)
            {
                if (string.IsNullOrEmpty(_defaultValue.Text) || !Utils.IsNumeric(_defaultValue.Text))
                    emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeDefaultValue);
            }
            if (string.IsNullOrEmpty(_minimum.Text) || !Utils.IsNumeric(_minimum.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeMinimumValue);
            if (string.IsNullOrEmpty(_maximum.Text) || !Utils.IsNumeric(_maximum.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeMaximumValue);
            if (string.IsNullOrEmpty(_step.Text) || !Utils.IsNumeric(_step.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeStep);
            if (string.IsNullOrEmpty(_getValueFormula.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeGetValueFormula);

            return new EditorFieldsErrors(emptyField);
        }

        private void _enableDefaultValue_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _defaultValue.IsReadOnly = false;
        }

        private void _enableDefaultValue_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _defaultValue.IsReadOnly = true;
        }

        private void _minimum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsCharNumeric(e.Text);
        }

        private void _maximum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsCharNumeric(e.Text);
        }

        private void _step_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsCharNumeric(e.Text);
        }

        private void _defaultValue_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsCharNumeric(e.Text);
        }
        private bool IsCharNumeric(string text)
        {
            string allowed = "-0123456789.";
            return allowed.Contains(text);
        }
    }
}
