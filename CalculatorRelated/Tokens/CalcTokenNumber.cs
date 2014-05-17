using System;
using System.Globalization;

namespace FlexRouter.CalculatorRelated.Tokens
{
    public class CalcTokenNumber : CalcTokenBase
    {
        public double Value;

        public CalcTokenNumber(int currentTokenPosition) : base(currentTokenPosition)
        {
        }

        /// <summary>
        /// Попытка извлечь токен-число
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="previousToken">предыдущий токен</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, ICalcToken previousToken, int currentTokenPosition)
        {
            var token = new CalcTokenNumber(currentTokenPosition);
            // Если начало текста токена, или следующий символ после знака минус не содержит цифр, значит это не число
            if (formula[currentTokenPosition] < '0' || formula[currentTokenPosition] > '9')
                return null;

            for (var i = currentTokenPosition; i < formula.Length; i++)
            {
                if ((formula[i] < '0' || formula[i] > '9') && formula[i] != '.')
                    break;
                // Число не может содержать две точки
                if (formula[i] == '.' && token.TokenText.Contains("."))
                {
                    token.Error = FormulaError.MultipluDotInNumber;
                    break;
                }
                token.TokenText += formula[i];
            }
            // Число не может заканчиваться на точку
            if (token.TokenText[token.TokenText.Length - 1] == '.')
                token.Error = FormulaError.DotCantBeLastSymbolOfNumber;
            token.Value = Convert.ToDouble(token.TokenText, CultureInfo.InvariantCulture);
            return token;
        }
    }
}