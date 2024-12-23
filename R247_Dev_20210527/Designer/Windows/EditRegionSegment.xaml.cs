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
using HalconDotNet;
namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for EditRegionSegment.xaml
    /// </summary>
    public partial class EditRegionSegment : Window
    {
        public HHomMat2D fixture;
        public HImage image;
        public HDrawingObject drawObject;
        public HWindow display;
        public HRegion region;
        public EditRegionSegment(HImage image, HRegion region, HHomMat2D fixture)
        {
            InitializeComponent();
            if (image != null)
            {
                this.image = image;
            }
                
            if (region!=null)
            {
                this.region = region;
                HTuple row1, col1, row2, col2;
                if (fixture!=null)
                {
                    this.fixture = fixture;
                    region = fixture != null ? fixture.AffineTransRegion(region, "nearest_neighbor") : region;
                }
                region.SmallestRectangle1(out row1, out col1, out row2, out col2);
                drawObject = new HDrawingObject(row1, col1, row2, col2);
            }
            else
            {
                drawObject = new HDrawingObject(200, 200, 1000, 1000);
            }
         


        }
        public void DisplayRegion()
        {
            if (drawObject != null)
            {
                drawObject.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                drawObject.SetDrawingObjectParams("color", "green");
                display?.AttachDrawingObjectToWindow(drawObject);
            }
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("fill");
            display.SetColor("green");
            display.ClearWindow();
            if (image != null)
            {
                display.AttachBackgroundToWindow(image);
            }
            DisplayRegion();
        }

        private void Window_display_HMouseMove(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void window_display_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
