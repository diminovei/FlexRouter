using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
            _moduleId.Text = "0";
            _controlId.Text = "0";
            ShowData();
            Localize();
            EnableControls();
        }

        ~AssignEditorForOutput()
        {
            HardwareManager.StopComponentSearch();
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
            var hardware = string.Format("{0}|{1}|{2}|{3}", _motherboardList.Text, _hardwareSupported, _moduleId.Text, _controlId.Text);
            return hardware;
        }

        public void Localize()
        {
            ShowData();
            _motherboardListLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareMotherboard);
            _moduleIdLabel.Content = LanguageManager.GetPhrase(Phrases.EditorHardwareModule);
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
            if (_moduleId == null || string.IsNullOrEmpty(_moduleId.Text))
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

        private void MotherboardListDropDownOpened(object sender, System.EventArgs e)
        {
            _motherboardList.Items.Clear();
            var devices = HardwareManager.GetConnectedDevices();
            foreach (var device in devices)
                _motherboardList.Items.Add(device);
        }

        private void ModuleIdPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 4;
        }

        private void ControlIdPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 4;
        }

        private void ModuleValueUpClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_moduleId.Text))
                _moduleId.Text = "0";
            var value = Convert.ToInt32(_moduleId.Text);
            _moduleId.Text = ((value + 1).ToString(CultureInfo.InvariantCulture));
        }

        private void ControlValueUpClick(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(_controlId.Text))
                _controlId.Text = "0";
            var value = Convert.ToInt32(_controlId.Text);
            _controlId.Text = ((value + 1).ToString(CultureInfo.InvariantCulture));
        }

        private void ModuleValueZeroClick(object sender, RoutedEventArgs e)
        {
            _moduleId.Text = "0";
        }

        private void ControlValueZeroClick(object sender, RoutedEventArgs e)
        {
            _controlId.Text = "0";
        }

        private void ModuleIdTextChanged(object sender, TextChangedEventArgs e)
        {
            StartSearch();
        }

        private void ControlIdTextChanged(object sender, TextChangedEventArgs e)
        {
            StartSearch();
        }

        private void MotherboardListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void EnableControls()
        {
            if (_motherboardList.Text.StartsWith("Arcc:") && _hardwareSupported == HardwareModuleType.Indicator)
            {
                _controlId.Visibility = Visibility.Hidden;
                _controlId.Text = "0";
                _controlValueDown.Visibility = Visibility.Hidden;
                _controlValueUp.Visibility = Visibility.Hidden;
                _controlValueZero.Visibility = Visibility.Hidden;
                _controlIdLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                _controlId.Visibility = Visibility.Visible;
                _controlValueDown.Visibility = Visibility.Visible;
                _controlValueUp.Visibility = Visibility.Visible;
                _controlValueZero.Visibility = Visibility.Visible;
                _controlIdLabel.Visibility = Visibility.Visible;
            }
        }

        private void StartSearch()
        {
            var hardware = GetSelectedHardwareGuid();
            if(!string.IsNullOrEmpty(hardware))
                HardwareManager.SetComponentToSearch(hardware);
        }

        private void MotherboardListDropDownClosed(object sender, EventArgs e)
        {
            EnableControls();
            StartSearch();
        }

        private void ModuleValueDownClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_moduleId.Text))
                _moduleId.Text = "0";
            var value = Convert.ToInt32(_moduleId.Text);
            if (value > 0)
                _moduleId.Text = ((value - 1).ToString(CultureInfo.InvariantCulture));
        }

        private void ControlValueDownClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_controlId.Text))
                _controlId.Text = "0";
            var value = Convert.ToInt32(_controlId.Text);
            if(value > 0)
                _controlId.Text = ((value - 1).ToString(CultureInfo.InvariantCulture));
        }
    }
}
