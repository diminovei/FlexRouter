namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Класс для обмена данными между DataGrid редактора и ControlProcessor'ом
    /// </summary>
    public class Assignment
    {
        /// <summary>
        /// Идентификатор состояния
        /// </summary>
        public int StateId;
        /// <summary>
        /// Имя состояния
        /// </summary>
        public string StateName;
        /// <summary>
        /// Назначение на состояние. В качестве назначения может быть идентификатор железа
        /// </summary>
        public string AssignedItem;
        /// <summary>
        /// Инвертировать направление
        /// </summary>
        public bool Inverse;
    }
}
