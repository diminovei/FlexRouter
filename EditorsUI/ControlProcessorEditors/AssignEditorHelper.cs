using System.Data;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    class AssignEditorHelper
    {
        private readonly IControlProcessor _assignedControlProcessor;
        private DataTable _dataTable = new DataTable();

        public AssignEditorHelper(IControlProcessor processor)
        {
            _assignedControlProcessor = processor;
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
            if (_assignedControlProcessor.HasInvertMode())
                _dataTable.Columns.Add(LanguageManager.GetPhrase(Phrases.EditorInvert), typeof(bool));
            var assignmentList = _assignedControlProcessor.GetAssignments();
            foreach (var assignment in assignmentList)
            {
                if (_assignedControlProcessor.HasInvertMode())
                    _dataTable.Rows.Add(assignment.GetConnector().Id, assignment.GetConnector().Name, assignment.GetAssignedHardware(), assignment.GetInverseState());
                else
                    _dataTable.Rows.Add(assignment.GetConnector().Id, assignment.GetConnector().Name, assignment.GetAssignedHardware());
            }
            return _dataTable.AsDataView();
        }

        public void Save(int selectedRowIndex, string hardware)
        {
            var rowCounter = -1;
            var assignments = _assignedControlProcessor.GetAssignments();
            // На случай, если меняется только инверсия, можно принимать и пустой hardware
            if (selectedRowIndex == -1 || (string.IsNullOrEmpty(hardware)&& assignments.Length == 0))
                return;
            foreach (var row in _dataTable.Rows)
            {
                rowCounter++;
                if(rowCounter!=selectedRowIndex)
                    continue;
                foreach (IAssignment a in assignments)
                {
                    if (a.GetConnector().Id == int.Parse((string) ((DataRow) row).ItemArray[0]))
                    {
                        a.SetAssignedHardware(string.IsNullOrEmpty(hardware) ? a.GetAssignedHardware() : hardware);
                        if (_assignedControlProcessor.HasInvertMode())
                            a.SetInverseState((bool)((DataRow)row).ItemArray[3]);
                        _assignedControlProcessor.SetAssignment(a);
                        break;
                    }
                }
            }
            HardwareManager.ResendLastControlEvent(hardware);
        }

        public string LocalizeHardwareLabel(HardwareModuleType hardwareModuleType)
        {
            if (hardwareModuleType == HardwareModuleType.Button)
                return LanguageManager.GetPhrase(Phrases.HardwareButton);
            if (hardwareModuleType == HardwareModuleType.Encoder)
                return LanguageManager.GetPhrase(Phrases.HardwareEncoder);
            if (hardwareModuleType == HardwareModuleType.Indicator)
                return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
            if (hardwareModuleType == HardwareModuleType.LedMatrixIndicator)
                return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
            if (hardwareModuleType == HardwareModuleType.BinaryOutput)
                return LanguageManager.GetPhrase(Phrases.HardwareBinaryOutput);
            if (hardwareModuleType == HardwareModuleType.Axis)
                return LanguageManager.GetPhrase(Phrases.HardwareAxis);
            return string.Empty;
        }
    }
}
