namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// ��������� ��������� �������� ���������� ��� ��������� �������� ���������� � ������
    /// </summary>
    public struct ManageMemoryVariableResult
    {
        public MemoryPatchVariableErrorCode Code;  // ��� ������
        public string ErrorMessage;                 // ����� ������/����������
        public double Value;                        // ���������/������������� ��������
    }
}