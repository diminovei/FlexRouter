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
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderFsuipc);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.FsuipcVariable;
        }

        //public bool IsInitialized = false;
        public int Offset;
        //private MemoryVariableSize _size;
        //public double ValueToSet;
        //public double ValueInMemory;
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

        //public MemoryVariableSize GetVariableSize()
        //{
        //    return _size;
        //}

        //public void SetVariableSize(MemoryVariableSize size)
        //{
        //    _size = size;
        //}
    }
}
