using System;
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
        private readonly List<TokenPreprocessor> _preprocessors = new List<TokenPreprocessor>();
        public void RegisterPreprocessor(TokenPreprocessor preprocessor)
        {
            _preprocessors.Add(preprocessor);
        }
        private readonly List<TryToExtractToken> _tokenizers = new List<TryToExtractToken>();
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
        /// Разбор формулы на токены
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <returns>разбор прошёл удачно, если в последнем токене код ошибки ErrorCode.Ok. Количество токенов всегда больше нуля</returns>
        public ICalcToken[] TokenizeFormula(string formula)
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
                if (lastToken.Error != TokenError.Ok)
                    return tokens.ToArray();
            }
        }
        /// <summary>
        /// Общий метод извлечения очередного токена
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

            if ((token = CalcTokenNumber.TryToExtract(formula, previousToken, position)) != null)
                return token;
            if ((token = CalcTokenFormatter.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenBracket.TryToExtract(formula, position)) != null)
                return token;
            if((token = CalcTokenLogicOperation.TryToExtract(formula, position)) != null)
                return token;
            if ((token = CalcTokenMathOperation.TryToExtract(formula, previousToken, position)) != null)
                return token;
            if (_tokenizers.Any(tokenizer => (token = tokenizer(formula, position)) != null))
                return token;

            return new CalcTokenUnknown(position) { Error = TokenError.UnexpectedSymbols, TokenText=formula[position].ToString(CultureInfo.InvariantCulture)};
        }
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
        /// Временная надстройка для возможности обработки формул вида XXX ? 0 ; 1
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <returns>результат вычислений</returns>
        public ProcessingMathFormulaResult CalculateMathFormula(string formula)
        {
            // Обработка выражения logicformula == true ? mathformula ; mathformula
            var splittedFormulaCombonations = formula.Split(';');
            foreach (var sf in splittedFormulaCombonations)
            {
                var splittedIf = sf.Split('?');
                if (splittedIf.Length == 2)
                {
                    var logicResult = CalculateLogicFormula(splittedIf[0]);
                    if (logicResult.Error != ProcessingLogicFormulaError.Ok)
                        return new ProcessingMathFormulaResult
                        {
                            Error = ProcessingMathFormulaError.LogicConditionIsIncorrect,
                            Value = 0
                        };
                    if(logicResult.Value)
                        return CalculateMathFormula2(splittedIf[1]);
                }
                else
                {
                    return CalculateMathFormula2(sf);
                }
            }
            return new ProcessingMathFormulaResult
            {
                Error = ProcessingMathFormulaError.FormulaIsEmpty,
                Value = 0
            };
        }
        /// <summary>
        /// Рассчитать значение формулы
        /// </summary>
        /// <param name="formula">текст формулы</param>
        /// <returns>результат расчёта</returns>
        private ProcessingMathFormulaResult CalculateMathFormula2(string formula)
        {
            if (string.IsNullOrEmpty(formula))
            {
                var processResult = new ProcessingMathFormulaResult
                {
                    Error = ProcessingMathFormulaError.Ok,
                    Value = 0
                };
                return processResult;
            }

            var tokenizedFormula = TokenizeFormula(formula);
//            tokenizedFormula = PreprocessTokens(tokenizedFormula);
            return CalculateMathFormula(tokenizedFormula);
        }
        /// <summary>
        /// Расчёт математической формулы
        /// </summary>
        /// <returns>Результат расчёта</returns>
        public ProcessingMathFormulaResult CalculateMathFormula(ICalcToken[] formula)
        {
            formula = PreprocessTokens(formula);
            if(formula == null)
                return new ProcessingMathFormulaResult { Error = ProcessingMathFormulaError.FormulaIsEmpty, Value = 0};
            var formulaRpn = ConvertFormulaToReversePolishNotation(formula);
            if (formulaRpn.Length == 0)
                return new ProcessingMathFormulaResult {Error = ProcessingMathFormulaError.Ok, Value = 0};

            var valStack = new Stack<ICalcToken>();

            foreach (var t in formulaRpn)
            {
                if (t is CalcTokenNumber)
                {
                    valStack.Push(t);
                    continue;
                }

                if (!(t is CalcTokenMathOperation))
                    continue;

                var v1 = valStack.Pop();

                if ((t as CalcTokenMathOperation).MathOperation == CalcMathOperation.UnaryMinus)
                {
                    ((CalcTokenNumber) v1).Value = ((CalcTokenNumber) v1).Value * -1;
                    valStack.Push(v1);
                    continue;
                }
                if ((t as CalcTokenMathOperation).MathOperation == CalcMathOperation.UnaryPlus)
                {
                    ((CalcTokenNumber)v1).Value = Math.Abs(((CalcTokenNumber)v1).Value);
                    valStack.Push(v1);
                    continue;
                }

                double res;
                var n1 = ((CalcTokenNumber)v1).Value;
                var v2 = valStack.Pop();
                var n2 = ((CalcTokenNumber) v2).Value;

                switch (((CalcTokenMathOperation)t).MathOperation)
                {
                    case CalcMathOperation.Plus: res = n1 + n2; break;
                    case CalcMathOperation.Minus: res = n1 - n2; break;
                    case CalcMathOperation.Multiply: res = n1 * n2; break;
                    case CalcMathOperation.Divide: res = n1 / n2; break;
                    case CalcMathOperation.DivideModulo: res = n1 % n2; break;
                    case CalcMathOperation.DivideInteger: res = (long)(n1 / n2); break;
                    default:
                        res = 0; break;
                }
                var newToken = new CalcTokenNumber(0) {Value = res};
                valStack.Push(newToken);
            }
            var value = valStack.Pop();
            return new ProcessingMathFormulaResult { Error = ProcessingMathFormulaError.Ok, Value = ((CalcTokenNumber)value).Value };
        }
        public ProcessingLogicFormulaResult CalculateLogicFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula))
            {
                var processResult = new ProcessingLogicFormulaResult
                {
                    Error = ProcessingLogicFormulaError.Ok,
                    Value = true
                };
                return processResult;
            }
            var tokenizedFormula = TokenizeFormula(formula);
