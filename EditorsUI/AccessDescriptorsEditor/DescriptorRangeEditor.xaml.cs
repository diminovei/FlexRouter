using System.Windows.Controls;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
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
            _defaultValue.IsReadOnly = !_assignedAccessDescriptor.EnableDefaultValue;

            var cbi = new ComboBoxItem { Tag = CycleType.None };
            _cycleType.Items.Add(cbi);
            _cycleType.Items.Add(new ComboBoxItem { Tag = CycleType.Simple });
            _cycleType.Items.Add(new ComboBoxItem { Tag = CycleType.UnreachableMinimum });
            _cycleType.Items.Add(new ComboBoxItem { Tag = CycleType.UnreachableMaximum });
            var ct = assignedAccessDescriptor.GetCycleType();
            foreach (ComboBoxItem cti in _cycleType.Items)
            {
                if((CycleType)cti.Tag != ct)
                    continue;
                _cycleType.SelectedItem = cti;
                break;
            }
            if(_cycleType.SelectedItem == null)
                _cycleType.SelectedItem = cbi;
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
            _assignedAccessDescriptor.EnableDefaultValue = _enableDefaultValue.IsChecked == true;
            if(_cycleType.SelectedItem == null)
                _assignedAccessDescriptor.SetCycleType(CycleType.None);
            else
                _assignedAccessDescriptor.SetCycleType((CycleType)((ComboBoxItem)_cycleType.SelectedItem).Tag);
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
            foreach (ComboBoxItem ct in _cycleType.Items)
                ct.Content = GetCycleTypeItemText((CycleType) ct.Tag);
            ((ComboBoxItem)_cycleType.SelectedItem).Content = GetCycleTypeItemText((CycleType)((ComboBoxItem)_cycleType.SelectedItem).Tag);
        }

        private string GetCycleTypeItemText(CycleType ct)
        {
            var res = string.Empty;
            switch (ct)
            {
                case CycleType.UnreachableMinimum:
                    res = LanguageManager.GetPhrase(Phrases.CycleTypeUnreachableMinimum);
                    break;
                case CycleType.UnreachableMaximum:
                    res = LanguageManager.GetPhrase(Phrases.CycleTypeUnreachableMaximum);
                    break;
                case CycleType.Simple:
                    res = LanguageManager.GetPhrase(Phrases.CycleTypeReachableMinMax);
                    break;
                default:
                    res = LanguageManager.GetPhrase(Phrases.CycleTypeNone);
                    break;
            }
            return res;
        }

        public bool IsDataChanged()
        {
            if (_enableDefaultValue.IsChecked == true && _defaultValue.Text != _assignedAccessDescriptor.GetDefaultValueFormula())
                    return true;
            if (_minimum.Text != _assignedAccessDescriptor.GetMinimumValueFormula())
                return true;
            if (_maximum.Text != _assignedAccessDescriptor.GetMaximumValueFormula())
                return true;
            if (_step.Text != _assignedAccessDescriptor.GetStepFormula())
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

        private void EnableDefaultValueChecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _defaultValue.IsReadOnly = false;
        }

        private void EnableDefaultValueUnchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _defaultValue.IsReadOnly = true;
        }
    }
}
