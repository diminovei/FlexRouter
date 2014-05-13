using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    abstract class ControlProcessorSingleAssignmentBase<T> : ControlProcessorBase<T> where T : class
    {
        protected bool Invert = false;

        protected ControlProcessorSingleAssignmentBase(DescriptorBase accessDescriptor) : base(accessDescriptor)
        {
        }

        public void AssignHardware(string hardwareGuid)
        {
            AssignedHardwareForSingle = hardwareGuid;
        }

        public override Assignment[] GetAssignments()
        {
            var assignmentList = new Assignment[1];

            var a = new Assignment
            {
                StateId = 0,
                StateName = "*",
                Inverse = Invert,
                AssignedItem = AssignedHardwareForSingle
            };
            assignmentList[0] = a;
            return assignmentList;
        }

        public override void SetAssignment(Assignment assignment)
        {
            AssignedHardwareForSingle = assignment.AssignedItem;
            Invert = assignment.Inverse;
        }
    }
}
