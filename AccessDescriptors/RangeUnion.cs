using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors
{
    public class RangeUnion : DescriptorBase, IDescriptorPrevNext, IDescriptorRangeExt, IRepeaterInDescriptor
    {
        readonly List<DescriptorRange> _dependentDescriptors = new List<DescriptorRange>();

        public override Connector[] GetConnectors(object controlProcessor)
        {
            var connectors = new List<Connector>();
            if (controlProcessor is EncoderProcessor || controlProcessor is ButtonPlusMinusProcessor)
            {
                var c = new Connector { Id = 0, Name = "*", Order = 0 };
                connectors.Add(c);
                return connectors.ToArray();
            }
            throw new Exception(string.Format("ControlProcessor типа '{0}' не может быть назначен на AccessDescriptor типа '{1}'", controlProcessor.GetType(), this.GetType()));
        }

        private int GetDependentDescriptorIndex(DescriptorRange descriptor)
        {
            for (var i = 0; i < _dependentDescriptors.Count; i++)
            {
                if (descriptor.GetId() == _dependentDescriptors[i].GetId())
                    return i;
            }
            return -1;
        }
        public void ClearDependentDescriptorList()
        {
            foreach (var dependentDescriptor in _dependentDescriptors)
                dependentDescriptor.ResetDependency();
            _dependentDescriptors.Clear();
        }

        public void AddDependentDescriptor(DescriptorRange descriptor)
        {
            if (GetDependentDescriptorIndex(descriptor) != -1)
                return;
            _dependentDescriptors.Add(descriptor);
            descriptor.SetDependency(this);
        }
        public void RemoveDependentDescriptor(DescriptorRange descriptor)
        {
            var index = GetDependentDescriptorIndex(descriptor);
            if (index == -1)
                return;
            _dependentDescriptors.ElementAt(index).ResetDependency();
            _dependentDescriptors.RemoveAt(index);
        }

        public DescriptorRange[] GetDependentDescriptorsList()
        {
            return _dependentDescriptors.ToArray();
        }

        public void SetNextState(int repeats)
        {
            foreach (var d in _dependentDescriptors)
                d.SetNextState(repeats);
        }

        public void SetPreviousState(int repeats)
        {
            foreach (var d in _dependentDescriptors)
                d.SetPreviousState(repeats);
        }

        public void SetPositionInPercents(double positionPercentage)
        {
            foreach (var d in _dependentDescriptors)
                d.SetPositionInPercents(positionPercentage);
        }
        
        public override string GetDescriptorType()
        {
            return LanguageManager.GetPhrase(Phrases.EditorTypeRangeUnion);
        }

        public override Bitmap GetIcon()
        {
            return Properties.Resources.RangeUnion;
        }

        private readonly List<int> _loadedDependentDescriptors = new List<int>();
        public override void Initialize()
        {
            base.Initialize();
            foreach (var ld in _loadedDependentDescriptors)
            {
                var ad = Profile.GetAccessDesciptorById(ld);
                if(!(ad is DescriptorRange))
                    continue;
                _dependentDescriptors.Add((DescriptorRange) ad);
                ad.SetDependency(this);
            }
                
            _loadedDependentDescriptors.Clear();
        }
        public override void SaveAdditionals(XmlWriter writer)
        {
            writer.WriteStartElement("DependentAccessDescriptors");
            foreach (var dd in _dependentDescriptors)
            {
                writer.WriteStartElement("Descriptor");
                writer.WriteAttributeString("Id", dd.GetId().ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public override void LoadAdditionals(XPathNavigator reader)
        {
            var readerAdd = reader.Select("DependentAccessDescriptors/Descriptor");
            while (readerAdd.MoveNext())
            {
                var id = int.Parse(readerAdd.Current.GetAttribute("Id", readerAdd.Current.NamespaceURI));
                _loadedDependentDescriptors.Add(id);
            }
        }
        /// <summary>
        /// Включен ли покторитель
        /// (в этом дексрипторе должен быть всегда включен)
        /// </summary>
        /// <returns>true - включен</returns>
        public bool IsRepeaterOn()
        {
            return true;
        }
        /// <summary>
        /// Включить/выключить повторитель
        /// (в этом дексрипторе должен быть всегда включен)
        /// </summary>
        public void EnableRepeater(bool enable)
        {
            throw new NotImplementedException("This access descriptor repeater must be unmanaged and always on. Check your code");
        }
    }
}
