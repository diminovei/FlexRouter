using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorBinaryOutput : DescriptorOutputBase, IBinaryOutputMethods
    {
        public override string GetDescriptorName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryBinaryOutput);
        }

        public DescriptorBinaryOutput()
        {
//            CalculatorE.RegisterTokenizer(FormulaResultTokenizer);
//            CalculatorE.RegisterPreprocessor(FormulaResultProcessor);
        }

        public bool GetLineState()
        {
            if (!IsPowerOn())
                return false;
            var calcResult = CalculatorE.ComputeFormula(GetFormula());
            if (!calcResult.CanUseBooleanValue())
                return false;
            return calcResult.CalculatedBoolValue;
        }
  /*      // Как потом раздать результат в другие переменные? Ввести в формулы термин "[R]"?
        private ICalcToken FormulaResultProcessor(ICalcToken tokenToPreprocess)
        {
            var sourceToken = tokenToPreprocess as CalcTokenNumber;
            if (sourceToken == null)
                return tokenToPreprocess;
            var destToken = new CalcTokenBoolean {Value = sourceToken.Value != 0};
            return destToken;
        }*/
    }
}
