using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.AccessDescriptors.Interfaces
{
    public interface IAccessDescriptor : IITemWithId
    {
        string GetDescriptorType();
        DescriptorBase GetCopy();
    }
}
