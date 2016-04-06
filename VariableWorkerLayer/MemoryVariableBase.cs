namespace FlexRouter.VariableWorkerLayer
{
    public abstract class MemoryVariableBase : VariableBase
    {
        public MemoryVariableSize Size;
        private double? _valueToSet;
        private double? _valueInMemory;

        public double? GetValueInMemory()
        {
            return _valueInMemory;
        }
        public void SetValueInMemory(double? value)
        {
            _valueInMemory = value;
        }
        public double? GetValueToSet()
        {
            return _valueToSet;
        }
        public void SetValueToSet(double? value)
        {
            _valueToSet = value;
        }
        public MemoryVariableSize GetVariableSize()
        {
            return Size;
        }
        public void SetVariableSize(MemoryVariableSize size)
        {
            Size = size;
        }
    }
}
