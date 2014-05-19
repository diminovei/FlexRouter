using FlexRouter.CalculatorRelated.Tokens;

namespace FlexRouter.CalculatorRelated
{
    /// <summary>
    /// Результат проверки и вычисления формулы
    /// </summary>
    public class FormulaComputeResult
    {
        /// <summary>
        /// Тип найденной в формуле ошибки
        /// </summary>
        private readonly FormulaError _formulaCheckResult;
        /// <summary>
        /// Начальная позиция отрезка текста в формуле, содержащего ошибку
        /// </summary>
        private readonly int _errorBeginPositionInFormulaText;
        /// <summary>
        /// Длина отрезка текста в формуле, содержащего ошибку
        /// </summary>
        private readonly int _errorLengthInFormulaText;
        /// <summary>
        /// Результат вычисления формулы - булевое значение?
        /// </summary>
        private readonly bool _resultIsBoolean;
        /// <summary>
        /// Получить успешность и тип значения полученного в результате расчёта формулы
        /// </summary>
        /// <returns></returns>
        public TypeOfComputeFormulaResult GetFormulaComputeResultType()
        {
            if (_formulaCheckResult == FormulaError.FormulaIsEmpty)
                return TypeOfComputeFormulaResult.FormulaWasEmpty;
            if (_formulaCheckResult != FormulaError.Ok)
                return TypeOfComputeFormulaResult.Error;
            return _resultIsBoolean ? TypeOfComputeFormulaResult.BooleanResult : TypeOfComputeFormulaResult.DoubleResult;
        }
        /// <summary>
        /// Получить результат проверки формулы
        /// </summary>
        /// <returns>Код ошибки</returns>
        public FormulaError GetFormulaCheckResult()
        {
            return _formulaCheckResult;
        }
        /// <summary>
        /// Получить начальную позицию отрезка текста в формуле, содержащего ошибку
        /// </summary>
        public int GetErrorBeginPositionInFormulaText()
        {
            return _errorBeginPositionInFormulaText;
        }
        /// <summary>
        /// Получить длину отрезка текста в формуле, содержащего ошибку
        /// </summary>
        public int GetErrorLengthPositionInFormulaText()
        {
            return _errorLengthInFormulaText;
        }
        public double CalculatedDoubleValue;
        public bool CalculatedBoolBoolValue;
        /// <summary>
        /// Этот конструктор используется, когда проблема формулы не в токене
        /// </summary>
        /// <param name="formulaError">код ошибки в формуле</param>
        public FormulaComputeResult(FormulaError formulaError)
        {
            _formulaCheckResult = formulaError;
            // ToDo: определить позицию при ошибках, чтобы корректно выделять цветом ошибочные части!!!
            _errorBeginPositionInFormulaText = 0;
            _errorLengthInFormulaText = 0;
        }
        /// <summary>
        /// Этот конструктор используется при успешном расчёте формулы с числовым результатом
        /// </summary>
        /// <param name="doubleValue">числовой результат расчёта формулы</param>
        public FormulaComputeResult(double doubleValue)
        {
            _formulaCheckResult = FormulaError.Ok;
            _errorBeginPositionInFormulaText = 0;
            _errorLengthInFormulaText = 0;

            CalculatedDoubleValue = doubleValue;
            _resultIsBoolean = false;
        }
        /// <summary>
        /// Этот конструктор используется при успешном расчёте формулы с булевым результатом
        /// </summary>
        /// <param name="boolValue">булевый результат расчёта формулы</param>
        public FormulaComputeResult(bool boolValue)
        {
            _formulaCheckResult = FormulaError.Ok;
            _errorBeginPositionInFormulaText = 0;
            _errorLengthInFormulaText = 0;
            CalculatedBoolBoolValue = boolValue;
            _resultIsBoolean = true;
        }
        /// <summary>
        /// Этот конструктор используется при неудачной обработке формулы, когда один из токенов содержит ошибку
        /// </summary>
        /// <param name="token">токен с ошибкой</param>
        public FormulaComputeResult(ICalcToken token)
        {
            _formulaCheckResult = token.Error;
            _errorBeginPositionInFormulaText = token.Position;
            _errorLengthInFormulaText = token.GetTokenTextLength();
        }
    }
}
