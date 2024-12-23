using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using static NOVisionDesigner.Designer.Windows.SegmentationWindow;

namespace NOVisionDesigner.Designer.Windows
{
    public partial class PadAnomalyWindow : ThemedWindow, INotifyPropertyChanged
    {

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        EvaluationViewModel EvalViewModel = new EvaluationViewModel();
        int _roi_x;
        public int ROIX
        {
            get
            {
                return _roi_x;
            }
            set
            {
                if (_roi_x != value)
                {
                    _roi_x = value;
                    RaisePropertyChanged("ROIX");
                }
            }
        }
        int _roi_y;
        public int ROIY
        {
            get
            {
                return _roi_y;
            }
            set
            {
                if (_roi_y != value)
                {
                    _roi_y = value;
                    RaisePropertyChanged("ROIY");
                }
            }
        }
        int _roi_w;
        public int ROIW
        {
            get
            {
                return _roi_w;
            }
            set
            {
                if (_roi_w != value)
                {
                    _roi_w = value;
                    RaisePropertyChanged("ROIW");
                }
            }
        }
        int roi_h;
        public int ROIH
        {
            get
            {
                return roi_h;
            }
            set
            {
                if (roi_h != value)
                {
                    roi_h = value;
                    RaisePropertyChanged("ROIH");
                }
            }
        }

        private List<DatasetSplit> _SelectedDisplayFilter = new List<DatasetSplit>() { DatasetSplit.train,DatasetSplit.test};

