﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FlexRouter.CalculatorRelated.Tokens;

namespace FlexRouter.CalculatorRelated
{
    public delegate ICalcToken TryToExtractToken(string formula, int currentTokenPosition);
    public delegate ICalcToken TokenPreprocessor(ICalcToken tokenToPreprocess);
    //  Добавить:
    //  Формулы ToBCD, FromBCD, GetInt, GetModulo, ...
    public class Calculator
    {
        /// <summary>
        /// Список зарегистрированных внешних плагинов-препроцессоров
        /// </summary>
        private readonly List<TokenPreprocessor> _preprocessors = new List<TokenPreprocessor>();
        // ToDo: возможно стоит объединить внешние препроцессоры и токенайзеры, так как токенизация и расчёт сейчас не разделяются и проверка формулы происходи на лету
        /// <summary>
        /// Зарегистрировать внешний плагин-препорцессор
        /// Препорцессор вызывается перед вычислением формулы. В нём можно обновить значение токена. Например, взять из переменной в памяти свежее значение
        /// </summary>
        /// <param name="preprocessor">метод токенизации</param>
        public void RegisterPreprocessor(TokenPreprocessor preprocessor)
        {
            _preprocessors.Add(preprocessor);
        }
        /// <summary>
        /// Список зарегистрированных внешних токенизаторов-плагинов
        /// </summary>
        private readonly List<TryToExtractToken> _tokenizers = new List<TryToExtractToken>();
        /// <summary>
        /// Зарегистрировать внешний токенайзер-плагин
        /// Токенизатор преобразует часть формулы в класс. Например, число, операция, скобка, ...
        /// </summary>
        /// <param name="tokenizer">метод токенизации</param>
        public void RegisterTokenizer(TryToExtractToken tokenizer)
        {
            _tokenizers.Add(tokenizer);
        }
        /// <summary>
        /// Превратить токенизированную формулу в текст
        /// </summary>
        /// <param name="tokenizedFormula"></param>
        /// <returns></returns>
        public string DetokenizeFormula(IEnumerable<ICalcToken> tokenizedFormula)
        {
            return tokenizedFormula == null ? string.Empty : tokenizedFormula.Aggregate(string.Empty, (current, token) => current + ((CalcTokenBase) token).TokenText);
        }
        /// <summary>
        /// Разбор формулы на токены, пропуская форматирующие токены
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <returns>разбор прошёл удачно, если в последнем токене код ошибки ErrorCode.Ok. Количество токенов всегда больше нуля</returns>
        private ICalcToken[] TokenizeFormulaSkipFormatters(string formula)
        {
            var tokens = new List<ICalcToken>();
            var tokenId = 0;
            ICalcToken lastToken = null;
            while (true)
            {
                lastToken = TryToExtract(formula, lastToken);
                // Разбор формулы окончен
                if (lastToken == null)
                    return tokens.ToArray();

                // Добавляем полученный токен в список токенов
                if (!(lastToken is CalcTokenFormatter))
                {
                    tokens.Add(lastToken);
                    lastToken.Id = tokenId;
                    tokenId++;
                }

                // Если произошла ошибка при разборе формулы, заканчиваем разбор
                if (lastToken.Error != FormulaError.Ok)
                    return tokens.ToArray();
            }
        }
        /// <summary>
        /// Общий метод извлечения очередного токена. Вызывает метод TryToExtract в классах токенов
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <param name="previousToken">предыдущий токен. Null, если это первый токен</param>
        /// <returns>null - формула разобрана, иначе токен</returns>
        private ICalcToken TryToExtract(string formula, ICalcToken previousToken)
        {
            ICalcToken token;
            var position = previousToken == null ? 0 : previousToken.GetNextTokenPosition();
            if (formula.Length == position)
                return null; // Формула распаршена

            if ((token = CalcTokenIfStatement.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenFormulaSeparator.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenNumber.TryToExtract(formula, previousToken, position)) != null)
                return token;
            if ((token = CalcTokenFormatter.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenBracket.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenLogicOperation.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenMathOperation.TryToExtract(formula, previousToken, position)) != null)
                return token;
            if (_tokenizers.Any(tokenizer => (token = tokenizer(formula, position)) != null))
                return token;

            return new CalcTokenUnknown(position) { Error = FormulaError.UnexpectedSymbols, TokenText = formula[position].ToString(CultureInfo.InvariantCulture) };
        }
        /// <summary>
        /// Предобработка токена перед тем, как передать его методу расчёта значения (например, получение значения из переменной в памяти)
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        private ICalcToken[] PreprocessTokens(ICalcToken[] formula)
        {
            for (var i = 0; i < formula.Length; i++)
            {
                if (!(formula[i] is CalcTokenNumber))
                    continue;
                foreach (var tokenPreprocessor in _preprocessors)
                {
                    var tokenRes = tokenPreprocessor(formula[i]);
                    if (tokenRes == null)
                        continue;
                    formula[i] = tokenRes;
                }
                if (formula[i] == null)
                    return null;
            }
            return formula;
        }
        /// <summary>
        /// Состояние обработки условий
        /// </summary>
        private enum ConditionFormulaState
        {
            Idle,                               // Знака ? и логической формулы не было, поэтому ожидаем математического результата
            LogicPartWasTrueWaitingForMathPart, // Встретился знак ? и результат вычисления логической части - true, поэтому ожидаем установку значения "1==1 ? 2; 3" - устанавливаем 2
            LogicPartWasFalseSkipMathPart       // Встретился знак ? и результат вычисления логической части - false, поэтому пропускаем следующую за знаком ? математическую формулу "1!=1 ? 2; 3" - устанавливаем 3
        }
        /// <summary>
        /// Рассчитать формулу. Вернуть результат или описание ошибки в формуле
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <returns>результат ресчёта или описание ошибки в формуле</returns>
        public FormulaComputeResult ComputeFormula(string formula)
        {
            try
            {
                if (string.IsNullOrEmpty(formula))
                    return new FormulaComputeResult(FormulaError.FormulaIsEmpty);
                var tokenizedFormula = TokenizeFormulaSkipFormatters(formula);
                if (tokenizedFormula.Length == 0)
                    return new FormulaComputeResult(FormulaError.FormulaIsEmpty);
                if (tokenizedFormula[tokenizedFormula.Length - 1].Error != FormulaError.Ok)
                    return new FormulaComputeResult(tokenizedFormula[tokenizedFormula.Length - 1]);
                var tokensToProcess = new List<ICalcToken>();
                var conditionFormulaState = ConditionFormulaState.Idle;
                foreach (var token in tokenizedFormula)
                {
                    if (conditionFormulaState == ConditionFormulaState.LogicPartWasFalseSkipMathPart && !(token is CalcTokenFormulaSeparator))
                        continue;
                    // Если встретили ?
                    if (token is CalcTokenIfStatement)
                    {
                        // x == true ? y == true ? (ошибка, 2 условия подряд без установки значения)
                        if (conditionFormulaState == ConditionFormulaState.LogicPartWasTrueWaitingForMathPart)
                            return new FormulaComputeResult(FormulaError.ThisFormulaPartMustBeMath);
                        // Обрабатываем логическую формулу
                        var calculateResult = ComputeTokenizedFormula(tokensToProcess.ToArray());
                        if (calculateResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.Error)
                            return calculateResult;
                        // x+1 ? 11 (часть до ? не была логическим условием)
                        if (calculateResult.GetFormulaComputeResultType() != TypeOfComputeFormulaResult.BooleanResult)
                            return new FormulaComputeResult(FormulaError.ThisFormulaPartMustBeLogic);
                        // Решаем, установка значения или его пропуск в зависимости от срабатывания условия в логической формуле
                        conditionFormulaState = calculateResult.CalculatedBoolBoolValue ? ConditionFormulaState.LogicPartWasTrueWaitingForMathPart : ConditionFormulaState.LogicPartWasFalseSkipMathPart;
                        tokensToProcess.Clear();
                        continue;
                    }
                    // Если встретили ;
                    if (token is CalcTokenFormulaSeparator)
                    {
                        // Если условие отработало с true - считаем формулу после условия и выходим
                        if (conditionFormulaState != ConditionFormulaState.LogicPartWasFalseSkipMathPart)
                            break;
                        conditionFormulaState = ConditionFormulaState.Idle;
                        tokensToProcess.Clear();
                        continue;
                    }
                    tokensToProcess.Add(token);
                }
                // Если токены закончились
                return ComputeTokenizedFormula(tokensToProcess.ToArray());
            }
            catch (Exception)
            {
                return new FormulaComputeResult(FormulaError.Exception);
            }
        }
        /// <summary>
        /// Рассчитать значение токенизированной формулы
        /// </summary>
        /// <param name="tokenizedFormula">Токенизированная формула</param>
        /// <returns></returns>
        private FormulaComputeResult ComputeTokenizedFormula(ICalcToken[] tokenizedFormula)
        {
            tokenizedFormula = PreprocessTokens(tokenizedFormula);
            if (tokenizedFormula == null || tokenizedFormula.Length == 0)
                return new FormulaComputeResult(FormulaError.FormulaIsEmpty);
            var badToken = CheckFormulaAndGetBadToken(tokenizedFormula);
            if(badToken != null)
                return new FormulaComputeResult(badToken);
            var formulaRpn = ConvertFormulaToReversePolishNotation(tokenizedFormula);
            if (formulaRpn.Length == 0)
                return new FormulaComputeResult(FormulaError.FormulaIsEmpty);
            var valStack = new Stack<ICalcToken>();

            foreach (var token in formulaRpn)
            {
                if (token is CalcTokenNumber || token is CalcTokenBoolean)
                {
                    valStack.Push(token);
                    continue;
                }

                if (!(token is CalcTokenMathOperation) && !(token is CalcTokenLogicOperation))
                    continue;

                var mathValueToken1 = valStack.Pop();

                ICalcToken resultToken;
                if (token is CalcTokenMathOperation)
                {
                    // Выполняем унарную операцию -
                    if (((token as CalcTokenMathOperation).MathOperation == CalcMathOperation.UnaryMinus)
                        || ((token as CalcTokenMathOperation).MathOperation == CalcMathOperation.UnaryPlus))
                    {
                        resultToken = ProcessUnaryMathOperation(((CalcTokenNumber) mathValueToken1), token as CalcTokenMathOperation);
                    }
                    else
                    {
                        var mathValueToken2 = valStack.Pop();
                        if (mathValueToken1.GetType() != mathValueToken2.GetType())
                            return new FormulaComputeResult(FormulaError.CantOperateMathAndLogicValues);
                        resultToken = ProcessMathOperation(mathValueToken2, mathValueToken1, (CalcTokenMathOperation)token);
                        if(resultToken.Error!=FormulaError.Ok)
                            return new FormulaComputeResult(resultToken);
                    }
                }
                else
                {
                    // Выполняем логические операции
                    var valueToken2 = valStack.Pop();
                    resultToken = ProcessLogicOperation(valueToken2, mathValueToken1, token as CalcTokenLogicOperation);
                    if (resultToken.Error != FormulaError.Ok)
                        return new FormulaComputeResult(resultToken);
                }
                valStack.Push(resultToken);
            }
            var value = valStack.Pop();

            return value is CalcTokenBoolean
                ? new FormulaComputeResult((value as CalcTokenBoolean).Value)
                : new FormulaComputeResult((value as CalcTokenNumber).Value);
        }
        /// <summary>
        /// Обработать унарную математическую операцию (один токен + операция. Например, -10 - сменить знак у 10 на отрицательный)
        /// </summary>
        /// <param name="token">токен</param>
        /// <param name="mathUnaryOperationToken">токен логической операции</param>
        /// <returns>результирующий токен или ошибка. Ошибок быть не может</returns>
        private ICalcToken ProcessUnaryMathOperation(CalcTokenNumber token, CalcTokenMathOperation mathUnaryOperationToken)
        {
            // Выполняем унарную операцию -
            if (mathUnaryOperationToken.MathOperation == CalcMathOperation.UnaryMinus)
                token.Value = token.Value * -1;
            // Выполняем унарную операцию +
            if(mathUnaryOperationToken.MathOperation == CalcMathOperation.UnaryPlus)
                token.Value = Math.Abs(token.Value);
            return token;
        }
        /// <summary>
        /// Обработать математическую операцию над двумя токенами
        /// Для корректного выполнения сравнения "больше, меньше, ..." важен порядок токенов, передаваемых в формулу
        /// </summary>
        /// <param name="token1">первый токен</param>
        /// <param name="token2">второй токен</param>
        /// <param name="mathOperationToken">токен матеметической операции</param>
        /// <returns>результирующий токен или ошибка, установленная методом в одном из входных токенов</returns>
        private ICalcToken ProcessMathOperation(ICalcToken token1, ICalcToken token2, CalcTokenMathOperation mathOperationToken)
        {
            double mathResult;
            var n1 = ((CalcTokenNumber)token1).Value;
            var n2 = ((CalcTokenNumber)token2).Value;

            var n1s = n1.ToString(CultureInfo.InvariantCulture);
            n1 = double.Parse(n1s, CultureInfo.InvariantCulture);
            var n2s = n2.ToString(CultureInfo.InvariantCulture);
            n2 = double.Parse(n2s, CultureInfo.InvariantCulture);
                
            switch (mathOperationToken.MathOperation)
            {
                case CalcMathOperation.Plus:
                    mathResult = n1 + n2;
                    break;
                case CalcMathOperation.Minus:
                    mathResult = n1 - n2;
                    break;
                case CalcMathOperation.Multiply:
                    mathResult = n1 * n2;
                    break;
                case CalcMathOperation.Divide:
                    if ((int)n2 == 0)
                    {
                        token2.Error = FormulaError.DivisionByZero;
                        return token2;
                    }
                    mathResult = n1 / n2;
                    break;
                case CalcMathOperation.DivideModulo:
                    if ((int)n2 == 0)
                    {
                        token2.Error = FormulaError.DivisionByZero;
                        return token2;
                    }
                    mathResult = n1 % n2;
                    break;
                case CalcMathOperation.DivideInteger:
                    if ((int)n2 == 0)
                    {
                        token2.Error = FormulaError.DivisionByZero;
                        return token2;
                    }
                    mathResult = (long)(n1 / n2);
                    break;
                default:
                     mathOperationToken.Error = FormulaError.UnknownMathOperation;
                     return mathOperationToken;
            }
            // Результат возвращаем в стэк
            return new CalcTokenNumber(0) { Value = mathResult };
        }
        /// <summary>
        /// Обработать логическую операцию над двумя логическими токенами (bool) или двумя числами
        /// Для корректного выполнения сравнения "больше, меньше, ..." важен порядок токенов, передаваемых в формулу
        /// </summary>
        /// <param name="token1">первый токен</param>
        /// <param name="token2">второй токен</param>
        /// <param name="logicOperationToken">токен логической операции</param>
        /// <returns>результирующий токен или ошибка, установленная методом в одном из входных токенов</returns>
        private ICalcToken ProcessLogicOperation(ICalcToken token1, ICalcToken token2, CalcTokenLogicOperation logicOperationToken)
        {
            bool boolResult;
            if (token1 is CalcTokenNumber && token2 is CalcTokenNumber)
            {
                var v1Value = ((CalcTokenNumber)token1).Value;
                var v2Value = ((CalcTokenNumber)token2).Value;
                switch (logicOperationToken.LogicOperation)
                {
                    case CalcLogicOperation.Equal:
                        boolResult = v1Value == v2Value;
                        break;
                    case CalcLogicOperation.Greater:
                        boolResult = v1Value > v2Value;
                        break;
                    case CalcLogicOperation.GreaterOrEqual:
                        boolResult = v1Value >= v2Value;
                        break;
                    case CalcLogicOperation.Less:
                        boolResult = v1Value < v2Value;
                        break;
                    case CalcLogicOperation.LessOrEqual:
                        boolResult = v1Value <= v2Value;
                        break;
                    case CalcLogicOperation.Not:
                        boolResult = v1Value != v2Value;
                        break;
                    default:
                        logicOperationToken.Error = FormulaError.UnknownLogicOperation;
                        return logicOperationToken;
                }
            }
            else if (token1 is CalcTokenBoolean && token2 is CalcTokenBoolean)
            {
                var v1Value = ((CalcTokenBoolean)token1).Value;
                var v2Value = ((CalcTokenBoolean)token2).Value;
                switch (logicOperationToken.LogicOperation)
                {
                    case CalcLogicOperation.And:
                        boolResult = v1Value && v2Value;
                        break;
                    case CalcLogicOperation.Equal:
                        boolResult = v1Value == v2Value;
                        break;
                    case CalcLogicOperation.Not:
                        boolResult = v1Value != v2Value;
                        break;
                    case CalcLogicOperation.Or:
                        boolResult = v1Value || v2Value;
                        break;
                    default:
                        logicOperationToken.Error = FormulaError.UnknownLogicOperation;
                        return logicOperationToken;
                }
            }
            else
            {
                return new CalcTokenUnknown(0) {Error = FormulaError.CantOperateMathAndLogicValues};
            }
            return new CalcTokenBoolean(0) { Value = boolResult };
        }
        /// <summary>
        /// Проверить корректность токенизированной формулы и вернуть ошибку, если она есть. Токены типа Formatter должны быть уже исключены.
        /// </summary>
        /// <param name="tokens">токенизированная формула</param>
        /// <returns>токен с ошибкой или null, если формула прошла проверку</returns>
        private ICalcToken CheckFormulaAndGetBadToken(ICalcToken[] tokens)
        {
            var bracketCounter = 0; // +1 - открыта, -1 - закрыта
            var lastOpenBracketIndex = -1; // Индекс первой открытой скобки
            if (tokens.Length == 0)
                return null;
            for (var i = 0; i < tokens.Length; i++)
            {
                var prevToken = i == 0 ? null : tokens[i - 1];
                var currentToken = tokens[i];
                var nextToken = i == tokens.Length - 1 ? null : tokens[i + 1];

                // Если ошибка была обнаружена на этапе токенизации
                if (currentToken.Error != FormulaError.Ok)
                    return currentToken;

                // Проверка скобок
                // ")" || "(1+3))" || "3()" || "1+3(" || "1+3)"
                var bracketToken = currentToken as CalcTokenBracket;
                if (bracketToken != null)
                {
                    if (bracketToken.Bracket == CalcBracket.Open)
                    {
                        // Перед открывающей скобкой не может быть числа. Только скобка, операция или разделитель
                        if (prevToken is CalcTokenNumber)
                        {
                            currentToken.Error = FormulaError.TokenMustBeOperation;
                            return currentToken;
                        }
                        // После открывающейся скобки может быть или открывающаяся скобка или число или унарный плюс/минус
                        var nextMathToken = nextToken as CalcTokenMathOperation;
                        var nextBracketToken = nextToken as CalcTokenBracket;
                        if ((nextBracketToken!=null && nextBracketToken.Bracket == CalcBracket.Open) || (nextMathToken != null && nextMathToken.MathOperation != CalcMathOperation.UnaryMinus && nextMathToken.MathOperation != CalcMathOperation.UnaryPlus))
                        {
                            nextToken.Error = FormulaError.TokenMustBeValue;
                            return nextToken;
                        }

                        bracketCounter++;
                        lastOpenBracketIndex = i;
                    }
                    else
                    {
                        bracketCounter--;
                        // Закрывающих скобок не может быть больше, чем открывающих
                        if (bracketCounter < 0)
                        {
                            currentToken.Error = FormulaError.ClosingBracketNotOpened;
                            return currentToken;
                        }
                    }
                }
                // Если подряд два токена одинакового типа
                if (prevToken != null && (prevToken.GetType() == currentToken.GetType()) && (!(currentToken is CalcTokenBracket)) && (!(currentToken is CalcTokenFormatter)))
                {
                    currentToken.Error = FormulaError.SimilarTokensOneByOne;
                    return currentToken;
                }
                //  Двойная точка в числе или число оканчивается на точку
                if (currentToken is CalcTokenNumber)
                {
                    if ((currentToken as CalcTokenNumber).TokenText.EndsWith("."))
                    {
                        currentToken.Error = FormulaError.DotCantBeLastSymbolOfNumber;
                        return currentToken;
                    }
                    if ((currentToken as CalcTokenNumber).TokenText.IndexOf('.') !=
                        (currentToken as CalcTokenNumber).TokenText.LastIndexOf('.'))
                    {
                        currentToken.Error = FormulaError.MultipluDotInNumber;
                        return currentToken;
                    }
                }
                // Если обрабатываем последний токен
                if (nextToken == null || nextToken is CalcTokenIfStatement || nextToken is CalcTokenFormulaSeparator)
                {
                    if (!(currentToken is CalcTokenNumber) &&
                        (!(currentToken is CalcTokenBracket) ||
                         (currentToken as CalcTokenBracket).Bracket != CalcBracket.Close)) 
                    {
                        currentToken.Error = FormulaError.LastTokenCantBeOperation;
                        return currentToken;
                    }
                }
            }
            if (bracketCounter > 0)
            {
                tokens[lastOpenBracketIndex].Error = FormulaError.OpeningBracketNotClosed;
                return tokens[lastOpenBracketIndex];
            }
            return null;
        }
        /// <summary>
        /// Превращаем токенизированную формулу в обратную польскую нотацию
        /// </summary>
        /// <param name="tokens">Токенизированная формула</param>
        /// <returns>Формула в обратной польской нотации</returns>
        private ICalcToken[] ConvertFormulaToReversePolishNotation(IEnumerable<ICalcToken> tokens)
        {
            var operationsStack = new Stack<ICalcToken>();
            var tokenRpn = new List<ICalcToken>();

            foreach (var t in tokens)
            {
                if(t is CalcTokenFormatter)
                    continue;
                //проверяем, это операнд или операция
                if (t is CalcTokenNumber || t is CalcTokenBoolean)
                {
                    tokenRpn.Add(t);
                    continue;
                }

                if (!(t is CalcTokenBracket))
                {
                    while (true)
                    {
                        if (operationsStack.Count == 0 || GetTokenPriority(t) > GetTokenPriority(operationsStack.Peek()))
                        {
                            operationsStack.Push(t);
                            break;
                        }
                        while (operationsStack.Count != 0 && GetTokenPriority(t) <= GetTokenPriority(operationsStack.Peek()))
                        {
                            tokenRpn.Add(operationsStack.Pop());
                        }
                    }
                    continue;
                }

//                if (!(t is CalcTokenBracket))
//                    continue;
                
                // Если скобка открывающаяся, закидываем её в стэк
                if (((CalcTokenBracket)t).Bracket == CalcBracket.Open)
                {
                    operationsStack.Push(t);
                    continue;
                }

                // Значит закрывающая скобка
                // закpывающая кpуглая скобка выталкивает все опеpации из стека до ближайшей откpывающей скобки
                // сами скобки в выходную стpоку не пеpеписываются, а уничтожают дpуг дpуга.
                while (operationsStack.Count != 0)
                {
                    if (operationsStack.Peek() is CalcTokenBracket)
                    {
                        // Если это открывающая скобка - забираем её из стека и выходим из цикла
                        if (((CalcTokenBracket) t).Bracket == CalcBracket.Open)
                        {
                            operationsStack.Pop();
                            break;
                        }
                    }
                    else // Если это не скобка
                        tokenRpn.Add(operationsStack.Peek());
                    operationsStack.Pop();
                }
            }
            // Забираем то, что осталось в стеке
            while (operationsStack.Count > 0)
                tokenRpn.Add(operationsStack.Pop());
            return tokenRpn.ToArray();
        }
        /// <summary>
        /// Получить приоритет математической/логической операции
        /// </summary>
        /// <param name="calcTokenBase">Токенизированная формула</param>
        /// <returns>Приоритет операции</returns>
        private int GetTokenPriority(ICalcToken calcTokenBase)
        {
            if (calcTokenBase is CalcTokenFormatter || calcTokenBase is CalcTokenBoolean ||
                calcTokenBase is CalcTokenNumber || calcTokenBase is CalcTokenUnknown)
                return 0;
                                                  
            if (calcTokenBase is CalcTokenBracket)
            {
                if (((CalcTokenBracket) calcTokenBase).Bracket == CalcBracket.Close)
                    return 1;
            } 
            if (calcTokenBase is CalcTokenLogicOperation)
            {
                var t = (CalcTokenLogicOperation) calcTokenBase;
                if (t.LogicOperation == CalcLogicOperation.Equal
                    || t.LogicOperation == CalcLogicOperation.Greater
                    || t.LogicOperation == CalcLogicOperation.Less
                    || t.LogicOperation == CalcLogicOperation.LessOrEqual
                    || t.LogicOperation == CalcLogicOperation.GreaterOrEqual
                    )
                    return 4;
                if (t.LogicOperation == CalcLogicOperation.And
                    || t.LogicOperation == CalcLogicOperation.Or
                    || t.LogicOperation == CalcLogicOperation.Not
                    )
                    return 3;
            }
            if (calcTokenBase is CalcTokenMathOperation)
            {
                var t = (CalcTokenMathOperation) calcTokenBase;
                if (t.MathOperation == CalcMathOperation.UnaryPlus
                    || t.MathOperation == CalcMathOperation.UnaryMinus
                )
                    return 10;
                if (t.MathOperation == CalcMathOperation.Multiply
                    || t.MathOperation == CalcMathOperation.Divide
                    ||t.MathOperation == CalcMathOperation.DivideModulo
                    ||t.MathOperation == CalcMathOperation.DivideInteger
                )
                return 8;
                if (t.MathOperation == CalcMathOperation.Plus
                    ||t.MathOperation == CalcMathOperation.Minus
                )
                return 6;
            }
            return 0;
        }
    }
}
