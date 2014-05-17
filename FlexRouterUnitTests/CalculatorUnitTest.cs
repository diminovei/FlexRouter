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
            var result = calc.CalculateMathFormula("-1");
            Assert.AreEqual(-1, result.Value);
            result = calc.CalculateMathFormula("1+(-10)");
            Assert.AreEqual(-9, result.Value);
            result = calc.CalculateMathFormula("1+(-(-10))");
            Assert.AreEqual(11, result.Value);
            result = calc.CalculateMathFormula("(-(10+3))");
            Assert.AreEqual(-13, result.Value);
        }

        [TestMethod]
        public void CheckUnaryMinusFormulaNew()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-1");
//            Assert.AreEqual(false, result.CalculatedBoolValue);
            Assert.AreEqual(-1, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("1+(-10)");
//            Assert.AreEqual(false, result.CalculatedBoolValue);
            Assert.AreEqual(-9, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("1+(-(-10))");
//            Assert.AreEqual(false, result.CalculatedBoolValue);
            Assert.AreEqual(11, result.CalculatedDoubleValue);
            result = calc.ComputeFormula("(-(10+3))");
//            Assert.AreEqual(false, result.CalculatedBoolValue);
            Assert.AreEqual(-13, result.CalculatedDoubleValue);
        }

        [TestMethod]
        public void CheckComputeFormula()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-30-2*3*(2+4)");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(false, result.CalculatedBoolValue);
            Assert.AreEqual(-66, result.CalculatedDoubleValue);
        }
        [TestMethod]
        public void CheckErrors()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("-30+-2");
            Assert.AreEqual(false, result.IsCalculatedSuccessfully());
            Assert.AreEqual(Calculator.ComputeResultType.Error, result.GetResultType());
            Assert.AreEqual(FormulaError.TokenMustBeValue, result.GetError());
            Assert.AreEqual(4, result.GetErrorBeginPositionInFormulaText());
            Assert.AreEqual(1, result.GetErrorLengthPositionInFormulaText());
        }

        [TestMethod]
        public void CheckDoubleConditionError()
        {
            // x == true ? y == true ? (ошибка, 2 условия подряд без установки значения)
            var calc = new Calculator();
            var result = calc.ComputeFormula("1 == 1 ? 2 == 2 ? 3");
            Assert.AreEqual(false, result.IsCalculatedSuccessfully());
            Assert.AreEqual(FormulaError.ThisFormulaPartMustBeMath, result.GetError());
        }

        [TestMethod]
        public void CheckMathFormulaInsteadOfCondition()
        {
            // 1+1 ? 11 - математическая формула вместо логической
            var calc = new Calculator();
            var result = calc.ComputeFormula("1+1 ? 11");
            Assert.AreEqual(false, result.IsCalculatedSuccessfully());
            Assert.AreEqual(FormulaError.ThisFormulaPartMustBeLogic, result.GetError());
        }

        [TestMethod]
        public void CheckConditions()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1==1 ? 11");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(11, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1!=1 ? 11 ; 12");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(12, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1!=1 ? 11 ; 2 == 2 ? 14");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(14, result.CalculatedDoubleValue);

            result = calc.ComputeFormula("1==1 ? 11 ;");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(11, result.CalculatedDoubleValue);
        }

        [TestMethod]
        public void CheckFormulaIsEmpty()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1==1 ? ;");
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetError());
            result = calc.ComputeFormula("1==1 ?");
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetError());
            result = calc.ComputeFormula(string.Empty);
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetError());
            result = calc.ComputeFormula(null);
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetError());
        }
        [TestMethod]
        public void CheckOperationsPriority()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("1+3>1");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(Calculator.ComputeResultType.BooleanResult, result.GetResultType());
            Assert.AreEqual(true, result.CalculatedBoolValue);

            result = calc.ComputeFormula("1+3>=1");
            Assert.AreEqual(true, result.IsCalculatedSuccessfully());
            Assert.AreEqual(Calculator.ComputeResultType.BooleanResult, result.GetResultType());
            Assert.AreEqual(true, result.CalculatedBoolValue);
        }
    }
}
