using System.Windows;

namespace FlexRouter.EditorPanels
{
    /// <summary>
    /// Interaction logic for InputString.xaml
    /// </summary>
    public partial class InputString : Window
    {
        public InputString(string dialogHeader)
        {
            InitializeComponent();
            Title = dialogHeader;
            _inputText.Focus();
        }

        public string GetText()
        {
            return _inputText.Text;
        }
        private void _ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void _close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            _inputText.Text = null;
            Close();
        }
    }
}
