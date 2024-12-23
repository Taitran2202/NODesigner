using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace NOVisionDesigner.Designer.Editors.Windows
{
    /// <summary>
    /// Interaction logic for StringListEditorWindow.xaml
    /// </summary>
    public partial class StringListEditorWindow : Window
    {
        ObservableCollection<EnumString> ListString;
        public StringListEditorWindow(ObservableCollection<EnumString> ListString)
        {
            InitializeComponent();
            lst_view.ItemsSource = ListString;
            this.ListString = ListString;
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext;
            int index = lst_view.Items.IndexOf(item);
            ListString.RemoveAt(index);
        }

        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            ListString.Add( new EnumString() { Value = "new data" });
        }
    }
}
