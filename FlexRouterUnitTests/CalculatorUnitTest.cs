using FlexRouter.CalculatorRelated;
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
            var res = calc.CalculateMathFormula("-1");
            Assert.AreEqual(-1, res.Value);
            res = calc.CalculateMathFormula("1+(-10)");
            Assert.AreEqual(-9, res.Value);
            res = calc.CalculateMathFormula("1+(-(-10))");
            Assert.AreEqual(11, res.Value);
            res = calc.CalculateMathFormula("(-(10+3))");
            Assert.AreEqual(-13, res.Value);
        }
    }
}
