namespace FlexRouter.AccessDescriptors.Helpers
{
    public interface IAccessDescriptor
    {
        string GetDescriptorType();
        DescriptorBase GetCopy();
    }
}
