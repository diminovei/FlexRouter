using System.Collections.Generic;
using System.Windows.Controls;

namespace FlexRouter.EditorsUI.Helpers
{
    class TreeViewStateKeeper
    {
        private readonly List<string> _expandedNodeNames = new List<string>();
        private string _selectedNode; 
        private void Clear()
        {
            _expandedNodeNames.Clear();
        }
        public void RememberState(ref TreeView tree)
        {
            Clear();
            if (tree.SelectedItem != null)
            {
                var node = (TreeViewItem)tree.SelectedItem;
                _selectedNode = GetNodeIdentifier(node);
            }
            RememberExpandedNodesSub(ref tree, null);
        }
        private void RememberExpandedNodesSub(ref TreeView tree, TreeViewItem treeNode)
        {
            var nodesToProcess = treeNode == null ? tree.Items : treeNode.Items;
            foreach (TreeViewItem node in nodesToProcess)
            {
                if (node.IsExpanded)
                    _expandedNodeNames.Add(GetNodeIdentifier(node));
                if (node.Items.Count != 0)
                    RememberExpandedNodesSub(ref tree, node);
            }
        }
        public void RestoreState(ref TreeView tree)
        {
            RestoreExpandedNodesSub(ref tree, null);
        }
        private void RestoreExpandedNodesSub(ref TreeView tree, TreeViewItem treeNode)
        {
            var nodesToProcess = treeNode == null ? tree.Items : treeNode.Items;
            foreach (TreeViewItem node in nodesToProcess)
            {
                if (_expandedNodeNames.Contains(GetNodeIdentifier(node)))
                    node.IsExpanded = true;
                if (!string.IsNullOrEmpty(_selectedNode) && _selectedNode == GetNodeIdentifier(node))
                    node.IsSelected = true;
                if (node.Items.Count != 0)
                    RestoreExpandedNodesSub(ref tree, node);
            }
        }
        private string GetNodeIdentifier(TreeViewItem node)
        {
            return ((IITemWithId)node.Tag).GetId() + "|" + node.Name;
        }
    }
}
