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
            _minimum.Text = _assignedAccessDescriptor.GetMinimumValueFormula();
            _maximum.Text = _assignedAccessDescriptor.GetMaximumValueFormula();
            _enableDefaultValue.IsChecked = _assignedAccessDescriptor.EnableDefaultValue;
            _defaultValue.Text = _assignedAccessDescriptor.EnableDefaultValue ? _assignedAccessDescriptor.GetDefaultValueFormula() : string.Empty;
            _step.Text = _assignedAccessDescriptor.GetStepFormula();
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

            _assignedAccessDescriptor.SetMinimumValueFormula(_minimum.Text);
            _assignedAccessDescriptor.SetMaximumValueFormula(_maximum.Text);
            _assignedAccessDescriptor.SetStepFormula(_step.Text);
            _assignedAccessDescriptor.IsLooped = _cyclic.IsChecked == true;
            _assignedAccessDescriptor.EnableDefaultValue = _enableDefaultValue.IsChecked == true;
            if (_enableDefaultValue.IsChecked == true)
                //Double.TryParse(_defaultValue.Text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _assignedAccessDescriptor.DefaultValue);
                _assignedAccessDescriptor.SetDefaultValueFormula(_defaultValue.Text);
        }

        public void Localize()
        {
            _getValueFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeGetValueFormula);
            _minimumLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeMinimumValue);
            _maximumLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeMaximumValue);
            _stepLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeStep);
            _cyclicLabel.Content = LanguageManager.GetPhrase(Phrases.EditorLoopRange);
            _defaultValueLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRangeDefaultValue);
        }

        public bool IsDataChanged()
        {
            if (!Utils.IsNumeric(_minimum.Text) || !Utils.IsNumeric(_maximum.Text) || !Utils.IsNumeric(_step.Text))
                return true;
            if (_enableDefaultValue.IsChecked == true)
            {
                if (!Utils.IsNumeric(_defaultValue.Text))
                    return true;
                if (_defaultValue.Text != _assignedAccessDescriptor.GetDefaultValueFormula())
                    return true;
            }
            if (_minimum.Text != _assignedAccessDescriptor.GetMinimumValueFormula())
                return true;
            if (_maximum.Text != _assignedAccessDescriptor.GetMaximumValueFormula())
                return true;
            if (_step.Text != _assignedAccessDescriptor.GetStepFormula())
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
                if (string.IsNullOrEmpty(_defaultValue.Text))
                    emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeDefaultValue);
            }
            if (string.IsNullOrEmpty(_minimum.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeMinimumValue);
            if (string.IsNullOrEmpty(_maximum.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorRangeMaximumValue);
            if (string.IsNullOrEmpty(_step.Text))
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

/*        private void _minimum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsCharNumeric(e.Text);
        }

        private void _maximum_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
        }*/
    }
}
