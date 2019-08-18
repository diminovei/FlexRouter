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
        public override bool IsEqualTo(object obj)
        {
            if (!(obj is MemoryPatchVariable))
                return false;
            var varToCompare = obj as MemoryPatchVariable;
            return Offset == varToCompare.Offset && string.Equals(ModuleName, varToCompare.ModuleName);
        }

        public uint Offset;
        public string ModuleName;
        public string NameInMapFile;
        public override void SaveAdditionals(XmlTextWriter writer)
        {
            writer.WriteAttributeString("Offset", Offset.ToString("X"));
            writer.WriteAttributeString("Size", Size.ToString());
            writer.WriteAttributeString("ModuleName", ModuleName);
            writer.WriteAttributeString("NameInMapFile", NameInMapFile);
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            Offset = uint.Parse(reader.GetAttribute("Offset", reader.NamespaceURI), NumberStyles.HexNumber);
            Size = (MemoryVariableSize)Enum.Parse(typeof(MemoryVariableSize), reader.GetAttribute("Size", reader.NamespaceURI));
            ModuleName = reader.GetAttribute("ModuleName", reader.NamespaceURI);
            NameInMapFile = reader.GetAttribute("NameInMapFile", reader.NamespaceURI);
        }
    }
}
