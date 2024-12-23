using DevExpress.Xpf.Core;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
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
using NOVisionDesigner.Designer.Nodes;
using HalconDotNet;
using Microsoft.WindowsAPICodePack.Dialogs;
using NOVisionDesigner.Designer.Keyboards;
using NOVisionDesigner.Designer.Misc;
using System.ComponentModel;
using NOVisionDesigner.Designer.Python;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for PadInspectionRecorderWindow.xaml
    /// </summary>
    public partial class PadInspectionRecorderWindow : ThemedWindow,INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public static Point LastWindowState = new Point(0, 0);
        public static double LastWidth = 0;
        public static double LastHeight = 0;
        public PadInspectionRecorderWindow(PadInspection node)
        {
            InitializeComponent();
            border_recording.DataContext = node.Recoder;
            this.DataContext = this;
            this.Recorder = node.Recoder;
            this.Node = node;
            lst_images.ItemsSource = node.Recoder.ResultRecoderQueue;
            if (LastHeight > 0 & LastWidth > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = LastWindowState.Y;
                this.Left = LastWindowState.X;
                this.Width = LastWidth;
                this.Height = LastHeight;
            }
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            this.SizeChanged += RecoderViewerWindow_SizeChanged;
            this.LocationChanged += RecoderViewerWindow_LocationChanged;
            this.Closed += (o, e) =>
            {
                node.Recoder.IsView = false;
            };
        }
        public PadInspectionRecorderWindow()
        {
            InitializeComponent();
        }
        private void RecoderViewerWindow_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                UpdateLastLocation();
            }

        }

        private void RecoderViewerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                UpdateLastLocation();
            }
        }
        void UpdateLastLocation()
        {
            if (this.Left < 0 | this.Top < 0)
            {
                return;
            }
            LastWindowState.X = this.Left;
            LastWindowState.Y = this.Top;
            LastWidth = this.ActualWidth;
            LastHeight = this.ActualHeight;
        }

        private bool _is_loading = false;
        public bool IsLoading
        {
            get { return _is_loading; }
            set
            {
                if (_is_loading != value)
                {
                    _is_loading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }
        private string SelectionColor;
        private double _colorOpacity = 20;
        public double ColorOpacity
        {
            get { return _colorOpacity; }
            set
            {
                if (_colorOpacity != value)
                {
                    SelectionColor = AddOpacity("#00ff00ff", value / 100);
                    _colorOpacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        public HWindow display;
        public PadInspectionRecorder Recorder { get; set; }

        PadInspectionResult defect;
        public PadInspection Node { get; set; }
        bool first_init = true;
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public void OnSelectionChanged(object sender, EventArgs e)
        {
            Recorder.IsView = true;
            ShowResult(sender as PadInspectionResult);
        }
        public void ShowResult(PadInspectionResult defect)
        {
            try
            {
                this.defect = defect;

                display.AttachBackgroundToWindow(defect.Image);
                display.ClearWindow();
                if (first_init)
                {
                    window_display.SetFullImagePart();
                    first_init = false;
                }
                if (defect.DefectRegion != null)
                {
                    display.SetColor("red");
                    display.SetDraw("margin");
                    display.DispObj(defect.DefectRegion);
                }
                
                
                display.FlushBuffer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HImage image = new HImage();
            image.GenEmptyObj();
            window_display.HalconWindow.DispImage(image);


        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("margin");
        }

        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            //if (defect != null & chk_select_region.IsChecked == true)
            //{
            //    HRegion selectedRegion = null;
            //    int MinArea = int.MaxValue;
            //    foreach (IDisplayable displayItem in defect.lst_display)
            //    {
            //        if (displayItem is DisplayObject)
            //        {
            //            DisplayObject region = displayItem as DisplayObject;
            //            if (region.display_object is HRegion)
            //            {

            //                HRegion selected = (region.display_object as HRegion).SelectRegionPoint((int)e.Row, (int)e.Column);
            //                if (selected.CountObj() > 0)
            //                {
            //                    if (selected.Area < MinArea)
            //                    {
            //                        selectedRegion = selected;
            //                        MinArea = selected.Area;
            //                    }
            //                    else
            //                    {
            //                        continue;
            //                    }
            //                }


            //            }

            //        }



            //    }
            //    if (selectedRegion != null)
            //    {
            //        if (selectedRegion.CountObj() > 0)
            //        {
            //            var features = selectedRegion.RegionFeatures(new HTuple("area", "width", "height"));
            //            lb_area.Content = Math.Round(((features[0].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
            //            lb_width.Content = Math.Round(((features[1].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
            //            lb_height.Content = Math.Round(((features[2].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
            //            display.ClearWindow();
            //            foreach (IDisplayable disp in defect.lst_display)
            //            {
            //                disp?.Display(display);
            //            }
            //            display.SetDraw("fill");
            //            display.SetColor(SelectionColor);
            //            display.DispRegion(selectedRegion);
            //            // selected.dis(display);
            //            //for (int index = 0; index < defect.regions.Count; index++)
            //            //{
            //            //    display.SetColor(defect.ColorCodes[index]);
            //            //    display.DispRegion(defect.regions[index].region);
            //            //}

            //            //display.SetDraw("fill");
            //            //selected.DispObj(display);
            //            //display.SetDraw("margin");
            //            display.FlushBuffer();
            //            //display.SetWindowParam("flush", "true");
            //        }
            //    }
            //}

        }
        private void btn_zoom_in_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, 120);
        }
        private void btn_zoom_out_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, -120);
        }
        private void btn_zoom_fit_click(object sender, RoutedEventArgs e)
        {
            window_display.SetFullImagePart();
        }
        private void btn_save_image_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            //diag.InitialDirectory = MainWindow.path_record_ram;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    string record_path = diag.FileName;


                    defect.Image.WriteImage("tiff", 0, record_path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void Btn_save_all_Click(object sender, RoutedEventArgs e)
        {
            VirtualNumericKeyboard wd_input = new VirtualNumericKeyboard(20);
            int image_number = 20;
            if (wd_input.ShowDialog() == true)
            {
                image_number = (int)wd_input.result;
                try
                {

                    System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                    if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        string record_path = diag.SelectedPath;
                        Recorder.IsSaving = true;
                        int max = Recorder.ResultRecoderQueue.Count;
                        if (max >= image_number)
                        {
                            max = image_number;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            try
                            {
                                Recorder.ResultRecoderQueue.ElementAt(max - i - 1).Image.WriteImage("tiff", 0, record_path + "\\" + i.ToString());
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        Recorder.IsSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_previous_Click(object sender, RoutedEventArgs e)
        {
            if (lst_images.SelectedIndex > 0)
            {
                lst_images.SelectedIndex = lst_images.SelectedIndex - 1;
            }
            
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            lst_images.SelectedIndex = lst_images.SelectedIndex +1;
        }

        void Run(HImage padImage)
        {
            var anomalyMap = Node.Segment(padImage);
            anomalyMap = anomalyMap.ReduceDomain(padImage.GetDomain());
            var IContext = new InspectionContext(null, padImage, 1, 1, "");
            foreach (var item in Node.PadDefectTools)
            {
                if (item.IsEnabled)
                {
                    var region = item.Run(anomalyMap, new HHomMat2D(), IContext);
                    bool resultBool = region.CountObj() > 0;
                }


            }
            window_display.HalconWindow.ClearWindow();
            IContext.inspection_result.Display(window_display);
        }
        private void btn_set_current_image_Click(object sender, RoutedEventArgs e)
        {
            if (Node != null)
            {
                if (defect != null)
                {
                    Run(defect.Image);
                }
                
            }

        }


        private void save_image_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (defect == null)
            {
                return;
            }
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            //diag.InitialDirectory = MainWindow.path_record_ram;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    string record_path = diag.FileName;


                    defect.Image.WriteImage("bmp", 0, record_path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void save_all_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            VirtualNumericKeyboard wd_input = new VirtualNumericKeyboard(20);
            int image_number = 20;
            if (wd_input.ShowDialog() == true)
            {
                image_number = (int)wd_input.result;
                try
                {

                    System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                    if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        string record_path = diag.SelectedPath;
                        Recorder.IsSaving = true;
                        int max = Recorder.ResultRecoderQueue.Count;
                        if (max >= image_number)
                        {
                            max = image_number;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            try
                            {
                                Recorder.ResultRecoderQueue.ElementAt(max - i - 1).Image.WriteImage("bmp", 0,
                                    System.IO.Path.Combine(record_path, i.ToString()));
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        Recorder.IsSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_save_all_image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommonOpenFileDialog diag = new CommonOpenFileDialog();
                diag.IsFolderPicker = true;
                //diag.folder
                // diag.SelectedPath = acq.Record_path;
                if (diag.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string record_path = diag.FileName;
                    Recorder.IsSaving = true;
                    int max = Recorder.ResultRecoderQueue.Count;
                    //if (max >= image_number)
                    //{
                    //    max = image_number;
                    //}
                    for (int i = 0; i < max; i++)
                    {
                        try
                        {
                            Recorder.ResultRecoderQueue.ElementAt(max - i - 1).Image.WriteImage("bmp", 0,
                                System.IO.Path.Combine(record_path, i.ToString()));
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    Recorder.IsSaving = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_save_selected_image_Click(object sender, RoutedEventArgs e)
        {
            if (defect == null)
            {
                return;
            }
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            diag.AddExtension = false;
            //diag.InitialDirectory = MainWindow.path_record_ram;
            diag.Filter = "Bitmap (*.bmp)|*.bmp|PNG (*.png)|*.png|All files (*.*)|*.*";
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {

                    string record_path = diag.FileName;

                    if (diag.FilterIndex == 1)
                    {
                        defect.Image.WriteImage("bmp", 0, record_path.Remove(record_path.Length - 4, 4));
                    }
                    else if (diag.FilterIndex == 2)
                    {
                        defect.Image.WriteImage("png", 0, record_path.Remove(record_path.Length - 4, 4));
                    }
                    else
                    {
                        defect.Image.WriteImage("bmp", 0, record_path);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }
        bool show_graphic = true;
        private void chk_show_graphic_Checked(object sender, RoutedEventArgs e)
        {
            show_graphic = true;
            if (this.defect != null)
            {
                ShowResult(this.defect);
            }
        }

        private void chk_show_graphic_Unchecked(object sender, RoutedEventArgs e)
        {
            show_graphic = false;
            if (this.defect != null)
            {
                ShowResult(this.defect);
            }
        }

        private void lst_images_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0)
            {
                return;
            }
            if(lst_images.SelectedItem!= defect)
            {
                OnSelectionChanged(lst_images.SelectedItem, e);
            }
            
        }

        private void btn_mark_good_CLick(object sender, RoutedEventArgs e)
        {
            if (lst_images.SelectedItem != null)
            {
                var selectedItem = lst_images.SelectedItem as PadInspectionResult;
                if (selectedItem != null)
                {
                    selectedItem.IsNormal= true; 
                }
            }
        }

        private void btn_mark_bad_Click(object sender, RoutedEventArgs e)
        {
            if (lst_images.SelectedItem != null)
            {
                var selectedItem = lst_images.SelectedItem as PadInspectionResult;
                if (selectedItem != null)
                {
                    selectedItem.IsNormal = false;
                }
            }
        }

        private void btn_pause_record_Click(object sender, RoutedEventArgs e)
        {
            Node.Recoder.IsRecording = false;
        }

        private void btn_continue_record_Click(object sender, RoutedEventArgs e)
        {
            Node.Recoder.IsRecording = true;
        }

        private void btn_add_train_quick_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_add_train_Click(object sender, RoutedEventArgs e)
        {
            var oldState = Node.Recoder.IsRecording;
            Node.Recoder.IsRecording= false;
            foreach(var item in Node.Recoder.ResultRecoderQueue)
            {
                if (item.IsNormal)
                {
                    if (!item.IsAdded)
                    {
                        var newfile = Extensions.Functions.NewFileName(Node.AnomalyConfig.NormalDir);
                        item.Image.FullDomain().WriteImage("png", 0, newfile);
                        item.IsAdded = true;
                    }
                    
                }
               
            }
            Node.Recoder.IsRecording = oldState;
            Train();
        }
        void Train()
        {
            Task.Run(new Action(() =>
            {
                TrainAnomalyV3 train = new TrainAnomalyV3();
                IsLoading = true;
                //Node.AnomalyConfig.ModelName = "FAPM_Pytorch";
                //Node.AnomalyConfig.FAPMConfig.UPDATE_THRESHOLD = false;
                Node.AnomalyConfig.Save();
                train.TrainPython(Node.AnomalyConfig.ConfigDir, Node.AnomalyConfig.ModelName, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsLoading = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Training was cancel because of error!", "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });

                    }
                    if (trainargs.State == TrainState.OnGoing)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsLoading = false;
                        //Node.AnomalyRuntime.State = ModelState.Unloaded;
                        Node.ReloadAnomaly();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
                IsLoading = false;
            }));
        }
    }
}
