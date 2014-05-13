namespace FlexRouter.AccessDescriptors.Interfaces
{
    interface IDescriptorPrevNext
    {
        void SetNextState(int repeats);
        void SetPreviousState(int repeats);
    }
}
