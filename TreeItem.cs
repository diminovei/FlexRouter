namespace FlexRouter
{
    class TreeItem
    {
        /// <summary>
        /// Тип объекта
        /// </summary>
        public TreeItemType Type;
        public string Name;
        public string FullName;
        /// <summary>
        /// Объект, информацию о котором отображает дерево (Panel, AccessDescriptor, Variable)
        /// </summary>
        public object Object;
    }
}