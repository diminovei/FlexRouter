using System;
using System.Globalization;
using System.Windows.Threading;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.EditorsUI.VariableEditors
{
    /// <summary>
    /// Interaction logic for MemoryPatchVariableEditor.xaml
    /// </summary>
    public partial class VariableValueEditor : IEditor
    {
        private readonly IVariable _editableVariable;
        DispatcherTimer _timer;

        public VariableValueEditor(IVariable variable)
        {
            _editableVariable = variable;
            Initialize();
            Localize();
        }
        private void Initialize()
        {
            InitializeComponent();
            Localize();
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimedEvent;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            var value = VariableManager.ReadValue(_editableVariable.Id);
            _variableDecValue.Text = value.Value.ToString(CultureInfo.InvariantCulture);
            _variableHexValue.Text = ((long)value.Value).ToString("X");
        }
        public bool IsDataChanged()
        {
            return false;
        }

        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        public void Save()
        {
        }

        public void Localize()
        {
            _variableDecValueLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableDecValue);
            _variableHexValueLabel.Content = LanguageManager.GetPhrase(Phrases.EditorVariableHexValue);
        }

        private void Grid_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
        }
    }
}
