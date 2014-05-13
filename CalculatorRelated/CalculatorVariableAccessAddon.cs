using System.Linq;
using FlexRouter.CalculatorRelated.Tokens;
using FlexRouter.VariableSynchronization;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.CalculatorRelated
{
    /// <summary>
    /// Класс, позволяющий получать доступ к значениям переменных из формул 
    /// </summary>
    public class CalculatorVariableAccessAddon
    {
        /// <summary>
        /// Метод для доступа к значению переменной по токену в формуле
        /// </summary>
        /// <param name="tokenToPreprocess">Токен, в котором есть ссылка на переменную, null, если не удалось обработать</param>
        /// <returns>Токен с добавленным значением переменной в поле Value</returns>
        public ICalcToken VariablePreprocessor(ICalcToken tokenToPreprocess)
        {
            if (!(tokenToPreprocess is CalcTokenNumber))
                return tokenToPreprocess;
            string text = ((CalcTokenNumber)tokenToPreprocess).TokenText;
            if (!text.Contains('[') || !text.Contains(']') || !text.Contains('.'))
                return tokenToPreprocess;
            var varAndPanelName = text.Substring(1, text.Length - 2).Split('.');
            if (varAndPanelName.Length != 2)
                return null;
            var varId = Profile.GetVariableByPanelAndName(varAndPanelName[0], varAndPanelName[1]);
            if (varId == -1)
                return tokenToPreprocess;
            var readResult = VariableManager.ReadValue(varId);
            if (readResult.Error != ProcessVariableError.Ok)
                return tokenToPreprocess;
            ((CalcTokenNumber)tokenToPreprocess).Value = readResult.Value;
            return tokenToPreprocess;
        }

        /// <summary>
        /// Распознавание токенов обращения к значению переменной
        /// </summary>
        /// <param name="formula">Формула</param>
        /// <param name="currentTokenPosition">Текущая позиция в формуле (указывает на начало ещё не разобранного токена)</param>
        /// <returns>Токен</returns>
        public ICalcToken VariableTokenizer(string formula, int currentTokenPosition)
        {
            var token = new CalcTokenNumber(currentTokenPosition);
            string text = string.Empty;
            if (formula[currentTokenPosition] != '[' || formula.Length < currentTokenPosition+2)
                return null;
            text += formula[currentTokenPosition];
            for (var i = currentTokenPosition + 1; i < formula.Length; i++)
            {
                if (formula[i] == '[')
                    return new CalcTokenUnknown(currentTokenPosition) { Error = TokenError.UnexpectedSymbols, TokenText = text };
                text += formula[i];

                if (formula[i] != ']')
                    continue;
                if (text.Length == 2)
                    return new CalcTokenUnknown(currentTokenPosition) { Error = TokenError.UnexpectedSymbols, TokenText = text };
                token.TokenText = text;
                var varAndPanelName = text.Substring(1, text.Length - 2).Split('.');
                if (varAndPanelName.Length != 2)
                    return null;
                var varId = Profile.GetVariableByPanelAndName(varAndPanelName[0], varAndPanelName[1]);
                if (varId == -1)
                    token.Error = TokenError.TokenPointsAbsentItem;
                token.Error = TokenError.Ok;
                return token;
            }
            return new CalcTokenUnknown(currentTokenPosition) { Error = TokenError.UnexpectedSymbols, TokenText = text };
        }
    }
}
