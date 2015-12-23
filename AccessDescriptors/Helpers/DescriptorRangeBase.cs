using System;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.FormulaKeeper;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorRangeBase : DescriptorMultistateBase
    {
        private Guid _minimumValueFormulaId = Guid.Empty;
        private Guid _maximumValueFormulaId = Guid.Empty;
        private Guid _defaultValueFormulaId = Guid.Empty;
        private Guid _stepFormulaId = Guid.Empty;
        private Guid _receiveValueFormulaId = Guid.Empty;
        private CycleType _cycleType;
        public bool EnableDefaultValue;

        public CycleType GetCycleType()
        {
            return _cycleType;
        }
        public void SetCycleType(CycleType cycleType)
        {
            _cycleType = cycleType;
        }
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
        public void SetFormulaHelper(string formula, ref Guid id)
        {
            id = id == Guid.Empty ? GlobalFormulaKeeper.Instance.StoreFormula(formula, GetId()) : GlobalFormulaKeeper.Instance.StoreOrChangeFormulaText(id, formula, GetId());
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
            writer.WriteAttributeString("GetValueFormula", GetReceiveValueFormula());
            if(_cycleType!=CycleType.None)
                writer.WriteAttributeString("CycleType", _cycleType.ToString());
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
            var getValueFormula = readerAdd.GetAttribute("GetValueFormula", readerAdd.NamespaceURI);
            SetReceiveValueFormula(getValueFormula);
            var cycleType = readerAdd.GetAttribute("CycleType", readerAdd.NamespaceURI);
            if (string.IsNullOrEmpty(cycleType))
                _cycleType = CycleType.None;
            foreach (CycleType ct in Enum.GetValues(typeof(CycleType)))
            {
                if(ct.ToString() != cycleType)
                    continue;
                _cycleType = ct;
                break;
            }

        }
    }
}
