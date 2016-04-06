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
}
