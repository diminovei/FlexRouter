using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors;
using FlexRouter.EditorPanels;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class AssignEditorForBinaryInput : IEditor, IControlProcessorEditor
    {
        private readonly HardwareModuleType _hardwareSupported;
        private readonly SelecedRowAndColumn _selecedRowAndColumn = new SelecedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;
        private DataTable _dataTable = new DataTable();

        ObservableCollection<ActiveButtonItem> activeButtonsList = new ObservableCollection<ActiveButtonItem>();
        
        private bool _initializationModeOn;
        internal class ActiveButtonItem
        {
            internal string Button;
            internal bool State;
        }

        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        public DataView FillActiveButtonGrid()
        {
            _dataTable = new DataTable();
            var dc = _dataTable.Columns.Add("Button"/*LanguageManager.GetPhrase(Phrases.EditorHardware)*/);
            dc.ReadOnly = true;
            dc = _dataTable.Columns.Add(/*new DataColumn(LanguageManager.GetPhrase(Phrases.EditorInvert), typeof(bool))*/"State");
            dc.ReadOnly = true;

            foreach (var bl in activeButtonsList)
                _dataTable.Rows.Add(bl.Button, bl.State ? "1" : "0");
            return _dataTable.AsDataView();
        }
        public AssignEditorForBinaryInput(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            _hardwareSupported = hardwareSupported;
            _assignEditorHelper = new AssignEditorHelper(processor, enableInverse);

            InitializeComponent();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            AssignmentGrid.ItemsSource = _assignEditorHelper.GetGridData();
            if (AssignmentGrid.Columns.Count != 0)
                AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
        }
        private string GetCurrentControlsCode()
        {
            return activeButtonsList.Aggregate(string.Empty, (current, bl) => current + (bl.State ? "1" : "0"));
        }

        public void Save()
        {
            var selectedRowIndex = _selecedRowAndColumn.GetSelectedRowIndex();
            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
                return;
            //ToDo: не сохраняет код
            _assignEditorHelper.Save(selectedRowIndex, GetCurrentControlsCode());
            ShowData();
        }

        public void Localize()
        {
            ShowData();
//            _hardwareLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardware);
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
            var ev = controlEvent as ButtonEvent;
            if (ev == null)
                return;
            if (_initializationModeOn)
            {
                var found = false;
                foreach (var item in activeButtonsList)
                {
                    if (item.Button != controlEvent.Hardware.GetHardwareGuid())
                        continue;
                    item.State = ev.IsPressed;
                    found = true;
                    break;
                }
                if (!found)
                {
                    activeButtonsList.Add(new ActiveButtonItem {Button = controlEvent.Hardware.GetHardwareGuid(), State = ev.IsPressed});
                }
                _allActiveButtons.ItemsSource = FillActiveButtonGrid();
            }
        }

        private void AssignmentGrid_Loaded(object sender, RoutedEventArgs e)
        {
            AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
        }

        private void _initialize_Checked(object sender, RoutedEventArgs e)
        {
            _initializationModeOn = _initialize.IsChecked == true;
            if (_initializationModeOn)
            {
                activeButtonsList.Clear();
                FillActiveButtonGrid();
            }
        }
    }
}
