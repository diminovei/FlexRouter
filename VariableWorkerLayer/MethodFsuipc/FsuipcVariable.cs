using System;
using System.Drawing;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Localizers;
using FlexRouter.VariableSynchronization;

namespace FlexRouter.VariableWorkerLayer.MethodFsuipc
{
    public class FsuipcVariable : VariableBase, IMemoryVariable
    {
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderFsuipc);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.FsuipcVariable;
        }

        public int Offset;
        private MemoryVariableSize _size;
        public double ValueToSet;
        public double ValueInMemory;
        public override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Offset", Offset.ToString("X"));
            writer.WriteAttributeString("Size", _size.ToString());
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            Offset = int.Parse(reader.GetAttribute("Offset", reader.NamespaceURI), NumberStyles.HexNumber);
            _size = (MemoryVariableSize)Enum.Parse(typeof(MemoryVariableSize), reader.GetAttribute("Size", reader.NamespaceURI));
        }

        public MemoryVariableSize GetVariableSize()
        {
            return _size;
        }

        public void SetVariableSize(MemoryVariableSize size)
        {
            _size = size;
        }
    }
}
