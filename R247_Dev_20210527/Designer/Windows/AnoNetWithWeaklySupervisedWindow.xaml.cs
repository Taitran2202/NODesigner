using HalconDotNet;
using Microsoft.Win32;
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

namespace NOVisionDesigner.Designer.Windows
{

    public partial class AnoNetWithWeaklySupervisedWindow : Window, INotifyPropertyChanged
    {

        private bool _is_visualize;

        public bool IsVisualize
        {
            get
            {
                return _is_visualize;
            }
            set
            {
                if (_is_visualize != value)
                {
                    _is_visualize = value;
                    RaisePropertyChanged("IsVisualize");
                }
            }
        }

        string _color_mode = "Color";
        public string ColorMode
        {
            get
            {
                return _color_mode;
            }
            set
            {
                if (_color_mode != value)
                {
                    _color_mode = value;
                    RaisePropertyChanged("ColorMode");
                }
            }
        }

        string _precision = "High";
        public string Precision
        {
            get
            {
                return _precision;
            }
            set
            {
                if (_precision != value)
                {
                    _precision = value;
                    RaisePropertyChanged("Precision");
                }
            }
        }

        string _modelOptions = "SExp1";
        public string ModelOptions
        {
            get
            {
                return _modelOptions;
            }
            set
            {
                if (_modelOptions != value)
                {
                    _modelOptions = value;
                    RaisePropertyChanged("ModelOptions");
                }
            }
        }

        bool _is_training;
        public bool IsTraining
        {
            get
            {
                return _is_training;
            }
            set
            {
                if (_is_training != value)
                {
                    _is_training = value;
                    RaisePropertyChanged("IsTraining");
                }
            }
        }

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

        ObservableCollection<ImageFilmstrip> _list_bad_images;
        public ObservableCollection<ImageFilmstrip> ListBadImage
        {
            get
            {
                return _list_bad_images;
            }
            set
            {
                if (_list_bad_images != value)
                {
                    _list_bad_images = value;
                    RaisePropertyChanged("ListBadImage");
                }
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        string imagedir, annotationdir, datadir;
        public bool Retrained { get; set; } = false;

        ObservableCollection<ImageFilmstrip> LoadImageList(string dir)
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
            return result;
        }

        AnoNetWithWeaklySupervisedNode anoNetWithWeaklySupervisedNode;
        Python.TrainAnoNet trainAnoNet;

        public AnoNetWithWeaklySupervisedWindow(string imageDir, string annotaionDir, string cameraDir, AnoNetWithWeaklySupervisedNode node, Action<Action<int>, Action<bool>, Action<bool>> TrainFunction)
        {
            InitializeComponent();
            this.imagedir = imageDir;
            this.annotationdir = annotaionDir;
            anoNetWithWeaklySupervisedNode = node;

            btn_add_image_camera.Click += (s, e) =>
            {
                var data = node.Image.Value;
                if (data != null)
                {
                    try
                    {
                        var image = data.Clone();
                        var filename = DateTime.Now.Ticks.ToString();
                        var newfile = System.IO.Path.Combine(cameraDir, filename);
                        image.WriteImage("bmp", 0, newfile);
                        //ListImage.Add(new ImageFilmstrip(newfile + ".bmp"));  

                    }
                    catch (Exception ex)
                    {

                    }
                }
            };

            btn_add_bad_image_camera.Click += (s, e) =>
            {

            };

            btn_solid.Checked += (s, e) =>
            {
                current_state = State.Pan;
                window_display.HMoveContent = true;
            };
            btn_gradient.Checked += (s, e) =>
            {
                current_state = State.GradientBrush;
                window_display.HMoveContent = false;
            };
            btn_eraser.Checked += (s, e) =>
            {
                current_state = State.Eraser;
                window_display.HMoveContent = false;
            };
            rad_move.Checked += (s, e) =>
            {
                current_state = State.Move;
                window_display.HMoveContent = true;
            };

            btn_add_image.Click += Btn_add_image_Click;
            btn_add_bad_image.Click += Btn_add_bad_image_onClick;

            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };
            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
                cbModelOpions.SelectedIndex = 0;

            };
            btn_step_cancel.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };

