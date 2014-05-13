using System;

namespace FlexRouter.CalculatorRelated.Tokens
{
    public enum CalcMathOperation
    {
        Plus,               // + (сложение
        UnaryPlus,          // -(+10) или +10
        Minus,              // - (вычитание)
        UnaryMinus,         // -10 или +(-3)
        Multiply,           // *
        Divide,             // /
        DivideModulo,       // % разделить и получить остаток
        DivideInteger,      // разделить и получить целую часть
    }
    public class CalcTokenMathOperation : CalcTokenBase
    {
        public CalcMathOperation MathOperation;

        /// <summary>
        /// Преобразование математической операции в текст 
        /// </summary>
        /// <param name="operation">операция</param>
        /// <param name="isUnary"></param>
        /// <returns>текст операции. null - не найдено соответствие</returns>
        public static string ToText(CalcMathOperation operation, bool isUnary)
        {
            if (!isUnary)
            {
                switch (operation)
                {
                    case CalcMathOperation.Divide:
                        return "/";
                    case CalcMathOperation.DivideInteger:
                        return ":";
                    case CalcMathOperation.DivideModulo:
                        return "%";
                    case CalcMathOperation.Minus:
                        return "-";
                    case CalcMathOperation.Multiply:
                        return "*";
                    case CalcMathOperation.Plus:
                        return "+";
                }
            }
            else
            {
                switch (operation)
                {
                    case CalcMathOperation.UnaryMinus:
                        return "-";
                    case CalcMathOperation.UnaryPlus:
                        return "+";
                }
            }
            return null;
        }

        /// <summary>
        /// Попытка извлечь токен - математическую операцию
        /// </summary>
        /// <param name="formula">формула</param>
        /// <param name="previousToken">предыдущий токен</param>
        /// <param name="currentTokenPosition">позиция в формуле, с которой нужно начинать парсить токен</param>
        /// <returns>информация о токене, null - токен не обнаружен</returns>
        public static ICalcToken TryToExtract(string formula, ICalcToken previousToken, int currentTokenPosition)
        {
            var token = new CalcTokenMathOperation { Position = currentTokenPosition };

            var formulaCut = formula.Remove(0, currentTokenPosition);
            foreach (var op in Enum.GetValues(typeof(CalcMathOperation)))
            {
                var operation = (CalcMathOperation)op;
                var isUnary = previousToken == null || previousToken is CalcTokenMathOperation || previousToken is CalcTokenLogicOperation ||
                (previousToken is CalcTokenBracket && (previousToken as CalcTokenBracket).Bracket == CalcBracket.Open);
                var operationText = ToText(operation, isUnary);
                if (operationText == null)
                    continue;
                if (!formulaCut.StartsWith(operationText))
                    continue;
                token.MathOperation = operation;
                token.TokenText = operationText;
                return token;
            }
            return null;
        }
    }
}