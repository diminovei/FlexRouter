namespace FlexRouter.CalculatorRelated.Tokens
{
    public abstract class CalcTokenBase : ICalcToken
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string TokenText;
        public FormulaError Error { get; set; }
        public int GetNextTokenPosition()
        {
            return Position + TokenText.Length;
        }
        public int GetTokenTextLength()
        {
            return TokenText.Length;
        }
        protected CalcTokenBase(int currentTokenPosition)
        {
            Error = FormulaError.Ok;
            Position = currentTokenPosition;
        }
    }
}
