using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Windows.HelperWindows
{
    /// <summary>
    /// Interaction logic for TagManagerWindow.xaml
    /// </summary>
    public partial class TagManagerWindow : ThemedWindow
    {
        public TagManagerWindow(TagManagerModel model)
        {
            InitializeComponent();
            gridControl.ItemsSource = model.TagList;
        }

        private void btn_expander_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void view_CanSelectRow(object sender, DevExpress.Xpf.Grid.CanSelectRowEventArgs e)
        {

        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            view.AddNewRow();
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var index = view.FocusedRowHandle;
            view.DeleteRow(view.FocusedRowHandle);
            if (index > 0)
            {
                view.FocusedRowHandle = index-1;
            }
            else
            {
                view.FocusedRowHandle = 0;
            }
        }
    }
}
