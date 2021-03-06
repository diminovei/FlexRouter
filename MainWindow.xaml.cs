﻿//Не полный оборот (Амперметр)
//    AD: значение
//Полный оборот (Компас)
//    AD: возможность перейти через 0
//    AD: 1 оборот – это от X до Y. По достижению Y устанавливаем Х
//Многооборотный (Высотометр)
//    AD: От скольки до скольки?
//    AD: 1 оборот – это сколько? (только для многооборотных)

//Тип:
//1.	Не полный оборот (Амперметр)
//2.	Полный оборот (Компас)
//3.	Многооборотный (Высотометр)
//AD: От скольки до скольки?
//AD: 1 оборот – это сколько? (только для 3)

//CP: колибруем начальную позицию
//CP: колибруем позицию окончания (только для 1)

//  ***************************** Внешнее
//  бага в тушке. Если включить ввод ЗК на ПН-5, а затем выключить стабилизацию крена будут гореть и Сброс программы и ввод ЗК (проверить, как работает в роутере)
//  Стабилизация по высоте включается, а лампа не загорается. Нужно разбираться (похоже, у меня плохо припаян светодиод. Перепроверить)
//  ***************************** Исправить в следующей версии
//  Кнопка: в приватный профиль/в общий профиль
//  ***************************** Бэклог
//      Железо:
//          AD для управления яркостью
//          В джойстике поддержать все оси и хатку
//          Поддержка L3/F3 - шаговики
//          При изменении настройки JoystickBindByInstanceGuid переинициализировать железо
//      AxisMultistate (управление набором значений при помощи оси, например, замена крана закрылков или галетных переключателей)
//      Реализовать функции (FromBCD, ToBCD, получить дробную часть)
//      Работа с окнами
//          Редактирование окон
//          Редактирование описателя "Клики мышью (Multistate)"
//          Редактирование описателя "Клики мышью (Range)"
//      Биндинг не работает, если в названии переменной (колонки) "плохой" символ. Точка, слэш или пробел в конце
//  Пройтись по всем ToDo и доделать
//  ***************************** Не срочно (или не нужно):


    //Math operators:
    //5 + 2 = 7	Plus operator
    //5 - 2 = 3	Munis operator
    //5 * 2 = 10	Multiply operator
    //5 % 2 = 1	The % operator computes the remainder after dividing its first operand by its second
    //5 / 2 = 2.5	The division operator (/) divides its first operand by its second operand
    //5 : 2 = 2	The division operator (:) divides its first operand by its second operand and gets the interger part of devision
    //Logic operators
    //    1 == 1	is true	Equal
    //    1 != 2 is true	Not
    //    And,            // &&
    //    Or,             // ||
    //    GreaterOrEqual, // >= Именно в таком порядке, чтобы при токенизации сначал проверялось >=, а потом >
    //    LessOrEqual,    // <=
    //    Greater,        // >
    //    Less,           // <




