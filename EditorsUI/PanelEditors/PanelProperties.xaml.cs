using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.EditorsUI.PanelEditors
{
    /// <summary>
    /// Interaction logic for PanelProperties.xaml
    /// </summary>
    public partial class PanelProperties : IEditor
    {
        private readonly Panel _editablePanel;
        private bool _isNewPanel;
/*        public PanelProperties()
        {
            InitializeComponent();
            Localize();
        }*/
        public PanelProperties(Panel panel, bool isNewPanel)
        {
            _editablePanel = panel;
            _isNewPanel = isNewPanel;
            InitializeComponent();
            Localize();
            _panelName.Text = _editablePanel.Name;
            _powerFormula.Text = _editablePanel.PowerFormula;
        }

        public void Save()
        {
/*            var isNewPanel = _editablePanel == null;
            if (_editablePanel == null)
                _editablePanel = new Panel();*/
            _editablePanel.Name = _panelName.Text;
            _editablePanel.PowerFormula = _powerFormula.Text;
            Profile.RegisterPanel(_editablePanel, _isNewPanel);
            _isNewPanel = false;
        }

        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (string.IsNullOrEmpty(_panelName.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName);
            if (_panelName.Text.Contains("."))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorName) + " (" + LanguageManager.GetPhrase(Phrases.EditorBadSymbols) + ")";
            // ToDo _calculator.CheckFormula
            return new EditorFieldsErrors(emptyField);
        }
        public void Localize()
        {
            _editorTypeLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHeaderPanel);
            _nameLabel.Content = LanguageManager.GetPhrase(Phrases.EditorName);
            _powerFormulaLabel.Content = LanguageManager.GetPhrase(Phrases.EditorPowerFormula);
        }

        public bool IsDataChanged()
        {
            if (_editablePanel == null)
                return true;
            return !Utils.AreStringsEqual(_panelName.Text, _editablePanel.Name) ||
                   !Utils.AreStringsEqual(_powerFormula.Text, _editablePanel.PowerFormula);
        }

        private void _panelName_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = e.Text == ".";
        }
    }
}