//            tokenizedFormula = PreprocessTokens(tokenizedFormula);
            return CalculateLogicFormula(tokenizedFormula);
        }
        /// <summary>
        /// Расчёт логической формулы
        /// </summary>
        /// <returns>Результат расчёта</returns>
        public ProcessingLogicFormulaResult CalculateLogicFormula(ICalcToken[] formula)
        {
            formula = PreprocessTokens(formula);
            var formulaRpn = ConvertFormulaToReversePolishNotation(formula);
            if (formulaRpn.Length == 0)
                return new ProcessingLogicFormulaResult {Error = ProcessingLogicFormulaError.Ok, Value = true};

            var valStack = new Stack<ICalcToken>();

            for (var i = 0; i < formulaRpn.Length; ++i)
            {
                if (formulaRpn[i] is CalcTokenNumber || formulaRpn[i] is CalcTokenBoolean)
                {
                    valStack.Push(formulaRpn[i]);
                    continue;
                }

                // Добавить сюда расчёт математики (+-*%...)
                if (!(formulaRpn[i] is CalcTokenLogicOperation))
                    continue;

                var v2 = valStack.Pop();
                var v1 = valStack.Pop();
                bool boolResult = false;
                if (v1 is CalcTokenNumber && v2 is CalcTokenNumber)
                {
                    var v1Value = ((CalcTokenNumber) v1).Value;
                    var v2Value = ((CalcTokenNumber) v2).Value;
                    switch (((CalcTokenLogicOperation) formulaRpn[i]).LogicOperation)
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
                    }
                }
                else if (v1 is CalcTokenBoolean && v2 is CalcTokenBoolean)
                {
                    var v1Value = ((CalcTokenBoolean) v1).Value;
                    var v2Value = ((CalcTokenBoolean) v2).Value;
                    switch (((CalcTokenLogicOperation) formulaRpn[i]).LogicOperation)
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
                    }
                }
                else
                {
                    var processResult = new ProcessingLogicFormulaResult
                        {
                            Error = ProcessingLogicFormulaError.CantCompareDifferentTypes,
                            Value = false
                        };
                    return processResult;
                }
                var resultToken = new CalcTokenBoolean(0) {Value = boolResult};
                valStack.Push(resultToken);
            }
            // Формула состояла из пробелов и переносов строк
            if (valStack.Count == 0)
                return new ProcessingLogicFormulaResult {Error = ProcessingLogicFormulaError.Ok, Value = true};

            var lastToken = valStack.Pop();
            return !(lastToken is CalcTokenBoolean)
                       ? new ProcessingLogicFormulaResult
                           {
                               Error = ProcessingLogicFormulaError.LogicFormulaResultIsNotBoolean,
                               Value = false
                           }
                       : new ProcessingLogicFormulaResult
                           {
                               Error = ProcessingLogicFormulaError.Ok,
                               Value = ((CalcTokenBoolean) lastToken).Value
                           };
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
                }

                if (!(t is CalcTokenBracket))
                    continue;
                
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
                    return 2;
            }
            if (calcTokenBase is CalcTokenMathOperation)
            {
                var t = (CalcTokenMathOperation) calcTokenBase;
                if (t.MathOperation == CalcMathOperation.UnaryPlus
                    || t.MathOperation == CalcMathOperation.UnaryMinus
                )
                    return 5;
                if (t.MathOperation == CalcMathOperation.Multiply
                    || t.MathOperation == CalcMathOperation.Divide
                    ||t.MathOperation == CalcMathOperation.DivideModulo
                    ||t.MathOperation == CalcMathOperation.DivideInteger
                )
                return 4;
                if (t.MathOperation == CalcMathOperation.Plus
                    ||t.MathOperation == CalcMathOperation.Minus
                )
                return 3;
            }
            return 0;
        }

        public ICalcToken CheckTokenizedFormula(ICalcToken[] formulaTokens)
        {
            var bracketCounter = 0; // +1 - открыта, -1 - закрыта
            var lastOpenBracketIndex = -1; // Индекс первой открытой скобки
            var importantTokenCounter = 0; // Нечётный токен - должен быть значением, чётный - операцией. А что насчёт скобок?

            // Если формулы нет
            if (formulaTokens.Length == 0)
                return null;
            // Если последний распаршенный токен имеет ошибку
            if (formulaTokens[formulaTokens.Length - 1].Error != TokenError.Ok)
                return formulaTokens[formulaTokens.Length - 1];


            for (int i = 0; i < formulaTokens.Length; i++)
            {
                ICalcToken token = formulaTokens[i];
                // Если очередной токен имеет ошибку
                if (token.Error != TokenError.Ok)
                    return token;
                if (token is CalcTokenBracket)
                {
                    if (((CalcTokenBracket) token).Bracket == CalcBracket.Open)
                    {
                        bracketCounter++;
                        lastOpenBracketIndex = i;
                    }
                    else
                    {
                        bracketCounter--;
                        if (bracketCounter < 0)
                        {
                            token.Error = TokenError.ClosingBracketNotOpened;
                            return token;
                        }
                    }
                }
                if (!(token is CalcTokenNumber) && !(token is CalcTokenLogicOperation) &&
                    !(token is CalcTokenMathOperation) && !(token is CalcTokenBoolean))
                    continue;
                // Одинаковые значимые токены?
                if (i > 0 && token.GetType() == formulaTokens[i - 1].GetType())
                {
                    token.Error = TokenError.SimilarTokensOneByOne;
                    return token;
                }
                importantTokenCounter++;
                if (importantTokenCounter%2 == 0) // Если токен чётный, это должна быть операция
                {
                    if (!(token is CalcTokenLogicOperation || token is CalcTokenMathOperation))
                    {
                        token.Error = TokenError.TokenMustBeOperation;
                        return token;
                    }
                }
                else // Если токен НЕчётный, это должно быть значение
                {
                    if (token is CalcTokenLogicOperation || token is CalcTokenMathOperation)
                    {
                        token.Error = TokenError.TokenMustBeValue;
                        return token;
                    }
                }
            }
            if (bracketCounter > 0)
            {
                formulaTokens[lastOpenBracketIndex].Error = TokenError.OpeningBracketNotClosed;
                return formulaTokens[lastOpenBracketIndex];
            }
            int lastTokenIndex = formulaTokens.Length - 1;
            if (formulaTokens[lastTokenIndex] is CalcTokenLogicOperation ||
                formulaTokens[lastTokenIndex] is CalcTokenMathOperation)
            {
                formulaTokens[lastTokenIndex].Error = TokenError.LastTokenCantBeOperation;
                return formulaTokens[lastTokenIndex];
            }
            return formulaTokens[lastTokenIndex];
        }
    }
}