using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.FormulaKeeper;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorsUI.AccessDescriptorsEditor;
using FlexRouter.EditorsUI.ControlProcessorEditors;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.EditorsUI.PanelEditors;
using FlexRouter.EditorsUI.VariableEditors;
using FlexRouter.Hardware;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ManageMapFiles;
using FlexRouter.MessagesToMainForm;
using FlexRouter.ProfileItems;
using FlexRouter.Properties;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string PrivacyMarker = "*";
        private RouterState _routerState = RouterState.Stopped;
        private readonly Core _routerCore = new Core();
        private DispatcherTimer _timer;
        private DispatcherTimer _timer2;
        private readonly Calculator _calculator = new Calculator();
        readonly AxisDebouncer _axisDebouncer = new AxisDebouncer();

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException
                += delegate(object sender, UnhandledExceptionEventArgs args)
                {
                    var e = (Exception)args.ExceptionObject;
                    var logFolder = Utils.GetFullSubfolderPath("Logs");
                    if (!Directory.Exists(logFolder))
                        Directory.CreateDirectory(logFolder);
                    var logPath = Path.Combine(logFolder, "flexrouter.log");
                    var version = Assembly.GetEntryAssembly().GetName().Version;
                    File.AppendAllText(logPath, string.Format("{0};Unhandled exception;{1};{2}\n", DateTime.Now, version, e));
                    MessageBox.Show(e.ToString(), "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Application.Current.Shutdown();
                };

            InitializeComponent();
            ApplicationSettings.LoadSettings();
            FillSelectProfileCombobox(ApplicationSettings.DefaultProfile);
            LanguageManager.Initialize();

            if (string.IsNullOrEmpty(ApplicationSettings.DefaultLanguage))
                ApplicationSettings.DefaultLanguage = "Русский";
            LanguageManager.LoadLanguage(ApplicationSettings.DefaultLanguage);

            Localize();
            
            _turnControlsSynchronizationOff.IsChecked = ApplicationSettings.ControlsSynchronizationIsOff;
            _joystickBindByInstanceGuid.IsChecked = ApplicationSettings.JoystickBindByInstanceGuid;
            _disablePersonalProfile.IsChecked = ApplicationSettings.DisablePersonalProfile;

            InitializeCalculator();

            FillSelectLanguageCombobox();

            LoadProfile(ApplicationSettings.DefaultProfile);

            _timer = new DispatcherTimer();
            _timer.Tick += OnTimedEvent;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _timer.Start();

            _timer2 = new DispatcherTimer();
            _timer2.Tick += OnTimedEvent2;
            _timer2.Interval = new TimeSpan(0, 0, 0, 3);
            _timer2.Start();
        }

        private readonly object _locker = new object();

        private void GetSubsystemsStatus()
        {
            lock (_locker)
            {
                if (!Problems.Problems.ProblemListWasChanged)
                    return;
                _statusList.Items.Clear();
                var problems = Problems.Problems.GetProblemList();
                foreach (var s in problems)
                    _statusList.Items.Add(CreateListViewItem(s.IsFixed ? s.Name : s.Name + " - " + s.Description,
                        s.IsFixed ? Properties.Resources.Ok : Properties.Resources.Fail));
            }
        }
        /// <summary>
        /// Установить заголовок главного окна
        /// </summary>
        /// <param name="profileName"></param>
        private void SetTitle(string profileName = null)
        {
            Title = "Flex Router v" + Assembly.GetExecutingAssembly().GetName().Version + (string.IsNullOrEmpty(profileName) ? "" : " - " + profileName);
        }
        /// <summary>
        /// Локализовать все надписи на формах
        /// </summary>
        private void Localize()
        {
            _accessDescriptorsToCreateList.Text = string.Empty;
            _controlProcessorsList.Text = string.Empty;
            _accessMethods.Text = string.Empty;

            _removeAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _removeControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonClearAssignment);
            _removeVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            foreach (var child in _accessDescriptorPanel.Children)
            {
                var editor = child as IEditor;
                if (editor != null)
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
            _removeProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRemove);
            _renameProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonRename);
            _createAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _createNewProfile.Content = LanguageManager.GetPhrase(Phrases.CommonButtonCreate);
            _saveAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _saveControlProcessor.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _saveVariable.Content = LanguageManager.GetPhrase(Phrases.CommonButtonSave);
            _selectProfileLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelProfile);
            _selectLanguageLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelLanguage);
            _formulaResultLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelFormulaResult);
            _copyFormulaToClipboard.Content = LanguageManager.GetPhrase(Phrases.CommonLabelCopyToClipboard);
            _addVarToFormula.Content = LanguageManager.GetPhrase(Phrases.CommonLabelCopyVariableToFormula);
            _errorLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelError);
            _routerStateLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelRouterState);
            _connectedHardwareLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelConnectedHardwareList);
            _problemsLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelProblemsList);
            _lastEventLabel.Content = LanguageManager.GetPhrase(Phrases.CommonLabelLastHardwareEvent);
            _profileManagementGroup.Header = LanguageManager.GetPhrase(Phrases.CommonProfileManagement);
            _turnControlsSynchronizationOffLabel.Content = LanguageManager.GetPhrase(Phrases.SettingsTurnControlsSynchronizationOff);
            _joystickBindByInstanceGuidLabel.Content = LanguageManager.GetPhrase(Phrases.SettingsJoystickBindByInstanceGuid);
            _dump.Content = LanguageManager.GetPhrase(Phrases.CommonDumpControls);
            _varAndPanelNameToClipboard.Content = LanguageManager.GetPhrase(Phrases.EditorVariableAndPanelNameToClipboard);
            _cloneAccessDescriptor.Content = LanguageManager.GetPhrase(Phrases.EditorCloneAccessDescriptor);
            _cloneVariable.Content = LanguageManager.GetPhrase(Phrases.EditorCloneVariable);
            _disablePersonalProfileLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDisablePersonalProfile);
            _mergePersonalAndPublicProfile.Content = LanguageManager.GetPhrase(Phrases.EditorMergePersonalProfileWithPublic);
            _initializeVarNamesFromMapFile.Content = LanguageManager.GetPhrase(Phrases.MapFileUIModeInitializeVarNames);
            _updateVarOffsetsFromMapFile.Content = LanguageManager.GetPhrase(Phrases.MapFileUIModeUpdateVarOffsets);
            _mapFileGroup.Header = LanguageManager.GetPhrase(Phrases.MapFileUIGroup);
            VisualizeRouterState();
        }
        /// <summary>
        /// Обновить надпись, сообщающую о текущем состояни роутера
        /// </summary>
        private void VisualizeRouterState()
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
        private void OnTimedEvent2(object sender, EventArgs e)
        {
            GetSubsystemsStatus();
        }
        private void OnTimedEvent(object sender, EventArgs e)
        {
            ProcessMessages();
            CalculateTestFormulaAndShowErrors();
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

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ApplicationSettings.SaveSettings();
            _routerCore.Stop();
            _timer.Stop();
        }
        /// <summary>
        /// Обработать сообщения с данными для отображения, посланные главной форме внутренними компонентами роутера
        /// </summary>
        private void ProcessMessages()
        {
            var messages = Messenger.GetMessages();
            foreach (var message in messages)
            {
                if (message.MessageType == MessageToMainForm.RouterStarted)
                    _routerState = RouterState.Running;
                VisualizeRouterState();
                if (message.MessageType == MessageToMainForm.RouterStopped)
                    _routerState = RouterState.Stopped;
                VisualizeRouterState();
                if (message.MessageType == MessageToMainForm.RouterPaused)
                    _routerState = RouterState.Paused;
                VisualizeRouterState();
                if (message.MessageType == MessageToMainForm.ClearConnectedDevicesList)

                    ConnectedDevicesList.Items.Clear();
                if (message.MessageType == MessageToMainForm.ChangeConnectedDevice)
                    ConnectedDevicesList.Items.Add(((TextMessage) message).Text);
                if (message.MessageType == MessageToMainForm.NewHardwareEvent)
                {
                    var evButton = (((ControlEventBase) ((ObjectMessage) message).AnyObject)) as ButtonEvent;
                    if (evButton != null)
                    {
                        var additionalInfo = evButton.IsPressed ? "vvv" : "^^^";
                        _incomingEvent.Text = ((ControlEventBase)((ObjectMessage)message).AnyObject).Hardware.GetHardwareGuid() + " " + additionalInfo;
                    }
                        
                    var evEncoder = (((ControlEventBase) ((ObjectMessage) message).AnyObject)) as EncoderEvent;
                    if (evEncoder != null)
                    {
                        string additionalInfo = evEncoder.RotateDirection ? ">>>" : "<<<";
                        _incomingEvent.Text = ((ControlEventBase)((ObjectMessage)message).AnyObject).Hardware.GetHardwareGuid() + " " + additionalInfo;
                    }
                    var evAxis = (((ControlEventBase)((ObjectMessage)message).AnyObject)) as AxisEvent;
                    if (evAxis != null && _axisDebouncer.IsNeedToProcessAxisEvent(evAxis, 2))
                    {
                        string additionalInfo = evAxis.Position.ToString(CultureInfo.InvariantCulture);
                        _incomingEvent.Text = ((ControlEventBase)((ObjectMessage)message).AnyObject).Hardware.GetHardwareGuid() + " " + additionalInfo;
                        _axisDebouncer.Reset();
                    }

                    foreach (var child in _controlProcessorsPanel.Children)
                    {
                        if (child is IControlProcessorEditor)
                            ((IControlProcessorEditor) child).OnNewControlEvent(((ControlEventBase) ((ObjectMessage) message).AnyObject));
                    }
                }
            }
        }
        #region AccessDescriptorEditor

        private readonly TreeViewHelper _accessDescriptorTreeHelper = new TreeViewHelper();

        private void AccessDescriptorsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ChangeInEditor(TreeItemType.AccessDescriptor, _accessDescriptorPanel, _accessDescriptorsTree, _accessDescriptorTreeHelper, e);
       }

        private void SaveAccessDescriptorClick(object sender, RoutedEventArgs e)
        {
            OnAnySaveButtonClicked(_accessDescriptorPanel, false);
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
                var comboboxItem = new ComboBoxItem
                {
                    Content =
                        (listItem is DescriptorBase
                            ? ((IAccessDescriptor) listItem).GetDescriptorType()
                            : ((Panel) listItem).GetNameOfProfileItemType()),
                    Tag = listItem
                };
                _accessDescriptorsToCreateList.Items.Add(comboboxItem);
            }
        }

        private void OnAccessDescriptorsToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createVariable.IsEnabled = !string.IsNullOrEmpty(((TextBox) sender).Text);
        }

        private void CreateAccessDescriptorClick(object sender, RoutedEventArgs e)
        {
            var itemToCreate = GetObjectToCreateFromCombobox(_accessDescriptorsToCreateList,_accessDescriptorPanel);
            if (itemToCreate == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoItemToCreateSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if ((itemToCreate as DescriptorBase) != null)
                ShowAccessDescriptorEditors((DescriptorBase) itemToCreate);
            if ((itemToCreate as Panel) != null)
                ShowPanel((Panel) itemToCreate, _accessDescriptorPanel);
            FillAccessDescriptorsToCreateList();
        }

        private void ShowAccessDescriptorEditors(DescriptorBase ad)
        {
            var selectedItemPanelName = GetSelectedItemPanelName(_accessDescriptorsTree);
            _accessDescriptorPanel.Children.Clear();
            var editors = new List<UserControl>();
            editors.Add(new DescriptorCommonEditor(ad, selectedItemPanelName));
            if (ad is DescriptorValue)
            {
                editors.Add(new DescriptorValueEditor((DescriptorValue) ad, true));
                editors.Add(new RepeaterEditor((DescriptorMultistateBase)ad));
            }
            if (ad is DescriptorBinaryOutput)
            {
                editors.Add(new DescriptorFormulaEditor((DescriptorOutputBase) ad));
            }
            if (ad is DescriptorIndicator)
            {
                editors.Add(new DescriptorDecPointEditor((DescriptorIndicator) ad));
                editors.Add(new DescriptorFormulaEditor((DescriptorOutputBase) ad));
            }
            if (ad is DescriptorRange)
            {
                editors.Add(new DescriptorValueEditor((DescriptorMultistateBase) ad, false));
                editors.Add(new DescriptorRangeEditor((DescriptorRange) ad));
            }
            if (ad is RangeUnion)
            {
                editors.Add(new RangeUnionEditor((RangeUnion) ad));
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
                return;
            if (item.Type == TreeItemType.Panel)
            {
                RemovePanel((Panel) item.Object);
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
                Profile.RemoveAccessDescriptor(((DescriptorBase) item.Object).GetId());
                Profile.Save(ApplicationSettings.DisablePersonalProfile);
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
                Profile.PanelStorage.RemovePanel(panel);
                Profile.Save(ApplicationSettings.DisablePersonalProfile);
                RenewTrees();
            }
        }

        #endregion

        #region VariableEditor

        private readonly TreeViewHelper _variableTreeHelper = new TreeViewHelper();

        private void VariablesTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ChangeInEditor(TreeItemType.Variable, _variablesPanel, _variablesTree, _variableTreeHelper, e);
        }

        private void ChangeInEditor(TreeItemType treeItemType, StackPanel panel, TreeView tree, TreeViewHelper treeViewHelper, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsNeedToChangeItemInEditor(tree, panel, e, treeViewHelper))
                return;

            var treeSelectedItem = GetTreeSelectedItem(tree);
            if (treeSelectedItem == null)
                return;

            panel.Children.Clear();
            if (((TreeViewItem)tree.SelectedItem).Name == TreeItemType.Panel.ToString())
            {
                if (treeItemType == TreeItemType.ControlProcessor)
                    return;
                IEditor ie = new PanelProperties((Panel)((TreeViewItem)tree.SelectedItem).Tag);
                DockPanel.SetDock((UserControl)ie, Dock.Top);
                panel.Children.Add((UserControl)ie);
                return;
            }
            var item = treeSelectedItem.Object;

            if (treeItemType == TreeItemType.AccessDescriptor)
                ShowAccessDescriptorEditors((DescriptorBase)item);

            if (treeItemType == TreeItemType.ControlProcessor)
            {
                FillCreateControlProcessorList();
                var controlProcessor = Profile.ControlProcessor.GetControlProcessor(((DescriptorBase)item).GetId());
                ShowControlProcessor(controlProcessor);
                // ToDo: нужно не отсюда вызывать, а давать CP панелям знать, что выбран другой узел дерева и нужно завершать поиск
                HardwareManager.StopComponentSearch();
            }

            if (treeItemType == TreeItemType.Variable)
                ShowVariable((IVariable)item);
        }

        private string GetSelectedItemPanelName(TreeView tree)
        {
            if (tree.SelectedItem != null)
            {
                if (((TreeViewItem) tree.SelectedItem).Name == TreeItemType.Panel.ToString())
                    return ((Panel) ((TreeViewItem) tree.SelectedItem).Tag).Name;
                var selectedItem = (TreeViewItem) tree.SelectedItem;
                Panel panel = null;
                if (selectedItem.Tag is IVariable)
                {
                    var selectedVariable = (IVariable) ((TreeViewItem) tree.SelectedItem).Tag;
                    panel = Profile.PanelStorage.GetPanelById(selectedVariable.PanelId);
                }
                if (selectedItem.Tag is DescriptorBase)
                {
                    var selectedDescriptor = (DescriptorBase) ((TreeViewItem) tree.SelectedItem).Tag;
                    panel = Profile.PanelStorage.GetPanelById(selectedDescriptor.GetAssignedPanelId());
                }
                if (panel != null)
                    return panel.Name;
            }
            return null;
        }

        private void ShowVariable(IVariable variable)
        {
            var selectedItemPanelName = GetSelectedItemPanelName(_variablesTree);
            if (variable == null)
                return;
            _variablesPanel.Children.Clear();

            var editors = new List<UserControl>();

            if (variable is MemoryPatchVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderMemoryPatch, selectedItemPanelName));
                editors.Add(new MemoryPatchVariableEditor(variable));
                editors.Add(new VariableSizeEditor(variable as IMemoryVariable));
                editors.Add(new VariableValueEditor(variable));
                editors.Add(new VariableEditorDescription(variable));
            }
            if (variable is FsuipcVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderFsuipc, selectedItemPanelName));
                editors.Add(new FsuipcVariableEditor(variable));
                editors.Add(new VariableSizeEditor(variable as IMemoryVariable));
                editors.Add(new VariableValueEditor(variable));
                editors.Add(new VariableEditorDescription(variable));
            }
            if (variable is FakeVariable)
            {
                editors.Add(new VariableEditorHeader(variable, Phrases.EditorHeaderFakeVariable, selectedItemPanelName));
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

        private void ShowPanel(Panel item, StackPanel panel)
        {
            if (item == null)
                return;
            panel.Children.Clear();

            var editors = new List<UserControl> {new PanelProperties(item)};
            for (var i = 0; i < editors.Count; i++)
            {
                DockPanel.SetDock(editors[i], i == 0 ? Dock.Top : Dock.Bottom);
                panel.Children.Add(editors[i]);
            }
        }

        private void SaveVariableClick(object sender, RoutedEventArgs e)
        {
            //var treeSelectedItem = GetTreeSelectedItem(_variablesTree);
            //if (treeSelectedItem == null)
            //    return;
            //if ((treeSelectedItem.Object as IVariable) != null)
            //{
            //    var editableVariable = (IVariable)treeSelectedItem.Object;
            //    var sameVarNames = Profile.GetSameVariablesNames(editableVariable);
            //    if (sameVarNames != null)
            //    {
            //        var message = LanguageManager.GetPhrase(Phrases.EditorMessageTheSameVariableIsExist) + ": " + Environment.NewLine + Environment.NewLine + sameVarNames;
            //        var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
            //        MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
            //        return;
            //    }
            //}
            OnAnySaveButtonClicked(_variablesPanel, true);
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
                var item = new ComboBoxItem
                {
                    Content = (variable is IVariable ? ((IVariable)variable).GetName() : ((Panel)variable).GetNameOfProfileItemType()),
                    Tag = variable
                };
                _accessMethods.Items.Add(item);
            }
        }

        private void OnVariablesToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createVariable.IsEnabled = !string.IsNullOrEmpty(((TextBox) sender).Text);
        }

        private void CreateVariableClick(object sender, RoutedEventArgs e)
        {
            var variable = GetObjectToCreateFromCombobox(_accessMethods, _variablesPanel);
            if (variable == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoItemToCreateSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if ((variable as IVariable) != null)
                ShowVariable((IVariable) variable);
            if ((variable as Panel) != null)
                ShowPanel((Panel) variable, _variablesPanel);
            FillVariablesToCreateList();
        }

        private void RemoveVariableClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_variablesTree);
            if (item == null)
                return;
            if (item.Type == TreeItemType.Panel)
            {
                RemovePanel((Panel) item.Object);
                return;
            }
            var variableLinks = GlobalFormulaKeeper.Instance.GetVariableOwnersByVariableId(((IVariable) item.Object).Id);
            if (variableLinks.Length != 0)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageCantRemoveVariableInUse) + "\n\n";
                foreach (var vl in variableLinks)
                {
                    var ad = Profile.AccessDescriptor.GetAccessDesciptorById(vl);
                    if (ad != null)
                    {
                        var adName = Profile.PanelStorage.GetPanelById(ad.GetAssignedPanelId()).Name + "." + ad.GetName();
                        message += LanguageManager.GetPhrase(Phrases.EditorAccessDescriptor) + " '" + adName + "'" +
                                   "\n";
                    }
                    var panel = Profile.PanelStorage.GetPanelById(vl);
                    if (panel != null)
                    {
                        var panelName = panel.GetNameOfProfileItemType();
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
                Profile.VariableStorage.RemoveVariable(((IVariable) item.Object).Id);
                Profile.Save(ApplicationSettings.DisablePersonalProfile);
                RenewVariablesTrees();
            }
        }

        private void _varAndPanelNameToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_variablesTree);
            if (item == null || item.Type == TreeItemType.Panel)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoVariableSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var varPanelAndName = Profile.GetVariableAndPanelNameById(((IVariable) item.Object).Id);
            Clipboard.SetText("[" + varPanelAndName + "]");
        }

        #endregion

        #region ControlProcessorEditorsMethods

        private readonly TreeViewHelper _controlProcessorTreeHelper = new TreeViewHelper();

        private void OnControlProcessorsToCreateTextChanged(object sender, TextChangedEventArgs e)
        {
            _createControlProcessor.IsEnabled = !string.IsNullOrEmpty(((TextBox) sender).Text);
        }

        private void ControlProcessorsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ChangeInEditor(TreeItemType.ControlProcessor, _controlProcessorsPanel, _controlProcessorsTree, _controlProcessorTreeHelper, e);
        }

        private void FillCreateControlProcessorList()
        {
            if (_controlProcessorsTree.SelectedItem == null)
                return;
            if (((TreeViewItem) _controlProcessorsTree.SelectedItem).Name == TreeItemType.Panel.ToString())
                return;

            var accessDescriptor = (DescriptorBase) ((TreeViewItem) _controlProcessorsTree.SelectedItem).Tag;
            _controlProcessorsList.Items.Clear();

            IEnumerable<IControlProcessor> processorsList = new List<IControlProcessor>
            {
                new IndicatorProcessor(accessDescriptor),
                new LedMatrixIndicatorProcessor(accessDescriptor),
                new LampProcessor(accessDescriptor),
                new ButtonProcessor(accessDescriptor),
                new EncoderProcessor(accessDescriptor),
                new ButtonPlusMinusProcessor(accessDescriptor),
                new ButtonBinaryInputProcessor(accessDescriptor),
                new AxisRangeProcessor(accessDescriptor)
            };
            var controlProcessors =
                processorsList.Where(x => x.IsAccessDesctiptorSuitable(accessDescriptor)).Select(x => x).ToList();
            foreach (var controlProcessor in controlProcessors)
            {
                var item = new ComboBoxItem {Content = controlProcessor.GetDescription(), Tag = controlProcessor};
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
            }
            if (controlProcessor is ButtonPlusMinusProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Button));
            }

            if (controlProcessor is AxisRangeProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Axis));
                editors.Add(new AxisSetLimitsEditor(controlProcessor));
            }
            if (controlProcessor is EncoderProcessor)
            {
                editors.Add(new AssignEditor(controlProcessor, true, HardwareModuleType.Encoder));
            }
            if (controlProcessor is IndicatorProcessor)
            {
                editors.Add(new AssignEditorForOutput(controlProcessor, false, HardwareModuleType.Indicator));
            }
            if (controlProcessor is LedMatrixIndicatorProcessor)
            {
                editors.Add(new AssignEditorForOutput(controlProcessor, false, HardwareModuleType.LedMatrixIndicator));
            }
            if (controlProcessor is LampProcessor)
            {
                editors.Add(new AssignEditorForOutput(controlProcessor, false, HardwareModuleType.BinaryOutput));
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

        private void CreateControlProcessorClick(object sender, RoutedEventArgs e)
        {
            var treeSelectedItem = GetTreeSelectedItem(_controlProcessorsTree);
            var dropDownListItem = _controlProcessorsList.SelectedItem as ComboBoxItem;

            if (treeSelectedItem != null)
            {
                if (treeSelectedItem.Type == TreeItemType.Panel)
                {
                    var message = LanguageManager.GetPhrase(Phrases.EditorPanelIsNotAnObjectToAssignHardware);
                    var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                    MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (dropDownListItem == null)
                {
                    var message = LanguageManager.GetPhrase(Phrases.EditorNoItemToCreateSelected);
                    var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                    MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoTreeItemAndItemToCreateSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var controlProcessor = (IControlProcessor) ((ComboBoxItem) _controlProcessorsList.SelectedItem).Tag;

            if (controlProcessor != null)
            {
                var oldControlProcessorId = (((DescriptorBase) treeSelectedItem.Object)).GetId();
                if (Profile.ControlProcessor.GetControlProcessor(oldControlProcessorId) != null)
                    Profile.ControlProcessor.RemoveControlProcessor(oldControlProcessorId);
                Profile.ControlProcessor.AddControlProcessor(controlProcessor, oldControlProcessorId);
                var ad = Profile.AccessDescriptor.GetAccessDesciptorById(oldControlProcessorId) as DescriptorMultistateBase;
                if (ad != null)
                {
                    controlProcessor.OnAssignmentsChanged();
                }
                ShowControlProcessor(controlProcessor);
                Profile.Save(ApplicationSettings.DisablePersonalProfile);
                FillCreateControlProcessorList();
            }
        }

        private void SaveControlProcrssorClick(object sender, RoutedEventArgs e)
        {
            OnAnySaveButtonClicked(_controlProcessorsPanel, false);
        }

        private void RemoveControlProcessorClick(object sender, RoutedEventArgs e)
        {
            var item = GetTreeSelectedItem(_controlProcessorsTree);
            if (item == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoItemInTreeSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (item.Type == TreeItemType.Panel)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorPanelIsNotAnObjectToAssignHardware);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(LanguageManager.GetPhrase(Phrases.EditorMessageRemoveControlProcessor) + " '" + item.FullName + "'?", LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) 
                return;
            
            _controlProcessorsPanel.Children.Clear();
            var id = ((DescriptorBase) item.Object).GetId();
            var events = Profile.ControlProcessor.GetShutDownEvents(id);
            HardwareManager.PostOutgoingEvents(events);
            Profile.ControlProcessor.RemoveControlProcessor(id);
            Profile.Save(ApplicationSettings.DisablePersonalProfile);
            RenewTrees();
        }

        #endregion
        #region TreeRenew
        private void RenewTrees()
        {
            System.Diagnostics.Debug.Print("BeginTree: {0}", DateTime.Now);
            ShowTree(_accessDescriptorsTree, TreeItemType.AccessDescriptor);
            ShowVariablesTree(_variablesTree);
            ShowTree(_controlProcessorsTree, TreeItemType.ControlProcessor);
            ShowVariablesTree(_variablesForFormulaTree);
            System.Diagnostics.Debug.Print("EndTree: {0}", DateTime.Now);
        }
        private void RenewVariablesTrees()
        {
            System.Diagnostics.Debug.Print("BeginVarTree: {0}", DateTime.Now);
            ShowVariablesTree(_variablesTree);
            ShowVariablesTree(_variablesForFormulaTree);
            System.Diagnostics.Debug.Print("EndVarTree: {0}", DateTime.Now);
        }
        private static void ShowTree(TreeView tree, TreeItemType tit)
        {
            var vtk = new TreeViewStateKeeper();
            vtk.RememberState(ref tree);
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            tree.Items.Clear();
            var dependentAccessDescriptors = Profile.AccessDescriptor.GetSortedAccessDesciptorList().Where(x=>x.IsDependent()).ToArray();
            foreach (var panel in panels)
            {
                var treeRootItem = new TreeViewItem
                {
                    Tag = panel,
                    Name = TreeItemType.Panel.ToString(),
                    Header = panel.GetPrivacyType() == ProfileItemPrivacyType.Private ? PrivacyMarker + panel.Name : panel.Name
                };

                var ad = Profile.AccessDescriptor.GetSortedAccessDesciptorListByPanelId(panel.Id);
                foreach (var adesc in ad)
                {
                    if (adesc.IsDependent())
                        continue;

                    var icon = GetIcon(tit, adesc);
                    var nodeName = (tit == TreeItemType.AccessDescriptor && adesc.GetPrivacyType() == ProfileItemPrivacyType.Private) ? PrivacyMarker + adesc.GetName() : adesc.GetName();
                    var treeAdItem = CreateTreeViewItem(nodeName, adesc, tit, icon);
                    treeAdItem.Tag = adesc;
                    treeAdItem.Name = tit.ToString();

                    treeRootItem.Items.Add(treeAdItem);
                    if (tit == TreeItemType.ControlProcessor)
                        continue;
                    foreach (var a in dependentAccessDescriptors)
                    {
                        if (!a.IsDependent())
                            continue;
                        if (a.GetDependency().GetId() != adesc.GetId())
                            continue;
                        icon = GetIcon(tit, a);
                        var depNodeName = Profile.PanelStorage.GetPanelById(a.GetAssignedPanelId()).Name + "." + a.GetName();
                        if (adesc.GetPrivacyType() == ProfileItemPrivacyType.Private)
                            depNodeName = PrivacyMarker + depNodeName;

                        var treeDependentItem = CreateTreeViewItem(depNodeName, a, tit, icon);
                        treeAdItem.Items.Add(treeDependentItem);
                    }
                }
                tree.Items.Add(treeRootItem);
            }
            vtk.RestoreState(ref tree);
        }
        private static IEnumerable<TreeViewItem> GetTreeViewItems(TreeViewItem currentTreeViewItem)
        {
            var returnItems = new List<TreeViewItem> {currentTreeViewItem};
            foreach (var t in currentTreeViewItem.Items)
            {
                returnItems.AddRange(GetTreeViewItems((TreeViewItem)t));
            }
            return returnItems.ToArray();
        }
        private void ShowVariablesTree(TreeView tree)
        {
            var vtk = new TreeViewStateKeeper();
            vtk.RememberState(ref tree);
            var panels = Profile.PanelStorage.GetSortedPanelsList();
            tree.Items.Clear();
            foreach (var panel in panels)
            {
                var treeRootItem = new TreeViewItem
                {
                    Tag = panel,
                    Name = TreeItemType.Panel.ToString(),
                    Header = panel.GetPrivacyType() == ProfileItemPrivacyType.Private ? PrivacyMarker + panel.Name : panel.Name
                };

                var ad = Profile.GetSortedVariablesListByPanelId(panel.Id);
                foreach (var adesc in ad)
                {

                    var icon = GetIcon(TreeItemType.Variable, adesc);
                    var nodeName = ((adesc as VariableBase).GetPrivacyType() == ProfileItemPrivacyType.Private) ? PrivacyMarker + adesc.Name : adesc.Name;

                    var treeAdItem = CreateTreeViewItem(nodeName, adesc, TreeItemType.Variable, icon);
                    treeAdItem.Name = TreeItemType.Variable.ToString();
                    treeRootItem.Items.Add(treeAdItem);
                }
                tree.Items.Add(treeRootItem);
            }
            vtk.RestoreState(ref tree);
        }
        static string GetNameOf<T>(Expression<Func<T>> property)
        {
            return (property.Body as MemberExpression).Member.Name;
        }
        private static ImageSource GetIcon(TreeItemType treeItemType, object item)
        {
            string iconKey = null;

            if (treeItemType == TreeItemType.ControlProcessor)
            {
                var cp = Profile.ControlProcessor.GetControlProcessor(((IAccessDescriptor)item).GetId());
                if (cp == null)
                {
                    iconKey = GetNameOf(() => Properties.Resources.ConnectedNot);
                }
                else
                {
                    var assignments = cp.GetAssignments();
                    iconKey = assignments.Any(x => x.GetAssignedHardware() == string.Empty) ? GetNameOf(() => Properties.Resources.ConnectedPartially) : GetNameOf(() => Properties.Resources.Connected);
                }
            }
            else
            {
                if (item is DescriptorBinaryOutput)
                    iconKey = GetNameOf(() => Properties.Resources.BinaryOutput);
                if (item is DescriptorIndicator)
                    iconKey = GetNameOf(() => Properties.Resources.Indicator);
                if (item is DescriptorRange)
                    iconKey = GetNameOf(() => Properties.Resources.Encoder);
                if (item is DescriptorValue)
                    iconKey = GetNameOf(() => Properties.Resources.Button);
                if (item is RangeUnion)
                    iconKey = GetNameOf(() => Properties.Resources.RangeUnion);
                if (item is FakeVariable)
                    iconKey = GetNameOf(() => Properties.Resources.FakeVariable);
                if (item is FsuipcVariable)
                    iconKey = GetNameOf(() => Properties.Resources.FsuipcVariable);
                if (item is MemoryPatchVariable)
                    iconKey = GetNameOf(() => Properties.Resources.MemoryVariable);
            }
            if (iconKey == null)
                return null;
            if (IconsCache.ContainsKey(iconKey))
            {
                return IconsCache[iconKey];
            }
            else
            {
                var iconBitmap = Properties.Resources.ResourceManager.GetObject(iconKey, Properties.Resources.Culture);
                var bc = new WpfBitmapConverter();
                var icon = (ImageSource)bc.Convert(iconBitmap, typeof(ImageSource), null, CultureInfo.InvariantCulture);
                IconsCache.Add(iconKey, icon);
                return icon;
            }

            
            
        }
        private static TreeViewItem CreateTreeViewItem(string text, object connectedObject, TreeItemType treeItemType, ImageSource icon)
        {
            var item = new TreeViewItem();

            if (icon != null)
            {
                // create stack panel
                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                // create Image
                var image = new Image {Source = icon, Width = 16, Height = 16};
                // Label
                var lbl = new Label { Content = text };

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
        private static readonly Dictionary<string, ImageSource> IconsCache = new Dictionary<string, ImageSource>();
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
            item.Name = GetTreeItemText(tree.SelectedItem as TreeViewItem);
            if (item.Type != TreeItemType.Panel)
            {
                var parentItem = ((TreeViewItem)((TreeViewItem)tree.SelectedItem).Parent);
                var parentText = GetTreeItemText(parentItem);
                item.FullName = parentText + "." + item.Name;
            }
            else
                item.FullName = item.Name;
            return item;
        }
        private static string GetTreeItemText(TreeViewItem tvi)
        {
            if (tvi.Header is StackPanel)
            {
                if ((tvi.Header as StackPanel).Children.Count < 2)
                    return null;
                var item = ((StackPanel)tvi.Header).Children[1] as Label;
                if (item == null)
                    return null;
                return (string)item.Content;
            }
            return (string)tvi.Header;

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

        private void OnAnySaveButtonClicked(StackPanel panel, bool isVariableTree)
        {
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            var clearEvents = Profile.ControlProcessor.GetShutDownEventsForAllControlProcessors();
            HardwareManager.PostOutgoingEvents(clearEvents.ToList());
            
            // Перерисовать дерево, развернуть необходимый узел и выделить переменную/дексриптор/панель. Нужно при переезде из одной панели в другую
            if (panel.Children.Count == 0)
                return;
            var errors = panel.Children.Cast<object>().Select(child => ((IEditor) child).IsCorrectData()).Where(res => !res.IsDataFilledCorrectly).Aggregate(string.Empty, (current, res) => current + res.ErrorsText);
            if (!string.IsNullOrEmpty(errors))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorMessageDataIsIncorrect) + errors;
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            _routerCore.Pause();
            foreach (var child in panel.Children)
                ((IEditor) child).Save();

            Profile.ControlProcessor.UpdateAssignments();

            if(isVariableTree)
                RenewVariablesTrees();
            else
                RenewTrees();
            Profile.Save(ApplicationSettings.DisablePersonalProfile);
            _routerCore.Start();
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
            return item == null ? null : ((ComboBoxItem) combobox.SelectedItem).Tag;
        }

        private void FillSelectLanguageCombobox()
        {
            _selectLanguage.Items.Clear();
            var languageProfiles = LanguageManager.GetProfileList();
            foreach (var profile in languageProfiles)
                _selectLanguage.Items.Add(profile);
            _selectLanguage.Text = string.IsNullOrEmpty(ApplicationSettings.DefaultLanguage) ? string.Empty : ApplicationSettings.DefaultLanguage;
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
            Settings.Default.DefaultLanguage = _selectLanguage.Text;
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
                    var index = textInRun.IndexOf(keyword, StringComparison.Ordinal);
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
                    _formulaResultHex.Text = (result.CalculatedDoubleValue%1) == 0
                        ? ((int) result.CalculatedDoubleValue).ToString("X")
                        : string.Empty;
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
                ((CalcTokenNumber) tokenToPreprocess).Value = string.IsNullOrEmpty(_formulaEditorInputValueDec.Text)
                    ? 0
                    : double.Parse(_formulaEditorInputValueDec.Text, CultureInfo.InvariantCulture);
            }
            return tokenToPreprocess;
        }

        private readonly CalculatorVariableAccessAddon _cvaa = new CalculatorVariableAccessAddon();

        private void InitializeCalculator()
        {
            _calculator.RegisterTokenizer(FormulaResultTokenizer);
            _calculator.RegisterPreprocessor(FormulaResultProcessor);
            _calculator.RegisterTokenizer(_cvaa.VariableTokenizer);
            _calculator.RegisterPreprocessor(_cvaa.VariablePreprocessor);
        }

        private void FormulaEditorInputValueDecPreviewTextInput(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Utils.IsNumeric(e.Text) || ((TextBox) sender).Text.Length > 8;
        }

        private void FormulaEditorInputValueHexPreviewTextInput(object sender,
            System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Utils.IsHexNumber(e.Text) || ((TextBox) sender).Text.Length > 8;
        }

        private void FormulaEditorInputValueDecTextChanged(object sender, TextChangedEventArgs e)
        {
            _formulaEditorInputValueDec.TextChanged -= FormulaEditorInputValueDecTextChanged;
            _formulaEditorInputValueHex.TextChanged -= FormulaEditorInputValueHexTextChanged;
            if (string.IsNullOrEmpty(_formulaEditorInputValueDec.Text))
                _formulaEditorInputValueHex.Text = string.Empty;
            int resultInt;
            _formulaEditorInputValueHex.Text = int.TryParse(_formulaEditorInputValueDec.Text, out resultInt)
                ? resultInt.ToString("X")
                : string.Empty;
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
            if (int.TryParse(_formulaEditorInputValueHex.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                out result))
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
            var profiles = Profile.GetProfileList(ProfileItemPrivacyType.Public.ToString());
            foreach (var profile in profiles)
            {
                var cbi = new ComboBoxItem {Content = profile.Key, Tag = profile.Value};
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
            _routerCore.Stop();
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
                    Settings.Default.DefaultProfile = name;
                    SetTitle(name);
                }
            }
            else
            {
                _selectProfile.Text = string.Empty;
                SetTitle();
            }
            _routerCore.Start();
            RenewTrees();
            RenewVariablesTrees();
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
        /// <returns>true - профиль загружен</returns>
        private bool LoadProfile(string profileName)
        {
            if (profileName == null)
                return false;
            PauseRouterOnChangeProfile();
            var loadResult = Profile.LoadProfileByName(profileName);
            ApplicationSettings.DefaultProfile = profileName;
            Settings.Default.DefaultProfile = profileName;
            SetTitle(loadResult ? ApplicationSettings.DefaultProfile : null);
            FillSelectProfileCombobox(ApplicationSettings.DefaultProfile);
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
            Profile.CreateNewProfile(ApplicationSettings.DisablePersonalProfile);
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
        /// Переименовать профиль
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameProfileClick(object sender, RoutedEventArgs e)
        {
            var name = Profile.RenameProfile(ApplicationSettings.DisablePersonalProfile);
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
        }
        private void TurnControlsSynchronizationOffChecked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.ControlsSynchronizationIsOff = true;
        }
        private void _joystickBindByInstanceGuid_Checked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.JoystickBindByInstanceGuid = true;
        }

        private void _joystickBindByInstanceGuid_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.JoystickBindByInstanceGuid = false;
        }

        private void _dump_Click(object sender, RoutedEventArgs e)
        {
            if (_routerCore.IsWorking())
                _routerCore.Dump();
        }

        private void _cloneVariable_Click(object sender, RoutedEventArgs e)
        {
            var treeSelectedItem = GetTreeSelectedItem(_variablesTree);
            if (treeSelectedItem == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoItemInTreeSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if ((treeSelectedItem.Object as IVariable) != null)
            {
                var newItem = ((VariableBase) treeSelectedItem.Object).GetCopy();
                newItem.Name = newItem.Name + "(2)";
                ShowVariable(newItem);
                newItem.Id = GlobalId.GetNew();
            }
            if ((treeSelectedItem.Object as Panel) != null)
            {
                var newItem = ((Panel)treeSelectedItem.Object).GetCopy();
                newItem.Name = newItem.Name + "(2)";
                ShowPanel(newItem, _variablesPanel);
                newItem.SetId(GlobalId.GetNew());
            }
        }
        private void _cloneAccessDescriptor_Click(object sender, RoutedEventArgs e)
        {
            var treeSelectedItem = GetTreeSelectedItem(_accessDescriptorsTree);
            if (treeSelectedItem == null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorNoItemInTreeSelected);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
                
            if ((treeSelectedItem.Object as IAccessDescriptor) != null)
            {
                var newItem = ((DescriptorBase)treeSelectedItem.Object).GetCopy();
                newItem.SetName(newItem.GetName() + "(2)");
                ShowAccessDescriptorEditors(newItem);
                newItem.SetId(GlobalId.GetNew());
            }
            if ((treeSelectedItem.Object as Panel) != null)
            {
                var newItem = ((Panel)treeSelectedItem.Object).GetCopy();
                newItem.Name = newItem.Name + "(2)";
                ShowPanel(newItem, _accessDescriptorPanel);
                newItem.SetId(GlobalId.GetNew());
            }
        }
        private void _disablePersonalProfile_Checked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.DisablePersonalProfile = true;
        }
        private void _disablePersonalProfile_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.DisablePersonalProfile = false;
        }

        private void _mergePersonalAndPublicProfile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(LanguageManager.GetPhrase(Phrases.EditorMessageSureToMovePrivateProfileToPublic), LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _routerCore.Pause();
            Profile.MakeAllItemsPublic();
            RenewVariablesTrees();
            RenewTrees();
            Profile.Save(ApplicationSettings.DisablePersonalProfile);
            _routerCore.Start();
        }

        private void _initializeVarNamesFromMapFile_Click(object sender, RoutedEventArgs e)
        {
            UpdateVarNamesFromMapFileUI mmu = new UpdateVarNamesFromMapFileUI();
            mmu.Show();
        }

        private void _updateVarOffsetsFromMapFile_Click(object sender, RoutedEventArgs e)
        {
            UpdateVarOffsetsFromMapFileUI mmu = new UpdateVarOffsetsFromMapFileUI();
            mmu.Show();
        }
    }
}

