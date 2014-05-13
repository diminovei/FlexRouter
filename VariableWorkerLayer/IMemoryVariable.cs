using FlexRouter.VariableSynchronization;

namespace FlexRouter.VariableWorkerLayer
{
    public interface IMemoryVariable
    {
        MemoryVariableSize GetVariableSize();
        void SetVariableSize(MemoryVariableSize size);
    }
}
