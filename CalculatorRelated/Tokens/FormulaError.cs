namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Возможные ошибки при парсинге токена
    /// </summary>
    public enum FormulaError
    {
        Ok,
        UnexpectedSymbols,
        SimilarTokensOneByOne,
        TokenMustBeOperation,
        TokenMustBeValue,
        LastTokenCantBeOperation,
        OpeningBracketNotClosed,
        ClosingBracketNotOpened,
        MultipluDotInNumber,
        DotCantBeLastSymbolOfNumber,
        TokenPointsAbsentItem,
        FormulaIsEmpty,
        UnknownMathOperation,
        UnknownLogicOperation,
        CantOperateMathAndLogicValues,
        ThisFormulaPartMustBeLogic,
        ThisFormulaPartMustBeMath,
        Exception,
        DivisionByZero
    }
}
