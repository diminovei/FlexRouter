namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Возможные ошибки при парсинге токена
    /// </summary>
    public enum TokenError
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
        TokenPointsAbsentItem
    }
}
