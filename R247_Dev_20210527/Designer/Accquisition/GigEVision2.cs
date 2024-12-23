using HalconDotNet;
using NOVisionDesigner.Designer.Accquisition.Windows;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class GigEVision2 : HelperMethods,CameraInterface,INotifyPropertyChanged
    {
        public  void LiveView()
        {
            var oldCallback = ImageAcquired;
            var wd = new GigEVision2LiveWindow(this);
            wd.ShowDialog();
            this.ImageAcquired = oldCallback;
        }
        public void SoftwareTrigger()
        {
            try
            {
                framegrabber?.SetFramegrabberParam("TriggerSoftware", 1);
            }
            catch (Exception ex) { }
        }
        public bool IsRun { get { return is_run; } }
        static GigEVision2()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVision2View(), typeof(IViewFor<GigEVision2>));
        }
        #region gige properties
        string _trigger_source;
        public string TriggerSource
        {
            get
            {
                return _trigger_source;
            }
            set
            {
                if (_trigger_source != value)
                {
                    if (SetFeature("TriggerSource", value))
                    {
                        _trigger_source = value;
                        RaisePropertyChanged("TriggerSource");
                    }
                }
            }
        }

        public string[] LstTriggerSource
        {
            get
            {
                return GetFeature("TriggerSource_values").ToSArr();
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

        string _trigger_mode="";
        public string TriggerMode
        {
            get
            {
                return _trigger_mode;
            }
            set
            {
                if (_trigger_mode != value)
                {
                    if (SetFeature("TriggerMode", value))
                    {
                        _trigger_mode = value;
                        RaisePropertyChanged("TriggerMode");
                    }
                }
            }
        }
        double _exposure;
        public double Exposure
        {
            get
            {

                return _exposure;
            }
            set
            {
                if (_exposure != value)
                {
                    if (SetFeature("ExposureTimeAbs", value))
                    {
                        _exposure = value;
                        RaisePropertyChanged("Exposure");
                    }

                }
            }
        }
        double _gain=1;
        public double Gain
        {
            get
            {
                return _gain;
            }
            set
            {
                if (_gain != value)
                {
                    if (SetFeature("Gain", value))
                    {
                        _gain = value;
                        RaisePropertyChanged("Gain");
                    }
                }
            }
        }

        #endregion
        #region commands
        private ICommand _trigger_software_command;
        public ICommand TriggerSoftwareCommand
        {
            get
            {
                return _trigger_software_command ?? (_trigger_software_command = new CommandHandler((result) =>
                {
                    try
                    {
                        framegrabber?.SetFramegrabberParam("TriggerSoftware", 1);
                    }
                    catch (Exception ex) { }
                },()=> true));
            }
        }
        #endregion
        bool _usb_trigger;
        public bool UsbTrigger
        {
            get
            {
                return _usb_trigger;
            }
            set
            {
                if (_usb_trigger != value)
                {
                    _usb_trigger = value;
                    RaisePropertyChanged("UsbTrigger");
                }
            }
        }

        int _pulse_width = 20; //output 20ms pulse
        public int PulseWidth
        {
            get
            {
                return _pulse_width;
            }
            set
            {
                if (_pulse_width != value & value > 0)
                {
                    _pulse_width = value;
                    //if (timer!=null)
                    //timer.Interval = _pulse_width;
                    RaisePropertyChanged("PulseWidth");
                }
            }
        }
        public HTuple GetFeatures(HTuple paramName)
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
        public bool SetFeature(string paramName, HTuple value)
        {
            if (value.Type== HTupleType.STRING)
            {
                if (value.S == "null"| value.S ==null)
                {
                    return false;
                }
            }
            
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
                    RaisePropertyChanged("Device");
                }
            }
        }

        public void Trigger()
        {



            //throw new NotImplementedException();
        }
        public OnDisconnected OnCameraDisconnected { get; set; }
        public bool IsHighSpeed { get; set; } = true;
        public bool IsRecordData()
        {
            if (_record_data_on_minimum)
                if (last_speed * 60 > MinimumDataSpeed)
                {
                    return true;
                }
                else
                    return false;
            else
                return true;
        }
        bool _record_data_on_minimum = false;
        public bool RecordDataOnMinimum
        {
            get
            {
                return _record_data_on_minimum;
            }
            set
            {
                if (_record_data_on_minimum != value)
                {
                    _record_data_on_minimum = value;
                    RaisePropertyChanged("RecordDataOnMinimum");
                }
            }
        }

        int _minimum_speed = 0;
        public int MinimumSpeed
        {
            get
            {
                return _minimum_speed;
            }
            set
            {
                if (_minimum_speed != value)
                {
                    _minimum_speed = value;
                    RaisePropertyChanged("MinimumSpeed");
                }
            }
        }
        int _minimum_data_speed = 0;
        public int MinimumDataSpeed
        {
            get
            {
                return _minimum_data_speed;
            }
            set
            {
                if (_minimum_data_speed != value)
                {
                    _minimum_data_speed = value;
                    RaisePropertyChanged("MinimumDataSpeed");
                }
            }
        }

        bool _record_on_minimum = true;
        public bool RecordOnMinimum
        {
            get
            {
                return _record_on_minimum;
            }
            set
            {
                if (_record_on_minimum != value)
                {
                    _record_on_minimum = value;
                    RaisePropertyChanged("RecordOnMinimum");
                }
            }
        }
        bool _is_enabled_reject = true;
        public bool IsEnabledReject
        {
            get
            {
                return _is_enabled_reject;
            }
            set
            {
                if (_is_enabled_reject != value)
                {
                    _is_enabled_reject = value;
                    RaisePropertyChanged("IsEnabledReject");
                }
            }
        }
        bool _record_on_speed = false;
        public bool RecordOnSpeed
        {
            get
            {
                return _record_on_speed;
            }
            set
            {
                if (_record_on_speed != value)
                {
                    _record_on_speed = value;
                    RaisePropertyChanged("RecordOnSpeed");
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

        public GigEVision2()
        {
            Type = "GigEVision2";

        }
        string[] _lst_ports;
        public string[] LstPort
        {
            get
            {
                return _lst_ports;
            }
            set
            {
                if (_lst_ports != value)
                {
                    _lst_ports = value;
                    RaisePropertyChanged("LstPort");
                }
            }
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public OnImageAcquired ImageAcquired { get; set; }

        public bool Connect()
        {
            // framegrabber = new HFramegrabber("GenICamTL", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "false", "default", "default", 0, -1);
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
                for(int i = 0; i < 10; i++)
                {
                    try
                    {
                        framegrabber = new HFramegrabber("GigEVision2", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "true", "default", Device, 0, -1);
                        
                        if (connecterror)
                        {
                            App.GlobalLogger.LogError("GigeVision2", "connect camera successfully");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (true)
                        {
                            connecterror = true;
                            App.GlobalLogger.LogError("GigeVision2", "Cannot connect camera");
                        }
                        else
                        {
                            break;
                        }

                    }
                    
                    App.GlobalLogger.LogError("GigeVision2", "Waiting to connect camera...");
                    Thread.Sleep(2000);
                }
                
                
                //Update parameter on connect
                ReloadFeatures();
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
            IsConnected = true;
            return true;
        }

        private void ReloadFeatures()
        {
            if (framegrabber == null)
                return;
            if (true)
            {
                try
                {
                    SetFeature("Gain", Gain);
                    SetFeature("ExposureTimeAbs", Exposure);
                    SetFeature("TriggerMode", TriggerMode);
                    SetFeature("TriggerDelayAbs", TriggerDelay);
                    SetFeature("TriggerSource", TriggerSource);

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
                    _exposure = GetFeature("ExposureTimeAbs");
                    RaisePropertyChanged("Exposure");
                    _trigger_mode = GetFeature("TriggerMode");
                    RaisePropertyChanged("TriggerMode");
                    _trigger_delay = GetFeature("TriggerDelayAbs");
                    RaisePropertyChanged("TriggerDelay");
                    _trigger_source = GetFeature("TriggerSource");
                    RaisePropertyChanged("TriggerSource");

                }
                catch (Exception ex)
                {

                }
            }
            //read only values
            RaisePropertyChanged("LstTriggerSource");

        }
        bool is_loading = false;
        public void Load(DeserializeFactory item)
        {
            //throw new NotImplementedException();
            //MessageBox.Show("start");
            is_loading = true;
            LoadParam(item, this);
            is_loading = false;

        }
        HFramegrabber framegrabber;
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            SaveParam(file, this);
        }
        void MatroxImageAcquired(HImage image)
        {
            ImageAcquired?.Invoke(image);
        }
        bool is_run = false;
        public void OnIOCallback(object channel, EventArgs e)
        {
            if (UsbTrigger)
            {
                try
                {
                    if (_trigger_delay >= 1000)
                    {
                        Thread.Sleep((int)(_trigger_delay / 1000));
                    }
                    framegrabber?.SetFramegrabberParam("TriggerSoftware", 1);
                }
                catch (Exception ex) { }
            }
        }
        public ulong FrameID { get; set; }
        public bool IsConnected = false;
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
                    if (UsbTrigger)
                    {
                        //ioclient.
                        //ioclient?.StartDI();
                        //MainWindow.OnIOCallback = this.OnIOCallback;
                    }

                    //reset reject
                    try
                    {
                        framegrabber.SetFramegrabberParam("SyncOutLevels", 0);
                    }
                    catch (Exception ex)
                    {

                    }

                    framegrabber.GrabImageStart(-1);
                    framegrabber.SetFramegrabberParam("grab_timeout", -1);
                    is_run = true;
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
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
                            ImageAcquired?.Invoke(image,FrameID);
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

            //throw new NotImplementedException();
        }



        public void Stop()
        {
            try
            {
                //throw new NotImplementedException();
                if (UsbTrigger)
                {
                    //ioclient?.StopDI();
                }

                is_run = false;
                framegrabber?.SetFramegrabberParam("grab_timeout", 1000);
                framegrabber?.SetFramegrabberParam("do_abort_grab", "true");
            }catch(Exception ex)
            {

            }
           
            
        }

        public void Dispose()
        {
            if (framegrabber != null)
            {
                try
                {
                    Stop();
                }
                catch (Exception ex)
                {

                }
                try
                {
                    framegrabber.Dispose();
                }
                catch (Exception ex)
                {

                }

            }
        }
        double? last_speed = 0;
        public double? GetFPS()
        {
            return 0;
        }
        public void PulseOutput()
        {



        }
        
        public void Reject()
        {
            //throw new NotImplementedException();
            //MatroxInterfaceHook.Instance.PulseReject();
            if (IsEnabledReject)
            {
                if (framegrabber != null)
                {
                    //return;
                    Task.Run(new Action(() =>
                    {

                        //instantDoCtrl?.WriteBit(1, 0, 1);
                        framegrabber.SetFramegrabberParam("SyncOutLevels", 2);
                        Thread.Sleep(_pulse_width);
                        framegrabber.SetFramegrabberParam("SyncOutLevels", 0);
                        // timer.Start();

                        //instantDoCtrl?.WriteBit(1, 0, 0);

                    }));
                }
            }
            
        }

        string last_record_path = "";
        public string TransformRecordPath(string record_path)
        {
            if (_record_on_minimum)
            {
                if (last_speed * 60 < _minimum_speed)
                {
                    return "";
                }
            }


            if (last_record_path.Equals(record_path, StringComparison.Ordinal))
            {

            }
            else
            {
                if (!Directory.Exists(System.IO.Path.Combine(record_path, "LowSpeed")))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(record_path, "LowSpeed"));
                }
                if (!Directory.Exists(System.IO.Path.Combine(record_path, "HighSpeed")))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(record_path, "HighSpeed"));
                }
                last_record_path = record_path;
            }
            if (_record_on_speed)
            {
                if (last_speed * 60 < TransitionSpeed)
                {
                    return System.IO.Path.Combine(record_path, "LowSpeed");
                }
                else
                {
                    return System.IO.Path.Combine(record_path, "HighSpeed");
                }
            }
            else return record_path;
        }


    }
    
}
