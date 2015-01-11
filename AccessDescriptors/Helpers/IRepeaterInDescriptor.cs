using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexRouter.AccessDescriptors.Helpers
{
    interface IRepeaterInDescriptor
    {
        bool IsRepeaterOn();
        void EnableRepeater(bool enable);
    }
}
