using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.AssignedHardware
{
    /// <summary>
    /// Индикатор состояния Toggle
    /// </summary>
    public enum ToggleState
    {
        DoNothing,
        MakeOn,
        MakeOff
    }
    /// <summary>
    /// Информация о кнопке
    /// </summary>
    internal class AssignmentForButton : IAssignment
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

        public bool IsInverseInUse()
        {
            return true;
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
        
        /// <summary>
        /// Нажата ли 
        /// </summary>
        public bool IsOn;
        /// <summary>
        /// Внутренний индикатор состояния Toggle
        /// </summary>
        private enum ToggleEmulatorStateType
        {
            SecondOff,
            FirstOn,
            FirstOff,
            SecondOn,
        }
        private ToggleEmulatorStateType _toggleEmulatorState;

        public ToggleState Toggle(bool direction)
        {
            if (_toggleEmulatorState == ToggleEmulatorStateType.SecondOff && direction)
            {
                _toggleEmulatorState = ToggleEmulatorStateType.FirstOn;
                return ToggleState.MakeOn;
            }
            if (_toggleEmulatorState == ToggleEmulatorStateType.FirstOn && !direction)
            {
                _toggleEmulatorState = ToggleEmulatorStateType.FirstOff;
                return ToggleState.DoNothing;
            }
            if (_toggleEmulatorState == ToggleEmulatorStateType.FirstOff && direction)
            {
                _toggleEmulatorState = ToggleEmulatorStateType.SecondOn;
                return ToggleState.MakeOff;
            }
            _toggleEmulatorState = ToggleEmulatorStateType.SecondOff;
            return ToggleState.DoNothing;
        }
    }
}
