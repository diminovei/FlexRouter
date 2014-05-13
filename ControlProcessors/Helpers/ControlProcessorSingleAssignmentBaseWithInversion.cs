using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors.Helpers;

namespace FlexRouter.ControlProcessors
{
    abstract class ControlProcessorSingleAssignmentBaseWithInversion<T> : ControlProcessorSingleAssignmentBase<T> where T : class
    {
        protected ControlProcessorSingleAssignmentBaseWithInversion(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public void SetInversion(bool invert)
        {
            Invert = invert;
        }
        public bool GetInversion()
        {
            return Invert;
        }
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteAttributeString("Invert", Invert.ToString());
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            Invert = bool.Parse(reader.GetAttribute("Invert", reader.NamespaceURI));
        }
    }
}
