﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FlexRouter.AccessDescriptors.FormulaKeeper;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.EditorsUI.Dialogues;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class DescriptorValueEditor : IEditor
    {
        // Внимание: при изменении состава состояний и переменных меняем данные только в локальных массивах, не трогая AccessDescriptor, 
        // который меняется только при сохранении
        // При удалении состояния/переменной нужно удалить данные из FormulaKeeper

        private readonly SelectedRowAndColumn _selectedRowAndColumn = new SelectedRowAndColumn();
        private readonly DescriptorMultistateBase _assignedAccessDescriptor;

        private DataTable _dataTable = new DataTable();
        private readonly List<Guid> _usedVariables;
        private readonly List<Connector> _connectorList;
        private readonly FormulaKeeper _localFormulaKeeper = new FormulaKeeper();

        private int _defaultState = -1;
        // Количество дополнительных колонок (до формул). Default, StateName, ...
        private int _additionalColumnsCount;
        private int _stateNameColumnIndex;

        public DescriptorValueEditor(DescriptorMultistateBase assignedAccessDescriptor, bool enableStateManagement)
        {
            _assignedAccessDescriptor = assignedAccessDescriptor;

            _usedVariables = _assignedAccessDescriptor.GetAllUsedVariables().ToList();
            _connectorList = _assignedAccessDescriptor.GetConnectors(null, true).ToList().OrderBy(i => i.Order).ToList();
            foreach (var s in _connectorList)
            {
                foreach (var v in _usedVariables)
                {
                    var formula = _assignedAccessDescriptor.GetFormula(v, s.Id);
                    _localFormulaKeeper.StoreVariableFormula(formula, _assignedAccessDescriptor.GetId(), v, s.Id);
                }
            }
            InitializeComponent();
            Localize();
            AddState.Visibility = enableStateManagement ? Visibility.Visible : Visibility.Hidden;
            RemoveState.Visibility = enableStateManagement ? Visibility.Visible : Visibility.Hidden;
            RenameState.Visibility = enableStateManagement ? Visibility.Visible : Visibility.Hidden;
            if(_assignedAccessDescriptor is IDefautValueAbility)
            {
                _defaultState = ((IDefautValueAbility) assignedAccessDescriptor).GetDefaultStateId();
                SelectDefaultState.Visibility = enableStateManagement ? Visibility.Visible : Visibility.Hidden;
            }
            else
                SelectDefaultState.Visibility = Visibility.Hidden;
            ShowData();
        }
        private void ColumnChanged(object sender, DataColumnChangeEventArgs e )
        {
            ApplyGridChangesToInternalDataStructures();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            _dataTable = new DataTable();
            _dataTable.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);

            // Добавляем колонку "По-умолчанию"
            if (_assignedAccessDescriptor is IDefautValueAbility)
            {
                var dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorDefaultStateColumnHeader), typeof(bool));
                dc.ReadOnly = true;
            }
            // Добавляем колонку States
            _stateNameColumnIndex = _dataTable.Columns.Count;
            _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorState));

            // Заполняем количество дополнительных колонок (тех, что до формул)
            _additionalColumnsCount = _dataTable.Columns.Count;
            // Добавляем колонки с именами переменных
            foreach (var v in _usedVariables)
                _dataTable.Columns.Add(Profile.VariableStorage.GetVariableById(v).Name);
            // Добавляем строки. Первая колонка - наименование состояния, затем формулы для переменных
            foreach (var s in _connectorList)
            {
                var formulaList = new List<object>();
                
                if (_assignedAccessDescriptor is IDefautValueAbility)
                    formulaList.Add(_defaultState == s.Id);
                formulaList.Add(s.Name);
                        
                formulaList.AddRange(_usedVariables.Select(v => _localFormulaKeeper.GetVariableFormulaText(_assignedAccessDescriptor.GetId(), v, s.Id)));
                _dataTable.Rows.Add(formulaList.ToArray());
            }
            StatesGrid.ItemsSource = _dataTable.AsDataView();
        }
        public void Save()
        {
            ApplyGridChangesToInternalDataStructures();
            ApplyInternalStructuresDataChangesToAccessDescriptor();
        }

        public void Localize()
        {
            // ToDo доделать
            RenameState.Content = LanguageManager.GetPhrase(Phrases.EditorRenameState);
            AddState.Content = LanguageManager.GetPhrase(Phrases.EditorAddState);
            RemoveState.Content = LanguageManager.GetPhrase(Phrases.EditorRemoveState);
            AddVariable.Content = LanguageManager.GetPhrase(Phrases.EditorAddVariable);
            RemoveVariable.Content = LanguageManager.GetPhrase(Phrases.EditorRemoveVariable);
            SelectDefaultState.Content = LanguageManager.GetPhrase(Phrases.EditorSetDefaultState);
            ShowData();
        }

        public bool IsDataChanged()
        {
            var origUsedVariables = _assignedAccessDescriptor.GetAllUsedVariables();
            var origStateList = _assignedAccessDescriptor.GetConnectors(null, true).ToList().OrderBy(i => i.Order);

            // Сравниваем размер и состав массива переменных
            if (origUsedVariables.Length != _usedVariables.Count())
                return true;
            if (origUsedVariables.Where((t, i) => t != _usedVariables[i]).Any())
                return true;
            // Сравниваем размер и состав массива состояний
            if (origStateList.Count() != _connectorList.Count())
                return true;
            for (var i = 0; i < origStateList.Count(); i++)
            {
                if (_connectorList.ElementAt(i).Id != origStateList.ElementAt(i).Id)
                    return true;
            }
            // Проверяем совпадение формул
            for (var stateIndex = 0; stateIndex < _dataTable.Rows.Count; stateIndex++)
            {
                var arr = _dataTable.Rows[stateIndex].ItemArray.ToList();
                // Удаляем колонку State и DefaultState
                arr.RemoveRange(0, _additionalColumnsCount);
                for (var varIndex = 0; varIndex < arr.Count; varIndex++)
                {
                    var origFormula = _localFormulaKeeper.GetVariableFormulaText(_assignedAccessDescriptor.GetId(), origUsedVariables[varIndex], origStateList.ElementAt(stateIndex).Id);
                    var dataGridFormula = (string) arr[varIndex];
                    if (!Utils.AreStringsEqual(origFormula, dataGridFormula))
                        return true;
                }
            }
            if (_assignedAccessDescriptor is IDefautValueAbility)
            {
                if (((IDefautValueAbility) _assignedAccessDescriptor).GetDefaultStateId() != _defaultState)
                    return true;
            }
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            // ToDo: проверить корректность формул
            return new EditorFieldsErrors(null);
        }

        //
        // SINGLE CLICK EDITING
        //
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            _selectedRowAndColumn.OnMouseDoubleClick(dep);
        }        
        
        private void AddStateClick(object sender, RoutedEventArgs e)
        {
            var stateNewName = SelectStateName();
            if (stateNewName == null)
                return;
            var ads = new Connector
                {
                    Id = _connectorList.Count == 0 ? 0 : _connectorList.Select(x => x.Id).Max() + 1,
                    Order = _connectorList.Count == 0 ? 0 : _connectorList.Select(x => x.Order).Max() + 1,
                    Name = stateNewName
                };
            _connectorList.Add(ads);
            ShowData();
        }

        private void AddVariableClick(object sender, RoutedEventArgs e)
        {
            var x = new Selector(SelectedType.Variable);
            x.ShowDialog();
            var selectedVariable = x.GetSelectedItemId();
            if (selectedVariable == Guid.Empty)
                return;
            if (_usedVariables.Contains(selectedVariable))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorVariableIsAlreadyExists);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            _usedVariables.Add(selectedVariable);
            ShowData();
        }

        private void RemoveVariableClick(object sender, RoutedEventArgs e)
        {
            var variables = _assignedAccessDescriptor.GetAllUsedVariables();
            var selectedCellIndex = _selectedRowAndColumn.GetSelectedCellIndex();
            if (selectedCellIndex < _additionalColumnsCount || selectedCellIndex - 1 > variables.Count())
                return;
            var varId = _usedVariables[selectedCellIndex - _additionalColumnsCount];
            var selectedVar = Profile.VariableStorage.GetVariableById(varId);

            if (MessageBox.Show(LanguageManager.GetPhrase(Phrases.EditorMessageRemoveVariableFromAccessDescriptor)+" '" + selectedVar.Name + "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo,MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                _usedVariables.Remove(selectedVar.Id);
                foreach (var state in _connectorList)
                    _localFormulaKeeper.RemoveFormulaByVariableIdAndOwnerId(_assignedAccessDescriptor.GetId(), selectedVar.Id, state.Id);
                ShowData();
            }
        }

        private void StatesGridGotFocus(object sender, RoutedEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            _selectedRowAndColumn.OnMouseDoubleClick(dep);
        }

        private void RemoveStateClick(object sender, RoutedEventArgs e)
        {
            var rowIndex = _selectedRowAndColumn.GetSelectedRowIndex();
            if (rowIndex == -1)
                return;
            var stateName = (string)_dataTable.Rows[rowIndex].ItemArray[_stateNameColumnIndex];
            var stateId = GetStateIdByName(stateName);
            if (MessageBox.Show(LanguageManager.GetPhrase(Phrases.EditorMessageRemoveStateFromAccessDescriptor) + " '" + stateName + "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _connectorList.RemoveAll(x => x.Id == stateId);
                foreach (var usedVariable in _usedVariables)
                    _localFormulaKeeper.RemoveFormulaByVariableIdAndOwnerId(_assignedAccessDescriptor.GetId(), usedVariable, stateId);
                ShowData();
            }
        }

        private void RenameStateClick(object sender, RoutedEventArgs e)
        {
            var rowIndex = _selectedRowAndColumn.GetSelectedRowIndex();
            if (rowIndex == -1)
                return;
            var stateOldName = (string)_dataTable.Rows[rowIndex].ItemArray[_stateNameColumnIndex];

            var stateNewName = SelectStateName();
            if (stateNewName == null)
                return;
            _connectorList.Single(x => x.Name == stateOldName).Name = stateNewName;
            ShowData();
        }
        /// <summary>
        /// Показать диалог ввода нового имени состояния
        /// </summary>
        /// <returns>имя состояния или null, если имя не выбрано</returns>
        private string SelectStateName()
        {
        loop:
            var it = new InputString(LanguageManager.GetPhrase(Phrases.EditorMessageInputStateName));
            if (it.ShowDialog() != true)
                return null;
            var stateName = it.GetText();
            if (GetStateIdByName(stateName) != -1)
            {
                MessageBox.Show(LanguageManager.GetPhrase(Phrases.EditorMessageStateNameIsAlreadyExist),
                                LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                goto loop;
            }
            return stateName;
        }
        /// <summary>
        /// Получить id состояния по имени
        /// </summary>
        /// <param name="name">Имя состояния</param>
        /// <returns>id состояния. -1, если не найдено</returns>
        private int GetStateIdByName(string name)
        {
            foreach (var s in _connectorList.Where(s => s.Name == name))
                return s.Id;
            return -1;
        }
        /// <summary>
        /// Установка значения по-умолчанию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectDefaultStateClick(object sender, RoutedEventArgs e)
        {
            var rowIndex = _selectedRowAndColumn.GetSelectedRowIndex();
            if (rowIndex == -1)
                return;
            var stateId = _connectorList[rowIndex].Id;
            _defaultState = stateId == _defaultState ? -1 : stateId;
            ShowData();
        }

        /// <summary>
        /// Сохранить изменения из внутренних структур данных редактора в AccessDescriptor
        /// </summary>
        private void ApplyInternalStructuresDataChangesToAccessDescriptor()
        {
            _assignedAccessDescriptor.OverwriteUsedVariables(_usedVariables);
            _assignedAccessDescriptor.OverwriteStates(_connectorList.ToList());
            _assignedAccessDescriptor.OverwriteFormulaKeeper(_localFormulaKeeper);
            if (_assignedAccessDescriptor is IDefautValueAbility)
            {
                if (_defaultState == -1)
                    ((IDefautValueAbility)_assignedAccessDescriptor).UnAssignDefaultStateId();
                else
                    ((IDefautValueAbility)_assignedAccessDescriptor).AssignDefaultStateId(_defaultState);
            }
        }
        /// <summary>
        /// Сохранить изменения, вносимые в Grid во внутренние структуры данных
        /// </summary>
        private void ApplyGridChangesToInternalDataStructures()
        {
            _localFormulaKeeper.ClearAll();
            for (var stateIndex = 0; stateIndex < _dataTable.Rows.Count; stateIndex++)
            {
                var arr = _dataTable.Rows[stateIndex].ItemArray.ToList();
                var stateNewName = (string)_dataTable.Rows[stateIndex].ItemArray[_stateNameColumnIndex];
                _connectorList[stateIndex].Name = stateNewName;
                // Удаляем колонку State и DefaultState
                arr.RemoveRange(0, _additionalColumnsCount);
                for (var varIndex = 0; varIndex < arr.Count; varIndex++)
                    _localFormulaKeeper.StoreVariableFormula(arr[varIndex] is string ? (string)arr[varIndex] : "", _assignedAccessDescriptor.GetId(), _usedVariables[varIndex], _connectorList[stateIndex].Id);
            }
        }
    }
}
