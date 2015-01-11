using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class RepeaterEditor : IEditor
    {
        private readonly DescriptorMultistateBase _accessDescriptor;
        public RepeaterEditor(DescriptorMultistateBase accessDescriptor)
        {
            _accessDescriptor = accessDescriptor;
            InitializeComponent();
            ShowData();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            _repeater.IsChecked = _accessDescriptor.IsRepeaterOn();
        }
        public void Save()
        {
            _accessDescriptor.EnableRepeater(_repeater.IsChecked == true);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _repeaterLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRepeater);
        }

        public bool IsDataChanged()
        {
            return _accessDescriptor.IsRepeaterOn() != (_repeater.IsChecked == true);
        }

        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }
    }
}
