using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.ControlProcessorEditors;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorPanels;
using FlexRouter.EditorsUI.AccessDescriptorsEditor;
using FlexRouter.EditorsUI.ControlProcessorEditors;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.EditorsUI.PanelEditors;
using FlexRouter.EditorsUI.VariableEditors;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Helpers;
using FlexRouter.Localizers;

//  ***************************** Проблемы профиля:
// Ответчик не работает. Нет работы с окнами
// Назначаем СПУ.Режим, на назначении последнего тумблера удаляем. Не получается. Писать почему.
//      Bug: бага в тушке. Если включить ввод ЗК на ПН-5, а затем выключить стабилизацию крена будут гореть и Сброс программы и ввод ЗК
// Катет, К1Г1К2Г2 - нужно питание, а то постоянно горит
//  ***************************** Пока не подтвердилось
// Автомат тяги работает не корректно. Не автоматит, не устанавливает в 2 "режим 2"
// Стабилизация по высоте включается, а лампа не загорается. Нужно разбираться
//  ***************************** Сделать
//      НВУ. Не верны формулы. В точке 360 "точно" ведёт себя странно. Нужно разбираться.
//      Возможность втыкать и вытыкать железо во время работы роутера
//      Вынести Repeater в AccessDescriptor (для Button вкл/выкл, для Range всегда вкл)
//          Или режим удержания: приводим состояние переменных в соответстве с состоянием контролов. Тогда повторитель не нужен в Button, а в Range нужен всегда
//      Проверить корректность срабатывания "данные изменились, сохранить?" в переменных, AD, CP
//      public void OverwriteFormulaKeeper(FormulaKeeper formulaKeeper) // Это нужно или можно удалить?
//      Контроль формул:
//          Не позволять сохранять AD, если в формуле синтаксическая ошибка
//          Если модули (dll) недоступны, в переменных показывать N/A и формулы не считать (для этого в обработке переменных нужно проверять, что модули загружены)
//      Управление профилем
//          Бэкап и ротация изменений профилей
//      Тест роутера без железа:
//          Горячие клавиши Ctrl+, Ctrl-, Ctrl1...9 для тестирования работоспособности роутера
//          Горячие клавиши остановить/запустить роутер
//          Для тестирования роутера без железа в CP добавить контрол: «вывод на индикатор/лампу» со значением N/A, если самолёт не загружен или формула неверна
//      Калькулятор. X:1 = X-1 (НВУ.ЗПУ1)

//      Железо:
//          Нужно сделать маски. Каждое железо говорит, что для индикаторов мне нужен только модуль, а номер контрола нет. И роутер сам удалит ненужные упраляющие элементы и будет укорачивать строку Arcc:xxx|Indicator|1
//          Поддержка L2/F2
//          Реализовать разовый дамп осей при старте роутера (это возможно?)
//          В джойстике поддержать все оси и хатку
//      AxisMultistate
//      Редактор для AxisMultistate
//      Что делать, если при загрузке профиля будет исключение?
//          Обернуть загрузку и сохранение в try/catch
//      Реализовать функции (FromBCD, ToBCD, получить дробную часть)
//      Поправить выравнивание, чтобы не образовывались ScrollBar'ы по-умолчанию
//      Refactoring: Вынести ускоряющийся Repeater из ButtonPlusMinisConrolProcessor и сделать его общим
//      Refactoring: Нужно унести управление StopSearch из MainWindow в CPEditor
//      Проверить работу с профилем на многопоточность. Обращение к AD, CP, VAR только через методы класса Profile (даже внутри этого класса) при сохранении загрузке. И всё через lock
//      Bug: Если удалить CP, то остаётся выбораннам пункт (например, "Лампа", но нет селекта в дереве(?)), но по кнопке Create ничего не происходит
//      Работа с окнами
//          Редактирование окон
//          Редактирование описателя "Клики мышью (Multistate)"
//          Редактирование описателя "Клики мышью (Range)"
//      Bug: биндинг не работает, если в названии переменной (колонки) "плохой" символ. Точка, слэш
//  ***************************** Не срочно (или не нужно):
//      При изменении имени переменной предупреждать, что нужно обновить данные в AD, если открытый AD использует эту переменную (узнать в FormulaKeeper)
//      Копия переменной, AccessDescriptor
//      После загрузки роутера на 100 индикаторов и все лампы подключенных модулей послать "выключить"
//	    В CP принимать нажатия клавиш, а при отжатии учитывать ID, чтобы выставлять Default. Тогде не будет перескакивать

//	    Ввод точки/запятой в RangeEditor проверить и сделать возможным и запятую и точку
//      Сделать в дескрипторах и формулах единообразно: запретить всё, кроме точки как разделителя или понимать текущий CulturalInfo
//          string s = someStringWithNum.Replace(",", ".");
//          CultureInfo ci = new CultureInfo("en-US");
//          double d = double.Parce(s, ci.NumberFormatInfo);
//     или
//          Double.TryParse(str_1, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
//
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
using Microsoft.Win32;
using Clipboard = System.Windows.Clipboard;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using Panel = FlexRouter.ProfileItems.Panel;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using TextBox = System.Windows.Controls.TextBox;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

namespace FlexRouter
{
    public enum TreeItemType
    {
        Panel,
        AccessDescriptor,
        Variable,
        ControlProcessor
    }
    public enum RouterState
    {
        Running,
        Paused,
        Stopped,
    }
    class TreeItem
    {
        public TreeItemType Type;
        public string Name;
        public string FullName;
        public object Object;
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private RouterState _routerState = RouterState.Stopped;
        private readonly Core _routerCore = new Core();
        readonly DispatcherTimer _timer;
        readonly DispatcherTimer _timer2;
        private readonly Calculator _calculator = new Calculator();

        public MainWindow()
        {
            InitializeComponent();
            SetTitle();
            LanguageManager.Initialize();
            ApplicationSettings.LoadSettings();
            InitializeCalculator();
            FillSelectLanguageCombobox();
            if (!string.IsNullOrEmpty(ApplicationSettings.DefaultLanguage))
                LanguageManager.LoadLanguage(ApplicationSettings.DefaultLanguage);
            Localize();
            if (LoadProfile(ApplicationSettings.DefaultProfile))
            {
                SetTitle(ApplicationSettings.DefaultProfile);
                FillSelectProfileCombobox(ApplicationSettings.DefaultProfile);
            }

            _createVariable.IsEnabled = false;
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimedEvent;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Start();

            _timer2 = new DispatcherTimer();
            _timer2.Tick += OnTimedEvent2;
            _timer2.Interval = new TimeSpan(0, 0, 0, 3);
            _timer2.Start();
        }

