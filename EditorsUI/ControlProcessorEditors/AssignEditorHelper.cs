using System.Data;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    class AssignEditorHelper
    {
        private readonly IControlProcessor _assignedControlProcessor;
        private DataTable _dataTable = new DataTable();
        private readonly bool _enableInverse;

        public AssignEditorHelper(IControlProcessor processor, bool enableInverse)
        {
            _assignedControlProcessor = processor;
            _enableInverse = enableInverse;
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        public DataView GetGridData()
        {
            _dataTable = new DataTable();

            var dc = _dataTable.Columns.Add("Id");
            dc.ReadOnly = true;
            dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorState));
            dc.ReadOnly = true;

            dc = _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorHardware));
            dc.ReadOnly = true;
            if (_enableInverse)
                _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorInvert), typeof(bool));
            var assignmentList = _assignedControlProcessor.GetAssignments();
            foreach (var assignment in assignmentList)
            {
                if(_enableInverse)
                    _dataTable.Rows.Add(assignment.StateId, assignment.StateName, assignment.AssignedItem, assignment.Inverse);
                else
                        _dataTable.Rows.Add(assignment.StateId, assignment.StateName, assignment.AssignedItem);
            }
            return _dataTable.AsDataView();
        }

        public void Save(int selectedRowIndex, string hardware)
        {
            foreach (var row in _dataTable.Rows)
            {
                var assignment = new Assignment
                {
                    StateId = int.Parse((string) ((DataRow) row).ItemArray[0]),
                    StateName = (string) ((DataRow) row).ItemArray[1]
                };
                if (selectedRowIndex!=-1 && (assignment.StateId == int.Parse((string)_dataTable.Rows[selectedRowIndex].ItemArray[0])) &&
                    !string.IsNullOrEmpty(hardware))
                    assignment.AssignedItem = hardware;
                else
                    assignment.AssignedItem = ((DataRow)row).ItemArray[2] is string ? (string)((DataRow)row).ItemArray[2] : null;
                assignment.Inverse = _enableInverse && (assignment.Inverse = (bool)((DataRow)row).ItemArray[3]);
                _assignedControlProcessor.SetAssignment(assignment);
                HardwareManager.ResendLastControlEvent(hardware);
            }
        }

        public string LocalizeHardwareLabel(HardwareModuleType hardwareModuleType)
        {
            if (hardwareModuleType == HardwareModuleType.Button)
                return LanguageManager.GetPhrase(Phrases.HardwareButton);
            if (hardwareModuleType == HardwareModuleType.Encoder)
                return LanguageManager.GetPhrase(Phrases.HardwareEncoder);
            if (hardwareModuleType == HardwareModuleType.Indicator)
                return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
            if (hardwareModuleType == HardwareModuleType.BinaryOutput)
                return LanguageManager.GetPhrase(Phrases.HardwareBinaryOutput);
            if (hardwareModuleType == HardwareModuleType.Axis)
                return LanguageManager.GetPhrase(Phrases.HardwareAxis);
            return string.Empty;
        }
    }
}
