namespace FlexRouter.VariableWorkerLayer.MethodClick
{
    public class WindowInfo
    {
        public int Id;
        public string Name;
        public WindowInfo Clone()
        {
            return (WindowInfo)MemberwiseClone();
        }
    }
}