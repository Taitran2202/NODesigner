using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI;
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

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for UniquaEbusWindow.xaml
    /// </summary>
    public partial class UniquaEbusWindow : ThemedWindow
    {
        EBUS model;
        public UniquaEbusWindow(EBUS model)
        {
            InitializeComponent();
            //menu.Items.Add(
            //    new HamburgerMenuNavigationButton
            //    {
            //        IsSelected=true,
            //        Content = "Live",
            //        NavigationTargetType = typeof(Windows.UniquaView.UniquaLiveView),
            //        NavigationTargetParameter= model,
            //    }
            //  );
            this.model= model;
            uniquaView.SetModel(model);
        }

        //private void NavigationFrame_Navigated(object sender, DevExpress.Xpf.WindowsUI.Navigation.NavigationEventArgs e)
        //{
        //    if(e.Content is Windows.UniquaView.UniquaLiveView)
        //    {
        //        (e.Content as Windows.UniquaView.UniquaLiveView).SetModel(model);
        //    }
        //}
    }
}
