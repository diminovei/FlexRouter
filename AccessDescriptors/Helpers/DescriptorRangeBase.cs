using System.Xml;
using System.Xml.XPath;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorRangeBase : DescriptorMultistateBase
    {
        private int _minimumValueFormulaId = -1;
        private int _maximumValueFormulaId = -1;
        private int _defaultValueFormulaId = -1;
        private int _stepFormulaId = -1;
        private int _receiveValueFormulaId = -1;
        public bool IsLooped;
        public bool EnableDefaultValue;

        public string GetStepFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(_stepFormulaId);
        }
        public void SetStepFormula(string formula)
        {
            SetFormulaHelper(formula, ref _stepFormulaId);
        }
        public string GetMinimumValueFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(_minimumValueFormulaId);
        }
        public void SetMinimumValueFormula(string formula)
        {
            SetFormulaHelper(formula, ref _minimumValueFormulaId);
        }
        public string GetMaximumValueFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(_maximumValueFormulaId);
        }
        public void SetMaximumValueFormula(string formula)
        {
            SetFormulaHelper(formula, ref _maximumValueFormulaId);
        }
        public string GetDefaultValueFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(_defaultValueFormulaId);
        }
        public void SetDefaultValueFormula(string formula)
        {
            SetFormulaHelper(formula, ref _defaultValueFormulaId);
        }
        public void SetFormulaHelper(string formula, ref int id)
        {
            if (id == -1)
                id = GlobalFormulaKeeper.Instance.StoreFormula(formula, GetId());
            else
                GlobalFormulaKeeper.Instance.ChangeFormulaText(id, formula);
        }
        public string GetReceiveValueFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormulaText(_receiveValueFormulaId);
        }
        public void SetReceiveValueFormula(string formula)
        {
            SetFormulaHelper(formula, ref _receiveValueFormulaId);
        }

        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteStartElement("RangeParameters");
            writer.WriteAttributeString("MinimumValue", GetMinimumValueFormula());
            writer.WriteAttributeString("MaximumValue", GetMaximumValueFormula());
            if (EnableDefaultValue)
                writer.WriteAttributeString("DefaultValue", GetDefaultValueFormula());
            writer.WriteAttributeString("Step", GetStepFormula());
            writer.WriteAttributeString("IsLooped", IsLooped.ToString());
            writer.WriteAttributeString("GetValueFormula", GetReceiveValueFormula());
            writer.WriteEndElement();
        }

        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            var readerAdd = reader.SelectSingleNode("RangeParameters");
            if (readerAdd == null)
                return;
            var minimumValueFormula = readerAdd.GetAttribute("MinimumValue", readerAdd.NamespaceURI);
            SetMinimumValueFormula(minimumValueFormula);
            var maximumValueFormula = readerAdd.GetAttribute("MaximumValue", readerAdd.NamespaceURI);
            SetMaximumValueFormula(maximumValueFormula);
            var defaultValueFormula = readerAdd.GetAttribute("DefaultValue", readerAdd.NamespaceURI);
            EnableDefaultValue = !string.IsNullOrEmpty(defaultValueFormula);
            SetDefaultValueFormula(defaultValueFormula);
            var stepFormula = readerAdd.GetAttribute("Step", readerAdd.NamespaceURI);
            SetStepFormula(stepFormula);
            IsLooped = bool.Parse(readerAdd.GetAttribute("IsLooped", readerAdd.NamespaceURI));
            var getValueFormula = readerAdd.GetAttribute("GetValueFormula", readerAdd.NamespaceURI);
            SetReceiveValueFormula(getValueFormula);
        }
    }
}
