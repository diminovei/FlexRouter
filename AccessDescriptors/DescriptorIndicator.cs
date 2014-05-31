using System.Globalization;
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
        public override string GetDescriptorName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeMemoryIndicator);
        }

        /// <summary>
        /// Количество цифр после запятой в тексте, передаваемом на индикатор
        /// </summary>
        private byte _digitsAfterPoint;
        /// <summary>
        /// Установить количество цифр после запятой в тексте, передаваемом на индикатор
        /// </summary>
        /// <param name="digitsNumber">количество цифр после запятой</param>
        public void SetNumberOfDigitsAfterPoint(byte digitsNumber)
        {
            _digitsAfterPoint = digitsNumber;
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
        /// Получить текст для вывода на индикатор
        /// </summary>
        /// <returns>Текст для индикатора</returns>
        public string GetIndicatorText()
        {
            if (!IsPowerOn())
                return string.Empty;
            var formulaResult = CalculatorE.ComputeFormula(GetFormula());
            if (formulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.FormulaWasEmpty)
                return string.Empty;
            if (formulaResult.GetFormulaComputeResultType() != TypeOfComputeFormulaResult.DoubleResult)
                return "Error";
            var mask = "{0:0.";

            for (var i = 0; i < _digitsAfterPoint; i++)
                mask += "0";
            mask += "}";
            var result = string.Format(mask, formulaResult.CalculatedDoubleValue);
            return result;
        }

        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteAttributeString("DigitsAfterPoint", _digitsAfterPoint.ToString(CultureInfo.InvariantCulture));
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            _digitsAfterPoint = byte.Parse(reader.GetAttribute("DigitsAfterPoint", reader.NamespaceURI));
        }
    }
}
