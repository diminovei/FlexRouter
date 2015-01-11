namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Токен участвует в конструкции если значение формулы true, то возвращаем X, иначе обрабатываем следующую формулу. Записывается например, так: 1==1 ? 3 ; 4 
    /// </summary>
    class CalcTokenFormulaSeparator : CalcTokenBase
    {
        public CalcTokenFormulaSeparator(int currentTokenPosition) : base(currentTokenPosition)
        {
        }

        public static string ToText()
        {
            return ";";
        }
        /// <summary>
        /// Попытка извлечь токен
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, int currentTokenPosition)
        {
            var token = new CalcTokenFormulaSeparator(currentTokenPosition);
            if (formula[currentTokenPosition] != ';')
                return null;
            token.TokenText = ToText();
            return token;
        }
    }
}
