using System.IO;
using System.Threading;
using FlexRouter.AccessDescriptors;
using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.ProfileItems;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodFakeVariable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexRouterUnitTests
{
    [TestClass]
    public class CalculatorUnitTest
    {
        #region Тесты калькулятора
        [TestMethod]
        public void CheckErrors()
        {
            var calc = new Calculator();
            var result = calc.ComputeFormula("3()");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.TokenMustBeOperation, result.GetFormulaCheckResult());
            Assert.AreEqual(1, result.GetErrorBeginPositionInFormulaText());

            result = calc.ComputeFormula("(%1+2)");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.UnexpectedSymbols, result.GetFormulaCheckResult());
            Assert.AreEqual(1, result.GetErrorBeginPositionInFormulaText());

            result = calc.ComputeFormula("(11+2))");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.ClosingBracketNotOpened, result.GetFormulaCheckResult());
            Assert.AreEqual(6, result.GetErrorBeginPositionInFormulaText());

            result = calc.ComputeFormula("(11+2");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.OpeningBracketNotClosed, result.GetFormulaCheckResult());
            Assert.AreEqual(0, result.GetErrorBeginPositionInFormulaText());

            result = calc.ComputeFormula("3/0");
            Assert.AreEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.DivisionByZero, result.GetFormulaCheckResult());
            Assert.AreEqual(2, result.GetErrorBeginPositionInFormulaText());
        }
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

            result = calc.ComputeFormula("1!=1 ? 11");
            Assert.AreNotEqual(TypeOfComputeFormulaResult.Error, result.GetFormulaComputeResultType());
            Assert.AreEqual(FormulaError.FormulaIsEmpty, result.GetFormulaCheckResult());

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
        #endregion
        #region Тесты профиля

/*        private void ClearProfile()
        {
            Profile.Clear();
        }

        private int[] CreateThreePanelsInProfile()
        {
            var panelIds = new List<int>();
            var panelId1 = Profile.RegisterPanel(new Panel {Name = "Panel1"}, true);
            panelIds.Add(panelId1);
            var panelId2 = Profile.RegisterPanel(new Panel {Name = "Panel2"}, true);
            panelIds.Add(panelId2);
            var panelId3 = Profile.RegisterPanel(new Panel {Name = "Panel3"}, true);
            panelIds.Add(panelId3);
            return panelIds.ToArray();
        }

        private int[] CreateThreeVariablesInProfile(int[] threePanelIds)
        {
            var varibleIds = new List<int>();
            var firstTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.Byte,
                Name = "Var1",
                PanelId = threePanelIds[0],
                Description = "Тестовая переменная 1\nНовая строка"
            };
            var firstTestVariableId = Profile.StoreVariable(firstTestVariable, true);
            varibleIds.Add(firstTestVariableId);
            var secondTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.EightByteFloat,
                Name = "Var2",
                PanelId = threePanelIds[1],
                Description = "Тестовая переменная 2\nНовая строка"
            };
            var secondTestVariableId = Profile.StoreVariable(secondTestVariable, true);
            varibleIds.Add(secondTestVariableId);
            var thirdTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.FourBytesSigned,
                Name = "Var3",
                PanelId = threePanelIds[2],
                Description = "Тестовая переменная 3\nНовая строка"
            };
            var thirdTestVariableId = Profile.StoreVariable(thirdTestVariable, true);
            varibleIds.Add(thirdTestVariableId);
            return varibleIds.ToArray();
        }*/
        [TestMethod]
        public void PrepareProfile()
        {
            Profile.Clear();
            // Создание трёх панелей
            var p1 = new Panel {Name = "Panel1"};
            Profile.PanelStorage.StorePanel(p1);
            var panelId1 = p1.Id;


            var p2 = new Panel { Name = "Panel2" };
            Profile.PanelStorage.StorePanel(p2);
            var panelId2 = p2.Id;

            var p3 = new Panel { Name = "Panel3" };
            Profile.PanelStorage.StorePanel(p3);
            var panelId3 = p3.Id;


            // Создание трёх переменных. Одна в первой панели, вторая и третья во второй
            var firstTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.Byte,
                Name = "Var1",
                PanelId = panelId1,
                Description = "Тестовая переменная 1\nНовая строка"
            };
            var firstTestVariableId = firstTestVariable.Id;
            Profile.VariableStorage.StoreVariable(firstTestVariable);
            
            var secondTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.EightByteFloat,
                Name = "Var2",
                PanelId = panelId2,
                Description = "Тестовая переменная 2\nНовая строка"
            };
            var secondTestVariableId = secondTestVariable.Id;
            Profile.VariableStorage.StoreVariable(secondTestVariable);
            
            var thirdTestVariable = new FakeVariable
            {
                Size = MemoryVariableSize.FourBytesSigned,
                Name = "Var3",
                PanelId = panelId2,
                Description = "Тестовая переменная 3\nНовая строка"
            };
            var thirdTestVariableId = thirdTestVariable.Id;
            Profile.VariableStorage.StoreVariable(thirdTestVariable);
            
            // Создание описателей доступа
            
            // Button
            var valueAccessDescriptor = new DescriptorValue();

            var powerFormula = "[" + Profile.PanelStorage.GetPanelById(Profile.VariableStorage.GetVariableById(firstTestVariableId).PanelId).Name + "." + Profile.VariableStorage.GetVariableById(firstTestVariableId).Name + "]";
            valueAccessDescriptor.SetPowerFormula(powerFormula);
            valueAccessDescriptor.AssignDefaultStateId(0);
            valueAccessDescriptor.AddConnector("Off");
            valueAccessDescriptor.AddConnector("On");
