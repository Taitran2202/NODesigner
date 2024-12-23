using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using HalconDotNet;
using Microsoft.Win32;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Controls;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;
using ReactiveUI;
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
    public partial class LDCEdgeDetectionWindow : ThemedWindow, INotifyPropertyChanged
    {

        TrainningStatusViewModel trainningStatusViewModel;
        EvaluationViewModel EvalViewModel = new EvaluationViewModel();
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

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        ObservableCollection<ImageSet> _list_image;
        public ObservableCollection<ImageSet> ListImage
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
            return;
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
            //switch (state)
            //{
            //    case State.Pan:
            //        window_display.HMoveContent = true;

            //        break;
            //    case State.GradientBrush:
            //        window_display.HMoveContent = false;
            //        DetachDrawingMarker();
            //        break;
            //    case State.Eraser:
            //        window_display.HMoveContent = false;
            //        AttachDrawingMarker();
            //        break;
            //    default:
            //        break;
            //}

        }
        void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageSet>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new ImageSet(file));
                }

            }
            ListImage = result;
        }
        Python.TrainEdgeDetection train = new Python.TrainEdgeDetection();
        LDCEdgeDetection node;
        HRegion ImageRegion, ROIRegion;
        Rect1 CurrentROI;
        RegionMaker RegionMaker;
        HRegion maskRegion;
        public void DispOverlay()
        {
            if (ImageRegion != null & ROIRegion != null)
            {
                display.ClearWindow();
                //maskRegion = ImageRegion.Difference(ROIRegion);
                DispMaskRegion();
                //window_display.HalconWindow.DispRegion(ROIRegion);
            }
        }

        private void DispMaskRegion()
        {
            window_display.HalconWindow.ClearWindow();

                window_display.HalconWindow.SetDraw("fill");
                window_display.HalconWindow.SetColor(AddOpacity("#00ff00ff", ColorOpacity / 100));
                //window_display.HalconWindow.DispRegion(maskRegion);
                
                window_display.HalconWindow.SetColor(AddOpacity("#ff0000ff", ColorOpacity / 100));
                if (annotationRegion != null)
                {
                    window_display.HalconWindow.DispRegion(annotationRegion);
                }

                window_display.HalconWindow.SetDraw("margin");
                window_display.HalconWindow.SetColor("red");
                if (annotationRegion != null)
                {
                    window_display.HalconWindow.DispRegion(annotationRegion);
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
                //maskRegion = ImageRegion.Difference(ROIRegion);
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
        public LDCEdgeDetectionWindow(LDCEdgeDetection node)
        {
            this.node = node;
            ContourGenerator = new ManualContourGeneration(node.TrainConfig.MASK_DIR);
            annotationGenneration = new ManualContourGeneration(node.TrainConfig.ANNOTATION_DIR);
            InitializeComponent();
            this.DataContext = this;
            //grid_model_prop.DataContext = node.TrainConfig;
            var Properties = new ObservableCollection<Property>();
            grid_model_prop.SelectedObject = node.TrainConfig;
            Properties.Add(new Property() { Name = "LR_TYPE" });
            Properties.Add(new Property() { Name = "LR_STEPS" });
            Properties.Add(new Property() { Name = "LR_INIT" });
            Properties.Add(new Property() { Name = "lmbda" });
            Properties.Add(new Property() { Name = "THRESHOLD" });
            Properties.Add(new Property() { Name = "OPTIMIZER" });
            Properties.Add(new Property() { Name = "PATIENCE" }); 
            grid_model_prop.PropertyDefinitionsSource = Properties;
            propertiesGrid.SelectedObject = this.node.Runtime;
            this.stack_train_parameter.DataContext = node.TrainConfig;
            stack_augmentation.DataContext = node.TrainConfig.Augmentations;
            btn_add_image_camera.Click += (s, e) =>
            {
                if (node.ImageIn.Value != null)
                {
                    try
                    {
                        var image = node.ImageIn.Value.Clone();
                        string newfile = Functions.NewFileName(node.TrainConfig.IMAGE_DIR, "png", "image");                       
                        if (node.RegionIn.Value != null)
                        {
                            var region = node.RegionIn.Value.Clone();
                            Functions.CropImageWithRegion(image, region).WriteImage("png", 0, newfile);
                        }
                        else
                        {
                            image.WriteImage("png", 0, newfile);
                        }

                        ListImage.Add(new ImageSet(newfile));

                    }
                    catch (Exception ex)
                    {

                    }
                }
            };
            //window_display.HMoveContent = true;

            btn_add_image.Click += Btn_add_image_Click;
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_train.Click += (sender, e) =>
            {
                //box_step.Visibility = Visibility.Visible;
                if(TrainModeSelection())
                    StartTrainning();

            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                
                box_step.Visibility = Visibility.Hidden;
                
                this.node.TrainConfig.Save();
                try
                {
                    NOVisionPython.TrainConsole("segmentation", "LDC", node.TrainConfig.ConfigDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                

            };
            btn_stop_train.Click += (sender, e) =>
            {
                train?.Cancel();
            };
            LoadImageList(node.TrainConfig.IMAGE_DIR);
            this.DataContext = this;
            box_step.DataContext = this.node.TrainConfig;
            stack_train_options.DataContext = this.node.TrainConfig;
            tab_eval.DataContext = EvalViewModel;
            tab_eval.Loaded += Tab_eval_Loaded;
            //HOperatorSet.SetSystem("int_zooming", "false");
        }
        bool eval_loaded = false;
        private void Tab_eval_Loaded(object sender, RoutedEventArgs e)
        {
            if (!eval_loaded)
            {
                LoadLastEvaluationResult();
                tab_eval.DataContext = EvalViewModel;
                eval_loaded = true;
            }
            
        }
        bool TrainModeSelection()
        {
            var bestScore = System.IO.Path.Combine(node.TrainConfig.LOG_DIR, "best_score.json");
            var latestScore = System.IO.Path.Combine(node.TrainConfig.LOG_DIR, "latest_score.json");
            var LatestCheckPoint = new EdgeDetectionCheckpointInfo();
            var BestCheckPoint = new EdgeDetectionCheckpointInfo();
            try
            {
                JObject latestScoreData = JObject.Parse(File.ReadAllText(latestScore));
                LatestCheckPoint.Epoch = latestScoreData["latest_step"].ToObject<int>();
                LatestCheckPoint.Loss = Math.Round(latestScoreData["latest_loss"].ToObject<double>(),4);
            }
            catch (Exception ex)
            {

            }
            try
            {
                JObject bestScoreData = JObject.Parse(File.ReadAllText(bestScore));
                BestCheckPoint.Epoch = bestScoreData["best_step"].ToObject<int>();
                BestCheckPoint.Loss = Math.Round(bestScoreData["best_loss"].ToObject<double>(),4);
            }
            catch (Exception ex)
            {

            }

            TrainModeSelectionWindow wd = new TrainModeSelectionWindow(LatestCheckPoint, BestCheckPoint);
            wd.Owner = this;
            if (wd.ShowDialog() == true)
            {
                if (wd.TrainType == TrainResume.New)
                {
                    node.TrainConfig.TRAINING_TYPE = wd.TrainType;
                }
                else
                {
                    node.TrainConfig.TRAINING_TYPE = wd.TrainType;
                    node.TrainConfig.CheckPoint = wd.CheckPoint;
                }
                return true;
            }
            else
            {
                return false;
            }

        }
        private void Btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.PNG;*.TIFF;*.GIF|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                foreach (var file in files)
                {
                    try
                    {


                        var filename = System.IO.Path.GetFileName(file);
                        var newfile = System.IO.Path.Combine(node.TrainConfig.IMAGE_DIR, filename);
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageSet(newfile));
                    }
                    catch (Exception ex)
                    {

                    }
                }

            }
        }

        

        public ClassInfo SelectClass = new ClassInfo() { ClassID = 255, Color = "#00ff00ff" };
        double _color_opacity = 50;
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
            if (CurrentImageMetadata != null & RegionMaker != null)
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
            if (ROI == null)
            {
                return;
            }
            RegionMaker.Region.row1 = ROI.row1;
            RegionMaker.Region.row2 = ROI.row2;
            RegionMaker.Region.col1 = ROI.col1;
            RegionMaker.Region.col2 = ROI.col2;
            RegionMaker.Region.current_draw?.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple(ROI.row1, ROI.col1, ROI.row2, ROI.col2));
        }
        private bool _isTraining;

        public bool IsTraining
        {
            get { return _isTraining; }
            set
            {
                if (_isTraining != value)
                {
                    _isTraining = value;
                    RaisePropertyChanged("IsTraining");
                }
            }
        }
        public void LoadLastEvaluationResult()
        {
            try
            {
                string resultDir = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "result.txt");
                if (File.Exists(resultDir))
                {
                    EvalViewModel = JsonConvert.DeserializeObject<EvaluationViewModel>(File.ReadAllText(resultDir));
                    //tab_eval.DataContext = EvalViewModel;
                    
                }
            }
            catch (Exception ex)
            {

            }

        }
        public void StartEvaluation()
        {
            Task.Run(() =>
            {
                EvalViewModel.IsEvaluation = true;
                if (node.Runtime.State == ModelState.Unloaded)
                {
                    node.Runtime.LoadRecipe();
                }
                string image_directory = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "images");

                string edge_directory = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "edges");
                string resultDir = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "result.txt");
                if (!System.IO.Directory.Exists(image_directory))
                {
                    System.IO.Directory.CreateDirectory(image_directory);
                }
                if (!System.IO.Directory.Exists(edge_directory))
                {
                    System.IO.Directory.CreateDirectory(edge_directory);
                }
                List<ClassificationResult> classificationResult = new List<ClassificationResult>();
                var imageFiles = Directory.GetFiles(node.TrainConfig.IMAGE_DIR).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                
                int total = imageFiles.Count();
                EvalViewModel.TotalSamples = total;              
                int step = 0;
                foreach (var ImageFile in imageFiles)
                {
                    if (EvalViewModel._is_cancel)
                    {
                        EvalViewModel._is_cancel = false;
                        EvalViewModel.IsEvaluation = false;
                        return;
                    }
                    string edgePath=System.IO.Path.Combine(edge_directory, System.IO.Path.GetFileNameWithoutExtension(ImageFile)+".png");
                    
                    HImage image= new HImage(ImageFile);
                    if (node.Runtime.State == ModelState.Loaded)
                    {
                        var edgeImage = node.Runtime.Infer(image);
                        edgeImage.WriteImage("png", 0, edgePath);
                    }
                    classificationResult.Add(new ClassificationResult()
                    {
                        FileName = System.IO.Path.GetFileName(ImageFile),
                        Image = ImageFile,
                        Label = "good",
                        Predict = "good",
                        Probability = 1,
                        PredictImage = edgePath
                    });

                    step++;
                    EvalViewModel.EvaluationProgress = Math.Round(((double)step / total) * 100, 0);
                }

                EvalViewModel.ClassificationResult = classificationResult;
                System.IO.File.WriteAllText(resultDir, JsonConvert.SerializeObject(EvalViewModel, Formatting.Indented));
                EvalViewModel.IsEvaluation = false;
            });


        }
        public void StartTrainning()
        {
            trainningStatusViewModel?.Dispose();
            trainningStatusViewModel = new TrainningStatusViewModel();
            grid_trainning_status.DataContext = trainningStatusViewModel;
            Task.Run(new Action(() =>
            {
                IsTraining = true;
                node.TrainConfig.Save();
                trainningStatusViewModel.StartListen();
                train.TrainPython(node.TrainConfig.ConfigDir, "LDC", (trainargs) =>
                {

                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTraining = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Training was cancel because of error!", "Warning",
                                MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTraining = false;
                        node.Runtime.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Train complete!", "Info",
                                MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }

                });
            }));
        }
        public void PostTrain()
        {
            //LoadHistogram();
            node.Runtime.State = ModelState.Unloaded;
            Application.Current.Dispatcher.Invoke(() =>
            {
                propertiesGrid.SelectedObject = null;
                propertiesGrid.SelectedObject = node.Runtime;
            });

        }
        public void SaveResult()
        {
            SaveMetaData(CurrentImagePath + ".txt");
            //GroundTruth?.WriteImage("png", 0, GroundTruthPath);
            //SaveMetaData(GroundTruthPath + ".txt");
            //if (im_w == 0 || im_h == 0| annotationRegion == null) return;
            //var mask = new HImage("byte", im_w, im_h);
            ////var boundary = GroundTruth.Threshold(1, 255.0).ErosionCircle(1.0).Boundary("outer");
            //mask.OverpaintRegion(annotationRegion.Boundary("outer"), new HTuple(SelectClass.ClassID), "fill");
            //var image_name = System.IO.Path.GetFileName(CurrentImagePath);
            //var boundary_path = System.IO.Path.Combine(node.TrainConfig.ANNOTATION_DIR, image_name + "_boundary");
            //mask?.WriteImage("png", 0, boundary_path);
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
            return;
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
        public string CurrentImagePath { get; set; }
        private void SetSelectedImage(ImageSet selected)
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
                    SelectedImage = image;
                    var maskdir = GetMaskPath(selected);
                    var annotationDir = GetAnnotationPath(selected);

                    if (System.IO.File.Exists(maskdir))
                    {
                        var maskImage = new HImage(maskdir);
                        contextMaskRegion = maskImage.Threshold(1.0, 255);
                    }
                    else
                    {
                        contextMaskRegion = null;
                    }

                    if (System.IO.File.Exists(annotationDir))
                    {
                        var annotationImage = new HImage(annotationDir);
                        annotationRegion = annotationImage.Threshold(1.0, 255);
                    }
                    else
                    {
                        annotationRegion = null;
                    }

                    CurrentImagePath = selected.FullPath;
                    SelectedImage.GetImageSize(out im_w, out im_h);
                    ImageRegion = new HRegion(0, 0.0, im_h, im_w);

                    LoadMetaData(CurrentImagePath + ".txt");
                    UpdateMarker(CurrentImageMetadata.ROI);
                    if (CurrentImageMetadata.ROI == null)
                    {
                        if (RegionMaker != null)
                        {
                            window_display.HalconWindow.DetachDrawingObjectFromWindow(RegionMaker.Region.current_draw);
                        }
                        CurrentImageMetadata.ROI = new Rect1(0, 0, im_h, im_w);
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
                        
                    }
                    CreateROI(CurrentImageMetadata.ROI);
                    window_display.HalconWindow.AttachBackgroundToWindow(SelectedImage);
                    window_display.HalconWindow.ClearWindow();
                    if (ImageRegion != null & ROIRegion != null)
                    {
                        //maskRegion = ImageRegion.Difference(ROIRegion);
                        DispMaskRegion();
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lst_view.SelectedItem = selected;
                        lst_view.ScrollIntoView(selected);
                    });
                }
                catch { }


            }
        }
        string GetMaskPath(ImageSet image)
        {
            return System.IO.Path.Combine(node.TrainConfig.MASK_DIR, image.FileName + "." + image.Tag + ".png");
        }
        string GetAnnotationPath(ImageSet image)
        {
            return System.IO.Path.Combine(node.TrainConfig.ANNOTATION_DIR, image.FileName + "_boundary.png");
        }
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                if (e.AddedItems.Count == 0)
                {

                    return;
                }
                var selected = e.AddedItems[0] as ImageSet;
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
        HTuple fill_mode = new HTuple("margin");
        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
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
        HImage image;

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageSet;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListImage.Remove(vm);

                    var imagegtpath = System.IO.Path.Combine(node.TrainConfig.ANNOTATION_DIR, System.IO.Path.GetFileNameWithoutExtension(vm.FullPath));
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

        private void btn_train_cmd_Click(object sender, RoutedEventArgs e)
        {
            box_step.Visibility = Visibility.Hidden;

            this.node.TrainConfig.Save();
            try
            {
                NOVisionPython.TrainConsole("segmentation", "LDC", node.TrainConfig.ConfigDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        IContourGeneration ContourGenerator;
        HRegion contextMaskRegion;
        private void cmb_mask_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmb_mask_mode.SelectedIndex)
            {
                case 0: 
                    ContourGenerator = new ManualContourGeneration(node.TrainConfig.MASK_DIR);
                    annotationGenneration = new ManualContourGeneration(node.TrainConfig.ANNOTATION_DIR);
                    break;
                case 1: 
                    ContourGenerator = new ManualContourGeneration(node.TrainConfig.MASK_DIR);
                    annotationGenneration = new ManualContourGeneration(node.TrainConfig.ANNOTATION_DIR);
                    break;
            }
        }
        HImage SelectedImage { get; set; }
        private void btn_edit_mask_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {


                var result = ContourGenerator.ShowEditor(selected, SelectedImage);
                if (result != null)
                {
                    annotationRegion = result;
                    DispOverlay();
                }


            }
        }

        private void btn_auto_mask_all_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {
                label_wait.DeferedVisibility = true;
                label_wait.Content = "Applying mask to all images...";
                Task.Run(() =>
                {
                    try
                    {
                        ContourGenerator.ApplyContourFunction(selected, ListImage);
                    }
                    catch (Exception ex)
                    {

                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        label_wait.DeferedVisibility = false;
                        label_wait.Content = "Loading...";
                    });
                });

                //DispOverlay();

            }
        }

        private void btn_train_config_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
            wd.ShowDialog();
            node.TrainConfig.Save();
        }

        string _color_draw = "#00ff00aa";
        IContourGeneration annotationGenneration;
        HRegion annotationRegion;
        private void btn_edit_annotation_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {


                var result = annotationGenneration.ShowEditor(selected, SelectedImage, annotationRegion);
                if (result != null)
                {
                    annotationRegion = result;
                    DispOverlay();
                }


            }
        }

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

        private void btn_draw_polygon_Click(object sender, RoutedEventArgs e)
        {
            //var contour = window_display.HalconWindow.DrawNurbs("true", "true", "true", "true", 3, out HTuple rows, out HTuple cols, out HTuple weights);
            //window_display.HalconWindow.DispObj(contour);
        }

        private void btn_draw_line_Click(object sender, RoutedEventArgs e)
        {
            //window_display.HalconWindow.DrawLine(out double r1, out double c1, out double r2, out double c2);
            //window_display.HalconWindow.DispLine(r1, c1, r2, c2);
        }

        private void window_display_HMouseMove(object sender, HMouseEventArgsWPF e)
        {

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
        private void btn_zoom_in_click(object sender, RoutedEventArgs e)
        {
            image_detail.HZoomWindowContents(image_detail.ActualWidth / 2, image_detail.ActualHeight / 2, 120);
        }
        private void btn_zoom_out_click(object sender, RoutedEventArgs e)
        {
            image_detail.HZoomWindowContents(image_detail.ActualWidth / 2, image_detail.ActualHeight / 2, -120);
        }
        private void btn_zoom_fit_click(object sender, RoutedEventArgs e)
        {
            image_detail.SetFullImagePart();
        }
        private void chk_heatmap_Checked(object sender, RoutedEventArgs e)
        {
            if (EvalViewModel.SelectedEdgeImage != null)
            {
                image_detail.HalconWindow?.AttachBackgroundToWindow(EvalViewModel.SelectedEdgeImage);
            }
            

        }

        private void chk_heatmap_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EvalViewModel.SelectedImage != null)
            {
                image_detail.HalconWindow?.AttachBackgroundToWindow(EvalViewModel.SelectedImage);
            }
        }
        private void image_detail_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (e.Row >= 0 & e.Column >= 0 & e.Row <= EvalViewModel.PredictImageHeight
                & e.Column <= EvalViewModel.PredictImageWidth & EvalViewModel.SelectedEdgeImage != null)
            {
                EvalViewModel.PixelScore = EvalViewModel.SelectedEdgeImage.GetGrayval(((int)e.Row), ((int)e.Column));
            }

        }
        private void image_detail_HInitWindow(object sender, EventArgs e)
        {
            image_detail.HalconWindow.SetWindowParam("background_color", "white");
            image_detail.HalconWindow.ClearWindow();
        }
        private void lst_evaluation_image_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {

                ChangeEvalImage((e.AddedItems[0] as ClassificationResult));


            }
            else
            {
                image_detail.HalconWindow.DetachBackgroundFromWindow();
                EvalViewModel.SelectedImage = null;
                EvalViewModel.SelectedEdgeImage = null;
                EvalViewModel.SelectedEdgeImage = null;
            }
        }
        private void btn_edit_paint_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {
                var newImage = NOVisionDesigner.Designer.Extensions.Functions.EditImageWithWindow(selected);
                if (newImage != null)
                {
                    SelectedImage = newImage;
                    window_display.HalconWindow.AttachBackgroundToWindow(newImage);
                }
            }

        }

        private void btn_edit_eval_paint_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var selected = lst_evaluation_image.SelectedItem as ClassificationResult;
            if (selected != null)
            {
                var newImage = NOVisionDesigner.Designer.Extensions.Functions.EditImageWithWindow(new ImageFilmstrip(selected.Image));
                if (newImage != null)
                {
                    if (node.Runtime.State != ModelState.Loaded)
                    {
                        node.Runtime.LoadRecipe();
                    }

                    if (node.Runtime.State == ModelState.Loaded)
                    {
                        //EvalViewModel.SelectedImage = newImage;
                        newImage.WriteImage("png", 0, selected.Image);
                        var predictionResult = node.Runtime.Infer(newImage);
                        predictionResult.WriteImage("png", 0, selected.PredictImage);
                        ChangeEvalImage(selected);
                    }
                    //selected.PredictImage
                }
            }
        }

        void ChangeEvalImage(ClassificationResult selected)
        {
            try
            {
                var imageDir = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, selected.Image);
                if (System.IO.File.Exists(imageDir))
                {
                    var image = new HImage(imageDir);
                    EvalViewModel.SelectedImage = image;

                    image_detail.SetFullImagePart(image);
                    if (!(chk_heatmap.IsChecked == true))
                    {
                        image_detail.HalconWindow.ClearWindow();
                        image_detail.HalconWindow.AttachBackgroundToWindow(image);
                        var map = new HImage(selected.PredictImage);
                        map.GetImageSize(out int w, out int h);
                        EvalViewModel.SelectedEdgeImage = map;
                    }
                    else
                    {

                        var map = new HImage(selected.PredictImage);
                        map.GetImageSize(out int w, out int h);
                        EvalViewModel.SelectedEdgeImage = map;
                        image_detail.HalconWindow.AttachBackgroundToWindow(map);
                        //image_detail.HalconWindow.DispImage(imageimposed);
                    }
                }
            }catch(Exception ex)
            {

            }
            

        }

        private void btn_eval_Click(object sender, RoutedEventArgs e)
        {
            StartEvaluation();
        }
        private void btn_stop_eval_Click(object sender, RoutedEventArgs e)
        {
            EvalViewModel._is_cancel = true;
        }

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }

        public class TrainningStatusViewModel : INotifyPropertyChanged, IDisposable
        {
            //object lock1 = new object();
            private readonly object _lock1 = new object();
            private readonly object _lock2 = new object();
            public TrainningStatusViewModel()
            {
                BindingOperations.EnableCollectionSynchronization(LossData, _lock1);
                BindingOperations.EnableCollectionSynchronization(AccData, _lock2);
            }
            void RaisePropertyChanged(string prop)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            public ObservableCollection<GraphData> LossData { get; set; } = new ObservableCollection<GraphData>();
            public ObservableCollection<GraphData> AccData { get; set; } = new ObservableCollection<GraphData>();
            public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
            double _epoch;
            public double Epoch
            {
                get
                {
                    return _epoch;
                }
                set
                {
                    if (_epoch != value)
                    {
                        _epoch = value;
                        RaisePropertyChanged("Epoch");
                    }
                }
            }
            double _acc;
            public double Acc
            {
                get
                {
                    return _acc;
                }
                set
                {
                    if (_acc != value)
                    {
                        _acc = value;
                        RaisePropertyChanged("Acc");
                    }
                }
            }
            double _loss;
            public double Loss
            {
                get
                {
                    return _loss;
                }
                set
                {
                    if (_loss != value)
                    {
                        _loss = value;
                        RaisePropertyChanged("Loss");
                    }
                }
            }
            double _learning_rate;
            public double LearningRate
            {
                get
                {
                    return _learning_rate;
                }
                set
                {
                    if (_learning_rate != value)
                    {
                        _learning_rate = value;
                        RaisePropertyChanged("LearningRate");
                    }
                }
            }
            TimeSpan _time_exlapsed;
            public TimeSpan TimeExlapsed
            {
                get
                {
                    return _time_exlapsed;
                }
                set
                {
                    if (_time_exlapsed != value)
                    {
                        _time_exlapsed = value;
                        RaisePropertyChanged("TimeExlapsed");
                    }
                }
            }
            TimeSpan _time_left;
            public TimeSpan TimeLeft
            {
                get
                {
                    return _time_left;
                }
                set
                {
                    if (_time_left != value)
                    {
                        _time_left = value;
                        RaisePropertyChanged("TimeLeft");
                    }
                }
            }
            NetMQPoller poller;
            SubscriberSocket subSocket;
            ImageSource _image_gt;
            public ImageSource ImageGt
            {
                get
                {
                    return _image_gt;
                }
                set
                {
                    if (_image_gt != value)
                    {
                        _image_gt = value;
                        RaisePropertyChanged("ImageGt");
                    }
                }
            }
            ImageSource _image_pred;
            public ImageSource ImagePred
            {
                get
                {
                    return _image_pred;
                }
                set
                {
                    if (_image_pred != value)
                    {
                        _image_pred = value;
                        RaisePropertyChanged("ImagePred");
                    }
                }
            }

            public void StartListen()
            {
                Task.Run(() =>
                {
                    var start = DateTime.Now;
                    subSocket = new SubscriberSocket();
                    subSocket.Options.ReceiveHighWatermark = 1000;
                    subSocket.Connect("tcp://localhost:51234");
                    subSocket.Subscribe(String.Empty);
                    Console.WriteLine("Subscriber socket connecting...");
                    poller = new NetMQPoller { subSocket };

                    subSocket.ReceiveReady += (s, a) =>
                    {
                        string messageTopicReceived = subSocket.ReceiveFrameString();

                        if (messageTopicReceived == "status")
                        {
                            string messageReceived = subSocket.ReceiveFrameString();
                            JObject data = JObject.Parse(messageReceived);
                            Epoch = data["epoch"].ToObject<double>();
                            Loss = data["loss"].ToObject<double>();
                            LearningRate = data["lr"].ToObject<double>();
                            if (data.ContainsKey("timeleft"))
                            {
                                TimeLeft = TimeSpan.FromSeconds((int)(data["timeleft"].ToObject<int>() / 1000));
                            }
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LossData.Add(new GraphData() { X = Epoch, Y = Loss });
                            });
                            TimeExlapsed = TimeSpan.FromSeconds((int)(DateTime.Now - start).TotalSeconds);
                        }
                        else if (messageTopicReceived == "log")
                        {
                            string messageReceived = subSocket.ReceiveFrameString();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Logs.Add(DateTime.Now.ToString() + ": " + messageReceived);
                            });
                        }
                        else if (messageTopicReceived == "image_gt")
                        {
                            var messageReceived = subSocket.ReceiveFrameBytes();
                            //JObject data = JObject.Parse(messageReceived);
                            using (var stream = new MemoryStream(messageReceived))
                            {
                                ImageGt = BitmapFrame.Create(
                                    stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                            }

                        }
                        else if (messageTopicReceived == "image_pred")
                        {
                            var messageReceived = subSocket.ReceiveFrameBytes();
                            //JObject data = JObject.Parse(messageReceived);
                            using (var stream = new MemoryStream(messageReceived))
                            {
                                ImagePred = BitmapFrame.Create(
                                    stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                            }
                        }
                        else
                        {
                            var messageReceived = subSocket.ReceiveFrameString();
                        }

                    };
                    poller.RunAsync();
                });

            }

            public void Dispose()
            {
                //poller?.Remove(subSocket);
                //subSocket?.Close();            
                poller.Stop();
                subSocket.Close();
            }


        }

        public class EvaluationViewModel : INotifyPropertyChanged, IDisposable
        {
            public void Dispose()
            {

            }
            void RaisePropertyChanged(string prop)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            bool _show_edge = true;
            public bool ShowEdge
            {
                get
                {
                    return _show_edge;
                }
                set
                {
                    if (_show_edge != value)
                    {
                        _show_edge = value;
                        RaisePropertyChanged("ShowHeatmap");
                    }
                }
            }
            double _PixelScore;
            public double PixelScore
            {
                get
                {
                    return _PixelScore;
                }
                set
                {
                    if (_PixelScore != value)
                    {
                        _PixelScore = value;
                        RaisePropertyChanged("PixelScore");
                    }
                }
            }
            public int PredictImageWidth { get; set; }
            public int PredictImageHeight { get; set; }
            public HImage SelectedImage { get; set; }
            HImage _selected_edge_image;
            public HImage SelectedEdgeImage
            {
                get { return _selected_edge_image; }
                set
                {
                    _selected_edge_image = value;
                    if (value != null)
                    {
                        _selected_edge_image.GetImageSize(out int w, out int h);
                        PredictImageWidth = w;
                        PredictImageHeight = h;
                    }
                }
            }
            bool _is_evaluation;
            [JsonIgnore]
            public bool IsEvaluation
            {
                get
                {
                    return _is_evaluation;
                }
                set
                {
                    if (_is_evaluation != value)
                    {
                        _is_evaluation = value;
                        RaisePropertyChanged("IsEvaluation");
                    }
                }
            }
            public bool _is_cancel = false;
            

            double _evaluationProgress;
            public double EvaluationProgress
            {
                get
                {
                    return _evaluationProgress;
                }
                set
                {
                    if (_evaluationProgress != value)
                    {
                        _evaluationProgress = value;
                        RaisePropertyChanged("EvaluationProgress");
                    }
                }
            }

            int _TotalSamples;
            public int TotalSamples
            {
                get
                {
                    return _TotalSamples;
                }
                set
                {
                    if (_TotalSamples != value)
                    {
                        _TotalSamples = value;
                        RaisePropertyChanged("TotalSamples");
                    }
                }
            }
            List<ClassificationResult> _ClassificationResult;
            public List<ClassificationResult> ClassificationResult
            {
                get
                {
                    return _ClassificationResult;
                }
                set
                {
                    if (_ClassificationResult != value)
                    {
                        _ClassificationResult = value;
                        RaisePropertyChanged("ClassificationResult");
                    }
                }
            }
            
            

        }
    }
}