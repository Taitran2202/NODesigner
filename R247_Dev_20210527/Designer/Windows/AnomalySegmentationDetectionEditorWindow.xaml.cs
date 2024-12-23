using DevExpress.Xpf.Core;
using HalconDotNet;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Windows.HelperWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
using NOVisionDesigner.Designer.Python;
using NOVisionDesigner.Windows;

namespace NOVisionDesigner.Designer.Windows
{
    public partial class AnomalySegmentationDetectionEditorWindow : Window, INotifyPropertyChanged
    {

        private bool _isTraining=false;
        public bool IsTraining
        {
            get
            {
                return _isTraining;
            }
            set
            {
                if (_isTraining != value)
                {
                    _isTraining = value;
                    RaisePropertyChanged("IsTraining");
                }
            }
        }

        private string _precision = "Light";
        public string Precision
        {
            get { return _precision; }
            set
            {
                if (_precision != value)
                {
                    _precision = value;
                    RaisePropertyChanged("Precision");
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
                }
            }
        }

        private bool _isMargin = true;
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

        private string _colorDraw = "#00ff00aa";
        public string ColorDraw
        {
            get { return _colorDraw; }
            set 
            {
                if (_colorDraw != value)
                {
                    _colorDraw = value;
                    RaisePropertyChanged("ColorDraw");
                }
            }
        }

        private ObservableCollection<ImageFilmstrip> _listImage;

        public ObservableCollection<ImageFilmstrip> ListImage
        {
            get { return _listImage; }
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
            get { return _selectedMarker; }
            set 
            {
                if (_selectedMarker != value)
                {
                    _selectedMarker = value;
                    RaisePropertyChanged("SelectedMarker");
                }
            }
        }


        public List<RegionMaker> CurrentRegionList { get; set; } = new List<RegionMaker>();
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public ObservableCollection<ClassifierClass1> ClassList { get; set; }

        public AnomalySegmentationDetectionNode node;


        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        
        private int row, col;
        private double mpx, mpy;
        private HWindow display;
        private HImage image;
        private string ImageDir, ModelDir;

