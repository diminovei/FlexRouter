using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Localizers;

namespace FlexRouter.ControlProcessors
{
    // Event. Pos, Min, Max. - какое число Min/Max может дать Event
    // MinMax - какими числами мы оперируем (приводим Event к ним)
    // MinMax - искусственное ограничение.
    internal class AxisRangeProcessor : CollectorBase <IDescriptorRangeExt>, ICollector
    {
        /// <summary>
        /// Значение, искусственно ограничивающее движение бегунка в меньшую сторону (нужен не весь ход потенциометра)
        /// </summary>
        private int _axisMinimum;
        /// <summary>
        /// Значение, искусственно ограничивающее движение бегунка в большую сторону (нужен не весь ход потенциометра)
        /// </summary>
        private int _axisMaximum;

        public AxisRangeProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        protected override Type GetAssignmentsType()
        {
            return typeof (Assignment);
        } 
        public override bool HasInvertMode()
        {
            return true;
        }
        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareAxis);
        }
        /// <summary>
        /// Сохранить дополнительные параметры
        /// </summary>
        /// <param name="writer">Стрим для записи XML</param>
        protected override void SaveAdditionals(XmlTextWriter writer)
        {
            base.SaveAdditionals(writer);
            writer.WriteStartElement("AxisSpecific");
            writer.WriteAttributeString("AxisMinimumLimit", _axisMinimum.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("AxisMaximumLimit", _axisMaximum.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }
        /// <summary>
        /// Загрузить дополнительные параметры
        /// </summary>
        /// <param name="reader">Итератор узла XML</param>
        public override void LoadAdditionals(XPathNavigator reader)
        {
            base.LoadAdditionals(reader);
            var readerAdd = reader.SelectSingleNode("AxisSpecific");
            if (readerAdd == null)
                return;
            int.TryParse(readerAdd.GetAttribute("AxisMinimumLimit", readerAdd.NamespaceURI), NumberStyles.Number, CultureInfo.InvariantCulture, out _axisMinimum);
            int.TryParse(readerAdd.GetAttribute("AxisMaximumLimit", readerAdd.NamespaceURI), NumberStyles.Number, CultureInfo.InvariantCulture, out _axisMaximum);
        }

        
        protected override void OnNewControlEvent(ControlEventBase controlEvent)
        {
            var relativePosition = CalculateRelativeAxisPosition(AxisDefaultRange.GetAxisDefaultMinimum(), AxisDefaultRange.GetAxisDefaultMaximum(), controlEvent);
            var positionPercentage = GetPercent(relativePosition, _axisMinimum, _axisMaximum);
            AccessDescriptor.SetPositionInPercents(positionPercentage);
        }

        protected override void OnTick()
        {
        }

        protected override bool IsControlEventSuitable(ControlEventBase controlEvent)
        {
            var ev = controlEvent as AxisEvent;
            if (ev == null)
                return false;

            if (controlEvent.Hardware.GetHardwareGuid() != Connections[0].GetAssignedHardware())
                return false;

            return true;
        }

        protected override bool IsNeedToRepeatControlEventOnPowerOn()
        {
            return true;
        }

        public double GetPercent(double position, int minimum, int maximum)
        {
            if (position < minimum)
                return 0;
            if (position > maximum)
                return 100;
            var range = maximum - minimum;
            var relativePosition = position - minimum;
            return (double)100 / range * relativePosition;
        }

        public int CalculateRelativeAxisPosition(int minimum, int maximum, ControlEventBase controlEvent)
        {
            var ev = (AxisEvent)controlEvent;

            double relativeEventPosition = ev.Position;
            if (ev.Position > ev.MaximumValue)
                relativeEventPosition = ev.MaximumValue;
            if (ev.Position < ev.MinimumValue)
                relativeEventPosition = ev.MinimumValue;
            relativeEventPosition = relativeEventPosition - ev.MinimumValue;
            var relativeEventRange = ev.MaximumValue - ev.MinimumValue;
            
            var relativeResultRange = maximum - minimum;

            var relativeResultPosition = (int)(relativeResultRange / relativeEventRange * relativeEventPosition);

            return relativeResultPosition;
        }

        public int GetAxisMinimum()
        {
            return _axisMinimum;
        }
        public int GetAxisMaximum()
        {
            return _axisMaximum;
        }
        public bool SetAxisRangeMinimum(int value)
        {
            if (value < 0 || value > AxisDefaultRange.GetAxisDefaultMaximum() - 1)
                return false;
            _axisMinimum = value;
            return true;
        }
        public bool SetAxisRangeMaximum(int value)
        {
            if (value < 0 + 1 || value > AxisDefaultRange.GetAxisDefaultMaximum())
                return false;
            _axisMaximum = value;
            return true;
        }

        public void SetAxisRangeDefaults()
        {
            _axisMaximum = AxisDefaultRange.GetAxisDefaultMaximum();
            _axisMinimum = AxisDefaultRange.GetAxisDefaultMinimum();
        }
    }
}