            btn_step_ok.Click += (sender, e) =>
            {

                var epochsForSEG = spin_epoch_seg.Value;
                var epochsForCLS = spin_epoch_cls.Value;
                node.anoNetModel.TrainConfig.EpochsForSeg = (int)epochsForSEG;
                node.anoNetModel.TrainConfig.EpochsForCls = (int)epochsForCLS;

                //var epochs = spin_epoch.Value;
                //node.anoNetModel.TrainConfig.Epochs = (int)epochs;

                var width = spin_width.Value;
                var heigth = spin_height.Value;
                var channels = spin_channels.Value;
                node.anoNetModel.TrainConfig.ImageWidth = (int)width;
                node.anoNetModel.TrainConfig.ImageHeight = (int)heigth;
                node.anoNetModel.TrainConfig.Channels = (int)channels;

                var modelOptions = ModelOptions;
                node.anoNetModel.TrainConfig.ModelOpitions = modelOptions;

                var batchSize = spin_batch_size.Value;
                node.anoNetModel.TrainConfig.BatchSize = (int)batchSize;

                node.anoNetModel.TrainConfig.Save(node.anoNetModel.TrainConfigPath);

                box_step.Visibility = Visibility.Hidden;
                Task.Run(new Action(() =>
                {
                    IsTraining = true;
                    if (node.anoNetModel.TrainConfig.Monitor)
                    {
                        IsVisualize = true;
                    }
                    else
                    {
                        IsVisualize = false;
                    }
                    VisualizeTrainning();
                    trainAnoNet = new Python.TrainAnoNet();
                    //node.anoNetModel.ClearSession();

                    trainAnoNet.TrainPython(node.anoNetModel.TrainConfigPath, (progress, iscancel, isfinish) =>
                    {
                        if (iscancel | isfinish)
                        {
                            IsTraining = false;
                            
                        }

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progress.Value = (progress);
                        }));
                    });
                    
                }));
            };
            string goodDir = System.IO.Path.Combine(imagedir, "good");
            string badDir = System.IO.Path.Combine(imagedir, "bad");
            ListImage = LoadImageList(goodDir);
            ListBadImage = LoadImageList(badDir);
            this.DataContext = this;
        }

        public void VisualizeTrainning()
        {
            try
            {
                if (anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Monitor)
                {
                    if (fileSystemWatcher == null)
                    {
                        d = new DirectoryInfo(anoNetWithWeaklySupervisedNode.anoNetModel.ResultDir);
                        FileInfo[] files = d.GetFiles("*.jpg");
                        if (files.Length > 0)
                        {
                            foreach (var file in files)
                            {
                                file.Delete();
                            }
                        }
                        folderToWatchFor = anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.ResultDir;
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
                    mask_img.Source = null;
                    predict_img.Source = null;
                }
            }
            catch (Exception)
            {

                
            }
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
                        var filename = System.IO.Path.GetFileName(file);
                        var newfile = System.IO.Path.Combine(imagedir, "good", filename);
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageFilmstrip(newfile));

                        // create mask image
                        HImage mask = new HImage(newfile);
                        int w, h;
                        mask.GetImageSize(out w, out h);
                        HImage mask1 = new HImage("byte", w, h);
                        
                        string filePath = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileNameWithoutExtension(newfile));
                        mask1.OverpaintRegion(new HRegion(0.0, 0.0, h, w), 0.0, "fill");
                        mask1.WriteImage("png", 0, filePath);
                        
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
        
        private void Btn_add_bad_image_onClick(object sender, RoutedEventArgs e)
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
                        var filename = System.IO.Path.GetFileName(file);
                        var newfile = System.IO.Path.Combine(imagedir, "bad", filename);
                        System.IO.File.Copy(file, newfile);
                        ListBadImage.Add(new ImageFilmstrip(newfile));
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
        State last_state = State.Pan;
        Cursor last_cursor = null;
        bool last_move_state = false;
        HImage image_gt;
        string image_gt_path;

        public void SaveResult()
        {
            image_gt?.WriteImage("png", 0, image_gt_path);
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
            if (image == null)
                return;
            display.ClearWindow();
            var region = image.Threshold(1, 255.0);
            display.SetColor(AddOpacity(SelectClass.Color, ColorOpacity / 100));
            display.SetDraw("fill");
            display.DispObj(region);
            display.SetColor(SelectClass.Color);
            display.SetDraw("margin");
            display.SetLineWidth(2);
            display.DispObj(region);
            //DispOverlay();
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
            if (e.AddedItems.Count < 1)
            {
                return;
            }
            var selected = e.AddedItems[0] as ImageFilmstrip;
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
                    image.GetImageSize(out im_w, out im_h);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
                try
                {
                    ImageName = System.IO.Path.GetFileNameWithoutExtension(selected.FullPath);
                    image_gt_path = System.IO.Path.Combine(annotationdir,"good",System.IO.Path.GetFileNameWithoutExtension(selected.FullPath));
                    if (System.IO.File.Exists(image_gt_path + ".png"))
                    {
                        try
                        {
                            image_gt = new HImage(image_gt_path);
                            DisplayImageGt(image_gt);
                        }
                        catch (Exception ex)
                        {
                            image_gt = new HImage("byte", im_w, im_h);
                        }

                    }
                    else
                    {
                        image_gt = new HImage("byte", im_w, im_h);
                    }

                }
                catch (Exception ex)
                {

                }
            }
        }

        private void lst_bad_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0)
            {
                return;
            }
            var selected = e.AddedItems[0] as ImageFilmstrip;
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
                    image.GetImageSize(out im_w, out im_h);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
                try
                {
                    ImageName = System.IO.Path.GetFileNameWithoutExtension(selected.FullPath);
                    image_gt_path = System.IO.Path.Combine(annotationdir,"bad",System.IO.Path.GetFileNameWithoutExtension(selected.FullPath));
                    if (System.IO.File.Exists(image_gt_path + ".png"))
                    {
                        try
                        {
                            image_gt = new HImage(image_gt_path);
                            DisplayImageGt(image_gt);
                        }
                        catch (Exception ex)
                        {
                            image_gt = new HImage("byte", im_w, im_h);
                        }

                    }
                    else
                    {
                        image_gt = new HImage("byte", im_w, im_h);
                    }

                }
                catch (Exception ex)
                {

                }
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
            DisplayImageGt(image_gt);
        }

        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
            {
                //window_display.HMouseUp += window_display_HMouseUp;
                //window_display.HMouseUp -= Window_display_HMouseUp_pan;
                enter_state = true;
                HRegion region_gen = new HRegion();
                switch (current_state)
                {
                    case State.Pan: break;
                    case State.SolidBrush: break;
                    case State.GradientBrush:

                        enter_state = false;
                        // region_undo.Push(region_base);
                        region_gen.GenCircle(e.Row, e.Column, BrushSize);
                        if (SelectClass != null)
                            image_gt?.OverpaintRegion(region_gen, new HTuple(SelectClass.ClassID), new HTuple("fill"));

                        //  region_base = region_base.Union2(region_gen);

                        DisplayImageGt(image_gt);
                        // display.DispObj(region_gen);

                        break;
                    case State.Eraser:


                        // region_undo.Push(region_base);
                        region_gen.GenCircle(e.Row, e.Column, BrushSize);
                        image_gt.OverpaintRegion(region_gen, new HTuple(0), new HTuple("fill"));
                        //region_base = region_base.Difference(region_gen);
                        DisplayImageGt(image_gt);


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
                switch (current_state)
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
                            image_gt.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(SelectClass.ClassID), "fill");
                            //region_base = region_base.Union2(region_gen.DilationCircle((double)BrushSize));
                            //  display.ClearWindow();
                            DisplayImageGt(image_gt);

                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            image_gt.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(SelectClass.ClassID), "fill");
                            // region_base = region_base.Union2(region_gen.DilationCircle((double)BrushSize));
                            //display.ClearWindow();
                            DisplayImageGt(image_gt);


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
                            image_gt.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(0), "fill");
                            //region_base = region_base.Difference(region_gen.DilationCircle((double)BrushSize));
                            //  display.ClearWindow();
                            DisplayImageGt(image_gt);
                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            image_gt.OverpaintRegion(region_gen.DilationCircle((double)BrushSize), new HTuple(0), "fill");
                            DisplayImageGt(image_gt);
                        }

                        HRegion region_erase = new HRegion();
                        region_erase.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_erase);
                        break;
                }
            else
            {
                switch (current_state)
                {
                    case State.Move: break;
                    case State.Pan:
                        break;
                    default:



                        //display.ClearWindow();
                        DisplayImageGt(image_gt);
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

        private void btn_remove_bad_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as ImageFilmstrip;
            if (vm != null)
            {
                try
                {
                    System.IO.File.Delete(vm.FullPath);
                    ListBadImage.Remove(vm);

                    var imagegtpath = System.IO.Path.Combine(annotationdir, System.IO.Path.GetFileNameWithoutExtension(vm.FullPath));
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

        double _scale = 1;
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

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }


        private void HandleChecked(object sender, RoutedEventArgs e) 
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "cbTrainSeg")
            {
                cbTrainCls.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.TrainSeg = true;
            }
            else if (cb.Name == "cbTrainCls") 
            {

                cbTrainSeg.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.TrainSeg = false;
            }
            else if (cb.Name == "cbScratch")
            {
                cbResume.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Transfer = "scratch";
            }
            else if (cb.Name == "cbResume")
            {
                cbScratch.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Transfer = "resume";
            }
            else if (cb.Name == "cbAugment")
            {
                cbAugment.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Augmentation = true;
            }
            else if(cb.Name == "cbMonitor")
            {
                cbMonitor.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Monitor = true;
            }
        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Name == "cbTrainSeg")
            {
                cbTrainCls.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.TrainSeg = false;
            }
            else if (cb.Name == "cbTrainCls")
            {
                cbTrainSeg.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.TrainSeg = true;
            }
            else if (cb.Name == "cbScratch")
            {
                cbResume.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Transfer = "resume";
            }
            else if (cb.Name == "cbResume")
            {
                cbScratch.IsChecked = true;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Transfer = "scratch";
            }
            else if (cb.Name == "cbAugment")
            {
                cbAugment.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Augmentation = false;
            }
            else if (cb.Name == "cbMonitor")
            {
                cbMonitor.IsChecked = false;
                anoNetWithWeaklySupervisedNode.anoNetModel.TrainConfig.Monitor = false;
            }
        }

        private FileSystemWatcher fileSystemWatcher;
        private string folderToWatchFor = "";
        DirectoryInfo d;
        List<System.Windows.Threading.DispatcherOperation> dispatcherOperations = new List<System.Windows.Threading.DispatcherOperation>();

        bool DipatcherForFileChanged = false;
        ImageSource originalImage;
        ImageSource maskImage;
        ImageSource preditctImage;
        public static BitmapImage BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
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
                        foreach (FileInfo file in Files.Take(3))
                        {
                            if (file.Name.Contains("img_with_epoch"))
                            {
                                //originalImage = (new ImageSourceConverter()).ConvertFromString(file.FullName) as ImageSource;
                                original_img.Source = BitmapFromUri(new Uri(file.FullName));
                            }
                            if (file.Name.Contains("mask_img_with_epoch"))
                            {
                                //maskImage = (new ImageSourceConverter()).ConvertFromString(file.FullName) as ImageSource;
                                mask_img.Source = BitmapFromUri(new Uri(file.FullName)); 
                            }
                            if (file.Name.Contains("predict_img_with_epoch"))
                            {
                                //preditctImage = (new ImageSourceConverter()).ConvertFromString(file.FullName) as ImageSource;
                                predict_img.Source = BitmapFromUri(new Uri(file.FullName)); 
                            }
                        }
                    });
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            trainAnoNet?.Cancel();
        }


    }
}
