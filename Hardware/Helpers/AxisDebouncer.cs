using System;
using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    class AxisDebouncer
    {
        /// <summary>
        /// Последние состояния осей. Используется для подавления помех (фильтрация мелкого дребезга)
        /// </summary>
        private readonly Dictionary<string, ushort> _previousAxisValues = new Dictionary<string, ushort>();
        private ControlProcessorHardware _currentControlProcessorHardware = new ControlProcessorHardware();
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Следует ли обрабатывать событие для визуализации и назначения оси
        /// </summary>
        /// <param name="controlEvent">событие</param>
        /// <param name="thresholdPercentage">порог, после которого наступает "захват" оси. После чего её показания можно демонстрировать</param>
        /// <returns>true - обрабатывать, false - пропустить</returns>
        public bool IsNeedToProcessAxisEvent(AxisEvent controlEvent, double thresholdPercentage)
        {
            lock (_syncRoot)
            {
                // Логика ниже работает следующим образом: для того, чтобы интерфейс работал с конкретной осью, а не со всеми сразу (из-за дребезга)
                // ось сначала нужно "захватить", то есть передвинуть на 1/10 хода. 
                // После этого отображаются данные только этой оси до следующего "захвата" интерфейса другой осью
                var key = controlEvent.Hardware.GetHardwareGuid();
                if (!_previousAxisValues.ContainsKey(key))
                {
                    _previousAxisValues.Add(key, controlEvent.Position);
                    // Здесь возвращается true для того, чтобы оси можно было сдампить. То есть, в первый раз дребезг не учитывается.
                    return true;
                }

                var threshold = Math.Abs(controlEvent.MaximumValue - controlEvent.MinimumValue) / 100 * thresholdPercentage;
                if (controlEvent.Position > _previousAxisValues[key] + threshold || controlEvent.Position < _previousAxisValues[key] - threshold)
                {
                    _currentControlProcessorHardware = controlEvent.Hardware;
                    _previousAxisValues[key] = controlEvent.Position;
                }
                return controlEvent.Hardware.Equals(_currentControlProcessorHardware);
            }
        }
        /// <summary>
        /// Сброс "захвата" оси
        /// </summary>
        public void Reset()
        {
            lock (_syncRoot)
            {
                _currentControlProcessorHardware = new ControlProcessorHardware();
                _previousAxisValues.Clear();
            }
        }
    }
}
