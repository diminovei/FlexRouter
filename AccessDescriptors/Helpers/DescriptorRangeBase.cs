using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorRangeBase : DescriptorMultistateBase
    {
        public double MinimumValue;
        public double MaximumValue;
        public double Step;
        public bool IsLooped;
        public bool EnableDefaultValue;
        public double DefaultValue;

        public string GetReceiveValueFormula()
        {
            return GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.GetValue, GetId());
        }
        public void SetReceiveValueFormula(string formula)
        {
            GlobalFormulaKeeper.Instance.SetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.GetValue, GetId(), formula);
        }

        public override void SaveAdditionals(XmlWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteStartElement("RangeParameters");
            writer.WriteAttributeString("MinimumValue", MinimumValue.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("MaximumValue", MaximumValue.ToString(CultureInfo.InvariantCulture));
            if (EnableDefaultValue)
                writer.WriteAttributeString("DefaultValue", DefaultValue.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Step", Step.ToString(CultureInfo.InvariantCulture));
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
            MinimumValue = double.Parse(readerAdd.GetAttribute("MinimumValue", readerAdd.NamespaceURI), CultureInfo.InvariantCulture);
            MaximumValue = double.Parse(readerAdd.GetAttribute("MaximumValue", readerAdd.NamespaceURI), CultureInfo.InvariantCulture);
            Step = double.Parse(readerAdd.GetAttribute("Step", readerAdd.NamespaceURI),CultureInfo.InvariantCulture);
            EnableDefaultValue = double.TryParse(readerAdd.GetAttribute("DefaultValue", readerAdd.NamespaceURI), NumberStyles.Number, CultureInfo.InvariantCulture, out DefaultValue);
            IsLooped = bool.Parse(readerAdd.GetAttribute("IsLooped", readerAdd.NamespaceURI));
            var getValueFormula = readerAdd.GetAttribute("GetValueFormula", readerAdd.NamespaceURI);
            SetReceiveValueFormula(getValueFormula);
        }
    }
}
