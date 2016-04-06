using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlexRouter.ControlProcessors.Helpers;
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
        private readonly SelectedRowAndColumn _selectedRowAndColumn = new SelectedRowAndColumn();
        private readonly AssignEditorHelper _assignEditorHelper;
        public AssignEditorForOutput(IControlProcessor processor, bool enableInverse, HardwareModuleType hardwareSupported)
        {
            InitializeComponent();
            _hardwareSupported = hardwareSupported;
            _assignEditorHelper = new AssignEditorHelper(processor);
            ShowData();
            Localize();
            CheckSelection();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            AssignmentGrid.ItemsSource = _assignEditorHelper.GetGridData();
        }
        public void Save()
        {
            var selectedRowIndex = _selectedRowAndColumn.GetSelectedRowIndex();
            if (selectedRowIndex == -1 || AssignmentGrid.Columns.Count == 0) 
                return;
            var hardware = GetSelectedHardwareGuid();
            _assignEditorHelper.Save(selectedRowIndex, hardware);
            ShowData();
            HardwareManager.StopComponentSearch();
        }
        private string GetSelectedHardwareGuid()
        {
            if (!IsCorrectData().IsDataFilledCorrectly)
                return string.Empty;

            var cph = new ControlProcessorHardware
            {
                ModuleType = _hardwareSupported,
                MotherBoardId = (string)_motherboardList.SelectedItem,
                BlockId = uint.Parse(string.IsNullOrEmpty((string)_blockId.SelectedItem) ? "0" : (string)_blockId.SelectedItem),
                ModuleId = uint.Parse(string.IsNullOrEmpty((string)_moduleList.SelectedItem) ? "0" : (string)_moduleList.SelectedItem),
                ControlId = uint.Parse(string.IsNullOrEmpty((string)_controlId.SelectedItem) ? "0" : (string)_controlId.SelectedItem),
            };
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
            return false;
        }
        public EditorFieldsErrors IsCorrectData()
        {
            var cph = CheckSelection();
            if(cph!=null)
                return new EditorFieldsErrors(string.Empty);
            var emptyField = string.Empty;

            if (string.IsNullOrEmpty((string)_motherboardList.SelectedItem))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareMotherboard);
            if (_moduleList.IsVisible && string.IsNullOrEmpty((string)_moduleList.SelectedItem))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareModule);
            if (_blockId.IsVisible && string.IsNullOrEmpty((string)_blockId.SelectedItem))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareBlock);
            if (_controlId.IsVisible && string.IsNullOrEmpty((string)_controlId.SelectedItem))
                emptyField += "\n" + LanguageManager.GetPhrase(Phrases.EditorHardwareControl);

            return new EditorFieldsErrors(emptyField);
        }
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
        }
        private void AssignmentGridLoaded(object sender, RoutedEventArgs e)
        {
            if (AssignmentGrid.Columns.Count != 0)
                AssignmentGrid.Columns[0].Visibility = Visibility.Hidden;
            SelectDataGridRow.SelectRowByIndex(AssignmentGrid, 0);
        }
        public static bool IsNumeric(string text)
        {
            var regex = new Regex("^[0-9]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }
        private void MotherboardListDropDownOpened(object sender, EventArgs e)
        {
            RememberSelectedItemOnOpenCombobox(_motherboardList, DeviceSubType.Motherboard);
        }
        private void MotherboardListDropDownClosed(object sender, EventArgs e)
        {
            RestoreSelectedItemOnCloseCombobox(_motherboardList);
        }
        private void _moduleList_DropDownOpened(object sender, EventArgs e)
        {
            RememberSelectedItemOnOpenCombobox(_moduleList, DeviceSubType.ExtensionBoard);
        }
        private void _moduleList_DropDownClosed(object sender, EventArgs e)
        {
            RestoreSelectedItemOnCloseCombobox(_moduleList);
        }
        private void _blockId_DropDownOpened(object sender, EventArgs e)
        {
            RememberSelectedItemOnOpenCombobox(_blockId, DeviceSubType.Block);
        }
        private void _blockId_DropDownClosed(object sender, EventArgs e)
        {
            RestoreSelectedItemOnCloseCombobox(_blockId);
        }
        private void _controlId_DropDownOpened(object sender, EventArgs e)
        {
            RememberSelectedItemOnOpenCombobox(_controlId, DeviceSubType.Control);
        }
        private void _controlId_DropDownClosed(object sender, EventArgs e)
        {
            RestoreSelectedItemOnCloseCombobox(_controlId);
        }
        private void SelectNextComboboxItem(ref ComboBox cb)
        {
            if (cb.SelectedItem == null)
            {
                SelectFirstComboboxItem(ref cb);
                OnSelectionChanged();
                return;
            }
            if (cb.SelectedIndex + 1 < cb.Items.Count)
                cb.SelectedIndex++;
            OnSelectionChanged();
        }
        private void SelectPrevComboboxItem(ref ComboBox cb)
        {
            if (cb.SelectedIndex - 1 >= 0)
                cb.SelectedIndex--;
            OnSelectionChanged();
        }
        private void SelectFirstComboboxItem(ref ComboBox cb)
        {
            if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
            OnSelectionChanged();
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
        private void OnSelectionChanged()
        {
            var cph = CheckSelection();
            if (cph != null)
                StartSearch();
        }
        private string _selectedItemOfOpenedCombobox;
        private void RememberSelectedItemOnOpenCombobox(ComboBox cb, DeviceSubType deviceSubType)
        {
            _selectedItemOfOpenedCombobox = (string)cb.SelectedItem;
            var cph = new ControlProcessorHardware
            {
                ModuleType = _hardwareSupported,
                MotherBoardId = (string)_motherboardList.SelectedItem,
                BlockId = uint.Parse(string.IsNullOrEmpty((string)_blockId.SelectedItem) ? "0" : (string)_blockId.SelectedItem),
                ModuleId = uint.Parse(string.IsNullOrEmpty((string)_moduleList.SelectedItem) ? "0" : (string)_moduleList.SelectedItem),
                ControlId = uint.Parse(string.IsNullOrEmpty((string)_controlId.SelectedItem) ? "0" : (string)_controlId.SelectedItem),
            };
            cb.Items.Clear();
            var capacity = HardwareManager.GetCapacity(cph, deviceSubType).Names;
            foreach (var c in capacity)
                cb.Items.Add(c);
        }
        private void RestoreSelectedItemOnCloseCombobox(ComboBox cb)
        {
            if (string.IsNullOrEmpty(cb.Text))
            {
                if (cb.Items.Cast<string>().Any(item => item == _selectedItemOfOpenedCombobox))
                {
                    cb.SelectedItem = _selectedItemOfOpenedCombobox;
                    cb.Text = _selectedItemOfOpenedCombobox;
                }
            }
            var cph = CheckSelection();
            if(cph!=null)
                StartSearch();
        }
        /// <summary>
        /// Проверяет возможности железа и от этого скрывает или показывает нужные контролы.
        /// </summary>
        /// <returns>null - не все контролы заполнены, !null - всё в порядке, можно отправлять событие на поиск</returns>
        private ControlProcessorHardware CheckSelection()
        {
            var cph = new ControlProcessorHardware
            {
                ModuleType = _hardwareSupported,
            };

            var isPreviousFiledCheckPassedSuccessfully = true;

            // Проверяем Motherboard
            var c = HardwareManager.GetCapacity(cph, DeviceSubType.Motherboard);
            // Если материнская плата не используется
            if (c.DeviceSubtypeIsNotSuitableForCurrentHardware)
            {
                cph.MotherBoardId = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty((string)_motherboardList.SelectedItem))
                    isPreviousFiledCheckPassedSuccessfully = false;
                else
                    cph.MotherBoardId = (string)_motherboardList.SelectedItem;
            }

            // Проверяем ExtensionBoard/ModuleList
            // Если предыдущий шаг провален - скрываем и считаем этот шаг также проваленым
            if (!isPreviousFiledCheckPassedSuccessfully)
                ShowModuleList(false);
            else
            {
                c = HardwareManager.GetCapacity(cph, DeviceSubType.ExtensionBoard);
                // Если материнская плата не используется
                if (c.DeviceSubtypeIsNotSuitableForCurrentHardware)
                {
                    ShowModuleList(false);
                    cph.ModuleId = 0;
                }
                else
                {
                    if (string.IsNullOrEmpty((string)_moduleList.SelectedItem))
                    {
                        ShowModuleList(true);
                        isPreviousFiledCheckPassedSuccessfully = false;
                    }
                        
                    else
                    {
                        ShowModuleList(true);
                        cph.ModuleId = uint.Parse((string)_moduleList.SelectedItem);
                    }
                }
            }

            // Проверяем Block
            // Если предыдущий шаг провален - скрываем и считаем этот шаг также проваленым
            if (!isPreviousFiledCheckPassedSuccessfully)
                ShowBlock(false);
            else
            {
                c = HardwareManager.GetCapacity(cph, DeviceSubType.Block);
                // Если материнская плата не используется
                if (c.DeviceSubtypeIsNotSuitableForCurrentHardware)
                {
                    ShowBlock(false);
                    cph.BlockId = 0;
                }
                else
                {
                    if (string.IsNullOrEmpty((string)_blockId.SelectedItem))
                    {
                        ShowBlock(true);
                        isPreviousFiledCheckPassedSuccessfully = false;
                    }
                    else
                    {
                        ShowBlock(true);
                        cph.BlockId = uint.Parse((string)_blockId.SelectedItem);
                    }
                        
                }
            }

            // Проверяем Control
            // Если предыдущий шаг провален - скрываем и считаем этот шаг также проваленым
            if (!isPreviousFiledCheckPassedSuccessfully)
                ShowControl(false);
            else
            {
                c = HardwareManager.GetCapacity(cph, DeviceSubType.Control);
                // Если материнская плата не используется
                if (c.DeviceSubtypeIsNotSuitableForCurrentHardware)
                {
                    cph.ControlId = 0;
                    ShowControl(false);
                }
                else
                {
                    if (string.IsNullOrEmpty((string)_controlId.SelectedItem))
                    {
                        isPreviousFiledCheckPassedSuccessfully = false;
                        ShowControl(true);
                    }
                        
                    else
                    {
                        ShowControl(true);
                        cph.ControlId = uint.Parse((string)_controlId.SelectedItem);
                    }
                }
            }
            if (isPreviousFiledCheckPassedSuccessfully)
                return cph;
            return null;
        }
        private void ShowModuleList(bool show)
        {
            _moduleList.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleListLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if (!show)
                _moduleList.Text = string.Empty;
            _moduleValueDown.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleValueUp.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _moduleValueZero.Visibility = show ? Visibility.Visible : Visibility.Hidden;

        }
        private void ShowBlock(bool show)
        {
            _blockId.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if (!show)
                _blockId.Text = string.Empty;
            _blockValueDown.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockValueUp.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockValueZero.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _blockIdLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }
        private void ShowControl(bool show)
        {
            _controlId.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            if(!show)
                _controlId.Text = string.Empty;
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
