using System;

namespace FlexRouter.Helpers
{
    static class GlobalId
    {
        static public Guid GetNew()
        {
            return Guid.NewGuid();
        }
    }
}
