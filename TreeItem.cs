namespace FlexRouter
{
    class TreeItem
    {
        /// <summary>
        /// ��� �������
        /// </summary>
        public TreeItemType Type;
        public string Name;
        public string FullName;
        /// <summary>
        /// ������, ���������� � ������� ���������� ������ (Panel, AccessDescriptor, Variable)
        /// </summary>
        public object Object;
    }
}