using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.ViewModel;
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

namespace NOVisionDesigner.Designer.Windows
{
    public partial class RYOLOEditorWindow : Window, INotifyPropertyChanged
    {

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _is_visualize;

        public bool IsVisualize
        {
            get { return _is_visualize; }
            set
            {
                if (_is_visualize != value)
                {
                    _is_visualize = value;
                    RaisePropertyChanged("IsVisualize");
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

        private double _colorOpacity = 50;
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

        private ObservableCollection<ImageFilmstrip> _listImage;

        public ObservableCollection<ImageFilmstrip> ListImage
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

        private BaseRegionMaker _selectedMarker;
        public BaseRegionMaker SelectedMarker
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
        private List<BaseRegionMaker> _selectedMarkers = new List<BaseRegionMaker>();
        public List<BaseRegionMaker> SelectedMarkers
        {
            get
            {
                return _selectedMarkers;
            }
            set
            {
                if (_selectedMarkers != value)
                {
                    _selectedMarkers = value;
                    RaisePropertyChanged("SelectedMarkers");
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
        public List<BaseRegionMaker> CurrentRegionList { get; set; } = new List<BaseRegionMaker>();

        public class LossValue
        {
            public double step { get; set; }
            public double loss { get; set; }

        }

        private Dictionary<string, string> annotationDict = new Dictionary<string, string>();
        private Dictionary<string, int> classNameDict = new Dictionary<string, int>();

        private string ImageDir, AnnotationDir, annotationPath;
        private int row, col;
        private int imgW, imgH;
        private double mpx, mpy;
        private HWindow display;
        private HImage image;
        RYOLO node;
        Python.TrainRYOLOv4 train;
        #endregion

        public RYOLOEditorWindow(RYOLO node)
        {
            this.node = node;
            InitializeComponent();
            propertiesGrid.SelectedObject = node.runtime;
            this.DataContext = this;
            this.box_step.DataContext = node.TrainConfig;
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
            this.ImageDir = node.ImageDir;
            this.AnnotationDir = node.AnnotationDir;

            btn_add_image.ItemClick += Btn_add_image_Click;
            btn_add_image_camera.ItemClick += (s, e) =>
            {
                if (node != null)
                {

                    var imageFeed = node.Image.GetCurrentConnectionValue();
                    if (imageFeed != null)

                        try
                        {
                            var image = imageFeed.Clone();
                            var filename = RandomName();
                            var newfile = System.IO.Path.Combine(ImageDir, filename);
                            image.WriteImage("bmp", 0, newfile);
                            var imageadded = new ImageFilmstrip(newfile + ".bmp");
                            ListImage.Add(imageadded);
                            lst_view.SelectedItem = imageadded;

                        }
                        catch (Exception ex)
                        {

                        }
                }
            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
                node.TrainConfig.Save();
                Task.Run(new Action(() =>
                {
                    IsTraining = true;
                    train = new Python.TrainRYOLOv4();
                    //string jsonListAugmentation = JsonConvert.SerializeObject(this.listAugmentation);
                    if (node.TrainConfig.VisualizeLearningProcess)
                    {
                        IsVisualize = true;
                    }
                    else
                    {
                        IsVisualize = false;
                    }
                    VisualizeTrainning();
                    train.TrainPython(node.TrainConfigDir, (trainargs) =>
                    {
                        if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                        {
                            IsTraining = false;
                            IsVisualize = false;
                        }
                        if (trainargs.State == Python.TrainState.Completed)
                        {
                            IsTraining = false;
                            IsVisualize = false;
                            node.runtime.State = ModelState.Unloaded;
                            node.runtime.LoadRecipe(node.TrainConfig.TrainConfigDir);
                            if (fileSystemWatcher != null)
                            {
                                fileSystemWatcher.EnableRaisingEvents = false;
                                fileSystemWatcher.Deleted -= new FileSystemEventHandler(FileChanged);
                                fileSystemWatcher = null;
                            }
                            folderToWatchFor = "";

                        }
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (trainargs.Progress);
                            this.lb_loss.Content = trainargs.Loss;
                        }));
                    });
                }));
            };
            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            LoadImageList(ImageDir);

            this.DataContext = this;
            if (lst_view.Items.Count > 0)
            {
                lst_view.SelectedItem = ListImage[0];
            }
            copyAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {
                copied.Clear();
                foreach(var item in SelectedMarkers)
                {
                    copied.Add(item.Copy());
                }
                

            });
            pasteAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                foreach(var item in copied)
                {
                    AddRegion(item.Copy());
                }

            });
            var copyBinding = new KeyBinding(copyAnnotation, Key.C, ModifierKeys.Control);
            var pasteBinding = new KeyBinding(pasteAnnotation, Key.V, ModifierKeys.Control);
            this.InputBindings.Add(copyBinding);
            this.InputBindings.Add(pasteBinding);
        }
        List<BaseRegionMaker> copied = new List<BaseRegionMaker>();
        public ICommand copyAnnotation { get; set; }
        public ICommand pasteAnnotation { get; set; }
        public void VisualizeTrainning()
        {
            try
            {
                if (node.TrainConfig.VisualizeLearningProcess)
                {
                    if (fileSystemWatcher == null)
                    {
                        d = new DirectoryInfo(node.TrainConfig.ResultDir);
                        FileInfo[] files = d.GetFiles("*.jpg");
                        if (files.Length > 0)
                        {
                            foreach (var file in files)
                            {
                                file.Delete();
                            }
                        }
                        folderToWatchFor = node.TrainConfig.ResultDir;
                        fileSystemWatcher = new FileSystemWatcher(folderToWatchFor);
                        fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastAccess |
                                                            NotifyFilters.DirectoryName | NotifyFilters.Attributes | NotifyFilters.Size;
                        fileSystemWatcher.Changed += new FileSystemEventHandler(FileChanged);
                        fileSystemWatcher.EnableRaisingEvents = true;
                    }
                }
                else
                {
                    original_img.Source = null;
                    predict_img.Source = null;
                }
            }
            catch (Exception ex)
            {

            }
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageFilmstrip>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png") | x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new ImageFilmstrip(file));
                }

            }
            ListImage = result;
        }

        public void UpdateSelectBoxPosition()
        {
            if (SelectedMarker != null)
            {
                SelectedMarker.ConvertCoordinatesImageToWindow(display, out double winposx, out double winposy);
                //display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row1, SelectedMarker.Region.col1, out winposx, out winposy);
                cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }
        }

        public void Update(BaseRegionMaker sender, Region region)
        {
            ChangeRegion();
            //DispOverlay();
            //SaveResult();
        }
        public void Selected(BaseRegionMaker sender, Region region)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                //if (!SelectedMarkers.Contains(SelectedMarker)&&SelectedMarkers.Count==0)
                //{
                //    SelectedMarkers.Add(SelectedMarker);
                //}
                if (!SelectedMarkers.Contains(sender))
                {
                    SelectedMarkers.Add(sender);
                }
                else
                {
                    SelectedMarkers.Remove(sender);
                }

            }
            else
            {
                SelectedMarkers.Clear();
                SelectedMarkers.Add(sender);
            }
            ChangeRegion();
            //DispOverlay();
            
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
                if (SelectedMarkers.Contains(item))
                {
                    display.SetColor(AddOpacity(item.Annotation.Color, Math.Min(100,ColorOpacity*1.5) / 100));
                    item.Display(display);
                    continue;
                }
                display.SetColor(AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                item.Display(display);
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            DispOverlay();
        }

        private void AddRegion(BaseRegionMaker region)
        {
            BaseRegionMaker region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
                SelectedMarkers.Add(CurrentRegionList[0]);
            }
            ChangeRegion();
        }
        private void AddRegionNew(BaseRegionMaker region)
        {
            region.Region.Initial((int)row, (int)col);
            BaseRegionMaker region_add = region;
            CurrentRegionList.Add(region_add);

            region_add.Attach(display, null, Update, Selected);

            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
                SelectedMarkers.Add(CurrentRegionList[0]);
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
        public RegionType shape_type = RegionType.Circle;
        public void SaveAnnotation(string path)
        {
            if (ImageNameWithExtension == null)
            {
                return;
            }
            string imgTrainPath = System.IO.Path.Combine(ImageDir, ImageNameWithExtension);
            var json_data = new JObject();

            if (shape_type == RegionType.Circle)
            {
                json_data["imagePath"] = ImageNameWithExtension;
                image = new HImage(imgTrainPath);
                image.GetImageSize(out imgW, out imgH);
                json_data["imageHeight"] = imgH;
                json_data["imageWidth"] = imgW;
                var shapes = new JObject[CurrentRegionList.Count];
                for (int i = 0; i < CurrentRegionList.Count; i++)
                {
                    shapes[i] = new JObject();
                    if (CurrentRegionList[i].Type != shape_type) { continue; }
                    shapes[i]["label"] = CurrentRegionList[i].Annotation.Name;
                    shapes[i]["group_id"] = CurrentRegionList[i].GroupId.ToString();
                    shapes[i]["shape_type"] = "circle";
                    var serial_data = CurrentRegionList[i].SerializeData();
                    double[] point1 = { serial_data["col"].Value<double>(), serial_data["row"].Value<double>() };
                    double[] point2 = { serial_data["col"].Value<double>() + serial_data["radius"].Value<double>(), serial_data["row"].Value<double>() };
                    object[] points = { point1, point2 };
                    shapes[i]["points"] = JToken.FromObject(points);
                }
                json_data["shapes"] = JToken.FromObject(shapes);
                if (path != null)
                {
                    var new_path = path.Split('.')[0]+".json";
                    System.IO.File.WriteAllText(new_path, JsonConvert.SerializeObject(json_data,Formatting.Indented));
                }
                return;
            }
            string annotationTrainPath = System.IO.Path.Combine(AnnotationDir, "annotations.txt");
            string classNameTrainPath = System.IO.Path.Combine(AnnotationDir, "className.names");
            
            JObject[] data = new JObject[CurrentRegionList.Count];
            string dataTrain = "";
            dataTrain += imgTrainPath + ",";
            for (int i = 0; i < data.Length; i++)
            {
                if (CurrentRegionList[i].Type != shape_type) { continue; }
                data[i] = new JObject();
                var serial_data = CurrentRegionList[i].SerializeData();
                if (CurrentRegionList[i].Type == RegionType.Rectangle1)
                {
                    data[i]["x"] = serial_data["col1"];
                    data[i]["y"] = serial_data["row1"];
                    data[i]["w"] = serial_data["col2"].Value<double>() - serial_data["col1"].Value<double>();
                    data[i]["h"] = serial_data["row2"].Value<double>() - serial_data["row1"].Value<double>();
                    string className = CurrentRegionList[i].Annotation.Name;
                    data[i]["annotation"] = className;


                    if (!classNameDict.ContainsKey(className))
                    {
                        int count = classNameDict.Count;
                        classNameDict.Add(className, count);

                    }

                    dataTrain += data[i]["x"].ToString() + "," + data[i]["y"].ToString() + "," + serial_data["col2"].ToString() + ",";
                    if (i == data.Length - 1)
                    {
                        dataTrain += serial_data["row2"].ToString() + "," + classNameDict[className].ToString();
                    }
                    else
                    {
                        dataTrain += serial_data["row2"].ToString() + "," + classNameDict[className].ToString() + " ";
                    }
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
            SelectedMarkers.Clear();
            var datatxt = System.IO.File.ReadAllText(path);
            
            if (shape_type == RegionType.Circle)
            {
                var new_path = path.Split('.')[0] + ".json";
                datatxt = System.IO.File.ReadAllText(new_path);
                var json_data = JsonConvert.DeserializeObject<JObject>(datatxt);
                foreach (var item in json_data["shapes"])
                {
                    var annotation = item["label"].ToString();
                    double row = item["points"].Value<JArray>()[0][1].Value<double>();
                    double col = item["points"].Value<JArray>()[0][0].Value<double>();
                    double row1 = item["points"].Value<JArray>()[1][1].Value<double>();
                    double col1 = item["points"].Value<JArray>()[1][0].Value<double>();
                    HOperatorSet.DistancePp(row1, col1, row, col, out HTuple radius);
                    var circle = new Circle(false) { row = row, col = col, radius = radius };
                    var color = ClassList.FirstOrDefault(x1 => x1.Name == annotation);
                    if (color != null)
                    {
                        AddRegion(new CircleMaker() { Region = circle, Annotation = color });
                    }
                    else
                    {
                        AddRegion(new CircleMaker() { Region = circle, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
                    }
                }
                return;
            }
            var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
            foreach (var item in data)
            {
                var annotation = item["annotation"].ToString();
                var color = ClassList.FirstOrDefault(x1 => x1.Name == annotation);
                int type = 2; //RegionType.Rectangle1
                if (item.ContainsKey("shape_type"))
                {
                    type = Int32.Parse(item["shape_type"].ToString());
                }
                if (type != (int)shape_type) { continue; }
                double x = item["x"].ToObject<double>();
                double y = item["y"].ToObject<double>();
                double w = item["w"].ToObject<double>();
                double h = item["h"].ToObject<double>();
                if (color != null)
                {
                    AddRegion(new Rectangle1Maker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = color });
                }
                else
                {
                    AddRegion(new Rectangle1Maker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
                }

            }
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
                    if (shape_type == RegionType.Circle)
                    {
                        annotationPath = annotationPath.Split('.')[0] + ".json";
                    }
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

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #region event onClick for btns
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
                        string newfile = RandomName();
                        newfile = newfile + extension;
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageFilmstrip(newfile));
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private string RandomName()
        {
            var filename = RandomString(8);
            var newfile = System.IO.Path.Combine(ImageDir, filename);
            while (true)
            {
                if (System.IO.File.Exists(newfile))
                {
                    filename = RandomString(16);
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
            ClassList.Add(new YOLOClass() { Color = "#00ff00ff", Name = "unknown" });
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageFilmstrip;
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
                    else if (System.IO.File.Exists(imagegtpath.Split('.')[0] + ".json"))
                    {
                        System.IO.File.Delete(imagegtpath.Split('.')[0] + ".json");
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
            if (SelectedClass != null)
            {
                switch (shape_type)
                {
                    case(RegionType.Circle):
                        AddRegionNew(new CircleMaker() { Annotation = SelectedClass, Region = new Circle(false) { row = mpy, col = mpx, radius = 50 } });
                        break;
                    case (RegionType.Rectangle1):
                        AddRegionNew(new Rectangle1Maker() { Annotation = SelectedClass, Region = new Rectange1(false) { row1 = mpy, col1 = mpx, row2 = mpy + 50, col2 = mpx + 50 } });
                        break;
                }
                    

                
            }

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

        private FileSystemWatcher fileSystemWatcher;
        private string folderToWatchFor = "";
        DirectoryInfo d;
        private void btn_monitor_onClick(object sender, RoutedEventArgs e)
        {


        }
        bool DipatcherForFileChanged = false;
        ImageSource originalImage;
        ImageSource preditctImage;
        List<System.Windows.Threading.DispatcherOperation> dispatcherOperations = new List<System.Windows.Threading.DispatcherOperation>();
        private void FileChanged(Object sender, FileSystemEventArgs e)
        {
            if (IsTraining)
            {
                Thread.Sleep(100);
                DipatcherForFileChanged = !DipatcherForFileChanged;
                if (DipatcherForFileChanged)
                {
                    FileInfo[] Files = d.GetFiles("*.jpg").OrderByDescending(f => f.LastWriteTime).ToArray();
                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)delegate ()
                    {
                        foreach (FileInfo file in Files.Take(2))
                        {
                            if (file.Name.Contains("original_img_with_epoch"))
                            {
                                originalImage = (new ImageSourceConverter()).ConvertFromString(file.FullName) as ImageSource;
                                original_img.Source = originalImage;
                            }
                            if (file.Name.Contains("predict_img_with_epoch"))
                            {
                                preditctImage = (new ImageSourceConverter()).ConvertFromString(file.FullName) as ImageSource;
                                predict_img.Source = preditctImage;
                            }
                        }
                    });
                }
            }
        }
        #endregion

        #region event for keys
        private void window_display_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (SelectedMarkers.Count>0)
                {
                    foreach(var item in SelectedMarkers)
                    {
                        item.Region.ClearDrawingData(display);
                        var index = CurrentRegionList.IndexOf(item);
                        CurrentRegionList.Remove(item);
                        if (CurrentRegionList.Count > 1)
                        {
                            SelectedMarker = CurrentRegionList[index];
                        }
                        else if (CurrentRegionList.Count > 0)
                        {
                            SelectedMarker = CurrentRegionList[0];
                        }
                        else
                        {
                            SelectedMarker = null;
                        }
                    }
                    SelectedMarkers.Clear();
                    SelectedMarkers.Add(SelectedMarker);
                    UpdateSelectBoxPosition();
                    DispOverlay();
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

        }

        private void window_display_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void btn_train_cmd_Click(object sender, RoutedEventArgs e)
        {
            box_step.Visibility = Visibility.Hidden;
            try
            {
                node.TrainConfig.Save();
                NOVisionPython.TrainConsole("Runtime", "ryolo", node.TrainConfigDir);
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

        private void btn_create_Click(object sender, RoutedEventArgs e)
        {
            //if (ImageName == String.Empty)
            //{
            //    return;
            //}
            //if (node.runtime.State == ModelState.NotFound)
            //{
            //    return;
            //}
            //if (node.runtime.State == ModelState.Unloaded)
            //{
            //    node.runtime.LoadRecipe(node.ModelDir);
            //}
            //var img = new HImage(System.IO.Path.Combine(node.ImageDir, ImageName));
            //if (node.runtime.State == ModelState.Loaded)
            //{
            //    var result = node.runtime.PredictV2(img);
            //    if (result.Count == 0)
            //    {
            //        return;
            //    }

            //    var reshaped = result[0][0].reshape(new int[] { -1, 6 });
            //    foreach(var item in reshaped)
            //    {

            //    }
            //}
        }

        private void btn_clear_image(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //AnomalyV3Window.EmptyFolder(node.TrainConfig.ImageDir);
            //AnomalyV3Window.EmptyFolder(node.TrainConfig.AnnotationDir);
            LoadImageList(node.TrainConfig.ImageDir);
        }

        private void btn_open_image_folder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", node.TrainConfig.ImageDir);
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
        #endregion

        #region event for checkbox
        #endregion


        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
