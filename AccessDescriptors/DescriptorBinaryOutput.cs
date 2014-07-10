using System.Drawing;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.Localizers;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorBinaryOutput : DescriptorOutputBase, IBinaryOutputMethods
    {
        public override string GetDescriptorType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryBinaryOutput);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.BinaryOutput;
        }

        public bool GetLineState()
        {
            if (!IsPowerOn())
                return false;
            var calcResult = CalculatorE.ComputeFormula(GetFormula());
            if (calcResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.FormulaWasEmpty)
                return true;
            return calcResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.BooleanResult && calcResult.CalculatedBoolBoolValue;
        }
    }
}
