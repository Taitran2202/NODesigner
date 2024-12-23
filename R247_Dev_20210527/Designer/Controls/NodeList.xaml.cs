using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for NodeList.xaml
    /// </summary>
    public partial class NodeList : UserControl
    {
        //List<AddNodeFactory> AddNodeFunction;
        //public NodeListViewModel ViewModel { get; set; }
        List<AddNodeFactory> AddNodeFunction;
        public NodeList()
        {
            
            InitializeComponent();
        }
        public void SetAddNodeFactory(List<AddNodeFactory> AddNodeFunction)
        {           
            this.AddNodeFunction = AddNodeFunction;
            ICollectionView view = CollectionViewSource.GetDefaultView(AddNodeFunction);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("TypeNode"));
            view.SortDescriptions.Add(new SortDescription("TypeNode", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
       
            list.ItemsSource = view;
            CollectionView view_filter = (CollectionView)CollectionViewSource.GetDefaultView(list.ItemsSource);
            view_filter.Filter = UserFilter;
        }
        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(txtFilter.Text))
            {
                return true;
            }
            else
            {
                return ((item as AddNodeFactory).Type.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            
        }

        private void OnNodeMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AddNodeFactory nodeVM = ((FrameworkElement)sender).DataContext as AddNodeFactory;
                if (nodeVM == null)
                {
                    return;
                }
                var nodeFactory = AddNodeFunction.First(t => t.Type == nodeVM.Type).AddFunction;
                NodeViewModel newNodeVM = nodeFactory();

                DragDrop.DoDragDrop(this, new DataObject("nodeVM", newNodeVM), DragDropEffects.Copy);
            }
        }

        private void txtFilter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(list.ItemsSource).Refresh();
        }
    }
}
