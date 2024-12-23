using DevExpress.Xpf.Core;
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
    /// Interaction logic for ViewModelViewHostWindow.xaml
    /// </summary>
    public partial class ViewModelViewHostWindow : ThemedWindow
    {
        public ViewModelViewHostWindow(object model,string title="Window")
        {
            InitializeComponent();
            host.ViewModel = model;
            this.Title = title;
        }
    }
}
