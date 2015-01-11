using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorPanels;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class AssignEditorForOutput : IEditor, IControlProcessorEditor
    {
        private readonly HardwareModuleType _hardwareSupported;
        private readonly SelecedRowAndColumn _selecedRowAndColumn = new SelecedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;

        public AssignEditorForOutput(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            InitializeComponent();
            _hardwareSupported = hardwareSupported;
            _assignEditorHelper = new AssignEditorHelper(processor, enableInverse);
            _moduleList.Text = "0";
            _blockId.Text = "0";
            _controlId.Text = "0";
            ShowData();
            Localize();
            EnableControls();
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
        public void Save()
        {
            var selectedRowIndex = _selecedRowAndColumn.GetSelectedRowIndex();
            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
                return;
            var hardware = GetSelectedHardwareGuid();
            //ToDo: сохранение не работает
            _assignEditorHelper.Save(selectedRowIndex, hardware);
            ShowData();
            HardwareManager.StopComponentSearch();
        }

        private string GetSelectedHardwareGuid()
        {
            if (!IsCorrectData().IsDataFilledCorrectly)
                return string.Empty;

            var cph = new ControlProcessorHardware();
            cph.BlockId = uint.Parse(_blockId.Text);
            cph.ControlId = _controlId.SelectedItem != null ? uint.Parse(_controlId.SelectedItem.ToString()) : uint.Parse(_controlId.Text);
            cph.MotherBoardId = _motherboardList.SelectedItem != null ? _motherboardList.SelectedItem.ToString() : _motherboardList.Text;
            cph.ModuleType = _hardwareSupported;
            cph.ModuleId = _moduleList.SelectedItem != null ? uint.Parse(_moduleList.SelectedItem.ToString()) : uint.Parse(_moduleList.Text);
            return cph.GetHardwareGuid();
        }

        public void Localize()
        {
            ShowData();
            _motherboardListLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareMotherboard);
            _moduleListLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareModule);
            _blockIdLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareBlock);
            _controlIdLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareControl);
            _hardwareTypeLabel.Content = _assignEditorHelper.LocalizeHardwareLabel(_hardwareSupported);
        }

        public bool IsDataChanged()
        {
            //ToDo
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            var emptyField = string.Empty;
            if (_motherboardList == null || string.IsNullOrEmpty(_motherboardList.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareMotherboard);
            if (_moduleList == null || string.IsNullOrEmpty(_moduleList.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareModule);
            if (_controlId == null || string.IsNullOrEmpty(_controlId.Text))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareControl);
            return new EditorFieldsErrors(emptyField);
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
        }
        private void AssignmentGridLoaded(object sender, RoutedEventArgs e)
        {
            AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
        }
        public static bool IsNumeric(string text)
        {
            var regex = new Regex("^[0-9]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }
        private void MotherboardListDropDownOpened(object sender, EventArgs e)
        {
            _motherboardList.Items.Clear();
            var devices = HardwareManager.GetConnectedDevices();
            foreach (var device in devices)
                _motherboardList.Items.Add(device);
        }
        private void MotherboardListDropDownClosed(object sender, EventArgs e)
        {
            EnableControls();
            StartSearch();
        }

        private void _moduleList_DropDownClosed(object sender, EventArgs e)
        {
            EnableControls();
            StartSearch();
        }

        private void _moduleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSearch();
        }
        private void _blockId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSearch();
        }
        private void _controlId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartSearch();
        }

        private void SelectNextComboboxItem(ref ComboBox cb)
        {
            if (cb.SelectedIndex + 1 < cb.Items.Count)
                cb.SelectedIndex++;
        }
        private void SelectPrevComboboxItem(ref ComboBox cb)
        {
            if (cb.SelectedIndex - 1 >= 0)
                cb.SelectedIndex--;
        }
        private void SelectFirstComboboxItem(ref ComboBox cb)
        {
            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
        }

        private void ModuleValueDownClick(object sender, RoutedEventArgs e)
        {
            SelectPrevComboboxItem(ref _moduleList);
        }
        private void ModuleValueUpClick(object sender, RoutedEventArgs e)
        {
            SelectNextComboboxItem(ref _moduleList);
        }
        private void ModuleValueZeroClick(object sender, RoutedEventArgs e)
        {
            SelectFirstComboboxItem(ref _moduleList);
        }
        private void _blockValueDown_Click(object sender, RoutedEventArgs e)
        {
            SelectPrevComboboxItem(ref _blockId);
        }
        private void _blockValueUp_Click(object sender, RoutedEventArgs e)
        {
            SelectNextComboboxItem(ref _blockId);
        }
        private void _blockValueZero_Click(object sender, RoutedEventArgs e)
        {
            SelectFirstComboboxItem(ref _blockId);
        }
        private void ControlValueZeroClick(object sender, RoutedEventArgs e)
        {
            SelectFirstComboboxItem(ref _controlId);
        }
        private void ControlValueDownClick(object sender, RoutedEventArgs e)
        {
            SelectPrevComboboxItem(ref _controlId);
        }
        private void ControlValueUpClick(object sender, RoutedEventArgs e)
        {
            SelectNextComboboxItem(ref _controlId);
        }

        private void _moduleList_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 4;
        }
        private void BlockIdPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 4;
        }
        private void ControlIdPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 4;
        }

        private void EnableControls()
        {
            if (string.IsNullOrEmpty(_motherboardList.Text))
            {
                ShowModuleList(false);
                ShowBlock(false);
                ShowControl(false);
                return;
            }
            // Заполняем ControlProcessorHardware
            var cph = new ControlProcessorHardware
            {
                ModuleType = _hardwareSupported,
                MotherBoardId = _motherboardList.Text
            };
            if (!string.IsNullOrEmpty(_moduleList.Text) && IsNumeric(_moduleList.Text))
                cph.ModuleId = uint.Parse(_moduleList.Text);
            if (!string.IsNullOrEmpty(_blockId.Text) && IsNumeric(_blockId.Text))
                cph.BlockId = uint.Parse(_blockId.Text);
            if (!string.IsNullOrEmpty(_controlId.Text) && IsNumeric(_controlId.Text))
                cph.ControlId = uint.Parse(_controlId.Text);
            // Получаем возможности железа
            var extensionModule = HardwareManager.GetCapacity(cph, DeviceSubType.ExtensionBoard);
            ShowModuleList(extensionModule != null);
            FillCombobox(_moduleList, extensionModule);

            var block = HardwareManager.GetCapacity(cph, DeviceSubType.Block);
            ShowBlock(block!=null && block.Length != 0);
            FillCombobox(_blockId, block);
                    
            var control = HardwareManager.GetCapacity(cph, DeviceSubType.Control);
            ShowControl(control!=null && control.Length != 0);
            FillCombobox(_controlId, control);
        }

        private void FillCombobox(ComboBox cb, int[] dc)
        {
            if (dc == null) 
                return;
            var value = cb.Text;
            cb.Items.Clear();
            foreach (var deviceIndex in dc)
                cb.Items.Add(deviceIndex);

            if (dc.Contains(int.Parse(value)))
                cb.Text = value;
        }
        private void ShowModuleList(bool show)
        {
            _moduleList.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleListLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if (!show)
                _moduleList.Text = "0";
            _moduleValueDown.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleValueUp.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleValueZero.Visibility = show ? Visibility.Visible : Visibility.Hidden;

        }
        private void ShowBlock(bool show)
        {
            _blockId.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if (!show)
                _blockId.Text = "0";
            _blockValueDown.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockValueUp.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockValueZero.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockIdLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }
        private void ShowControl(bool show)
        {
            _controlId.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if(!show)
                _controlId.Text = "0";
            _controlValueDown.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _controlValueUp.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _controlValueZero.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _controlIdLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        private void StartSearch()
        {
            var hardware = GetSelectedHardwareGuid();
            if(!string.IsNullOrEmpty(hardware))
                HardwareManager.SetComponentToSearch(hardware);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            HardwareManager.StopComponentSearch();
        }
    }
}
