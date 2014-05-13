namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Возможные форматирующие символы
    /// </summary>
    public enum CalcFormatterType
    {
        Space,
        Tab,
        NewLine
    }

    public class CalcTokenFormatter : CalcTokenBase
    {
        public CalcFormatterType Formatter;

        public CalcTokenFormatter(int currentTokenPosition) : base(currentTokenPosition)
        {
        }

        /// <summary>
        /// Попытка извлечь незначимый токен (пробел, конец строки, ...)
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, int currentTokenPosition)
        {
            var token = new CalcTokenFormatter(currentTokenPosition);

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