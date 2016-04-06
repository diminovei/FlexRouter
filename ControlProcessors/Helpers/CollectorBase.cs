using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// Класс нужен для ситуации: когда пропало или появилось питание. В таких случаях, нужно повторить последнее произошедшее событие для AccessDescriptor, в котором изменилось питание.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CollectorBase<T> : ControlProcessorBase<T> where T : class
    {
        private ControlEventBase _lastControlEvent;
        private bool _lastTimePowerWasOff;

        protected CollectorBase(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public void ProcessControlEvent(ControlEventBase controlEvent)
        {
            if (!IsControlEventSuitable(controlEvent))
                return;

            _lastControlEvent = controlEvent;
            if (!(AccessDescriptor as DescriptorBase).IsPowerOn())
            {
                _lastTimePowerWasOff = true;
                return;
            }
            OnNewControlEvent(controlEvent);
        }

        public void Tick()
        {
            if (IsNeedToRepeatControlEventOnPowerOn() && _lastTimePowerWasOff)
            {
                if ((AccessDescriptor as DescriptorBase).IsPowerOn())
                {
                    ProcessControlEvent(_lastControlEvent);
                    _lastTimePowerWasOff = false;
                    _lastControlEvent = null;
                }
            }
            OnTick();
        }

        protected abstract void OnNewControlEvent(ControlEventBase controlEvent);
        protected abstract void OnTick();
        protected abstract bool IsControlEventSuitable(ControlEventBase controlEvent);
        protected abstract bool IsNeedToRepeatControlEventOnPowerOn();
    }
}