using System;
using System.Drawing;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.Localizers;

namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    public class MemoryPatchVariable : MemoryVariableBase, IMemoryVariable
    {
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.EditorHeaderMemoryPatch);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.MemoryVariable;
        }

//        public bool IsValueToSetInitialized = false;
        public uint Offset;
        public string ModuleName;
//        private MemoryVariableSize _size;
//        public double ValueToSet;
//        public double ValueInMemory;
        public override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Offset", Offset.ToString("X"));
            writer.WriteAttributeString("Size", Size.ToString());
            writer.WriteAttributeString("ModuleName", ModuleName);
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            Offset = uint.Parse(reader.GetAttribute("Offset", reader.NamespaceURI), NumberStyles.HexNumber);
            Size = (MemoryVariableSize)Enum.Parse(typeof(MemoryVariableSize), reader.GetAttribute("Size", reader.NamespaceURI));
            ModuleName = reader.GetAttribute("ModuleName", reader.NamespaceURI);
        }

/*        public MemoryVariableSize GetVariableSize()
        {
            return _size;
        }

        public void SetVariableSize(MemoryVariableSize size)
        {
            _size = size;
        }*/
    }
}
