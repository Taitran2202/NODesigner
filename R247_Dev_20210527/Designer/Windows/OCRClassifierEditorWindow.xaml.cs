using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
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
    public partial class OCRClassifierEditorWindow : Window, INotifyPropertyChanged
    {

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        private bool _isTrainning;
        public bool IsTrainning
        {
            get
            {
                return _isTrainning;
            }
            set
            {
                if (_isTrainning != value)
                {
                    _isTrainning = value;
                    RaisePropertyChanged("IsTrainning");
                }
            }
        }

        private bool _isMargin = true;
        public bool IsMargin
        {
            get
            {
                return _isMargin;
            }
            set
            {
                if (_isMargin != value)
                {
                    _isMargin = value;
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

        private double _colorOpacity = 50;
        public double ColorOpacity
        {
            get
            {
                return _colorOpacity;
            }
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
            get
            {
                return _imageName;
            }
            set
            {
                if (_imageName != value)
                {
                    _imageName = value;
                    RaisePropertyChanged("ImageName");
                }
            }
        }

        private string _colorGood = "#00ff0011";
        public string ColorGood
        {
            get
            {
                return _colorGood;
            }
            set
            {
                if (_colorGood != value)
                {
                    _colorGood = value;
                    RaisePropertyChanged("ColorGood");
                }
            }
        }

        string _colorBad = "#ff000055";
        public string ColorBad
        {
            get
            {
                return _colorBad;
            }
            set
            {
                if (_colorBad != value)
                {
                    _colorBad = value;
                    RaisePropertyChanged("ColorBad");
                }
            }
        }

        private double _scale = 1.0;
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

        private double _smooth = 1;
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

        private ClassifierClass1 _selectedClass;
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
        public ObservableCollection<ClassifierClass1> ClassList { get; set; }
        public List<RegionMaker> CurrentRegionList { get; set; } = new List<RegionMaker>();
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();

        public delegate void RegionMaker2Callback(RegionMaker2 sender, Region region);

        public delegate void RegionMakerCallback(RegionMaker sender, Region region);

        private Dictionary<string, int> classNameDict = new Dictionary<string, int>();
        private Dictionary<string, string> annotationDict = new Dictionary<string, string>();

        private HWindow display;
        private HImage image_mask;
        private HImage image;
        private HHomMat2D transform;
        private State current_state = State.Pan;
        private HTuple w, h;

        
        private string ImageDir, AnnotationDir, ModelDir, RootDir, annotationPath;
        private int row, col;
        private double mpx, mpy;
        private int imgW, imgH;
        #endregion

        public OCRClassifierEditorWindow(string rootDir, string imageDir, string annotationDir, string modelDir, ObservableCollection<ClassifierClass1> classList,
            Action<Action<int>, Action<bool>> trainFuntion, OCRForDigitsAndCharacters ocrClassifier)
        {
            InitializeComponent();
            if (listAugmentation.Count > 0)
            {
                foreach (var item in listAugmentation)
                {
                    this.listAugmentation.Add(item);
                    list_augmentation.Items.Add(item);
                }
            }

            this.ClassList = classList;
            cmb_select_class.ItemsSource = this.ClassList;
            lst_class.ItemsSource = this.ClassList;
            this.ImageDir = imageDir;
            this.ModelDir = modelDir;
            this.AnnotationDir = annotationDir;
            this.RootDir = rootDir;

            btn_add_image.Click += Btn_add_image_Click;
            btn_add_image_camera.Click += (s, e) =>
            {

            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };

            btn_step_ok.Click += (sender, e) =>
            {
                var step = spin_step.Value;
                box_step.Visibility = Visibility.Hidden;

                Task.Run(new Action(() =>
                {
                    IsTrainning = true;
                    Python.TrainOCRClassifier train = new Python.TrainOCRClassifier();
                    string jsonListAugmentation = JsonConvert.SerializeObject(this.listAugmentation);
                    train.TrainPython("",(trainargs) =>
                    {
                        //if (iscancel | isfinish)
                        //{
                        //    IsTrainning = false;
                        //}
                        //if (isfinish)
                        //{
                        //}
                        //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        //{
                        //    this.progress.Value = (progress);
                            
                        //}));
                    });
                }));
            };

            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            LoadImageList(ImageDir);
            this.DataContext = this;
        }

        private void Progress(int progress)
        {

        }
        private void TrainResult(bool result)
        {

        }


        private void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageFilmstrip>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png"));
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
                double winposx, winposy;
                display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row1, SelectedMarker.Region.col1, out winposx, out winposy);
                cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }
        }

        public void Update(RegionMaker sender, Region region)
        {
            //Selected_region = sender;
            //UpdateSelectBoxPosition();
            ChangeRegion();
            DispOverlay();
            SaveResult();
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
            //display.SetColor("#00ff0025");
            foreach (var item in CurrentRegionList)
            {
                //display.SetColor(item.Color);
                display.SetColor(AddOpacity(item.Annotation.Color, ColorOpacity / 100));
                display.DispObj(item.Region.region);
                display.DispText(item.Annotation.Name, "image", item.Region.row1, item.Region.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
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

            //lst_region.SelectedItem = region_add;
            ChangeRegion();
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
                    UpdateSelectBoxPosition();
                }
            }
            catch
            {
                return;
            }
        }

        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public void SaveResult()
        {
            SaveAnnotation(annotationPath);
        }

        public void SaveAnnotation(string path)
        {
            JObject[] data = new JObject[CurrentRegionList.Count];
            string dataTrain = "";
            string annotationTrainPath = System.IO.Path.Combine(AnnotationDir, "annotations.txt");
            string classNameTrainPath = System.IO.Path.Combine(AnnotationDir, "className.names.nams");
            string imgTrainPath = System.IO.Path.Combine(ImageDir, ImageNameWithExtension);
            dataTrain += imgTrainPath + " ";
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
                    AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = color });
                }
                else
                {
                    AddRegion(new RegionMaker() { Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
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

        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
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
                }
                catch (Exception ex)
                {

                }

            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedClass != null)
            {
                AddRegionNew(new RegionMaker() { Annotation = SelectedClass, Region = new Rectange1(false) { row1 = mpy, col1 = mpx, row2 = mpy + 50, col2 = mpx + 50 } });
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

        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            ClassList.Add(new ClassifierClass1() { Color = "#00ff00ff", Name = "unknown" });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as ClassifierClass1;
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
        private void btn_pre_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
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
                        var newfile = System.IO.Path.Combine(ImageDir, filename);
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageFilmstrip(newfile));
                    }
                    catch (Exception ex)
                    {

                    }
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


        #region define classes
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

        public class RegionMaker2
        {
            public int GroupId { get; set; } = -1;
            private bool _isSelected = false;

            public Rectange2 Region { get; set; }
            public ClassifierClass1 Annotation { get; set; }
            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
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

        public class RegionMaker
        {
            public int GroupId { get; set; } = -1;
            private bool _isSelected = false;
            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
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

        #endregion


    }
}
