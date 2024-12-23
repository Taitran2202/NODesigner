using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Controls;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;
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
using ReactiveUI;
using DynamicData;
using System.Reactive.Linq;
using NOVisionDesigner.Designer.Extensions;

namespace NOVisionDesigner.Designer.Windows
{

    public partial class ClassifierEditorWindow : ThemedWindow, INotifyPropertyChanged
    {
        ClassifierLabelViewModel LabelViewModel { get; set; } = new ClassifierLabelViewModel();
        bool _is_trainning;
        public bool IsTraining
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
                    RaisePropertyChanged("IsTraining");
                }
            }
        }
        
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        bool first_initialize = true;
        void LoadImageList(string dir)
        {
            Task.Run(() =>
            {
                var result = new ObservableCollection<ImageSet>();
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                    foreach (string file in files)
                    {
                        var item = new ImageSet(file) {DateTime = File.GetCreationTime(file) } ;
                        var annotationPath = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(item.FullPath) + ".txt");
                        if (System.IO.File.Exists(annotationPath))
                        {
                            try
                            {


                                var data = JsonConvert.DeserializeObject<JObject[]>(File.ReadAllText(annotationPath));
                                if (data != null)
                                {
                                    foreach (var item2 in data)
                                    {
                                        var annotation = item2["annotation"].ToString();
                                        if (!item.Tags.Contains(annotation))
                                        {
                                            item.Tags.Add(annotation);
                                        }

                                    }
                                }
                            }catch (Exception ex)
                            {

                            }
                            
                        }
                        result.Add(item);
                    }

                }
                if (LabelViewModel.ListImage == null)
                {
                    LabelViewModel.ListImage = new ObservableCollection<ImageSet>();
                }
                LabelViewModel.ListImage.Clear();
                LabelViewModel.ListImage.AddRange(result.OrderByDescending(x=>x.DateTime));
                
                if (LabelViewModel.ListImage.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LabelViewModel.SelectedImage = LabelViewModel.ListImage.First();
                    });
                    
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    lst_view.SelectedIndex = 0;
                    //});
                    
                    //window_display.SetFullImagePart();
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    view = CollectionViewSource.GetDefaultView(LabelViewModel.ListImage);
                    view.Filter = CustomerFilter;
                });
            });
            
        }
        ObservableCollection<ClassifierClass1> _ClassList;
        public ObservableCollection<ClassifierClass1> ClassList
        {
            get
            {
                return _ClassList;
            }
            set
            {
                if (_ClassList != value)
                {
                    _ClassList = value;
                    RaisePropertyChanged("ClassList");
                   
                }
            }
        }

        //public bool Retrained { get; set; } = false;
        RegionMaker _selected_marker;
        public RegionMaker SelectedMarker
        {
            get
            {
                return _selected_marker;
            }
            set
            {
                if (_selected_marker != value)
                {
                    _selected_marker = value;
                    RaisePropertyChanged("SelectedMarker");
                }
            }
        }


        public List<RegionMaker> CurrentRegionList { get; set; } = new List<RegionMaker>();
        public void UpdateSelectBoxPosition()
        {
            if (SelectedMarker != null)
            {
                double winposx, winposy;
                display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row1, SelectedMarker.Region.col1, out winposx, out winposy);
                cmb_select_class.Margin = new Thickness(winposy, winposx - 35, 0, 0);
            }

        }
        public void Update(RegionMaker sender, Region region)
        {
            //Selected_region = sender;
            //UpdateSelectBoxPosition();
            ChangeRegion();
            DispOverlay();
        }
        public void Selected(RegionMaker sender, Region region)
        {
            //Selected_region = sender;
            ChangeRegion();
            DispOverlay();
            SelectedMarker = sender;
            UpdateSelectBoxPosition();
        }
        public void DispOverlay()
        {
            if (display == null)
                return;
            display.ClearWindow();
            display.SetDraw("fill");
            foreach (var item in CurrentRegionList)
            {
                if (item.Annotation != null)
                {
                    display.SetColor(AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                    display.DispText(item.Annotation.Name, "image", item.Region.row1, item.Region.col1, "black", new HTuple("box_color"), new HTuple(item.Annotation.Color));
                }
                
                display.DispObj(item.Region.region);
                
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            DispOverlay();
        }
        int row, col;
        private void AddRegion(RegionMaker region)
        {
            RegionMaker region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }
            ChangeRegion();
        }
        private void AddRegionNew(RegionMaker region)
        {
            region.Region.Initial((int)row, (int)col);
            RegionMaker region_add = region;
            CurrentRegionList.Add(region_add);

            region_add.Attach(display, null, Update, Selected);

            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }

            //lst_region.SelectedItem = region_add;
            ChangeRegion();
        }
        Python.TrainClassifier train = new Python.TrainClassifier();
        DeepClassifier node;
        public ClassifierEditorWindow(DeepClassifier node)
        {
            InitializeComponent();
            this.node = node;
            this.ClassList = node.ClassList;
            cmb_select_class.ItemsSource = this.ClassList;
            lst_class.ItemsSource = this.ClassList;
            btn_add_image.Click += Btn_add_image_Click;
            btn_add_image_camera.Click += (s, e) =>
            {
                if (node != null)
                {
                   
                    var imageFeed = node.ImageInput.GetCurrentConnectionValue();
                    HRegion regionInput = null;
                    //if (classifier.RegionInput.Value.SourceLink != null)
                    //{
                    if (node.Regions.Value != null)
                    {
                        regionInput = node.Regions.Value.Connection().SelectShape("area","and",2,9999999999999);
                    }
                    //}
                    else
                    {
                       

                    }


                    if (imageFeed != null)

                            try
                            {
                                var image = imageFeed.Clone();
                               // var filename = DateTime.Now.Ticks.ToString();
                                //var newfile = System.IO.Path.Combine(node.TrainConfig.ImageDir, filename);
                            var newfile = NOVisionDesigner.Designer.Extensions.Functions.NewFileName(node.TrainConfig.ImageDir, "png", "Image");
                                image.WriteImage("png", 0, newfile);
                                var imageadded = new ImageSet(newfile) { DateTime= DateTime.Now };
                            first_initialize = true;
                                LabelViewModel.ListImage.Insert(0,imageadded);
                                LabelViewModel.SelectedImage = imageadded;
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                lst_view.ScrollIntoView(imageadded);
                                var listBoxItem = (ListBoxItem)lst_view
                                 .ItemContainerGenerator
                                 .ContainerFromItem(imageadded);

                                listBoxItem?.Focus();
                            }));
                                
                                //lst_view.SelectedItem = imageadded;
                                if (regionInput != null)
                                    try
                                    {
                                        var region = regionInput;
                                        for (int i = 0; i < region.CountObj(); i++)
                                        {
                                            HTuple row1, col1, row2, col2;
                                            region[i + 1].SmallestRectangle1(out row1, out col1, out row2, out col2);
                                            ClassifierClass1 newclass = null;
                                            if (ClassList.Count > 0)
                                            {
                                                newclass = ClassList[0];
                                            }
                                            if (newclass != null)
                                            {
                                                var region_add = new RegionMaker() { Annotation = newclass, 
                                                    Region = new Rectange1(false) { row1 = row1, col1 = col1, row2 = row2, col2 = col2 } };

                                                CurrentRegionList.Add(region_add);

                                                region_add.Attach(display, null, Update, Selected);


                                            }
                                            else
                                            {
                                                var region_add = new RegionMaker() { Annotation = new ClassifierClass1() { Name = "unknow" }, 
                                                    Region = new Rectange1(false) { row1 = row1, col1 = col1, row2 = row2, col2 = col2 } };

                                                CurrentRegionList.Add(region_add);

                                                region_add.Attach(display, null, Update, Selected);
                                            }
                                        }
                                        if (CurrentRegionList.Count > 0)
                                        {
                                            SelectedMarker = CurrentRegionList[0];
                                            ChangeRegion();
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                
            };


            btn_train.Click += (sender, e) =>
            {
                if (TrainModeSelection())
                {
                    StartTrainning();
                }

            };
            btn_stop_train.Click += (sender, e) =>
            {
                train?.Cancel();
            };
            
            this.DataContext = this;
            stack_train_parameter.DataContext = node.TrainConfig;
            propertiesGrid.SelectedObject = node.Runtime;
            this.Closed += (o, e) =>
            {
                trainningStatusViewModel?.Dispose();
            };
            stack_augmentation.DataContext = node.TrainConfig.Augmentations;
            tab_eval.DataContext = EvalViewModel;
            confusionMatrix.SelectionChanged += List_confusion_matrix_SelectionChanged;

            lst_view.DataContext = LabelViewModel;
            lst_display_filter.DataContext = LabelViewModel;
            lst_display_filter.ItemsSource = ClassList;
            LoadImageList(node.TrainConfig.ImageDir);
            LabelViewModel.WhenAnyValue(x => x.SelectedImage).Buffer(2, 1).Select(b => (Previous: b[0], Current: b[1])).Subscribe(t =>
            {
                if (t.Previous != null)
                {
                    if(LabelViewModel.ListImage.Contains(t.Previous))
                    {
                        SaveAnnotation(t.Previous, CurrentRegionList);
                    }
                    
                }
                OnSelectedImageChange(t.Current);
            });

        }
        bool TrainModeSelection()
        {
            if (true)
            {
                var dataPath = node.TrainConfig.ModelDir;
                var bestScore = System.IO.Path.Combine(dataPath, "best_epoch.json");
                var BestCheckPoint = new ClassifierCheckpointInfo();
                try
                {
                    JObject bestScoreData = JObject.Parse(File.ReadAllText(bestScore));
                    BestCheckPoint.Epoch = bestScoreData["epoch"].ToObject<int>();
                    BestCheckPoint.ValidationAccuracy = bestScoreData["val_accuracy"].ToObject<double>();
                    BestCheckPoint.ValidationLoss = bestScoreData["val_loss"].ToObject<double>();
                }
                catch (Exception ex)
                {

                }
                

                TrainModeSelectionWindow wd = new TrainModeSelectionWindow(BestCheckPoint);
                wd.Owner = this;
                if (wd.ShowDialog() == true)
                {
                    if (wd.TrainType == TrainResume.New)
                    {
                        node.TrainConfig.TrainType = wd.TrainType;
                    }
                    else
                    {
                        node.TrainConfig.TrainType = wd.TrainType;
                        node.TrainConfig.CheckPoint = wd.CheckPoint;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
        }



        bool eval_loaded = false;
        public void LoadLastEvaluationResult()
        {
            string resultDir = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "result.txt");
            if (File.Exists(resultDir))
            {
                try
                {
                    EvalViewModel = JsonConvert.DeserializeObject<EvaluationViewModel>(File.ReadAllText(resultDir));
                    foreach (var item in EvalViewModel.ListClassification)
                    {
                        if (!item.Image.Contains(node.TrainConfig.EvaluationDir))
                        {
                            item.Image = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "images", item.Label, System.IO.Path.GetFileName(item.Image));

                        }
                    }
                    tab_eval.DataContext = EvalViewModel;
                    UpdateEvaluation(EvalViewModel);
                }
                catch(Exception ex)
                {

                }
                
                //recreate classification dir in case node was copied
                
            }
        }
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
        TrainningStatusViewModel trainningStatusViewModel;
        public void StartTrainning()
        {
            trainningStatusViewModel?.Dispose();
            trainningStatusViewModel = new TrainningStatusViewModel();
            grid_trainning_status.DataContext = trainningStatusViewModel;
            node.TrainConfig.Save();
            Task.Run(new Action(() =>
            {
                IsTraining = true;
                node.TrainConfig.ClassList = "[" + String.Join(",", ClassList.Select(x => string.Format("'{0}'", x.Name)).ToArray()) + "]";
                node.TrainConfig.Save();
                trainningStatusViewModel.StartListen();
                //classifier.ReduceSessionMemory();
                train.TrainPython(node.TrainConfig.ConfigDir, (trainargs) =>
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
            node.Runtime.State = ModelState.Unloaded;
            Application.Current.Dispatcher.Invoke(() =>
            {
                propertiesGrid.SelectedObject = null;
                propertiesGrid.SelectedObject = node.Runtime;
            });
        }
        private void Btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.GIF;*.PNG|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                foreach (var file in files)
                {
                    try
                    {
                        var extension = System.IO.Path.GetExtension(file);
                        var newfile = Functions.RandomFileName(node.TrainConfig.ImageDir)+ extension;


                        System.IO.File.Copy(file, newfile);
                        var new_image = new ImageSet(newfile) { DateTime= DateTime.Now};
                        LabelViewModel.ListImage.Add(new_image);
                        lst_view.SelectedItem = new_image;
                        lst_view.ScrollIntoView(new_image);
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
        //string _image_name;
        //public string ImageName
        //{
        //    get
        //    {
        //        return _image_name;
        //    }
        //    set
        //    {
        //        if (_image_name != value)
        //        {
        //            _image_name = value;
        //            RaisePropertyChanged("ImageName");
        //        }
        //    }
        //}

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
        //HImage image_gt;
        //string annotation_path;
        public void SaveResult(ImageFilmstrip image)
        {
            
            SaveAnnotation(image, CurrentRegionList);
        }
        //color are define with format "#rrggbbaa".
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        private void btn_pre_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
        }
        public void PredictImage()
        {

        }
        int im_w, im_h;
        //ImageFilmstrip SelectedImage { get; set; }
        void OnSelectedImageChange(ImageFilmstrip selected)
        {
            if (selected != null)
            {
                try
                {
                    image = new HImage(selected.FullPath);
                    image.GetImageSize(out im_w, out im_h);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                    window_display.HalconWindow.ClearWindow();
                    if (first_initialize)
                    {
                        window_display.SetFullImagePart();
                        first_initialize = false;
                    }
                }
                catch (Exception ex)
                {

                }

                //load current annotation
                try
                {

                    LoadAnnotation(selected);

                }
                catch (Exception ex)
                {

                }
                if (CurrentRegionList.Count == 0)
                {
                    SelectedMarker = null;
                    cmb_select_class.Margin = new Thickness(-100, -100, 0, 0);
                }
                UpdateSelectBoxPosition();
                if (AutoPredict)
                {
                    PredictCurrentImage();
                }
                
            }
            else
            {
                ClearAnnotation();
                window_display.HalconWindow.DetachBackgroundFromWindow();
                window_display.HalconWindow.ClearWindow();
                cmb_select_class.Margin = new Thickness(-100, -100, 0, 0);
                
            }
            
        }
        
        public void ClearAnnotation()
        {
            foreach (var item in CurrentRegionList)
            {
                item.Region.ClearDrawingData(display);
            }
            CurrentRegionList.Clear();
        }

        public void SaveAnnotation(ImageFilmstrip image,List<RegionMaker> RegionList)
        {
            var path = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(image.FullPath) + ".txt");
            if (path != null)
            {
                JObject[] data = new JObject[RegionList.Count];
                for (int i = 0; i < data.Length; i++)
                {
                    if (RegionList[i].Annotation == null)
                    {
                        return;
                    }
                    data[i] = new JObject();
                    data[i]["x"] = RegionList[i].Region.col1;
                    data[i]["y"] = RegionList[i].Region.row1;
                    data[i]["w"] = RegionList[i].Region.col2 - RegionList[i].Region.col1;
                    data[i]["h"] = RegionList[i].Region.row2 - RegionList[i].Region.row1;
                    data[i]["annotation"] = RegionList[i].Annotation.Name;
                    
                }
                if (image != null)
                {
                    image.Tags = CurrentRegionList.Select(x => x.Annotation.Name).Distinct().ToList();
                }
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data));
            }
            
        }
        public void LoadAnnotation(ImageFilmstrip image)
        {
            ClearAnnotation();
            var path = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(image.FullPath) + ".txt");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    var datatxt = System.IO.File.ReadAllText(path);
                    var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            var annotation = item["annotation"].ToString();
                            double x = item["x"].ToObject<double>();
                            double y = item["y"].ToObject<double>();
                            double w = item["w"].ToObject<double>();
                            double h = item["h"].ToObject<double>();

                            var color = ClassList.FirstOrDefault(x1 => x1.Name == annotation);
                            if (color != null)
                            {
                                AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = color });
                            }
                            else
                            {
                                AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
                            }

                        }
                    }
                }catch(Exception ex)
                {

                }
               
                
            }
            
        }
        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            for (int i =0; i < 100; i++)
            {
                if (!ClassList.Any(x => x.Name == "unknown" + i.ToString()))
                {
                    ClassList.Add(new ClassifierClass1() { Color = "#00ff00ff", Name = "unknown" + i.ToString() });
                    break;
                }
            }
                       
        }

        ClassifierClass1 _selected_class;
        public ClassifierClass1 SelectedClass
        {
            get
            {
                return _selected_class;
            }
            set
            {
                if (_selected_class != value)
                {
                    _selected_class = value;
                    RaisePropertyChanged("SelectedClass");
                }
            }
        }

        HWindow display;
        HImage image_mask;
        HImage image;
        HHomMat2D transform;
        State current_state = State.Pan;
        HTuple w, h;

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageSet;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    LabelViewModel.ListImage.Remove(vm);

                    var imagegtpath = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(vm.FullPath));
                   
                    if (System.IO.File.Exists(imagegtpath + ".txt"))
                    {
                        System.IO.File.Delete(imagegtpath + ".txt");
                    }
                    //else if (System.IO.File.Exists(imagegtpath + ".jpg.txt"))
                    //{
                    //    System.IO.File.Delete(imagegtpath + ".jpg.txt");
                    //}
                    //else if (System.IO.File.Exists(imagegtpath + ".bmp.txt"))
                    //{
                    //    System.IO.File.Delete(imagegtpath + ".bmp.txt");
                    //}
                }
                catch (Exception ex)
                {

                }

            }

        }

        double _scale = 1;

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedClass != null)
            {
                AddRegionNew(new RegionMaker() { Annotation = SelectedClass, Region = new Rectange1(false) { row1 = mpy, col1 = mpx, row2 = mpy + 50, col2 = mpx + 50 } });
            }

        }

        private void window_display_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (SelectedMarker != null)
                {
                    SelectedMarker.Region.ClearDrawingData(display);
                    CurrentRegionList.Remove(SelectedMarker);
                    if (CurrentRegionList.Count > 0)
                    {
                        SelectedMarker = CurrentRegionList[CurrentRegionList.Count - 1];
                    }
                    else
                    {
                        SelectedMarker = null;
                        cmb_select_class.Margin = new Thickness(-100, -100, 0, 0);
                    }

                    DispOverlay();
                }
            }
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

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
        double mpx, mpy;
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            
            //window_display.Focus();
            if (e.Button == MouseButton.Right)
            {
                row = (int)e.Row;
                col = (int)e.Column;
            }
            else
            {
                cmb_select_class.Visibility = Visibility.Hidden;
            }
            Keyboard.Focus(window_display);
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectBoxPosition();
            }
        }

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }
        private void btn_open_folder(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", node.TrainConfig.ImageDir);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            train.Cancel();
        }



        private void window_display_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement fe = e.Source as FrameworkElement;
            fe.ContextMenu = BuildMenu();
        }
        ContextMenu BuildMenu()
        {
            ContextMenu theMenu = new ContextMenu();
            foreach(var item in ClassList)
            {
                MenuItem mia = new MenuItem();
                mia.Click += (s,e)=>
                {
                    AddRegionNew(new RegionMaker() { Annotation = item, Region = new Rectange1(false) { row1 = mpy, col1 = mpx, row2 = mpy + 50, col2 = mpx + 50 } });
                };
                mia.Header = "New Annotation "+item.Name;
                theMenu.Items.Add(mia);
            }
            
            return theMenu;
        }

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as ClassifierClass1;
            if (selected != null)
            {
                ClassList.Remove(selected);
            }
        }

        string _color_draw = "#00ff00aa";

        private void btn_train_cmd_Click(object sender, RoutedEventArgs e)
        {
            node.TrainConfig.ClassList = "[" + String.Join(",", ClassList.Select(x => string.Format("'{0}'", x.Name)).ToArray()) + "]";
            node.TrainConfig.Save();
            NOVisionPython.TrainConsole("classification", "v1", node.TrainConfig.ConfigDir);
        }

        private void btn_option_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
            wd.ShowDialog();
        }

        private void btn_save_class_Click(object sender, RoutedEventArgs e)
        {
            //node.SaveClassList(System.IO.Path.Combine(node.Dir, "classlist.txt"));
            node.SaveClassList(node.TrainConfig.classList_path);

        }

        private void window_display_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            cmb_select_class.Visibility = Visibility.Visible;
        }

        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {
            cmb_select_class.Visibility = Visibility.Visible;
        }

        private void window_display_HMouseDoubleClick(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PredictCurrentImage();

        }

        private void PredictCurrentImage()
        {
            try
            {
                if (node.Runtime.State == ModelState.Unloaded)
                {
                    node.Runtime.LoadRecipe(node.TrainConfig.ModelDir);
                }
                if (node.Runtime.State == ModelState.Loaded)
                {
                    foreach (var region in CurrentRegionList)
                    {


                        var result = node.Runtime.Infer(image.CropRectangle1(region.Region.row1, region.Region.col1, region.Region.row2, region.Region.col2));
                        display.DispText(new HTuple("Predicted:").TupleConcat(node.ClassList[result.Item1].Name + ": " + (result.Item2).ToString("P0")), "image", region.Region.row1, region.Region.col2 + 10, "black", new HTuple("box_color"), new HTuple(node.ClassList[result.Item1].Color));
                    }
                }
            }catch(Exception ex)
            {
                MessageBox.Show(this,  ex.Message, "Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            
            
        }
        bool _auto_predict;
        public bool AutoPredict
        {
            get
            {
                return _auto_predict;
            }
            set
            {
                if (_auto_predict != value)
                {
                    _auto_predict = value;
                    RaisePropertyChanged("AutoPredict");
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
        string _export_dataset_name="DefaultExportData";
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
        private void ExportDatasetFolder(string directory,string Format="png")
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach(var classes in ClassList)
            {
                var classesDirectory = System.IO.Path.Combine(directory, classes.Name);
                System.IO.Directory.CreateDirectory(classesDirectory);
                result.Add(classes.Name, classesDirectory);
            }
            foreach(var ImageFile in LabelViewModel.ListImage)
            {
                var annotation_path = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, ImageFile.FileName + ".txt");
                if (System.IO.File.Exists(annotation_path))
                {
                    var datatxt = System.IO.File.ReadAllText(annotation_path);
                    var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
                    var image = new HImage(ImageFile.FullPath);
                    var imageName = System.IO.Path.GetFileNameWithoutExtension(ImageFile.FullPath);
                    int count = 0;
                    foreach (var item in data)
                    {
                        var annotation = item["annotation"].ToString();
                        if (result.ContainsKey(annotation))
                        {
                            string partName = imageName + "_" + count.ToString();
                            double x = item["x"].ToObject<double>();
                            double y = item["y"].ToObject<double>();
                            double w = item["w"].ToObject<double>();
                            double h = item["h"].ToObject<double>();
                            image.CropRectangle1(y, x, y + h, x + w).WriteImage(Format, 0, System.IO.Path.Combine(result[annotation], partName));
                            count++;
                        }
                    }
                }                              
            }
            
        }
        private void btn_export_dataset_click(object sender, RoutedEventArgs e)
        {
            if (ExportDatasetDirectory!="")
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
                    catch(Exception ex)
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

        private void btn_open_exportdataset_menu_Click(object sender, RoutedEventArgs e)
        {
            box_export.Visibility = Visibility.Visible;
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

        private void cmb_select_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DispOverlay();
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
        EvaluationViewModel EvalViewModel = new EvaluationViewModel();
        private void btn_eval_Click(object sender, RoutedEventArgs e)
        {
            StartEvaluation();
        }
        public void RemoveDirectory(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
        public void StartEvaluation()
        {
            Task.Run(() =>
            {
                EvalViewModel.IsEvaluation = true;
                try
                {

                    if (node.Runtime.State == ModelState.Unloaded)
                    {
                        node.Runtime.LoadRecipe(node.TrainConfig.ModelDir);
                    }
                    if (!System.IO.Directory.Exists(node.TrainConfig.EvaluationDir))
                    {
                        Directory.CreateDirectory(node.TrainConfig.EvaluationDir);
                    }
                    string directory = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "images");
                    if (!System.IO.Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    //clear image directory
                    RemoveDirectory(directory);

                    string resultDir = System.IO.Path.Combine(node.TrainConfig.EvaluationDir, "result.txt");
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
                    var files = Directory.GetFiles(node.TrainConfig.ImageDir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                    int total = 0;

                    foreach (var ImageFile in files)
                    {
                        var annotation_path = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(ImageFile) + ".txt");
                        if (System.IO.File.Exists(annotation_path))
                        {
                            var datatxt = System.IO.File.ReadAllText(annotation_path);
                            var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
                            var image = new HImage(ImageFile);
                            var imageName = System.IO.Path.GetFileNameWithoutExtension(ImageFile);

                            foreach (var item in data)
                            {
                                var annotation = item["annotation"].ToString();
                                if (result.ContainsKey(annotation))
                                {
                                    classSummary[annotation]++;
                                    total++;
                                }
                            }
                        }
                    }
                    int step = 0;
                    foreach (var ImageFile in files)
                    {
                        var annotation_path = System.IO.Path.Combine(node.TrainConfig.AnnotationDir, System.IO.Path.GetFileName(ImageFile) + ".txt");
                        if (System.IO.File.Exists(annotation_path))
                        {
                            var datatxt = System.IO.File.ReadAllText(annotation_path);
                            var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
                            var image = new HImage(ImageFile);
                            var imageName = System.IO.Path.GetFileNameWithoutExtension(ImageFile);
                            int count = 0;
                            foreach (var item in data)
                            {
                                if (EvalViewModel._is_cancel)
                                {
                                    EvalViewModel._is_cancel = false;
                                    EvalViewModel.IsEvaluation = false;
                                    return;
                                }
                                var annotation = item["annotation"].ToString();
                                if (result.ContainsKey(annotation))
                                {
                                    string partName = imageName + "_" + count.ToString();
                                    double x = item["x"].ToObject<double>();
                                    double y = item["y"].ToObject<double>();
                                    double w = item["w"].ToObject<double>();
                                    double h = item["h"].ToObject<double>();
                                    var imagePart = image.CropRectangle1(y, x, y + h, x + w);
                                    //predict
                                    if (node.Runtime.State == ModelState.Loaded)
                                    {
                                        var predictionResult = node.Runtime.Infer(imagePart);
                                        classificationResult.Add(new ClassificationResult()
                                        {
                                            OriginalImageSource = ImageFile,
                                            Image = System.IO.Path.Combine(result[annotation], partName + ".png"),
                                            Label = annotation,
                                            Predict = ClassList[predictionResult.Item1].Name,
                                            Probability = predictionResult.Item2
                                        });
                                    }
                                    step++;
                                    EvalViewModel.EvaluationProgress = Math.Round(((double)step / total) * 100, 0);
                                    imagePart.WriteImage("png", 0, System.IO.Path.Combine(result[annotation], partName + ".png"));
                                    count++;
                                }
                            }
                        }
                    }
                    EvalViewModel.ListClassification = classificationResult;
                    UpdateEvaluation(EvalViewModel);
                    var classSummaryList = classSummary.Select(x => new ClassCount() { Name = x.Key, Value = x.Value }).ToList();
                    EvalViewModel.ClassSummaryList = classSummaryList;
                    System.IO.File.WriteAllText(resultDir, JsonConvert.SerializeObject(EvalViewModel, Formatting.Indented));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                EvalViewModel.IsEvaluation = false;
            });
            
           
        }

        private void UpdateEvaluation(EvaluationViewModel EvalViewModel)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                confusionMatrix.CreateLayout(EvalViewModel.ListClassification.Count, ClassList.Select(x => x.Name).ToList(), EvalViewModel.ListClassification);
                EvalViewModel.PrecisionList = confusionMatrix.PrecisionList;
                EvalViewModel.Acc = confusionMatrix.Acc;
                EvalViewModel.F1Score = confusionMatrix.F1;
            });
        }

        private void lst_evaluation_image_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //image_detail.Source =
            if (e.AddedItems.Count > 0)
            {
                
                image_detail.HalconWindow.AttachBackgroundToWindow(new HImage((e.AddedItems[0] as ClassificationResult).Image));
            }
            else
            {
                image_detail.HalconWindow.DetachBackgroundFromWindow();
            }
           
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("fill");
                display.SetColor(ColorDraw);
                display.SetFont("default-Normal-14");
                //display.getf
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private void DXTabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
        {
            if(e.NewSelectedItem == tab_eval)
            {
                if (!eval_loaded)
                {
                    LoadLastEvaluationResult();
                    eval_loaded = true;
                }
                
            }
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
        public List<string> SelectedDisplayTag { get; set; }
        ICollectionView view;
        private void lst_display_filter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                SelectedDisplayTag = new List<string>((e.NewValue as List<object>).Select(x => (x as ClassifierClass1).Name));
            }
            else
            {
                SelectedDisplayTag = new List<string>();
            }
            if(SelectedDisplayTag.Count == ClassList.Count)
            {
                SelectedDisplayTag = new List<string>();
            }
            view?.Refresh();
        }
        private bool CustomerFilter(object item)
        {
            if (SelectedDisplayTag == null)
            {
                return true;
            }
            if (SelectedDisplayTag.Count == 0)
            {
                return true;
            }
            var Items = (ImageFilmstrip)item;
            foreach(var tag in SelectedDisplayTag)
            {
                if (Items.Tags.Contains(tag))
                {
                    return true;
                }
                
            }
            return false;
            

        }

        private void btn_pre_Click_1(object sender, RoutedEventArgs e)
        {
            if (lst_view.SelectedIndex > 0)
            {
                lst_view.SelectedIndex--;
            }
        }

        private void btn_next_Click_1(object sender, RoutedEventArgs e)
        {
            if(lst_view.SelectedIndex >= 0)
            {
                lst_view.SelectedIndex++;
            }
            
        }

        private void btn_go_to_original_source_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = (lst_evaluation_image.SelectedItem as ClassificationResult);
                if (selected != null)
                {
                    LabelViewModel.SelectedDisplayFilter = new List<object>() { ClassList.FirstOrDefault(x => x.Name == selected.Label) };
                    lst_view.SelectedItem = LabelViewModel.ListImage.FirstOrDefault(x => x.FullPath == selected.OriginalImageSource);
                    tab_label.IsSelected = true;
                }
            }catch(Exception ex)
            {

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

        private void btn_save_annotation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAnnotation(LabelViewModel.SelectedImage, CurrentRegionList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                        string messageReceived = subSocket.ReceiveFrameString();
                        if (messageTopicReceived == "status")
                        {
                            JObject data = JObject.Parse(messageReceived);
                            if (data.ContainsKey("epoch"))
                            {
                                Epoch = data["epoch"].ToObject<double>();
                            }
                            if (data.ContainsKey("acc"))
                            {
                                Acc = data["acc"].ToObject<double>();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    AccData.Add(new GraphData() { X = Epoch, Y = Acc });
                                });
                            }
                            if (data.ContainsKey("loss"))
                            {
                                Loss = data["loss"].ToObject<double>();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    LossData.Add(new GraphData() { X = Epoch, Y = Loss });
                                });
                            }
                            if (data.ContainsKey("lr"))
                            {
                                LearningRate = data["lr"].ToObject<double>();
                            }
                            if (data.ContainsKey("timeleft"))
                            {
                                TimeLeft = TimeSpan.FromSeconds((int)(data["timeleft"].ToObject<int>() / 1000));
                            }
                            
                            TimeExlapsed = TimeSpan.FromSeconds((int)(DateTime.Now - start).TotalSeconds);
                        }
                        else if (messageTopicReceived == "log")
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Logs.Add(DateTime.Now.ToString() + " : " + messageReceived);
                            });
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
            public List<ClassificationResult> ListClassification { get; set; }
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
    public class ClassifierLabelViewModel:ReactiveUI.ReactiveObject
    {
        List<object> _selected_display_filter;
        public List<object> SelectedDisplayFilter
        {
            get { return _selected_display_filter; }
            set { this.RaiseAndSetIfChanged(ref _selected_display_filter, value); }
        }
        ImageSet _selected_image;
        public ImageSet SelectedImage
        {
            get { return _selected_image; }
            set { this.RaiseAndSetIfChanged(ref _selected_image, value); }
        }

        ObservableCollection<ImageSet> _list_image;
        public ObservableCollection<ImageSet> ListImage
        {
            get { return _list_image; }
            set { this.RaiseAndSetIfChanged(ref _list_image, value); }
        }
        private IReadOnlyCollection<ImageSet> _list_image_filter;
        public IReadOnlyCollection<ImageSet> ListImageFilter
        {
            get { return _list_image_filter; }
            set { this.RaiseAndSetIfChanged(ref _list_image_filter, value); }

        }
        public ClassifierLabelViewModel()
        {
            var filter = this.WhenAnyValue(x => x.SelectedDisplayFilter).Select(MakeFilter);
            //this.WhenAnyValue(x => x.ListImage).Subscribe(x =>
            //{
            //    if (x == null)
            //    {
            //        return;
            //    }
            //   x.Connect().ObserveOn(RxApp.TaskpoolScheduler).Filter(filter).ToCollection().BindTo(this, x2 => x2.ListImageFilter);
            //});
            
        }
        
        private Func<ImageSet, bool> MakeFilter(List<object> tags)
        {
            if (tags == null)
            {
                return image=>true;
                return image => (image.Tags.Count==0);
            }
            var tagstring=tags.Select(x => x as ClassifierClass1);
            return image =>
            {
                foreach (var tag in tagstring)
                {
                    if (image.Tags.Contains(tag.Name))
                    {
                        return true;
                    }
                }
                return false;
            };
                
            
        }
    }
    public class StringToColor : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Color?) | targetType == typeof(Color))
            {
                Color temp = (Color)ColorConverter.ConvertFromString((string)value);
                string convert = string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", temp.B, temp.A, temp.R, temp.G);
                return (Color)ColorConverter.ConvertFromString(convert);
            }
            throw new InvalidOperationException("The target must be a color");


        }


        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a color");
            Color e = (Color)value;
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", e.R, e.G, e.B, e.A);
        }

        #endregion
    }
    public delegate void RegionMaker2Callback(RegionMaker2 sender, Region region);
    public delegate void EllipseMarkerCallback(EllipseMarker sender, Region region);
    public delegate void CircleMarkerCallback(CircleMarker2 sender, Region region);
    public delegate void RegionMakerCallback(RegionMaker sender, Region region);
    public delegate void WordMarkerCallback(WordMarker sender, Region region);
    public delegate void BaseRegionMakerCallback(BaseRegionMaker sender, Region region);
    public class RegionMaker2
    {
        public int GroupId { get; set; } = -1;
        bool _is_selected = false;
        public bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public Rectange2 Region { get; set; }
        public ClassifierClass1 Annotation { get; set; }

        public void Attach(HWindow display, HHomMat2D transform, RegionMaker2Callback OnUpdate, RegionMaker2Callback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
    }
    public class EllipseMarker
    {
        public int GroupId { get; set; } = -1;
        bool _is_selected = false;
        public bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public NOVisionDesigner.Designer.Nodes.Ellipse Region { get; set; }
        public ClassifierClass1 Annotation { get; set; }

        public void Attach(HWindow display, HHomMat2D transform, EllipseMarkerCallback OnUpdate, EllipseMarkerCallback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
    }
    public class CircleMarker2
    {
        public int GroupId { get; set; } = -1;
        bool _is_selected = false;
        public bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public NOVisionDesigner.Designer.Nodes.Circle Region { get; set; }
        public ClassifierClass1 Annotation { get; set; }

        public void Attach(HWindow display, HHomMat2D transform, CircleMarkerCallback OnUpdate, CircleMarkerCallback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
    }
    public class WordMarker
    {
        bool _is_selected = false;
        public bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public Rectange2 Region { get; set; }
        public string Word { get; set; }

        public void Attach(HWindow display, HHomMat2D transform, WordMarkerCallback OnUpdate, WordMarkerCallback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
    }
    public class RegionMaker
    {
        public int GroupId { get; set; } = -1;
        bool _is_selected = false;
        public bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public Rectange1 Region { get; set; }
        public ClassifierClass1 Annotation { get; set; }

        public void Attach(HWindow display, HHomMat2D transform, RegionMakerCallback OnUpdate, RegionMakerCallback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
    }
    public class Rectangle1Maker:BaseRegionMaker
    {
        public Rectangle1Maker()
        {
            Type = RegionType.Rectangle1;
        }
        public override void Display(HWindow display)
        {
            display.DispObj(Region.region);
            display.DispText(Annotation.Name, "image", (Region as Rectange1).row1, (Region as Rectange1).col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
        }
        public override JObject SerializeData()
        {
            var data = new JObject();
            data["row1"] = (Region as Rectange1).row1;
            data["col1"] = (Region as Rectange1).col1;
            data["row2"] = (Region as Rectange1).row2;
            data["col2"] = (Region as Rectange1).col2;
            return data;
        }
        public override void ConvertCoordinatesImageToWindow(HWindow display, out double winposx, out double winposy)
        {
            display.ConvertCoordinatesImageToWindow((Region as Rectange1).row1, (Region as Rectange1).col1, out winposx, out winposy);
        }
        public override BaseRegionMaker Copy()
        {
            var region = new Rectange1(false) { col1 = (Region as Rectange1).col1, row1 = (Region as Rectange1).row1, col2 = (Region as Rectange1).col2, row2 = (Region as Rectange1).row2 };
            return new Rectangle1Maker() { Annotation = Annotation, Region = region };
        }
    }
    public class Rectangle2Maker : BaseRegionMaker
    {

        public Rectangle2Maker()
        {
            Type = RegionType.Rectangle2;


        }
        public override void Display(HWindow display)
        {
            display.DispObj(Region.region);
            display.DispText(Annotation.Name, "image", (Region as Rectange2).row, (Region as Rectange2).col, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
        }
        public override JObject SerializeData()
        {
            var data = new JObject();
            data["row"] = (Region as Rectange2).row;
            data["col"] = (Region as Rectange2).col;
            data["phi"] = (Region as Rectange2).phi;
            data["lenght1"] = (Region as Rectange2).length1;
            data["lenght2"] = (Region as Rectange2).length2;
            return data;
        }
        public override void ConvertCoordinatesImageToWindow(HWindow display, out double winposx, out double winposy)
        {
            display.ConvertCoordinatesImageToWindow((Region as Rectange2).row, (Region as Rectange2).col, out winposx, out winposy);
        }
        public override BaseRegionMaker Copy()
        {
            var region = new Rectange2(false)
            {
                col = (Region as Rectange2).col,
                row = (Region as Rectange2).row,
                phi = (Region as Rectange2).phi,
                length1 = (Region as Rectange2).length1,
                length2 = (Region as Rectange2).length2
            };
            return new Rectangle2Maker() { Annotation = Annotation, Region = region };
        }
    }
    public class CircleMaker : BaseRegionMaker
    {
        public CircleMaker()
        {
            Type = RegionType.Circle;
        }
        public override void Display(HWindow display)
        {
            display.DispObj(Region.region);
            display.DispText(Annotation.Name, "image", (Region as Circle).row - (Region as Circle).radius, (Region as Circle).col, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
        }
        public override JObject SerializeData()
        {
            var data = new JObject();
            data["row"] = (Region as Circle).row;
            data["col"] = (Region as Circle).col;
            data["radius"] = (Region as Circle).radius;
            return data;
        }
        public override void ConvertCoordinatesImageToWindow(HWindow display, out double winposx, out double winposy)
        {
            display.ConvertCoordinatesImageToWindow((Region as Circle).row - (Region as Circle).radius, (Region as Circle).col, out winposx, out winposy);
        }
        public override BaseRegionMaker Copy()
        {
            var region = new Circle(false) { col = (Region as Circle).col, row = (Region as Circle).row, radius = (Region as Circle).radius };
            return new CircleMaker() { Annotation = Annotation, Region = region };
        }
    }
    public class BaseRegionMaker
    {
        public virtual int GroupId { get; set; } = -1;
        bool _is_selected = false;
        public virtual bool IsSelected
        {
            get { return _is_selected; }
            set
            {
                if (_is_selected != value)
                {
                    _is_selected = value;
                    if (Region.current_draw != null)
                    {
                        if (value)
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "blue");
                        }
                        else
                        {
                            Region.current_draw.SetDrawingObjectParams("color", "red");
                        }

                    }
                }
            }
        }
        public RegionType Type { get; set; }
        public virtual Region Region { get; set; }
        public virtual ClassifierClass1 Annotation { get; set; }
        public virtual void Attach(HWindow display, HHomMat2D transform, BaseRegionMakerCallback OnUpdate, BaseRegionMakerCallback OnSelected)
        {

            Region.OnUpdated = (e) => { OnUpdate(this, e); };
            Region.OnSelected = (e) => { OnSelected(this, e); };
            HDrawingObject draw = Region.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(Region.OnResize);
                draw.OnDrag(Region.OnResize);
                draw.OnSelect(Region.OnSelect);
            }
            display.AttachDrawingObjectToWindow(Region.current_draw);

        }
        public virtual void Display(HWindow display)
        {
            throw new NotImplementedException();
        }
        public virtual JObject SerializeData()
        {
            throw new NotImplementedException();
        }
        public virtual void ConvertCoordinatesImageToWindow(HWindow display, out double winposx, out double winposy)
        {
            throw new NotImplementedException();
        }
        public virtual BaseRegionMaker Copy()
        {
            throw new NotImplementedException();
        }
    }
    public class ClassifierClass1:IHalconDeserializable
    {
        public bool NG { get; set; } = false;
        public string Color { get; set; } = "#00ff00ff";
        public string Name { get; set; }
        public int ConfidentThreshold { get; set; } = 50;

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }

    public class ClassResult
    {
        public bool NG;
        public ClassifierClass1 TargetClass;
        public HRegion regions;
        public string Color;
        public double Confidence;
        public ClassResult(ClassifierClass1 targetClass, HRegion regions, string Color, bool NG,double Confidence)
        {
            this.TargetClass = targetClass;
            this.regions = regions;
            this.Color = Color;
            this.NG = NG;
            this.Confidence = Confidence;
        }
    }

    

}
