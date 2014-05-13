using FlexRouter.CalculatorRelated.Tokens;

namespace FlexRouter.CalculatorRelated
{
    static class CalculatorErrorsLocalizer
    {
        /// <summary>
        /// Преобразование кода ошибки в локализованную строку
        /// </summary>
        /// <param name="tokenError">код ошибки</param>
        /// <returns>локализованная строка с текстом ошибки</returns>
        public static string TokenErrorToString(TokenError tokenError)
        {
            switch (tokenError)
            {
                case TokenError.UnexpectedSymbols:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorUnexpectedSymbols);
                case TokenError.ClosingBracketNotOpened:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorClosingBracketNotOpened);
                case TokenError.LastTokenCantBeOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorLastTokenCantBeOperation);
                case TokenError.OpeningBracketNotClosed:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorOpeningBracketNotClosed);
                case TokenError.SimilarTokensOneByOne:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorSimilarTokensOneByOne);
                case TokenError.TokenMustBeOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenMustBeOperation);
                case TokenError.TokenMustBeValue:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenMustBeValue);
                case TokenError.MultipluDotInNumber:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorMultiplyDotInNumber);
                case TokenError.TokenPointsAbsentItem:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenPointsAbsentItem);
                default:
                    return string.Empty;
            }
        }
    }
}
