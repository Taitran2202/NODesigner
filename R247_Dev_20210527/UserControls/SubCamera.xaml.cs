using HalconDotNet;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.SimpleView;
using NOVisionDesigner.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for SubCamera.xaml
    /// </summary>

    public partial class SubCamera : UserControl
    {
        public SubCamera()
        {
            InitializeComponent();
            this.Loaded += SubCamera_Loaded;
            hmi.OnSizeAndPositionChanged += Hmi_SizeChanged;
            grid_display.OnSizeAndPositionChanged += display_SizeChanged;



        }
        Designer.DesignerHost designerHost;
        private void display_SizeChanged(object sender, EventArgs e)
        {
            designerHost.displayData.Width = grid_display.Width;
            designerHost.displayData.Height = grid_display.Height;
            designerHost.displayData.PosX = grid_display.Margin.Left;
            designerHost.displayData.PosY = grid_display.Margin.Top;
        }
        private void Hmi_SizeChanged(object sender, EventArgs e)
        {
            designerHost.HMI.Width = hmi.Width;
            designerHost.HMI.Height = hmi.Height;
            designerHost.HMI.PosX = hmi.Margin.Left;
            designerHost.HMI.PosY = hmi.Margin.Top;
        }
        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }
            return null;
        }
        private void SubCamera_Loaded(object sender, RoutedEventArgs e)
        {
            designerHost = (this.DataContext as VisionModel).Designer;
            HSmartWindowControlWPF wd, wd1;
            wd = GetByUid(grid_display, "wd") as HSmartWindowControlWPF;
            wd1 = GetByUid(grid_display, "wd1") as HSmartWindowControlWPF;

            //wd.UpdateLayout();
            // HImage test = new HImage("byte", 512, 512);
            //wd.HalconWindow.AttachBackgroundToWindow(test);
            //wd.SetFullImagePart();
            designerHost.SetDisplayMainWindow(wd, wd1);
            //wd.UpdateLayout();
            //wd.InvalidateVisual();
            //wd.HKeepAspectRatio = true;
            hmi.DataContext = designerHost.HMI;
            btn_hmi.IsChecked = designerHost.HMI.AlwaysShowMenu;
        }
        void LoadDefaultLayout()
        {
            var w = grid_view.ActualWidth;
            var h = grid_view.ActualHeight;
            int HMIWidth = 300;
            if (w > 0 & h > 0)
            {
                designerHost.HMI.Width= HMIWidth;
                designerHost.HMI.Height = h;
                designerHost.HMI.PosX = w - HMIWidth;
                designerHost.HMI.PosY = 0;

                designerHost.displayData.PosX = 0;
                designerHost.displayData.PosY = 0;
                designerHost.displayData.Width= w- HMIWidth;
                designerHost.displayData.Height = h;
            }
        }
        private void SimpleViewHost_Loaded(object sender, RoutedEventArgs e)
        {
            var designerHost = (this.DataContext as VisionModel).Designer;
            (sender as SimpleViewHost)?.SetDesigner(designerHost);
            ReloadLayout(designerHost);
        }

        private void ReloadLayout(DesignerHost designerHost)
        {
            if (designerHost.UseDefaultLayout)
            {
                LoadDefaultLayout();
                designerHost.UseDefaultLayout = false;
            }
            hmi.Width = designerHost.HMI.Width;
            hmi.Height = designerHost.HMI.Height;
            hmi.Margin = new Thickness(designerHost.HMI.PosX, designerHost.HMI.PosY, 0, 0);
            var wd = GetByUid(grid_display, "wd1") as HSmartWindowControlWPF;
            wd.HKeepAspectRatio = false;
            grid_display.Margin = new Thickness(designerHost.displayData.PosX, designerHost.displayData.PosY, 0, 0);
            grid_display.Width = designerHost.displayData.Width;
            grid_display.Height = designerHost.displayData.Height;
        }

        private void HSmartWindowControlWPF_HInitWindow(object sender, EventArgs e)
        {
            HSmartWindowControlWPF display = sender as HSmartWindowControlWPF;
            display.HalconWindow.SetWindowParam("graphics_stack_max_element_num", 50);
        }

        private void grid_display_Loaded(object sender, RoutedEventArgs e)
        {
            

        }
    }
}
