using HalconDotNet;
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
using AVT.VmbAPINET;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DevExpress.Data.Async.Helpers;
using Newtonsoft.Json;
using SharpCompress.Common;
using Newtonsoft.Json.Linq;
using DevExpress.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Windows.Documents;
using System.Xml.Linq;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using NOVisionDesigner.Designer.Accquisition.Windows;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class GigEVisionVimba : BaseVimbaInterface
    {
        
        static GigEVisionVimba()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVisionVimbaView(), typeof(IViewFor<GigEVisionVimba>));
            
        }
        #region properties
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
                    //if (Device != null)
                    //{
                    //    Connected = Connect();
                    //}
                    RaisePropertyChanged("Device");
                }
            }
        }


        #endregion

        public GigEVisionVimba(string basedir):base(basedir)
        {
            Type = "GigEVisionVimba";
            CommonFeatures = new ObservableCollection<string>() { "TriggerMode", "TriggerDelayAbs", "TriggerSource", "ExposureTimeAbs", "Gain", "OffsetX", "OffsetY", "Width", "Height" };
            LoadCommonFeatures();
        }
        
        public override void OnFrameReceived(Frame frame)
        {
            try
            {
                if (frame.ReceiveStatus == VmbFrameStatusType.VmbFrameStatusIncomplete)
                {
                    //camera.QueueFrame(frame);
                    return;
                }
                // Convert frame into displayable image
                //OutPutFrameInfo(frame);
                ulong frameId = 0;
                //var ancillaryData = frame.AncillaryData;
                //ancillaryData.Open();
                //try
                //{
                //    var features = ancillaryData.Features;
                //    frameId = (ulong)features["ChunkAcquisitionFrameCount"].IntValue;

                //}
                //finally { ancillaryData.Close(); }
                frameId = (ulong)(frame.AncillaryData.Buffer[0] << 24 | frame.AncillaryData.Buffer[1] << 16 | frame.AncillaryData.Buffer[2] << 8 | frame.AncillaryData.Buffer[3]);
                HImage image = new HImage();
                byte[] array_input = frame.Buffer;
                int input_width = (int)frame.Width;
                int input_height = (int)frame.Height;
                unsafe
                {
                    fixed (byte* pointer = &frame.Buffer[0])
                    {
                        image.GenImageInterleaved((IntPtr)pointer, "rgb", input_width, input_height, 0, "byte", input_width, input_height, 0, 0, 8, 0);
                        //ImageQueue.Enqueue((image, frameId));
                        Task.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Priority = ThreadPriority.Highest;
                            ImageAcquired?.Invoke(image, frameId);
                        }, TaskCreationOptions.None);

                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(VimbaException))
                {
                    Console.WriteLine((ex as VimbaException).ToString());
                }
            }
            finally
            {
                // We make sure to always return the frame to the API
                try
                {

                    camera.QueueFrame(frame);
                }
                catch (Exception ex)
                {
                    // Do nothing
                }
            }
        }

        public static int TotalReconnection=10;
        public override bool Connect()
        {
            // framegrabber = new HFramegrabber("GenICamTL", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "false", "default", "default", 0, -1);
            try
            {
                if (camera != null)
                {
                    camera.Close();
                    camera= null;
                }

                // framegrabber.CloseFramegrabber();
            }
            catch (Exception ex)
            {

            }
            int currentTry = 0;
            while (currentTry < TotalReconnection)
            {
                try
                {
                    camera = VimbaSystem.OpenCameraByID(Device, VmbAccessModeType.VmbAccessModeFull);
                    break;
                }
                catch (Exception ex)
                {

                }
                Thread.Sleep(2000);
                currentTry++;
            }
            if (camera == null)
            {
                return false;
            }
            try
            {
                
                if (camera != null)
                {

                    camera.Features["ChunkModeActive"].BoolValue = true;
                    camera.Features["PixelFormat"].EnumValue = "RGB8Packed";
                    //SetFeature("ChunkModeActive", new HTuple(true));
                    //SetFeature("PixelFormat", "RGB8Packed");
                }
                //Update parameter on connect
                if (camera != null)
                {
                    ListFeatures.Clear();
                    //ListCommonFeatures.Clear();
                    foreach (Feature f in camera.Features)
                    {
                        var feature = new CustomVimbaFeature(f);
                        ListFeatures.Add(feature);
                        
                        if (feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsVolatile))
                        {
                            var fswCreated = Observable.FromEvent<Feature.OnFeatureChangeHandler, Feature>(handler =>
                            {
                                Feature.OnFeatureChangeHandler fsHandler = (sender) =>
                                {
                                    handler(sender);
                                };

                                return fsHandler;
                            },
                            fsHandler => f.OnFeatureChanged += fsHandler,
                            fsHandler => f.OnFeatureChanged -= fsHandler);
                                fswCreated.Throttle(TimeSpan.FromMilliseconds(200)).ObserveOn(Application.Current.Dispatcher).Subscribe(e =>
                            {
                                FeatureChanged(e);

                            });
                        }
                    }
                }
                if (Version == "1.0")
                {
                    ReloadFeatures();
                }
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
                    App.GlobalLogger.LogError("GigeVisionVimba", "Cannot connect camera");

                    OnCameraDisconnected?.Invoke();
                    //Connected = Disconnect();
                    return false;
                }
                App.GlobalLogger.LogError("GigeVisionVimba", "Cannot connect camera");
                //MessageBox.Show(ex.Message);
                OnCameraDisconnected?.Invoke();
                Connected = Disconnect();
                return false;
            }

            return true;
        }
        private void FeatureChanged(Feature feature)
        {
            //lock (feature_lock)
            //{
            //    UpdateFeatureValue(feature);
            //    Console.WriteLine(feature.Name);
            //}
            UpdateFeatureValue(feature);
            //Console.WriteLine(feature.Name);
        }
        private void UpdateFeatureValue(Feature f)
        {
            try
            {
                if (f.Visibility == VmbFeatureVisibilityType.VmbFeatureVisibilityUnknown ||
                    f.Visibility == VmbFeatureVisibilityType.VmbFeatureVisibilityInvisible)
                    return;
                var index = ListFeatures.FirstOrDefault(x => x.Name == f.Name);
                if (index == null) return;
                index.Flags = CustomVimbaFeature.DecodeVmbFeatureFlags((int)f.Flags);
                if (index.Flags.Any(x => x == VmbFeatureFlagsType.VmbFeatureFlagsNone)) return;
                switch (f.DataType)
                {
                    case VmbFeatureDataType.VmbFeatureDataInt:
                        index.IntValue = f.IntValue;
                        index.IntRangeMax = f.IntRangeMax;
                        index.IntRangeMin = f.IntRangeMin;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataFloat:
                        index.FloatValue = f.FloatValue;
                        index.FloatRangeMax = f.FloatRangeMax;
                        index.FloatRangeMin = f.FloatRangeMin;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataEnum:
                        index.EnumValue = f.EnumValue;
                        index.EnumValues.Clear();
                        foreach (string val in f.EnumValues)
                        {
                            if (f.IsEnumValueAvailable(val)) index.EnumValues.Add(val);
                        }
                        //ListFeatures[index].EnumValues = f.EnumValues;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataString:
                        index.StringValue = f.StringValue;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataBool:
                        index.BoolValue = f.BoolValue;
                        break;
                }

            }
            catch { }
        }
        
        public override void ReloadFeatures()
        {
            if (camera == null)
                return;
            try
            {
                for(int i=0; i< ListCommonFeatures.Count; i++)
                {
                    if (camera.Features.ContainsName(ListCommonFeatures[i].Name))
                    {
                        object value;
                        switch (ListCommonFeatures[i].DataType)
                        {
                            case VmbFeatureDataType.VmbFeatureDataBool:
                                value = ListCommonFeatures[i].BoolValue;
                                break;
                            case VmbFeatureDataType.VmbFeatureDataEnum:
                                value = ListCommonFeatures[i].EnumValue;
                                break;
                            case VmbFeatureDataType.VmbFeatureDataFloat:
                                value = ListCommonFeatures[i].FloatValue;
                                break;
                            case VmbFeatureDataType.VmbFeatureDataInt:
                                value = ListCommonFeatures[i].IntValue;
                                break;
                            case VmbFeatureDataType.VmbFeatureDataString:
                                value = ListCommonFeatures[i].StringValue;
                                break;
                            default:
                                value = 0;
                                break;
                        }
                        ListCommonFeatures[i] = ListFeatures.FirstOrDefault(x=>x.Name==ListCommonFeatures[i].Name);
                        SetAndUpdateFeatureValue(ListCommonFeatures[i], value);
                        //SetFeature(f.Name, value);

                        //var feature = ListFeatures.FirstOrDefault(x => x.Name == f.Name);
                        //if (feature != null)
                        //    ListCommonFeatures.Add(feature);
                    }
                }
                //SetFeature("Gain", Gain);
                //SetFeature("ExposureTimeAbs", Exposure);
                //if(TriggerMode!=null)
                //    SetFeature("TriggerMode", TriggerMode);
                //SetFeature("TriggerDelayAbs", TriggerDelay);
                //if (TriggerSource != null)
                //    SetFeature("TriggerSource", TriggerSource);
                //SetFeature("OffsetX", Top);
                //SetFeature("OffsetY", Left);
                //SetFeature("Width", Width);
                //SetFeature("Height", Height);

            }
            catch (Exception ex)
            {

            }
            MainWindow.is_load = false;


            //read only values
            RaisePropertyChanged("LstTriggerSource");

        }
        public override string GetDevice()
        {
            return Device;
        }
        public override void SetDevice(string device)
        {
            Device = device;
        }
        public override void LiveView()
        {
            base.LiveView();
            var wd = new VimbaLiveViewWindow(this);
            wd.ShowDialog();
        }
    }
    public class BaseVimbaInterface : HelperMethods, CameraInterface, INotifyPropertyChanged
    {
        public virtual void LiveView()
        {

        }
        public void SoftwareTrigger()
        {

        }
        public static Vimba VimbaSystem;
        public static List<string> CameraList = new List<string>();
        //public object feature_lock = new object();
        public static void StartVimbaSystem()
        {
            try
            {
                if (VimbaSystem == null)
                {
                    VimbaSystem = new Vimba();
                    VimbaSystem.Startup();

                    VimbaSystem.OnCameraListChanged += VimbaSystem_OnCameraListChanged;
                }
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("VimbaSystem", ex.Message);
            }

        }

        public static void VimbaSystem_OnCameraListChanged(VmbUpdateTriggerType reason)
        {
            var newCameraList = new List<string>();
            foreach (Camera camera in VimbaSystem.Cameras)
            {
                try
                {
                    newCameraList.Add(camera.Id);
                }
                catch (VimbaException ve)
                {
                    App.GlobalLogger.LogError("VimbaSystem", "New camera plug in");
                }

            }
            //check camera disconnect from list
            var disconnectList = new List<string>();
            foreach (var camera in CameraList)
            {
                if (!newCameraList.Contains(camera))
                {
                    disconnectList.Add(camera);
                }
            }
            //check camera plug in
            var newCameraPlugIn = new List<string>();
            foreach (var camera in newCameraList)
            {
                if (!CameraList.Contains(camera))
                {
                    newCameraPlugIn.Add(camera);
                }
            }
            CameraList = new List<string>();
            foreach (Camera camera in VimbaSystem.Cameras)
            {
                try
                {
                    CameraList.Add(camera.Id);
                }
                catch (VimbaException ve)
                {
                    App.GlobalLogger.LogError("VimbaSystem", ve.Message);
                }

            }
            switch (reason)
            {
                case VmbUpdateTriggerType.VmbUpdateTriggerPluggedIn:
                    App.GlobalLogger.LogWarning("VimbaSystem", "New camera plug in");
                    break;
                case VmbUpdateTriggerType.VmbUpdateTriggerPluggedOut:
                    App.GlobalLogger.LogWarning("VimbaSystem", "Camera plug out");
                    break;
                case VmbUpdateTriggerType.VmbUpdateTriggerOpenStateChanged:
                    App.GlobalLogger.LogWarning("VimbaSystem", "Current camera change state (open or close)");
                    break;
            }
            if (newCameraPlugIn.Count > 0)
            {
                App.GlobalLogger.LogWarning("VimbaSystem", "New camera plug in: " + string.Format("[{0}]", String.Join(",", newCameraPlugIn)));
            }
            if (disconnectList.Count > 0)
            {
                App.GlobalLogger.LogWarning("VimbaSystem", "Camera disconnect: " + string.Format("[{0}]", String.Join(",", disconnectList)));
            }


        }
        public string basedir;
        public BaseVimbaInterface(string basedir)
        {
            this.basedir = basedir;
            StartVimbaSystem();
        }
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
        int _top;
        [HMIProperty]
        public int Top
        {
            get
            {
                return _top;
            }
            set
            {
                if (_top != value)
                {
                    if (SetFeature("OffsetX", value))
                    {
                        _top = value;
                        RaisePropertyChanged("Top");
                    }
                }
            }
        }
        int _left;
        public int Left
        {
            get
            {
                return _left;
            }
            set
            {
                if (_left != value)
                {
                    if (SetFeature("OffsetY", value))
                    {
                        _left = value;
                        RaisePropertyChanged("Left");
                    }
                }
            }
        }
        int _width=400;
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value)
                {
                    if (SetFeature("Width", value))
                    {
                        _width = value;
                        RaisePropertyChanged("Width");
                    }
                }
            }
        }
        int _height =400;
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    if (SetFeature("Height", value))
                    {
                        _height = value;
                        RaisePropertyChanged("Height");
                    }
                }
            }
        }
        public string[] LstTriggerSource
        {
            get
            {
                return null;
                //return GetFeature("TriggerSource",true).ToSArr();
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
                        RaisePropertyChanged("TriggerMode");
                    }
                }
            }
        }
        double _exposure = 1000;
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
        #endregion
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
        bool _connected;
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    RaisePropertyChanged("Connected");

                }
            }
        }
        public bool IsHighSpeed { get; set; } = true;
        
        

        
        ObservableCollection<CustomVimbaFeature> _listFeatures = new ObservableCollection<CustomVimbaFeature>();
        public ObservableCollection<CustomVimbaFeature> ListFeatures
        {
            get
            {
                return _listFeatures;
            }
            set
            {
                if (_listFeatures != value)
                {
                    _listFeatures = value;
                    RaisePropertyChanged("ListFeatures");
                }
            }
        }
        public List<string> IgnoredUpdateFeature;
        public List<string> SlowUpdateFeature;
        public ObservableCollection<string> _commonFeatures;
        public ObservableCollection<string> CommonFeatures
        {
            get
            {
                return _commonFeatures;
            }
            set
            {
                if (_commonFeatures != value)
                {
                    _commonFeatures = value;
                    RaisePropertyChanged("CommonFeatures");
                }
            }
        }
        public object featureCommonLock = new object();
        public object featureLock = new object();
        ObservableCollection<CustomVimbaFeature> _listCommonFeatures = new ObservableCollection<CustomVimbaFeature>();
        public ObservableCollection<CustomVimbaFeature> ListCommonFeatures
        {
            get
            {
                return _listCommonFeatures;
            }
            set
            {
                if (_listCommonFeatures != value)
                {
                    _listCommonFeatures = value;
                    RaisePropertyChanged("ListCommonFeatures");
                }
            }
        }
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
                        SetFeature("TriggerSoftware", 1);
                    }
                    catch (Exception ex) { }
                }, () => true));
            }
        }
        #endregion
       
        public object GetFeature(string paramName)
        {
            if (camera == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    Feature feature = camera.Features[paramName];
                    try
                    {
                        switch (feature.DataType)
                        {
                            case VmbFeatureDataType.VmbFeatureDataBool:
                                return feature.BoolValue;
                            case VmbFeatureDataType.VmbFeatureDataEnum:
                                return feature.EnumValue;
                            case VmbFeatureDataType.VmbFeatureDataFloat:
                                return feature.FloatValue;
                            case VmbFeatureDataType.VmbFeatureDataInt:
                                return feature.IntValue;
                            case VmbFeatureDataType.VmbFeatureDataString:
                                return feature.StringValue;
                            default:
                                return 0;
                        }
                    }
                    catch (VimbaException ve)
                    {
                        return ve.Message;
                    }
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }
        public bool SetFeature(string paramName, object value)
        {
            //if (is_loading)
            //{
            //    return true;
            //}
            if (camera == null)
            {
                return false;
            }
            else
            {
                try
                {
                    Feature f = camera.Features[paramName];
                    if (!f.IsWritable()) return false;
                    switch (f.DataType)
                    {
                        case VmbFeatureDataType.VmbFeatureDataBool:
                            f.BoolValue = (bool)value;
                            break;
                        case VmbFeatureDataType.VmbFeatureDataEnum: 
                            f.EnumValue = (string)value;
                            break;
                        case VmbFeatureDataType.VmbFeatureDataCommand:
                            f.RunCommand();
                            break;
                        case VmbFeatureDataType.VmbFeatureDataFloat:
                            f.FloatValue = (double)value;
                            break;
                        case VmbFeatureDataType.VmbFeatureDataInt:
                            f.IntValue = (long)value;
                            break;
                        case VmbFeatureDataType.VmbFeatureDataString:
                            f.StringValue = (string)value;
                            break;
                        default:
                            break;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }

            }
        }
        public virtual bool SetAndUpdateFeatureValue(CustomVimbaFeature feature, object value)
        {
            if (!feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsWrite) && 
                !feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsModifyWrite))
                return true;
            //if (!SetFeature(feature.Name, value))
            //    return false;
            //return UpdateFeatureValue(feature);
            SetFeature(feature.Name, value);
            UpdateAffectedFeatureValue(feature);
            return true;

        }
        private bool UpdateFeatureValue(CustomVimbaFeature feature)
        {
            try
            {
                if (feature.Visibility == VmbFeatureVisibilityType.VmbFeatureVisibilityUnknown ||
                    feature.Visibility == VmbFeatureVisibilityType.VmbFeatureVisibilityInvisible)
                    return true;
                var f = camera.Features[feature.Name];
                var index = ListFeatures.FirstOrDefault(x => x.Name == feature.Name);
                if (index == null)
                    return true;
                index.Flags = CustomVimbaFeature.DecodeVmbFeatureFlags((int)f.Flags);
                if (index.Flags.Any(x => x == VmbFeatureFlagsType.VmbFeatureFlagsNone))
                    return true;
                switch (f.DataType)
                {
                    case VmbFeatureDataType.VmbFeatureDataInt:
                        index.IntValue = f.IntValue;
                        index.IntRangeMax = f.IntRangeMax;
                        index.IntRangeMin = f.IntRangeMin;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataFloat:
                        index.FloatValue = f.FloatValue;
                        index.FloatRangeMax = f.FloatRangeMax;
                        index.FloatRangeMin = f.FloatRangeMin;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataEnum:
                        index.EnumValue = f.EnumValue;
                        index.EnumValues.Clear();
                        foreach (string val in f.EnumValues)
                        {
                            if (f.IsEnumValueAvailable(val)) index.EnumValues.Add(val);
                        }
                        break;
                    case VmbFeatureDataType.VmbFeatureDataString:
                        index.StringValue = f.StringValue;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataBool:
                        index.BoolValue = f.BoolValue;
                        break;
                }

            }
            catch (Exception ex)
            {

            }
            return true;
        }
        public virtual void UpdateAffectedFeatureValue(CustomVimbaFeature feature)
        {
            try
            {
                foreach (AVT.VmbAPINET.Feature d in feature.AffectedFeatures)
                {
                    var f = ListFeatures.FirstOrDefault(x => x.Name == d.Name);
                    if (f != null) UpdateFeatureValue(f);
                }

            }
            catch(Exception ex)
            {

            }
        }
        public void Trigger()
        {
            //throw new NotImplementedException();
        }
        public OnDisconnected OnCameraDisconnected { get; set; }
        public bool IsRecordData()
        {
            //if (_record_data_on_minimum)
            //    if (last_speed * 60 > MinimumDataSpeed)
            //    {
            //        return true;
            //    }
            //    else
            //        return false;
            //else
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
        private void ReleaseVimba()
        {

                try
                {
                    // First we release the camera (if there is one)
                    ReleaseCamera();
                }
                finally
                {
                    // Now finally shutdown the API
                        
                }

            
        }
        public void ReleaseCamera()
        {
            if (null != camera)
            {
                // We can use cascaded try-finally blocks to release the
                // camera step by step to make sure that every step is executed.
                try
                {
                    try
                    {
                        if(is_run) 
                            Stop();
                    }
                    finally
                    {
                        is_run = false;
                        camera.Close();
                    }
                }
                finally
                {
                    camera = null;
                }
            }
        }
        public virtual void OnFrameReceived(Frame frame) { }
        
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public OnImageAcquired ImageAcquired { get; set; }

        public virtual bool Connect()
        {
            
            return true;
        }
        public void LoadCameraSetting()
        {
            SetFeature("UserSetSelector", "UserSet1");
            SetFeature("UserSetLoad", 1);
            try
            {
                //_gain = GetFeature("Gain");
                //RaisePropertyChanged("Gain");
                //_exposure = GetFeature("ExposureTimeAbs");
                //RaisePropertyChanged("Exposure");
                //_trigger_mode = GetFeature("TriggerMode");
                //RaisePropertyChanged("TriggerMode");
                //_trigger_delay = GetFeature("TriggerDelayAbs");
                //RaisePropertyChanged("TriggerDelay");
                //_trigger_source = GetFeature("TriggerSource");
                //RaisePropertyChanged("TriggerSource");
                //_top = GetFeature("X");
                //RaisePropertyChanged("Top");
                //_left = GetFeature("Y");
                //RaisePropertyChanged("Left");
                //_width = GetFeature("Width");
                //RaisePropertyChanged("Width");
                //_height = GetFeature("Height");
                //RaisePropertyChanged("Height");

            }
            catch (Exception ex)
            {

            }
        }
        public void SaveCameraSetting()
        {
            SetFeature("UserSetSelector", "UserSet1");
            SetFeature("UserSetSave", 1);
            
        }
        public bool Disconnect()
        {
            var is_connected = true;
            try
            {
                ReleaseCamera();
                //SetDevice(null);
                //ListFeatures.Clear();
                //ListCommonFeatures.Clear();
                is_connected = false;
                return is_connected;
            }
            catch
            {
                return is_connected;
            }
        }
        public virtual void ReloadFeatures()
        {
            
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
        public Camera camera;
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            SaveParam(file, this);
            SaveCommonFeatures();
        }
        public string Version { get; set; } = "1.0";
        public void LoadCommonFeatures()
        {
            string file_path = "";
            if (this.Type == "GigEVisionVimba")
                file_path = System.IO.Path.Combine(basedir, "CommonFeatures.json");
            else if (this.Type == "USB3VisionVimba")
                file_path = System.IO.Path.Combine(basedir, "USB3VisionVimbaCommonFeatures.json");
            Version = "1.1";
            if (System.IO.File.Exists(file_path))
            {
                using (StreamReader r = new StreamReader(file_path))
                {
                    var fileData = r.ReadToEnd();
                    if (fileData != "")
                    {
                        var objData = JsonConvert.DeserializeObject<List<CustomVimbaFeature>>(fileData);
                        foreach (var data in objData)
                        {
                            data.Flags = new List<VmbFeatureFlagsType>() { VmbFeatureFlagsType.VmbFeatureFlagsRead };
                            ListCommonFeatures.Add(data);
                        }
                    }
                }
            }
            else
            {
                //foreach (var name in CommonFeatures)
                //{
                //    var feature = ListFeatures.FirstOrDefault(x => x.Name == name);
                //    if (feature != null)
                //        ListCommonFeatures.Add(feature);
                //}
            }
        }
        private void SaveCommonFeatures()
        {
            string file_path = "";
            if (this.Type == "GigEVisionVimba")
                file_path = System.IO.Path.Combine(basedir, "CommonFeatures.json");
            else if (this.Type == "USB3VisionVimba")
                file_path = System.IO.Path.Combine(basedir, "USB3VisionVimbaCommonFeatures.json");
            if(string.IsNullOrEmpty(file_path)) { return; }
            try
            {
                System.IO.File.WriteAllText(file_path, SerializeFeatures());
            }
            catch (Exception ex)
            {

            }
        }
        private string SerializeFeatures()
        {
            return JsonConvert.SerializeObject(ListCommonFeatures.ToList(), Formatting.Indented);
        }
        public virtual string GetDevice() { return null; }
        public virtual void SetDevice(string device) { }
        public virtual string GetInterfaceType() { return null; }
        public virtual void SetInterfaceType(string type) { }
        public bool is_run = false;
        public void OnIOCallback(object channel, EventArgs e)
        {

        }
        public void Start()
        {


            Task.Run(new Action(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                if (camera == null)
                {
                    this.Connect();
                }
                try
                {
                    if (camera == null)
                        return;
                    if (UsbTrigger)
                    {

                    }

                    //reset reject
                    try
                    {
                        //SetFeature("SyncOutLevels", 0);
                    }
                    catch (Exception ex)
                    {

                    }

                    bool error = true;
                    try
                    {
                        // Register frame callback
                        camera.OnFrameReceived += this.OnFrameReceived;

                        is_run = true;
                        // Start synchronous image acquisition (grab)
                        camera.StartContinuousImageAcquisition(3);

                        error = false;
                    }
                    finally
                    {
                        // Close camera already if there was an error
                        if (true == error)
                        {
                            ReleaseCamera();
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
            //throw new NotImplementedException();
            if (UsbTrigger)
            {
                //ioclient?.StopDI();
            }

            IsRun = false;
            // Check if API has been started up at all
            if (null == VimbaSystem)
            {
                App.GlobalLogger.LogError("VimbaSystem", "Vimba is not started.");
                return;
            }

            // Check if no camera is open
            if (null == camera)
            {
                App.GlobalLogger.LogError("VimbaCamera", "No camera open.");
                return;
            }

            // Close camera
            try
            {
                camera.OnFrameReceived -= this.OnFrameReceived;
                camera.StopContinuousImageAcquisition();

            }
            catch
            {
            }

        }
        public void Dispose()
        {
            ReleaseVimba();
        }
        public double? GetFPS()
        {
            return 0;
        }
        public void PulseOutput()
        {



        }
        public void Reject()
        {
           
        }
        public string TransformRecordPath(string record_path)
        {
            throw new NotImplementedException();
        }
    }
    public class SerializedVimbaFeature
    {
        public string Name;
        public object Value;
    }
    [DataContract]
    public class CustomVimbaFeature : ReactiveObject
    {
        public CustomVimbaFeature() { }
        public CustomVimbaFeature(Feature f)
        {
            Name = f.Name;
            Description = f.Description;
            if (f.AffectedFeatures != null) AffectedFeatures = f.AffectedFeatures;

            Tooltip = f.ToolTip;
            Visibility = f.Visibility;
            Category = f.Category;
            Flags = DecodeVmbFeatureFlags((int)f.Flags);
            DataType = f.DataType;
            if (!f.IsReadable()) return;
            try
            {
                switch (f.DataType)
                {
                    case VmbFeatureDataType.VmbFeatureDataInt:
                        IntValue = f.IntValue;
                        if (Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsConst)) break;
                        IntRangeMax = f.IntRangeMax;
                        IntRangeMin = f.IntRangeMin;
                        IntIncrement = f.IntIncrement;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataFloat:
                        FloatValue = f.FloatValue;
                        if (Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsConst)) break;
                        FloatRangeMax = f.FloatRangeMax;
                        FloatRangeMin = f.FloatRangeMin;
                        try
                        {
                            if (f.FloatHasIncrement)
                                FloatIncrement = f.FloatIncrement;
                        }
                        catch
                        {
                            FloatIncrement = 1;
                        }
                         
                        break;
                    case VmbFeatureDataType.VmbFeatureDataEnum:
                        EnumValue = f.EnumValue;
                        EnumValues = new List<string>();
                        foreach (string val in f.EnumValues)
                        {
                            if (f.IsEnumValueAvailable(val)) EnumValues.Add(val);
                        }
                        //EnumValues = f.EnumValues;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataString:
                        StringValue = f.StringValue;
                        break;
                    case VmbFeatureDataType.VmbFeatureDataBool:
                        BoolValue = f.BoolValue;
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                
                DataType = VmbFeatureDataType.VmbFeatureDataString;
                StringValue = "Unknown";
                Flags = DecodeVmbFeatureFlags(1);
            }
            
        }
        #region VimbaFeatureProperties
        string _name;
        [DataMember]
        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }
        string _description;
        [DataMember]
        public string Description
        {
            get { return _description; }
            set { this.RaiseAndSetIfChanged(ref _description, value); }
        }
        FeatureCollection _affectedFeatures;
        [IgnoreDataMember]
        public FeatureCollection AffectedFeatures
        {
            get { return _affectedFeatures; }
            set { this.RaiseAndSetIfChanged(ref _affectedFeatures, value); }
        }
        string _tooltip;
        [DataMember]
        public string Tooltip
        {
            get { return _tooltip; }
            set { this.RaiseAndSetIfChanged(ref _tooltip, value); }
        }
        VmbFeatureVisibilityType _visibility;
        [IgnoreDataMember]
        public VmbFeatureVisibilityType Visibility
        {
            get { return _visibility; }
            set { this.RaiseAndSetIfChanged(ref _visibility, value); }
        }
        string _category;
        [DataMember]
        public string Category
        {
            get { return _category; }
            set { this.RaiseAndSetIfChanged(ref _category, value); }
        }
        List<VmbFeatureFlagsType> _flags;
        public List<VmbFeatureFlagsType> Flags
        {
            get { return _flags; }
            set { this.RaiseAndSetIfChanged(ref _flags, value); }
        }
        VmbFeatureDataType _dataType;
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public VmbFeatureDataType DataType
        {
            get { return _dataType; }
            set { this.RaiseAndSetIfChanged(ref _dataType, value); }
        }
        bool _boolValue;
        [DataMember]
        public bool BoolValue
        {
            get { return _boolValue; }
            set { this.RaiseAndSetIfChanged(ref _boolValue, value); }
        }
        string _stringValue;
        [DataMember]
        public string StringValue
        {
            get { return _stringValue; }
            set { this.RaiseAndSetIfChanged(ref _stringValue, value); }
        }
        List<string> _enumValues;
        [DataMember]    
        public List<string> EnumValues
        {
            get { return _enumValues; }
            set { this.RaiseAndSetIfChanged(ref _enumValues, value); }
        }
        string _enumValue;
        [DataMember]
        public string EnumValue
        {
            get { return _enumValue; }
            set { this.RaiseAndSetIfChanged(ref _enumValue, value); }
        }
        double _floatValue;
        [DataMember]
        public double FloatValue
        {
            get { return _floatValue; }
            set { this.RaiseAndSetIfChanged(ref _floatValue, value); }
        }
        long _intValue;
        [DataMember]
        public long IntValue
        {
            get { return _intValue; }
            set { this.RaiseAndSetIfChanged(ref _intValue, value); }
        }
        double _floatRangeMax;
        [DataMember]
        public double FloatRangeMax
        {
            get { return _floatRangeMax; }
            set { this.RaiseAndSetIfChanged(ref _floatRangeMax, value); }
        }
        double _floatRangeMin;
        [DataMember]
        public double FloatRangeMin
        {
            get { return _floatRangeMin; }
            set { this.RaiseAndSetIfChanged(ref _floatRangeMin, value); }
        }
        long _intRangeMax;
        [DataMember]
        public long IntRangeMax
        {
            get { return _intRangeMax; }
            set { this.RaiseAndSetIfChanged(ref _intRangeMax, value); }
        }
        long _intRangeMin;
        [DataMember]
        public long IntRangeMin
        {
            get { return _intRangeMin; }
            set { this.RaiseAndSetIfChanged(ref _intRangeMin, value); }
        }
        long _intIncrement;
        [DataMember]
        public long IntIncrement
        {
            get { return _intIncrement; }
            set { this.RaiseAndSetIfChanged(ref _intIncrement, value); }
        }
        double _floatIncrement;
        [DataMember]
        public double FloatIncrement
        {
            get { return _floatIncrement; }
            set { this.RaiseAndSetIfChanged(ref _floatIncrement, value); }
        }
        #endregion
        private static List<int> SplitIntoPowersOfTwo(int num)
        {
            var result = new List<int>();
            if (num == 0)
            {
                result.Add(0);
                return result;
            }
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & num) != 0)
                {
                    result.Add(1 << i);
                }
            }
            return result;
        }

        private static List<VmbFeatureFlagsType> SplitFlagsIntoEnum(int num)
        {
            var list_enums = new List<VmbFeatureFlagsType>();
            foreach (var val in SplitIntoPowersOfTwo(num))
            {
                list_enums.Add((VmbFeatureFlagsType)val);
            }
            return list_enums;
        }
        public static List<VmbFeatureFlagsType> DecodeVmbFeatureFlags(int num)
        {
            //num &= ~4;
            return SplitFlagsIntoEnum(num);
        }
    }
}
