using DevExpress.Xpf.Bars;
using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static NOVisionDesigner.Designer.Nodes.OCRNode;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for OCRWindow.xaml
    /// </summary>
    public partial class OCRWindow : Window, INotifyPropertyChanged
    {
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
        DisplayMode displaymodes;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string imagedir, annotationdir, modeldir;
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

        void LoadImageList(string dir)
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
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                //Display = ocrwindow.window_display.HalconWindow;
                this.window_display.HalconWindow.SetDraw("fill");
                this.window_display.HalconWindow.SetColor(this.ColorDraw);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }
        public List<ClassifierClass1> ClassList { get; set; }
        //public bool Retrained { get; set; } = false;
        public OCRNode OCRnode;
        TrainOCR train;
        string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public OCRWindow(OCRNode OCRnode)
        {
            InitializeComponent();
            this.OCRnode = OCRnode;
            displaymodes = new EditDisplayMode(window_display, this);
            
            ClassList = new List<ClassifierClass1>();
            foreach (char item in CharList)
            {
                ClassList.Add(new ClassifierClass1() { Name = item.ToString() });
            }
            cmb_select_class.ItemsSource = this.ClassList;
            lst_class.ItemsSource = this.ClassList;
            this.imagedir = OCRnode.ImageDir;
            this.modeldir = OCRnode.ModelDir;
            this.annotationdir = OCRnode.AnnotationDir;
            stack_properties.DataContext = OCRnode;
            rad_edit.Checked += Rad_edit_Checked;
            rad_view.Checked += Rad_view_Checked;
            btn_add_image.Click += Btn_add_image_Click;
            btn_add_current_result.Click += (s, e) =>
            {
                var Region = OCRnode.RegionInput.Value;
                var image = OCRnode.ImageInput.Value;
                if (Region != null & image!=null)
                {
                    if (Region.CountObj() > 0)
                    {
                        HImage image_infer;
                        HTuple row1R = 0, col1R = 0, row2R = 0, col2R = 0;
                        image.GetImageSize(out HTuple imW, out HTuple imH);
                        Region.Union1().SmallestRectangle1(out  row1R, out  col1R, out  row2R, out  col2R);
                        row1R = Math.Max(row1R, 0);
                        col1R = Math.Max(col1R, 0);
                        if (row1R < imH & col1R < imW)
                        {
                            image_infer = image.CropRectangle1(row1R, col1R, row2R, col2R);
                            SaveImageTrain(image_infer);
                           
                        }
                        
                    }
                }
                
            };
            btn_add_image_camera.Click += (s, e) =>
            {
                if (OCRnode != null)
                {

                    var imageFeed = OCRnode.ImageInput;
                    HRegion regionInput = null;
                    //if (classifier.RegionInput.Value.SourceLink != null)
                    //{
                    if (OCRnode.RegionInput.Value != null)
                    {
                        regionInput = OCRnode.RegionInput.Value.Clone();
                    }
                    //}
                    else
                    {
                        regionInput = OCRnode.RegionInput.Value.Clone();
                    }


                    if (imageFeed != null)
                        if (imageFeed.Value != null)
                        {
                            try
                            {
                                SaveImageTrain(imageFeed.Value);
                                
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                }
            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
                train?.Cancel();
            };
            btn_step_cancel_classifier.Click += (sender, e) =>
            {
                box_step_classifier.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                int step = (int) spin_step.Value;
                box_step.Visibility = Visibility.Hidden;
                this.OCRnode.DetectionTrainConfig.Epochs = (int)step;
                this.OCRnode.DetectionTrainConfig.Save(this.OCRnode.DetectionTrainConfigDir);
                Task.Run(new Action(() =>
                {
                    IsTrainning = true;
                    train = new TrainOCR();
                    train.TrainPython(OCRnode.DetectionTrainConfigDir, (trainargs) =>
                    {
                        if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                        {
                            IsTrainning = false;
                        }
                        if (trainargs.State == Python.TrainState.Completed)
                        {
                            OCRnode.detector.PostTrainEvent();
                            OCRnode.detector.LoadRecipe();
                            IsTrainning = false;
                        }
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (trainargs.Progress);
                            txt_acc.Text = trainargs.Accuracy.ToString();
                        }));
                    });
                }));
            }; 
            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            btn_train_classifier.Click += (sender, e) =>
            {
                box_step_classifier.Visibility = Visibility.Visible;
            };
            btn_step_ok_classifier.Click += (sender, e) =>
            {
                int step = (int)spin_step_classifier.Value;
                box_step_classifier.Visibility = Visibility.Hidden;
                this.OCRnode.ClassificationTrainConfig.Epochs = (int)step;
                this.OCRnode.ClassificationTrainConfig.Save(this.OCRnode.ClassificationTrainConfigDir);
                Task.Run(new Action(() =>
                {
                    IsTrainning = true;
                    TrainOCRClassifier train = new TrainOCRClassifier();
                    train.TrainPython(this.OCRnode.ClassificationTrainConfigDir,(trainargs) =>
                    {
                        if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                        {
                            IsTrainning = false;
                        }
                        if (trainargs.State == Python.TrainState.Completed)
                        {
                            OCRnode.classifier.ClearTensorRTProfile();
                            OCRnode.classifier.LoadRecipe();
                            IsTrainning = false;
                        }
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (trainargs.Progress);
                            txt_acc.Text = trainargs.Accuracy.ToString();
                        }));
                    });
                }));
            };
            LoadImageList(imagedir);
            this.DataContext = this;
            stack_detection_train_options.DataContext = OCRnode.DetectionTrainConfig;
            stack_classifier_train_options.DataContext = OCRnode.ClassificationTrainConfig;
            stack_detection_augmentation.DataContext = OCRnode.DetectionTrainConfig.AugmentationSetting;
            stack_classifier_augmentation.DataContext = OCRnode.ClassificationTrainConfig.AugmentationSetting;

        }

        private void Rad_view_Checked(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
            if (displaymodes is ViewDisplayMode)
            {

            }
            else
            {
                displaymodes.Dispose();
                displaymodes = new ViewDisplayMode(this);
            }
        }

        private void Rad_edit_Checked(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
            if(displaymodes is  EditDisplayMode)
            {
                
            }
            else
            {
                displaymodes.Dispose();
                displaymodes = new EditDisplayMode(window_display, this);
            }
        }
        public void ChangeDisplayMode()
        {
            if (displaymodes is EditDisplayMode)
            {
                displaymodes.Dispose();
                displaymodes = new ViewDisplayMode(this);
            }
            else
            {
                displaymodes.Dispose();
                displaymodes = new EditDisplayMode(window_display, this);
            }
        }
        private void SaveImageTrain(HImage imageFeed)
        {
            var image = imageFeed.Clone();
            string filename = DateTime.Now.Ticks.ToString();
            string newfile = System.IO.Path.Combine(imagedir, filename);
            image.WriteImage("bmp", 0, newfile);
            var imageadded = new ImageFilmstrip(newfile + ".bmp");
            ListImage.Add(imageadded);
            lst_view.SelectedItem = imageadded;
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
        double _scale = 1;
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
        public string annotation_path;

        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0)
                {
                    var selected = e.AddedItems[0] as ImageFilmstrip;
                    if (selected != null)
                    {
                        try
                        {
                            displaymodes.OnImageChanging(null, annotation_path);
                            
                        }

                        catch (Exception ex)
                        {

                        }

                        try
                        {
                            CurrentImage = new HImage(selected.FullPath);
                            CurrentImage.GetImageSize(out im_w, out im_h);
                            window_display.HalconWindow.AttachBackgroundToWindow(CurrentImage);
                            window_display.HalconWindow.ClearWindow();
                        }
                        catch (Exception ex)
                        {
                            //image = new HImage();
                        }
                        //load current annotation
                        try
                        {
                            
                            ImageName = System.IO.Path.GetFileNameWithoutExtension(selected.FullPath);
                            annotation_path = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileName(selected.FullPath) + ".txt");

                            displaymodes.OnImageChanged(null, annotation_path);

                            

                        }
                        catch (Exception ex)
                        {

                        }
                        
                    }
                }
               
            }
            catch
            {
                return;
            }
        }

       
        //color are define with format "#rrggbbaa".
        public static string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        int im_w, im_h;

        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            ClassList.Add(new ClassifierClass1() { Color = "#00ff00ff", Name = "unknown" });
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
        
        

        public HImage CurrentImage;
        HHomMat2D transform;
        State current_state = State.Pan;
        HTuple w, h;

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void window_display_PreviewKeyDown(object sender, KeyEventArgs e)
        {

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
       
    }
    public class CharacterGroup
    {
        public int GroupId { get; set; }
        public List<RegionMaker> Items = new List<RegionMaker>();
    }
    interface DisplayMode: IDisposable
    {
        void OnImageChanging(string imagepath,string annotationpath);
        void OnImageChanged(string imagepath,string annotationpath);

    }
    public class ViewDisplayMode : DisplayMode
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
            updatebinding.Dispose();
        }
        OCRWindow wd;
        OCRNode ocrnode;
        IDisposable updatebinding;
        BarButtonItem newannotation_menu;
        public ViewDisplayMode(OCRWindow wd)
        {
            this.wd = wd;
            this.ocrnode = wd.OCRnode;
            updatebinding = ocrnode.WhenAnyValue(x => x.LinkThreshold, x => x.TextHeight, 
                x => x.TextThreshold, x => x.MinWidth, x => x.Confidence,x=>x.MinSize)
                .Subscribe(x =>UpdateView(x.Item1,x.Item2,x.Item3,x.Item4,x.Item5));
            PopupMenu popupMenu = new PopupMenu();
            newannotation_menu = new BarButtonItem() { Content = "Accept view to annotation" };
            newannotation_menu.ItemClick += MenuItem_Click;
            popupMenu.Items.Add(newannotation_menu);
            wd.window_display.SetValue(BarManager.DXContextMenuProperty, popupMenu);
        }
        public static (int x,int y)  ConvertCor(double x_in, double y_in, double center_x, double center_y,double theta)
        {
            double x = center_x + (x_in - center_x) * Math.Cos(theta) + (y_in - center_y) * Math.Sin(theta);
            double y = center_y - (x_in - center_x) * Math.Sin(theta) + (y_in - center_y) * Math.Cos(theta);
            return ((int)x, (int)y);
        }
        public static Rect1 BoundingBox(double row,double col, double phi, double length1, double length2)
        {
            double[] x_cors = new double[4] { col - length1, col + length1, col - length1, col + length1 };
            double[] y_cor = new double[4] { row - length2, row - length2, row + length2, row + length2 };
            double[] x_ori = new double[4];
            double[] y_ori = new double[4];
            for (int i = 0; i < 4; i++)
            {
                var originalPoint = ConvertCor(x_cors[i], y_cor[i], col, row, phi);
                x_ori[i] = originalPoint.x;
                y_ori[i] = originalPoint.y;
            }
            var row1 = y_ori.Min();
            var row2 = y_ori.Max();
            var col1 = x_ori.Min();
            var col2 = x_ori.Max();
            return new Rect1(row1, col1, row2, col2);
        }
        public static Rect1 BoundingBox(Rect2 rect2)
        {
            double[] x_cors = new double[4] { rect2.col - rect2.length1, rect2.col + rect2.length1, rect2.col - rect2.length1, rect2.col + rect2.length1 };
            double[] y_cor = new double[4] { rect2.row - rect2.length2, rect2.row - rect2.length2, rect2.row + rect2.length2 , rect2.row + rect2.length2 };
            double[] x_ori = new double[4];
            double[] y_ori = new double[4];
            for(int i = 0; i < 4; i++)
            {
                var originalPoint = ConvertCor(x_cors[i], y_cor[i], rect2.col, rect2.row, rect2.phi);
                x_ori[i] = originalPoint.x;
                y_ori[i] = originalPoint.y;
            }
            var row1 = y_ori.Min();
            var row2 = y_ori.Max();
            var col1 = x_ori.Min();
            var col2 = x_ori.Max();
            return new Rect1(row1, col1, row2, col2);
        }
        private void MenuItem_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (CharactersList != null)
            {
                JObject allData = new JObject();
                JObject[] data = new JObject[CharactersList.Count];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = new JObject();
                    //convert rotated rectangle to rectangle 1 (temporary)
                    var rect = BoundingBox(CharactersList[i].Box);
                    data[i]["x"] = rect.col1;
                    data[i]["y"] = rect.row1;
                    data[i]["w"] = rect.col2 - rect.col1;
                    data[i]["h"] = rect.row2 - rect.row1;
                    data[i]["annotation"] = CharactersList[i].ClassName;
                    data[i]["groupId"] = -1;
                }
                allData["characters"] = JArray.FromObject(data);

                //group data
                JObject[] groupData = new JObject[0];
                
                allData["groups"] = JArray.FromObject(groupData);
                System.IO.File.WriteAllText(wd.annotation_path, JsonConvert.SerializeObject(allData));
            }
            
        }
        
        List<CharacterResult> CharactersList;
        public void UpdateView(int LinkThreshold,int TextHeight,int TextThreshold,int MinWidth,int Confidence)
        {
            
            if (wd.CurrentImage != null)
            {
                Task.Run(new Action(() =>
                {
                    var InspectionContext = new InspectionContext(null,wd.CurrentImage, 1, 1, "");
                    var result = ocrnode.RunInside(wd.CurrentImage, wd.CurrentImage, InspectionContext);
                    CharactersList = result.Item4;
                    wd.window_display.HalconWindow.ClearWindow();
                    InspectionContext.inspection_result.Display(wd.window_display);
                }));
                
            }
            
        }
        public void OnImageChanged(string imagepath, string annotationpath)
        {
            //throw new NotImplementedException();
            var InspectionContext = new InspectionContext(null,wd.CurrentImage, 1, 1,"");
            ocrnode.RunInside(wd.CurrentImage, wd.CurrentImage, InspectionContext);
            wd.window_display.HalconWindow.ClearWindow();
            InspectionContext.inspection_result.Display(wd.window_display);
        }

        public void OnImageChanging(string imagepath, string annotationpath)
        {
            //throw new NotImplementedException();
        }
    }
    public class EditDisplayMode:INotifyPropertyChanged,DisplayMode
    {
        OCRWindow ocrwindow;
        bool ctrl_keydown = false;
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
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

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

        public ICommand copyAnnotation { get; set; }
        public ICommand pasteAnnotation { get; set; }
        public List<RegionMaker> CurrentRegionList { get; set; } = new List<RegionMaker>();
        HWindow Display
        {
            get { return ocrwindow.window_display.HalconWindow; }
        }
        
        public void Dispose()
        {
            //ocrwindow.window_display.HInitWindow -= Window_display_HInitWindow;
            ocrwindow.window_display.SetValue(BarManager.DXContextMenuProperty, null);
            foreach (var item in CurrentRegionList)
            {
                Display.DetachDrawingObjectFromWindow(item.Region.current_draw);
                item.Region.current_draw = null;
            }
            //remove input behavior
            ocrwindow.InputBindings.Remove(copyBinding);
            ocrwindow.InputBindings.Remove(pasteBinding);


            SaveAnnotation(ocrwindow.annotation_path);
            
        }
        KeyBinding copyBinding, pasteBinding;
        BarButtonItem newannotation_menu,group_menu;
        public EditDisplayMode(HSmartWindowControlWPF window_display, OCRWindow ocrwindow)
        {
            //window_display.HInitWindow += Window_display_HInitWindow;
            this.ocrwindow = ocrwindow;

            PopupMenu popupMenu = new PopupMenu();
            newannotation_menu = new BarButtonItem() { Content = "Add new Annotation" };
            newannotation_menu.ItemClick += MenuItem_Click;
            group_menu = new BarButtonItem() { Content = "Group" };
            group_menu.ItemClick += group_menu_Click;
            popupMenu.Items.Add(newannotation_menu);
            popupMenu.Items.Add(group_menu);
            ocrwindow.window_display.SetValue(BarManager.DXContextMenuProperty, popupMenu);
            
            ocrwindow.window_display.HMouseMove += window_display_HMouseMove;
            ocrwindow.window_display.HMouseWheel += window_display_HMouseWheel;
            ocrwindow.window_display.HMouseDown += window_display_HMouseDown;
            ocrwindow.window_display.KeyDown += window_display_KeyDown;
            ocrwindow.lst_class.SelectionChanged += Lst_class_SelectionChanged;
            copyAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                copied = new RegionMaker() { Annotation = SelectedMarker.Annotation, Region = SelectedMarker.Region };

            });
            pasteAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                if (copied != null)
                {
                    AddRegion(new RegionMaker() { Annotation = copied.Annotation, Region = new Rectange1(false) { row1 = copied.Region.row1 - 20, col1 = copied.Region.col1 + 20, row2 = copied.Region.row2 - 20, col2 = copied.Region.col2 + 20 } });
                }

            });
            copyBinding = new KeyBinding(copyAnnotation, Key.C, ModifierKeys.Control);
            pasteBinding = new KeyBinding(pasteAnnotation, Key.V, ModifierKeys.Control);
            ocrwindow.InputBindings.Add(copyBinding);
            ocrwindow.InputBindings.Add(pasteBinding);
            LoadAnnotation(ocrwindow.annotation_path);
        }

        private void Window_display_HInitWindow(object sender, EventArgs e)
        {
            LoadAnnotation(ocrwindow.annotation_path);
        }

        public void ChangeRegion()
        {
            if (Display == null)
                return;
            DispOverlay();
        }
        int row, col;
        
        private void AddRegion(RegionMaker region)
        {
            RegionMaker region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(Display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }
            ChangeRegion();
        }
        
        private void AddRegionNew(RegionMaker region)
        {
            //region.Region.Initial((int)row, (int)col);
            //region.Region.row2 = region.Region.
            RegionMaker region_add = region;
            CurrentRegionList.Add(region_add);

            region_add.Attach(Display, null, Update, Selected);

            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }

            //lst_region.SelectedItem = region_add;
            ChangeRegion();
        }

        RegionMaker copied;
        public void Update(RegionMaker sender, Region region)
        {
            //Selected_region = sender;
            //UpdateSelectBoxPosition();
            ChangeRegion();
            //DispOverlay();
        }
        
        public void Selected(RegionMaker sender, Region region)
        {
            //Selected_region = sender;          

            if(Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    //group selection
                    if (sender != null)
                    {
                        sender.IsSelected = !sender.IsSelected;
                    }
                }
                else
                {
                    sender.IsSelected = true;
                    //reset selection to single object
                    foreach (var item in CurrentRegionList)
                    {
                        if (item == sender)
                        {

                        }
                        else
                        {
                            item.IsSelected = false;
                        }

                    }
                }
            }
            else
            {
                if (sender != null)
                {
                    //right click on a selected rect will not reset group selection
                    if (sender.IsSelected)
                    {

                    }
                    else
                    {
                        //reset selection to single object
                        foreach (var item in CurrentRegionList)
                        {
                            if (item == sender)
                            {

                            }
                            else
                            {
                                item.IsSelected = false;
                            }

                        }
                    }
                }

                
            }
                
                
                    
            //DispOverlay();
            SelectedMarker = sender;
            //SelectedMarker.IsSelected = true;
            ChangeRegion();
            UpdateSelectBoxPosition();
            //update box label
            if (SelectedMarker != null)
            {
                ocrwindow.cmb_select_class.SelectedItem = SelectedMarker.Annotation;
            }
        }
        void DrawGroupBoundingBox()
        {
            int padding = 5;
            foreach(var item in Groups)
            {
                var charlist = item.Items;
                if (charlist.Count > 0)
                {
                    var row1 = charlist.Min(x => x.Region.row1) - padding;
                    var row2 = charlist.Max(x => x.Region.row2) + padding;
                    var col1 = charlist.Min(x => x.Region.col1) - padding;
                    var col2 = charlist.Max(x => x.Region.col2) + padding;
                    Display.SetDraw("margin");
                    Display.SetColor(OCRWindow.AddOpacity("#0000ffff", 100 / 100));
                    Display.DispRectangle1(row1, col1, row2, col2);
                    Display.SetDraw("fill");
                    Display.SetColor(OCRWindow.AddOpacity("#0000ffff", ocrwindow.ColorOpacity / 100));
                    Display.DispRectangle1(row1, col1, row2, col2);
                }
                
            }
        }
        public void DispOverlay()
        {
            if (Display == null)
                return;
            Display.ClearWindow();
            Display.SetDraw("fill");
            //display.SetColor("#00ff0025");
            foreach (var item in CurrentRegionList)
            {
                //display.SetColor(item.Color);
                if (item.Annotation != null)
                {
                    Display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, ocrwindow.ColorOpacity / 100));
                    Display.DispObj(item.Region.region);
                    Display.DispText(item.Annotation.Name, "image", item.Region.row1, item.Region.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
                }

            }
            DrawGroupBoundingBox();

        }
        public void UpdateSelectBoxPosition()
        {
            //if (SelectedMarker != null)
            //{
            //    double winposx, winposy;
            //    //Display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row1, SelectedMarker.Region.col1, out winposx, out winposy);
            //    //ocrwindow.cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            //}

        }
        public void ClearAnnotation()
        {
            foreach (var item in CurrentRegionList)
            {
                item.Region.ClearDrawingData(Display);
            }
            CurrentRegionList.Clear();
            Groups.Clear();
        }
       

        public void SaveAnnotation(string path)
        {
            if (path != null)
            {
                JObject allData = new JObject();
                JObject[] data = new JObject[CurrentRegionList.Count];
                for (int i = 0; i < CurrentRegionList.Count; i++)
                {
                    data[i] = new JObject();
                    data[i]["x"] = CurrentRegionList[i].Region.col1;
                    data[i]["y"] = CurrentRegionList[i].Region.row1;
                    data[i]["w"] = CurrentRegionList[i].Region.col2 - CurrentRegionList[i].Region.col1;
                    data[i]["h"] = CurrentRegionList[i].Region.row2 - CurrentRegionList[i].Region.row1;
                    data[i]["annotation"] = CurrentRegionList[i].Annotation.Name;
                    data[i]["groupId"] = CurrentRegionList[i].GroupId;
                }
                allData["characters"] = JArray.FromObject(data);


                //save group data
                JObject[] groupData = new JObject[Groups.Count];
                for(int i = 0; i < Groups.Count; i++)
                {
                    var sorted_group = Groups[i].Items.OrderBy(x => x.Region.col1);
                    JArray chars = new JArray();
                    foreach(var character in sorted_group)
                    {
                        var addchar = new JObject();
                        addchar["x"] = character.Region.col1;
                        addchar["y"] = character.Region.row1;
                        addchar["w"] = character.Region.col2 - character.Region.col1;
                        addchar["h"] = character.Region.row2 - character.Region.row1;
                        chars.Add(addchar);
                    }
                    
                    groupData[i] = new JObject();
                    groupData[i]["box"] = chars;
                    groupData[i]["groupId"] = Groups[i].GroupId;
                }
                allData["groups"] = JArray.FromObject(groupData);
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(allData));
            }

        }
        public void LoadAnnotation(string path)
        {
            CurrentRegionList.Clear();
            Groups.Clear();
            if (path != null)
            {
                var datatxt = System.IO.File.ReadAllText(path);
                var dataread = JsonConvert.DeserializeObject<JObject>(datatxt);
                var data = dataread["characters"];
                foreach (var item in data)
                {
                    var annotation = item["annotation"].ToString();
                    double x = item["x"].ToObject<double>();
                    double y = item["y"].ToObject<double>();
                    double w = item["w"].ToObject<double>();
                    double h = item["h"].ToObject<double>();
                    int id = item["groupId"].ToObject<int>();
                    var color = ocrwindow.ClassList.FirstOrDefault(x1 => x1.Name == annotation);
                    if (color != null)
                    {
                        AddRegion(new RegionMaker() {GroupId=id, Region = new Rectange1(false) {row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = color });
                    }
                    else
                    {
                        AddRegion(new RegionMaker() {GroupId=id, Region = new Rectange1(false) { row1 = y, col1 = x, row2 = y + h, col2 = x + w }, Annotation = new ClassifierClass1() { Name = annotation, Color = "#00ff00ff" } }); ;
                    }

                }
                var groupdata = dataread["groups"];
                foreach(var item in groupdata)
                {
                    var groupId = item["groupId"].ToObject<int>();
                    var addedGroup = new CharacterGroup() { GroupId = groupId };
                    var groupItems = CurrentRegionList.Where(x => x.GroupId == groupId);
                    addedGroup.Items.AddRange(groupItems);
                    Groups.Add(addedGroup);

                }
            }
            
        }
        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedMarker != null)
            {
                SelectedMarker.Annotation = ocrwindow.lst_class.SelectedItem as ClassifierClass1;
                ocrwindow.cmb_select_class.SelectedItem = ocrwindow.lst_class.SelectedItem as ClassifierClass1;
                DispOverlay();
            }
        }
        public void OnImageChanging(string imagepath, string annotation_path)
        {
            SaveAnnotation(annotation_path);
        }
       
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            //window_display.Focus();
            if (e.Button == MouseButton.Right)
            {
                row = (int)e.Row;
                col = (int)e.Column;
            }
            Keyboard.Focus(ocrwindow.window_display);
        }
        public List<CharacterGroup> Groups = new List<CharacterGroup>();
        private void group_menu_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            int newGroupId = -1;
            for(int i = 0; i < 100; i++)
            {
                if (Groups.FirstOrDefault(x => x.GroupId == i) == null)
                {
                    newGroupId = i;
                    break;
                }
            }
            if (newGroupId != -1)
            {
                var newgroup = new CharacterGroup() { GroupId =newGroupId };
                foreach (var item in CurrentRegionList)
                {
                    if (item.IsSelected)
                    {
                        //remove char from an existing group
                        foreach(var item2  in Groups)
                        {
                            if (item2.Items.Contains(item))
                            {
                                item2.Items.Remove(item);
                            }
                        }
                        //assign char to new group
                        newgroup.Items.Add(item);
                        item.GroupId = newgroup.GroupId;

                    }
                }
                


                Groups.Add(newgroup);
            }
            
        }
        private void MenuItem_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (ocrwindow.SelectedClass != null)
            {
                //calculate optimal rec size base on current zoom 
                var width = ocrwindow.window_display.HImagePart.Width / 4;
                var height = ocrwindow.window_display.HImagePart.Height / 4;
                AddRegionNew(new RegionMaker() { Annotation = ocrwindow.SelectedClass, Region = new Rectange1(false) { row1 = row, col1 = col, row2 = row + height, col2 = col + width } });
            }
            else
            {
                if (ocrwindow.ClassList.Count > 0)
                {
                    var width = ocrwindow.window_display.HImagePart.Width / 4;
                    var height = ocrwindow.window_display.HImagePart.Height / 4;
                    ocrwindow.SelectedClass = ocrwindow.ClassList[0];
                    AddRegionNew(new RegionMaker() { Annotation = ocrwindow.SelectedClass, Region = new Rectange1(false) { row1 = row, col1 = col, row2 = row + height, col2 = col + width } });
                }
            }
        }
        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrl_keydown = true;
                return;
            }

            if (ctrl_keydown)
            {
                if (e.Key == Key.C)
                {
                    ctrl_keydown = true;
                    return;
                }

                ctrl_keydown = false;
                return;
            }

            if (e.Key == Key.Delete)
            {
                if (SelectedMarker != null)
                {
                    SelectedMarker.Region.ClearDrawingData(Display);
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
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                if (SelectedMarker != null)
                {
                    ClassifierClass1 item = ocrwindow.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == e.Key.ToString().ToLower()).First();
                    SelectedMarker.Annotation = item;
                    ocrwindow.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                if (SelectedMarker != null)
                {
                    ClassifierClass1 item = ocrwindow.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == ((int)e.Key - 34).ToString()).First();
                    SelectedMarker.Annotation = item;
                    ocrwindow.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                if (SelectedMarker != null)
                {
                    ClassifierClass1 item = ocrwindow.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == ((int)e.Key - 74).ToString()).First();
                    SelectedMarker.Annotation = item;
                    ocrwindow.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
        }

        public void OnImageChanged(string imagepath, string annotation_path)
        {
            ClearAnnotation();
            if (System.IO.File.Exists(annotation_path))
            {
                try
                {
                    LoadAnnotation(annotation_path);
                }
                catch (Exception ex)
                {

                }
                UpdateSelectBoxPosition();

            }
            else
            {

            }
        }
    }
}
