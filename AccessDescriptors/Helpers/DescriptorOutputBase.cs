using System.Xml;
using System.Xml.XPath;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorOutputBase: DescriptorBase
   {
        protected int OutputFormulaId = -1;

        /// <summary>
        /// Получить формулу для расчёта значения для вывода на индикатор
        /// </summary>
        /// <returns>Токинезированная формула</returns>
        public string GetFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(OutputFormulaId);
        }
        /// <summary>
        /// Установить формулу для расчёта значения для вывода на индикатор
        /// </summary>
        /// <param name="formula">Токинезированная формула</param>
        public void SetFormula(string formula)
        {
            if (OutputFormulaId == -1)
                OutputFormulaId = GlobalFormulaKeeper.Instance.StoreFormula(formula, GetId());
            else
                GlobalFormulaKeeper.Instance.ChangeFormulaText(OutputFormulaId, formula);
        }
        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteAttributeString("Formula", GetFormula());
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            var formula = reader.GetAttribute("Formula", reader.NamespaceURI);
            SetFormula(formula);
        }
    }
}


            
