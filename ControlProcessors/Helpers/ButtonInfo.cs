using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors
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
    internal class ButtonInfo
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id;
        /// <summary>
        /// Порядок отображения
        /// </summary>
        public int Order;
        /// <summary>
        /// Имя
        /// </summary>
        public string Name;
        /// <summary>
        /// Кнопка инвертирована
        /// </summary>
        public bool Invert;
        /// <summary>
        /// Нажата ли 
        /// </summary>
        public bool IsOn;
        /// <summary>
        /// Нажата ли 
        /// </summary>
        public string AssignedHardware;
        /// <summary>
        /// Сравнить данные State и ButtonInfo
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool CompareState(AccessDescriptorState state)
        {
            return state.Id == Id;
        }
        #region ToggleEmulator
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
        #endregion
    }
}
