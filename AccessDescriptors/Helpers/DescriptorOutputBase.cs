﻿using System;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.FormulaKeeper;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorOutputBase: DescriptorBase
   {
        protected Guid OutputFormulaId = Guid.Empty;

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
            if (OutputFormulaId == Guid.Empty)
                OutputFormulaId = GlobalFormulaKeeper.Instance.StoreFormula(formula, GetId());
            else
                OutputFormulaId = GlobalFormulaKeeper.Instance.StoreOrChangeFormulaText(OutputFormulaId, formula, GetId());
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


            
