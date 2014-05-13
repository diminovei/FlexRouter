namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Токен участвует в конструкции если значение формулы true, то возвращаем X, иначе обрабатываем следующую формулу. Записывается например, так: 1==1 ? 3 ; 4 
    /// </summary>
    class CalcTokenIfStatement : CalcTokenBase
    {
        public static string ToText()
        {
            return "?";
        }
        /// <summary>
        /// Попытка извлечь токен
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, int currentTokenPosition)
        {
            var token = new CalcTokenIfStatement { Position = currentTokenPosition };

            for (var i = currentTokenPosition; i < formula.Length; i++)
            {
                switch (formula[i])
                {
                    case ' ':
                        token.Formatter = CalcFormatterType.Space;
                        token.TokenText += formula[i];
                        break;
                    case '\t':
                        token.Formatter = CalcFormatterType.Tab;
                        token.TokenText += formula[i];
                        break;
                    case '\n':
                    case '\r':
                        token.Formatter = CalcFormatterType.NewLine;
                        token.TokenText += formula[i];
                        break;
                    default:
                        return null;
                }
            }
            return token.TokenText == string.Empty ? null : token;
        }
    }
}
