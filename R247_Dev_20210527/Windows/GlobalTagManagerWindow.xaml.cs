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
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using NOVisionDesigner.ViewModel;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for GlobalTagManagerWindow.xaml
    /// </summary>
    public partial class GlobalTagManagerWindow : ThemedWindow
    {
        public GlobalTagManagerWindow()
        {
            InitializeComponent();
            gridControl.ItemsSource = NOVisionDesigner.ViewModel.GlobalTagManager.Instance.Tags;
        }

        private void btn_add_new_tag_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.Save();
        }

        private void btn_add_int_tag_Click(object sender, RoutedEventArgs e)
        {
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.AddTag(new IntTag() { Value = 0,Name="new_tag" }); 
        }

        private void btn_add_float_tag_Click(object sender, RoutedEventArgs e)
        {
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.AddTag(new FloatTag() { Value = 0, Name = "new_tag" });
        }

        private void btn_add_string_tag_Click(object sender, RoutedEventArgs e)
        {
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.AddTag(new StringTag() { Value = "0", Name = "new_tag" });
        }

        private void btn_add_bool_tag_Click(object sender, RoutedEventArgs e)
        {
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.AddTag(new BoolTag() { Value = false, Name = "new_tag" });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var row = (EditGridCellData)button.DataContext;
            var item= row.Row;
            NOVisionDesigner.ViewModel.GlobalTagManager.Instance.Tags.Remove(item as Tag);
            gridControl.RefreshData();
        }
    }
}