        private static ListViewItem CreateListViewItem(string text, System.Drawing.Bitmap iconBitmap)
        {
            var item = new ListViewItem();

            if (iconBitmap != null)
            {
                // create stack panel
                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                // create Image
                var image = new Image();
                var bc = new WpfBitmapConverter();
                var icon =
                    (ImageSource)bc.Convert(iconBitmap, typeof(ImageSource), null, CultureInfo.InvariantCulture);

                image.Source = icon;
                image.Width = 16;
                image.Height = 16;
                // Label
                var lbl = new Label { Content = text };

                // Add into stack
                stack.Children.Add(image);
                stack.Children.Add(lbl);

                // assign stack to header
                item.Content = stack;
            }
            else
                item.Content = text;
            return item;
        }
        object _locker = new object();
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void GetSubsystemsStatus()
        {
            lock (_locker)
            {
                //var subsystemStatuses = VariableManager.GetInitializationStatus();
                //_statusList.Items.Clear();
                //// Добавляем строки. Первая колонка - наименование состояния, затем формулы для переменных
                //foreach (var s in subsystemStatuses)
                //    _statusList.Items.Add(CreateListViewItem(s.IsOk ? s.System : s.System + " - " + s.ErrorMessage, s.IsOk ? Properties.Resources.Ok : Properties.Resources.Fail));

                if (!Problems.ProblemListWasChanged)
                    return;
                _statusList.Items.Clear();
                var problems = Problems.GetProblemList();
                foreach (var s in problems)
                    _statusList.Items.Add(CreateListViewItem(s.IsFixed ? s.Name : s.Name + " - " + s.Description, s.IsFixed ? Properties.Resources.Ok : Properties.Resources.Fail));
            }
        }