        private void Progress(int progress)
        {

        }
        private void TrainResult(bool result)
        {

        }
        TrainASModel trainASModel;
        public AnomalySegmentationDetectionEditorWindow(string imageDir, string modelDir, Action<Action<int>, Action<bool>> TrainFunction,
            AnomalySegmentationDetectionNode anomalySegmentationDetectionNode, List<Augmentation> listAugmentation)
        {
            InitializeComponent();
            box_step.DataContext = anomalySegmentationDetectionNode.TrainConfig;
            node = anomalySegmentationDetectionNode;
            

            this.ImageDir = imageDir;
            this.ModelDir = modelDir;

            btn_add_image.Click += Btn_add_image_Click;
            btn_add_image_camera.Click += (s, e) =>
            {
                if (node != null)
                {
                    var imageFeed = node.Image;
                    if (imageFeed != null)
                    {
                        if (imageFeed.Value != null)
                        {
                            try
                            {
                                var image = imageFeed.Value.Clone();
                                var filename = DateTime.Now.Ticks.ToString();
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
                    }
                }
            };

            btn_step_cancel.Click += (s, e) =>
            {
                box_step.Visibility = Visibility.Hidden;
            };

            btn_step_ok.Click += (s, e) =>
            {
                var step = spin_step.Value;
                box_step.Visibility = Visibility.Hidden;
                anomalySegmentationDetectionNode.TrainConfig.Save(anomalySegmentationDetectionNode.TrainConfigDir);
                Task.Run(() =>
                {
                    IsTraining = true;
                    anomalySegmentationDetectionNode.ClearSession();
                    trainASModel = new TrainASModel();
                    trainASModel.TrainPython(anomalySegmentationDetectionNode.TrainConfigDir,(trainargs) =>
                        {
                            if (trainargs.State== TrainState.Completed)
                            {
                                IsTraining = false;
                                try
                                {
                                    var threshold_text = File.ReadAllText(System.IO.Path.Combine(anomalySegmentationDetectionNode.TrainConfig.SaveModelDir,"threshold.txt"));
                                    MessageBox.Show("Train complete with minimum threshold: " + threshold_text);
                                }catch (Exception ex)
                                {

                                }
                                
                                anomalySegmentationDetectionNode.segmentation.LoadRecipe(anomalySegmentationDetectionNode.segmentation.ModelDir);
                            }
                            if(trainargs.State== TrainState.Cancel | trainargs.State== TrainState.Error)
                            {
                                IsTraining = false;
                            }
                            if(trainargs.State== TrainState.OnGoing)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.progress.Value = (trainargs.Progress);
                                    lb_acc.Content = trainargs.Loss;
                                    lb_eta.Content = trainargs.ETA.ToString();

                                }));
                            }
                            
                        }
                    , (logString) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            log.AppendText(String.Format("{0}{1}", logString, Environment.NewLine));
                        }));
                       
                    });
                });
            };

            btn_train.Click += (sender, e) =>
            {
                box_step.Visibility = Visibility.Visible;
            };
            LoadImageList(imageDir);
            this.DataContext = this;
            
        }
        HRegion ImageRegion;
        public void DispOverlay()
        {
            if (ImageRegion != null)
            {
                display.ClearWindow();
                HRegion OuterRegion=ImageRegion.Difference(RegionMaker.Region.region);
                window_display.HalconWindow.SetColor("#00ff005f");
                window_display.HalconWindow.DispRegion(OuterRegion);
            }
        }
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems[0] as ImageFilmstrip;
            if (selected != null)
            {
                //save previous result
                
                //load current image
                try
                {
                    image = new HImage(selected.FullPath);
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                    ImageName = selected.FileName;
                    image.GetImageSize(out HTuple w, out HTuple h);
                    ImageRegion = new HRegion(0,0,h, w);
                }
                catch (Exception ex)
                {
                    //image = new HImage();
                }
                //load current annotation
               


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

        //public void DispOverlay()
        //{
        //    if (display == null)
        //        return;
        //    display.ClearWindow();
        //    display.SetDraw("fill");
        //    foreach (var item in CurrentRegionList)
        //    {
        //        display.SetColor(AddOpacity(item.Annotation.Color, ColorOpacity / 100));
        //        display.DispObj(item.Region.region);
        //        display.DispText(item.Annotation.Name, "image", item.Region.row1, item.Region.col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
        //    }
        //}

        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }


        #region functions for onclick events
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
                        var newfile = System.IO.Path.Combine(ImageDir, filename);
                        if (System.IO.File.Exists(newfile))
                        {
                            newfile = System.IO.Path.Combine(ImageDir, filename+"_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
                        }
                        System.IO.File.Copy(file, newfile);
                        ListImage.Add(new ImageFilmstrip(newfile));
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
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


                }
                catch (Exception ex)
                {

                }

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
        private void btn_clear_train_data_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(ModelDir + "/checkpoints"))
            {
                SaveWindow.DeleteDirectory(ModelDir + "/checkpoints");
            }
            DXMessageBox.Show("Train data has been removed.");
        }
        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion


        #region functions for key events
        private void window_display_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Delete)
            //{
            //    if (SelectedMarker != null)`
            //    {
            //        SelectedMarker.Region.ClearDrawingData(display);
            //        CurrentRegionList.Remove(SelectedMarker);
            //        if (CurrentRegionList.Count > 0)
            //        {
            //            SelectedMarker = CurrentRegionList[CurrentRegionList.Count - 1];
            //        }
            //        else
            //        {
            //            SelectedMarker = null;
            //        }

            //        DispOverlay();
            //    }
            //}
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


        #region function for mouse events and display
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (e.Button == MouseButton.Right)
            {
                row = (int)e.Row;
                col = (int)e.Column;
            }
            Keyboard.Focus(window_display);
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            trainASModel?.Cancel();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            PropertiesWindow wd = new PropertiesWindow(node.TrainConfig);
            wd.ShowDialog();
        }
        RegionMaker RegionMaker;
        public void OnUpdate(RegionMaker marker, Region region)
        {
            node.TrainConfig.Mask.row1 = (int)marker.Region.row1;
            node.TrainConfig.Mask.col1 = (int)marker.Region.col1;
            node.TrainConfig.Mask.row2 = (int)marker.Region.row2;
            node.TrainConfig.Mask.col2 = (int)marker.Region.col2;
            DispOverlay();
        }
        public void OnSelected(RegionMaker marker, Region region)
        {

        }
        public void AttachMask()
        {
            if(node.TrainConfig.UseMask)
            if (RegionMaker == null)
            {
                RegionMaker = new RegionMaker()
                {
                    Region = new Rectange1(false)
                    {
                        row1 = node.TrainConfig.Mask.row1,
                        row2 = node.TrainConfig.Mask.row2,
                        col1 = node.TrainConfig.Mask.col1,
                        col2 = node.TrainConfig.Mask.col2
                    }
                };
                RegionMaker.Attach(window_display.HalconWindow, null, OnUpdate, OnSelected);
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (image == null)
            {
                MessageBox.Show("Add or select an image first!");
                return;
            }
            if (RegionMaker == null)
            {
                node.TrainConfig.UseMask = true;
                image.GetImageSize(out HTuple w, out HTuple h);
                RegionMaker = new RegionMaker() { Region = new Rectange1(false) { 
                    row1 = 0, 
                    row2 = h, 
                    col1 = 0, 
                    col2 = w } };
                RegionMaker.Attach(window_display.HalconWindow, null, OnUpdate, OnSelected);
            }
            
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (RegionMaker != null)
            {
                RegionMaker.Region.ClearDrawingData(display);
                RegionMaker = null;
                node.TrainConfig.UseMask = false;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            box_step.Visibility = Visibility.Hidden;
            try
            {
                node.TrainConfig.Save(node.TrainConfigDir);
                System.Diagnostics.Process.Start("cmd.exe", "/k python Designer/Python/AnomalySegmentationDetection/train.py \"" + node.TrainConfigDir + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("fill");
                display.SetColor(ColorDraw);
                if (node.TrainConfig.UseMask)
                {
                    AttachMask();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
        #endregion
    }
}
