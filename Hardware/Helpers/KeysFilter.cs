using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;

namespace FlexRouter.Hardware.Helpers
{
    class KeysFilter
    {
        private readonly Dictionary<string, bool> _previousButtonsState = new Dictionary<string, bool>();
        private readonly object _syncRoot = new object();
        public bool IsNeedToProcessControlEvent(ButtonEvent controlEvent)
        {
            lock (_syncRoot)
            {
                var key = controlEvent.Hardware.GetHardwareGuid();
                if (_previousButtonsState.ContainsKey(key) && _previousButtonsState[key] == controlEvent.IsPressed)
                    return false;
                _previousButtonsState[key] = controlEvent.IsPressed;
                return true;
            }
        }

        public void Reset()
        {
            lock (_syncRoot)
            {
                _previousButtonsState.Clear();
            }
        }
    }
}
