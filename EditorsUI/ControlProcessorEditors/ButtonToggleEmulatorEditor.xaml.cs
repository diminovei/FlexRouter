using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class ButtonToggleEmulatorEditor : IEditor, IControlProcessorEditor
    {
        private readonly IControlProcessor _assignedControlProcessor;
        public ButtonToggleEmulatorEditor(IControlProcessor processor)
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
            _emulateToggle.IsChecked = ((ButtonProcessor) _assignedControlProcessor).GetEmulateToggleMode();
        }
        public void Save()
        {
            ((ButtonProcessor)_assignedControlProcessor).SetEmulateToggleMode(_emulateToggle.IsChecked == true);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _emulateToggleLabel.Content = LanguageManager.GetPhrase(Phrases.EditorToggleEmulator);
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
