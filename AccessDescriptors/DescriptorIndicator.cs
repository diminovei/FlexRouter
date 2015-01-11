using System.Drawing;
using System.Globalization;
using System.Windows.Controls;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.Localizers;

namespace FlexRouter.AccessDescriptors
{
    public class DescriptorIndicator : DescriptorOutputBase, IIndicatorMethods
    {
        public override string GetDescriptorType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryIndicator);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.Indicator;
        }

        /// <summary>
        /// Количество цифр после запятой в тексте, передаваемом на индикатор
        /// </summary>
        private byte _digitsAfterPoint = 1;
        /// <summary>
        /// Количество знаков в тексте, передаваемом на индикатор (без точки)
        /// </summary>
        private byte _digitsNumber = 7;
        /// <summary>
        /// Установить количество цифр после запятой в тексте, передаваемом на индикатор
        /// </summary>
        /// <param name="digitsNumber">количество цифр после запятой</param>
        public void SetNumberOfDigitsAfterPoint(byte digitsNumber)
        {
            _digitsAfterPoint = digitsNumber;
        }
        /// <summary>
        /// Установить количество знаков в тексте, передаваемом на индикатор (без точки)
        /// </summary>
        /// <param name="digitsNumber">количество цифр после запятой</param>
        public void SetNumberOfDigits(byte digitsNumber)
        {
            _digitsNumber = digitsNumber;
        }
        /// <summary>
        /// Получить количество цифр после запятой в тексте, передаваемом на индикатор
        /// </summary>
        /// <returns>количество цифр после запятой</returns>
        public byte GetNumberOfDigitsAfterPoint()
        {
            return _digitsAfterPoint;
        }
        /// <summary>
        /// Получить общее количество отображаемых знаков в тексте, передаваемом на индикатор
        /// </summary>
        /// <returns>количество знаков</returns>
        public byte GetNumberOfDigits()
        {
            return _digitsNumber;
        }

        /// <summary>
        /// Получить текст для вывода на индикатор
        /// </summary>
        /// <returns>Текст для индикатора</returns>
        public string GetIndicatorText()
        {
            string res = string.Empty;
            if (!IsPowerOn())
            {
                res = res.PadRight(_digitsNumber, ' ');
                return res;
            }
            var formulaResult = CalculatorE.ComputeFormula(GetFormula());
            if (formulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.FormulaWasEmpty)
            {
                res = res.PadRight(_digitsNumber, ' ');
                return res;
            }
            if (formulaResult.GetFormulaComputeResultType() != TypeOfComputeFormulaResult.DoubleResult)
                return "Error";
            var valueAsString = formulaResult.CalculatedDoubleValue.ToString(CultureInfo.InvariantCulture).Split('.');
            
            var beforePointText = valueAsString[0];
            var afterPointText = valueAsString.Length > 1 ? valueAsString[1] : string.Empty;
            var digitsBeforePoint = _digitsNumber - _digitsAfterPoint;
            // Цифры после запятой. Удаляем лишние или дополняем нулями, если не хватает
            // Дополняем нулями, если не хватает
            if (afterPointText.Length < _digitsAfterPoint)
                afterPointText = afterPointText.PadRight(_digitsAfterPoint, '0');
            // Удаляем лишнее, если оно есть
            if (afterPointText.Length > _digitsAfterPoint)
                afterPointText = afterPointText.Remove(_digitsAfterPoint);

            // Дополняем нулями, если не хватает
            if (beforePointText.Length < digitsBeforePoint)
                beforePointText = beforePointText.PadLeft(digitsBeforePoint, ' ');
            // Удаляем лишнее, если оно есть
            if (beforePointText.Length > digitsBeforePoint)
                beforePointText = beforePointText.Remove(0, beforePointText.Length-digitsBeforePoint);

            res = beforePointText + (_digitsAfterPoint == 0 ? "" : "." + afterPointText);

            return res;
            //var mask = "{0:0.";

            //for (var i = 0; i < _digitsAfterPoint; i++)
            //    mask += "0";
            //mask += "}";
            //var result = string.Format(mask, formulaResult.CalculatedDoubleValue);
            //return result;
        }

        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteAttributeString("DigitsAfterPoint", _digitsAfterPoint.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("TotalDigitsNumber", _digitsNumber.ToString(CultureInfo.InvariantCulture));
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            _digitsAfterPoint = byte.Parse(reader.GetAttribute("DigitsAfterPoint", reader.NamespaceURI));
            _digitsNumber = byte.Parse(reader.GetAttribute("TotalDigitsNumber", reader.NamespaceURI));
        }
    }
}
