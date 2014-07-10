using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using SlimDX.DirectInput;

namespace FlexRouter.EditorsUI.Dialogues
{
    /// <summary>
    /// Interaction logic for InputString.xaml
    /// </summary>
    public partial class ProfileEditor : Window
    {
        public ProfileEditor()
        {
            InitializeComponent();
            Localize();
            var processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                var cbi = new ComboBoxItem { Content = p.ProcessName};
                _mainProcessName.Items.Add(cbi);
            }
            _profileName.Focus();
        }

        private void Localize()
        {
            Title = LanguageManager.GetPhrase(Phrases.EditorCreateProfile);
            _profileNameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorProfileName);
            _mainProcessNameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorMainProcessName);
        }

        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_profileName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorProfileName);
            if (string.IsNullOrEmpty(_mainProcessName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorMainProcessName);

            return new EditorFieldsErrors(emptyField);
        }

        public string GetProfileName()
        {
            return _profileName.Text;
        }
        public string GetMainProcessName()
        {
            return _mainProcessName.Text;
        }
        private void OkClick(object sender, RoutedEventArgs e)
        {
            var res = IsCorrectData();
            if (!res.IsDataFilledCorrectly)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageDataIsIncorrect) + res.ErrorsText;
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            _profileName.Text = null;
            _mainProcessName.Items.Clear();
            _mainProcessName.Text = null;
            Close();
        }

        private void InputTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
                OkClick(null, null);
            }
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                CloseClick(null, null);
            }
        }
    }
}
