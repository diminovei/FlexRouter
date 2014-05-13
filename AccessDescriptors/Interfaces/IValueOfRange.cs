using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexRouter.AccessDescriptors.Interfaces
{
    interface IValueOfRange
    {
        void SetValueOfRange(double valueToSet, double rangeMaxValue);
        void SetDefaultValue();
    }
}
