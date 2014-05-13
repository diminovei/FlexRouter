namespace FlexRouter.CalculatorRelated.Tokens
{
    public interface ICalcToken
    {
        int Id { get; set; }
        int Position { get; set; }
        TokenError Error { get; set; }
        int GetTokenTextLentgh();
        int GetNextTokenPosition();
    }
}
