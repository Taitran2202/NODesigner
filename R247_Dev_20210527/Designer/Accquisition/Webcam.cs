using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static DevExpress.Mvvm.Native.TaskLinq;

namespace NOVisionDesigner.Designer.Accquisition
{
   
    public class Webcam : CameraInterfaceBase, CameraInterface, INotifyPropertyChanged
    {
        static Webcam()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.WebcamView(), typeof(IViewFor<Webcam>));
        }
        public Webcam()
        {
            Type = "Webcam";
            TriggerSoftwareCommand = new RelayCommand(ExecuteMyCommand, CanExecuteMyCommand);
        }
        public void SoftwareTrigger()
        {

        }
        public void LiveView()
        {

        }
        public event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public ICommand TriggerSoftwareCommand { get; set; }
        private void ExecuteMyCommand(object parameter)
        {
            HImage image = null;
            try
            {
                image = framegrabber.GrabImageAsync(-1);
            }
            catch (Exception ex)
            {

            }

            if (image != null)
            {
                FrameID++;
                ImageAcquired?.Invoke(image, FrameID);
            }
        }
        
        private bool CanExecuteMyCommand(object parameter)
        {
            
            // Kiểm tra xem lệnh có thể được thực thi hay không
            return true; // hoặc false tùy vào logic của bạn
        }
        
        public bool Connect()
        {
            try
            {
                if (framegrabber != null)
                {
                    HOperatorSet.CloseFramegrabber(framegrabber);
                }

                // framegrabber.CloseFramegrabber();
            }
            catch (Exception ex)
            {

            }
            if (Device == null)
            {
                return false;
            }
            try
            {

                bool connecterror = false;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        framegrabber = new HFramegrabber("DirectShow", 1, 1, 0, 0, 0, 0, "default", 8, "rgb", -1, "false", "default", Device, 0, -1);

                        if (connecterror)
                        {
                            App.GlobalLogger.LogError("DirectShow", "connect camera successfully");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (true)
                        {
                            connecterror = true;
                            App.GlobalLogger.LogError("DirectShow", "Cannot connect camera");
                        }
                        else
                        {
                            break;
                        }

                    }

                    App.GlobalLogger.LogError("DirectShow", "Waiting to connect camera...");
                    Thread.Sleep(2000);
                }
                
                //Update parameter on connect
                //ReloadFeatures();
            }
            catch (Exception ex)
            {
                if (ex is HalconException)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Camera Disconnected! Waiting to implement");
                        //CameraDisconnectedWindow wd = new CameraDisconnectedWindow();
                        //wd.Owner = Application.Current.MainWindow;
                        //wd.ShowDialog();
                    }));
                    App.GlobalLogger.LogError("GigeVision2", "Cannot connect camera");

                    OnCameraDisconnected?.Invoke();
                    return false;
                }
                App.GlobalLogger.LogError("GigeVision2", "Cannot connect camera");
                MessageBox.Show(ex.Message);
            }
            return true;
        }
        public bool ConnectCamera()
        {
            return true;
        }
        public OnDisconnected OnCameraDisconnected { get; set; }
        public OnImageAcquired ImageAcquired { get; set; }
        public bool IsHighSpeed { get; set; } = true;
        bool is_run = false;
        public bool IsRun
        {
            get
            {
                return is_run;
            }
            set
            {
                if (is_run != value)
                {
                    is_run = value;
                    RaisePropertyChanged("IsRun");
                }
            }
        }
        string _type;
        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    RaisePropertyChanged("Type");
                }
            }
        }
        int _transition_speed = 500;
        public int TransitionSpeed
        {
            get
            {
                return _transition_speed;
            }
            set
            {
                if (_transition_speed != value)
                {
                    _transition_speed = value;
                    RaisePropertyChanged("TransitionSpeed");
                }
            }
        }
        double? last_speed = 0;
        double fps = 0;
        public double? GetFPS()
        {
            last_speed = fps;
            if (last_speed * 60 < TransitionSpeed)
                IsHighSpeed = false;
            else
                IsHighSpeed = true;
            return last_speed;

        }
        string _device;
        public string Device
        {
            get
            {
                return _device;
            }
            set
            {
                if (_device != value)
                {
                    _device = value;
                    //DeviceName = _device;
                    RaisePropertyChanged("Device");
                }
            }
        }
        string _device_name;
        public string DeviceName
        {
            get
            {
                return _device_name;
            }
            set
            {
                if (_device_name != value)
                {
                    _device_name = value;
                    RaisePropertyChanged("DeviceName");
                }
            }
        }
        double _exposure = 1500;
        public double Exposure
        {
            get
            {
                return _exposure;
            }
            set
            {
                if (value != _exposure)
                {
                    _exposure = value;
                    RaisePropertyChanged("Exposure");
                }
            }
        }

        double start_x = 0;
        public double StartX
        {
            get
            {
                return start_x;
            }
            set
            {
                if (value != start_x)
                {
                    start_x = value;
                    RaisePropertyChanged("StartX");
                }
            }

        }
        double start_y = 0;
        public double StartY
        {
            get
            {
                return start_y;
            }
            set
            {
                if (value != start_y)
                {
                    start_y = value;
                    RaisePropertyChanged("StartY");
                }
            }
        }
        double _width = 4128;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (value != _width)
                {
                    _width = value;
                    RaisePropertyChanged("Width");
                }
            }
        }
        double _height = 3008;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (value != _height)
                {
                    _height = value;
                    RaisePropertyChanged("Height");
                }
            }
        }
        double _gain;
        public double Gain
        {
            get
            {
                // double aaa = 0;
                // MIL.MdigInquireFeature(MatroxInterface.MatroxInterfaceHook.Instance.DIGID, MIL.M_FEATURE_VALUE, "Gain", MIL.M_TYPE_DOUBLE, ref aaa);
                // _gain = aaa;
                return _gain;
            }
            set
            {
                if (value != _gain)
                {
                    _gain = value;
                    //  MIL.MdigControlFeature(MatroxInterface.MatroxInterfaceHook.Instance.DIGID, MIL.M_FEATURE_VALUE, "Gain", MIL.M_TYPE_DOUBLE, ref _gain);
                    RaisePropertyChanged("Gain");
                }
            }
        }
        double _trigger_delay;
        public double TriggerDelay
        {
            get
            {
                return _trigger_delay;
            }
            set
            {
                if (_trigger_delay != value)
                {
                    if (SetFeature("TriggerDelayAbs", value))
                    {
                        _trigger_delay = value;
                        RaisePropertyChanged("TriggerDelay");
                    }
                }
            }
        }




        public HFramegrabber framegrabber;
        public ulong FrameID { get; set; }
        public void Start()
        {
            Task.Run(new Action(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                if (framegrabber == null)
                {
                    this.Connect();
                }


                try
                {
                    if (framegrabber == null)
                        return;
                    /*framegrabber.SetFramegrabberParam("Width", Width);
                    framegrabber.SetFramegrabberParam("Height", Height);
                    framegrabber.SetFramegrabberParam("OffsetX", StartX);
                    framegrabber.SetFramegrabberParam("OffsetY", StartY);*/
                     //framegrabber.SetFramegrabberParam("grab_timeout", -1);
                    //framegrabber.SetFramegrabberParam("ExposureTime", Exposure);
                    


                    //MainWindow.DeltaPLC.WriteSingleRegisters("D200", (ushort)DelayProduct);


                    is_run = true;
                    while (is_run)
                    {
                        HImage image = null;
                        try
                        {
                            image = framegrabber.GrabImageAsync(-1);
                        }
                        catch (Exception ex)
                        {

                        }

                        if (image != null)
                        {
                            FrameID++;
                            ImageAcquired?.Invoke(image, FrameID);
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (ex is HalconException)
                        return;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(ex.Message)));
                    return;
                }

            }
           ));

            return;
        }
        private void ReloadFeatures()
        {
            if (framegrabber == null)
                return;
            if (false)
            {
                try
                {
                    SetFeature("Gain", Gain);
                    //SetFeature("ExposureTimeAbs", Exposure);
                    SetFeature("ExposureTime", Exposure);
                    //SetFeature("TriggerMode", TriggerMode);
                    //SetFeature("TriggerDelayAbs", TriggerDelay);
                    //SetFeature("TriggerSource", TriggerSource);

                }
                catch (Exception ex)
                {

                }
                MainWindow.is_load = false;
            }
            else
            {
                try
                {
                    _gain = GetFeature("Gain");
                    RaisePropertyChanged("Gain");
                    _exposure = GetFeature("ExposureTime");
                    RaisePropertyChanged("Exposure");

                }
                catch (Exception ex)
                {

                }
            }
            //read only values
            RaisePropertyChanged("LstTriggerSource");

        }
        public HTuple GetFeature(string paramName)
        {
            if (framegrabber == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    return framegrabber.GetFramegrabberParam(paramName);

                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }
        bool is_loading = false;
        public bool SetFeature(string paramName, HTuple value)
        {
            if (is_loading)
            {
                return true;
            }
            if (framegrabber == null)
            {
                return false;
            }
            else
            {
                try
                {
                    framegrabber.SetFramegrabberParam((HTuple)paramName, value);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }

            }
        }
        public void Stop()
        {
            is_run = false;
            framegrabber?.SetFramegrabberParam("grab_timeout", 1000);
            framegrabber.SetFramegrabberParam("do_abort_grab", "true");
        }
        /*private ICommand _trigger_software_command;
        public ICommand TriggerSoftwareCommand
        {
            
            get
            {
                return _trigger_software_command ?? (_trigger_software_command = new CommandHandler((result) =>
                {
                    try
                    {
                        HImage image = null;
                        try
                        {
                            image = framegrabber.GrabImageAsync(-1);
                        }
                        catch (Exception ex)
                        {

                        }

                        if (image != null)
                        {
                            FrameID++;
                            ImageAcquired?.Invoke(image, FrameID);
                        }
                    }
                    catch (Exception ex) { }
                }, () => true));
            }
        }*/
        public void Trigger()
        {

        }
        public void Reject()
        {

        }
        
        string last_record_path = "";
        public string TransformRecordPath(string record_path)
        {
            return record_path;
        }
        public bool IsRecordData()
        {
            return true;
        }
        public void Dispose()
        {
            
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}
