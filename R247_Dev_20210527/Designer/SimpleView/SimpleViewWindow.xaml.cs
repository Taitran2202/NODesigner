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

namespace NOVisionDesigner.Designer.SimpleView
{
    /// <summary>
    /// Interaction logic for SimpleViewWindow.xaml
    /// </summary>
    public partial class SimpleViewWindow : ThemedWindow
    {
        DesignerHost designerHost;
        public SimpleViewWindow(DesignerHost designerHost)
        {
            InitializeComponent();
            this.designerHost = designerHost;
            simpleviewhost.SetDesigner(designerHost);
            this.Owner = Application.Current.MainWindow;
            this.Width = designerHost.HMI.Width;
            this.Height = designerHost.HMI.Height;
            this.Top = designerHost.HMI.PosY;
            this.Left = designerHost.HMI.PosX;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ThemedWindow_LocationChanged(object sender, EventArgs e)
        {
            designerHost.HMI.PosY = this.Top;
            designerHost.HMI.PosX = this.Left;
        }

        private void ThemedWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            designerHost.HMI.Width = this.Width;
            designerHost.HMI.Height = this.Height;
        }
    }
}
