using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FlexRouter.EditorPanels
{
    class SelecedRowAndColumn
    {
        private int selectedRowIndex = -1;
        private int selectedCellIndex = -1;
        public int GetSelectedRowIndex()
        {
            return selectedRowIndex;
        }
        public int GetSelectedCellIndex()
        {
            return selectedCellIndex;
        }
        public void OnMouseDoubleClick(DependencyObject dep)
        {
            FindSelectedRowAndColumn(dep);
        }
        private void FindSelectedRowAndColumn(DependencyObject dep)
        {
            //Stepping through the visual tree
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            //Is the dep a cell or outside the bounds of Window1?
            if (dep == null | !(dep is DataGridCell))
                return;
            var cell = (DataGridCell)dep;
            while ((dep != null) && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep == null)
                return;

            var colindex = cell.Column.DisplayIndex; //this returns COLUMN INDEX

            var row = dep as DataGridRow;
            var dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
/*            if (dataGrid == null)
            {
                selectedCellIndex = -1;
                selectedRowIndex = -1;
                return;
            }*/
            var rowindex = dataGrid.ItemContainerGenerator.IndexFromContainer(row); //this returns ROW INDEX
            var value = ExtractBoundValue(row, cell);
            selectedCellIndex = colindex;
            selectedRowIndex = rowindex;
            //MessageBox.Show(value + "\tColIndex:" + colindex + "\tRowIndex:" + rowindex);
        }
        /// <summary>
        /// Find the value that is bound to a DataGridCell
        /// </summary>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private object ExtractBoundValue(DataGridRow row, DataGridCell cell)
        {
            // find the property that this cell's column is bound to
            string boundPropertyName = FindBoundProperty(cell.Column);

            // find the object that is realted to this row
            object data = row.Item;

            // extract the property value
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(data);
            PropertyDescriptor property = properties[boundPropertyName];
            object value = property.GetValue(data);

            return value;
        }

        /// <summary>
        /// Find the name of the property which is bound to the given column
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        private string FindBoundProperty(DataGridColumn col)
        {
            var boundColumn = col as DataGridBoundColumn;

            // find the property that this column is bound to
            var binding = boundColumn.Binding as Binding;
            var boundPropertyName = binding.Path.Path;

            return boundPropertyName;
        }
    }
}
