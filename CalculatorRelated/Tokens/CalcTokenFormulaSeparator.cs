namespace FlexRouter.CalculatorRelated.Tokens
{
    /// <summary>
    /// Токен участвует в конструкции если значение формулы true, то возвращаем X, иначе обрабатываем следующую формулу. Записывается например, так: 1==1 ? 3 ; 4 
    /// </summary>
    class CalcTokenFormulaSeparator : CalcTokenBase
    {
        public static string ToText()
        {
            return ";";
        }
    }
}
