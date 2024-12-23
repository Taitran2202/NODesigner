using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
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
using DevExpress.Xpf.Charts;
using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for BlobDetectionView.xaml
    /// </summary>
    public partial class BlobDetectionView : UserControl,IViewFor<BlobDetection>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(BlobDetection), typeof(BlobDetectionView), new PropertyMetadata(null));

        public BlobDetection ViewModel
        {
            get => (BlobDetection)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (BlobDetection)value;
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
                }
            }
        }
        public Region SelectedRegion { get; set; }
        public BlobDetection Model { get; set; }
        public HHomMat2D Fixture { get; set; }
        
        public List<ImageChannel> channels = new List<ImageChannel>() { ImageChannel.Gray, ImageChannel.Red, ImageChannel.Green, ImageChannel.Blue };
        public ObservableCollection<BlobResults> BlobResults { get; set; } = new ObservableCollection<BlobResults>();
        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        HWindow display = null;
        public int ImageWidth;
        public int ImageHeight;
        HRegion Result;
        private HImage _image;
        public HImage Image
        {
            get => _image;
            set
            {
                _image = value;
                _image?.GetImageSize(out ImageWidth, out ImageHeight);
                if (display != null & value != null)
                {
                    display.DetachBackgroundFromWindow();
                    display.AttachBackgroundToWindow(value);
                }
            }
        }

        public BlobDetectionView()
        {
            InitializeComponent();
        }
        public void Initialize(HImage Image, HHomMat2D Fixture)
        {
            this.Image = Image;
            this.Fixture = Fixture;

            this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel.MinArea,
                    v=> v.ViewModel.MaxArea,
                    v => v.ViewModel.LowerValue,
                    v => v.ViewModel.UpperValue,
                    v => v.ViewModel.Closing,
                    v => v.ViewModel.IsFill,
                    v => v.ViewModel.Invert
                    ).Subscribe(x =>
                    {
                        Run();
                    }).DisposeWith(d);
            });
        }
        #region Method
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
                series.Points.Clear();
                series.Points.AddRange(Points);
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
            display.SetColor(AddOpacity(ViewModel.RegionColor, ColorOpacity / 100));
            if (Result != null)
            {
                display.DispRegion(Result);
            }
            display.SetColor("blue");
            display.SetDraw("margin");
            display.SetLineStyle(new HTuple(20, 7));
            display.DispRegion(ViewModel.Regions.Region);
        }
        private void Update(Region sender)
        {
            SelectedRegion = sender;
            ChangeRegion();
            Run();
        }
        public HTuple Histogram()
        {
            HRegion region_inspection = SelectedRegion.region;
            if (Fixture != null)
            {
                region_inspection = Fixture.AffineTransRegion(region_inspection, "nearest_neighbor");
            }
            HTuple relative;
            Image.GrayHisto(region_inspection, out relative);
            return relative;
        }

        public void Run()
        {
            if (Image == null)
            {
                //display region boundary
                display.ClearWindow();
                display.SetDraw("margin");
                display.SetLineStyle(new HTuple(20, 7));
                display.DispRegion(ViewModel.Regions.Region);
                return;
            }
            HRegion RegionInspect = ViewModel.Regions.Region;
            HRegion regionInspectTransform = Fixture != null ? Fixture.AffineTransRegion(RegionInspect, "nearest_neighbor") : RegionInspect;
            Result = ViewModel.RunInside(Image, regionInspectTransform);
            BlobResults.Clear();
            for (int i = 0; i < Result.CountObj(); i++)
            {
                BlobResults.Add(new BlobResults(Result[i + 1], i));
            }
            UpdatedDisplay();
        }
        private void AddRegionNew(Region region,int row,int col)
        {

            region.Initial((int)row, (int)col);

            Region region_add = region;
            ViewModel.Regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(Fixture);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            Update(region_add);
        }
        private void AddRegion(Region region)
        {
            Region region_add = region;
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(Fixture);
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
            ViewModel.Regions.MergeRegion();
            //DispOverlay();
        }
        #endregion

        private void window_display_HInitWindow(object sender, EventArgs e)
        {

        }

        private void window_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void Cmb_channels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void Setting_Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
