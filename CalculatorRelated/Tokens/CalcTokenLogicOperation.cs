using System;

namespace FlexRouter.CalculatorRelated.Tokens
{
    public enum CalcLogicOperation
    {
        Equal,          // ==
        Not,            // !=
        And,            // &&
        Or,             // ||
        GreaterOrEqual, // >= Именно в таком порядке, чтобы при токенизации сначал проверялось >=, а потом >
        LessOrEqual,    // <=
        Greater,        // >
        Less,           // <
    }

    public class CalcTokenLogicOperation : CalcTokenBase
    {
        public CalcLogicOperation LogicOperation;

        public CalcTokenLogicOperation(int currentTokenPosition) : base(currentTokenPosition)
        {
        }

        public static string ToText(CalcLogicOperation operation)
        {
            switch (operation)
            {
                case CalcLogicOperation.And:
                    return "&&";
                case CalcLogicOperation.Equal:
                    return "==";
                case CalcLogicOperation.Greater:
                    return ">";
                case CalcLogicOperation.GreaterOrEqual:
                    return ">=";
                case CalcLogicOperation.Less:
                    return "<";
                case CalcLogicOperation.LessOrEqual:
                    return "<=";
                case CalcLogicOperation.Not:
                    return "!=";
                case CalcLogicOperation.Or:
                    return "||";
                default:
                    return null;
            }
        }
        /// <summary>
        /// Попытка извлечь токен - логическую операцию
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, int currentTokenPosition)
        {
            var token = new CalcTokenLogicOperation(currentTokenPosition);

            var formulaCut = formula.Remove(0, currentTokenPosition);
            foreach (var op in Enum.GetValues(typeof(CalcLogicOperation)))
            {
                var operation = (CalcLogicOperation)op;
                var operationText = ToText(operation);
                if (!formulaCut.StartsWith(operationText))
                    continue;
                token.LogicOperation = operation;
                token.TokenText = operationText;
                return token;
            }
            return null;
        }
    }
}
