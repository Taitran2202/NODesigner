using NOVisionDesigner.Designer.Nodes;
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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for EditRejectionClassWindow.xaml
    /// </summary>
    public partial class EditRejectionClassWindow : Window
    {
        public EditRejectionClassWindow(IEnumerable<RejectionClass> lst)
        {
            InitializeComponent();
            item_control.ItemsSource = lst;
        }
    }
}
