using System;
using System.Drawing;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Localizers;
using FlexRouter.VariableSynchronization;

namespace FlexRouter.VariableWorkerLayer.MethodFakeVariable
{
    public class FakeVariable : VariableBase, IMemoryVariable
    {
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderFakeVariable);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.FakeVariable;
        }

        public MemoryVariableSize Size;
        public double ValueToSet;
        public double ValueInMemory;
        public override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Size", Size.ToString());
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            Size = (MemoryVariableSize)Enum.Parse(typeof(MemoryVariableSize), reader.GetAttribute("Size", reader.NamespaceURI));
        }

        public MemoryVariableSize GetVariableSize()
        {
            return Size;
        }

        public void SetVariableSize(MemoryVariableSize size)
        {
            Size = size;
        }
    }
}
