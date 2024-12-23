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

    public partial class ObjectDetectionEditorWindow : Window, INotifyPropertyChanged
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

        bool _is_finished;
        public bool isFinished
        {
            get
            {
                return _is_finished;
            }
            set
            {
                if (_is_finished != value)
                {
                    _is_finished = value;
                    RaisePropertyChanged("isFinished");
                }
            }
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string imagedir, annotationdir, modeldir, datasetdir;
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
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png")|x.ToLower().EndsWith(".jpeg"));
                foreach (string file in files)
                {
                    result.Add(new ImageFilmstrip(file));
                }

            }
            ListImage = result;
        }

        public ObservableCollection<ClassifierClass1> ClassList { get; set; }
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
                cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
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
        public ObjectDetectionEditorWindow(string datasetdir, string imagedir, string annotationdir, string modeldir, ObservableCollection<ClassifierClass1> ClassList, Action<Action<int>, Action<bool>> TrainFunction,  List<Augmentation> listAugmentation)
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


            this.ClassList = ClassList;
            cmb_select_class.ItemsSource = this.ClassList;
            lst_class.ItemsSource = this.ClassList;
            this.imagedir = imagedir;
            this.modeldir = modeldir;
            this.annotationdir = annotationdir;

            this.datasetdir = datasetdir;

            btn_add_image.Click += Btn_add_image_Click;
            btn_add_image_camera.Click += (s, e) =>
            {
                //if (classifier != null)
                //{

                //    var imageFeed = classifier.ImageInput;
                //    HRegion regionInput = null;
                //    //if (classifier.RegionInput.Value.SourceLink != null)
                //    //{
                //    if (classifier.Regions.Value != null)
                //    {
                //        regionInput = classifier.Regions.Value.Region.Clone();
                //    }
                //    //}
                //    else
                //    {
                //        //if (classifier.InteractiveRegion)
                //        //{
                //        //  regionInput = classifier.RegionNew.Clone();
                //        //}
                //        //else
                //        //{
                //        regionInput = classifier.Region.Region.Clone();
                //        //}

                //    }


                //    if (imageFeed != null)
                //        if (imageFeed.Value != null)
                //        {
                //            try
                //            {
                //                var image = imageFeed.Value.Clone();
                //                var filename = DateTime.Now.Ticks.ToString();
                //                var newfile = System.IO.Path.Combine(imagedir, filename);
                //                image.WriteImage("bmp", 0, newfile);
                //                var imageadded = new ImageFilmstrip(newfile + ".bmp");
                //                ListImage.Add(imageadded);
                //                lst_view.SelectedItem = imageadded;
                //                if (regionInput != null)
                //                    try
                //                    {
                //                        var region = regionInput;
                //                        for (int i = 0; i < region.CountObj(); i++)
                //                        {
                //                            HTuple row1, col1, row2, col2;
                //                            region[i + 1].SmallestRectangle1(out row1, out col1, out row2, out col2);
                //                            ClassifierClass1 newclass = null;
                //                            if (ClassList.Count > 0)
                //                            {
                //                                newclass = ClassList[0];
                //                            }
                //                            if (newclass != null)
                //                            {
                //                                var region_add = new RegionMaker() { Annotation = newclass, Region = new Rectange1(false) { row1 = row1, col1 = col1, row2 = row2, col2 = col2 } };

                //                                CurrentRegionList.Add(region_add);

                //                                region_add.Attach(display, null, Update, Selected);


                //                            }
                //                            else
                //                            {
                //                                var region_add = new RegionMaker() { Annotation = new ClassifierClass1() { Name = "unknow" }, Region = new Rectange1(false) { row1 = row1, col1 = col1, row2 = row2, col2 = col2 } };

                //                                CurrentRegionList.Add(region_add);

                //                                region_add.Attach(display, null, Update, Selected);
                //                            }
                //                        }
                //                        if (CurrentRegionList.Count > 0)
                //                        {
                //                            SelectedMarker = CurrentRegionList[0];
                //                            ChangeRegion();
                //                        }
                //                    }
                //                    catch (Exception ex)
                //                    {

                //                    }
                //            }
                //            catch (Exception ex)
                //            {

                //            }
                //        }
                //}
            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_step_ok.Click += (sender, e) =>
            {
                var step = spin_step.Value;
                box_step.Visibility = Visibility.Hidden;
                Loss_Values.Clear();
                Task.Run(new Action(() =>
                {
                    isFinished = false;
                    IsTrainning = true;
                    bool Retrained = false;
                    //objectDetection.ClearSession();
                    Python.TrainObjectDetection train = new Python.TrainObjectDetection();
                    string jsonListAugmentation = JsonConvert.SerializeObject(this.listAugmentation);

                    train.TrainPython((int)step, datasetdir, modeldir, ClassList.Select(x => x.Name).ToArray(), (progress, iscancel, isfinish,lossData) =>
                    {
                        if (iscancel | isfinish)
                        {
                            IsTrainning = false;

                        }
                        if (isfinish)
                        {
                            isFinished = true;
                            Retrained = true;
                            //objectDetection.LoadRecipie_ObjectDetection();
                            //objectDetection.SaveClassList(System.IO.Path.Combine(modeldir,"classListResult.txt"));
                            //objectDetection.LoadClassListResult(System.IO.Path.Combine(modeldir, "classListResult.txt"));
                           

                        }
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (progress);
                            this.loss.Text = lossData.ToString();
                            Loss_Values.Add(new LossValue { step=progress, loss=lossData});
                        }));
                    });
                  
                }));
             
            };
            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            LoadImageList(imagedir);
            this.DataContext = this;
        }
        public ObservableCollection<LossValue> Loss_Values { get; } = new ObservableCollection<LossValue>();
        public class LossValue
        {
            public double step { get; set; }
            public double loss { get; set; }

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
        string annotation_path;
        public void SaveResult()
        {
            SaveAnnotation(annotation_path);
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

        int im_w, im_h;
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
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
                        image.GetImageSize(out im_w, out im_h);
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
                        annotation_path = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileName(selected.FullPath) + ".txt");
                        if (System.IO.File.Exists(annotation_path))
                        {
                            try
                            {
                                LoadAnnotation(annotation_path);
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
        public void ClearAnnotation()
        {
            foreach (var item in CurrentRegionList)
            {
                item.Region.ClearDrawingData(display);
            }
            CurrentRegionList.Clear();
        }

        public void SaveAnnotation(string path)
        {
            JObject[] data = new JObject[CurrentRegionList.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new JObject();
                data[i]["x"] = CurrentRegionList[i].Region.col1;
                data[i]["y"] = CurrentRegionList[i].Region.row1;
                data[i]["w"] = CurrentRegionList[i].Region.col2 - CurrentRegionList[i].Region.col1;
                data[i]["h"] = CurrentRegionList[i].Region.row2 - CurrentRegionList[i].Region.row1;
                data[i]["annotation"] = CurrentRegionList[i].Annotation.Name;
            }
            if (path != null)
            {
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data));
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
        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

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
        HImage image;
        HHomMat2D transform;
        State current_state = State.Pan;
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
                        annotation_path = null;
                    }
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
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        private void Btn_add_augmentation_Click(object sender, RoutedEventArgs e)
        {
            AugmentationEditorWindow augmentationWd = new AugmentationEditorWindow(imagedir, annotationdir, ClassList);
            augmentationWd.ShowDialog();
            LoadImageList(imagedir);

            var tempList = augmentationWd.listAugmentationcs;
            foreach (var item in tempList)
            {
                list_augmentation.Items.Add(item);
                listAugmentation.Add(item);
            }

        }

        private void btn_remove_chart_Click(object sender, RoutedEventArgs e)
        {
            this.chart_loss.Visibility = Visibility.Hidden;
        }

        private void btn_loss_Click(object sender, RoutedEventArgs e)
        {
            this.chart_loss.Visibility = Visibility.Visible;
            this.chart.DataSource = Loss_Values;
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
                LoadImageList(imagedir);
            }
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


}
