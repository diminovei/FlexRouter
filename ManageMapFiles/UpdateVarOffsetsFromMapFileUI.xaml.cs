using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer.MethodMemoryPatch;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.Localizers;

namespace FlexRouter.ManageMapFiles
{
    /// <summary>
    /// Interaction logic for MapFileManageUI.xaml
    /// </summary>
    public partial class UpdateVarOffsetsFromMapFileUI : Window
    {
        private List<VariableOffsetFromMapFile> variableOffsetFromMapFile = new List<VariableOffsetFromMapFile>();
        private string openMapFileDialogHeader;
        private string saveCsvFileDialogHeader;
        private string messageBoxMessage;
        public UpdateVarOffsetsFromMapFileUI()
        {
            InitializeComponent();
            Localize();
            var moduleList = GetListOfModulesUsedInProfile();
            foreach(var m in moduleList)
            {
                _modulesList.Items.Add(m);
            }
            EnableControls();
        }
        private void Localize()
        {
            Title = LanguageManager.GetPhrase(Phrases.MapFileUIHeaderUpdateOffsets);
            _getVarialbesName.Content = LanguageManager.GetPhrase(Phrases.MapFileUISearchVariablesOffsetInMapFile);
            _relativeMatch.Content = LanguageManager.GetPhrase(Phrases.MapFileUINotFoundOffsets);

            _moduleSelectorLabel.Content = LanguageManager.GetPhrase(Phrases.MapFileUISelectModule);
            _exactMatch.Content = LanguageManager.GetPhrase(Phrases.MapFileUIExactMatch);
            _saveNotFoundVariablesToFile.Content = LanguageManager.GetPhrase(Phrases.MapFileUISaveMatchesToFile);
            _applyVariableNamesToProfile.Content = LanguageManager.GetPhrase(Phrases.MapFileUIApplyAndSave);
            openMapFileDialogHeader = LanguageManager.GetPhrase(Phrases.MapFileUISelectMapFileToLoad);
            saveCsvFileDialogHeader = LanguageManager.GetPhrase(Phrases.MapFileUISelectCsvFileToSave);
            messageBoxMessage = LanguageManager.GetPhrase(Phrases.MapFileUIMessageBoxDone);
        }
        private void _modulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            variableOffsetFromMapFile.Clear();
            EnableControls();
        }
        private void _applyVariableNamesToProfile_Click(object sender, RoutedEventArgs e)
        {
            AssignVariableOffsetsFromMapFile();
        }
        private void EnableControls()
        {
            _saveNotFoundVariablesToFile.IsEnabled = variableOffsetFromMapFile.Count() != 0;
            _applyVariableNamesToProfile.IsEnabled = variableOffsetFromMapFile.Count() != 0;
            _getVarialbesName.IsEnabled = _modulesList.SelectedItem != null;
        }
        /// <summary>
        /// Диалог открытия .map-файла
        /// </summary>
        /// <returns>путь к .map-файлу или null, если нажата клавиша Cancel</returns>
        private string OpenMapFileDialogue()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "map files (*.map)|*.map|All files (*.*)|*.*",
                Title = openMapFileDialogHeader
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null;
        }
        private void _getVarialbesName_Click(object sender, RoutedEventArgs e)
        {
            if (_modulesList.SelectedValue != null)
            {
                var mapFilePath = OpenMapFileDialogue();
                if (mapFilePath != null)
                {
                    variableOffsetFromMapFile = MapFileManipulator.GetVariableOffsetFromMapFile(mapFilePath, _modulesList.SelectedValue.ToString()).ToList();
                    _exactMatchCount.Content = variableOffsetFromMapFile.Count();

                    var variables = Profile.VariableStorage.GetAllVariables();
                    var memoryPathVariablesArray = variables.Where(x => x is MemoryPatchVariable && (x as MemoryPatchVariable).ModuleName.ToLower() == _modulesList.SelectedValue.ToString().ToLower()).ToArray();
                    _relativeMatchCount.Content = memoryPathVariablesArray.Count() - variableOffsetFromMapFile.Count();
                }
                EnableControls();
            }
        }
        private void _saveNotFoundVariablesToFile_Click(object sender, RoutedEventArgs e)
        {
            SaveOffsetsToFile();
        }
        /// <summary>
        /// Получить список уникальных имён модулей, используемых в профиле для MemoryPatchVariable
        /// </summary>
        /// <returns>Массив неповторяющихся имён модулей</returns>
        public static string[] GetListOfModulesUsedInProfile()
        {
            var variables = Profile.VariableStorage.GetAllVariables();
            var modules = variables.Where(x => x is MemoryPatchVariable).Select(y => ((MemoryPatchVariable)y).ModuleName).Distinct().ToArray();
            return modules;
        }
        private string OpenMapFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "map files (*.map)|*.map|All files (*.*)|*.*";
            dialog.Title = openMapFileDialogHeader;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null;
        }
        public void SaveTextToFile(string text)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            dialog.Title = saveCsvFileDialogHeader;

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, text, Encoding.UTF8);
            }
        }
        /// <summary>
        /// Записать в профиль имена и смещения от переменной в выбранном map-файле
        /// </summary>
        private void AssignVariableOffsetsFromMapFile()
        {
            var variables = Profile.VariableStorage.GetAllVariables();
            foreach (var v in variables)
            {
                if (!(v is MemoryPatchVariable))
                    continue;
                var memoryPatchVariable = v as MemoryPatchVariable;

                foreach (var offsets in variableOffsetFromMapFile)
                {
                    if (offsets.Offset == null)
                        continue;
                    if (memoryPatchVariable.GetId() != offsets.VarId)
                        continue;
                    memoryPatchVariable.Offset = (uint)offsets.Offset;
                }
            }
            Profile.Save(ApplicationSettings.DisablePersonalProfile);
            MessageBox.Show(messageBoxMessage);
        }
        private void SaveOffsetsToFile()
        {
            var stringToSave = "Old offset\tNew offset\tNameInMapFile\tVariableName\tPanelName\n";
            var variables = Profile.VariableStorage.GetAllVariables();
            var panels = Profile.PanelStorage.GetAllPanels();

            foreach (var i in variableOffsetFromMapFile)
            {
                var variableFromProfile = (MemoryPatchVariable)variables.Where(x => x is MemoryPatchVariable && i.VarId == x.Id).First();
                var panel = panels.Where(x => x.Id == variableFromProfile.PanelId).First();
                stringToSave +=
                        variableFromProfile.Offset.ToString("x2") + "\t" +
                        i.Offset == null ? "NOT FOUND\t" : ((uint)i.Offset).ToString("x2") + "\t" +
                        variableFromProfile.Name + "\t" +
                        panel.Name + "\n";
            }
            SaveTextToFile(stringToSave);
        }
    }
}
