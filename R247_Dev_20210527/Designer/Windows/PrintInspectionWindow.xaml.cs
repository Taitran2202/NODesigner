using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for PrintInspectionWindow.xaml
    /// </summary>
    public partial class PrintInspectionWindow : Window,INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        double _color_opacity = 80;
        public double ColorOpacity
        {
            get
            {
                return _color_opacity;
            }
            set
            {
                if (_color_opacity != value)
                {
                    _color_opacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        bool _is_drawing = false;
        public bool IsDrawing
        {
            get
            {
                return _is_drawing;
            }
            set
            {
                if (_is_drawing != value)
                {
                    _is_drawing = value;
                    RaisePropertyChanged("IsDrawing");
                }
            }
        }
        Rect2 pre_rect;
        PrintInspectionNode node;
        HImage image;
        HHomMat2D fixture;
        RegionMaker2 ReferenceRegion = new RegionMaker2() { Region = new Rectange2(false), Annotation = new ClassifierClass1() { Name = "Reference Region" } };
        public void AttachReferenceImage()
        {
            if (node.Options.ReferenceImage != null)
            {
                window_reference.HalconWindow.AttachBackgroundToWindow(node.Options.ReferenceImage);
                UpdateMask();
                window_reference.SetFullImagePart();
            }

        }

        private void UpdateMask()
        {
            if (node.Options.ReferenceMask != null)
            {
                window_reference.HalconWindow.ClearWindow();
                node.Options.ReferenceImage.GetImageSize(out int w, out int h);
                HRegion hRegion = new HRegion();
                hRegion.GenRectangle1(0.0, 0, h, w);
                var regionMask = hRegion.Difference(node.Options.ReferenceMask.Region);
                window_reference.HalconWindow.SetColor("#00ff00aa");
                window_reference.HalconWindow.SetDraw("fill");
                window_reference.HalconWindow.DispObj(regionMask);
                window_reference.HalconWindow.SetColor("#00ff00ff");
                window_reference.HalconWindow.SetDraw("margin");
                window_reference.HalconWindow.DispObj(regionMask);
            }
        }

        public PrintInspectionWindow(PrintInspectionNode node)
        {
            this.node = node;
            InitializeComponent();
            if(node.ImageInput.Value != null)
            {
                image = node.ImageInput.Value.Clone();
            }
            if (node.HomInput.Value != null)
            {
                fixture = node.HomInput.Value.Clone();
            }
            else
            {
                fixture = new HHomMat2D();
            }
            if (node.Options.InspectionRegion != null)
            {
                ReferenceRegion = new RegionMaker2()
                {
                    Region = new Rectange2(false, node.Options.InspectionRegion.row,
                node.Options.InspectionRegion.col,
                node.Options.InspectionRegion.phi,
                node.Options.InspectionRegion.length1,
                node.Options.InspectionRegion.length2
                ),
                    Annotation = new ClassifierClass1() { Name = "Reference Region" }
                };
            }
            else
            {
                ReferenceRegion = new RegionMaker2()
                {
                    Region = new Rectange2(false, 0,
                0,
                0,
                100,
                100
                ),
                    Annotation = new ClassifierClass1() { Name = "Reference Region" }
                };
            }
            
            propertiesGrid.SelectedObject = node.Options;
            
            this.DataContext = this;

        }
        public void UpdateRegionToMarker(Rect2 region, RegionMaker2 marker)
        {
                
           marker.Region = new Rectange2(false, region.row, region.col, region.phi, region.length1, region.length2);


        }
        private void HSmartWindowControlWPF_HInitWindow(object sender, EventArgs e)
        {
            var display = (sender as HSmartWindowControlWPF);
            display.HalconWindow.SetWindowParam("background_color", "white");
            display.HalconWindow.ClearWindow();
            if(image != null)
            {
                display.HalconWindow.AttachBackgroundToWindow(image);
            }
        }

        private void HSmartWindowControlWPF_HInitWindow_1(object sender, EventArgs e)
        {
            (sender as HSmartWindowControlWPF).HalconWindow.SetWindowParam("background_color", "white");
            (sender as HSmartWindowControlWPF).HalconWindow.ClearWindow();
            AttachReferenceImage();
        }


        private void btn_change_region_Click(object sender, RoutedEventArgs e)
        {
            
            ReferenceRegion.Attach(window_display.HalconWindow, fixture, PatternRegionParametersChangedCallback, PatternRegionSelectedCallback);
            pre_rect = new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2);
            IsDrawing = true;
        }
        public void UpdateSelectBoxPosition()
        {
            if (IsDrawing )
            {
                double winposx, winposy;
                var rect2 = new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2);
                var bb = ViewDisplayMode.BoundingBox(PrintInspectionNode.AffineTranRect2(rect2,fixture));
                window_display.HalconWindow.ConvertCoordinatesImageToWindow(bb.row1, bb.col1, out winposx, out winposy);
                box_pattern_accept.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }

        }
        public void DispOverlay()
        {
            if (window_display.HalconWindow == null)
                return;
            window_display.HalconWindow.ClearWindow();
            //display.SetColor("#00ff0025");
            DisplayMarker(ReferenceRegion, true);
        }
        public void PatternRegionParametersChangedCallback(RegionMaker2 sender, Region region)
        {
            DispOverlay();
            //UpdateSelectBoxPosition();
        }
        public void PatternRegionSelectedCallback(RegionMaker2 sender, Region region)
        {

        }

        private void btn_accept_pattern_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            ReferenceRegion.Region.ClearDrawingData(window_display.HalconWindow);
            AcceptRegion();
        }
        public void AcceptRegion()
        {
            if (ReferenceRegion != null)
            {
                if (image != null)
                {
                    var rect2Trans = PrintInspectionNode.AffineTranRect2(new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2),fixture);
                    HImage referenceImage = CropRect2(rect2Trans.row, rect2Trans.col, rect2Trans.phi, rect2Trans.length1, rect2Trans.length2);
                    node.Options.ReferenceImage = referenceImage.Clone();
                    node.Options.InspectionRegion = new Rect2(ReferenceRegion.Region.row, ReferenceRegion.Region.col, ReferenceRegion.Region.phi, ReferenceRegion.Region.length1, ReferenceRegion.Region.length2);
                    node.IsPrepared = false;
                    AttachReferenceImage();
                }
            }
        }

        private HImage CropRect2(double row,double col,double phi,double length1,double length2)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            hHomMat2D.VectorAngleToRigid(row, col, phi, length2,length1, 0);
            var referenceImage = hHomMat2D.AffineTransImageSize(image, "constant", (int)(length1 * 2), (int)(length2 * 2));
            return referenceImage;
        }
        
        private void DisplayMarker(RegionMaker2 item, bool draw_bb = false)
        {
            if (item.Annotation != null)
            {
                window_display.HalconWindow.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                window_display.HalconWindow.DispObj(item.Region.region.AffineTransRegion(fixture,"constant"));
                var rect2 = new Rect2(item.Region.row, item.Region.col, item.Region.phi, item.Region.length1, item.Region.length2);
                var transRect = PrintInspectionNode.AffineTranRect2(rect2, fixture);
                var bb = ViewDisplayMode.BoundingBox(transRect);
                if (draw_bb)
                {
                    //display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                    window_display.HalconWindow.SetDraw("margin");
                    window_display.HalconWindow.DispRectangle1(bb.row1, bb.col1, bb.row2, bb.col2);
                    window_display.HalconWindow.SetDraw("fill");
                }

                window_display.HalconWindow.DispText(item.Annotation.Name, "image", bb.row1, bb.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
            }
        }
        private void btn_discard_pattern_Click(object sender, RoutedEventArgs e)
        {
            IsDrawing = false;
            ReferenceRegion.Region.ClearDrawingData(window_display.HalconWindow);
            ReferenceRegion.Region = new Rectange2(false, pre_rect.row, pre_rect.col, pre_rect.phi, pre_rect.length1, pre_rect.length2);
        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            
        }

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }

        private void btn_change_mask_click(object sender, RoutedEventArgs e)
        {
            WindowRegionWindowInteractive wd = new WindowRegionWindowInteractive(node.Options.ReferenceImage, node.Options.ReferenceMask, null);
            wd.ShowDialog();
            UpdateMask();
        }
    }
}
