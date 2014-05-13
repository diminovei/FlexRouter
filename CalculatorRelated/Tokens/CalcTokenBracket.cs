namespace FlexRouter.CalculatorRelated.Tokens
{
    public enum CalcBracket
    {
        Open,
        Close
    }
    public class CalcTokenBracket : CalcTokenBase
    {
        public CalcBracket Bracket;

        public CalcTokenBracket(int currentTokenPosition) : base(currentTokenPosition)
        {
        }

        /// <summary>
        /// Попытка извлечь токен - скобку
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, int currentTokenPosition)
        {
            if (formula[currentTokenPosition] == '(')
                return new CalcTokenBracket(currentTokenPosition) { Bracket = CalcBracket.Open, TokenText = "(" };
            if (formula[currentTokenPosition] == ')')
                return new CalcTokenBracket(currentTokenPosition) { Bracket = CalcBracket.Close, TokenText = ")" };
            return null;
        }
    }
}