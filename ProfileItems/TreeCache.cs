using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using FlexRouter.Helpers;

namespace FlexRouter.ProfileItems
{

    public class ItemTag
    {
        public Guid Id;
        public TreeItemType ItemType;
    }

    public class Item
    {
        public TreeViewItem TreeItem;
        public ItemTag Tag;

    }
    public class TreeCache
    {
        private static System.Drawing.Bitmap GetIcon(TreeItemType tit, Guid itemId)
        {
            if (tit == TreeItemType.ControlProcessor)
            {
                var cp = Profile.ControlProcessor.GetControlProcessor(itemId);
                if (cp == null)
                    return Properties.Resources.ConnectedNot;
                var assignments = cp.GetAssignments();
                bool foundUnassigned = false;
                foreach (var assignment in assignments)
                {
                    if (string.IsNullOrEmpty(assignment.GetAssignedHardware()))
                        foundUnassigned = true;
                }
                return foundUnassigned ? Properties.Resources.ConnectedPartially : Properties.Resources.Connected;
            }
            if (tit == TreeItemType.Variable)
            {
                var variable = Profile.VariableStorage.GetVariableById(itemId);
                return ((ITreeItem)variable).GetIcon();
            }
            if (tit == TreeItemType.AccessDescriptor)
            {
                var descriptor = Profile.AccessDescriptor.GetAccessDesciptorById(itemId);
                return descriptor.GetIcon();
            }
            return null;
        }
        private static TreeViewItem CreateTreeViewItem(string text, object connectedObject, TreeItemType treeItemType, System.Drawing.Bitmap iconBitmap)
        {
            var item = new TreeViewItem();

            if (iconBitmap != null)
            {
                // create stack panel
                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                // create Image
                var image = new Image();
                var bc = new WpfBitmapConverter();
                var icon = (ImageSource)bc.Convert(iconBitmap, typeof(ImageSource), null, CultureInfo.InvariantCulture);

                image.Source = icon;
                image.Width = 16;
                image.Height = 16;
                // Label
                var lbl = new Label { Content = text };

                // Add into stack
                stack.Children.Add(image);
                stack.Children.Add(lbl);

                // assign stack to header
                item.Header = stack;
            }
            else
                item.Header = text;
            item.Name = treeItemType.ToString();
            item.Tag = connectedObject;
            return item;
        }

    }
}
