using System.Collections.Generic;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.ControlProcessors
{
    class SteppingMotorProcessor : ControlProcessorSingleAssignmentBaseWithInversion<IDescriptorRangeExt>, IVisualizer
    {
        private double? _previousPosition;

        public SteppingMotorProcessor(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }
        public override string GetName()
        {
            return LanguageManager.GetPhrase(Phrases.HardwareIndicator);
        }

        public IEnumerable<ControlEventBase> GetNewEvent()
        {
            if (string.IsNullOrEmpty(AssignedHardwareForSingle))
                return null;

            var ad = Profile.GetAccessDesciptorById(AssignedAccessDescriptorId);
            var position = ((DescriptorIndicator)ad).GetIndicatorValue();
            if (position == null || position == _previousPosition)
                return null;
            _previousPosition = position;
            var ev = new SteppingMotorEvent
            {
                Hardware = ControlProcessorHardware.GenerateByGuid(AssignedHardwareForSingle),
                Position = (short) CalculateStepperPosition((short) position)
            };
            return new List<ControlEventBase> { ev };
        }

        public int CalculateStepperPosition(short positionToSet)
        {
            return 0;
        }

        public IEnumerable<ControlEventBase> GetClearEvent()
        {
            //ToDo: сохранить позицию шагового двигателя
            return new List<ControlEventBase>();
        }
        // ToDo: временно, пока не разобрался, куда это девать. Это минимальные и максимальные значения, которые могут быть переданы AccessDescriptor на двигатель
        private int _minimumValue = 0;
        private int _maximumValue = 500;
        /// <summary>
        /// Сколько значений на один оборот
        /// </summary>
        private int _turnRange = 300;
        /// <summary>
        /// Тип стрелочного прибора
        /// </summary>
        public enum PointerInstrumentType
        {
            ///<summary>
            ///Выключен
            ///</summary>
            None,           
            ///<summary>
            ///Не полный оборот - Амперметр (возврат через 0 не возможнен)
            ///</summary>
            IncompleteTurn,
            ///<summary>
            ///Полный оборот с возвратом через 0 (компас)
            ///</summary>
            FullTurn,
            ///<summary>
            ///Многооборотный (альтиметр)
            ///</summary>
            MultiTurn
        }

        /// <summary>
        /// Тип стрелочного прибора
        /// </summary>
        private PointerInstrumentType _pointerInstrumentType = PointerInstrumentType.IncompleteTurn;



        /// <summary>
        /// Значение, искусственно ограничивающее движение ротора двигателя в меньшую сторону (нужен не весь ход)
        /// </summary>
        private int _minimumStepperPosition = 0;
        /// <summary>
        /// Значение, от которого начинается отсчёт нуля (в случае, если прибор имеет отрицательные и положительные значения. 
        /// Если только положительные, то _beginStepperPosition = _minimumStepperPosition)
        /// </summary>
        private int _beginStepperPosition = 0;
        /// <summary>
        /// Значение, искусственно ограничивающее движение ротора двигателя в большую сторону (нужен не весь ход)
        /// </summary>
        private int _maximumStepperPosition = 1024;
        /// <summary>
        /// Сколько шагов нужно сделать, чтобы совершить полный оборот
        /// </summary>
        private int _stepsPerTurn = 1024;

        public int GetStepsPerTurn()
        {
            return _stepsPerTurn;
        }
        public int GetBeginStepperPosition()
        {
            return _beginStepperPosition;
        }
        public int GetMinimumStepperPosition()
        {
            return _minimumStepperPosition;
        }
        public int GetMaximumStepperPosition()
        {
            return _maximumStepperPosition;
        }
        public void SetMinimumStepperPosition(int value)
        {
            _minimumStepperPosition = value;
        }
        public void SetMaximumStepperPosition(int value)
        {
            _maximumStepperPosition = value;
        }
        public void SetStepsPerTurn(int value)
        {
            _stepsPerTurn = value;
        }
        public void SetBeginStepperPosition(int value)
        {
            _beginStepperPosition = value;
        }
    }
}
