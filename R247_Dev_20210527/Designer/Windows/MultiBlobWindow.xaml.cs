using DevExpress.Xpf.Charts;
using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.ViewModel;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for BlobWindow.xaml
    /// </summary>
    public partial class MultiBlobWindow : Window
    {        
        public MultiBlobWindow(MultiBlobNode blobNode)
        {
            InitializeComponent();
            lst_view.ItemsSource = blobNode.ListBlobDetection;
            blobNode.ListBlobDetection.Add(new BlobDetection());
        }
       
    }
}
