using System.Xml;
using System.Xml.XPath;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorOutputBase: DescriptorBase
   {
//        protected string Formula;
        /// <summary>
        /// Получить формулу для расчёта значения для вывода на индикатор
        /// </summary>
        /// <returns>Токинезированная формула</returns>
        public string GetFormula()
        {
            //return Formula;
            return GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.SetValue, GetId());
        }
        /// <summary>
        /// Установить формулу для расчёта значения для вывода на индикатор
        /// </summary>
        /// <param name="formula">Токинезированная формула</param>
        public void SetFormula(string formula)
        {
            GlobalFormulaKeeper.Instance.SetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.SetValue, GetId(), formula);
//            Formula = formula;
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


            
