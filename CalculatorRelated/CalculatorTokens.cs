namespace FlexRouter.CalculatorRelated
{
    public enum ProcessingMathFormulaError
    {
        Ok,
        FormulaIsEmpty,
        LogicConditionIsIncorrect
    }
    public enum ProcessingLogicFormulaError
    {
        Ok,
        CantCompareDifferentTypes,
        LogicFormulaResultIsNotBoolean
    }
    public class ProcessingMathFormulaResult
    {
        public double Value;
        public ProcessingMathFormulaError Error;
    }
    public class ProcessingLogicFormulaResult
    {
        public bool Value;
        public ProcessingLogicFormulaError Error;
    }
/*    public enum TokenType
    {
        Unknown,
        Formatter,
        Number,
        LogicOperation,
        MathOperation,
        Bracket,
        Boolean
    }*/


/*    public class CalcTokenNegative : CalcTokenBase
    {
        public CalcLogicOperation LogicOperation;
    }*/
}
