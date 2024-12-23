using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public partial class RYOLOv4Window : ThemedWindow, INotifyPropertyChanged
    {

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

        private Rectangle2Maker _selectedMarker;
        public Rectangle2Maker SelectedMarker
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
        private List<Rectangle2Maker> _selectedMarkers = new List<Rectangle2Maker>();
        public List<Rectangle2Maker> SelectedMarkers
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
        public List<Rectangle2Maker> CurrentRegionList { get; set; } = new List<Rectangle2Maker>();

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
        RYOLOv4 node;
        Python.TrainRYOLOv4 train;
        #endregion
        TrainningStatusViewModel trainningStatusViewModel;
        public RegionType shape_type = RegionType.Rectangle2;
        public void StartTrainning()
        {
            trainningStatusViewModel?.Dispose();
            trainningStatusViewModel = new TrainningStatusViewModel();
            grid_trainning_status.DataContext = trainningStatusViewModel;
            node.TrainConfig.Save();
            Task.Run(new Action(() =>
            {
                IsTraining = true;
                train = new Python.TrainRYOLOv4();

                string jsonListAugmentation = JsonConvert.SerializeObject(this.listAugmentation);
                node.TrainConfig.Save();
                trainningStatusViewModel.StartListen();
                train.TrainPython(node.TrainConfig.ConfigDir, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTraining = false;
                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTraining = false;
                        node.runtime.State = ModelState.Unloaded;

                    }
                });
            }));
        }
        public RYOLOv4Window(RYOLOv4 node)
        {
            this.node = node;
            InitializeComponent();
            propertiesGrid.SelectedObject = node.runtime;
            this.DataContext = this;
            this.stack_train_parameter.DataContext = node.TrainConfig;
            //this.stack_graycalse_option.DataContext = this;
            //stack_graycalse_option.Visibility = Visibility.Visible;
            //this.stack_retrain_option.DataContext = this;
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

            //var copyBinding = new KeyBinding(copyAnnotation, Key.C, ModifierKeys.Control);
            //var pasteBinding = new KeyBinding(pasteAnnotation, Key.V, ModifierKeys.Control);
            //this.InputBindings.Add(copyBinding);
            //this.InputBindings.Add(pasteBinding);

            this.Closed += (o, e) =>
            {
                trainningStatusViewModel?.Dispose();
            };
        }
        Rectangle2Maker copied;
        public ICommand copyAnnotation { get; set; }
        public ICommand pasteAnnotation { get; set; }

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
                cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }
        }

        public void Update(BaseRegionMaker sender, Region region)
        {
            ChangeRegion();
            DispOverlay();
            SaveResult();
        }
        public void Selected(BaseRegionMaker sender, Region region)
        {
            ChangeRegion();
            DispOverlay();
            SelectedMarker = (Rectangle2Maker)sender;
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
                    display.SetColor(AddOpacity(item.Annotation.Color, Math.Min(100, ColorOpacity * 1.5) / 100));
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

        private void AddRegion(Rectangle2Maker region)
        {
            Rectangle2Maker region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }
            ChangeRegion();
        }



        private void AddRegionNew(Rectangle2Maker region)
        {
            region.Region.Initial((int)row, (int)col);
            Rectangle2Maker region_add = region;
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
            dataTrain += imgTrainPath + " ";
            for (int i = 0; i < data.Length; i++)
            {
                string className = "";
                data[i] = new JObject();
                data[i]["row"] = (CurrentRegionList[i].Region as Rectange2).row;
                data[i]["col"] = (CurrentRegionList[i].Region as Rectange2).col;
                data[i]["phi"] = (CurrentRegionList[i].Region as Rectange2).phi;
                data[i]["lenght1"] = (CurrentRegionList[i].Region as Rectange2).length1;
                data[i]["lenght2"] = (CurrentRegionList[i].Region as Rectange2).length2;
                className = CurrentRegionList[i].Annotation.Name;
                data[i]["annotation"] = className;
                if (!classNameDict.ContainsKey(className))
                {
                    int count = classNameDict.Count;
                    classNameDict.Add(className, count);

                }

                dataTrain += data[i]["row"].ToString() + "," + data[i]["col"].ToString() + "," + (CurrentRegionList[i].Region as Rectange2).phi.ToString() + "," + (CurrentRegionList[i].Region as Rectange2).length1.ToString() + ",";
                if (i == data.Length - 1)
                {
                    dataTrain += (CurrentRegionList[i].Region as Rectange2).length2.ToString() + "," + classNameDict[className].ToString();
                }
                else
                {
                    dataTrain += (CurrentRegionList[i].Region as Rectange2).length2.ToString() + "," + classNameDict[className].ToString() + " ";
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
                double row = item["row"].ToObject<double>();
                double col = item["col"].ToObject<double>();
                double phi = item["phi"].ToObject<double>();
                double lenght1 = item["lenght1"].ToObject<double>();
                double lenght2 = item["lenght2"].ToObject<double>();
                var rectangle2 = new Rectange2(false) { row = row, col = col, phi = phi, length1 = lenght1, length2 = lenght2 };
                var color = ClassList.FirstOrDefault(x1 => x1.Name == annotation);
                if (color != null)
                {
                    AddRegion(new Rectangle2Maker() { Region = rectangle2, Annotation = color });
                }
                else
                {
                    AddRegion(new Rectangle2Maker() { Region = rectangle2, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
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

                    case (RegionType.Rectangle2):
                        AddRegionNew(new Rectangle2Maker() { Annotation = SelectedClass, Region = new Rectange2(false) { row = mpy, col = mpx, phi = 0, length1 = 50, length2 = 50 } });
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
        private void btn_monitor_onClick(object sender, RoutedEventArgs e)
        {


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
            try
            {
                node.TrainConfig.Save();
                NOVisionPython.TrainConsole("detection", "ryolov4", node.TrainConfig.ConfigDir);
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
            Keyboard.Focus(window_display);
        }

        private void Btn_auto_label(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedClass == null || image == null)
                {
                    return;
                }
                CurrentRegionList.Clear();
                HRegion region = image.Threshold(16.0, 96).Connection().SelectShape("area", "and", 1000.0, double.MaxValue);
                for (int i = 1; i < region.CountObj() + 1; i++)
                {
                    region[i].SmallestRectangle2(out HTuple row, out HTuple col, out HTuple phi, out HTuple l1, out HTuple l2);
                    AddRegion(new Rectangle2Maker()
                    {
                        Annotation = SelectedClass,
                        Region = new Rectange2(false)
                        {
                            row = row,
                            col = col,
                            phi = phi,
                            length1 = l1,
                            length2 = l2,
                        }
                    });
                    DispOverlay();
                }
                SaveResult();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.ToString());
            }
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
                    if (node.runtime.State == ModelState.NotFound)
                    {
                        return;
                    }
                    if (SelectedClass == null || image == null)
                    {
                        return;
                    }
                    CurrentRegionList.Clear();
                    if (node.runtime.State == ModelState.Unloaded)
                    {
                        node.runtime.LoadRecipe(node.TrainConfig.SavedModelDir);
                    }
                    if (node.runtime.State == ModelState.Loaded)
                    {
                        var result = node.runtime.PredictRYOLO(image, node.TrainConfig.Classes.Count, 100, 50, 50);
                        int[] shape = result.shape; // no_boxes, xywhaconfcls
                        if (shape is null)
                            return;
                        if (shape[0] == 0)
                            return;
                        for (int i = 0; i < shape[0]; i++)
                        {
                            NDArray xywhaScoreClass = result[string.Format("{0}, 0:7", i)];
                            float xc = xywhaScoreClass[0];
                            float yc = xywhaScoreClass[1];
                            float w = xywhaScoreClass[2];
                            float h = xywhaScoreClass[3];
                            float angle = xywhaScoreClass[4];
                            float confidence = xywhaScoreClass[5];
                            float classes = xywhaScoreClass[6];
                            int cls = (int)classes;
                            HRegion region = new HRegion();
                            region.GenRectangle2((HTuple)yc, (HTuple)xc, (HTuple)(angle * -1.0f), (HTuple)(w / 2.0f), (HTuple)(h / 2.0f));
                            AddRegion(new Rectangle2Maker()
                            {
                                Annotation = SelectedClass,
                                Region = new Rectange2(false)
                                {
                                    row = (HTuple)yc,
                                    col = (HTuple)xc,
                                    phi = (HTuple)(angle * -1.0f),
                                    length1 = (HTuple)(w / 2.0f),
                                    length2 = (HTuple)(h / 2.0f),
                                }
                            });
                            DispOverlay();
                        }
                        SaveResult();
                    }

                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.ToString());
            }
        }

        string auto_label_mode;
        private void cmb_auto_label_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmb_mask_mode.SelectedIndex)
            {
                case 0:
                    {
                        if (stack_graycalse_option != null && stack_retrain_option != null)
                        {
                            stack_graycalse_option.Visibility = Visibility.Visible;
                            stack_retrain_option.Visibility = Visibility.Collapsed;
                            auto_label_mode = "grayscale";
                            break;
                        }
                        else
                            break;

                        
                       
                    }                                      
                case 1:
                    {
                        if (stack_graycalse_option != null && stack_retrain_option != null)
                        {
                            stack_graycalse_option.Visibility = Visibility.Collapsed;
                            stack_retrain_option.Visibility = Visibility.Visible;
                            auto_label_mode = "retrain";
                            break;
                        }
                        else
                            break;
                    }
                    
                   
            }
        }

        private void Btn_generate_label(object sender, RoutedEventArgs e)
        {
            if(auto_label_mode == "grayscale")
            {
                Btn_auto_label(null, null);
            }
            else if (auto_label_mode == "retrain")
            {
                btn_acept_view_Click(null, null);
            }
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
