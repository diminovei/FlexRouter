namespace FlexRouter.AccessDescriptors.Interfaces
{
    interface IDefautValueAbility
    {
        void AssignDefaultStateId(int stateId);
        void UnAssignDefaultStateId();
        int GetDefaultStateId();
    }
}