        private void SetTitle(string profileName = null)
        {
            Title = "Flex Router v" + Assembly.GetExecutingAssembly().GetName().Version + (string.IsNullOrEmpty(profileName) ? "" : " - " + profileName);
        }
        private void Localize()
        {
            _accessDescriptorsToCreateList.Text = string.Empty;
            _controlProcessorsList.Text = string.Empty;
            _accessMethods.Text = string.Empty;
            
            _removeAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            foreach (var child in _accessDescriptorPanel.Children)
            {
                var editor = child as IEditor;
                if(editor != null)
                    editor.Localize();
            }
            foreach (var child in _controlProcessorsPanel.Children)
            {
                var editor = child as IEditor;
                if (editor != null)
                    editor.Localize();
            }
            foreach (var child in _variablesPanel.Children)
            {
                var editor = child as IEditor;
                if (editor != null)
                    editor.Localize();
            }
            tabInformation.Header = LanguageManager.GetPhrase(Phrases.TabInfo);
            tabSettings.Header = LanguageManager.GetPhrase(Phrases.TabSettings);
            tabAccessDescriptors.Header = LanguageManager.GetPhrase(Phrases.TabAccessDescriptors);
            tabVariables.Header = LanguageManager.GetPhrase(Phrases.TabVariables);
            tabControlProcessors.Header = LanguageManager.GetPhrase(Phrases.TabControlProcessors);
            tabFormulaEditor.Header = LanguageManager.GetPhrase(Phrases.TabFormulaEditor);
            _removeAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _renameProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRename);
            _createAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createNewProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _saveAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _saveControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _saveVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _importProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonImport);
            _exportProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonExport);
            _importProfileAndKeepAssignments.Content = LanguageManager.GetPhrase(Phrases.CommonButtonImportAndKeepAssignments);
            _selectProfileLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelProfile);
            _selectLanguageLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelLanguage);
            _formulaResultLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelFormulaResult) + " (Dec/Hex/Bool)";
            _copyFormulaToClipboard.Content = LanguageManager.GetPhrase(Phrases.CommonLabelCopyToClipboard);
            _addVarToFormula.Content = LanguageManager.GetPhrase(Phrases.CommonLabelCopyVariableToFormula);
            _errorLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelError);
            _routerStateLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelRouterState);
            _connectedHardwareLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelConnectedHardwareList);
            _problemsLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelProblemsList);
            _lastEventLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelLastHardwareEvent);
            _profileManagementGroup.Header = LanguageManager.GetPhrase(Phrases.CommonProfileManagement);
            _turnControlsSynchronizationOffLabel.Content = LanguageManager.GetPhrase(Phrases.SettingsTurnControlsSynchronizationOff);
            LocalizeRouterState();
        }
        private void OnTimedEvent2(object sender, EventArgs e)
        {
            GetSubsystemsStatus();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            ProcessMessages();
            CalculateTestFormulaAndShowErrors();
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_routerCore.IsWorking())
                _routerCore.Stop(false);
            _timer.Stop();
            VariableManager.Stop();
        }
        private void ProcessMessages()
        {
            var messages = Messenger.GetMessages();
            foreach (var message in messages)
            {
                if (message.MessageType == MessageToMainForm.RouterStarted)
                    _routerState = RouterState.Running;
                    LocalizeRouterState();
                if (message.MessageType == MessageToMainForm.RouterStopped)
                    _routerState = RouterState.Stopped;
                    LocalizeRouterState();
                if (message.MessageType == MessageToMainForm.RouterPaused)
                    _routerState = RouterState.Paused;
                    LocalizeRouterState();    
                if (message.MessageType == MessageToMainForm.ClearConnectedDevicesList)
                    ConnectedDevicesList.Items.Clear();
                if (message.MessageType == MessageToMainForm.AddConnectedDevice)
                    ConnectedDevicesList.Items.Add(((TextMessage) message).Text);
                if (message.MessageType == MessageToMainForm.NewHardwareEvent)
                {
                    var direction = string.Empty;
                    var evButton = (((ControlEventBase)((ObjectMessage) message).AnyObject)) as ButtonEvent;
                    if(evButton!=null)
                        direction = evButton.IsPressed ? "vvv" : "^^^";
                    var evEncoder = (((ControlEventBase)((ObjectMessage) message).AnyObject)) as EncoderEvent;
                    if(evEncoder!=null)
                        direction = evEncoder.RotateDirection ? ">>>" : "<<<";
                    _incomingEvent.Text = ((ControlEventBase)((ObjectMessage)message).AnyObject).Hardware.GetHardwareGuid() + direction;
                    foreach (var child in _controlProcessorsPanel.Children) 
                    {
                        if(child is IControlProcessorEditor)
                            ((IControlProcessorEditor)child).OnNewControlEvent(((ControlEventBase)((ObjectMessage)message).AnyObject));
                    }
                }
                    
            }
        }
        private void LocalizeRouterState()
        {
            switch (_routerState)
            {
                    case RouterState.Running:
                    Output.Text = LanguageManager.GetPhrase(Phrases.CommonStateRunning);
                    break;
                    case RouterState.Paused:
                    Output.Text = LanguageManager.GetPhrase(Phrases.CommonStatePaused);
                    break;
                default:
                    Output.Text = LanguageManager.GetPhrase(Phrases.CommonStateStopped);
                    break;
            }
        }
        #region AccessDescriptorEditor
        private readonly TreeViewHelper _accessDescriptorTreeHelper = new TreeViewHelper();
        private void AccessDescriptorsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsNeedToChangeItemInEditor(_accessDescriptorsTree, _accessDescriptorPanel, e, _accessDescriptorTreeHelper))
                ChangeAccessDescriptoriInEditor();
        }
        private void ChangeAccessDescriptoriInEditor()
        {
            var treeSelectedItem = GetTreeSelectedItem(_accessDescriptorsTree);
            if (treeSelectedItem == null)
                return;
            _accessDescriptorPanel.Children.Clear();
            if (treeSelectedItem.Type == TreeItemType.Panel)
            {
                var panel = new PanelProperties((Panel) treeSelectedItem.Object, false);
                DockPanel.SetDock(panel, Dock.Top);
                _accessDescriptorPanel.Children.Add(panel);
            }
            else
            {
                var ad = Profile.GetAccessDesciptorById(((DescriptorBase)treeSelectedItem.Object).GetId());
                ShowAccessDescriptorEditors(ad);
            }
        }
        private void SaveAccessDescriptorClick(object sender, RoutedEventArgs e)
        {
            OnAnySaveButtonClicked(_accessDescriptorPanel);
        }
        private void AccessDescriptorToCreateDropDownOpened(object sender, EventArgs e)
        {
            FillAccessDescriptorsToCreateList();
        }
        private void FillAccessDescriptorsToCreateList()
        {
            _accessDescriptorsToCreateList.Items.Clear();

            IEnumerable<object> descriptorsList = new List<object>
            {
                new DescriptorValue(),
                new DescriptorRange(),
                new DescriptorIndicator(),
                new DescriptorBinaryOutput(),
                new RangeUnion(),
                new Panel()
            };
            foreach (var listItem in descriptorsList)
            {
                var comboboxItem = new ComboBoxItem { Content = (listItem is DescriptorBase ? ((IAccessDescriptor)listItem).GetDescriptorType() : ((Panel)listItem).GetName()), Tag = listItem};
                _accessDescriptorsToCreateList.Items.Add(comboboxItem);
            }
        }
        private void OnAccessDescriptorsToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createVariable.IsEnabled = !string.IsNullOrEmpty(((TextBox)sender).Text);
        }
        private void CreateAccessDescriptorClick(object sender, RoutedEventArgs e)
        {
            var accessDescriptor = GetObjectToCreateFromCombobox(_accessDescriptorsToCreateList, _variablesPanel);
            if ((accessDescriptor as DescriptorBase)!= null)
                //ToDo: Добавить IsNew
                ShowAccessDescriptorEditors((DescriptorBase)accessDescriptor);
            if ((accessDescriptor as Panel) != null)
                ShowPanel((Panel)accessDescriptor, _accessDescriptorPanel, true);
            FillAccessDescriptorsToCreateList();

        }
        private void ShowAccessDescriptorEditors(DescriptorBase ad)
        {
            string selectedItemPanelName = GetSelectedItemPanelName(_accessDescriptorsTree);
            _accessDescriptorPanel.Children.Clear();
            var editors = new List<UserControl>();
            editors.Add(new DescriptorCommonEditor(ad, selectedItemPanelName));
            if (ad is DescriptorValue)
            {
                editors.Add(new DescriptorValueEditor((DescriptorValue)ad, true));
//                editors.Add(new RepeaterEditor((DescriptorMultistateBase)ad));
            }
            if (ad is DescriptorBinaryOutput)
            {
                editors.Add(new DescriptorFormulaEditor((DescriptorOutputBase)ad));
            }
            if (ad is DescriptorIndicator)
            {
                editors.Add(new DescriptorDecPointEditor((DescriptorIndicator)ad));
                editors.Add(new DescriptorFormulaEditor((DescriptorOutputBase)ad));
            }
            if (ad is DescriptorRange)
            {
                editors.Add(new DescriptorValueEditor((DescriptorMultistateBase)ad, false));
//                editors.Add(new RepeaterEditor((DescriptorMultistateBase)ad));
                editors.Add(new DescriptorRangeEditor((DescriptorRange)ad));
            }
            if (ad is RangeUnion)
            {
                editors.Add(new RangeUnionEditor((RangeUnion)ad));
//                editors.Add(new RepeaterEditor((DescriptorMultistateBase)ad));
            }
            for (var i = 0; i < editors.Count; i++)
            {
                DockPanel.SetDock(editors[i], i == 0 ? Dock.Top : Dock.Bottom);
                _accessDescriptorPanel.Children.Add(editors[i]);
            }
        }
        private void RemoveAccessDescriptorClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_accessDescriptorsTree);
            if (item == null)
            {
                return;
            }
            if (item.Type == TreeItemType.Panel)
            {
                RemovePanel((Panel)item.Object);
                return;
            }
            if (
                MessageBox.Show(
                    LanguageManager.GetPhrase(Phrases.EditorMessageRemoveAccessDescriptor) + " '" + item.FullName +
                    "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo,
                    MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {

                _accessDescriptorPanel.Children.Clear();
                Profile.RemoveAccessDescriptor(((DescriptorBase)item.Object).GetId());
                Profile.SaveCurrentProfile();
                RenewTrees();
            }
        }
        private void RemovePanel(Panel panel)
        {
            if (Profile.IsPanelInUse(panel.Id))
            {
                MessageBox.Show(
                    LanguageManager.GetPhrase(Phrases.EditorMessageCantRemoveNotEmptyPanel),
                    LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(
                LanguageManager.GetPhrase(Phrases.EditorMessageRemovePanel) + " '" + panel.Name +
                "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo,
                MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                Profile.RemovePanel(panel.Id);
                Profile.SaveCurrentProfile();
                RenewTrees();
            }
        }
        #endregion
        #region VariableEditor
        private readonly TreeViewHelper _variableTreeHelper = new TreeViewHelper();
        private void VariablesTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsNeedToChangeItemInEditor(_variablesTree, _variablesPanel, e, _variableTreeHelper))
                ChangeVariableInEditor();
        }
        private void ChangeVariableInEditor()
        {
            _variablesPanel.Children.Clear();
            if (((TreeViewItem)_variablesTree.SelectedItem).Name == TreeItemType.Panel.ToString())
            {
                IEditor ie = new PanelProperties((Panel)((TreeViewItem)_variablesTree.SelectedItem).Tag, false);
                DockPanel.SetDock((UserControl)ie, Dock.Top);
                _variablesPanel.Children.Add((UserControl)ie);
            }
            else
            {
                var selectedVariable = (IVariable)((TreeViewItem)_variablesTree.SelectedItem).Tag;
                ShowVariable(selectedVariable, false);
            }
        }
        private string GetSelectedItemPanelName(TreeView tree)
        {
            if (tree.SelectedItem != null)
            {
                if (((TreeViewItem)tree.SelectedItem).Name == TreeItemType.Panel.ToString())
                    return ((Panel)((TreeViewItem)tree.SelectedItem).Tag).Name;
                var selectedItem = (TreeViewItem) tree.SelectedItem;
                Panel panel = null;
                if (selectedItem.Tag is IVariable)
                {
                    var selectedVariable = (IVariable)((TreeViewItem)tree.SelectedItem).Tag;
                    panel = Profile.GetPanelById(selectedVariable.PanelId);
                }
                if (selectedItem.Tag is DescriptorBase)
                {
                    var selectedDescriptor = (DescriptorBase)((TreeViewItem)tree.SelectedItem).Tag;
                    panel = Profile.GetPanelById(selectedDescriptor.GetAssignedPanelId());
                }
                if (panel != null)
                    return panel.Name;
            }
            return null;
        }
        private void ShowVariable(IVariable variable, bool isNew)
        {
            string selectedItemPanelName = GetSelectedItemPanelName(_variablesTree);
            if (variable == null)
                return;
            _variablesPanel.Children.Clear();

            var editors = new List<UserControl>();

            if (variable is MemoryPatchVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderMemoryPatch, isNew, selectedItemPanelName));
                editors.Add(new MemoryPatchVariableEditor(variable));
                editors.Add(new VariableSizeEditor(variable as IMemoryVariable));
                editors.Add(new VariableValueEditor(variable));
                editors.Add(new VariableEditorDescription(variable));
            }
            if (variable is FsuipcVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderFsuipc, isNew, selectedItemPanelName));
                editors.Add(new FsuipcVariableEditor(variable));
                editors.Add(new VariableSizeEditor(variable as IMemoryVariable));
                editors.Add(new VariableValueEditor(variable));
                editors.Add(new VariableEditorDescription(variable));
            }
            if (variable is FakeVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderFakeVariable, isNew, selectedItemPanelName));
                editors.Add(new VariableSizeEditor(variable as IMemoryVariable));
                editors.Add(new VariableValueEditor(variable));
                editors.Add(new VariableEditorDescription(variable));
            }
            for (var i = 0; i < editors.Count; i++)
            {
                DockPanel.SetDock(editors[i], i == 0 ? Dock.Top : Dock.Bottom);
                _variablesPanel.Children.Add(editors[i]);
            }
        }
        private void ShowVariablesTree(TreeView tree)
        {
            var vtk = new TreeViewStateKeeper();
            vtk.RememberState(ref tree);
            var panels = Profile.GetPanelsList();
            tree.Items.Clear();
            foreach (var panel in panels)
            {
                var treeRootItem = new TreeViewItem { Tag = panel, Name = TreeItemType.Panel.ToString(), Header = panel.Name };

                var ad = Profile.GetSortedVariablesListByPanelId(panel.Id);
                foreach (var adesc in ad)
                {

                    var icon = GetIcon(TreeItemType.Variable, adesc.Id);
                    var treeAdItem = CreateTreeViewItem(adesc.Name, adesc, TreeItemType.Variable, icon);
                    treeAdItem.Name = TreeItemType.Variable.ToString();
                    
//                    var treeAdItem = new TreeViewItem { Tag = adesc, Name = TreeItemType.Variable.ToString(), Header = adesc.Name };
                    treeRootItem.Items.Add(treeAdItem);
                }
                tree.Items.Add(treeRootItem);
            }
            vtk.RestoreState(ref tree);
        }

        private void ShowPanel(Panel item, StackPanel panel, bool isNew)
        {
            if (item == null)
                return;
            panel.Children.Clear();

            var editors = new List<UserControl> {new PanelProperties(item, isNew)};
            for (var i = 0; i < editors.Count; i++)
            {
                DockPanel.SetDock(editors[i], i == 0 ? Dock.Top : Dock.Bottom);
                panel.Children.Add(editors[i]);
            }
        }
        private void SaveVariableClick(object sender, RoutedEventArgs e)
        {
            OnAnySaveButtonClicked(_variablesPanel);
        }
        private void VariableToCreateDropDownOpened(object sender, EventArgs e)
        {
            FillVariablesToCreateList();
        }
        private void FillVariablesToCreateList()
        {
            _accessMethods.Items.Clear();

            IEnumerable<object> variablesList = new List<object>
            {
                new MemoryPatchVariable(),
                new FsuipcVariable(),
                new FakeVariable(),
                new Panel()
            };
            foreach (var variable in variablesList)
            {
                var item = new ComboBoxItem { Content = (variable is IVariable ? ((IVariable)variable).GetName() : ((Panel)variable).GetName()), Tag = variable };
                _accessMethods.Items.Add(item);
            }
        }
        private void OnVariablesToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createVariable.IsEnabled = !string.IsNullOrEmpty(((TextBox)sender).Text);
        }
        private void CreateVariableClick(object sender, RoutedEventArgs e)
        {
            var variable = GetObjectToCreateFromCombobox(_accessMethods, _variablesPanel);
            if((variable as IVariable)!=null)
                ShowVariable((IVariable)variable, true);
            if((variable as Panel)!=null)
                ShowPanel((Panel)variable, _variablesPanel, true);
            FillVariablesToCreateList();
        }
        private void RemoveVariableClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_variablesTree);
            if (item == null)
                return;
            if (item.Type == TreeItemType.Panel)
            {
                RemovePanel((Panel)item.Object);
                return;
            }
            var variableLinks = GlobalFormulaKeeper.Instance.GetVariableOwnersByVariableId(((IVariable)item.Object).Id);
            if (variableLinks.Length != 0)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageCantRemoveVariableInUse) + "\n\n";
                foreach (var vl in variableLinks)
                {
                    var ad = Profile.GetAccessDesciptorById(vl);
                    if (ad!=null)
                    {
                        var adName = Profile.GetPanelById(ad.GetAssignedPanelId()).GetName() + "." + ad.GetName();
                        message += LanguageManager.GetPhrase(Phrases.EditorAccessDescriptor) + " '" + adName + "'" + "\n";
                    }
                    var panel = Profile.GetPanelById(vl);
                    if (panel !=null)
                    {
                        var panelName = panel.GetName();
                        message += LanguageManager.GetPhrase(Phrases.EditorPanel) + " '" + panelName + "'" + "\n";
                    }
                }
                MessageBox.Show(message,
                    LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(
                    LanguageManager.GetPhrase(Phrases.EditorMessageRemoveVariable) + " '" + item.FullName +
                    "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo,
                    MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                _variablesPanel.Children.Clear();
                Profile.RemoveVariable(((IVariable)item.Object).Id);
                Profile.SaveCurrentProfile();
                RenewTrees();
            }
        }
        #endregion
        #region ControlProcessorEditorsMethods
        private readonly TreeViewHelper _controlProcessorTreeHelper = new TreeViewHelper();
        private void OnControlProcessorsToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createControlProcessor.IsEnabled = !string.IsNullOrEmpty(((TextBox)sender).Text);
        }
        private void ControlProcessorsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsNeedToChangeItemInEditor(_controlProcessorsTree, _controlProcessorsPanel, e, _controlProcessorTreeHelper))
                ChangeControlProcessorInEditor();
        }
        private void FillCreateControlProcessorList()
        {
            if (_controlProcessorsTree.SelectedItem == null)
                return;
            if (((TreeViewItem) _controlProcessorsTree.SelectedItem).Name == TreeItemType.Panel.ToString())
                return;

            var accessDescriptor = (DescriptorBase)((TreeViewItem)_controlProcessorsTree.SelectedItem).Tag;
            _controlProcessorsList.Items.Clear();

            IEnumerable<IControlProcessor> processorsList = new List<IControlProcessor>
            {
                new IndicatorProcessor(accessDescriptor),
                new LampProcessor(accessDescriptor),
                new ButtonProcessor(accessDescriptor),
                new EncoderProcessor(accessDescriptor),
                new ButtonPlusMinusProcessor(accessDescriptor),
                new ButtonBinaryInputProcessor(accessDescriptor),
                new AxisRangeProcessor(accessDescriptor)
            };
            var controlProcessors = processorsList.Where(x => x.IsAccessDesctiptorSuitable(accessDescriptor)).Select(x => x).ToList();
            foreach (var controlProcessor in controlProcessors)
            {
                var item = new ComboBoxItem {Content = controlProcessor.GetName(), Tag = controlProcessor};
                _controlProcessorsList.Items.Add(item);
            }
            if (_controlProcessorsList.Items.Count == 1)
                _controlProcessorsList.SelectedItem = _controlProcessorsList.Items[0];
        }
        private void ShowControlProcessor(IControlProcessor controlProcessor)
        {
            if (controlProcessor == null)
                return;
            _controlProcessorsPanel.Children.Clear();

            var editors = new List<UserControl>();

            if (controlProcessor is ButtonProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Button));
                editors.Add(new ButtonToggleEmulatorEditor(controlProcessor));
                editors.Add(new ButtonRepeaterEditor(controlProcessor as IRepeater));
            }
            if (controlProcessor is ButtonPlusMinusProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Button));
                editors.Add(new ButtonRepeaterEditor(controlProcessor as IRepeater));
            }

            if (controlProcessor is AxisRangeProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Axis));