        public List<DatasetSplit> SelectedDisplayFilter
        {
            get { return _SelectedDisplayFilter; }
            set
            {
                if (_SelectedDisplayFilter != value)
                {
                    _SelectedDisplayFilter = value;
                    RaisePropertyChanged("SelectedDisplayFilter");
                }
            }
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
        private string _selected_tag = "All";

        public string SelectedTag
        {
            get { return _selected_tag; }
            set
            {
                if (_selected_tag != value)
                {
                    _selected_tag = value;
                    
                    RaisePropertyChanged("SelectedTag");
                    view?.Refresh();
                }
            }
        }
        private string _wait_message = "...";

        public string WaitMessage
        {
            get { return _wait_message; }
            set
            {
                if (_wait_message != value)
                {
                    _wait_message = value;

                    RaisePropertyChanged("WaitMessage");
                }
            }
        }
        private bool _show_message;
        public bool ShowMessage
        {
            get { return _show_message; }
            set
            {
                if (_show_message != value)
                {
                    _show_message = value;
                    RaisePropertyChanged("ShowMessage");
                }
            }
        }

        private bool _isMargin;
        public bool IsMargin
        {
            get { return _isMargin; }
            set
            {
                if (_isMargin != value)
                {
                    _isMargin = value;
                    RaisePropertyChanged("IsMargin");
                }
            }
        }

        private double _colorOpacity = 20;
        public double ColorOpacity
        {
            get { return _colorOpacity; }
            set
            {
                if (_colorOpacity != value)
                {
                    _colorOpacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }

        private string _imageName;
        public string ImageName
        {
            get { return _imageName; }
            set
            {
                if (_imageName != value)
                {
                    _imageName = value;
                    RaisePropertyChanged("ImageName");
                }
            }
        }


        private string _colorDraw = "#00ff00aa";
        public string ColorDraw
        {
            get
            {
                return _colorDraw;
            }
            set
            {
                if (_colorDraw != value)
                {
                    _colorDraw = value;
                    display?.SetColor(value);
                    RaisePropertyChanged("ColorDraw");
                }
            }
        }

        private ObservableCollection<ImageSet> _listImage;

        public ObservableCollection<ImageSet> ListImage
        {
            get
            {
                return _listImage;
            }
            set
            {
                if (_listImage != value)
                {
                    _listImage = value;
                    RaisePropertyChanged("ListImage");
                }
            }
        }
        private string ImageDir;
        private int row, col;
        private int imgW, imgH;
        private double mpx, mpy;
        private HWindow display;
        //private HImage image;
        PadInspection node;
        TrainAnomalyV3 train = new TrainAnomalyV3();
        #endregion
        
        public ImageSet AddImages(HImage image,string directory,string tag,bool Prepend = false)
        {
            try
            {
                
                var filename = Functions.RandomFileName(System.IO.Path.Combine(directory,tag));
                //var newfile = System.IO.Path.Combine(ImageDir, filename);
                image.WriteImage("png", 0, filename);
                var imageadded = new ImageSet(filename + ".png") { Tag = tag};
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Prepend)
                    {
                        ListImage.Insert(0, imageadded);
                    }
                    else
                    {
                        ListImage.Add(imageadded);
                    }
                }));
                
                
                return imageadded;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        HRegion ImageRegion,ROIRegion;

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
        RegionMaker RegionMaker;
        public void OnUpdate(RegionMaker marker, Region region)
        {
            CurrentImageMetadata.ROI.row1 = (int)marker.Region.row1;
            CurrentImageMetadata.ROI.col1 = (int)marker.Region.col1;
            CurrentImageMetadata.ROI.row2 = (int)marker.Region.row2;
            CurrentImageMetadata.ROI.col2 = (int)marker.Region.col2;
            
            ROIY = (int)marker.Region.row1;
            ROIX = (int)marker.Region.col1;
            ROIW = (int)marker.Region.col2 - (int)marker.Region.col1;
            ROIH = (int)marker.Region.row2 - (int)marker.Region.row1;
            ROIRegion = RegionMaker.Region.region;
            if (ImageRegion != null & ROIRegion != null)
            {
                maskRegion = ImageRegion.Difference(ROIRegion);
            }
            DispMaskRegion();

        }
        public void CreateROI(Rect1 ROI)
        {
            if (ROI != null)
            {
                ROIRegion = new HRegion((double)ROI.row1, ROI.col1, ROI.row2, ROI.col2);

            }
        }
        public void OnSelected(RegionMaker marker, Region region)
        {

        }
        bool TrainModeSelection()
        {
            if(node.AnomalyConfig.ModelName == "MemSeg_Pytorch" | node.AnomalyConfig.ModelName == "cfa")
            {
                var dataPath = System.IO.Path.Combine(node.AnomalyConfig.BaseDir, "MemSeg-images");
                var bestScore = System.IO.Path.Combine(dataPath, "best_score.json");
                var latestScore = System.IO.Path.Combine(dataPath, "latest_score.json");
                var LatestCheckPoint = new AnomalyCheckpointInfo();
                var BestCheckPoint = new AnomalyCheckpointInfo();
                try
                {
                    JObject latestScoreData = JObject.Parse(File.ReadAllText(latestScore));
                    LatestCheckPoint.Epoch = latestScoreData["latest_step"].ToObject<int>();
                    LatestCheckPoint.AUROCImage = latestScoreData["eval_AUROC-image"].ToObject<double>();
                    LatestCheckPoint.AUROCPixel = latestScoreData["eval_AUROC-image"].ToObject<double>();
                    LatestCheckPoint.AUPROPixel = latestScoreData["eval_AUROC-image"].ToObject<double>();
                }catch(Exception ex)
                {

                }
                try
                {
                    JObject bestScoreData = JObject.Parse(File.ReadAllText(bestScore));
                    BestCheckPoint.Epoch = bestScoreData["best_step"].ToObject<int>();
                    BestCheckPoint.AUROCImage = bestScoreData["eval_AUROC-image"].ToObject<double>();
                    BestCheckPoint.AUROCPixel = bestScoreData["eval_AUROC-image"].ToObject<double>();
                    BestCheckPoint.AUPROPixel = bestScoreData["eval_AUROC-image"].ToObject<double>();
                }catch(Exception ex)
                {

                }
                
                TrainModeSelectionWindow wd = new TrainModeSelectionWindow(LatestCheckPoint,BestCheckPoint);
                wd.Owner = this;
                if (wd.ShowDialog()==true)
                {
                    if(wd.TrainType == TrainResume.New)
                    {
                        node.AnomalyConfig.TrainType = wd.TrainType;
                    }
                    else
                    {
                        node.AnomalyConfig.TrainType = wd.TrainType;
                        node.AnomalyConfig.CheckPoint = wd.CheckPoint;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if(node.AnomalyConfig.ModelName == "DiffusionAD")
            {
                var dataPath = System.IO.Path.Combine(node.AnomalyConfig.BaseDir, "MemSeg-images");
                var bestScore = System.IO.Path.Combine(dataPath, "best_score.json");
                var latestScore = System.IO.Path.Combine(dataPath, "latest_score.json");
                var LatestCheckPoint = new AnomalyCheckpointInfo();
                var BestCheckPoint = new AnomalyCheckpointInfo();
                try
                {
                    JObject latestScoreData = JObject.Parse(File.ReadAllText(latestScore));
                    LatestCheckPoint.Epoch = latestScoreData["latest_step"].ToObject<int>();
                    LatestCheckPoint.AUROCImage = latestScoreData["eval_AUROC-image"].ToObject<double>();
                    LatestCheckPoint.AUROCPixel = latestScoreData["eval_AUROC-image"].ToObject<double>();
                    LatestCheckPoint.AUPROPixel = latestScoreData["eval_AUROC-image"].ToObject<double>();
                }
                catch (Exception ex)
                {

                }
                try
                {
                    JObject bestScoreData = JObject.Parse(File.ReadAllText(bestScore));
                    BestCheckPoint.Epoch = bestScoreData["best_step"].ToObject<int>();
                    BestCheckPoint.AUROCImage = bestScoreData["eval_AUROC-image"].ToObject<double>();
                    BestCheckPoint.AUROCPixel = bestScoreData["eval_AUROC-image"].ToObject<double>();
                    BestCheckPoint.AUPROPixel = bestScoreData["eval_AUROC-image"].ToObject<double>();
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
                        node.AnomalyConfig.TrainType = wd.TrainType;
                    }
                    else
                    {
                        node.AnomalyConfig.TrainType = wd.TrainType;
                        node.AnomalyConfig.CheckPoint = wd.CheckPoint;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        public PadAnomalyWindow(PadInspection node)
        {
            this.node = node;
            maskGeneration = new MannualMaskGeneration(node.AnomalyConfig.MaskDir);
            annotationGenneration = new MannualMaskGeneration(node.AnomalyConfig.AnnotationDir);
            InitializeComponent();
            propertiesGrid.SelectedObject = node.AnomalyRuntime;
            this.DataContext = this;
            this.stack_train_parameter.DataContext = node.AnomalyConfig;
            
            this.ImageDir = node.AnomalyConfig.ImageDir;

            btn_train.Click += (sender, e) =>
            {
                if (TrainModeSelection())
                {
                    StartTrainning();
                }
                
            };
            btn_stop_train.Click+= (sender, e) =>
            {
                train?.Cancel();
            };

            this.DataContext = this;
            if (lst_view.Items.Count > 0)
            if (lst_view.Items.Count > 0)
            {
                lst_view.SelectedItem = ListImage[0];
            }
            //LoadHistogram();
            LoadImageList(ImageDir);
            
            //CreateROI();
            this.Closed += (o, e) =>
            {
                trainningStatusViewModel?.Dispose();
            };
            Task.Run(() =>
            {
                LoadLastEvaluationResult();
            });
            
            tab_eval.DataContext = EvalViewModel;
            confusionMatrix.SelectionChanged += List_confusion_matrix_SelectionChanged;
            stack_augmentation.DataContext = node.AnomalyConfig.Augmentations;
            lst_display_filter.ItemsSource = new List<object>() { DatasetSplit.train, DatasetSplit.test };
            lst_display_filter.EditValue = new List<object>() { DatasetSplit.train, DatasetSplit.test }; 
        }
        public void LoadLastEvaluationResult()
        {
            try
            {
                string resultDir = System.IO.Path.Combine(node.AnomalyConfig.EvaluationDir, "result.txt");
                if (File.Exists(resultDir))
                {
                    EvalViewModel = JsonConvert.DeserializeObject<EvaluationViewModel>(File.ReadAllText(resultDir));
                    //tab_eval.DataContext = EvalViewModel;
                    UpdateEvaluation(EvalViewModel.ClassificationResult);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        tab_eval.DataContext = EvalViewModel;
                    });
                }
            }catch(Exception ex)
            {

            }
            
        }
        public List<ClassifierClass1> ClassList
        {
            get; set;
        } = new List<ClassifierClass1>() { new ClassifierClass1() { Name = "good" }, new ClassifierClass1() {Name="bad" } };
        private void List_confusion_matrix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var data = e.AddedItems[0] as ConfusionMatrixPoint;
                if (data != null)
                {
                    lst_evaluation_image.ItemsSource = data.Data;
                }
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

        private void image_detail_HInitWindow(object sender, EventArgs e)
        {
            image_detail.HalconWindow.SetWindowParam("background_color", "white");
            image_detail.HalconWindow.ClearWindow();
        }
        public void StartEvaluation()
        {
            Task.Run(() =>
            {
            EvalViewModel.IsEvaluation = true;
            if (node.AnomalyRuntime.State == ModelState.Unloaded)
            {
                node.AnomalyRuntime.LoadRecipe();
            }
            string directory = System.IO.Path.Combine(node.AnomalyConfig.EvaluationDir, "images");
            
            string resultDir = System.IO.Path.Combine(node.AnomalyConfig.EvaluationDir, "result.txt");
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            Dictionary<string, string> result = new Dictionary<string, string>();
            Dictionary<string, int> classSummary = new Dictionary<string, int>();
            foreach (var classes in ClassList)
            {
                var classesDirectory = System.IO.Path.Combine(directory, classes.Name);
                System.IO.Directory.CreateDirectory(classesDirectory);
                result.Add(classes.Name, classesDirectory);
                classSummary.Add(classes.Name, 0);
            }
            List<ClassificationResult> classificationResult = new List<ClassificationResult>();
            var goodfiles = Directory.GetFiles(node.AnomalyConfig.NormalDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
            var badfiles = Directory.GetFiles(node.AnomalyConfig.AnomalyDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
            int total = goodfiles.Count() + badfiles.Count();
                EvalViewModel.TotalSamples = total;
            classSummary["good"] = goodfiles.Count();
            classSummary["bad"] = badfiles.Count();
            int step = 0;
            double maxGoodScore = 0;
            foreach (var ImageFile in goodfiles)
            {
                if (EvalViewModel._is_cancel)
                {
                    EvalViewModel._is_cancel = false;
                    EvalViewModel.IsEvaluation = false;
                    return;
                }

                HImage imagePart;
                if (System.IO.File.Exists(ImageFile + ".txt"))
                {
                    var metaData = JsonConvert.DeserializeObject<ImageMetaData>(File.ReadAllText(ImageFile + ".txt"));
                        imagePart = new HImage(ImageFile);//.CropRectangle1(metaData.ROI.row1, metaData.ROI.col1, metaData.ROI.row2-1, metaData.ROI.col2-1);
                    imagePart.GetImageSize(out int w,out int h);
                    }
                    else
                    {
                        imagePart = new HImage(ImageFile);
                    }
                    var savedImage = System.IO.Path.Combine(result["good"], System.IO.Path.GetFileNameWithoutExtension(ImageFile))+".png";
                    imagePart.WriteImage("png",0, savedImage);
                    if (node.AnomalyRuntime.State == ModelState.Loaded)
                    {
                        var predictionResult = node.AnomalyRuntime.Infer(imagePart,out double score);
                        var fileName = System.IO.Path.GetFileName(ImageFile);
                        var predictImage = System.IO.Path.Combine(result["good"],  fileName);
                        predictionResult.WriteImage("hobj", 0,predictImage);
                        classificationResult.Add(new ClassificationResult()
                        {
                            FileName = fileName,
                            Image = savedImage,
                            Label = "good",
                            Predict = "good",
                            Probability = score,
                            PredictImage= predictImage + ".hobj"
                        });
                        maxGoodScore = Math.Max(score, maxGoodScore);
                    }
                    step++;
                    EvalViewModel.EvaluationProgress = Math.Round(((double)step / total) * 100, 0);                  
                }
                EvalViewModel.EvalThreshold = maxGoodScore;
                foreach (var ImageFile in badfiles)
                {
                    if (EvalViewModel._is_cancel)
                    {
                        EvalViewModel._is_cancel = false;
                        EvalViewModel.IsEvaluation = false;
                        return;
                    }

                    HImage imagePart;
                    if (System.IO.File.Exists(ImageFile + ".txt"))
                    {
                        var metaData = JsonConvert.DeserializeObject<ImageMetaData>(File.ReadAllText(ImageFile + ".txt"));
                        imagePart = new HImage(ImageFile);//.CropRectangle1(metaData.ROI.row1, metaData.ROI.col1, metaData.ROI.row2, metaData.ROI.col2);
                    }
                    else
                    {
                        imagePart = new HImage(ImageFile);
                    }
                    var savedImage = System.IO.Path.Combine(result["bad"], System.IO.Path.GetFileNameWithoutExtension(ImageFile)) + ".png";
                    imagePart.WriteImage("png", 0, savedImage);
                    if (node.AnomalyRuntime.State == ModelState.Loaded)
                    {
                        var predictionResult = node.AnomalyRuntime.Infer(imagePart, out double score);
                        var fileName = System.IO.Path.GetFileName(ImageFile);
                        var predictImage = System.IO.Path.Combine(result["bad"], fileName);
                        predictionResult.WriteImage("hobj", 0, predictImage);
                        classificationResult.Add(new ClassificationResult()
                        {
                            FileName = fileName,
                            Image = savedImage,
                            Label = "bad",
                            Predict = score> maxGoodScore?"bad":"good",
                            Probability = score,
                            PredictImage = predictImage+ ".hobj"
                        });
                    }
                    step++;
                    EvalViewModel.EvaluationProgress = Math.Round(((double)step / total) * 100, 0);
                }
                UpdateEvaluation(classificationResult);
                var classSummaryList = classSummary.Select(x => new ClassCount() { Name = x.Key, Value = x.Value }).ToList();
                EvalViewModel.ClassSummaryList = classSummaryList;
                EvalViewModel.ClassificationResult = classificationResult;
                System.IO.File.WriteAllText(resultDir, JsonConvert.SerializeObject(EvalViewModel, Formatting.Indented));
                EvalViewModel.IsEvaluation = false;
            });


        }
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
            var ptr = image.GetImagePointer1(out string type, out int w, out int h);
            
            var mapByte = (map*255).ConvertImageType("byte");
            var ptrMap = mapByte.GetImagePointer1(out string typem, out int wm, out int hm);
            //Mat cvImage = new Mat(h, w, MatType.CV_8U, ptr);
            Mat cvMap = new Mat(hm, wm, MatType.CV_8U, ptrMap);
            Mat cvMapThresh = new Mat();
            var th = Cv2.Threshold(cvMap, cvMapThresh, threshold * 255, 255, ThresholdTypes.Binary);
            Cv2.GaussianBlur(cvMapThresh, cvMapThresh,new OpenCvSharp.Size(13, 13), 11);
            var heatmap_img = new Mat();
            //var superImposed = new Mat();
            Cv2.ApplyColorMap(cvMapThresh, heatmap_img, ColormapTypes.Jet);
            //Cv2.AddWeighted(cvImage, 0.5, heatmap_img, 0.5, 0, superImposed);
            var c0= heatmap_img.ExtractChannel(0);
            var c1 = heatmap_img.ExtractChannel(1);
            var c2 = heatmap_img.ExtractChannel(2);

            HImage b = new HImage("byte", w, h, c0.Data);
            HImage g = new HImage("byte", w, h, c1.Data);
            HImage r = new HImage("byte", w, h, c2.Data);
            HImage t = r.Compose3(g, b);
            t = t * 0.5 + image * 0.5;
            return t;

        }
        private void UpdateEvaluation(List<ClassificationResult> classificationResult)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                confusionMatrix.CreateLayout(classificationResult.Count, ClassList.Select(x => x.Name).ToList(), classificationResult);
                EvalViewModel.PrecisionList = confusionMatrix.PrecisionList;
                EvalViewModel.Acc = confusionMatrix.Acc;
                EvalViewModel.F1Score = confusionMatrix.F1;
                gridcontrol_evaluation.BestFitColumns();
            });
        }
        HRegion annotation_region;
        void ChangeEvalImage(ClassificationResult selected)
        {
            var imageDir = System.IO.Path.Combine(node.AnomalyConfig.EvaluationDir, selected.Image);
            var img_name = System.IO.Path.GetFileName(imageDir);
            var annotation_imgpath = System.IO.Path.Combine(node.AnomalyConfig.AnnotationDir, img_name + ".bad.png");
            if (System.IO.File.Exists(imageDir))
            {
                var image = new HImage(imageDir);
                EvalViewModel.SelectedImage = image;
                
                image_detail.SetFullImagePart(image);
                if(System.IO.File.Exists(annotation_imgpath))
                {
                    HImage annot_img = new HImage(annotation_imgpath);
                    annotation_region = annot_img.Threshold(128.0, 255);
                }
                else
                {
                    annotation_region=null;
                }

                if (!(chk_heatmap.IsChecked == true))
                {
                    image_detail.HalconWindow.ClearWindow();
                    image_detail.HalconWindow.AttachBackgroundToWindow(image);
                    
                    EvalViewModel.SelectedHeatmapImage = null;
                    var map = new HImage(selected.PredictImage);
                    map.GetImageSize(out int w, out int h);
                    EvalViewModel.SelectedPredictImage = map;
                }
                else
                {

                    var map = new HImage(selected.PredictImage);
                    map.GetImageSize(out int w, out int h);
                    EvalViewModel.SelectedPredictImage = map;
                    var imageimposed = ApplyColorMap(image, EvalViewModel.SelectedPredictImage, EvalViewModel.EvalThreshold);
                    image_detail.HalconWindow.AttachBackgroundToWindow(imageimposed);
                    EvalViewModel.SelectedHeatmapImage = imageimposed; ;
                    //image_detail.HalconWindow.DispImage(imageimposed);
                }
            }

        }
        private void lst_evaluation_image_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //image_detail.Source =
            if (e.AddedItems.Count > 0)
            {

                ChangeEvalImage((e.AddedItems[0] as ClassificationResult));


            }
            else
            {
                image_detail.HalconWindow.DetachBackgroundFromWindow();
                EvalViewModel.SelectedImage = null;
                EvalViewModel.SelectedPredictImage = null;
                EvalViewModel.SelectedHeatmapImage = null;
            }

        }
        TrainningStatusViewModel trainningStatusViewModel;
        public void StartTrainning()
        {
            trainningStatusViewModel?.Dispose();
            trainningStatusViewModel = new TrainningStatusViewModel();
            grid_trainning_status.DataContext = trainningStatusViewModel;
            Task.Run(new Action(() =>
            {
                IsTraining = true;
                node.AnomalyConfig.Save();
                trainningStatusViewModel.StartListen();
                train.TrainPython(node.AnomalyConfig.ConfigDir, node.AnomalyConfig.ModelName, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTraining = false;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            DXMessageBox.Show(this, trainargs.ErrorLog, "Train error");
                        }));
                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTraining = false;
                        
                        PostTrain();
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            DXMessageBox.Show(this, "Train Complete!", "Message");
                        }));

                    }
                   
                });
            }));
        }
        public void PostTrain()
        {
            //LoadHistogram();
            node.AnomalyRuntime.State = ModelState.Unloaded;
            Application.Current.Dispatcher.Invoke(() =>
            {
                propertiesGrid.SelectedObject = null;
                propertiesGrid.SelectedObject = node.AnomalyRuntime;
            });
           
        }
        List<double> _good_predictions = new List<double>();
        public List<double> GoodPredictions
        {
            get
            {
                return _good_predictions;
            }
            set
            {
                if (_good_predictions != value)
                {
                    _good_predictions = value;
                    RaisePropertyChanged("GoodPredictions");
                }
            }
        }
        List<double> _bad_predictions = new List<double>();
        public List<double> BadPredictions
        {
            get
            {
                return _bad_predictions;
            }
            set
            {
                if (_bad_predictions != value)
                {
                    _bad_predictions = value;
                    RaisePropertyChanged("BadPredictions");
                }
            }
        }
        //private void LoadHistogram()
        //{
        //    var histogramDir = System.IO.Path.Combine(node.AnomalyConfig.ResultDir, "prediction.txt");
        //    if (System.IO.File.Exists(histogramDir))
        //    {
        //        try
        //        {
        //            JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(histogramDir)));
        //            var Min = data["min"].Value<float>();
        //            var Max = data["max"].Value<float>();
        //            var Threshold = data["threshold"].Value<float>();
        //            GoodPredictions = data["normal"].Values<double>().ToList();//   ToArray<double>().ToList();
        //            BadPredictions = data["anomalous"].Values<double>().ToList();
        //            //(chart.Diagram as XYDiagram2D).ActualAxisX.ActualVisualRange.MaxValue = 1;
        //            //(chart.Diagram as XYDiagram2D).ActualAxisX.ActualVisualRange.MinValue = 0;
        //            Application.Current.Dispatcher.Invoke(() =>
        //            {
        //                (chart.Diagram as XYDiagram2D).ActualAxisX.WholeRange.SetMinMaxValues(0, 1);
        //                (chart.Diagram as XYDiagram2D).ActualAxisX.VisualRange.SetMinMaxValues(0, 1);
        //            });
                  
        //        }
        //        catch (Exception ex)
        //        {

        //        }

        //    }
        //}
        ICollectionView view;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        private bool CustomerFilter(object item)
        {
            var Items = (ImageSet)item;
            if(SelectedTag.ToLower()=="all")
            {
                if (_SelectedDisplayFilter == null)
                {
                    return false;
                }
                else
                {
                    return _SelectedDisplayFilter.Contains(Items.MetaData.Dataset);
                }
                
            }
            else
            {
                if (_SelectedDisplayFilter == null)
                {
                    return false;
                }
                return (Items.Tag == SelectedTag.ToLower() & _SelectedDisplayFilter.Contains(Items.MetaData.Dataset));
            }
            
        }
        void LoadImageList(string dir)
        {
            Task.Run(() =>
            {
                List<string> normal_image_paths = new List<string>();
                List<string> anomalous_image_paths = new List<string>();
                var histogramDir = System.IO.Path.Combine(node.AnomalyConfig.ResultDir, "prediction.txt");
                if (System.IO.File.Exists(histogramDir))
                {
                    try
                    {
                        JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(histogramDir)));
                        if (data.ContainsKey("normal_image_paths"))
                        {
                            normal_image_paths = data["normal_image_paths"].Values<string>().ToList();
                        }
                        if (data.ContainsKey("normal_image_paths"))
                        {
                            anomalous_image_paths = data["anomalous_image_paths"].Values<string>().ToList();
                        }

                    }
                    catch (Exception ex)
                    {

                    }

                }
                var result = new List<ImageSet>();
                if (Directory.Exists(dir))
                {
                    var goodImageFiles = Directory.GetFiles(node.AnomalyConfig.NormalDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                    foreach (var goodImageFile in goodImageFiles)
                    {
                        var score = 0.0;
                        if (normal_image_paths.Contains(goodImageFile))
                        {
                            var idx = normal_image_paths.IndexOf(goodImageFile);
                            if (idx != -1 && idx < GoodPredictions.Count - 1)
                            {
                                score = GoodPredictions[idx];
                            }
                        }
                        
                        result.Add(new ImageSet(goodImageFile) { Tag = "good", Score = score ,DateTime= File.GetCreationTime(goodImageFile) });
                    }
                    var badImageFiles = Directory.GetFiles(node.AnomalyConfig.AnomalyDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                    foreach (var badImageFile in badImageFiles)
                    {
                        var score = 0.0;
                        if (anomalous_image_paths.Contains(badImageFile))
                        {
                            var idx = anomalous_image_paths.IndexOf(badImageFile);
                            if (idx != -1 && idx < BadPredictions.Count - 1)
                            {
                                score = BadPredictions[idx];
                            }
                        }
                        result.Add(new ImageSet(badImageFile) { Tag = "bad", Score = score , DateTime = File.GetCreationTime(badImageFile) });
                    }
                    var unknownImageFiles = Directory.GetFiles(node.AnomalyConfig.UnknownDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                    foreach (var unknownImageFile in unknownImageFiles)
                    {
                        result.Add(new ImageSet(unknownImageFile) { Tag = "unknown", DateTime = File.GetCreationTime(unknownImageFile) });
                    }
                }
                ListImage = new ObservableCollection<ImageSet>(result.OrderByDescending(x=>x.DateTime));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    view = CollectionViewSource.GetDefaultView(ListImage);
                    view.Filter = CustomerFilter;
                });
                
            });
            

        }
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
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
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
        HRegion maskRegion;
        HRegion contextMaskRegion;
        HRegion annotationRegion;
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
        private void DispMaskRegion()
        {
            window_display.HalconWindow.ClearWindow();
            if (maskRegion != null)
            {
                window_display.HalconWindow.SetDraw("fill");
                window_display.HalconWindow.SetColor(AddOpacity("#00ff00ff", ColorOpacity / 100));
                //window_display.HalconWindow.DispRegion(maskRegion);
                if (contextMaskRegion != null)
                {
                    window_display.HalconWindow.DispRegion(contextMaskRegion);
                }
                window_display.HalconWindow.SetColor(AddOpacity("#ff0000ff", ColorOpacity / 100));
                if (annotationRegion != null)
                {
                    window_display.HalconWindow.DispRegion(annotationRegion);
                }

                window_display.HalconWindow.SetDraw("margin");
                window_display.HalconWindow.SetColor("green");
                if (contextMaskRegion != null)
                {
                    window_display.HalconWindow.DispRegion(contextMaskRegion);
                }

                window_display.HalconWindow.SetColor("red");
                if (annotationRegion != null)
                {
                    window_display.HalconWindow.DispRegion(annotationRegion);
                }

            }

        }
        
        public string CurrentImagePath { get; set; }
        HImage SelectedImage { get; set; }
        string GetMaskPath(ImageSet image)
        {
            return System.IO.Path.Combine(node.AnomalyConfig.MaskDir, image.FileName + "." + image.Tag + ".png");
        }
        string GetAnnotationPath(ImageSet image)
        {
            return System.IO.Path.Combine(node.AnomalyConfig.AnnotationDir, image.FileName + "." + image.Tag + ".png");
        }
        private void SetSelectedImage(ImageSet selected)
        {
            if (selected != null)
            {
                //save previous result
                try
                {
                    SaveMetaData(CurrentImagePath + ".txt");
                }
                catch (Exception ex)
                {

                }
                try
                {
                    SelectedImage = new HImage(selected.FullPath);
                    var maskdir = GetMaskPath(selected);
                    var annotationDir = GetAnnotationPath(selected);

                    if (System.IO.File.Exists(maskdir))
                    {
                        var maskImage = new HImage(maskdir);
                        contextMaskRegion= maskImage.Threshold(1.0, 255);
                    }
                    else
                    {
                        contextMaskRegion = null;
                    }

                    if(System.IO.File.Exists(annotationDir))
                    {
                        var annotationImage = new HImage(annotationDir);
                        annotationRegion = annotationImage.Threshold(1.0, 255);
                    }
                    else
                    {
                        annotationRegion = null;
                    }

                    CurrentImagePath = selected.FullPath;
                    SelectedImage.GetImageSize(out imgW, out imgH);
                    ImageRegion = new HRegion(0, 0.0, imgH, imgW);
                    
                    LoadMetaData(CurrentImagePath + ".txt");
                    UpdateMarker(CurrentImageMetadata.ROI);
                    if (CurrentImageMetadata.ROI == null)
                    {
                        if (RegionMaker != null)
                        {
                            window_display.HalconWindow.DetachDrawingObjectFromWindow(RegionMaker.Region.current_draw);
                        }
                        CurrentImageMetadata.ROI = new Rect1(0, 0, imgH, imgW);
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
                        ROIY = (int)RegionMaker.Region.row1;
                        ROIX = (int)RegionMaker.Region.col1;
                        ROIW = (int)RegionMaker.Region.col2 - (int)RegionMaker.Region.col1;
                        ROIH = (int)RegionMaker.Region.row2 - (int)RegionMaker.Region.row1;
                    }
                    else
                    {
                        ROIY = (int)CurrentImageMetadata.ROI.row1;
                        ROIX = (int)CurrentImageMetadata.ROI.col1;
                        ROIW = (int)CurrentImageMetadata.ROI.col2 - (int)CurrentImageMetadata.ROI.col1;
                        ROIH = (int)CurrentImageMetadata.ROI.row2 - (int)CurrentImageMetadata.ROI.row1;
                    }
                    CreateROI(CurrentImageMetadata.ROI);
                    window_display.HalconWindow.AttachBackgroundToWindow(SelectedImage);
                    window_display.HalconWindow.ClearWindow();
                    if (ImageRegion != null & ROIRegion != null)
                    {
                        maskRegion = ImageRegion.Difference(ROIRegion);
                        DispMaskRegion();
                    }
                    //if (System.IO.File.Exists(CurrentImageResultPath))
                    //{
                    //    var imageResult = new HImage(CurrentImageResultPath);
                    //    //if (node.AnomalyConfig.ROI != null)
                    //    //{
                    //    //    var image_croped = image.CropRectangle1(node.AnomalyConfig.ROI.row1, node.AnomalyConfig.ROI.col1, node.AnomalyConfig.ROI.row2, node.AnomalyConfig.ROI.col2);
                    //    //    image_croped.GetImageSize(out int imgW_new, out int imgH_new);
                    //    //    imageResult = imageResult.ZoomImageSize(imgW_new, imgH_new, "constant");
                    //    //    image_croped = image_croped * 0.8 + imageResult * 0.2;
                    //    //    image=image_croped.PaintGray(image.Rectangle1Domain(node.AnomalyConfig.ROI.row1, node.AnomalyConfig.ROI.col1, node.AnomalyConfig.ROI.row2, node.AnomalyConfig.ROI.col2));
                    //    //    image=image.FullDomain();
                    //    //    display.
                    //    //}
                    //    //else
                    //    //{
                    //        imageResult = imageResult.ZoomImageSize(imgW, imgH, "constant");
                    //        image = image * 0.8 + imageResult * 0.2;
                    //    //}


                    //}

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lst_view.SelectedItem = selected;
                        lst_view.ScrollIntoView(selected);
                    });

                    
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
            }
            else
            {
                SelectedImage = null;
            }
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("fill");
                display.SetColor(ColorDraw);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
        #region event onClick for btns
        private void Btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageSet;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListImage.Remove(vm);
                    var imagegtpath = System.IO.Path.GetFileNameWithoutExtension(vm.FullPath);

                    if (System.IO.File.Exists(imagegtpath + ".png.txt"))
                    {
                        System.IO.File.Delete(imagegtpath + ".png.txt");
                    }
                    else if (System.IO.File.Exists(imagegtpath + ".jpg.txt"))
                    {
                        System.IO.File.Delete(imagegtpath + ".jpg.txt");
                    }
                    else if (System.IO.File.Exists(imagegtpath + ".bmp.txt"))
                    {
                        System.IO.File.Delete(imagegtpath + ".bmp.txt");
                    }
                }
                catch (Exception ex)
                {

                }

            }

        }

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        private void window_display_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            train?.Cancel();
        }

        private void window_display_HMouseDoubleClick(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
        }

        private void window_display_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }


        private void window_display_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void window_display_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void btn_train_cmd_Click(object sender, RoutedEventArgs e)
        {
            //box_step.Visibility = Visibility.Hidden;
            try
            {
                node.AnomalyConfig.Save();
                train.TrainConsole(node.AnomalyConfig.ConfigDir, node.AnomalyConfig.ModelName, (TrainingArgs) =>
                {
                    if(TrainingArgs.State == TrainState.Completed)
                    {
                        
                        PostTrain();
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            DXMessageBox.Show(this, "Train Complete!", "Message");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_option_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.AnomalyConfig);
            wd.ShowDialog();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
        #endregion

        #region event for mouses
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            //window_display.Focus();
            if (e.Button == MouseButton.Right)
            {
                row = (int)e.Row;
                col = (int)e.Column;
            }
            Keyboard.Focus(window_display);
        }
        public void AddFromFiles(string directory,string tag,bool cropped=false)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.bmp;*.jpg;*.gif;*.png|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                Task.Run(new Action(() =>
                {

                    ShowMessage = true;
                    try
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            WaitMessage = "Add image " + (i + 1).ToString() + "/" + files.Length;
                            var file = files[i];
                            try
                            {
                                HImage image = new HImage(file);
                                image = node.GetPadImage(image);
                                if (i == files.Length - 1)
                                {
                                    var added = AddImages(image, directory, tag, true);
                                    SetSelectedImage(added);
                                }
                                else
                                {
                                    AddImages(image, directory, tag);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    ShowMessage = false;
                }));
                

            }
            
        }
       
        

        

        private void btn_edit_roi_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
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

        private void btn_apply_roi_all(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                ShowMessage = true;
                WaitMessage = "Appling region of interest to all images....";
                try
                {
                    if (CurrentImageMetadata != null & RegionMaker != null)
                    {
                        CurrentImageMetadata.ROI = new Rect1(RegionMaker.Region.row1, RegionMaker.Region.col1, RegionMaker.Region.row2, RegionMaker.Region.col2);
                        var jsonData = JsonConvert.SerializeObject(CurrentImageMetadata);
                        foreach (var item in ListImage)
                        {
                            File.WriteAllText(item.FullPath + ".txt", jsonData);
                        }

                    }
                }
                catch (Exception ex)
                {

                }

                ShowMessage = false;
            }));
            

        }

        private void btn_change_label_good_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = ((sender as FrameworkContentElement).DataContext as ImageSet);
            if (item != null )
            {
                if (item.Tag != "good")
                {
                    //item.Tag = "good";
                    var dir = node.AnomalyConfig.NormalDir;
                    var newPath = System.IO.Path.Combine(dir, item.FileName);
                    if (!System.IO.File.Exists(newPath))
                    {
                        System.IO.File.Move(item.FullPath, newPath);
                    }
                    else
                    {
                        var newName = Functions.RandomFileName(dir) + ".png";
                        newPath = System.IO.Path.Combine(dir, newName);
                        System.IO.File.Move(item.FullPath, newPath);
                    }
                    var index = ListImage.IndexOf(item);
                    ListImage.Remove(item);
                    var newItem = new ImageSet(newPath) { Tag = "good" };
                    ListImage.Insert(index, newItem);
                }
                
            }
        }

        private void btn_change_label_bad_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = ((sender as FrameworkContentElement).DataContext as ImageSet);
            if (item != null)
            {
                if (item.Tag != "bad")
                {
                    //item.Tag = "good";
                    var dir = node.AnomalyConfig.AnomalyDir;
                    var newPath = System.IO.Path.Combine(dir, item.FileName);
                    if (!System.IO.File.Exists(newPath))
                    {
                        System.IO.File.Move(item.FullPath, newPath);
                    }
                    else
                    {
                        var newName = Functions.RandomFileName(dir) + ".png";
                        newPath = System.IO.Path.Combine(dir, newName);
                        System.IO.File.Move(item.FullPath, newPath);
                    }
                    var index = ListImage.IndexOf(item);
                    ListImage.Remove(item);
                    var newItem = new ImageSet(newPath) { Tag = "bad" };
                    ListImage.Insert(index, newItem);
                }

            }
        }

        //private void chart_BoundDataChanged(object sender, RoutedEventArgs e)
        //{
        //    XYDiagram2D diagram = (XYDiagram2D)chart.Diagram;
        //    diagram.ActualAxisX.ActualWholeRange.SetMinMaxValues(0, 1);
        //}

        private void btn_clear_roi(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            
            
        }

        private void sld_eval_threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (EvalViewModel.SelectedPredictImage != null)
            {
                if (chk_heatmap.IsChecked==false)
                {
                    chk_heatmap.IsChecked = true;
                    //image_detail.HalconWindow.AttachBackgroundToWindow(EvalViewModel.);
                    EvalViewModel.SelectedHeatmapImage = ApplyColorMap(EvalViewModel.SelectedImage, EvalViewModel.SelectedPredictImage, EvalViewModel.EvalThreshold);
                    if (image_detail.HalconWindow != null)
                    {
                        image_detail.HalconWindow.AttachBackgroundToWindow(EvalViewModel.SelectedHeatmapImage);
                    }
                }
                else
                {
                    EvalViewModel.SelectedHeatmapImage = ApplyColorMap(EvalViewModel.SelectedImage, EvalViewModel.SelectedPredictImage, EvalViewModel.EvalThreshold);
                    if (image_detail.HalconWindow != null)
                    {
                        image_detail.HalconWindow.AttachBackgroundToWindow(EvalViewModel.SelectedHeatmapImage);
                    }
                    
                    //image_detail.HalconWindow.DispImage(imageimposed);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach(var item  in EvalViewModel.ClassificationResult)
            {
                if (item.Probability > EvalViewModel.EvalThreshold)
                {
                    item.Predict = "bad";
                }
                else
                {
                    item.Predict = "good";
                }
            }
            UpdateEvaluation(EvalViewModel.ClassificationResult);
        }
        string AddImageTag;
        private void btn_add_image_category_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).DataContext.ToString().ToLower();
            if (tag == "all")
            {
                tag = "unknown";
            }
            AddImageTag = tag;
            box_add_images.Visibility = Visibility.Visible;
            //AddFromFiles(node.AnomalyConfig.ImageDir, tag);
        }
        private void btn_open_folder(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", node.AnomalyConfig.ImageDir);
        }

        private void btn_add_image_camera_category_Click(object sender, RoutedEventArgs e)
        {
            
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
            if (EvalViewModel.SelectedHeatmapImage != null)
            {
                image_detail.HalconWindow?.AttachBackgroundToWindow(EvalViewModel.SelectedHeatmapImage);
            }
            else
            {     
                if(EvalViewModel.SelectedImage != null & EvalViewModel.SelectedPredictImage!=null)
                {
                    var imageimposed = ApplyColorMap(EvalViewModel.SelectedImage, EvalViewModel.SelectedPredictImage, EvalViewModel.EvalThreshold);
                    image_detail.HalconWindow?.AttachBackgroundToWindow(imageimposed);
                    EvalViewModel.SelectedHeatmapImage = imageimposed; ;
                }
                
            }
            
        }

        private void chk_heatmap_Unchecked(object sender, RoutedEventArgs e)
        {
            if (EvalViewModel.SelectedImage != null)
            {
                image_detail.HalconWindow?.AttachBackgroundToWindow(EvalViewModel.SelectedImage);
            }
        }

        IMaskGeneration maskGeneration;
        IMaskGeneration annotationGenneration;
        private void btn_edit_mask_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {

                 
                var result    = maskGeneration.ShowEditor(selected, SelectedImage);
                if (result != null)
                {
                    contextMaskRegion = result;
                    DispOverlay();
                }
                
                
            }

        }
        private void btn_edit_annotation_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {


                var result = annotationGenneration.ShowEditor(selected, SelectedImage);
                if (result != null)
                {
                    annotationRegion = result;
                    DispOverlay();
                }


            }

        }

        private void btn_apply_mask_all(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                ShowMessage = true;
                WaitMessage = "Appling mask to all images....";
                try
                {
                    if (contextMaskRegion != null )
                    {
                        SelectedImage.GetImageSize(out int w, out int h);
                        HImage region = new HImage("byte", w, h);
                        region.OverpaintRegion(contextMaskRegion, new HTuple(255), "fill");
                        foreach (var item in ListImage)
                        {
                            var maskDir = GetMaskPath(item);
                            region.WriteImage("png", 0, maskDir);
                        }

                    }
                }
                catch (Exception ex)
                {

                }

                ShowMessage = false;
            }));
        }

        private void btn_auto_mask_Click(object sender, RoutedEventArgs e)
        {
            maskGeneration = new BlobMaskGeneration(node.AnomalyConfig.MaskDir);
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {

                contextMaskRegion = maskGeneration.ShowEditor(selected, SelectedImage);
                DispOverlay();

            }
            //try
            //{
            //    if (SelectedImage != null)
            //    {
            //        SelectedImage.GetImageSize(out int w, out int h);
            //        var maskThresh = SelectedImage.Rgb1ToGray().Threshold(slider_range.SelectionStart, slider_range.SelectionEnd);
            //        HImage region = new HImage("byte", w, h);
            //        region.OverpaintRegion(maskThresh, new HTuple(255), "fill");
            //        var maskDir = GetMaskPath((lst_view.SelectedItem as ImageSet));
            //        region.WriteImage("png", 0, maskDir);
            //        contextMaskRegion = maskThresh;
            //        DispOverlay();
            //    }
            //}catch(Exception ex)
            //{

            //}


        }

        private void btn_auto_mask_all_Click(object sender, RoutedEventArgs e)
        {
            //maskGeneration = new BlobMaskGeneration(node.AnomalyConfig.MaskDir);
            var selected = lst_view.SelectedItem as ImageSet;
            if (selected != null)
            {
                label_wait.DeferedVisibility = true;
                label_wait.Content = "Applying mask to all images...";
                Task.Run(() =>
                {
                    try
                    {
                        maskGeneration.ApplyMaskFunction(selected, ListImage);
                    }catch(Exception ex)
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
            //double start = slider_range.SelectionStart;
            //double end = slider_range.SelectionEnd;
            //Task.Run(new Action(() =>
            //{
            //    ShowMessage = true;
            //    WaitMessage = "Appling mask to all images....";
            //    try
            //    {
            //        if (contextMaskRegion != null)
            //        {

            //            foreach (var item in ListImage)
            //            {
            //                var maskDir = GetMaskPath(item);
            //                var image = new HImage(item.FullPath).Rgb1ToGray();
            //                image.GetImageSize(out int w, out int h);
            //                HImage mask = new HImage("byte", w, h);
            //                var maskThresh = image.Threshold(start, end);
            //                mask.OverpaintRegion(maskThresh, new HTuple(255), "fill");
            //                mask.WriteImage("png", 0, maskDir);
            //            }

            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }

            //    ShowMessage = false;
            //}));
        }

        private void cmb_mask_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmb_mask_mode.SelectedIndex)
            {
                case 0: maskGeneration = new MannualMaskGeneration(node.AnomalyConfig.MaskDir);break;
                case 1: maskGeneration = new BlobMaskGeneration(node.AnomalyConfig.MaskDir); break;
            }
        }

        private void btn_change_dataset_train_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = ((sender as FrameworkContentElement).DataContext as ImageSet);
            if (item != null)
            {
                if (item.MetaData.Dataset != DatasetSplit.train)
                {
                    //item.Tag = "good";
                    item.MetaData.Dataset = DatasetSplit.train;
                    item.SaveMetaData();
                }

            }
        }

        private void btn_change_dataset_test_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = ((sender as FrameworkContentElement).DataContext as ImageSet);
            if (item != null)
            {
                if (item.MetaData.Dataset != DatasetSplit.test)
                {
                    //item.Tag = "good";
                    item.MetaData.Dataset = DatasetSplit.test;
                    item.SaveMetaData();
                }

            }
        }

        private void btn_close_add_images_Click(object sender, RoutedEventArgs e)
        {
            box_add_images.Visibility = Visibility.Hidden;
        }

        private void btn_add_image_from_files_Click(object sender, RoutedEventArgs e)
        {
            AddFromFiles(node.AnomalyConfig.ImageDir, AddImageTag);
        }
        //HImage CropImage(HImage image, HRegion region)
        //{
        //    region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
        //    var imageCroped = image.CropRectangle1(row1, col1, row2, col2);
        //    imageCroped.GetImageSize(out int w, out int h);
        //    HRegion diffrg = new HRegion(0, 0.0, h, w);
        //    var subRg = diffrg.Difference(region.MoveRegion(-row1, -col1));
        //    imageCroped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
        //    return imageCroped;
        //}
        private void btn_add_image_input_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = node.Image.GetCurrentConnectionValue();
                if (image != null)
                {
                    HImage imageToSave;
                    
                    
                    if (image.IsInitialized())
                    {
                        imageToSave = node.GetPadImage(image);
                        var added = AddImages(imageToSave, node.AnomalyConfig.ImageDir, AddImageTag, Prepend: true);                        
                        SetSelectedImage(added);
                        DXMessageBox.Show("Image added successfully!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        return;
                    }

                }
                DXMessageBox.Show("Cannot add image, no image from input!", "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }catch(Exception ex)
            {

            }
            
        }

        private void btn_record_input_Click(object sender, RoutedEventArgs e)
        {
            string directory = "";
            if (AddImageTag == "good")
            {
                directory = node.AnomalyConfig.NormalDir;
            }
            if (AddImageTag == "bad")
            {
                directory = node.AnomalyConfig.AnomalyDir;
            }
            if (AddImageTag == "unknown")
            {
                directory = node.AnomalyConfig.UnknownDir;
            }

            RecordInputImageWindow wd = new RecordInputImageWindow(node.Image, directory);
            wd.Owner = this;
            wd.Show();
        }

        private void btn_add_image_from_files_input_Click(object sender, RoutedEventArgs e)
        {
            AddFromFiles(node.AnomalyConfig.ImageDir, AddImageTag,true);
        }

        private void image_detail_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (e.Row >= 0 & e.Column >= 0 & e.Row <= EvalViewModel.PredictImageHeight 
                & e.Column <= EvalViewModel.PredictImageWidth & EvalViewModel.SelectedPredictImage != null)
            {
                EvalViewModel.PixelScore = EvalViewModel.SelectedPredictImage.GetGrayval(((int)e.Row), ((int)e.Column));
            }
            
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cmb_model_name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var Properties = new ObservableCollection<Property>();
            switch (node.AnomalyConfig.ModelName)
            {
                
                case "FAPM":
                    grid_model_prop.SelectedObject = node.AnomalyConfig.FAPMConfig;
                    //var Properties = new ObservableCollection<Property>();
                    Properties.Add(new Property() { Name = "MODEL_TYPE" });
                    Properties.Add(new Property() { Name = "DISTANCE_GAIN_FOR_KNN" });
                    Properties.Add(new Property() { Name = "N_SAMPLES" });
                    Properties.Add(new Property() { Name = "PERCENT_RETRAINED" });
                    Properties.Add(new Property() { Name = "PatchWidth" });
                    Properties.Add(new Property() { Name = "PatchHeight" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "EfficientAD":
                    grid_model_prop.SelectedObject = node.AnomalyConfig.EfficientADConfig;
                    //var Properties = new ObservableCollection<Property>();
                    Properties.Add(new Property() { Name = "MODEL_TYPE" });
                    Properties.Add(new Property() { Name = "DETECTION_TYPE" });
                    Properties.Add(new Property() { Name = "EVAL_INTERVAL" });
                    Properties.Add(new Property() { Name = "PATIENCE" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "FAPM_Pytorch":
                    grid_model_prop.SelectedObject = node.AnomalyConfig.FAPMConfig;
                    //var Properties = new ObservableCollection<Property>();
                    Properties.Add(new Property() { Name = "MODEL_TYPE" });
                    Properties.Add(new Property() { Name = "DISTANCE_GAIN_FOR_KNN" });
                    Properties.Add(new Property() { Name = "N_SAMPLES" });
                    Properties.Add(new Property() { Name = "CoresetSamplingRatio" });
                    Properties.Add(new Property() { Name = "PatchWidth" });
                    Properties.Add(new Property() { Name = "PatchHeight" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "MemSeg_v2":
                    grid_model_prop.SelectedObject = node.AnomalyConfig;
                    
                    Properties.Add(new Property() { Name = "BIAS_GAIN_FOR_BAD_IMAGES" });
                    Properties.Add(new Property() { Name = "N_SAMPLES" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "MemSeg":
                    grid_model_prop.SelectedObject = node.AnomalyConfig;

                    Properties.Add(new Property() { Name = "BIAS_GAIN_FOR_BAD_IMAGES" });
                    Properties.Add(new Property() { Name = "N_SAMPLES" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "MemSeg_Pytorch":
                    grid_model_prop.SelectedObject = node.AnomalyConfig;
                    Properties.Add(new Property() { Name = "TrainType" });
                    Properties.Add(new Property() { Name = "TestRatio" });
                    Properties.Add(new Property() { Name = "BACKBONE" });
                    Properties.Add(new Property() { Name = "LR_INIT" });
                    Properties.Add(new Property() { Name = "MIN_LR" });
                    Properties.Add(new Property() { Name = "EVAL_INTERVAL" });
                    Properties.Add(new Property() { Name = "NUM_WORKERS" });
                    Properties.Add(new Property() { Name = "MEMORY_SAMPLE" });
                    Properties.Add(new Property() { Name = "VisualizeAugmentations" });
                    Properties.Add(new Property() { Name = "PerlinScale" });
                    Properties.Add(new Property() { Name = "MinPerlinScale" });
                    Properties.Add(new Property() { Name = "PerlinNoiseThreshold" });
                    Properties.Add(new Property() { Name = "StructureGridSize" });
                    Properties.Add(new Property() { Name = "TransparencyRangeHigh" });
                    Properties.Add(new Property() { Name = "TransparencyRangeLow" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                case "AESeg_Pytorch":
                    grid_model_prop.SelectedObject = node.AnomalyConfig;
                    Properties.Add(new Property() { Name = "TrainType" });
                    Properties.Add(new Property() { Name = "BACKBONE" });
                    Properties.Add(new Property() { Name = "LR_INIT" });
                    Properties.Add(new Property() { Name = "MIN_LR" });
                    Properties.Add(new Property() { Name = "EVAL_INTERVAL" });
                    Properties.Add(new Property() { Name = "NUM_WORKERS" });
                    Properties.Add(new Property() { Name = "MEMORY_SAMPLE" });
                    Properties.Add(new Property() { Name = "VisualizeAugmentations" });
                    Properties.Add(new Property() { Name = "PerlinScale" });
                    Properties.Add(new Property() { Name = "MinPerlinScale" });
                    Properties.Add(new Property() { Name = "PerlinNoiseThreshold" });
                    Properties.Add(new Property() { Name = "StructureGridSize" });
                    Properties.Add(new Property() { Name = "TransparencyRangeHigh" });
                    Properties.Add(new Property() { Name = "TransparencyRangeLow" });
                    grid_model_prop.PropertyDefinitionsSource = Properties;
                    break;
                default:
                    grid_model_prop.SelectedObject = null;
                    break;
            }
            
           
        }
        string lastSearchText = string.Empty;
        IEnumerator<ImageSet> lastSearchResult = null;
        private void btn_search_Click(object sender, RoutedEventArgs e)
        {
            var text = txt_search.Text;
            if (text != "")
            {
                if(text!= lastSearchText)
                {
                    lastSearchText = text;
                    var result = ListImage.Where(x => x.FileName.Contains(text));
                    if(result != null)
                    {
                        lastSearchResult = result.GetEnumerator();
                        lastSearchResult.MoveNext();
                        lst_view.ScrollIntoView(lastSearchResult.Current);
                        lst_view.SelectedItem = lastSearchResult.Current;
                    }
                    else
                    {
                        lastSearchResult = null;
                    }
                }
                else
                {
                    if (lastSearchResult != null)
                    {
                        if (lastSearchResult.MoveNext())
                        {
                            lst_view.ScrollIntoView(lastSearchResult.Current);
                            lst_view.SelectedItem = lastSearchResult.Current;
                        }
                        
                        

                    }
                }
            }
        }

        private void lst_display_filter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var selecteds = e.NewValue as IEnumerable<object>;
                SelectedDisplayFilter = new List<DatasetSplit>();
                foreach (var item in selecteds)
                {
                    SelectedDisplayFilter.Add((DatasetSplit)item );
                }
            }
            else
            {
                SelectedDisplayFilter = null;
            }
            view?.Refresh();
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
                    if (node.AnomalyRuntime.State != ModelState.Loaded)
                    {
                        node.AnomalyRuntime.LoadRecipe();
                    }

                        if (node.AnomalyRuntime.State == ModelState.Loaded)
                    {
                        //EvalViewModel.SelectedImage = newImage;
                        newImage.WriteImage("png", 0, selected.Image);
                        var predictionResult = node.AnomalyRuntime.Infer(newImage, out double score);
                        predictionResult.WriteImage("hobj", 0, selected.PredictImage);
                        ChangeEvalImage(selected);
                    }
                    //selected.PredictImage
                }
            }
        }

        private void image_detail_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if(annotation_region != null)
            {
                
                image_detail.HalconWindow.SetColor("black");
                image_detail.HalconWindow.SetDraw("margin");
                image_detail.HalconWindow.SetLineWidth(3);
                image_detail.HalconWindow.SetLineStyle(new HTuple(5,10));
                //image_detail.HalconWindow.AttachBackgroundToWindow();
                image_detail.HalconWindow.DispObj(annotation_region);
            }
        }

        private void image_detail_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            image_detail.HalconWindow.ClearWindow();
            EvalViewModel.ShowHeatmap = true;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindingExpression be = GetBindingExpression(TextBox.TextProperty);
                if (be != null)
                {
                    be.UpdateSource();
                }
            }
        }
        string _export_dataset_directory;
        public string ExportDatasetDirectory
        {
            get
            {
                return _export_dataset_directory;
            }
            set
            {
                if (_export_dataset_directory != value)
                {
                    _export_dataset_directory = value;
                    RaisePropertyChanged("ExportDatasetDirectory");
                }
            }
        }
        string _export_dataset_name = "DefaultExportData";
        public string ExportDatasetName
        {
            get
            {
                return _export_dataset_name;
            }
            set
            {
                if (_export_dataset_name != value)
                {
                    _export_dataset_name = value;
                    RaisePropertyChanged("ExportDatasetName");
                }
            }
        }
        private void btn_open_exportdataset_menu_Click(object sender, RoutedEventArgs e)
        {
            box_export.Visibility = Visibility.Visible;
        }

        private void change_export_directory_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            //diag.folder
            // diag.SelectedPath = acq.Record_path;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {

                ExportDatasetDirectory = dialog.FileName;
            }
        }
        bool _is_export_dataset;
        public bool IsExportDataset
        {
            get
            {
                return _is_export_dataset;
            }
            set
            {
                if (_is_export_dataset != value)
                {
                    _is_export_dataset = value;
                    RaisePropertyChanged("IsExportDataset");
                }
            }
        }
        
        /// <summary>
        /// MVTecAD format
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="Format"></param>
        private void ExportDatasetFolder(string directory, string Format = "png")
        {
            CreateIfNotExist(directory);
            //create train and test folder, ground truth
            string trainFolder = System.IO.Path.Combine(directory, "train", "good");
            string testGoodFolder = System.IO.Path.Combine(directory, "test", "good");
            string testBadFolder = System.IO.Path.Combine(directory, "test", "bad");
            string gtFolder = System.IO.Path.Combine(directory, "ground_truth", "bad");
            CreateIfNotExist(trainFolder);
            CreateIfNotExist(testGoodFolder);
            CreateIfNotExist(testBadFolder);
            CreateIfNotExist(gtFolder);
            int totalFile = ListImage.Count;
            int count = 0;
            foreach (var ImageFile in ListImage.Where(x=>x.Tag=="good"))
            {
                var extension = System.IO.Path.GetExtension(ImageFile.FullPath);
                System.IO.File.Copy(ImageFile.FullPath, System.IO.Path.Combine(trainFolder, count.ToString() +  extension));
                count++;
            }
            foreach (var ImageFile in ListImage.Where(x => x.Tag == "bad"))
            {
                var extension = System.IO.Path.GetExtension(ImageFile.FullPath);
                System.IO.File.Copy(ImageFile.FullPath, System.IO.Path.Combine(testBadFolder, count.ToString() + extension));
                var annotationFile = GetAnnotationPath(ImageFile);
                if (System.IO.File.Exists(annotationFile))
                {
                    var extension1 = System.IO.Path.GetExtension(annotationFile);
                    System.IO.File.Copy(annotationFile, System.IO.Path.Combine(gtFolder, count.ToString() + "_mask" + extension));
                }
                count++;
            }

        }

        private static void CreateIfNotExist(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }

        private void btn_export_dataset_click(object sender, RoutedEventArgs e)
        {
            if (ExportDatasetDirectory != "")
            {
                Task.Run(() =>
                {
                    IsExportDataset = true;
                    try
                    {
                        string path = System.IO.Path.Combine(ExportDatasetDirectory, ExportDatasetName);
                        ExportDatasetFolder(path);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(this, "Dataset exported to: " + path, "Export complete");
                            box_export.Visibility = Visibility.Hidden;
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(this, ex.Message, "Export error");
                        });

                    }
                    IsExportDataset = false;

                });

            }
            else
            {
                MessageBox.Show("Directory not exist, please check again!");
            }
        }

        private void btn_cancel_dataset_click(object sender, RoutedEventArgs e)
        {
            box_export.Visibility = Visibility.Hidden;
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
            }
        }

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
        }
        #endregion

        #region event for checkbox
        #endregion
        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
            bool _show_heatmap=true;
            public bool ShowHeatmap
            {
                get
                {
                    return _show_heatmap;
                }
                set
                {
                    if (_show_heatmap != value)
                    {
                        _show_heatmap = value;
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
            HImage _selected_predict_image;
            public HImage SelectedPredictImage 
            { 
                get { return _selected_predict_image; }
                set
                {
                    _selected_predict_image = value;
                    if(value != null)
                    {
                        _selected_predict_image.GetImageSize(out int w, out int h);
                        PredictImageWidth = w;
                        PredictImageHeight = h;
                    }
                }
            }
            public HImage SelectedHeatmapImage { get; set; }
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
            double _eval_threshold;
            public double EvalThreshold
            {
                get
                {
                    return _eval_threshold;
                }
                set
                {
                    if (_eval_threshold != value)
                    {
                        _eval_threshold = value;
                        RaisePropertyChanged("EvalThreshold");
                    }
                }
            }

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

            double _f1score;
            public double F1Score
            {
                get
                {
                    return _f1score;
                }
                set
                {
                    if (_f1score != value)
                    {
                        _f1score = value;
                        RaisePropertyChanged("F1Score");
                    }
                }
            }
            public List<ClassificationResult> ClassificationResult { get; set; }
            List<ClassCount> _class_summary_list;
            public List<ClassCount> ClassSummaryList
            {
                get
                {
                    return _class_summary_list;
                }
                set
                {
                    if (_class_summary_list != value)
                    {
                        _class_summary_list = value;
                        RaisePropertyChanged("ClassSummaryList");
                    }
                }
            }
            List<ClassSummary> _precision_list;
            public List<ClassSummary> PrecisionList
            {
                get
                {
                    return _precision_list;
                }
                set
                {
                    if (_precision_list != value)
                    {
                        _precision_list = value;
                        RaisePropertyChanged("PrecisionList");
                    }
                }
            }

        }
    }

}
