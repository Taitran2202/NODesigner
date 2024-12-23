using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using NOVisionDesigner.Designer.Deeplearning.SegmentAnything;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for SegmentationWindow.xaml
    /// </summary>
    public partial class SegmentationWindow : ThemedWindow, INotifyPropertyChanged
    {
        private bool[] _modeArray = new bool[] { true, false, false,false,false };  //array length equal to number of State enum
        public bool[] ModeArray
        {
            get { return _modeArray; }
        }
        private State _drawing_mode;
        public State DrawingMode
        {
            get { return _drawing_mode; }
            set
            {
                if (_drawing_mode != value)
                {
                    _drawing_mode = value;
                    OnDrawingStateChange(value);
                    RaisePropertyChanged("DrawingMode");
                    
                }
            }
        }

        string _color_mode = "Color";
        public string ColorMode
        {
            get
            {
                return _color_mode;
            }
            set
            {
                if (_color_mode != value)
                {
                    _color_mode = value;
                    RaisePropertyChanged("ColorMode");
                }
            }
        }

        string _precision = "High";
        public string Precision
        {
            get
            {
                return _precision;
            }
            set
            {
                if (_precision != value)
                {
                    _precision = value;
                    RaisePropertyChanged("Precision");
                }
            }
        }
        List<HistogramPoint> _good_histogram = new List<HistogramPoint>();
        public List<HistogramPoint> GoodHistogram
        {
            get
            {
                return _good_histogram;
            }
            set
            {
                if (_good_histogram != value)
                {
                    _good_histogram = value;
                    RaisePropertyChanged("GoodHistogram");
                }
            }
        }
        List<HistogramPoint> _bad_histogram = new List<HistogramPoint>();
        public List<HistogramPoint> BadHistogram
        {
            get
            {
                return _bad_histogram;
            }
            set
            {
                if (_bad_histogram != value)
                {
                    _bad_histogram = value;
                    RaisePropertyChanged("BadHistogram");
                }
            }
        }


        bool _is_trainning;
        public bool IsTrainning
        {
            get
            {
                return _is_trainning;
            }
            set
            {
                if (_is_trainning != value)
                {
                    _is_trainning = value;
                    RaisePropertyChanged("IsTrainning");
                }
            }
        }
        IMaskGeneration annotationGenneration;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string imagedir, annotationdir, datadir;
        ObservableCollection<ImageFilmstrip> _list_image;
        public ObservableCollection<ImageFilmstrip> ListImage
        {
            get
            {
                return _list_image;
            }
            set
            {
                if (_list_image != value)
                {
                    _list_image = value;
                    RaisePropertyChanged("ListImage");
                }
            }
        }
        public void CreateDrawingMarker()
        {
            var image = window_display.HalconWindow.GetWindowBackgroundImage();
            if (image != null)
                if (RegionMaker == null)
                {
                    image.GetImageSize(out HTuple w, out HTuple h);
                    if (CurrentImageMetadata.ROI == null)
                    {
                        CurrentImageMetadata.ROI = new Rect1(0, 0, h, w);
                    }
                    RegionMaker = new RegionMaker()
                    {
                        Region = new Rectange1(false)
                        {
                            row1 = CurrentImageMetadata.ROI.row1,
                            row2 = CurrentImageMetadata.ROI.row2,
                            col1 = CurrentImageMetadata.ROI.col1,
                            col2 = CurrentImageMetadata.ROI.col2,
                        }
                    };
                    RegionMaker.Attach(window_display.HalconWindow, null, OnUpdate, OnSelected);

                }
        }
        public void AttachDrawingMarker()
        {
            CreateDrawingMarker();
        }
    
        public void DetachDrawingMarker()
        {
            if (RegionMaker != null)
            {
                RegionMaker.Region.ClearDrawingData(window_display.HalconWindow);
            }
        }
        public void OnDrawingStateChange(State state)
        {
            switch (state)
            {
                case State.Pan:
                    window_display.HMoveContent = true;
                    stack_brush.Visibility = Visibility.Collapsed;
                    break;
                case State.GradientBrush:
                    window_display.HMoveContent = false;
                    stack_brush.Visibility = Visibility.Visible;
                    DetachDrawingMarker();
                    break;
                case State.Eraser:
                    window_display.HMoveContent = false;
                    stack_brush.Visibility = Visibility.Visible;
                    AttachDrawingMarker();
                    break;
                case State.SmartLabel:
                    window_display.HMoveContent = false;
                    stack_brush.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
            
        }
        
        void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageFilmstrip>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png")| x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new ImageFilmstrip(file));
                }

            }
            ListImage = result;
        }
        Python.TrainSegmentation train = new Python.TrainSegmentation();
        SegmentationNode node;
        HRegion ImageRegion, ROIRegion;
        Rect1 CurrentROI;
        RegionMaker RegionMaker;
        HRegion maskRegion;
        public void DispOverlay()
        {
            if (ImageRegion != null & ROIRegion != null)
            {
                display.ClearWindow();
                maskRegion = ImageRegion.Difference(ROIRegion);
                DispMaskRegion();
                //window_display.HalconWindow.DispRegion(ROIRegion);
            }
        }

        private void DispMaskRegion()
        {
            if (maskRegion != null)
            {
                window_display.HalconWindow.SetDraw("fill");
                window_display.HalconWindow.SetColor(AddOpacity("#00ffff5f", ColorOpacity / 100));
                window_display.HalconWindow.DispRegion(maskRegion);
                window_display.HalconWindow.SetColor("green");
                window_display.HalconWindow.SetDraw("margin");
                window_display.HalconWindow.DispRegion(maskRegion);
            }
            
        }

        public void OnSelected(RegionMaker marker, Region region)
        {

        }
        
        public void OnUpdate(RegionMaker marker, Region region)
        {
            CurrentImageMetadata.ROI.row1 = (int)marker.Region.row1;
            CurrentImageMetadata.ROI.col1 = (int)marker.Region.col1;
            CurrentImageMetadata.ROI.row2 = (int)marker.Region.row2;
            CurrentImageMetadata.ROI.col2 = (int)marker.Region.col2;
            ROIRegion = RegionMaker.Region.region;
            if (ImageRegion != null & ROIRegion != null)
            {
                maskRegion = ImageRegion.Difference(ROIRegion);
            }
            DisplayImageGt(GroundTruth);

        }
        public void CreateROI(Rect1 ROI)
        {
            if (ROI != null)
            {
                ROIRegion = new HRegion((double)ROI.row1, ROI.col1, ROI.row2, ROI.col2);
            }
        }
        HImage CropImage(HImage image, HRegion region)
        {
            region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
            var imageCroped = image.CropRectangle1(Math.Max(0, row1), Math.Max(0, col1), row2, col2);
            imageCroped.GetImageSize(out int w, out int h);
            HRegion diffrg = new HRegion(0, 0.0, h, w);
            var subRg = diffrg.Difference(region.MoveRegion(-Math.Max(row1,0), -Math.Max(0,col1)));
            imageCroped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
            return imageCroped;
        }
        public SegmentationWindow(string imagedir, string annotationdir, string datadir, Action<Action<int>, Action<bool>> TrainFunction, SegmentationNode node)
        {
            InitializeComponent();
            this.node = node;
            this.imagedir = imagedir;
            this.datadir = datadir;
            this.annotationdir = annotationdir;
            btn_add_image_camera.Click += (s, e) =>
            {
                if (node.ImageInput.Value != null)
                {
                    try
                    {
                        var image = node.ImageInput.Value.Clone();
                        var filename = DateTime.Now.Ticks.ToString();
                        var newfile = System.IO.Path.Combine(imagedir, filename);
                        if(node.ROIInput.Value != null)
                        {
                            if (node.ROIInput.Value.Area > 0)
                            {
                                var region = node.ROIInput.Value;
                                CropImage(image, region).WriteImage("bmp", 0, newfile);
                            }
                            else
                            {
                                image.WriteImage("bmp", 0, newfile);
                            }
                            
                        }
                        else
                        {
                            image.WriteImage("bmp", 0, newfile);
                        }
                        
                        ListImage.Add(new ImageFilmstrip(newfile + ".bmp"));

                    }
                    catch (Exception ex)
                    {

                    }
                }
            };
            window_display.HMoveContent = true;
            
            btn_add_image.Click += Btn_add_image_Click;
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                var step = spin_step.Value;
                box_step.Visibility = Visibility.Hidden;
                //this.node.segmentation.
                this.node.TrainConfig.EPOCHS = (int)step;
                this.node.TrainConfig.Save();
                Task.Run(new Action(() =>
                {

                    //Nodes.TrainDirect traindirect = new Nodes.TrainDirect();
                    //traindirect.BuildModelUnet(512, 512);
                    //IsTrainning = true;
                    //traindirect.Train(imagedir, annotationdir, epochs: 100,step_per_epochs:100, TrainPredict: (x, y) =>
                    //  {
                    //      var r = new HImage("byte", x.shape[2], x.shape[1]);

                    //      var g = new HImage("byte", x.shape[2], x.shape[1]);
                    //      var b = new HImage("byte", x.shape[2], x.shape[1]);
                    //      var rgb = r.Compose3(g, b);
                    //      HTuple w, h, pointerR, pointerG, pointerB, type;
                    //      rgb.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out w, out h);

                    //      x["0,:,:,0"].CopyTo(pointerR);
                    //      x["0,:,:,1"].CopyTo(pointerG);
                    //      x["0,:,:,2"].CopyTo(pointerB);

                    //      var imagepredict = new HImage("byte", y.shape[2], y.shape[1]);
                    //      pointerR = imagepredict.GetImagePointer1(out type, out w, out h);
                    //      y.CopyTo(pointerR);

                    //      window_train1.HalconWindow.DispObj(rgb);
                    //      window_train2.HalconWindow.DispObj(imagepredict);
                    //  });
                    //return;
                    IsTrainning = true;

                    train.TrainPython(this.node.TrainConfig.ConfigDir, (trainargs) =>
                    {
                        if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                        {
                            IsTrainning = false;
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                                DXMessageBox.Show(this, "Train cancel or error:"+ trainargs.ErrorLog,"train",MessageBoxButton.OK,MessageBoxImage.Error))
                                );
                        }
                        if (trainargs.State == Python.TrainState.Completed)
                        {
                            IsTrainning = false;
                            PostTrain();
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>

                                    DXMessageBox.Show(this, "Train complete", "train", MessageBoxButton.OK, MessageBoxImage.Information))

                                );

                        }
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (trainargs.Progress);
                            txt_acc.Content = trainargs.Accuracy.ToString();
                        }));


                    });


                }));

            };
            LoadImageList(imagedir);
            this.DataContext = this;
            box_step.DataContext = this.node.TrainConfig;
            stack_train_options.DataContext = this.node.TrainConfig;
            stack_augmentation.DataContext = this.node.TrainConfig.AugmentationSetting;
            LoadHistogram();
        }

        public void PostTrain()
        {
            node.segmentation.LoadRecipe(node.segmentation.ModelDir);

            //reload histogram

            LoadHistogram();

        }
        
        private void LoadHistogram()
        {
            var histogramDir = System.IO.Path.Combine(node.segmentation.ModelDir, "histogram.json");
            if (System.IO.File.Exists(histogramDir))
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<Histogram>(File.ReadAllText(histogramDir));
                    var listHistogramGood = new List<HistogramPoint>();
                    var listHistogramBad = new List<HistogramPoint>();
                    for(int i=0;i<data.good.Count(); i++)
                    {
                        listHistogramGood.Add(new HistogramPoint() { Index = i, Value = data.good[i] });
                    }
                    for (int i = 0; i < data.bad.Count(); i++)
                    {
                        listHistogramBad.Add(new HistogramPoint() { Index = i, Value = data.bad[i] });
                    }
                    GoodHistogram = listHistogramGood;
                    BadHistogram = listHistogramBad;
                }
                catch (Exception ex)
                {

                }

            }
        }
        

        public class HistogramPoint
        {
            public int Index { get; set; }
            public double Value { get; set; }
        }
        public class Histogram
        {
            public double[] good { get; set; }
            public double[] bad { get; set; }
        }
        private void Btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.GIF|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                foreach (var file in files)
                {
                    try
                    {


                        var filename = System.IO.Path.GetFileName(file);
                        var newfile = System.IO.Path.Combine(imagedir, filename);
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageFilmstrip(newfile));
                    }
                    catch (Exception ex)
                    {

                    }
                }

            }
        }

        void Progress(int progress)
        {

        }
        void TrainResult(bool result)
        {

        }
        bool _is_margin = true;
        public bool IsMargin
        {
            get
            {
                return _is_margin;
            }
            set
            {
                if (_is_margin != value)
                {
                    _is_margin = value;
                    if (value)
                    {
                        display?.SetDraw("fill");
                    }
                    else
                    {

                        display?.SetDraw("margin");
                    }

                    RaisePropertyChanged("IsMargin");
                }
            }
        }

        public ClassInfo SelectClass = new ClassInfo() { ClassID = 255, Color = "#00ff00ff" };
        double _color_opacity = 20;
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
        string _image_name;
        public string ImageName
        {
            get
            {
                return _image_name;
            }
            set
            {
                if (_image_name != value)
                {
                    _image_name = value;
                    RaisePropertyChanged("ImageName");
                }
            }
        }

        string _color_good = "#00ff0011";
        public string ColorGood
        {
            get
            {
                return _color_good;
            }
            set
            {
                if (_color_good != value)
                {
                    _color_good = value;
                    RaisePropertyChanged("ColorGood");
                }
            }
        }
        string _color_bad = "#ff000055";
        public string ColorBad
        {
            get
            {
                return _color_bad;
            }
            set
            {
                if (_color_bad != value)
                {
                    _color_bad = value;
                    RaisePropertyChanged("ColorBad");
                }
            }
        }
        public double Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    RaisePropertyChanged("Scale");
                }
            }
        }
        double _smooth = 1;
        public double Smooth
        {
            get
            {
                return _smooth;
            }
            set
            {
                if (_smooth != value)
                {
                    _smooth = value;
                    RaisePropertyChanged("Smooth");
                }
            }
        }

        int _brush_size = 10;
        public int BrushSize
        {
            get
            {
                return _brush_size;
            }
            set
            {
                if (_brush_size != value)
                {
                    _brush_size = value;
                    RaisePropertyChanged("BrushSize");
                }
            }
        }
        double row, col;
        double row_start, col_start;
        bool enter_state = false;
        HImage GroundTruth;
        string GroundTruthPath;
        ImageMetaData CurrentImageMetadata;
        public void LoadMetaData(string file)
        {
            if (System.IO.File.Exists(file))
            {
                CurrentImageMetadata = JsonConvert.DeserializeObject<ImageMetaData>(File.ReadAllText(file));
            }
            else
            {
                CurrentImageMetadata = new ImageMetaData();
            }
            
        }
        public void SaveMetaData(string file)
        {
            if (CurrentImageMetadata != null & RegionMaker!=null)
            {
                CurrentImageMetadata.ROI = new Rect1(RegionMaker.Region.row1, RegionMaker.Region.col1, RegionMaker.Region.row2, RegionMaker.Region.col2);

                File.WriteAllText(file, JsonConvert.SerializeObject(CurrentImageMetadata));
            }
            
        }
        private void btn_edit_roi_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            DrawingMode = State.Pan;
            var image = window_display.HalconWindow.GetWindowBackgroundImage();
            if (image != null)
                if (RegionMaker == null)
                {


                    image.GetImageSize(out HTuple w, out HTuple h);
                    if (CurrentImageMetadata.ROI == null)
                    {
                        CurrentImageMetadata.ROI = new Rect1(0, 0, h, w);
                    }
                    RegionMaker = new RegionMaker()
                    {
                        Region = new Rectange1(false)
                        {
                            row1 = CurrentImageMetadata.ROI.row1,
                            row2 = CurrentImageMetadata.ROI.row2,
                            col1 = CurrentImageMetadata.ROI.col1,
                            col2 = CurrentImageMetadata.ROI.col2,
                        }
                    };
                    RegionMaker.Attach(window_display.HalconWindow, null, OnUpdate, OnSelected);
                    
                }
                else
                {
                    if (RegionMaker.Region.current_draw == null)
                    {
                        image.GetImageSize(out HTuple w, out HTuple h);
                        if (CurrentImageMetadata.ROI == null)
                        {
                            CurrentImageMetadata.ROI = new Rect1(0, 0, h, w);
                        }
                        RegionMaker = new RegionMaker()
                        {
                            Region = new Rectange1(false)
                            {
                                row1 = CurrentImageMetadata.ROI.row1,
                                row2 = CurrentImageMetadata.ROI.row2,
                                col1 = CurrentImageMetadata.ROI.col1,
                                col2 = CurrentImageMetadata.ROI.col2,
                            }
                        };
                        RegionMaker.Attach(window_display.HalconWindow, null, OnUpdate, OnSelected);
                    }
                }
            
        }
        public void UpdateMarker(Rect1 ROI)
        {
            if (RegionMaker == null)
            {
                return;
            }
            if(ROI == null)
            {
                return;
            }
            RegionMaker.Region.row1 = ROI.row1;
            RegionMaker.Region.row2 = ROI.row2;
            RegionMaker.Region.col1 = ROI.col1;
            RegionMaker.Region.col2 = ROI.col2;
            RegionMaker.Region.current_draw?.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple(ROI.row1,ROI.col1,ROI.row2,ROI.col2));
        }

        public void SaveResult()
        {
            GroundTruth?.WriteImage("png", 0, GroundTruthPath);
            SaveMetaData(GroundTruthPath + ".txt");
        }
        //color are define with format "#rrggbbaa".
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public void DisplayImageGt(HImage image)
        {
            if (image == null)
                return;
            if (display == null)
            {
                return;
            }
            display.ClearWindow();
            var region = image.Threshold(1, 255.0);
            display.SetColor(AddOpacity(SelectClass.Color, ColorOpacity / 100));
            display.SetDraw("fill");
            display.DispObj(region);
            display.SetColor(SelectClass.Color);
            display.SetDraw("margin");
            display.SetLineWidth(2);
            display.DispObj(region);
            DispMaskRegion();
            //DispOverlay();
        }
        private void btn_pre_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {

        }
        int im_w, im_h;
        public void LoadResultMask(ImageFilmstrip selected)
        {

        }
        private void SetSelectedImage(ImageFilmstrip selected)
        {
            if (selected != null)
            {
                //save previous result
                try
                {
                    SaveResult();
                }
                catch (Exception ex)
                {

                }
                //load current image
                try
                {
                    image = new HImage(selected.FullPath);
                    image.GetImageSize(out im_w, out im_h);
                    ImageRegion = new HRegion(0, 0.0, im_h, im_w);
                    window_display.HalconWindow?.AttachBackgroundToWindow(image);
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
                try
                {
                    ImageName = System.IO.Path.GetFileNameWithoutExtension(selected.FullPath);
                    GroundTruthPath = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileNameWithoutExtension(selected.FullPath));
                    LoadMetaData(GroundTruthPath + ".txt");
                    if (CurrentImageMetadata.ROI == null)
                    {
                        if(RegionMaker!=null)
                        RegionMaker.Region.ClearDrawingData(display);
                    }
                    UpdateMarker(CurrentImageMetadata.ROI);
                    CreateROI(CurrentImageMetadata.ROI);
                    if (ImageRegion != null & ROIRegion != null)
                    {
                        maskRegion = ImageRegion.Difference(ROIRegion);
                    }
                    if (System.IO.File.Exists(GroundTruthPath + ".png"))
                    {
                        try
                        {
                            GroundTruth = new HImage(GroundTruthPath);
                            DisplayImageGt(GroundTruth);
                        }
                        catch (Exception ex)
                        {
                            GroundTruth = new HImage("byte", im_w, im_h);
                        }

                    }
                    else
                    {
                        GroundTruth = new HImage("byte", im_w, im_h);
                    }

                    
                    
                }
                catch (Exception ex)
                {

                }
                

            }
        }
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                if (e.AddedItems.Count == 0)
                {

                    return;
                }
                var selected = e.AddedItems[0] as ImageFilmstrip;
                SetSelectedImage(selected);

            }
            catch
            {
                return;
            }

            
        }

        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {

        }

        private void window_display_HMouseUp(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            enter_state = false;
        }

        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {
            DisplayImageGt(GroundTruth);
        }

        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            //if (DrawingMode == State.SmartLabel)
            //{
            //    if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
            //    {
            //        if (SmartLabel != null)
            //        {
            //            //SmartLabel.SetImage(image);
            //            var result= SmartLabel.RunDecoder((float)e.Column, (float)e.Row);
            //            try
            //            {
            //                result.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
            //                HXLDCont cont = new HXLDCont();
            //                cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
            //                var region = cont.GenRegionContourXld("filled");
            //                display.DispObj(region);
            //            }catch(Exception ex)
            //            {

            //            }
                        
            //        }
            //    }
            //    return;
            //}

            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
            {
                //window_display.HMouseUp += window_display_HMouseUp;
                //window_display.HMouseUp -= Window_display_HMouseUp_pan;
                enter_state = true;
                HRegion region_gen = new HRegion();
                switch (_drawing_mode)
                {
                    case State.Pan: break;
                    case State.SolidBrush: break;
                    case State.GradientBrush:

                        enter_state = false;
                        // region_undo.Push(region_base);
                        region_gen.GenCircle(e.Row, e.Column, BrushSize);
                        if (SelectClass != null)
                            GroundTruth?.OverpaintRegion(region_gen, new HTuple(SelectClass.ClassID), new HTuple("fill"));

                        //  region_base = region_base.Union2(region_gen);

                        DisplayImageGt(GroundTruth);
                        // display.DispObj(region_gen);

                        break;
                    case State.Eraser:


                        // region_undo.Push(region_base);
                        region_gen.GenCircle(e.Row, e.Column, BrushSize);
                        GroundTruth.OverpaintRegion(region_gen, new HTuple(0), new HTuple("fill"));
                        //region_base = region_base.Difference(region_gen);
                        DisplayImageGt(GroundTruth);


                        display.DispObj(region_gen);
                        break;
                    default:
                        // region_undo.Push(region_base);
                        break;
                }
            }
            row_start = e.Row;
            col_start = e.Column;
        }

        private void window_display_HMouseMove(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            
            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
                switch (_drawing_mode)
                {
                    case State.Move:
                        break;
                        //region_base = region_base.MoveRegion((int)(e.Row - row_start), (int)(e.Column - col_start));
                        //row_start = e.Row;
                        //col_start = e.Column;
                        //display.ClearWindow();
                        //DispOverlay();

                        break;
                    case State.Pan: break;
                    case State.SolidBrush: break;
                    case State.GradientBrush:
                        if (enter_state)
                        {
                            enter_state = false;
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row_start, e.Row), new HTuple(col_start, e.Column));
                            GroundTruth.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(SelectClass.ClassID), "fill");
                            //region_base = region_base.Union2(region_gen.DilationCircle((double)BrushSize));
                            //  display.ClearWindow();
                            DisplayImageGt(GroundTruth);

                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            GroundTruth.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(SelectClass.ClassID), "fill");
                            // region_base = region_base.Union2(region_gen.DilationCircle((double)BrushSize));
                            //display.ClearWindow();
                            DisplayImageGt(GroundTruth);


                        }

                        HRegion region_disp = new HRegion();
                        region_disp.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_disp);

                        break;
                    case State.Eraser:
                        if (enter_state)
                        {
                            enter_state = false;
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row_start, e.Row), new HTuple(col_start, e.Column));
                            GroundTruth.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(0), "fill");
                            //region_base = region_base.Difference(region_gen.DilationCircle((double)BrushSize));
                            //  display.ClearWindow();
                            DisplayImageGt(GroundTruth);
                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            GroundTruth.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(0), "fill");
                            DisplayImageGt(GroundTruth);
                        }

                        HRegion region_erase = new HRegion();
                        region_erase.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_erase);
                        break;
                }
            else
            {
                switch (_drawing_mode)
                {
                    case State.Move: break;
                    case State.Pan:
                        break;
                    default:



                        //display.ClearWindow();
                        DisplayImageGt(GroundTruth);
                        HRegion region_disp = new HRegion();
                        region_disp.GenCircle(e.Row, e.Column, BrushSize);
                        if (SelectClass != null)
                            display.SetColor(SelectClass.Color);
                        display.DispObj(region_disp);
                        break;

                }
            }

            row = e.Row;
            col = e.Column;

        }
        HWindow display;
        HImage image_mask;
        HImage image;
        HHomMat2D transform;
        HTuple w, h;

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageFilmstrip;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListImage.Remove(vm);

                    var imagegtpath = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileNameWithoutExtension(vm.FullPath));
                    if (System.IO.File.Exists(imagegtpath + ".png"))
                    {
                        System.IO.File.Delete(imagegtpath + ".png");
                    }
                }
                catch (Exception ex)
                {

                }

            }

        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            train.Cancel();
        }

        private void btn_clear_roi(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void train_console_Click(object sender, RoutedEventArgs e)
        {
            var step = spin_step.Value;
            box_step.Visibility = Visibility.Hidden;
            this.node.TrainConfig.EPOCHS = (int)step;
            this.node.TrainConfig.Save();
            train.TrainConsole(this.node.TrainConfig.ConfigDir, "unet", (TrainingArgs) =>
            {
                if (TrainingArgs.State == TrainState.Completed)
                {

                    PostTrain();
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        DXMessageBox.Show(this, "Train Complete!", "Message");
                    }));
                }
            });

        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveResult();
                border_message.Visibility = Visibility.Visible;
                Task.Run(() =>
                {
                    Thread.Sleep(500);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        border_message.Visibility = Visibility.Collapsed;
                    });
                    
                });
                
            }
            catch(Exception ex)
            {
                DXMessageBox.Show(this, "Save", "Error saving annotation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void btn_annotation_editor_Click(object sender, RoutedEventArgs e)
        {
            var regions = new CollectionOfregion();
            WindowRegionWindowInteractive wd = new WindowRegionWindowInteractive(image, regions, null, true);
            if (wd.ShowDialog() == true)
            {
                GroundTruth?.OverpaintRegion(regions.Region, new HTuple(SelectClass.ClassID), new HTuple("fill"));
            }
            else
            {
                
            }
        }

        private void btn_smart_labeling_Click(object sender, RoutedEventArgs e)
        {
            Deeplearning.Windows.SmartLabelWindow wd = new Deeplearning.Windows.SmartLabelWindow(image);
            if (wd.ShowDialog() == true)
            {
                try
                {
                    if (wd.Regions.regions != null)
                    {
                        wd.Regions.MergeRegion();
                        if (SelectClass != null)
                            GroundTruth?.OverpaintRegion(wd.Regions.Region, new HTuple(SelectClass.ClassID), new HTuple("fill"));
                        DisplayImageGt(GroundTruth);
                    }
                }catch(Exception ex)
                {

                }

            }
        }

        private void btn_train_config_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
            wd.ShowDialog();
            node.TrainConfig.Save();
        }

        double _scale = 1;
        string _color_draw = "#00ff00aa";
        public string ColorDraw
        {
            get
            {
                return _color_draw;
            }
            set
            {
                if (_color_draw != value)
                {
                    _color_draw = value;
                    display?.SetColor(value);
                    RaisePropertyChanged("ColorDraw");
                }
            }
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("fill");
                display.SetColor(ColorDraw);
                if (ListImage.Count > 0)
                {
                    lst_view.SelectedIndex = 0;
                }
                
                //display.AttachBackgroundToWindow(image);
                // DispOverlay();
                // display.DispObj(image_mask);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class ClassInfo
    {
        public int ClassID { get; set; } = 0;
        public string ClassName { get; set; } = "un_defined";
        public string Color { get; set; } = "#ff000011";
    }
    
    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int integer = (int)value;
            if (integer == int.Parse(parameter.ToString()))
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if((bool)value == false)
            {
                return Binding.DoNothing;
            }
            return (State)int.Parse(parameter.ToString());
        }
    }
}