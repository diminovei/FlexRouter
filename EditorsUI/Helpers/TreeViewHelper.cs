using System;
using System.Windows;
using System.Windows.Controls;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.Helpers
{
    class TreeViewHelper
    {
        private TreeViewItem _selectedTreeViewItem;

        public bool SelectionChanging(TreeView tree, RoutedPropertyChangedEventArgs<object> e, bool askIsNeedToSaveItemBeforeChangeTreeSelection)
        {
            if (_selectedTreeViewItem == null)
            {
                _selectedTreeViewItem = e.NewValue as TreeViewItem;
                return false;
            }
            if (Equals(e.NewValue, _selectedTreeViewItem))
            {
                // Will only end up here when reversing item
                // Without this line childs can't be selected
                // twice if "No" was pressed in the question..   
                tree.Focus();
                return true;
            }
            if (askIsNeedToSaveItemBeforeChangeTreeSelection && MessageBox.Show(LanguageManager.GetPhrase(Phrases.MessageBoxUnsavedEditorData), 
                                                                                LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader),
                                                                                MessageBoxButton.YesNo,
                                                                                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                EventHandler eventHandler = null;
                eventHandler = delegate
                    {
                        tree.LayoutUpdated -= eventHandler;
                        _selectedTreeViewItem.IsSelected = true;
                    };
                // Will be fired after SelectedItemChanged, to early to change back here
                tree.LayoutUpdated += eventHandler;
                return true;
            }
            _selectedTreeViewItem = e.NewValue as TreeViewItem;
            return false;
        }

/*        public bool IsNeedToSaveItemBeforeChangeTreeSelection(TreeView tree, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_selectedTreeViewItem != null)
            {
                if (e.NewValue == _selectedTreeViewItem)
                {
                    // Will only end up here when reversing item
                    // Without this line childs can't be selected
                    // twice if "No" was pressed in the question..   
                    tree.Focus();
                }
                else
                {
                    if (MessageBox.Show("Change TreeViewItem?",
                                        "Really change",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        EventHandler eventHandler = null;
                        eventHandler = new EventHandler(delegate
                            {
                                tree.LayoutUpdated -= eventHandler;
                                _selectedTreeViewItem.IsSelected = true;
                            });
                        // Will be fired after SelectedItemChanged, to early to change back here
                        tree.LayoutUpdated += eventHandler;
                        return true;
                    }
                    else
                    {
                        _selectedTreeViewItem = e.NewValue as TreeViewItem;
                    }
                }
            }
            else
            {
                _selectedTreeViewItem = e.NewValue as TreeViewItem;
            }
            return false;
        }*/

/*        private MyInterface m_selectedTreeViewItem = null;
        private void c_treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (m_selectedTreeViewItem != null)
            {
                if (e.NewValue == m_selectedTreeViewItem)
                {
                    // Will only end up here when reversing item
                    // Without this line childs can't be selected
                    // twice if "No" was pressed in the question..   
                    c_treeView.Focus();
                }
                else
                {
                    if (MessageBox.Show("Change TreeViewItem?",
                                        "Really change",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        EventHandler eventHandler = null;
                        eventHandler = new EventHandler(delegate
                        {
                            c_treeView.LayoutUpdated -= eventHandler;
                            m_selectedTreeViewItem.IsSelected = true;
                        });
                        // Will be fired after SelectedItemChanged, to early to change back here
                        c_treeView.LayoutUpdated += eventHandler;
                    }
                    else
                    {
                        m_selectedTreeViewItem = e.NewValue as MyInterface;
                    }
                }
            }
            else
            {
                m_selectedTreeViewItem = e.NewValue as MyInterface;
            }
        }*/
    }
}