//                editors.Add(new AssignEditorForOutput(controlProcessor, true, HardwareModuleType.Axis));
                editors.Add(new AxisSetLimitsEditor(controlProcessor));
            }
            if (controlProcessor is EncoderProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Encoder));
            }
            if (controlProcessor is IndicatorProcessor)
            {
                editors.Add(new AssignEditorForOutput(controlProcessor, true, HardwareModuleType.Indicator));
            }
            if (controlProcessor is LampProcessor)
            {
                editors.Add(new AssignEditorForOutput(controlProcessor, true, HardwareModuleType.BinaryOutput));
            }
            if (controlProcessor is ButtonBinaryInputProcessor)
            {
                editors.Add(new AssignEditorForBinaryInput(controlProcessor, false, HardwareModuleType.Button));
            }
            for (var i = 0; i < editors.Count; i++)
            {
                DockPanel.SetDock(editors[i], i == 0 ? Dock.Top : Dock.Bottom);
                _controlProcessorsPanel.Children.Add(editors[i]);
            }
        }
        private void ChangeControlProcessorInEditor()
        {
            var treeSelectedItem = GetTreeSelectedItem(_controlProcessorsTree);
            if (treeSelectedItem == null)
                return;

            _controlProcessorsPanel.Children.Clear();

            if (treeSelectedItem.Type == TreeItemType.Panel)
            {
                _controlProcessorsPanel.Children.Add(new PanelProperties((Panel)treeSelectedItem.Object, false));
            }
            else
            {
                FillCreateControlProcessorList();
                var descriptor = (DescriptorBase)treeSelectedItem.Object;
                var controlProcessor = Profile.GetControlProcessorByAccessDescriptorId(descriptor.GetId());
                ShowControlProcessor(controlProcessor);
            }
            // ToDo: нужно не отсюда вызывать, а давать CP панелям знать, что выбран другой узел дерева и нужно завершать поиск
            HardwareManager.StopComponentSearch();
        }
        private void CreateControlProcessorClick(object sender, RoutedEventArgs e)
        {
            var treeSelectedItem = GetTreeSelectedItem(_controlProcessorsTree);

            if (treeSelectedItem == null)
                return;
            if (treeSelectedItem.Type == TreeItemType.Panel)
                return;

            if(string.IsNullOrEmpty(_controlProcessorsList.Text))
                return;

            var item = _controlProcessorsList.SelectedItem as ComboBoxItem;
            if (item == null)
                return;
            var controlProcessor = (IControlProcessor)((ComboBoxItem) _controlProcessorsList.SelectedItem).Tag;

            if (controlProcessor != null)
            {
                var oldControlProcessorId = (((DescriptorBase)treeSelectedItem.Object)).GetId();
                if (Profile.GetControlProcessorByAccessDescriptorId(oldControlProcessorId) != null)
                    Profile.RemoveControlProcessor(oldControlProcessorId);
                Profile.RegisterControlProcessor(controlProcessor, oldControlProcessorId);
                var ad = Profile.GetAccessDesciptorById(oldControlProcessorId) as DescriptorMultistateBase;
                if (ad != null)
                {
                    var states = ad.GetStateDescriptors();
                    var cp = controlProcessor as IControlProcessorMultistate;
                    if(cp!=null)
                        cp.RenewStatesInfo(states);
                }
                ShowControlProcessor(controlProcessor);
                Profile.SaveCurrentProfile();
                FillCreateControlProcessorList();
            }
        }
        private void SaveControlProcrssorClick(object sender, RoutedEventArgs e)
        {
            OnAnySaveButtonClicked(_controlProcessorsPanel);
        }
        private void RemoveControlProcessorClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_controlProcessorsTree);
            if (item == null)
                return;
            if (item.Type == TreeItemType.Panel)
            {
                RemovePanel((Panel)item.Object);
                return;
            }
            if (
                MessageBox.Show(
                    LanguageManager.GetPhrase(Phrases.EditorMessageRemoveControlProcessor) + " '" + item.FullName +
                    "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo,
                    MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                _controlProcessorsPanel.Children.Clear();
                Profile.RemoveControlProcessor(((DescriptorBase)item.Object).GetId());
                Profile.SaveCurrentProfile();
                RenewTrees();
            }
        }
        private static TreeViewItem CreateTreeViewItem(string text, object connectedObject, TreeItemType treeItemType, System.Drawing.Bitmap iconBitmap)
        {
            var item = new TreeViewItem();

            if (iconBitmap != null)
            {
                // create stack panel
                var stack = new StackPanel {Orientation = Orientation.Horizontal};

                // create Image
                var image = new Image();
                var bc = new WpfBitmapConverter();
                var icon =
                    (ImageSource) bc.Convert(iconBitmap, typeof (ImageSource), null, CultureInfo.InvariantCulture);

                image.Source = icon;
                image.Width = 16;
                image.Height = 16;
                // Label
                var lbl = new Label {Content = text};

                // Add into stack
                stack.Children.Add(image);
                stack.Children.Add(lbl);

                // assign stack to header
                item.Header = stack;
            }
            else
                item.Header = text;
            item.Name = treeItemType.ToString();
            item.Tag = connectedObject;
            return item;
        }

        #endregion
        #region CommonEditorsMethods
        private bool IsNeedToChangeItemInEditor(TreeView tree, StackPanel panel, RoutedPropertyChangedEventArgs<object> e, TreeViewHelper treeHelper)
        {
            if (tree.SelectedItem == null)
                return false;
            var askUserToSaveVarBeforeSelectionChange = false;
            if (panel.Children.Count != 0)
                askUserToSaveVarBeforeSelectionChange = IsPanelDataChanged(panel);

            return !treeHelper.SelectionChanging(tree, e, askUserToSaveVarBeforeSelectionChange);
        }
        private void RenewTrees()
        {
            ShowTree(_accessDescriptorsTree, TreeItemType.AccessDescriptor);
            ShowVariablesTree(_variablesTree);
            ShowTree(_controlProcessorsTree, TreeItemType.ControlProcessor);
            ShowVariablesTree(_variablesForFormulaTree);
        }
        private void OnAnySaveButtonClicked(StackPanel panel)
        {
            // Перерисовать дерево, развернуть необходимый узел и выделить переменную/дексриптор/панель. Нужно при переезде из одной панели в другую
            if (panel.Children.Count == 0)
                return;
            var errors = string.Empty;
            foreach (var child in panel.Children)
            {
                var res = ((IEditor)child).IsCorrectData();
                if (!res.IsDataFilledCorrectly)
                    errors += res.ErrorsText;
            }
            if (!string.IsNullOrEmpty(errors))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageDataIsIncorrect) + errors;
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            _routerCore.Lock();
            foreach (var child in panel.Children)
                ((IEditor)child).Save();
            RenewTrees();
            Profile.SaveCurrentProfile();
            _routerCore.Unlock();
        }
        private bool IsPanelDataChanged(StackPanel panel)
        {
            bool isChanged = false;
            foreach (var child in panel.Children)
            {
                if (((IEditor) child).IsDataChanged())
                    isChanged = true;
            }
            return isChanged;
        }
        private TreeItem GetTreeSelectedItem(TreeView tree)
        {
            if (tree.SelectedItem == null)
                return null;
            var item = new TreeItem();
            var found = false;
            foreach (var type in (TreeItemType[])Enum.GetValues(typeof(TreeItemType)))
            {
                if (type.ToString() != ((TreeViewItem)tree.SelectedItem).Name)
                    continue;
                item.Type = type;
                found = true;
                break;
            }
            if (!found)
                return null;
            item.Object = ((TreeViewItem)tree.SelectedItem).Tag;
//           item.Name = (string)((TreeViewItem)tree.SelectedItem).Header;
            item.Name = GetTreeItemText(tree.SelectedItem as TreeViewItem);
            if (item.Type != TreeItemType.Panel)
            {
                var parentItem = ((TreeViewItem)((TreeViewItem)tree.SelectedItem).Parent);
                var parentText = GetTreeItemText(parentItem as TreeViewItem);
//                item.FullName = (string)parentItem.Header + "." + item.Name;
                item.FullName = parentText + "." + item.Name;
            }
            else
                item.FullName = item.Name;
            return item;
        }

        private string GetTreeItemText(TreeViewItem tvi)
        {
            if (tvi.Header is StackPanel)
            {
                if ((tvi.Header as StackPanel).Children.Count < 2)
                    return null;
                var item = ((StackPanel)tvi.Header).Children[1] as Label;
                if(item == null)
                return null;
                return (string)item.Content;
            }
            return (string) tvi.Header;

        }
        /// <summary>
        /// Получить из Combobox объект, который предстоит создать
        /// </summary>
        /// <param name="combobox">комбобокс</param>
        /// <param name="panel">панель, на которой будет создаваться объект</param>
        /// <returns></returns>
        private object GetObjectToCreateFromCombobox(ComboBox combobox, StackPanel panel)
        {
            if (string.IsNullOrEmpty(combobox.Text))
                return null;
            var askUser = false;
            if (panel.Children.Count != 0)
                askUser = IsPanelDataChanged(panel);
            if (askUser && MessageBox.Show(LanguageManager.GetPhrase(Phrases.MessageBoxUnsavedEditorData),
                                           LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader),
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return null;
            }

            var item = combobox.SelectedItem as ComboBoxItem;
            return item == null ? null : ((ComboBoxItem)combobox.SelectedItem).Tag;
        }
        static void ShowTree(TreeView tree, TreeItemType tit)
        {
            var vtk = new TreeViewStateKeeper();
            vtk.RememberState(ref tree);
            var panels = Profile.GetPanelsList();
            tree.Items.Clear();
            var adAll = Profile.GetSortedAccessDesciptorList();
            foreach (var panel in panels)
            {
                var treeRootItem = new TreeViewItem { Tag = panel, Name = TreeItemType.Panel.ToString(), Header = panel.Name };

                var ad = Profile.GetSortedAccessDesciptorListByPanelId(panel.Id);
                foreach (var adesc in ad)
                {
                    if (adesc.IsDependent())
                        continue;
                    var icon = GetIcon(tit, adesc.GetId());
                    var treeAdItem = CreateTreeViewItem(adesc.GetName(), adesc, tit, icon);
                    treeAdItem.Tag = adesc;
                    treeAdItem.Name = tit.ToString();

                    treeRootItem.Items.Add(treeAdItem);
                    if(tit == TreeItemType.ControlProcessor)
                        continue;
                    foreach (var a in adAll)
                    {
                        if(!a.IsDependent())
                            continue;
                        if(a.GetDependency().GetId() != adesc.GetId())
                            continue;
                        icon = GetIcon(tit, a.GetId());
                        var treeDependentItem = CreateTreeViewItem(Profile.GetPanelById(a.GetAssignedPanelId()).Name + "." + a.GetName(), a, tit, icon);
                        treeAdItem.Items.Add(treeDependentItem);
                    }
                }
                tree.Items.Add(treeRootItem);
            }
            vtk.RestoreState(ref tree);
        }

        private static System.Drawing.Bitmap GetIcon(TreeItemType tit, int itemId)
        {
            if (tit == TreeItemType.ControlProcessor)
            {
                var cp = Profile.GetControlProcessorByAccessDescriptorId(itemId);
                if (cp == null)
                    return Properties.Resources.ConnectedNot;
                var assignments = cp.GetAssignments();
                bool foundUnassigned = false;
                foreach (var assignment in assignments)
                {
                    if (string.IsNullOrEmpty(assignment.AssignedItem))
                        foundUnassigned = true;
                }
                return foundUnassigned ? Properties.Resources.ConnectedPartially : Properties.Resources.Connected;
            }
            if (tit == TreeItemType.Variable)
            {
                var variable = Profile.GetVariableById(itemId);
                return  ((ITreeItem)variable).GetIcon();
            }
            if (tit == TreeItemType.AccessDescriptor)
            {
                var descriptor = Profile.GetAccessDesciptorById(itemId);
                return descriptor.GetIcon();
            }
            return null;
        }
        private void FillSelectLanguageCombobox()
        {
            _selectLanguage.Items.Clear();
            var languageProfiles = LanguageManager.GetProfileList();
            foreach (var profile in languageProfiles)
                _selectLanguage.Items.Add(profile);
            if (!string.IsNullOrEmpty(ApplicationSettings.DefaultLanguage))
                _selectLanguage.Text = ApplicationSettings.DefaultLanguage;
            else
                _selectLanguage.Text = string.Empty;
        }
        private void SelectLanguageDropDownOpened(object sender, EventArgs e)
        {
            FillSelectLanguageCombobox();
        }
        private void SelectLanguageDropDownClosed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectLanguage.Text))
                return;
            ApplicationSettings.DefaultLanguage = _selectLanguage.Text;
            ApplicationSettings.SaveSettings();
            LanguageManager.LoadLanguage(_selectLanguage.Text);
            Localize();
        }
        #endregion
        #region Тестирование формул
        /// <summary>
        ///     Raises an event when the text is changed,
        ///     this even is used to handle syntax colorization.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs" /> instance containing the event data.</param>
        private void FormulaTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTestFormulaAndShowErrors();
        }
        private void SelectText(string keyword)
        {
            var text = new TextRange(_formulaTextBox.Document.ContentStart, _formulaTextBox.Document.ContentEnd);
            var current = text.Start.GetInsertionPosition(LogicalDirection.Forward);
            while (current != null)
            {
                string textInRun = current.GetTextInRun(LogicalDirection.Forward);
                if (!string.IsNullOrWhiteSpace(textInRun))
                {
                    var index = textInRun.IndexOf(keyword);
                    if (index != -1)
                    {
                        var selectionStart = current.GetPositionAtOffset(index, LogicalDirection.Forward);
                        var selectionEnd = selectionStart.DocumentEnd;
                        var selection = new TextRange(selectionStart, selectionEnd);
                        selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
                    }
                }
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        /// <summary>
        /// Подсчитать и подкрасить формулу, если в ней есть ошибки и показать текст ошибки
        /// </summary>
        private void CalculateTestFormulaAndShowErrors()
        {
            var range = new TextRange(_formulaTextBox.Document.ContentStart, _formulaTextBox.Document.ContentEnd);
            var text = range.Text;
            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2, 2);

            var result = _calculator.ComputeFormula(text);
            _formulaTextBox.TextChanged -= FormulaTextBoxTextChanged;
            range.ClearAllProperties();
            switch (result.GetFormulaComputeResultType())
            {
                case TypeOfComputeFormulaResult.FormulaWasEmpty:
                {
                    _formulaResultDec.Text = string.Empty;
                    _formulaResultHex.Text = string.Empty;
                    _formulaResultBool.Text = string.Empty;
                    _formulaError.Text = string.Empty;
                    break;
                }
                case TypeOfComputeFormulaResult.Error:
                {
                    _formulaResultDec.Text = string.Empty;
                    _formulaResultHex.Text = string.Empty;
                    _formulaResultBool.Text = string.Empty;
                    var error = result.GetFormulaCheckResult();
                    _formulaError.Text = CalculatorErrorsLocalizer.TokenErrorToString(error);
                    if (string.IsNullOrEmpty(_formulaError.Text))
                        _formulaError.Text = error.ToString();
                    var keyword = text.Remove(0, result.GetErrorBeginPositionInFormulaText());
                    SelectText(keyword);
                    break;
                }
                case TypeOfComputeFormulaResult.BooleanResult:
                {
                    _formulaError.Text = string.Empty;
                    _formulaResultDec.Text = string.Empty;
                    _formulaResultHex.Text = string.Empty;
                    _formulaResultBool.Text = result.CalculatedBoolBoolValue.ToString();
                    break;
                }
                case TypeOfComputeFormulaResult.DoubleResult:
                {
                    _formulaError.Text = string.Empty;
                    _formulaResultDec.Text = result.CalculatedDoubleValue.ToString(CultureInfo.InvariantCulture);
                    _formulaResultHex.Text = (result.CalculatedDoubleValue % 1) == 0 ? ((int)result.CalculatedDoubleValue).ToString("X") : string.Empty;
                    _formulaResultBool.Text = string.Empty;
                    break;
                }
            }
            _formulaTextBox.TextChanged += FormulaTextBoxTextChanged;
        }
        /// <summary>
        /// Токенизатор [R]
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция, с которой нужно продолжить разбор текста формулы</param>
        /// <returns>null - не удалось обнаружить токен [R], иначе токен</returns>
        private ICalcToken FormulaResultTokenizer(string formula, int currentTokenPosition)
        {
            const string resultTokenText = "[R]";
            if (formula.Length == 0 || formula.Length < currentTokenPosition + 3)
                return null;
            var token = new CalcTokenNumber(currentTokenPosition);
            if (formula.Substring(currentTokenPosition, 3) != resultTokenText)
                return null;
            token.TokenText = resultTokenText;
            return token;
        }
        /// <summary>
        /// Препроцессор токенов для подстановки [R]
        /// </summary>
        /// <param name="tokenToPreprocess">токен</param>
        /// <returns>null - это не [R], иначе токен со значением</returns>
        private ICalcToken FormulaResultProcessor(ICalcToken tokenToPreprocess)
        {
            if (!(tokenToPreprocess is CalcTokenNumber))
                return tokenToPreprocess;
            const string resultTokenText = "[R]";
            if (((CalcTokenNumber) tokenToPreprocess).TokenText == resultTokenText)
            {
                ((CalcTokenNumber) tokenToPreprocess).Value = string.IsNullOrEmpty(_formulaEditorInputValueDec.Text) ? 0 : double.Parse(_formulaEditorInputValueDec.Text, CultureInfo.InvariantCulture);
            }
            return tokenToPreprocess;
        }
        readonly CalculatorVariableAccessAddon _cvaa = new CalculatorVariableAccessAddon();
        private void InitializeCalculator()
        {
            _calculator.RegisterTokenizer(FormulaResultTokenizer);
            _calculator.RegisterPreprocessor(FormulaResultProcessor);
            _calculator.RegisterTokenizer(_cvaa.VariableTokenizer);
            _calculator.RegisterPreprocessor(_cvaa.VariablePreprocessor);
        }
        private void FormulaEditorInputValueDecPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Utils.IsNumeric(e.Text) || ((TextBox)sender).Text.Length > 8;
        }
        private void FormulaEditorInputValueHexPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Utils.IsHexNumber(e.Text) || ((TextBox)sender).Text.Length > 8;
        }
        private void FormulaEditorInputValueDecTextChanged(object sender, TextChangedEventArgs e)
        {
            _formulaEditorInputValueDec.TextChanged -= FormulaEditorInputValueDecTextChanged;
            _formulaEditorInputValueHex.TextChanged -= FormulaEditorInputValueHexTextChanged;
            if (string.IsNullOrEmpty(_formulaEditorInputValueDec.Text))
                _formulaEditorInputValueHex.Text = string.Empty;
            int resultInt;
            _formulaEditorInputValueHex.Text = int.TryParse(_formulaEditorInputValueDec.Text, out resultInt) ? resultInt.ToString("X") : string.Empty;
            _formulaEditorInputValueDec.TextChanged += FormulaEditorInputValueDecTextChanged;
            _formulaEditorInputValueHex.TextChanged += FormulaEditorInputValueHexTextChanged;
            CalculateTestFormulaAndShowErrors();
        }
        private void FormulaEditorInputValueHexTextChanged(object sender, TextChangedEventArgs e)
        {
            _formulaEditorInputValueDec.TextChanged -= FormulaEditorInputValueDecTextChanged;
            _formulaEditorInputValueHex.TextChanged -= FormulaEditorInputValueHexTextChanged;
            if (string.IsNullOrEmpty(_formulaEditorInputValueHex.Text))
                _formulaEditorInputValueDec.Text = string.Empty;
            int result;
            if (int.TryParse(_formulaEditorInputValueHex.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
                _formulaEditorInputValueDec.Text = result.ToString(CultureInfo.InvariantCulture);
            _formulaEditorInputValueDec.TextChanged += FormulaEditorInputValueDecTextChanged;
            _formulaEditorInputValueHex.TextChanged += FormulaEditorInputValueHexTextChanged;
            CalculateTestFormulaAndShowErrors();
        }
        /// <summary>
        /// Скопировать формулу из редактора формул в буфер обмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [STAThread]
        private void CopyFormulaToClipboardClick(object sender, RoutedEventArgs e)
        {
            var range = new TextRange(_formulaTextBox.Document.ContentStart, _formulaTextBox.Document.ContentEnd);
            var text = range.Text;
            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2, 2);
            Clipboard.SetText(text);
        }
        /// <summary>
        /// Добавить переменную в виде [панель.переменная] в текст контрола редактора формул
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddVarToFormulaClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_variablesForFormulaTree);
            if (item == null)
                return;
            if (item.Type == TreeItemType.Panel)
                return;
            var variableName = "[" + item.FullName + "]";
            _formulaTextBox.CaretPosition.InsertTextInRun(variableName);
       }
        #endregion
        #region Управление профилем
        /// <summary>
        /// Заполнить комбобокс списком профилей и установить выбранный элемент
        /// </summary>
        /// <param name="selectedItemText">текст выбранного элемента. null - нет выбранного элемента</param>
        private void FillSelectProfileCombobox(string selectedItemText = null)
        {
            _selectProfile.Items.Clear();
            var profiles = Profile.GetProfileList();
            foreach (var profile in profiles)
            {
                var cbi = new ComboBoxItem { Content = profile.Key, Tag = profile.Value };
                _selectProfile.Items.Add(cbi);
            }
            if (!string.IsNullOrEmpty(selectedItemText))
                _selectProfile.Text = selectedItemText;
        }
        /// <summary>
        /// Приостановить работу роутера перед сменой профиля
        /// </summary>
        private void PauseRouterOnChangeProfile()
        {
            _routerCore.Stop(true);
            VariableManager.Stop();
        }
        /// <summary>
        /// Возобновить работу роутера после смены профиля
        /// </summary>
        private void ResumeRouterOnChangeProfile()
        {
            if (Profile.IsProfileLoaded())
            {
                var name = Profile.GetLoadedProfileName();
                if (name != ApplicationSettings.DefaultProfile)
                {
                    ApplicationSettings.DefaultProfile = name;
                    ApplicationSettings.SaveSettings();
                    SetTitle(name);
                }
            }
            else
            {
                _selectProfile.Text = string.Empty;
                SetTitle();
            }
            VariableManager.Start();
            _routerCore.Start();
            RenewTrees();
        }
        /// <summary>
        /// При закрытии списка профилей загрузить выбранный. Если выбор не изменился - ничего не грузим
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectProfileDropDownClosed(object sender, EventArgs e)
        {
            if (_selectProfile.Text == null || _selectProfile.Text == Profile.GetLoadedProfileName())
                return;
            LoadProfile(_selectProfile.Text);
        }
        /// <summary>
        /// Загрузить профиль
        /// </summary>
        /// <param name="profileName">имя профиля. null - ничего не грузить</param>
        /// <param name="controlProcessorsProfile">имя профиля, откуда брать назначения. null - брать из профиля, переданного в параметре profileName</param>
        /// <returns>true - профиль загружен</returns>
        private bool LoadProfile(string profileName, string controlProcessorsProfile = null)
        {
            if (profileName == null)
                return false;
            var profileList = Profile.GetProfileList();
            if (!profileList.ContainsKey(profileName))
                return false;
            var profilePath = profileList[profileName];
            PauseRouterOnChangeProfile();
            bool loadResult;
            if (controlProcessorsProfile == null)
                loadResult = Profile.LoadProfile(profilePath);
            else
                loadResult = Profile.MergeAssignmentsWithProfile(profileName, controlProcessorsProfile);
            ResumeRouterOnChangeProfile();
            return loadResult;
        }
        /// <summary>
        /// Создать новый профиль
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateNewProfileClick(object sender, RoutedEventArgs e)
        {
            PauseRouterOnChangeProfile();
            Profile.CreateNewProfile();
            FillSelectProfileCombobox(ApplicationSettings.DefaultProfile);
            ResumeRouterOnChangeProfile();
        }
        /// <summary>
        /// Удалить профиль
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveProfileClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguageManager.GetPhrase(Phrases.SettingsMessageRemoveProfile),
                LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;
            PauseRouterOnChangeProfile();
            Profile.RemoveCurrentProfile();
            ResumeRouterOnChangeProfile();
        }
        /// <summary>
        /// Экспортировать профиль в указанный пользователем файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportProfileClick(object sender, RoutedEventArgs e)
        {
            var of = new SaveFileDialog
            {
                Title = LanguageManager.GetPhrase(Phrases.SettingsExportProfileDialogHeader),
                CheckPathExists = true,
                Filter = @"Aircraft profile files (*.ap)|*.ap|All files (*.*)|*.*"
            };
            if (of.ShowDialog() != true)
                return;
            Profile.SaveProfileAs(of.FileName);
        }
        /// <summary>
        /// Импортировать профиль с сохранением назначений текущего профиля
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportProfileAndSaveMyAssignmentsClick(object sender, RoutedEventArgs e)
        {
            if (!Profile.IsProfileLoaded())
                return;
            var of = new OpenFileDialog
            {
                Title = LanguageManager.GetPhrase(Phrases.SettingsImportProfileKeepAssignmentsDialogHeader),
                Multiselect = false,
                Filter = @"Aircraft profile files (*.ap)|*.ap|All files (*.*)|*.*"
            };
            if (of.ShowDialog() != true)
                return;
            PauseRouterOnChangeProfile();
            Profile.ImportAndSaveAssignments(of.FileName);
            ResumeRouterOnChangeProfile();
        }
        /// <summary>
        /// Испортировать профиль из внешнего файла, указанного пользователем во внутреннее хранилище профилей роутера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportProfileClick(object sender, RoutedEventArgs e)
        {
            var of = new OpenFileDialog
            {
                Title = LanguageManager.GetPhrase(Phrases.SettingsImportProfileDialogHeader),
                Multiselect = false,
                Filter = @"Aircraft profile files (*.ap)|*.ap|All files (*.*)|*.*"
            };
            if (of.ShowDialog() != true)
                return;
            PauseRouterOnChangeProfile();
            Profile.Import(of.FileName);
            ResumeRouterOnChangeProfile();
        }
        /// <summary>
        /// Переименовать профиль
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameProfileClick(object sender, RoutedEventArgs e)
        {
            var name = Profile.RenameProfile();
            if (name == null)
                return;
            SetTitle(name);
            FillSelectProfileCombobox(name);
            _selectProfile.Text = name;
        }
        #endregion

        private void TurnControlsSynchronizationOffUnchecked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.ControlsSynchronizationIsOff = false;
            ApplicationSettings.SaveSettings();
        }

        private void TurnControlsSynchronizationOffChecked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.ControlsSynchronizationIsOff = true;
            ApplicationSettings.SaveSettings();
        }

    }
}
