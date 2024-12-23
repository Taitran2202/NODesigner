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
using NOVisionDesigner.Designer.Nodes;
using HalconDotNet;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for RectangleMetrologyWindow.xaml
    /// </summary>
    public partial class RectangleMetrologyWindow : Window
    {
        Rectangle2MetrologyNode node;
        HHomMat2D fixture;
        HImage image;
        RegionMaker2 RegionMarker; 
        
        public RectangleMetrologyWindow(Rectangle2MetrologyNode node)
        {
            InitializeComponent();
            this.node = node;
            propertiesGrid.SelectedObject = node.Model.Parameter;
            if (node.Fixture.Value == null)
            {
                fixture = new HHomMat2D();
            }
            else
            {
                fixture = node.Fixture.Value.Clone();
            }
            if (node.Image.Value == null)
            {
                image = null;
            }
            else
            {
                image = node.Image.Value.Clone();
            }


        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            RegionMarker = new RegionMaker2() { Region = new Rectange2(false, 
                node.Model.Parameter.Row, 
                node.Model.Parameter.Column, 
                node.Model.Parameter.Phi, 
                node.Model.Parameter.Length1, 
                node.Model.Parameter.Length2) };
            RegionMarker.Attach(window_display.HalconWindow, fixture, OnUpdate, OnSelected);

            if (image != null)
            {
                window_display.HalconWindow.AttachBackgroundToWindow(image);
            }
        }
        public void OnUpdate(RegionMaker2 marker, Region region)
        {
           
        }
        public void OnSelected(RegionMaker2 marker, Region region)
        {

        }
        public void Apply()
        {
            if (image != null)
            {
                window_display.HalconWindow.ClearWindow();
                try
                {
                    node.Model.UpdateRect(RegionMarker.Region);
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                
                var rect2 = node.Model.Run(image, fixture);
                if (rect2 != null)
                {
                    var contours = node.Model.GetContourResult();
                    var measure = node.Model.GetMeasureResult();
                    window_display.HalconWindow.SetColor("green");
                    
                    window_display.HalconWindow.DispObj(contours);
                    window_display.HalconWindow.SetColor("orange");
                    window_display.HalconWindow.DispObj(measure.measure);
                    window_display.HalconWindow.SetColor("blue");
                    window_display.HalconWindow.DispObj(measure.cross);
                }
                
            }
            
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            this.Close();
        }

        private void btn_apply_Click(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void btn_grab_image_Click(object sender, RoutedEventArgs e)
        {
            if (node.Image.Value!=null) {
                image = node.Image.Value.Clone();
                if (node.Fixture.Value == null)
                {
                    fixture = new HHomMat2D();
                }
                else
                {
                    fixture = node.Fixture.Value.Clone();
                }
                RegionMarker.Region.transform = fixture;
                window_display.HalconWindow.AttachBackgroundToWindow(image);
            }
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
