using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.VariableSynchronization;

namespace FlexRouter.VariableWorkerLayer.MethodFsuipc
{
    public class FsuipcVariable : VariableBase
    {
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderFsuipc);
        }
        public int Offset;
        public MemoryVariableSize Size;
        public double ValueToSet;
        public double ValueInMemory;
        public override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Offset", Offset.ToString("X"));
            writer.WriteAttributeString("Size", Size.ToString());
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            Offset = int.Parse(reader.GetAttribute("Offset", reader.NamespaceURI), NumberStyles.HexNumber);
            Size = (MemoryVariableSize)Enum.Parse(typeof(MemoryVariableSize), reader.GetAttribute("Size", reader.NamespaceURI));
        }
    }
}
