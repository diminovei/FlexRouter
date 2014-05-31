using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FlexRouter.AccessDescriptors.Helpers;
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
    partial class AssignEditorForBinaryInput : IEditor, IControlProcessorEditor
    {
        private readonly HardwareModuleType _hardwareSupported;
        private readonly SelecedRowAndColumn _selecedRowAndColumn = new SelecedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;
        private readonly ButtonBinaryInputProcessor _assignedControlProcessor;
        private DataTable _dataTable = new DataTable();

//        readonly ObservableCollection<ActiveButtonItem> _activeButtonsList = new ObservableCollection<ActiveButtonItem>();
        private readonly SortedDictionary<string, bool> _activeButtonsList = new SortedDictionary<string, bool>();
        
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
            var dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorHardware));
            dc.ReadOnly = true;
            dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorState));
            dc.ReadOnly = true;

            foreach (var bl in _activeButtonsList)
                //_dataTable.Rows.Add(bl.Button, bl.State ? "1" : "0");
                _dataTable.Rows.Add(bl.Key, bl.Value ? "1" : "0");
            return _dataTable.AsDataView();
        }
        public AssignEditorForBinaryInput(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            _hardwareSupported = hardwareSupported;
            _assignedControlProcessor = (ButtonBinaryInputProcessor)processor;
            var usedHardware = _assignedControlProcessor.GetUsedHardwareWithStates();
            foreach (var b in usedHardware)
            {
                _activeButtonsList.Add(b.Key, b.Value);
            }
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
            //return _activeButtonsList.Aggregate(string.Empty, (current, bl) => current + (bl.State ? "1" : "0"));
            return _activeButtonsList.Aggregate(string.Empty, (current, bl) => current + (bl.Value ? "1" : "0"));
        }

        public void Save()
        {
            var selectedRowIndex = _selecedRowAndColumn.GetSelectedRowIndex();
            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
                return;
            _assignedControlProcessor.SetUsedHardwareWithStates(_activeButtonsList);
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
            _selecedRowAndColumn.OnMouseDoubleClick((DependencyObject)e.OriginalSource);
        }

        private void StatesGridGotFocus(object sender, RoutedEventArgs e)
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
            var found = false;
            foreach (var item in _activeButtonsList)
            {
                //if (item.Button != controlEvent.Hardware.GetHardwareGuid())
                if (item.Key != controlEvent.Hardware.GetHardwareGuid())
                    continue;
                //item.State = ev.IsPressed;
                _activeButtonsList[item.Key] = ev.IsPressed;
//                item.Value = ev.IsPressed;
                found = true;
                break;
            }
            if (!found && _initializationModeOn)
            {
                //_activeButtonsList.Add(new ActiveButtonItem {Button = controlEvent.Hardware.GetHardwareGuid(), State = ev.IsPressed});
                _activeButtonsList.Add(controlEvent.Hardware.GetHardwareGuid(), ev.IsPressed);
            }
            _allActiveButtons.ItemsSource = FillActiveButtonGrid();
        }

        private void AssignmentGridLoaded(object sender, RoutedEventArgs e)
        {
            AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
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
