using System.Drawing;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.Localizers;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorRange : DescriptorRangeBase, IDescriptorPrevNext, IDescriptorRangeExt
    {
        /// <summary>
        /// Сюда сохраняется результат, полученный из переменных для того, чтобы токенизировать [R]
        /// </summary>
        private double _currentFormulaResultForTokenizer;
        /// <summary>
        /// Получить текст с типом AccessDescriptor'а
        /// </summary>
        /// <returns></returns>
        public override string GetDescriptorType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryRange);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.Encoder;
        }

        /// <summary>
        /// Отдельный калькулятор не понимающий [R], для того, чтобы не зацикливаться на получении значения из переменной 
        /// </summary>
        private readonly Calculator _inputValueCalculator = new Calculator();
        public DescriptorRange()
        {
            StateDescriptors.Add(new AccessDescriptorState { Id = 0, Name = "*", Order = 0 });
            CalculatorE.RegisterTokenizer(FormulaResultTokenizer);
            CalculatorE.RegisterPreprocessor(FormulaResultProcessor);
            _inputValueCalculator.RegisterTokenizer(CalculatorVariableAccessAddonE.VariableTokenizer);
            _inputValueCalculator.RegisterPreprocessor(CalculatorVariableAccessAddonE.VariablePreprocessor);
        }
        /// <summary>
        /// Вычислить новое значение
        /// </summary>
        /// <param name="value">текущее значение</param>
        /// <param name="stepValue">шаг изменения значения</param>
        /// <param name="minimumValue">минимальное значение</param>
        /// <param name="maximumValue">максимальное значение</param>
        /// <param name="nextState">false - уменьшаем, true - увеличиваем значение</param>
        /// <returns>новое значение</returns>
        private double CalculateNewValue(double value, double stepValue, double minimumValue, double maximumValue, bool nextState)
        {
            double min, max;
            // Определяем направление и задаём минимум и максимум
            if (minimumValue < maximumValue)
            {
                min = minimumValue;
                max = maximumValue;
            }
            else
            {
                min = maximumValue;
                max = minimumValue;
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
        /// <summary>
        /// Увеличить значение переменных
        /// </summary>
        /// <param name="repeats"></param>
        public void SetNextState(int repeats)
        {
            ChangeState(repeats, true);
        }
        /// <summary>
        /// Вычислить формулу, если питание включенор
        /// </summary>
        /// <param name="formula">формула</param>
        /// <returns>значение. null, если питание выключено или при вычислении формулы произошла ошибка</returns>
        private double? CalculateFormulaIfPowerIsOn(string formula)
        {
            if (!IsPowerOn())
                return null;
            var formulaForVar = CalculatorE.ComputeFormula(formula);
            return formulaForVar.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.DoubleResult
                ? (double?) formulaForVar.CalculatedDoubleValue
                : null;
        }
        /// <summary>
        /// Изменить значение, если с формулами всё в порядке
        /// </summary>
        /// <param name="repeats">число повторов</param>
        /// <param name="nextState">false - уменьшить, true - увеличить</param>
        private void ChangeState(int repeats, bool nextState)
        {
            var stepValue = CalculateFormulaIfPowerIsOn(GetStepFormula());
            if (stepValue == null)
                return;

            var minimumValue = CalculateFormulaIfPowerIsOn(GetMinimumValueFormula());
            if (minimumValue == null)
                return;

            var maximumValue = CalculateFormulaIfPowerIsOn(GetMaximumValueFormula());
            if (maximumValue == null)
                return;
            
            stepValue = stepValue * repeats;
            var receivedValueFormula = GetReceiveValueFormula();
            var formulaResult = _inputValueCalculator.ComputeFormula(receivedValueFormula);
            if (formulaResult.GetFormulaComputeResultType() != TypeOfComputeFormulaResult.DoubleResult)
                return;
            _currentFormulaResultForTokenizer = CalculateNewValue(formulaResult.CalculatedDoubleValue, (double)stepValue, (double)minimumValue, (double)maximumValue, nextState);

            CalculateVariablesFormulaAndWriteValues();
        }
        /// <summary>
        /// Для всех переменных вычислить формулы и записать значения
        /// </summary>
        private void CalculateVariablesFormulaAndWriteValues()
        {
            if (!IsPowerOn())
                return;
            foreach (var varId in UsedVariables)
            {
                var formula = GlobalFormulaKeeper.Instance.GetVariableFormulaText(GetId(), varId, 0);
                var formulaForVar = CalculatorE.ComputeFormula(formula);
                if (formulaForVar.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.DoubleResult)
                    VariableManager.WriteValue(varId, formulaForVar.CalculatedDoubleValue);
            }
        }
        /// <summary>
        /// Инициализация описателя доступа
        /// </summary>
        public override void Initialize()
        {
            if (!EnableDefaultValue)
                return;
            var defaultValue = CalculateFormulaIfPowerIsOn(GetDefaultValueFormula());
            if (defaultValue == null)
                return;

            _currentFormulaResultForTokenizer = (double)defaultValue;
            CalculateVariablesFormulaAndWriteValues();
        }
        /// <summary>
        /// Уменьшить значение переменных
        /// </summary>
        /// <param name="repeats"></param>
        public void SetPreviousState(int repeats)
        {
            ChangeState(repeats, false);
        }
        // InputFormula
        // Как потом раздать результат в другие переменные? Ввести в формулы термин "[R]"?
        private ICalcToken FormulaResultTokenizer(string formula, int currentTokenPosition)
        {
            const string resultTokenText = "[R]";
            var token = new CalcTokenNumber(currentTokenPosition) {TokenText = resultTokenText };
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
        /// <summary>
        /// Установить значение, равное X процентам от хода бегунка
        /// </summary>
        /// <param name="positionPercentage">процент</param>
        public void SetPositionInPercents(double positionPercentage)
        {
            var stepValue = CalculateFormulaIfPowerIsOn(GetStepFormula());
            if (stepValue == null)
                return;

            var minimumValue = CalculateFormulaIfPowerIsOn(GetMinimumValueFormula());
            if (minimumValue == null)
                return;

            var maximumValue = CalculateFormulaIfPowerIsOn(GetMaximumValueFormula());
            if (maximumValue == null)
                return;
            
            double range;
            if (minimumValue > maximumValue)
                range = (double)minimumValue - (double)maximumValue;
            else
                range = (double)maximumValue - (double)minimumValue;
            var pos = range*(positionPercentage/100);

            var finalPosition = pos - pos % (double)stepValue;
            if (pos % stepValue > stepValue / 2)
                finalPosition += (double)stepValue;

            if (minimumValue > maximumValue)
                finalPosition = (double)minimumValue - finalPosition;
            else
                finalPosition = (double)minimumValue + finalPosition;

            _currentFormulaResultForTokenizer = finalPosition;

            CalculateVariablesFormulaAndWriteValues();
        }
    }
}
