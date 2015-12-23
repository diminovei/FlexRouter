using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.AssignedHardware
{
    public interface IAssignment
    {
        Connector GetConnector();
        void SetConnector(Connector connector);
        string GetAssignedHardware();
        void SetAssignedHardware(string assignedHardware);
        bool GetInverseState();
        void SetInverseState(bool inverseState);
    }
}
