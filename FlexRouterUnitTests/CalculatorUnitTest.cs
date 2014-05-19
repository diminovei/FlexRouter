using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexRouterUnitTests
{
    [TestClass]
    public class CalculatorUnitTest
    {
        [TestMethod]
        public void CheckUnaryMinusFormula()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-1");
            Assert.AreEqual(-1, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("1+(-10)");
            Assert.AreEqual(-9, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("1+(-(-10))");
            Assert.AreEqual(11, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("(-(10+3))");
            Assert.AreEqual(-13, result.CalculatedDoubleValue);
        }

        [TestMethod]
        public void CheckFormulaTypeRecognition()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1+1");
            Assert.AreEqual(TypeOfComputeFormulaResult.DoubleResult, result.GetFormulaComputeResultType());
            result = calc.ComputeFormula("1!=1");
            Assert.AreEqual(TypeOfComputeFormulaResult.BooleanResult, result.GetFormulaComputeResultType());
        }

        [TestMethod]
        public void CheckComputeFormula()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-30-2*3*(2+4)");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(false, result.CalculatedBoolBoolValue);
            Assert.AreEqual(-66, result.CalculatedDoubleValue);
        }
        [TestMethod]
        public void CheckErrorDoubleOperation()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-30+-2");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.SimilarTokensOneByOne, result.GetFormulaCheckResult());
            Assert.AreEqual(4, result.GetErrorBeginPositionInFormulaText());
            Assert.AreEqual(1, result.GetErrorLengthPositionInFormulaText());
        }

        [TestMethod]
        public void CheckDoubleConditionError()
        {
            // x == true ? y == true ? (ошибка, 2 условия подряд без установки значения)
            var calc = new Calculator();
            var result = calc.ComputeFormula("1 == 1 ? 2 == 2 ? 3");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.ThisFormulaPartMustBeMath, result.GetFormulaCheckResult());
        }

        [TestMethod]
        public void CheckErrorMathFormulaInsteadOfCondition()
        {
            // 1+1 ? 11 - математическая формула вместо логической
            var calc = new Calculator();
            var result = calc.ComputeFormula("1+1 ? 11");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.ThisFormulaPartMustBeLogic, result.GetFormulaCheckResult());
        }

        [TestMethod]
        public void CheckConditions()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1==1 ? 11");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(11, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1!=1 ? 11 ; 12");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(12, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1!=1 ? 11 ; 2 == 2 ? 14");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(14, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1==1 ? 11 ;");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(11, result.CalculatedDoubleValue);
        }

        [TestMethod]
        public void CheckFormulaIsEmpty()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1==1 ? ;");
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetFormulaCheckResult());
            result = calc.ComputeFormula("1==1 ?");
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetFormulaCheckResult());
            result = calc.ComputeFormula(string.Empty);
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetFormulaCheckResult());
            result = calc.ComputeFormula(null);
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetFormulaCheckResult());
        }
        [TestMethod]
        public void CheckOperationsPriority()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1+3>1");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(TypeOfComputeFormulaResult.BooleanResult, result.GetFormulaComputeResultType());
            Assert.AreEqual(true, result.CalculatedBoolBoolValue);

            result = calc.ComputeFormula("1+3>=1");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(TypeOfComputeFormulaResult.BooleanResult, result.GetFormulaComputeResultType());
            Assert.AreEqual(true, result.CalculatedBoolBoolValue);
        }
    }
}
