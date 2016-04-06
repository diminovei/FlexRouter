using System;

namespace FlexRouter
{
    public interface IITemWithId
    {
        Guid GetId();
        void SetId(Guid id);
    }
}
