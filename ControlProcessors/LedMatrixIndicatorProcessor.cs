using System;
using System.Collections.Generic;
using System.Linq;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.AssignedHardware;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.ControlProcessors
{
    class LedMatrixIndicatorProcessor : ControlProcessorBase<IIndicatorMethods>, IVisualizer
    {
        /// <summary>
        /// Здесь запоминаем предыдущее значение, выведенное на индикатор, чтобы не повторять вывод одного и того же значения много раз
        /// </summary>
        private string _previousIndicatorText;
        public LedMatrixIndicatorProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
            FillSymbolLightedSegments();
            LoadSegmentToPinMapping();
        }
        /// <summary>
        /// Сегменты, которые нужно зажигать при показе символа
        /// </summary>
        private readonly Dictionary<char, char[]> _symbolLightedSegments = new Dictionary<char, char[]>();
        private readonly Dictionary<char, int> _segmentToPinMapping = new Dictionary<char, int>();
        private void FillSymbolLightedSegments()
        {
            _symbolLightedSegments.Add('0', new[] { 'A', 'B', 'C', 'D', 'E', 'F' });
            _symbolLightedSegments.Add('1', new[] { 'B', 'C' });
            _symbolLightedSegments.Add('2', new[] { 'A', 'B', 'G', 'E', 'D' });
            _symbolLightedSegments.Add('3', new[] { 'A', 'B', 'G', 'C', 'D' });
            _symbolLightedSegments.Add('4', new[] { 'F', 'G', 'B', 'C' });
            _symbolLightedSegments.Add('5', new[] { 'A', 'F', 'G', 'C', 'D' });
            _symbolLightedSegments.Add('6', new[] { 'A', 'F', 'E', 'D', 'C', 'G' });
            _symbolLightedSegments.Add('7', new[] { 'A', 'B', 'C' });
            _symbolLightedSegments.Add('8', new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' });
            _symbolLightedSegments.Add('9', new[] { 'A', 'B', 'C', 'D', 'F', 'G' });
            _symbolLightedSegments.Add(' ', new char[0] );
            _symbolLightedSegments.Add('F', new[] { 'A', 'B', 'G', 'C' });
            _symbolLightedSegments.Add('D', new[] { 'A', 'B', 'C', 'D', 'E', 'F' });
            _symbolLightedSegments.Add('C', new[] { 'A', 'F', 'E', 'D' });
            _symbolLightedSegments.Add('-', new[] { 'G' });
            _symbolLightedSegments.Add('_', new[] { 'D' });
            _symbolLightedSegments.Add('E', new[] { 'A', 'F', 'G', 'E', 'D' });
            _symbolLightedSegments.Add('r', new[] { 'G', 'E' });
            _symbolLightedSegments.Add('o', new[] { 'G', 'E', 'D', 'C' });

            //  ,--A--,     ,--0--,
            //  F-----B     5-----1
            //  |--G--|     |--6--|
            //  E-----C     4-----2
            //  '--D--'-P   '--3--'-7
        }

        private void LoadSegmentToPinMapping()
        {
            //    //  ,--A--,     ,--0--,
            //    //  F-----B     5-----1
            //    //  |--G--|     |--6--|
            //    //  E-----C     4-----2
            //    //  '--D--'-P   '--3--'-7

//            const string setting = "A-5,B-4,C-2,D-1,E-0,F-7,G-6,P-3";
            //const string setting = "A-0,B-1,C-2,D-3,E-4,F-5,G-6,P-7";
            //var segmentToPinPair = setting.ToUpper().Split(',');
            var segmentToPinPair = Properties.Settings.Default.F3IndicatorSegmentsToPin.ToUpper().Split(',');
            foreach (var p in segmentToPinPair)
            {
                var segmentToPin = p.Split('-');
                _segmentToPinMapping.Add(char.Parse(segmentToPin[0]), int.Parse(segmentToPin[1]));
            }
        }
        public override bool HasInvertMode()
        {
            return false;
        }
        protected override Type GetAssignmentsType()
        {
            return typeof(Assignment);
        } 
        public override string GetDescription()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareLedMatrixIndicator);
        }
        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return null;
            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var text = ((IIndicatorMethods) ad).GetIndicatorText();
            if (text == _previousIndicatorText)
                return null;
            _previousIndicatorText = text;
            var ev = TextToControlEvents(text);
            return ev;
        }
        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            if (string.IsNullOrEmpty(Connections[0].GetAssignedHardware()))
                return null;
            var eventArray = new List<ControlEventBase>();

            var digitsNumber = ((DescriptorIndicator)AccessDescriptor).GetNumberOfDigits();
            var elementInDigitCounter = 0;
            var digitsCounter = 0;
            var hw = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware());
            var currentBlockId = hw.BlockId;
            var currentControlId = hw.ControlId;
            while (digitsCounter < digitsNumber)
            {
                var hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware());
                if (currentControlId == 16)
                {
                    currentBlockId++;
                    currentControlId = 0;
                }

                hardware.ControlId = currentControlId;
                hardware.BlockId = currentBlockId;
                var ev = new LampEvent
                {
                    Hardware = hardware,
                    IsOn = false
                };
                eventArray.Add(ev);
                currentControlId++;
                elementInDigitCounter++;
                if (elementInDigitCounter == 8)
                {
                    elementInDigitCounter = 0;
                    digitsCounter++;
                }
            }
            // Требуется для того, чтобы при изменении, например, числа цифр в индикаторе не оставались гореть цифры
            _previousIndicatorText = "";
            return eventArray;
        }
        private IEnumerable<ControlEventBase> TextToControlEvents(string text)
        {
            var eventArray = new List<ControlEventBase>();
            uint symbolNumber = 1;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '.' || text[i] == ',')
                    continue;

                var withPoint = i + 1 < text.Length && (text[i + 1] == '.' || text[i + 1] == ',');
                var evs = LetterToControlEvents(symbolNumber, text[i], withPoint);
                symbolNumber++;
                eventArray.AddRange(evs);
            }
            return eventArray;
        }
        private IEnumerable<ControlEventBase> LetterToControlEvents(uint digitNumber, char symbol, bool addPoint)
        {
            var eventArray = new List<LampEvent>();
            var symbolSegments = _symbolLightedSegments[symbol];
            foreach (var i in _segmentToPinMapping)
            {
                var ev = new LampEvent
                {
                    Hardware = ControlProcessorHardware.GenerateByGuid(Connections[0].GetAssignedHardware()),
                    IsOn = symbolSegments.Contains(i.Key) || (addPoint && i.Key == 'P')
                };
                // Note: Только для железа, где блок состоит из 16 пинов (F3)
                uint addToBlock = 0;
                uint addToControlId = 0;
                if (ev.Hardware.ControlId == 0)
                {
                    // 12 34 56 78 - номер символа
                    // 00 11 22 33 - добавить к блоку

                    // Если нечётное
                    if (digitNumber%2 == 1)
                    {
                        addToBlock = (digitNumber + 1) / 2 - 1;
                        addToControlId = 0;
                    }
                    else
                    {
                        addToBlock = digitNumber / 2 - 1;
                        addToControlId = 8;
                    }
                }
                if (ev.Hardware.ControlId == 8)
                {
                    // 1 23 45 67
                    // 0 11 22 33

                    // Если нечётное
                    if (digitNumber%2 == 1)
                    {
                        addToBlock = (digitNumber + 1)/2 - 1;
                        addToControlId = 8;
                    }
                    else
                    {
                        addToBlock = digitNumber / 2;
                        addToControlId = 0;
                    }
                }
                ev.Hardware.BlockId += addToBlock;
                ev.Hardware.ControlId = (uint) (addToControlId + i.Value);

                eventArray.Add(ev);
            }
            return eventArray;
        }
    }
}