//            valueAccessDescriptor.AddVariable(firstTestVariableId);
            valueAccessDescriptor.AddVariable(secondTestVariableId);
            valueAccessDescriptor.AddVariable(thirdTestVariableId);
//            valueAccessDescriptor.SetFormula(0, 0, "0");
            valueAccessDescriptor.SetFormula(0, secondTestVariableId, "0");
            valueAccessDescriptor.SetFormula(0, thirdTestVariableId, "0");
//            valueAccessDescriptor.SetFormula(1, 0, "1");
            valueAccessDescriptor.SetFormula(1, secondTestVariableId, "2.58");
            valueAccessDescriptor.SetFormula(1, thirdTestVariableId, "-3");
            valueAccessDescriptor.AssignDefaultStateId(0);
            valueAccessDescriptor.SetAssignedPanelId(panelId3);
            valueAccessDescriptor.SetName("AccessDescriptor1");
            var valueAccessDescriptorId = Profile.AccessDescriptor.RegisterAccessDescriptor(valueAccessDescriptor);


            // Encoder
            var rangeAccessDescriptor = new DescriptorRange();
            rangeAccessDescriptor.SetReceiveValueFormula("[" + Profile.PanelStorage.GetPanelById(panelId2).Name + "." + Profile.VariableStorage.GetVariableById(secondTestVariableId).Name + "]");
            rangeAccessDescriptor.AddVariable(secondTestVariableId);
            rangeAccessDescriptor.AddVariable(firstTestVariableId);
            rangeAccessDescriptor.SetAssignedPanelId(panelId3);
            rangeAccessDescriptor.SetName("AccessDescriptorRange");
            rangeAccessDescriptor.SetFormula(0, secondTestVariableId, "[R]");
            rangeAccessDescriptor.SetFormula(1, thirdTestVariableId, "[R]:1");
            rangeAccessDescriptor.SetMinimumValueFormula("0");
            rangeAccessDescriptor.SetMaximumValueFormula("4");
            rangeAccessDescriptor.SetStepFormula("0.5");
//            rangeAccessDescriptor.IsLooped = true;
            var rangeAccessDescriptorId = Profile.AccessDescriptor.RegisterAccessDescriptor(rangeAccessDescriptor);
            var tempFile = Path.GetTempFileName();

            Profile.SaveAs(tempFile);
            Profile.Clear();
            Profile.Load(tempFile, ProfileItemPrivacyType.Public);

            var valueDescriptor = (DescriptorValue)Profile.AccessDescriptor.GetAccessDesciptorById(valueAccessDescriptorId);
            Profile.VariableStorage.WriteValue(firstTestVariableId, 0);
            Profile.VariableStorage.WriteValue(secondTestVariableId, 0);
            Profile.VariableStorage.WriteValue(thirdTestVariableId, 0);
            Thread.Sleep(200);
            valueDescriptor.SetState(1);
            Thread.Sleep(200);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(firstTestVariableId).Value);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(secondTestVariableId).Value);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(thirdTestVariableId).Value);
            Profile.VariableStorage.WriteValue(firstTestVariableId, 1);
            Thread.Sleep(200);
            valueDescriptor.SetState(1);
            Thread.Sleep(200);
            Assert.AreEqual(1, Profile.VariableStorage.ReadValue(firstTestVariableId).Value);
            Assert.AreEqual(2.58, Profile.VariableStorage.ReadValue(secondTestVariableId).Value);
            Assert.AreEqual(-3, Profile.VariableStorage.ReadValue(thirdTestVariableId).Value);
            valueAccessDescriptor.SetDefaultState();
            Thread.Sleep(200);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(firstTestVariableId).Value);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(secondTestVariableId).Value);
            Assert.AreEqual(0, Profile.VariableStorage.ReadValue(thirdTestVariableId).Value);
            
            
            File.Delete(tempFile);
        }
