namespace FlexRouter.CalculatorRelated
{
    /// <summary>
    /// Тип результата расчёта формулы
    /// </summary>
    public enum TypeOfComputeFormulaResult
    {
        Error,              //  Ошибка при проверке или вычислении формулы
        FormulaWasEmpty,    //  Формула, переданная на вычисление была пуста
        BooleanResult,      //  Результат вычисления формулы - булевый
        DoubleResult        //  Результат вычисления формулы - число
    }
}
