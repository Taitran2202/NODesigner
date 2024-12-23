using DynamicData;
using NodeNetwork.ViewModels;
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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for DesignerListViewWindow.xaml
    /// </summary>
    public partial class DesignerListViewWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        public DesignerListViewWindow(Designer.DesignerHost designer)
        {
            InitializeComponent();
            designerListView.SetDesigner(designer);
            this.Closing += DesignerListViewWindow_Closing;
        }

        private void DesignerListViewWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Activate();
            }
        }
    }
}
