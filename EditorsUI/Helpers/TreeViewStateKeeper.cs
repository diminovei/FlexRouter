using System.Collections.Generic;
using System.Windows.Controls;

namespace FlexRouter.EditorsUI.Helpers
{
    class TreeViewStateKeeper
    {
        private readonly List<string> _expandedNodeNames = new List<string>();
        private void Clear()
        {
            _expandedNodeNames.Clear();
        }
        public void RememberState(ref TreeView tree)
        {
            Clear();
            RememberExpandedNodesSub(ref tree, null);
        }
        private void RememberExpandedNodesSub(ref TreeView tree, TreeViewItem treeNode)
        {
            var nodesToProcess = treeNode == null ? tree.Items : treeNode.Items;
            foreach (TreeViewItem node in nodesToProcess)
            {
                if (node.IsExpanded)
                    _expandedNodeNames.Add(node.Header+"|" +node.Name);
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
                if (_expandedNodeNames.Contains(node.Header+"|" +node.Name))
                    node.IsExpanded = true;
                if (node.Items.Count != 0)
                    RestoreExpandedNodesSub(ref tree, node);
            }
        }
    }
}
