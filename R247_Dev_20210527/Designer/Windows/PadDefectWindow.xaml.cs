using DevExpress.Xpf.Charts;
using HalconDotNet;
using NOVisionDesigner.Designer.Deeplearning.Windows;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.ViewModel;
using OpenCvSharp;
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
    public partial class PadDefectWindow : DevExpress.Xpf.Core.ThemedWindow, INotifyPropertyChanged
    {
        #region Field
        HWindow display = null;
        HHomMat2D hom;
        HRegion Result;
        HImage image;
        HImage backgroundImage;
        HImage image_original;
        CollectionOfregion region_collection;
        int _Current_Index = 0;
        Region selected_region = null;
        public HRegion region = new HRegion();
        ImageChannel _channel = 0;
        PadDefectTool node;
        double row, col;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        public List<ImageChannel> channels = new List<ImageChannel>() { ImageChannel.Gray, ImageChannel.Red, ImageChannel.Green, ImageChannel.Blue };
        public ObservableCollection<BlobResults> BlobResults { get; set; } = new ObservableCollection<BlobResults>();
        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        public HImage Image
        {
            get => image;
            set
            {
                image = value;
                image?.GetImageSize(out imW, out imH);
                if (display != null&value!=null)
                {
                    //display.DetachBackgroundFromWindow();
                    //display.AttachBackgroundToWindow(value);
                    Run();
                }
                RaisePropertyChanged("Image");
            }
        }
        public HImage BackgroundImage
        {
            get => backgroundImage;
            set
            {
                backgroundImage = value;
                if (display != null & value != null)
                {
                    display.DetachBackgroundFromWindow();
                    display.AttachBackgroundToWindow(value);
                }
                RaisePropertyChanged("BackgroundImage");
            }
        }
        public int imW, imH;
        public double LowerValue
        {
            get
            {
                return node.LowerValue;
            }
            set
            {
                if (node.LowerValue != value)
                {
                    
                    node.LowerValue = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("LowerValue");
                }
            }
        }
        public InspectionSideEnum InspectionSide
        {
            get
            {
                return node.InspectionSide;
            }
            set
            {
                if (node.InspectionSide != value)
                {

                    node.InspectionSide = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("InspectionSide");
                }
            }
        }
        public double ReduceBorder
        {
            get
            {
                return node.ReduceBorder;
            }
            set
            {
                if (node.ReduceBorder != value)
                {

                    node.ReduceBorder = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("ReduceBorder");
                }
            }
        }
        public double UpperValue
        {
            get
            {
                return node.UpperValue;
            }
            set
            {
                if (node.UpperValue != value)
                {
                    
                    node.UpperValue = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("UpperValue");
                }
            }
        }
        double _color_opacity=30;
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
                    if (display != null)
                    {
                        DispOverlay();
                    }
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        string _color= "#00ff00ff";
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    if (display != null)
                    {
                        DispOverlay();
                    }
                    RaisePropertyChanged("Color");
                }
            }
        }

        public int MinArea
        {
            get
            {
                return node.MinArea;
            }
            set
            {
                if (node.MinArea != value)
                {
                    //UserViewModel.WriteActionDatabase(node.Name, "MinArea", node.MinArea.ToString(), value.ToString(), "Change Parameter", null);
                    node.MinArea = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("MinArea");
                }
            }
        }
        public int MaxArea
        {
            get
            {
                return node.MaxArea;
            }
            set
            {
                if (node.MaxArea != value)
                {
                 
                    node.MaxArea = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("MaxArea");
                }
            }
        }
        public double ClosingCircle
        {
            get
            {
                return node.Closing;
            }
            set
            {
                if (node.Closing != value)
                {
                    
                    node.Closing = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("ClosingCircle");
                }
            }
        }
        public bool IsFill
        {
            get
            {
                return node.IsFill;
            }
            set
            {
                if (node.IsFill != value)
                {
                    
                    node.IsFill = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("IsFill");
                }
            }
        }
        public bool Invert
        {
            get
            {
                return node.Invert;
            }
            set
            {
                if (node.Invert != value)
                {
                    node.Invert = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("Invert");
                }
            }
        }
        public int Current_Index
        {
            get
            {
                return _Current_Index;
            }
            set
            {
                if (_Current_Index != value)
                {
                    _Current_Index = value;
                    OnIndexChanged(value);
                    RaisePropertyChanged("Current_Index");
                }
            }
        }

        public Region Selected_region
        {
            get => selected_region;
            set
            {
                if (value != selected_region)
                {
                    selected_region = value;
                }
            }
        }
        public ImageChannel Channel
        {
            get
            {
                return node.Channel;
            }
            set
            {
                if (node.Channel != value)
                {
                    
                    node.Channel = value;
                    RaisePropertyChanged("Channel");
                }
            }
        }
        #endregion
        public PadDefectWindow(PadDefectTool blobNode,HImage image,HImage backgroundImage, HHomMat2D fixture)
        {
            node = blobNode;
            InitializeComponent();
            
            if (image != null)
            {
                image_original = image;
            }
            else
            {
                image = new HImage("byte", 512, 512);
            }
            if (backgroundImage == null)
            {
                BackgroundImage = new HImage("byte", 512, 512);
            }
            else
            {
                BackgroundImage = backgroundImage;
            }

            Image = node.GetImageChannel(image_original);
            hom = fixture != null? fixture.Clone(): new HHomMat2D();
            series.Points?.Clear();
            series.Points.AddRange(Points);
            cmb_channels.ItemsSource = channels;
            DataContext = this;
            region_collection = blobNode.Regions;
            region = region_collection.Region;
            stack_display.DataContext = node;
            lst_region.ItemsSource = blobNode.Regions.regions;
        }
        #region Method
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        private void UpdatedDisplay()
        {
            DispOverlay();
            //HTuple hist = Histogram();
            //Points.Clear();
            //for (int i = 0; i < 255; i++)
            //{
            //    Points.Add(new SeriesPoint(i, hist[i]));
            //}
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    series.Points.Clear();
            //    series.Points.AddRange(Points);
            //    //line_thresh.Y = edges[index].Threshold;
            //}
            //));
        }
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public void DispOverlay()
        {
            
            display.ClearWindow();
            display.SetDraw("fill");
            display.SetColor(AddOpacity(Color, ColorOpacity / 100));
            if (Result != null)
            {
                display.DispRegion(Result);
            }
            display.SetColor("red");
            display.SetDraw("margin");
            display.SetLineWidth(3);
            display.SetLineStyle(new HTuple(20, 7));
            if (Result != null)
            {
                display.DispRegion(Result);
            }
            display.SetLineWidth(1);
            display.SetColor("blue");
            display.DispRegion(node.Regions.Region);
        }
        private void Update(Region sender)
        {
            Selected_region = sender;                  
            ChangeRegion();
            Run();
            HTuple hist = Histogram();
            Points.Clear();
            for (int i = 0; i < 255; i++)
            {
                Points.Add(new SeriesPoint(i, hist[i]));
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);
                //line_thresh.Y = edges[index].Threshold;
            }
            ));
        }
        public HTuple Histogram()
        {
            HRegion region_inspection = region_collection.Region;
            if (hom != null)
            {
                region_inspection = hom.AffineTransRegion(region_inspection, "nearest_neighbor");
            }
            HTuple relative;
            image.GrayHisto(region_inspection, out relative);
            return relative;
        }

        public void Run()
        {
            if (image == null)
            {
                //display region boundary
                display.ClearWindow();
                display.SetDraw("margin");
                display.SetLineStyle(new HTuple(20, 7));
                display.DispRegion(node.Regions.Region);
                return;
            }
            HRegion RegionInspect = node.Regions.Region;
            HRegion regionInspectTransform = hom != null ? hom.AffineTransRegion(RegionInspect, "nearest_neighbor") : RegionInspect;
            Result = node.RunInside(image, regionInspectTransform,out HRegion ProcessedRegion);
            BlobResults.Clear();
            for (int i = 0; i < Result.CountObj(); i++)
            {
                BlobResults.Add(new BlobResults(Result[i + 1], i));
            }
            if (EnableHeatMap)
            {
                HImage heatmap = ApplyColorMap(BackgroundImage, image, node.LowerValue);
                window_display.HalconWindow.AttachBackgroundToWindow(heatmap);
            }
            else
            {
                window_display.HalconWindow.AttachBackgroundToWindow(BackgroundImage);
            }
            
            
            display.ClearWindow();
            window_display.HalconWindow.SetColor("green");
            window_display.HalconWindow.DispObj(ProcessedRegion);

           
            display.SetDraw("fill");
            display.SetColor(AddOpacity(Color, ColorOpacity / 100));
            if (Result != null)
            {
                display.DispRegion(Result);
            }
            display.SetColor("red");
            display.SetDraw("margin");
            display.SetLineWidth(3);
            display.SetLineStyle(new HTuple(20, 7));
            if (Result != null)
            {
                display.DispRegion(Result);
            }
            display.SetLineWidth(1);
            display.SetColor("blue");
            display.DispRegion(node.Regions.Region);
            //UpdatedDisplay();
        }
        private void AddRegionNew(Region region)
        {
            display.GetPart(out int r1, out int c1, out int r2, out int c2);
            region.Initial((int)(r2 + r1) / 2 - 50, (int)(c2 + c1) / 2 - 50);
            //region.Initial((int)row, (int)col);

            Region region_add = region;
            region_collection.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(hom);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            if (region_collection.regions.Count == 1)
            {
                Selected_region = region_collection.regions[0];
            }
            lst_region.SelectedItem = region_add;
            Update(region_add);
        }
        private void AddRegion(Region region)
        {
            Region region_add = region;
            // regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(hom);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            Update(region_add);
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            region.Dispose();

            region.GenEmptyRegion();
            foreach (Region item in region_collection.regions)
            {
                if (!item.Minus)
                {
                    region = region.Union2(item.region);
                }
            }
            foreach (Region item in region_collection.regions)
            {
                if (item.Minus)
                {
                    region = region.Difference(item.region);
                }
            }
            //DispOverlay();
            region_collection.Region = region;
        }
        #endregion

        #region Event Handler
        private void OnIndexChanged(int index)
        {
            if(index == -1) { return; }
            display.SetColor("blue");
            display.DispRegion(Result);
            display.SetColor("green");
            display.DispRegion(Result[index+1]);

        }

        private void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
            Run();
        }
        #endregion

        #region Views

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("fill");
            if(image != null)
            {
                display.AttachBackgroundToWindow(image);
                foreach (Region region in region_collection.regions)
                {
                    AddRegion(region);
                }
                if (region_collection.regions.Count > 0)
                {
                    Selected_region = region_collection.regions[0];
                    lst_region.SelectedItem = node.Regions.regions[0];
                }
                window_display.SetFullImagePart(image);
                try
                {
                    HImage heatmap = ApplyColorMap(BackgroundImage, image, node.LowerValue);
                    window_display.HalconWindow.AttachBackgroundToWindow(heatmap);
                }catch(Exception ex)
                {

                }
                
            }

        }
        

        private void Setting_Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Tab_Grid.Focus();
        }


        private void SpinEdit_EditValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (line_pos != null & line_neg != null)
            {
                line_pos.Value = UpperValue;
                line_neg.Value = LowerValue;

                //if (display != null)
                //{
                //    Run();
                //}
            }
        }

        private void SpinEdit_EditValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (line_pos != null & line_neg != null)
            {
                line_pos.Value = UpperValue;
                line_neg.Value = LowerValue;
                //if (display != null)
                //{
                //    Run();
                //}
            }
        }

        private void range_slider_ValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {

        }

        private void btn_add_rect_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AddRegionNew(new Rectange1(false));
        }

        private void btn_add_rect2_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }

        private void btn_add_ellipse_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AddRegionNew(new Nodes.Ellipse(false));
        }

        private void btn_add_circle_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AddRegionNew(new Circle(false));
        }

        private void Cmb_channels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_channels.SelectedItem != null)
            {
                Image = node.GetImageChannel(image_original);

            }
        }
        bool EnableHeatMap = true;
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            EnableHeatMap = true;
            try
            {
                if (display == null)
                {
                    return;
                }
                Run();
            }catch(Exception ex)
            {

            }
            
            //window_display.HalconWindow.SetWindowParam("background_color", "white");
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            EnableHeatMap = false;
            try
            {
                if (display == null)
                {
                    return;
                }
                Run();
            }
            catch (Exception ex)
            {
                 
            }
            //window_display.HalconWindow.SetWindowParam("background_color", "black");
        }

        private void btn_add_nurb_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //AddRegionNew(new Nodes.Nurbs(false));
            AddNurbs();
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as Region;
            if (selected != null)
            {
                selected.ClearDrawingData(display);
                node.Regions.regions.Remove(selected);
                
                ChangeRegion();
                DispOverlay();
                Run();
            }
        }

        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Region selected = lst_region.SelectedItem as Region;
            if (selected == null)
            {
                foreach (Region region in node.Regions.regions)
                {
                    if (region.current_draw != null)
                        display.DetachDrawingObjectFromWindow(region.current_draw);
                }
                //selectedRegion = null;
            }
            else
            {
                //stack_parameter.DataContext = selected;

                foreach (Region region in node.Regions.regions)
                {
                    if (region.current_draw != null)
                        display.DetachDrawingObjectFromWindow(region.current_draw);
                }
                if (selected.current_draw != null)
                    display.AttachDrawingObjectToWindow(selected.current_draw);
                if (selected is BrushRegion)
                {
                    //selectedRegion = selected.region;
                }
                else
                {
                    //selectedRegion = null;
                }
            }
            DispOverlay();
        }

        private void btn_add_rec_Click(object sender, RoutedEventArgs e)
        {
            
            AddRegionNew(new Rectange1(false));
        }

        private void btn_add_rec2_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }
        void AddNurbs()
        {
            Nurbs region_add = new Nurbs(false);


            WindowNurbs wd = null;
            bool? result = false;
            try
            {
                wd = new WindowNurbs(image, region_add, hom, this, false);
                result = wd.ShowDialog();
            }
            catch (Exception ex)
            {
                wd?.Close();
                return;
            }
            if (result == true)
            {
                if (region_add.rows.Length < 3)
                {
                    MessageBox.Show("This region require atleast 3 point!!!");
                    return;

                }
                region_collection.regions.Add(region_add);
                region_add.OnUpdated = Update;
                HDrawingObject draw = region_add.CreateDrawingObject(hom);
                if (draw != null)
                {
                    //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                    draw.OnResize(region_add.OnResize);
                    draw.OnDrag(region_add.OnResize);
                    draw.OnSelect(region_add.OnResize);
                }
                if (region_collection.regions.Count == 1)
                {
                    Selected_region = region_collection.regions[0];
                }
                lst_region.SelectedItem = region_add;
                ChangeRegion();
                Run();
            }
        }
        private void btn_add_curve_Click(object sender, RoutedEventArgs e)
        {
            //AddRegionNew(new Nurbs(false));

            AddNurbs();



        }

        private void btn_add_brush_Click(object sender, RoutedEventArgs e)
        {
            var newBrushRegion = new BrushRegion(false);
            BrushWindow wd = new BrushWindow(image, hom, newBrushRegion);
            wd.ShowDialog();
            AddRegionNew(newBrushRegion);
        }

        private void btn_add_circle_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Circle(false));
        }

        private void btn_add_ellipse_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nodes.Ellipse(false));
        }

        private void btn_edit_region_Click(object sender, RoutedEventArgs e)
        {
            if (lst_region.SelectedItem != null)
            {
                if(lst_region.SelectedItem is BrushRegion)
                {
                    BrushWindow wd = new BrushWindow(image, hom, lst_region.SelectedItem as BrushRegion);
                    wd.ShowDialog();
                    return;
                }
                if (lst_region.SelectedItem is Nurbs)
                {
                    WindowNurbs wd = null;
                    try
                    {
                        var regionAdd = lst_region.SelectedItem as Nurbs;
                        wd = new WindowNurbs(image, regionAdd, hom, this, true);
                        if (wd.ShowDialog() == true)
                        {
                            if (regionAdd.current_draw != null)
                            {
                                regionAdd.ClearDrawingData(window_display.HalconWindow);

                            }
                            HDrawingObject draw = regionAdd.CreateDrawingObject(hom);
                            if (draw != null)
                            {
                                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                                draw.OnResize(regionAdd.OnResize);
                                draw.OnDrag(regionAdd.OnResize);
                                draw.OnSelect(regionAdd.OnResize);
                            }
                            window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                            ChangeRegion();
                            Run();
                        }
                        

                    }
                    catch (Exception ex)
                    {
                        wd?.Close();
                        return;
                    }
                }
            }
        }

        private void btn_remove_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (Selected_region != null)
            {
                Selected_region.ClearDrawingData(display);
                region_collection.regions.Remove(Selected_region);
                ChangeRegion();
                Run();
                //DispOverlay();
                Selected_region = null;
            }
        }


        private void window_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            try
            {
                row = e.Row;
                col = e.Column;

                if (e.Row > 0 & e.Column > 0 & e.Row <= imH & e.Column<=imW )
                    lb_gray_value.Content = Image?.GetGrayval((int)e.Row,(int) e.Column);
                else
                    lb_gray_value.Content = 0;
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        /// <summary>
        /// aply color map using opencv
        /// </summary>
        /// <param name="image">gray image</param>
        /// <param name="map">float image range 0-1</param>
        public HImage ApplyColorMap(HImage image, HImage map, double threshold)
        {
            if (map == null | !image.IsInitialized())
            {
                return null;
            }
            if (image.CountChannels() == 1)
            {
                image = image.Compose3(image, image);
            }
            image.GetImagePointer1(out string type, out int w, out int h);
            var ptrMap = map.GetImagePointer1(out string typem, out int wm, out int hm);
            //Mat cvImage = new Mat(h, w, MatType.CV_8U, ptr);
            Mat cvMap = new Mat(hm, wm, MatType.CV_32FC1, ptrMap);
            Mat cvMapThresh = new Mat();
            var th = Cv2.Threshold(cvMap, cvMapThresh, threshold , 255, ThresholdTypes.Binary);
            Mat cvMapThreshByte = new Mat();
            cvMapThresh.ConvertTo(cvMapThreshByte, MatType.CV_8UC1);
            Cv2.GaussianBlur(cvMapThreshByte, cvMapThreshByte, new OpenCvSharp.Size(13, 13), 11);
            var heatmap_img = new Mat();
            //var superImposed = new Mat();
            Cv2.ApplyColorMap(cvMapThreshByte, heatmap_img, ColormapTypes.Jet);
            //Cv2.AddWeighted(cvImage, 0.5, heatmap_img, 0.5, 0, superImposed);
            var c0 = heatmap_img.ExtractChannel(0);
            var c1 = heatmap_img.ExtractChannel(1);
            var c2 = heatmap_img.ExtractChannel(2);

            HImage b = new HImage("byte", w, h, c0.Data);
            HImage g = new HImage("byte", w, h, c1.Data);
            HImage r = new HImage("byte", w, h, c2.Data);
            HImage t = r.Compose3(g, b);
            t = t * 0.5 + image * 0.5;
            return t;

        }
    }

    
}
