using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.AccessDescriptors.Interfaces
{
    public interface IAccessDescriptor
    {
        string GetDescriptorType();
        DescriptorBase GetCopy();
    }
}
