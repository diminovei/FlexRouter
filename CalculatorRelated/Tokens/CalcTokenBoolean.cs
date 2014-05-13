namespace FlexRouter.CalculatorRelated.Tokens
{
    public enum CalcBoolean
    {
        True,
        False
    }
    public class CalcTokenBoolean : CalcTokenBase
    {
        public bool Value;

        public CalcTokenBoolean(int currentTokenPosition) : base(currentTokenPosition)
        {
        }
    }
}
