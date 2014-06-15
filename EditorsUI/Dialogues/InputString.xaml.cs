using System.Windows;
using SlimDX.DirectInput;

namespace FlexRouter.EditorsUI.Dialogues
{
    /// <summary>
    /// Interaction logic for InputString.xaml
    /// </summary>
    public partial class InputString : Window
    {
        public InputString(string dialogHeader, string defauleValue = null)
        {
            InitializeComponent();
            Title = dialogHeader;
            if (!string.IsNullOrEmpty(defauleValue))
                _inputText.Text = defauleValue;
            _inputText.Focus();
        }

        public string GetText()
        {
            return _inputText.Text;
        }
        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            _inputText.Text = null;
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
