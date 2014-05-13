namespace FlexRouter.AccessDescriptors.Helpers
{
    public class EditorFieldsErrors
    {
        public EditorFieldsErrors(string errorText)
        {
            IsDataFilledCorrectly = string.IsNullOrEmpty(errorText);
            ErrorsText = errorText;
        }
        /// <summary>
        /// Корректно ли заполнены поля в форме редактора
        /// </summary>
        public bool IsDataFilledCorrectly;
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorsText;
    }
    interface IEditor
    {
        /// <summary>
        /// Сохранить данные
        /// </summary>
        void Save();
        /// <summary>
        /// Сменить язык
        /// </summary>
        void Localize();
        /// <summary>
        /// Изменены ли данные в редакторе
        /// </summary>
        /// <returns></returns>
        bool IsDataChanged();
        /// <summary>
        /// Корректно ли заполнены поля в форме редактора
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        EditorFieldsErrors IsCorrectData();
    }
}
