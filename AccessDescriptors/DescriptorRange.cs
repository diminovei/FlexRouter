using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorRange : DescriptorRangeBase, IDescriptorPrevNext, IDescriptorRangeExt
    {
        /// <summary>
        /// Сюда сохраняется результат, полученный из переменных для того, чтобы токенизировать [R]
        /// </summary>
        private double _currentFormulaResultForTokenizer;
        public override string GetDescriptorName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryRange);
        }

        public DescriptorRange()
        {
            StateDescriptors.Add(new AccessDescriptorState { Id = 0, Name = "*", Order = 0 });
            CalculatorE.RegisterTokenizer(FormulaResultTokenizer);
            CalculatorE.RegisterPreprocessor(FormulaResultProcessor);
        }

        private double CalculateNewValue(double value, double stepValue, bool nextState)
        {
            double min, max;
            // Определяем направление и задаём минимум и максимум
            if (MinimumValue < MaximumValue)
            {
                min = MinimumValue;
                max = MaximumValue;
            }
            else
            {
                min = MaximumValue;
                max = MinimumValue;
                nextState = !nextState;
            }
            //  Нормализуем значение
            if (value < min)
                value = min;
            if (value > max)
                value = max;

            if (nextState)
            {
                if (value + stepValue <= max)
                    value += stepValue;
                else
                {
                    if (!IsLooped)
                        value = max;
                    else
                        value = (min - 1) + (stepValue - (max- value));
                }
            }
            else
            {
                if (value - stepValue >= min)
                    value -= stepValue;
                else
                {
                    if (!IsLooped)
                        value = min;
                    else
                        value = (max + 1) - (stepValue - (value - min));
                }
            }
            return value;
        }

        public void SetNextState(int repeats)
        {
            ChangeState(repeats, true);
        }

        private void ChangeState(int repeats, bool nextState)
        {
            var stepValue = Step * repeats;
            var formulaResult = CalculatorE.CalculateMathFormula(GetReceiveValueFormula());
            if (formulaResult.Error != ProcessingMathFormulaError.Ok)
                return;

            _currentFormulaResultForTokenizer = CalculateNewValue(formulaResult.Value, stepValue, nextState);
            
            if (!IsPowerOn())
                return;
            foreach (var varId in UsedVariables)
            {
                var formula = GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.SetValue, GetId(), varId, 0);
                var formulaResult2 = CalculatorE.CalculateMathFormula(formula);
                if (formulaResult2.Error != ProcessingMathFormulaError.Ok)
                    return;
                VariableManager.WriteValue(varId, formulaResult2.Value);
            }
        }

        public override void Initialize()
        {
            if (!EnableDefaultValue)
                return;
            _currentFormulaResultForTokenizer = DefaultValue;
            foreach (var varId in UsedVariables)
            {
                var formula = GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.SetValue, GetId(), varId, 0);
                var formulaResult2 = CalculatorE.CalculateMathFormula(formula);
                if (formulaResult2.Error != ProcessingMathFormulaError.Ok)
                    return;
                VariableManager.WriteValue(varId, formulaResult2.Value);
            }
        }
        public void SetPreviousState(int repeats)
        {
            ChangeState(repeats, false);
        }

        // InputFormula
        // Как потом раздать результат в другие переменные? Ввести в формулы термин "[R]"?
        private ICalcToken FormulaResultTokenizer(string formula, int currentTokenPosition)
        {
            const string resultTokenText = "[R]";
            var token = new CalcTokenNumber {Position = currentTokenPosition, TokenText = resultTokenText};
            return formula.Substring(currentTokenPosition, 3) != resultTokenText ? null : token;
        }

        // Как потом раздать результат в другие переменные? Ввести в формулы термин "[R]"?
        private ICalcToken FormulaResultProcessor(ICalcToken tokenToPreprocess)
        {
            if (!(tokenToPreprocess is CalcTokenNumber))
                return tokenToPreprocess;
            const string resultTokenText = "[R]";
            if (((CalcTokenNumber)tokenToPreprocess).TokenText == resultTokenText)
                ((CalcTokenNumber)tokenToPreprocess).Value = _currentFormulaResultForTokenizer;
            return tokenToPreprocess;
        }

        public void SetPositionInPercents(double positionPercentage)
        {
            double range;
            if (MinimumValue > MaximumValue)
                range = MinimumValue - MaximumValue;
            else
                range = MaximumValue - MinimumValue;
            var pos = range*(positionPercentage/100);

            var finalPosition = pos - pos % Step;
            if (pos % Step > Step / 2)
                finalPosition += Step;

            if (MinimumValue > MaximumValue)
                finalPosition = MinimumValue - finalPosition;
            else
                finalPosition = MinimumValue + finalPosition;

            var formulaResult = CalculatorE.CalculateMathFormula(GetReceiveValueFormula());
            if (formulaResult.Error != ProcessingMathFormulaError.Ok)
                return;
            formulaResult.Value = finalPosition;
            _currentFormulaResultForTokenizer = finalPosition;

            if (!IsPowerOn())
                return;
            foreach (var varId in UsedVariables)
            {
                var formula = GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.SetValue, GetId(), varId, 0);
                var formulaResult2 = CalculatorE.CalculateMathFormula(formula);
                if (formulaResult2.Error != ProcessingMathFormulaError.Ok)
                    return;
                VariableManager.WriteValue(varId, formulaResult2.Value);
            }
        }
    }
}
