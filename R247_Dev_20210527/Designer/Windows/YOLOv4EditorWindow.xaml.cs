using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using NumSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

namespace NOVisionDesigner.Designer.Windows
{ 
    public partial class YOLOv4EditorWindow : ThemedWindow,INotifyPropertyChanged
    {
        bool first_init = true;
        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

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
        private int _SizeWidth = 100;
        public int SizeWidth
        {
            get { return _SizeWidth; }
            set
            {
                if (_SizeWidth != value)
                {
                    _SizeWidth = value;
                    RaisePropertyChanged("SizeWidth");

                }
            }
        }
        private int _SizeHeight = 100;
        public int SizeHeight
        {
            get { return _SizeHeight; }
            set
            {
                if (_SizeHeight != value)
                {
                    _SizeHeight = value;
                    RaisePropertyChanged("SizeHeight");
                }
            }
        }

        private int _maxOutputSize = 100;
        public int MaxOutputSize
        {
            get { return _maxOutputSize; }
            set
            {
                if (_maxOutputSize != value)
                {
                    _maxOutputSize = value;
                    RaisePropertyChanged("MaxOutputSize");
                    
                }
            }
        }
        private int _IOU = 100;
        public int IOU
        {
            get { return _IOU; }
            set
            {
                if (_IOU != value)
                {
                    _IOU = value;
                    RaisePropertyChanged("IOU");

                }
            }
        }
        private int _Score = 100;
        public int Score
        {
            get { return _Score; }
            set
            {
                if (_Score != value)
                {
                    _Score = value;
                    RaisePropertyChanged("Score");

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

        private double _colorOpacity = 30;
        public double ColorOpacity
        {
            get { return _colorOpacity; }
            set
            {
                if (_colorOpacity != value)
                {
                    _colorOpacity = value;
                    RaisePropertyChanged("ColorOpacity");
                    DispOverlay();
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

        private string _imgNameWithExtension;
        public string ImageNameWithExtension
        {
            get { return _imgNameWithExtension; }
            set
            {
                if (_imgNameWithExtension != value)
                {
                    _imgNameWithExtension = value;
                    RaisePropertyChanged("ImageNameWithExtension");
                }
            }
        }

        private string _colorGood = "#00ff0011";
        public string ColorGood
        {
            get { return _colorGood; }
            set
            {
                if (_colorGood != value)
                {
                    _colorGood = value;
                    RaisePropertyChanged("ColorGood");
                }
            }
        }

        private string _colorBad = "#ff000055";
        public string ColorBad
        {
            get { return _colorBad; }
            set
            {
                if (_colorBad != value)
                {
                    _colorBad = value;
                    RaisePropertyChanged("ColorBad");
                }
            }
        }

        private double _scale;
        public double Scale
        {
            get { return _scale; }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    RaisePropertyChanged("Scale");
                }
            }
        }

        private double _smooth;
        public double Smooth
        {
            get { return _smooth; }
            set
            {
                if (_smooth != value)
                {
                    _smooth = value;
                    RaisePropertyChanged("Smooth");
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

        private RegionMaker _selectedMarker;
        public RegionMaker SelectedMarker
        {
            get
            {
                return _selectedMarker;
            }
            set
            {
                if (_selectedMarker != value)
                {
                    _selectedMarker = value;
                    RaisePropertyChanged("SelectedMarker");
                }
            }
        }

        public ClassifierClass1 _selectedClass;
        public ClassifierClass1 SelectedClass
        {
            get
            {
                return _selectedClass;
            }
            set
            {
                if (_selectedClass != value)
                {
                    _selectedClass = value;
                    RaisePropertyChanged("SelectedClass");
                }
            }
        }

        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public ObservableCollection<YOLOClass> ClassList { get; set; }
        public List<RegionMaker> _CurrentRegionList { get; set; } = new List<RegionMaker>();
        public List<RegionMaker> CurrentRegionList
        {
            get
            {
                return _CurrentRegionList;
            }
            set
            {
                if (_CurrentRegionList != value)
                {
                    _CurrentRegionList = value;
                    CountRegionList = CurrentRegionList.Count();
                    RaisePropertyChanged("CountRegionList");
                }
            }
        }


        public int _CountRegionList = 0;
        public int CountRegionList
        {
            get
            {
                return _CountRegionList;
            }
            set
            {
                if (_CountRegionList != value)
                {
                    _CountRegionList = value;
                    RaisePropertyChanged("CountRegionList");
                }
            }
        }


        public class LossValue
        {
            public double step { get; set; }
            public double loss { get; set; }

        }

        private Dictionary<string, string> annotationDict = new Dictionary<string, string>();
        private Dictionary<string, int> classNameDict = new Dictionary<string, int>();

        private string ImageDir, AnnotationDir,  annotationPath;
        private int row, col;
        private int imgW, imgH;
        private double mpx, mpy;
        private HWindow display;
        private HImage image;
        YOLOv4 node;
        Python.TrainYOLOv4 train;
        #endregion
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
                train = new Python.TrainYOLOv4();

                string jsonListAugmentation = JsonConvert.SerializeObject(this.listAugmentation);
                node.TrainConfig.Save();
                trainningStatusViewModel.StartListen();
                train.TrainPython(node.TrainConfig.ConfigDir, node.TrainConfig.ModelName, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTraining = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this,"Training was cancel because of error!", "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });
                        
                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTraining = false;
                        node.RuntimeEngine.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this,"Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
            }));
        }
        public YOLOv4EditorWindow(YOLOv4 node)
        {
            this.node = node;
            InitializeComponent();
            IOU = node.IOUThreshold.Value;
            Score = node.ConfidenceThreshold.Value;
            MaxOutputSize= node.MaxOutputSize.Value;
            propertiesGrid.SelectedObject = node.RuntimeEngine;
            this.DataContext = this;
            this.stack_train_parameter.DataContext = node.TrainConfig;
            this.ClassList = node.TrainConfig.Classes;
            if (listAugmentation.Count > 0)
            {
                foreach (var item in listAugmentation)
                {
                    this.listAugmentation.Add(item);
                    list_augmentation.Items.Add(item);
                }
            }
            cmb_select_class.ItemsSource = this.ClassList;
            lst_class.ItemsSource = this.ClassList;
            this.ImageDir = node.TrainConfig.ImageDir;
            this.AnnotationDir = node.TrainConfig.AnnotationDir;

            btn_add_image.Click += Btn_add_image_Click;
            btn_add_image_camera.Click += (s, e) =>
            {
                if (node != null)
                {

                    var imageFeed = node.Image.GetCurrentConnectionValue();
                    var region = Functions.GetNoneEmptyRegion(node.Region.Value);
                    
                    if (imageFeed != null)

                        try
                        {
                            var image = imageFeed.Clone();
                            var filename = RandomName();
                            var newfile = System.IO.Path.Combine(ImageDir, filename);
                            var imageCropped = Functions.CropImageWithRegion(image, region,node.FillBackground);
                            imageCropped.WriteImage("bmp", 0, newfile);
                            var imageadded = new ImageSet(newfile + ".bmp") { DateTime=DateTime.Now};
                            ListImage.Insert(0,imageadded);
                            lst_view.SelectedItem = imageadded;
                            lst_view.ScrollIntoView(imageadded);
                        }
                        catch (Exception ex)
                        {

                        }
                }
            };
            btn_train.Click += (sender, e) =>
            {
                StartTrainning();
            };
            btn_stop_train.Click += (sender, e) =>
            {
                train?.Cancel();
            };
            LoadImageList(ImageDir);
            
            this.DataContext = this;
            if (lst_view.Items.Count > 0)
            {
                lst_view.SelectedItem = ListImage[0];
            }
            copyAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                copied = new RegionMaker() { Annotation = SelectedMarker.Annotation, Region = SelectedMarker.Region };

            });
            pasteAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                if (copied != null)
                {
                    AddRegion(new RegionMaker() { Annotation = copied.Annotation, Region = new Rectange1(false) { row1 = copied.Region.row1 - 20, col1 = copied.Region.col1 + 20, row2 = copied.Region.row2 - 20, col2 = copied.Region.col2 + 20 } });
                }

            });
            var copyBinding = new KeyBinding(copyAnnotation, Key.C, ModifierKeys.Control);
            var pasteBinding = new KeyBinding(pasteAnnotation, Key.V, ModifierKeys.Control);
            this.InputBindings.Add(copyBinding);
            this.InputBindings.Add(pasteBinding);

            this.Closed += (o, e) =>
            {
                trainningStatusViewModel?.Dispose();
            };
        }
        RegionMaker copied;
        public ICommand copyAnnotation { get; set; }
        public ICommand pasteAnnotation { get; set; }
        
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        void LoadImageList(string dir)
        {
            var result = new List<ImageSet>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new ImageSet(file) { DateTime = File.GetCreationTime(file) });
                }

            }
            var data = result.OrderByDescending(x => x.DateTime);
            ListImage = new ObservableCollection<ImageSet>(data);
            
        }

        public void UpdateSelectBoxPosition()
        {
            if (SelectedMarker != null)
            {
                double winposx, winposy;
                display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row1, SelectedMarker.Region.col1, out winposx, out winposy);
                cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
                SizeHeight = (int)(SelectedMarker.Region.Row2 - SelectedMarker.Region.Row1);
                SizeWidth = (int)(SelectedMarker.Region.Col2- SelectedMarker.Region.Col1);

            }
        }

        public void Update(RegionMaker sender, Region region)
        {
            ChangeRegion();
            DispOverlay();
            SaveResult();
        }
        public void Selected(RegionMaker sender, Region region)
        {
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
                    display.DispObj(item.Region.region);
                    display.DispText(item.Annotation.Name, "image", item.Region.row1, item.Region.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
                }
               
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            DispOverlay();
        }

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

            ChangeRegion();
        }

        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }

        public void SaveResult()
        {
            SaveAnnotation(annotationPath);
        }


        public void SaveAnnotation(string path)
        {
            if (ImageNameWithExtension == null)
            {
                return;
            }
            JObject[] data = new JObject[CurrentRegionList.Count];
            string dataTrain = "";
            string annotationTrainPath = System.IO.Path.Combine(AnnotationDir, "annotations.txt");
            string classNameTrainPath = System.IO.Path.Combine(AnnotationDir, "className.names");
            string imgTrainPath = System.IO.Path.Combine(ImageDir, ImageNameWithExtension);
            dataTrain += imgTrainPath + ",";
            for (int i = 0; i < data.Length; i++)
            {
                string className = "";
                data[i] = new JObject();
                data[i]["x"] = CurrentRegionList[i].Region.col1;
                data[i]["y"] = CurrentRegionList[i].Region.row1;
                data[i]["w"] = CurrentRegionList[i].Region.col2 - CurrentRegionList[i].Region.col1;
                data[i]["h"] = CurrentRegionList[i].Region.row2 - CurrentRegionList[i].Region.row1;
                className = CurrentRegionList[i].Annotation.Name;
                data[i]["annotation"] = className;
                if (!classNameDict.ContainsKey(className))
                {
                    int count = classNameDict.Count;
                    classNameDict.Add(className, count);

                }

                dataTrain += data[i]["x"].ToString() + "," + data[i]["y"].ToString() + "," + CurrentRegionList[i].Region.col2.ToString() + ",";
                if (i == data.Length - 1)
                {
                    dataTrain += CurrentRegionList[i].Region.row2.ToString() + "," + classNameDict[className].ToString();
                }
                else
                {
                    dataTrain += CurrentRegionList[i].Region.row2.ToString() + "," + classNameDict[className].ToString() + " ";
                }

            }
            if (path != null)
            {
                annotationDict[ImageName] = dataTrain;

                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data));

                string[] dataForAllImg = new string[annotationDict.Keys.Count];
                int i = 0;
                foreach (var imgName in annotationDict.Keys)
                {
                    dataForAllImg[i] += annotationDict[imgName];
                    i++;
                }

                if (dataForAllImg.Count() < 30)
                {
                    int count = dataForAllImg.Count();
                    int k = (int)Math.Ceiling((double)30 / count);
                    string[] upSampleData = new string[k * count];
                    for (int j = 0; j < k; j++)
                    {
                        Array.Copy(dataForAllImg, 0, upSampleData, j * count, count);
                    }
                    dataForAllImg = upSampleData;
                }

                string[] classNames = new string[classNameDict.Keys.Count];
                i = 0;
                foreach (var className in classNameDict.Keys)
                {
                    classNames[i] = className;
                    i++;
                }
                System.IO.File.WriteAllLines(annotationTrainPath, dataForAllImg);
                System.IO.File.WriteAllLines(classNameTrainPath, classNames);
            }

        }
        public void LoadAnnotation(string path)
        {
            CurrentRegionList.Clear();
            var datatxt = System.IO.File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
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
                    AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w}, Annotation = color });
                }
                else
                {
                    AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
                }
                

            }
            CountRegionList = CurrentRegionList.Count();
        }
        
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                
                cmb_select_class.Visibility = Visibility.Hidden;
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

        private void SetSelectedImage(ImageFilmstrip selected)
        {
            if (selected != null)
            {
                try
                {
                    SaveResult();
                }

                catch (Exception ex)
                {

                }

                try
                {
                    image = new HImage(selected.FullPath);
                    image.GetImageSize(out imgW, out imgH);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                    window_display.HalconWindow.ClearWindow();
                    if (first_init)
                    {
                        first_init = false;
                        window_display.SetFullImagePart();
                    }
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
                try
                {
                    ClearAnnotation();
                    ImageName = System.IO.Path.GetFileNameWithoutExtension(selected.FullPath);
                    ImageNameWithExtension = System.IO.Path.GetFileName(selected.FullPath);
                    annotationPath = System.IO.Path.Combine(AnnotationDir, System.IO.Path.GetFileName(selected.FullPath) + ".txt");
                    if (System.IO.File.Exists(annotationPath))
                    {
                        try
                        {
                            LoadAnnotation(annotationPath);
                        }
                        catch (Exception ex)
                        {

                        }

                    }
                    else
                    {

                    }

                }
                catch (Exception ex)
                {

                }
                if (CurrentRegionList.Count > 0)
                {
                    UpdateSelectBoxPosition();
                    cmb_select_class.Visibility = Visibility.Visible;
                }

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

        

        
        private void Btn_add_image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.bmp;*.jpg;*.gif;*.png;*.jpeg|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                Task.Run(new Action(() =>
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            var extension = System.IO.Path.GetExtension(file);
                            string newfile = RandomName();
                            newfile = newfile + extension;
                            System.IO.File.Copy(file, newfile);
                            var imageadded = new ImageSet(newfile) { DateTime = DateTime.Now};
                            
                            
                           
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ListImage.Insert(0, imageadded);
                                    if (file == files[files.Length - 1])
                                    {
                                    lst_view.SelectedItem = imageadded;
                                    lst_view.ScrollIntoView(imageadded);

                                    }
                                });
                           
                        }
                        catch (Exception ex)
                        {

                        }
                    };

                }));
            }
        }

        private string RandomName()
        {
            var filename = Functions.RandomString(8);
            var newfile = System.IO.Path.Combine(ImageDir, filename);
            while (true)
            {
                if (System.IO.File.Exists(newfile))
                {
                    filename = Functions.RandomString(16);
                    newfile = System.IO.Path.Combine(ImageDir, filename);
                }
                else
                {
                    break;
                }
            }

            return newfile;
        }
        
        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            ClassList.Add(new YOLOClass() { Color = Functions.RandomHalconColor(), Name = "unknown" });
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

                    var imagegtpath = System.IO.Path.Combine(AnnotationDir, System.IO.Path.GetFileNameWithoutExtension(vm.FullPath));

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

                    //Add by Minh
                    if (ListImage.Count == 0)
                    {
                        display?.ClearWindow();
                        ClearAnnotation();
                        display?.DetachBackgroundFromWindow();
                        annotationPath = null;
                    }
                }
                catch (Exception ex)
                {

                }

            }

        }





        private void Btn_Augmentation_Remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as Augmentation;
            if (vm != null)
            {
                var indexRemove = list_augmentation.Items.IndexOf(vm);
                listAugmentation.RemoveAt(indexRemove);
                //list_augmentation.ClearValue(ItemsControl.ItemsSourceProperty);
                list_augmentation.Items.RemoveAt(indexRemove);
                LoadImageList(ImageDir);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as YOLOClass;
            if (selected != null)
            {
                ClassList.Remove(selected);
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
            if (SelectedClass == null)
            {
                if (lst_class.Items.Count > 0)
                {
                    lst_class.SelectedIndex = 0;
                }
            }
            if (SelectedClass != null)
            {
                AddRegionNew(new RegionMaker() { Annotation = SelectedClass, Region = new Rectange1(false) { row1 = mpy - (int)(SizeHeight/2), col1 = mpx - (int)(SizeWidth/2), row2 = mpy + (int)(SizeHeight / 2), col2 = mpx + (SizeWidth / 2) } });
            }
            CountRegionList = CurrentRegionList.Count();

        }

        private void Btn_add_augmentation_Click(object sender, RoutedEventArgs e)
        {
            AugmentationEditorWindow augmentationWd = new AugmentationEditorWindow(ImageDir, AnnotationDir, ClassList);
            augmentationWd.ShowDialog();
            LoadImageList(ImageDir);

            var tempList = augmentationWd.listAugmentationcs;
            foreach (var item in tempList)
            {
                list_augmentation.Items.Add(item);
                listAugmentation.Add(item);
            }

        }
        private void btn_monitor_onClick(object sender, RoutedEventArgs e)
        {
           
            
        }

        private void btn_acept_view_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(node.TrainConfig.SavedModelDir, "model.onnx")))
                {
                    if (image == null)
                    {
                        return;
                    }
                    if (node.RuntimeEngine.State == ModelState.NotFound)
                    {
                        return;
                    }
                    if (SelectedClass == null || image == null)
                    {
                        return;
                    }
                    CurrentRegionList.Clear();
                    if (node.RuntimeEngine.State == ModelState.Unloaded)
                    {
                        node.RuntimeEngine.LoadRecipe(node.TrainConfig.SavedModelDir);
                    }
                    if (node.RuntimeEngine.State == ModelState.Loaded)
                    {
                        var result = node.RuntimeEngine.PredictV2(image, node.TrainConfig.Classes.Count, MaxOutputSize, IOU, Score);
                        int[] shape = result.shape;
                        if (shape is null)
                            return;
                        if (shape[0] == 0)
                            return;
                        List<Rect1> rectList = new List<Rect1>();
                        for (int i = 0; i < shape[0]; i++)
                        {
                            NDArray xyxyScoreClass = result[string.Format("{0}, 0:6", i)];
                            float x0 = xyxyScoreClass[0];
                            float y0 = xyxyScoreClass[1];
                            float x1 = xyxyScoreClass[2];
                            float y1 = xyxyScoreClass[3];
                            float confidence = xyxyScoreClass[4];
                            float classes = xyxyScoreClass[5];
                            int cls = (int)classes;
                            HRegion region = new HRegion();
                            region.GenRectangle1((HTuple)y0, (HTuple)x0, (HTuple)y1, (HTuple)x1);
                            AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y0, col1 = x0 , row2 = y1, col2 = x1}, Annotation = SelectedClass});
                            DispOverlay();
                        }
                        SaveResult();
                        CountRegionList = CurrentRegionList.Count();
                    }

                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.ToString());
            }
        }

        #region event for keys
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
                    }
                    UpdateSelectBoxPosition();
                    DispOverlay();
                    CountRegionList = CurrentRegionList.Count();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            train?.Cancel();
        }

        private void window_display_HMouseDoubleClick(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }

        private void window_display_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSelectBoxPosition();
        }

        private void cmb_select_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DispOverlay();
        }

        private void window_display_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyUp(Key.LeftCtrl))
            {
                if (display == null)
                    return;
                DispOverlay();              
            }
        }

        private void window_display_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void btn_train_cmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                node.TrainConfig.Save();
                NOVisionPython.TrainConsole("detection", node.TrainConfig.ModelName, node.TrainConfig.ConfigDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_option_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
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
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && SelectedClass != null)
            {
                var region = new RegionMaker()
                {
                    Annotation = SelectedClass,
                    Region = new Rectange1(false)
                    {
                        row1 = e.Row - (SizeHeight / 2),
                        col1 = e.Column - (SizeWidth / 2),
                        row2 = e.Row + (SizeHeight / 2),
                        col2 = e.Column + (SizeWidth / 2)                      
                    }
                };
                RegionMaker region_add = region;
                CurrentRegionList.Add(region_add);

                region_add.Attach(display, null, Update, Selected);

                if (CurrentRegionList.Count == 1)
                {
                    SelectedMarker = CurrentRegionList[0];
                }

                ChangeRegion();
            }
            Keyboard.Focus(window_display);
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
            catch (Exception ex)
            {
                DXMessageBox.Show(this, "Save", "Error saving annotation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_new_annotation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedClass == null)
            {
                if (lst_class.Items.Count > 0)
                {
                    lst_class.SelectedIndex = 0;
                }
            }
            if (SelectedClass != null)
            {
                var imagePart = window_display.HImagePart;
                var cx = imagePart.X + imagePart.Width / 2;
                var cy = imagePart.Y + imagePart.Height / 2;
                row = (int)cy;
                col = (int)cx;
                AddRegionNew(new RegionMaker() { Annotation = SelectedClass, Region = new Rectange1(false) { row1 = cy - (SizeHeight / 2), col1 = cx - (SizeWidth / 2), row2 = cy + (SizeHeight / 2), col2 = cx + (SizeWidth / 2) } });
                CountRegionList = CurrentRegionList.Count();
            }
        }

        private void btn_edit_paint_3d_Click(object sender, RoutedEventArgs e)
        {
            //Open the Paint 3D window with the image loaded
            if (lst_view.SelectedItem != null)
            {
                var selected = lst_view.SelectedItem as ImageFilmstrip;
                if (selected != null)
                {
                    ProcessStartInfo Info = new ProcessStartInfo()
                    {
                        FileName = "ms-paint:",
                        WindowStyle = ProcessWindowStyle.Maximized,
                        Arguments = selected.FullPath+ " //ForceBootstrapPaint3D"
                    };
                    Process.Start(Info);
                    //Process.Start("ms-paint:", selected.FullPath);

                    //// Wait for the Paint 3D window to close
                    //Process[] paintProcesses = Process.GetProcessesByName("mspaint");
                    //if (paintProcesses.Length > 0)
                    //{
                    //    paintProcesses[0].WaitForExit();
                    //}
                }
            }
            
        }

        private void btn_add_image_file_crop_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.bmp;*.jpg;*.gif;*.png;*.jpeg|All files|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames;
                Task.Run(new Action(() =>
                {
                    var region = Functions.GetNoneEmptyRegion(node.Region.Value);

                    foreach (var file in files)
                    {
                        try
                        {
                            var extension = System.IO.Path.GetExtension(file);
                            string newfile = RandomName();
                            var newfileWithExtension = newfile + extension;
                            if(region == null)
                            {
                                System.IO.File.Copy(file, newfileWithExtension);
                            }
                            else
                            {
                                HImage image = new HImage(file);
                                var imageCropped = Functions.CropImageWithRegion(image, region,node.FillBackground);
                                newfileWithExtension = newfile + ".png";
                                imageCropped.WriteImage("png",0, newfile);
                            }
                            
                            var imageadded = new ImageSet(newfileWithExtension) { DateTime = DateTime.Now };



                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ListImage.Insert(0, imageadded);
                                if (file == files[files.Length - 1])
                                {
                                    lst_view.SelectedItem = imageadded;
                                    lst_view.ScrollIntoView(imageadded);

                                }
                            });

                        }
                        catch (Exception ex)
                        {

                        }
                    };

                }));
            }
        }
        
        private void btn_edit_paint_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as ImageFilmstrip;
            if (selected != null)
            {
                if (image != null)
                {
                    PaintImageWindow wd = new PaintImageWindow(image);
                    wd.Owner = this;
                    if (wd.ShowDialog() == true)
                    {
                        try
                        {
                            image = wd.Image;
                            window_display.HalconWindow.AttachBackgroundToWindow(image);
                            var extension = System.IO.Path.GetExtension(selected.FullPath);
                            image.WriteImage(extension.Substring(1), 0, System.IO.Path.ChangeExtension(selected.FullPath, null));
                        }catch(Exception ex)
                        {
                            MessageBox.Show(this, "Error", ex.Message);
                        }
                        
                        
                    }
                }
            }
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectBoxPosition();
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (display == null)
                    return;
                DispOverlay();
                display.SetDraw("margin"); 
                display.SetColor("blue");
                display.SetLineStyle(new HTuple(5,10));
                //display.DispRectangle1(e.Row-100,e.Column-100,e.Row+100,e.Column+100);
                display.DispRectangle1(e.Row - (SizeHeight / 2),
                        e.Column - (SizeWidth / 2),
                        e.Row + (SizeHeight / 2),
                        e.Column + (SizeWidth / 2));
            }
        }
        

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }
        #endregion

        #region event for checkbox
        #endregion
        

        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        public class GraphData
        {
            public double X { get; set; }
            public double Y { get; set; }
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
                    subSocket.Connect("tcp://localhost:6789");
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
                                Logs.Add(DateTime.Now.ToString() + " : " + messageReceived);
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
    }
}
