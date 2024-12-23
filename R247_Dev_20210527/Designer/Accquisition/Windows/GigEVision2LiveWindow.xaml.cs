using AVT.VmbAPINET;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using HalconDotNet;
using Microsoft.Win32;
using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static OpenCvSharp.Stitcher;

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for VimbaLiveViewWindow.xaml
    /// </summary>
    public partial class GigEVision2LiveWindow : ThemedWindow, INotifyPropertyChanged
    {
        GigEVision2 Model;
        ICollectionView view;
        string trigger_mode;
        
        public GigEVision2LiveWindow(GigEVision2 model)
        {
            InitializeComponent();
            this.DataContext = this;
            Model= model;
            tab_parameter.DataContext = Model;


        }
        
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _isLive = false;
        public bool IsLive
        {
            get
            {
                return _isLive;
            }
            set
            {
                if (_isLive != value)
                {
                    _isLive = value;
                    RaisePropertyChanged("IsLive");
                }
            }
        }
        int _focusValue;
        public int FocusValue
        {
            get
            {
                return _focusValue;
            }
            set
            {
                if (_focusValue != value)
                {
                    _focusValue = value;
                    RaisePropertyChanged("FocusValue");
                }
            }
        }
        int _frameID;
        public int FrameID
        {
            get
            {
                return _frameID;
            }
            set
            {
                if (_frameID != value)
                {
                    _frameID = value;
                    RaisePropertyChanged("FrameID");
                }
            }
        }
        bool _isLoading = false;
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
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

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            //window_display.HalconWindow.SetWindowParam("background_color", "white");
        }
        bool re_run = false;
        bool firstRun = true;
        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            if (!Model.IsConnected)
            {
                Model.Connect();
            }
            if (!Model.IsConnected)
            {
                DXMessageBox.Show(this, "Cannot connect to camera", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Model.IsRun)
            {
                if(DXMessageBox.Show(this, "Camera running! Do you want to continue","Warning", 
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning)== MessageBoxResult.OK)
                {
                    Model.Stop();
                    re_run = true;
                }
                else
                {
                    return;
                }
                
            }
            if (firstRun)
            {
                trigger_mode = Model.TriggerMode;
                firstRun = false;
            }
            
            try
            {
                Model.TriggerMode = "Off";
                Model.ImageAcquired = OnImageAcquired;
                Model.Start();
                IsLive = true;
            }catch(Exception ex)
            {

            }
            
        }
        void OnImageAcquired(HImage image, ulong frame_count = 0, double fps = 0)
        {
            HTuple deviation, intensity;
            intensity = image.Intensity(image, out deviation);
            FocusValue = (int)((deviation.D - 80) * 100 / 40);
            window_display.HalconWindow.DispObj(image);
            FrameID++;
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Model.Stop();
                IsLive= false;
            }
            catch
            {
            }
        }

        private void ThemedWindow_Closed(object sender, EventArgs e)
        {

            if(IsLive)
            {
                try {
                    btn_stop_Click(null, null);
                    }catch(Exception ex)
                {

                }
                
            }
            if (trigger_mode != string.Empty| trigger_mode!="")
            {
                try
                {
                    Model.TriggerMode = trigger_mode;
                }catch(Exception ex)
                {

                }
               
                
            }
            if (re_run)
            {
                try
                {
                    Model?.Start();
                }catch(Exception ex)
                {

                }
               
            }
            //if (trigger_source != null)
            //{
            //    var source = Model.ListFeatures.FirstOrDefault(x => x.Name == "TriggerSource");
            //    if (source != null)
            //        Model.SetAndUpdateFeatureValue(source, trigger_source);
            //}
        }

        private void cmb_visibility_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (view == null) return;
            view.Refresh();
        }

        
    }
}
