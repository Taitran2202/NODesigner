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
    public partial class TextRecognitionWindow : ThemedWindow,INotifyPropertyChanged
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
        public TextPosition DisplayPosition
        {
            get { return _DisplayPosition; }
            set
            {
                if (_DisplayPosition != value)
                {
                    _DisplayPosition = value;
                    RaisePropertyChanged("DisplayPosition");
                }
            }
        }
        public TextPosition _DisplayPosition  = TextPosition.Right;



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

        private WordMarker _selectedMarker;
        public WordMarker SelectedMarker
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

        private ObservableCollection<WordMarker> _current_region_list= new ObservableCollection<WordMarker>();
        public ObservableCollection<WordMarker> CurrentRegionList
        {
            get
            {
                return _current_region_list;
            }
            set
            {
                if (_current_region_list != value)
                {
                    _current_region_list = value;
                    RaisePropertyChanged("CurrentRegionList");
                }
            }
        }


        public class LossValue
        {
            public double step { get; set; }
            public double loss { get; set; }

        }

        private string ImageDir, AnnotationDir,  annotationPath;
        private int row, col;
        private double mpx, mpy;
        private HWindow display;
        private HImage image;
        TextRecognitionNode node;
        Python.TrainPPOCR train;
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
                train = new Python.TrainPPOCR();
                node.TrainConfig.Save();
                trainningStatusViewModel.StartListen();
                train.TrainPython(node.TrainConfig.ConfigDir,node.TrainConfig.ModelName,  (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsTraining = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this,"Training was cancel because of error!", "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });
                        
                    }
                    if (trainargs.State == TrainState.OnGoing)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            trainningStatusViewModel.Logs.Add(trainargs.Log);
                            if (log_box.VerticalOffset == log_box.ScrollableHeight)
                            { log_box.ScrollToEnd(); }
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsTraining = false;
                        node.Runtime.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this,"Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
            }));
        }
        public TextRecognitionWindow(TextRecognitionNode node)
        {
            this.node = node;
            InitializeComponent();
            propertiesGrid.SelectedObject = node.Runtime;
            this.DataContext = this;
            this.stack_train_parameter.DataContext = node.TrainConfig;
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
                            var imageadded = new ImageSet(newfile + ".bmp") { DateTime = DateTime.Now };
                            ListImage.Insert(0, imageadded);
                            lst_view.SelectedItem = imageadded;
                            lst_view.ScrollIntoView(imageadded);
                            if (node.BoxOutput.Value != null)
                            {
                                var boxes = node.BoxOutput.Value.Data;
                                var texts = node.TextOutput.Value.Data;
                                for (int i = 0; i < boxes.Length; i++)
                                {
                                    AddRegion(new WordMarker()
                                    {
                                        Word = texts[i],
                                        Region = new Rectange2(false)
                                        {
                                            row = boxes[i].row,
                                            col = boxes[i].col,
                                            length1 = boxes[i].length1,
                                            length2 = boxes[i].length2,
                                            phi = boxes[i].phi
                                        }
                                    });
                                }

                            }
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
            copyAnnotation = new RelayCommand<object>((p) => { return true; }, (p) =>
            {

                copied = new WordMarker() { Word = SelectedMarker.Word, Region = SelectedMarker.Region };

            });
            pasteAnnotation = new RelayCommand<object>((p) => { return true; }, (p) =>
            {

                if (copied != null)
                {
                    AddRegion(new WordMarker()
                    {
                        Word = copied.Word,
                        Region = new Rectange2(false)
                        {
                            row = copied.Region.row - 20,
                            col = copied.Region.col + 20,
                            length1 = copied.Region.length1,
                            length2 = copied.Region.length2
                        }
                    });
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


            var Properties = new ObservableCollection<Property>();

            grid_model_prop.SelectedObject = node.TrainConfig;
            //var Properties = new ObservableCollection<Property>();
            Properties.Add(new Property() { Name = "TrainType" });
            Properties.Add(new Property() { Name = "ExtendDataDir" });
            //Properties.Add(new Property() { Name = "BATCH_SIZE" });
            grid_model_prop.PropertyDefinitionsSource = Properties;

        }
        WordMarker copied;
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
                display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row, 
                    SelectedMarker.Region.col, 
                    out winposx, out winposy);
                //cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }
        }

        public void Update(WordMarker sender, Region region)
        {
            
            ChangeRegion();
            DispOverlay();
            //SaveResult();
        }
        public void Selected(WordMarker sender, Region region)
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
                if (item.Word != null)
                {
                    display.SetColor(AddOpacity("#ffffffff", ColorOpacity / 100));
                    display.DispObj(item.Region.region);
                   (int row,int col)= Functions.GetDisplayPosition(DisplayPosition, item.Region.row, item.Region.col,
                        item.Region.phi, item.Region.length1, item.Region.length2
                        );
                    display.DispText(item.Word, "image", 
                        row, 
                        col, "black", 
                        new HTuple("box_color"), 
                        new HTuple("#ffffffff"));
                }
               
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            DispOverlay();
        }

        private void AddRegion(WordMarker region)
        {
            WordMarker region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }
            ChangeRegion();
        }
        private void AddRegionNew(WordMarker region)
        {
            region.Region.Initial((int)row, (int)col);
            WordMarker region_add = region;
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
            string imgTrainPath = System.IO.Path.Combine(ImageDir, ImageNameWithExtension);
            for (int i = 0; i < data.Length; i++)
            {
                
                data[i] = new JObject();
                data[i]["row"] = CurrentRegionList[i].Region.row;
                data[i]["col"] = CurrentRegionList[i].Region.col;
                data[i]["phi"] = CurrentRegionList[i].Region.phi;
                data[i]["length1"] = CurrentRegionList[i].Region.length1;
                data[i]["length2"] = CurrentRegionList[i].Region.length2;
               
                data[i]["word"] = CurrentRegionList[i].Word;

            }
            if (path != null)
            {
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data,Formatting.Indented));
            }

        }
        public void LoadAnnotation(string path)
        {
            var datatxt = System.IO.File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
            foreach (var item in data)
            {
                var word = item["word"].ToString();
                double row = item["row"].ToObject<double>();
                double col = item["col"].ToObject<double>();
                double phi = item["phi"].ToObject<double>();
                double length1 = item["length1"].ToObject<double>();
                double length2 = item["length2"].ToObject<double>();

                AddRegion(new WordMarker() 
                { 
                    Region = new Rectange2(false) 
                    { 
                        row = row, col = col, phi = phi, length1 = length1,length2=length2
                    }, 
                    Word = word
                }); 
                

            }
        }
        
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                
                //cmb_select_class.Visibility = Visibility.Hidden;
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
                    //image.GetImageSize(out imgW, out imgH);
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
                    //cmb_select_class.Visibility = Visibility.Visible;
                }
                lst_class.ItemsSource = CurrentRegionList;
            }
            else
            {
                lst_class.ItemsSource = null;
            }
        }

        public void ClearAnnotation()
        {
            if (CurrentRegionList != null)
            {
                foreach (var item in CurrentRegionList)
                {
                    item.Region.ClearDrawingData(display);
                }
                CurrentRegionList.Clear();
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
        string RandomHalconColor()
        {
            const string chars = "abcdef0123456789";
            return "#"+new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray())+"ff";
        }
        private void Btn_add_class_Click(object sender, RoutedEventArgs e)
        {
            //ClassList.Add(new YOLOClass() { Color = RandomHalconColor(), Name = "unknown" });
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
            
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as YOLOClass;
            if (selected != null)
            {
                
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

            AddRegionNew(new WordMarker() 
            { 
                Word = "", 
                Region = new Rectange2(false) { row = mpy, col = mpx, phi =  0, length1 =  50,length2=50 } 
            });
            

        }

        private void Btn_add_augmentation_Click(object sender, RoutedEventArgs e)
        {
            

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
                NOVisionPython.TrainConsole("ocr",node.TrainConfig.ModelName, node.TrainConfig.ConfigDir);
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

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            SaveResult();
        }

        private void btn_new_annotation_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new WordMarker()
            {
                Word = "",
                Region = new Rectange2(false) { row = mpy, col = mpx, phi = 0, length1 = 50, length2 = 50 }
            });
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectBoxPosition();
            }
        }

        private void grid_model_prop_Loaded(object sender, RoutedEventArgs e)
        {
            
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
