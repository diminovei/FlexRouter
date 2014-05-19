namespace FlexRouter.CalculatorRelated.Tokens
{
    public interface ICalcToken
    {
        int Id { get; set; }
        int Position { get; set; }
        FormulaError Error { get; set; }
        int GetTokenTextLength();
        int GetNextTokenPosition();
    }
}
