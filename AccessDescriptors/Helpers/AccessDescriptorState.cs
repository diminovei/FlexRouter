namespace FlexRouter.AccessDescriptors.Helpers
{
    /// <summary>
    /// Класс описывает "торчок" (state) описателя доступа, на который можно повесить элемент управления
    /// </summary>
    public class AccessDescriptorState
    {
        /// <summary>
        /// Идентификатор состояния AccessDescriptor
        /// </summary>
        public int Id;
        /// <summary>
        /// Имя состояния AccessDescriptor
        /// </summary>
        public string Name;
        /// <summary>
        /// Порядковый номер при отображении в редакторе
        /// </summary>
        public int Order;
    }
}
