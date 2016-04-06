using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlexRouter.EditorsUI.Helpers
{
    class SelectDataGridRow
    {
        //public static void SelectRowByIndex(DataGrid dataGrid, int rowIndex, int columnIndex)
        //{
        //    for (var i = 0; i < dataGrid.Items.Count; i++)
        //    {
        //        var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(i);
        //        var cellContent = dataGrid.Columns[columnIndex].GetCellContent(row) as TextBlock;
        //        if (cellContent != null /*&& cellContent.Text.Equals(textBox1.Text)*/)
        //        {
        //            object item = dataGrid.Items[i];
        //            dataGrid.SelectedItem = item;
        //            dataGrid.ScrollIntoView(item);
        //            row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        //            break;
        //        }
        //    }            
        //}
        public static void SelectRowByIndex(DataGrid dataGrid, int rowIndex)
        {
            if (!dataGrid.SelectionUnit.Equals(DataGridSelectionUnit.FullRow))
                throw new ArgumentException("The SelectionUnit of the DataGrid must be set to FullRow.");

            if (rowIndex < 0 || rowIndex > (dataGrid.Items.Count - 1))
                throw new ArgumentException(string.Format("{0} is an invalid row index.", rowIndex));

            dataGrid.SelectedItems.Clear();
            /* set the SelectedItem property */
            object item = dataGrid.Items[rowIndex]; // = Product X
            dataGrid.SelectedItem = item;

            var row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row == null)
            {
                /* bring the data item (Product object) into view
                 * in case it has been virtualized away */
                dataGrid.ScrollIntoView(item);
                row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;

            }
            if(row != null)
                row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            //TODO: Retrieve and focus a DataGridCell object
        }

    }
}
