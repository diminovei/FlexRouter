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
    public partial class UpdateVarNamesFromMapFileUI : Window
    {
        private List<VariableNameFromMapFile> variableNameFromMapFile = new List<VariableNameFromMapFile>();
        private string openMapFileDialogHeader;
        private string saveCsvFileDialogHeader;
        private string messageBoxMessage;
        
        public UpdateVarNamesFromMapFileUI()
        {
            InitializeComponent();
            Localize();
            var moduleList = MapFileManipulator.GetListOfModulesUsedInProfile();
            foreach(var m in moduleList)
            {
                _modulesList.Items.Add(m);
            }
            EnableControls();
        }
        private void Localize()
        {
            Title = LanguageManager.GetPhrase(Phrases.MapFileUIHeaderInitializeNames);
            _getVarialbesName.Content = LanguageManager.GetPhrase(Phrases.MapFileUISearchVariablesNameInMapFile);
            _relativeMatch.Content = LanguageManager.GetPhrase(Phrases.MapFileUIRelativeMatch);

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
            variableNameFromMapFile.Clear();
            EnableControls();
        }
        private void _applyVariableNamesToProfile_Click(object sender, RoutedEventArgs e)
        {
            AssignVariableNamesFromMapFile();
        }
        private void EnableControls()
        {
            _saveNotFoundVariablesToFile.IsEnabled = variableNameFromMapFile.Count() != 0;
            _applyVariableNamesToProfile.IsEnabled = variableNameFromMapFile.Count() != 0;
            _getVarialbesName.IsEnabled = _modulesList.SelectedItem != null;
        }
        private void _getVarialbesName_Click(object sender, RoutedEventArgs e)
        {
            if (_modulesList.SelectedValue != null)
            {
                var mapFilePath = OpenMapFileDialogue();
                if (mapFilePath != null)
                {
                    variableNameFromMapFile = MapFileManipulator.GetVariableNamesFromMapFile(mapFilePath, _modulesList.SelectedValue.ToString()).ToList();
                    _exactMatchCount.Content = variableNameFromMapFile.Where(x => x.DistanceToClosestVarBelow == 0).Count();
                    _relativeMatchCount.Content = variableNameFromMapFile.Where(x => x.DistanceToClosestVarBelow != 0).Count();
                }
                EnableControls();
            }
        }
        private void _saveNotFoundVariablesToFile_Click(object sender, RoutedEventArgs e)
        {
            SaveVariablesNameToFile();
        }
        private void SaveVariablesNameToFile()
        {
            var stringToSave = "Offset\tDistanceToClosestVarBelow\tNameInMapFile\tVariableName\tPanelName\n";
            var variables = Profile.VariableStorage.GetAllVariables();
            var panels = Profile.PanelStorage.GetAllPanels();

            foreach (var i in variableNameFromMapFile)
            {
                var variableFromProfile = (MemoryPatchVariable)variables.Where(x => x is MemoryPatchVariable && i.VarId == x.Id).First();
                var panel = panels.Where(x => x.Id == variableFromProfile.PanelId).First();
                stringToSave +=
                        variableFromProfile.Offset.ToString("x2") + "\t" +
                        i.DistanceToClosestVarBelow.ToString("x2") + "\t" +
                        i.NameInMapFile + "\t" +
                        variableFromProfile.Name + "\t" +
                        panel.Name + "\n";
            }
            SaveTextToFile(stringToSave);
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
        private void AssignVariableNamesFromMapFile()
        {
            var variables = Profile.VariableStorage.GetAllVariables();
            foreach (var v in variables)
            {
                if (!(v is MemoryPatchVariable))
                    continue;
                var memoryPatchVariable = v as MemoryPatchVariable;

                foreach (var names in variableNameFromMapFile)
                {
                    if (memoryPatchVariable.GetId() != names.VarId)
                        continue;
                    memoryPatchVariable.NameInMapFile = $"{names.NameInMapFile}+0x{names.DistanceToClosestVarBelow}";
                }
            }
            Profile.Save(ApplicationSettings.DisablePersonalProfile);
            MessageBox.Show(messageBoxMessage);
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
    }
}
