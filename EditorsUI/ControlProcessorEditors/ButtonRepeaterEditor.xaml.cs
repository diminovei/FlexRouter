using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class ButtonRepeaterEditor : IEditor, IControlProcessorEditor
    {
        private readonly IRepeater _assignedControlProcessor;
        public ButtonRepeaterEditor(IRepeater processor)
        {
            _assignedControlProcessor = processor;
            InitializeComponent();
            ShowData();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            _repeater.IsChecked = ((IRepeater)_assignedControlProcessor).IsRepeaterOn();
        }
        public void Save()
        {
            ((IRepeater)_assignedControlProcessor).EnableRepeater(_repeater.IsChecked == true);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _repeaterLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRepeater);
        }

        public bool IsDataChanged()
        {
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        /// <summary>
        /// Функция обрабатывает нажатие кнопки или кручение энкодера
        /// Для того, чтобы корректно обрабатывать галетники, функция реагирует преимущестенно на состояние "включено"
        /// </summary>
        /// <param name="controlEvent">Структура, описывающая произошедшее событие</param>
        public void OnNewControlEvent(ControlEventBase controlEvent)
        {
        }
    }
}
