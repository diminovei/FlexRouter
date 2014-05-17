using FlexRouter.CalculatorRelated.Tokens;

namespace FlexRouter.CalculatorRelated
{
    static class CalculatorErrorsLocalizer
    {
        /// <summary>
        /// Преобразование кода ошибки в локализованную строку
        /// </summary>
        /// <param name="formulaError">код ошибки</param>
        /// <returns>локализованная строка с текстом ошибки</returns>
        public static string TokenErrorToString(FormulaError formulaError)
        {
            switch (formulaError)
            {
                case FormulaError.UnexpectedSymbols:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorUnexpectedSymbols);
                case FormulaError.ClosingBracketNotOpened:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorClosingBracketNotOpened);
                case FormulaError.LastTokenCantBeOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorLastTokenCantBeOperation);
                case FormulaError.OpeningBracketNotClosed:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorOpeningBracketNotClosed);
                case FormulaError.SimilarTokensOneByOne:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorSimilarTokensOneByOne);
                case FormulaError.TokenMustBeOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenMustBeOperation);
                case FormulaError.TokenMustBeValue:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenMustBeValue);
                case FormulaError.MultipluDotInNumber:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorMultiplyPointInNumber);
                case FormulaError.TokenPointsAbsentItem:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorTokenPointsAbsentItem);
                case FormulaError.DotCantBeLastSymbolOfNumber:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorPointCantBeLastSymbolOfNumber);
                case FormulaError.UnknownMathOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorUnknownMathOperation);
                case FormulaError.UnknownLogicOperation:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorUnknownLogicOperation);
                case FormulaError.CantOperateMathAndLogicValues:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorCantOperateMathAndLogicValues);
                case FormulaError.ThisFormulaPartMustBeLogic:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorThisFormulaPartMustBeLogic);
                case FormulaError.ThisFormulaPartMustBeMath:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorThisFormulaPartMustBeMath);
                case FormulaError.Exception:
                    return LanguageManager.GetPhrase(Phrases.FormulaErrorException);
                default:
                    return "Untranslated: " + formulaError;
            }
        }
    }
}
