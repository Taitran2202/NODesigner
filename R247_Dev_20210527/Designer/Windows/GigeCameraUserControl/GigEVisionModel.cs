using HalconDotNet;
using Microsoft.Win32;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NOVisionDesigner.Designer.Windows.GigeCameraUserControl
{
    public class GigeCameraModel : OutputSource<HImage>, CameraInterface, INotifyPropertyChanged, IObservable<HImage>
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region Interface

        //List<IObserver<HImage>> obser = new List<IObserver<HImage>>();
        //public IDisposable Subscribe(IObserver<HImage> observer)
        //{
        //    lock (obserlock)
        //    {
        //        obser.Add(observer);
        //    }

        //    return new Unsubscriber(obser, observer);
        //}
        object obserlock = new object();
        
        public void OnImageAquired(object sender, HImage image)
        {
            foreach (var item in obser)
            {
                item.OnNext(image);
            }
        }
        private class Unsubscriber : IDisposable
        {
            private List<IObserver<HImage>> _observers;
            private IObserver<HImage> _observer;

            public Unsubscriber(List<IObserver<HImage>> observers, IObserver<HImage> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
        #endregion
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
        public string[] LstUserSetDefaultSelector
        {
            get
            {
                return GetFeature("UserSetDefaultSelector_values").ToSArr();
            }

        }

        public string[] LstUserSetSelector
        {
            get
            {
                return GetFeature("UserSetSelector_values").ToSArr();
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

        string _trigger_mode;
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
                        //if (value == "On")
                        //{
                        //    this.Start();
                        //}
                        //else
                        //{
                        //    this.Stop();
                        //}
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
        double _gain;
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
        #region Properties
        HFramegrabber framegrabber;
        bool is_load = false;
        bool is_loading = false;
        //public bool is_run = false;
        int _pulse_width = 20; //output 20ms pulse
        string last_record_path = "";

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
        public OnDisconnected OnCameraDisconnected { get; set; }

        public OnImageAcquired ImageAcquired { get; set; }

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

        string _stateRun = "Start";
        public string stateRun
        {
            get
            {
                return _stateRun;
            }
            set
            {
                if (_stateRun != value)
                {
                    _stateRun = value;
                    RaisePropertyChanged("stateRun");
                }
            }
        }

        bool _is_run = false;
        public bool is_run
        {
            get
            {
                return _is_run;
            }
            set
            {
                if (_is_run != value)
                {
                    _is_run = value;
                    stateRun = is_run ? "Stop":"Start";
                    isEnableUserSetLoad = !is_run;
             
                    isEnableUserSetSave = UserSetSelector=="Default"?false: !is_run;
                    RaisePropertyChanged("is_run");
                }
            }
        }
        bool _isEnableUserSetLoad;
        public bool isEnableUserSetLoad
        {
            get
            {
                return _isEnableUserSetLoad;
            }
            set
            {
                if (_isEnableUserSetLoad != value)
                {
                    _isEnableUserSetLoad = value;
                    RaisePropertyChanged("isEnableUserSetLoad");
                }
            }
        }
        bool _isEnableUserSetSave;
        public bool isEnableUserSetSave
        {
            get
            {
                return _isEnableUserSetSave;
            }
            set
            {
                if (_isEnableUserSetSave != value)
                {
                    _isEnableUserSetSave = value;
                    RaisePropertyChanged("isEnableUserSetSave");
                }
            }
        }
        string _userSetDefaultSelector;
        public string UserSetDefaultSelector
        {
            get
            {
                return _userSetDefaultSelector;
            }
            set
            {
                if (_userSetDefaultSelector != value)
                {
                    if (SetFeature("UserSetDefaultSelector", value))
                    {
                        _userSetDefaultSelector = value;
                        RaisePropertyChanged("UserSetDefaultSelector");
                    }
                }
              
            }
        }

        string _userSetSelector;
        public string UserSetSelector
        {
            get
            {
                return _userSetSelector;
            }
            set
            {
                if (_userSetSelector != value)
                {
                    if (SetFeature("UserSetSelector", value))
                    {
                        _userSetSelector = value;
                        if (_userSetSelector == "Default")
                        {
                            isEnableUserSetSave = false;
                        }
                        RaisePropertyChanged("UserSetSelector");
                    }
                }

            }
        }
        public GigeCameraModel()
        {
            Type = "GigEVision2";
            // camera = new E2VUniqua();
            //View = new GigEVision2View(this);

        }
        UserControl _view;
        public UserControl View
        {
            get
            {
                return _view;
            }
            set
            {
                if (_view != value)
                {
                    _view = value;
                    RaisePropertyChanged("View");
                }
            }
        }
        double? last_speed = 0;

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
        #endregion
        #region Method
        public void Load(DeserializeFactory item)
        {
            //throw new NotImplementedException();
            //MessageBox.Show("start");
            is_loading = true;
            HelperMethods.LoadParam(item, this);
            is_loading = false;

        }
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            HelperMethods.SaveParam(file, this);
        }
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
            try
            {

                framegrabber = new HFramegrabber("GigEVision2", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "false", "default", Device, 0, -1);
                SetFeature("SensorShutterMode", "GlobalReset");
                //Update parameter on connect
                ReloadFeatures();
               
            }
            catch (Exception ex)
            {
                if (ex is HalconException)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //CameraDisconnectedWindow wd = new CameraDisconnectedWindow();
                        //wd.Owner = Application.Current.MainWindow;
                        //wd.ShowDialog();
                    }));


                    OnCameraDisconnected?.Invoke();
                    return false;
                }
                MessageBox.Show(ex.Message);
            }

            return true;
        }

        public void UserSetLoadEvent()
        {
            SetFeature("UserSetLoad", 1);
            ReloadFeatures();
        }

        public void UserSetSaveEvent()
        {
            SetFeature("UserSetSave", 1); 
        }

        public void Start()
        {

            //if(TriggerMode == "Off") { return; }
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
                    framegrabber.GrabImageStart(-1);
                    framegrabber.SetFramegrabberParam("grab_timeout", 300);
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

                        ///test
                        if (image != null)
                        {
                            //foreach (var item in obser)
                            //{
                            //    item.OnNext(image);
                            //}
                             ImageAcquired?.Invoke(image);
                        }
                        // ImageAcquired?.Invoke(image);

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
            //throw new NotImplementedException();
            is_run = false;
            framegrabber.SetFramegrabberParam("do_abort_grab", "true");

        }
        public void Trigger()
        {
            if (is_run) { return; }
            try
            {
                //framegrabber?.SetFramegrabberParam("TriggerSoftware", 1);
                framegrabber?.GrabImageStart(-1);
                framegrabber?.SetFramegrabberParam("grab_timeout", -1);
                framegrabber?.SetFramegrabberParam("TriggerMode", "Off");
                framegrabber?.SetFramegrabberParam("AcquisitionStart", 1);
                HImage image = framegrabber?.GrabImageAsync(-1);
                //foreach (var item in obser)
                //{
                //    item.OnNext(image);
                //}
                //image.WriteImage("bmp", 0, "TestIMGgg");
                ImageAcquired?.Invoke(image);
                framegrabber?.SetFramegrabberParam("AcquisitionStart", 0);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " 1");
            }

            //throw new NotImplementedException();
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
        private void ReloadFeatures()
        {
            if (framegrabber == null)
                return;
            if (is_load)
            {
                try
                {
                    SetFeature("Gain", Gain);
                    SetFeature("ExposureTimeAbs", Exposure);
                    SetFeature("TriggerMode", TriggerMode);
                    SetFeature("TriggerDelayAbs", TriggerDelay);
                    SetFeature("TriggerSource", TriggerSource);
                    SetFeature("UserSetDefaultSelector", UserSetDefaultSelector);
                    SetFeature("UserSetSelector", UserSetSelector);

                }
                catch (Exception ex)
                {

                }
                is_load = false;
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
                    _userSetDefaultSelector = GetFeature("UserSetDefaultSelector");
                    RaisePropertyChanged("UserSetDefaultSelector");
                    _userSetSelector = GetFeature("UserSetSelector");
                    RaisePropertyChanged("UserSetSelector");

                }
                catch (Exception ex)
                {

                }
            }
            //read only values
            RaisePropertyChanged("LstTriggerSource");
            RaisePropertyChanged("LstUserSetDefaultSelector");
            RaisePropertyChanged("LstUserSetSelector");

        }
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
        public void Reject()
        {
            //throw new NotImplementedException();
            //MatroxInterfaceHook.Instance.PulseReject();
            if (framegrabber != null)
            {
                Task.Run(new Action(() =>
                {

                    //instantDoCtrl?.WriteBit(1, 0, 1);
                    framegrabber.SetFramegrabberParam("SyncOutLevels", 0);
                    Thread.Sleep(_pulse_width);
                    framegrabber.SetFramegrabberParam("SyncOutLevels", 1);
                    // timer.Start();

                    //instantDoCtrl?.WriteBit(1, 0, 0);

                }));
            }
        }
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
                if (!Directory.Exists(record_path + "\\LowSpeed"))
                {
                    Directory.CreateDirectory(record_path + "\\LowSpeed");
                }
                if (!Directory.Exists(record_path + "\\HighSpeed"))
                {
                    Directory.CreateDirectory(record_path + "\\HighSpeed");
                }
                last_record_path = record_path;
            }
            if (_record_on_speed)
            {
                if (last_speed * 60 < TransitionSpeed)
                {
                    return record_path + "\\LowSpeed";
                }
                else
                {
                    return record_path + "\\HighSpeed";
                }
            }
            else return record_path;
        }


        public void OpenImage()
        {
            string file = "";
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.BMP;*.JPG;*.GIF|All files|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                file = fileDialog.FileName;
            }
            if (System.IO.File.Exists(file))
            {

                var image = new HImage(file);
                //foreach (var item in obser)
                //{
                //    item.OnNext(image);
                //}
                ImageAcquired?.Invoke(image);
            }
            else
            {
                
            }
        }
        public double? GetFPS()
        {
            return 0;
        }

        #endregion

        #region Event Handler

        #endregion
    }
    public delegate void OnImageAcquired(HImage image);
    public delegate void OnDisconnected();
    public interface CameraInterface : IDisposable
    {

        string Type { get; }
        void Save(HFile file);
        void Load(DeserializeFactory item);
        UserControl View { get; set; }
        bool Connect();
        void Start();
        void Trigger();
        void Stop();
        OnImageAcquired ImageAcquired { get; set; }
        Nullable<double> GetFPS();
        bool IsHighSpeed { get; set; }
        void Reject();
        string TransformRecordPath(string record_path);
        bool IsRecordData();
        OnDisconnected OnCameraDisconnected { get; set; }
    }
    public class Accquisition : INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void Load(DeserializeFactory item)
        {
            //throw new NotImplementedException();
            //MessageBox.Show("start");
            HelperMethods.LoadParam(item, this);
            if (item.DeserializeTuple() == 1)
            {
                string type = item.DeserializeTuple();
                switch (type)
                {
                    case "GigEVision2": Interface = new GigeCameraModel(); break;
                    default: Interface = null; break;
                }
                if (Interface != null)
                {
                    Interface.Load(item);
                }
            }
            else
            {
                Interface = null;
            }

        }
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            HelperMethods.SaveParam(file, this);
            new HTuple(Interface == null ? 0 : 1).SerializeTuple().FwriteSerializedItem(file);
            if (Interface != null)
            {
                new HTuple(Interface.Type).SerializeTuple().FwriteSerializedItem(file);
                Interface.Save(file);
            }
        }
        CameraInterface _interface;
        public CameraInterface Interface
        {
            get
            {
                return _interface;
            }
            set
            {
                if (_interface != value)
                {
                    _interface?.Dispose();
                    _interface = value;
                    InterfaceChanged?.Invoke(this,new EventArgs());
                    RaisePropertyChanged("Interface");
                }
            }
        }

        //public HImage CurrentImage = new HImage();
        public event EventHandler InterfaceChanged;
        

    }
}
