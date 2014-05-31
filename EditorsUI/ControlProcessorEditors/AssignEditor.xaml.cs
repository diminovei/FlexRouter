using System.Windows;
using System.Windows.Input;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessorEditors;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorPanels;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class AssignEditor : IEditor, IControlProcessorEditor
    {
        private readonly HardwareModuleType _hardwareSupported;
        private readonly SelecedRowAndColumn _selecedRowAndColumn = new SelecedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;

        public AssignEditor(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            _hardwareSupported = hardwareSupported;
            _assignEditorHelper = new AssignEditorHelper(processor, enableInverse);
            InitializeComponent();
            ShowData();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            AssignmentGrid.ItemsSource = _assignEditorHelper.GetGridData();
            if(AssignmentGrid.Columns.Count!=0)
                AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
        }
        public void Save()
        {
            var selectedRowIndex = _selecedRowAndColumn.GetSelectedRowIndex();
//            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
//                return;
            _assignEditorHelper.Save(selectedRowIndex, _hardware.Text);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _hardwareLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardware);
            _hardwareTypeLabel.Content = _assignEditorHelper.LocalizeHardwareLabel(_hardwareSupported);
        }

        public bool IsDataChanged()
        {
            //ToDO
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            //ToDO
            return new EditorFieldsErrors(null);
     //       throw new NotImplementedException();
        }

        //
        // SINGLE CLICK EDITING
        //
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _selecedRowAndColumn.OnMouseDoubleClick((DependencyObject)e.OriginalSource);
        }

        private void StatesGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            _selecedRowAndColumn.OnMouseDoubleClick((DependencyObject)e.OriginalSource);
        }
        /// <summary>
        /// Функция обрабатывает нажатие кнопки или кручение энкодера
        /// Для того, чтобы корректно обрабатывать галетники, функция реагирует преимущестенно на состояние "включено"
        /// </summary>
        /// <param name="controlEvent">Структура, описывающая произошедшее событие</param>
        public void OnNewControlEvent(ControlEventBase controlEvent)
        {
            // Если событие не соответствует контролу, указанному в AccessDescriptor, то игнорируем его
            if (controlEvent.Hardware.ModuleType != _hardwareSupported)
                return;
            if (_hardwareSupported == HardwareModuleType.Button)
            {
                if (!((ButtonEvent) controlEvent).IsPressed)
                    return;
                _direction.Text = ((ButtonEvent)controlEvent).IsPressed ? "^^^" : "vvv";
            }
            if(_hardwareSupported == HardwareModuleType.Encoder)
                _direction.Text = ((EncoderEvent)controlEvent).RotateDirection ? ">>>" : "<<<";
            _hardware.Text = controlEvent.Hardware.GetHardwareGuid();
        }

        private void AssignmentGrid_Loaded(object sender, RoutedEventArgs e)
        {
            AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
        }
    }
}
