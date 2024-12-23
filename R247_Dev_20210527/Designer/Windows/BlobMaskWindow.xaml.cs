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
    public partial class BlobMaskWindow : Window, INotifyPropertyChanged
    {
        #region Field
        HWindow display = null;
        HHomMat2D hom;
        public HRegion Result;
        HImage image;
        HImage image_original;
        CollectionOfregion region_collection;
        int _Current_Index = 0;
        Region selected_region = null;
        public HRegion region = new HRegion();
        ImageChannel _channel = 0;
        BlobMask node;
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
                if (display != null & value != null)
                {
                    display.DetachBackgroundFromWindow();
                    display.AttachBackgroundToWindow(value);
                    Run();
                }
                RaisePropertyChanged("Image");
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
                    if (display != null)
                    {
                        DispOverlay();
                    }
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        string _color = "#00ff00ff";
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

        public int Min_Area
        {
            get
            {
                return node.MinArea;
            }
            set
            {
                if (node.MinArea != value)
                {
                    
                    node.MinArea = value;
                    if (display != null)
                    {
                        Run();
                    }
                    RaisePropertyChanged("Min_Area");
                }
            }
        }
        public double Closing
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
                    RaisePropertyChanged("Closing");
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
        public BlobMaskWindow(BlobMask blobNode)
        {
            node = blobNode;
            InitializeComponent();
            image_original = blobNode.Image;
            Image = node.GetImageChannel(image_original);
            hom =  new HHomMat2D();
            series.Points?.Clear();
            series.Points.AddRange(Points);
            cmb_channels.ItemsSource = channels;
            DataContext = this;
            region_collection = blobNode.Regions;
            if (region_collection.regions.Count == 0)
            {
                Image.GetImageSize(out int w, out int h);
                region_collection.regions.Add(new Rectange1(false)
                {
                    Row1=0,Col1=0,Row2=h,Col2=w
                });
            }
            region = region_collection.Region;
            //stack_display.DataContext = node;
        }
        #region Method
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        private void UpdatedDisplay()
        {
            DispOverlay();
            HTuple hist = Histogram();
            Points.Clear();
            for (int i = 0; i < 255; i++)
            {
                Points.Add(new SeriesPoint(i, hist[i]));
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //series.Points.Clear();
                //series.Points.AddRange(Points);
                //line_thresh.Y = edges[index].Threshold;
            }
            ));
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
            display.SetColor("blue");
            display.SetDraw("margin");
            display.SetLineStyle(new HTuple(20, 7));
            display.DispRegion(node.Regions.Region);
        }
        private void Update(Region sender)
        {
            Selected_region = sender;
            ChangeRegion();
            Task.Run(() =>
            {
                Run();
            });
            
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
            Result = node.RunInside(image, hom);
            Application.Current.Dispatcher.Invoke(() =>
            {
                BlobResults.Clear();
                for (int i = 0; i < Result.CountObj(); i++)
                {
                    BlobResults.Add(new BlobResults(Result[i + 1], i));
                }
            });
            
            UpdatedDisplay();
        }
        private void AddRegionNew(Region region)
        {

            region.Initial((int)row, (int)col);

            Region region_add = region;
            region_collection.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(hom);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            //if (region_collection.regions.Count == 1)
            //{
            //    Selected_region = region_collection.regions[0];
            //}
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
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
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
            if (index == -1) { return; }
            display.SetColor("blue");
            display.DispRegion(Result);
            display.SetColor("green");
            display.DispRegion(Result[index + 1]);

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
            if (image != null)
            {
                display.AttachBackgroundToWindow(image);
                foreach (Region region in region_collection.regions)
                {
                    AddRegion(region);
                }
                if (region_collection.regions.Count > 0)
                {
                    Selected_region = region_collection.regions[0];
                }
                window_display.SetFullImagePart(image);
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

                if (display != null)
                {
                    Run();
                }
            }
        }

        private void SpinEdit_EditValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (line_pos != null & line_neg != null)
            {
                line_pos.Value = UpperValue;
                line_neg.Value = LowerValue;
                if (display != null)
                {
                    Run();
                }
            }
        }

        private void range_slider_ValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {

        }

        private void btn_add_rect_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange1(false));
        }

        private void btn_add_rect2_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }

        private void btn_add_ellipse_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nodes.Ellipse(false));
        }

        private void btn_add_circle_Click(object sender, RoutedEventArgs e)
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

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            window_display.HalconWindow.SetWindowParam("background_color", "white");
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            window_display.HalconWindow.SetWindowParam("background_color", "black");
        }

        private void btn_add_nurb_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nodes.Nurbs(false));
        }





        private void btn_remove_Click(object sender, RoutedEventArgs e)
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

                if (e.Row > 0 & e.Column > 0 & e.Row <= imH & e.Column <= imW)
                    lb_gray_value.Content = Image?.GetGrayval((int)e.Row, (int)e.Column);
                else
                    lb_gray_value.Content = 0;
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }
    
    public class BlobMask
    {
        public HRegion RunInside(HImage original_image, HHomMat2D hom)
        {
            if (original_image == null) { return null; }
            HImage image = GetImageChannel(original_image);
            HRegion region_inspection = _regions.Region;
            HRegion region = hom != null ? hom.AffineTransRegion(region_inspection, "nearest_neighbor") : region_inspection;
            HRegion result = new HRegion();
            result.GenEmptyRegion();

            if (Invert)
            {
                result = region.Difference(image.ReduceDomain(region).Threshold(LowerValue, UpperValue)).ClosingCircle(Closing);
            }
            else
            {
                result = image.ReduceDomain(region).Threshold(LowerValue, UpperValue).ClosingCircle(Closing);
            }
            if (IsFill)
            {
                result = result.FillUp();
            }
            result = result.Connection().SelectShape("area", "and", MinArea, double.MaxValue);

            return result;
        }
        public HImage GetImageChannel(HImage image_original)
        {
            if (image_original == null)
            {
                return null;
            }
            HImage image;
            if (Channel == 0)
            {
                image = image_original.Rgb1ToGray();
            }
            else
            {
                HTuple channels = image_original.CountChannels();
                if (channels > (int)Channel)
                    image = image_original.AccessChannel((int)Channel);
                else
                {
                    image = image_original.AccessChannel(channels);
                }
            }
            return image;
        }
        public HImage Image { get; set; }
        public double LowerValue { get; set; } = 0;
        public double UpperValue { get; set; } = 255;
        public int MinArea { get; set; } = 100;
        public int MaxArea { get; set; } = 9999999;
        public bool IsFill { get; set; } = false;
        public bool Invert { get; set; } = false;
        public double Closing { get; set; } = 1;
        public bool DisplayArea { get; set; } = true;
        public bool DisplayBlobName { get; set; } = true;
        public string BlobName { get; set; } = "";
        public int MaxBlobCount { get; set; } = 20;
        public string DisplayColor { get; set; } = "#00ff00ff";
        public bool DisplayIndividual { get; set; } = false;
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        CollectionOfregion _regions = new CollectionOfregion();
        public CollectionOfregion Regions
        {
            get
            {
                return _regions;
            }
            set
            {
                if (_regions != value)
                {
                    _regions = value;
                    //RaisePropertyChanged("Regions");
                }
            }
        }
        public ImageChannel Channel { get; set; }
    }
   
}
