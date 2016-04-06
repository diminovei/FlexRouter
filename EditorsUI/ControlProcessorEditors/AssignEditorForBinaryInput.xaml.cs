using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class AssignEditorForBinaryInput : IEditor, IControlProcessorEditor
    {
        private readonly HardwareModuleType _hardwareSupported;
        private readonly SelectedRowAndColumn _selectedRowAndColumn = new SelectedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;
        private readonly ButtonBinaryInputProcessor _assignedControlProcessor;
        private DataTable _dataTable = new DataTable();

        private readonly SortedDictionary<string, bool> _activeButtonsList = new SortedDictionary<string, bool>();
        
        private bool _initializationModeOn;
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        public DataView FillActiveButtonGrid()
        {
            _dataTable = new DataTable();
            var dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorHardware));
            dc.ReadOnly = true;
            dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorState));
            dc.ReadOnly = true;

            foreach (var bl in _activeButtonsList)
                _dataTable.Rows.Add(bl.Key, bl.Value ? "1" : "0");
            return _dataTable.AsDataView();
        }
        public AssignEditorForBinaryInput(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            InitializeComponent();
            _hardwareSupported = hardwareSupported;
            _assignedControlProcessor = (ButtonBinaryInputProcessor)processor;
            var usedHardware = _assignedControlProcessor.GetInvolvedHardwareWithCurrentStates();
            foreach (var b in usedHardware)
            {
                _activeButtonsList.Add(b.Key, b.Value);
            }
            _assignEditorHelper = new AssignEditorHelper(processor);

            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            AssignmentGrid.ItemsSource = _assignEditorHelper.GetGridData();
        }
        private string GetCurrentControlsCode()
        {
            return _activeButtonsList.Aggregate(string.Empty, (current, bl) => current + (bl.Value ? "1" : "0"));
        }

        public void Save()
        {
            var selectedRowIndex = _selectedRowAndColumn.GetSelectedRowIndex();
            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
                return;
            _assignedControlProcessor.SetInvolvedHardwareWithCurrentStates(_activeButtonsList);
            _assignEditorHelper.Save(selectedRowIndex, GetCurrentControlsCode());
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _initialize.Content = LanguageManager.GetPhrase(_initializationModeOn ? Phrases.EditorStopInitializeBinaryInputButtonsList : Phrases.EditorStartInitializeBinaryInputButtonsList);
            _hardwareTypeLabel.Content = _assignEditorHelper.LocalizeHardwareLabel(_hardwareSupported);
            _allActiveButtons.ItemsSource = FillActiveButtonGrid();
        }

        public bool IsDataChanged()
        {
            //ToDO
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        //
        // SINGLE CLICK EDITING
        //
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _selectedRowAndColumn.OnMouseDoubleClick((DependencyObject)e.OriginalSource);
        }

        private void StatesGridGotFocus(object sender, RoutedEventArgs e)
        {
            _selectedRowAndColumn.OnMouseDoubleClick((DependencyObject)e.OriginalSource);
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
            var found = false;
            foreach (var item in _activeButtonsList)
            {
                if (item.Key != controlEvent.Hardware.GetHardwareGuid())
                    continue;
                _activeButtonsList[item.Key] = ev.IsPressed;
                found = true;
                break;
            }
            if (!found && _initializationModeOn)
            {
                _activeButtonsList.Add(controlEvent.Hardware.GetHardwareGuid(), ev.IsPressed);
            }
            _allActiveButtons.ItemsSource = FillActiveButtonGrid();
        }

        private void AssignmentGridLoaded(object sender, RoutedEventArgs e)
        {
            if (AssignmentGrid.Columns.Count != 0)
                AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
            SelectDataGridRow.SelectRowByIndex(AssignmentGrid, 0);
        }

        private void InitializeChecked(object sender, RoutedEventArgs e)
        {
            ChangeInitializationMode();
        }

        private void InitializeUnchecked(object sender, RoutedEventArgs e)
        {
            ChangeInitializationMode();
        }

        private void ChangeInitializationMode()
        {
            _initializationModeOn = _initialize.IsChecked == true;
            if (_initializationModeOn)
            {
                _activeButtonsList.Clear();
                _allActiveButtons.ItemsSource = FillActiveButtonGrid();
            }
            Localize();
        }
    }
}