//    private static void TestInit()
//    {
//        // Button
//        var ad = new DescriptorValue();
//        ad.AddConnector("Off");
//        ad.AddConnector("On");
//        ad.AddVariable(/*var1Id*/0);
//        ad.AddVariable(/*var2Id*/1);
//        ad.SetFormula(0, 0, "0");
//        ad.SetFormula(0, 1, "0");
//        ad.SetFormula(1, 0, "1");
//        ad.SetFormula(1, 1, "2");
//        ad.AssignDefaultStateId(0);
//        ad.SetAssignedPanelId(/*panelId3*/2);
//        ad.SetName("TestAD");
//        RegisterAccessDescriptor(ad, true);

//        var cp = new ButtonProcessor(ad.GetId());
//        var cpId = RegisterControlProcessor(cp, ad.GetId());
//        //            ad.AssignControlProcessor(cpId);
//        cp.AssignHardware(1, "Arcc:2905A4F9|Button|1|81");

//        // BinaryInput
//        var ad3 = new DescriptorValue();
//        ad3.AddConnector("Off");
//        ad3.AddConnector("On");
//        ad3.AddVariable(/*var1Id*/0);
//        ad3.AddVariable(/*var2Id*/1);
//        ad3.SetFormula(0, 0, "0");
//        ad3.SetFormula(0, 1, "0");
//        ad3.SetFormula(1, 0, "1");
//        ad3.SetFormula(1, 1, "2");
//        //            ad3.AssignDefaultState(0);
//        ad3.SetAssignedPanelId(/*panelId3*/2);
//        ad3.SetName("TestADBI");
//        RegisterAccessDescriptor(ad3, true);

//        /*            var cp3 = new ButtonBinaryInputProcessor(ad3.GetId());
//                    var cpId3 = RegisterControlProcessor(cp3);
//                    ad3.AssignControlProcessor(cpId3);*/


//        /////////// Encoder
//        var ad1 = new DescriptorRange();
//        ad1.SetFormulaToGetValues("[Оверхед.АРК1 (сотни)]");
//        ad1.AddVariable(/*var5Id*/4);
//        ad1.SetAssignedPanelId(/*panelId1*/0);
//        ad1.SetName("Оверхед.АРК1 (сотни)");
//        ad1.SetFormula(0, /*var5Id*/4, "[R]");
//        ad1.SetFormula(1, /*var5Id*/4, "[R]");
//        ad1.MinimumValue = 0;
//        ad1.MaximumValue = 16;
//        ad1.Step = 1;
//        ad1.IsLooped = true;
//        var encoderAdId = RegisterAccessDescriptor(ad1, true);

//        var ecp = new EncoderProcessor(encoderAdId);
//        ecp.AssignHardware("Arcc:2905A4F9|Encoder|1|4");
//        var ecpId = RegisterControlProcessor(ecp, ad1.GetId());
//        //            ad1.AssignControlProcessor(ecpId);
//        ecp.SetInversion(true);
//        /////////// Indicator
//        var indicatorAd = new DescriptorIndicator();
//        indicatorAd.SetName("Оверхед.АРК1 (сотни) индикатор");
//        indicatorAd.SetAssignedPanelId(/*panelId1*/0);
//        indicatorAd.SetFormula("[Оверхед.АРК1 (сотни)]");
//        indicatorAd.SetNumberOfDigitsAfterPoint(3);
//        var indicatorId = RegisterAccessDescriptor(indicatorAd, true);
//        var icp = new IndicatorProcessor(indicatorId);
//        icp.AssignHardware("Arcc:2905A4F9|Indicator|8|0");
//        var icpId = RegisterControlProcessor(icp, indicatorAd.GetId());
//        //            indicatorAd.AssignControlProcessor(icpId);
//        /////////// BinaryOutput
//        var boAd = new DescriptorBinaryOutput();
//        boAd.SetFormula("[НВУ.Переключатель ДМЕ (3 позиции)]==1");
//        boAd.SetName("НВУ.Переключатель ДМЕ");

//        boAd.SetAssignedPanelId(/*panelId1*/0);
//        var boId = RegisterAccessDescriptor(boAd, true);
//        var lcp = new LampProcessor(boId);
//        lcp.AssignHardware("Arcc:2905A4F9|BinaryOutput|1|7");
//        var lcpId = RegisterControlProcessor(lcp, boAd.GetId());
//        //           boAd.AssignControlProcessor(lcpId);
//    }
//*/
        #endregion
    }
}
