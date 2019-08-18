using System;
using System.Drawing;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Localizers;

namespace FlexRouter.VariableWorkerLayer.MethodFsuipc
{
    public class FsuipcVariable : MemoryVariableBase, IMemoryVariable
    {
        public override bool IsEqualTo(object obj)
        {
            if (!(obj is FsuipcVariable))
                return false;
            var varToCompare = obj as FsuipcVariable;
            return Offset == varToCompare.Offset;
        }
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderFsuipc);
        }

        public int Offset;
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
