using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.AssignedHardware
{
    /// <summary>
    /// Класс для обмена данными между DataGrid редактора и ControlProcessor'ом
    /// </summary>
    class Assignment : IAssignment
    {
        private Connector _connector;
        public Connector GetConnector()
        {
            return _connector;
        }

        public void SetConnector(Connector connector)
        {
            _connector = connector;
        }
        /// <summary>
        /// Назначение на состояние. В качестве назначения может быть идентификатор железа
        /// </summary>
        private string _assignedItem = string.Empty;
        public string GetAssignedHardware()
        {
            return _assignedItem;
        }

        public void SetAssignedHardware(string assignedHardware)
        {
            _assignedItem = assignedHardware;
        }
        /// <summary>
        /// Инвертировать направление
        /// </summary>
        private bool _inverse;
        public bool GetInverseState()
        {
            return _inverse;
        }

        public void SetInverseState(bool inverseState)
        {
            _inverse = inverseState;
        }
    }
}
