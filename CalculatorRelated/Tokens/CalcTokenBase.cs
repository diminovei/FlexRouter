namespace FlexRouter.CalculatorRelated.Tokens
{
    public abstract class CalcTokenBase : ICalcToken
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string TokenText;
        public TokenError Error { get; set; }
        public int GetNextTokenPosition()
        {
            return Position + TokenText.Length;
        }
        public int GetTokenTextLentgh()
        {
            return TokenText.Length;
        }
        protected CalcTokenBase(int currentTokenPosition)
        {
            Error = TokenError.Ok;
            Position = currentTokenPosition;
        }
    }
}
