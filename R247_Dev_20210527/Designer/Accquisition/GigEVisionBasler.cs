using Automation.BDaq;
using Basler.Pylon;
using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DevExpress.Mvvm.Native.TaskLinq;
using System.ComponentModel;
using AVT.VmbAPINET;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using MySqlX.XDevAPI.Common;
using NOVisionDesigner.Designer.Data;
using System.Reflection;
using System.Drawing.Imaging;
using System.Drawing;
using NOVisionDesigner.Windows;
using System.Diagnostics.Contracts;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class Person
    {
        public string Name;
    }
    public class GigEVisionBasler : HelperMethods, CameraInterface, INotifyPropertyChanged
    {

        public bool IsRun { get { return is_run; } }
        static GigEVisionBasler()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVisionBaslerView(), typeof(IViewFor<GigEVisionBasler>));
        }
        public void SoftwareTrigger()
        {

        }
        public void LiveView()
        {

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
                if (_trigger_source != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.TriggerSource].TrySetValue((HTuple)value))
                    {
                        _trigger_source = value;
                        RaisePropertyChanged("TriggerSource");
                    }
                }
            }
        }
        string _top;
        [HMIProperty]
        public string Top
        {
            get
            {
                return _top;
            }
            set
            {
                if (_top != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.OffsetY].TrySetValue((HTuple)value))
                    {
                        _top = value;
                        RaisePropertyChanged("Top");
                    }
                }
            }
        }
        string _left;
        public string Left
        {
            get
            {
                return _left;
            }
            set
            {
                if (_left != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.OffsetX].TrySetValue((HTuple)value))
                    {
                        _left = value;
                        RaisePropertyChanged("Left");
                    }
                }
            }
        }
        string _width;
        public string Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.Width].TrySetValue((HTuple)value))
                    {
                        _width = value;
                        RaisePropertyChanged("Width");
                    }
                }
            }
        }
        string _height;
        public string Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.Height].TrySetValue((HTuple)value))
                    {
                        _height = value;
                        RaisePropertyChanged("Height");
                    }
                }
            }
        }
        private string[] _LstTriggerSource;
        public string[] LstTriggerSource
        {            
            get
            {
                return _LstTriggerSource;
            }
            set
            {
                _LstTriggerSource = value;
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
                if (_trigger_delay != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.TriggerDelayAbs].TrySetValue((HTuple)value))
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
                if (_trigger_mode != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.TriggerMode].TrySetValue((HTuple)value))
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
                if (_exposure != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.ExposureTimeAbs].TrySetValue((HTuple)value))
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
                if (_gain != value && camera != null)
                {
                    if (camera.Parameters[PLCamera.Gain].TrySetValue((HTuple)value))
                    {
                        _gain = value;
                        RaisePropertyChanged("Gain");
                    }
                }
            }
        }

        #endregion
        #region commands
        public ICommand TriggerSoftwareCommand { get; set; }
        
        private void ExecuteMyCommand(object parameter)
        {
            RaiseTriggerSoftware();
        }
        public void RaiseTriggerSoftware()
        {
            try
            {
                camera.Parameters[PLCamera.TriggerSelector].SetValue(PLCamera.TriggerSelector.FrameStart);
                camera.Parameters[PLCamera.TriggerMode].SetValue(PLCamera.TriggerMode.On);
                camera.Parameters[PLCamera.TriggerSource].SetValue(PLCamera.TriggerSource.Software);
                IEnumParameter p = camera.Parameters[PLCamera.PixelFormat];
                camera.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.YUV422Packed);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);
                
                if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                {
                    camera.ExecuteSoftwareTrigger();
                }
                camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.Return);
                camera.StreamGrabber.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private bool CanExecuteMyCommand(object parameter)
        {
            // Kiểm tra xem lệnh có thể được thực thi hay không
            return true; // hoặc false tùy vào logic của bạn
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
                    RaisePropertyChanged("PulseWidth");
                }
            }
        }
        public HTuple GetFeature2(string paramName, bool values = false)
        {
            if (camera == null)
            {
                return 0;
            }
            else
            {
                try
                {

                    return 0;
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }
        public bool SetFeature(string paramName, HTuple value)
        {
            if (is_loading)
            {
                return true;
            }
            if (camera == null)
            {
                return false;
            }
            else
            {
                try
                {
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
            RaiseTriggerSoftware();
            return;
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
        ConcurrentQueue<(HImage, ulong)> ImageQueue = new ConcurrentQueue<(HImage, ulong)>();
        public GigEVisionBasler()
        {
            Type = "GigEVisionBasler";
            TriggerSoftwareCommand = new RelayCommand(ExecuteMyCommand, CanExecuteMyCommand);
        }
        private void ReleaseBasler()
        {
            try
            {
                ReleaseCamera();
            }
            finally
            {
                
            }
        }
        private void ReleaseCamera()
        {
            if (null != camera)
            {
                // We can use cascaded try-finally blocks to release the
                // camera step by step to make sure that every step is executed.
                try
                {
                    try
                    {
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
            if (camera != null)
            {
                DestroyCamera();
            }

            try
            {
                // Create a new camera object.
                List<ICameraInfo> cameras = CameraFinder.Enumerate();
                foreach (ICameraInfo camInfo in cameras)
                {
                    if (camInfo[CameraInfoKey.FullName] == Device)
                    {
                        camera = new Basler.Pylon.Camera(camInfo);
                    }    
                }
                camera.ConnectionLost += OnConnectionLost;
                camera.CameraOpened += OnCameraOpened;
                camera.CameraClosed += OnCameraClosed;
                camera.StreamGrabber.GrabStarted += OnGrabStarted;
                camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                camera.StreamGrabber.GrabStopped += OnGrabStopped;
                camera.Open();
                Console.WriteLine(camera.Parameters[PLCamera.TriggerMode].GetValue());
                LoadCameraSetting();

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
                    return false;
                }
                App.GlobalLogger.LogError("GigeVisionVimba", "Cannot connect camera");
            }
            
            return true;
        }
        private void OnConnectionLost(Object sender, EventArgs e)
        {
            DestroyCamera();
        }
        private void OnCameraOpened(Object sender, EventArgs e)
        {
            Console.WriteLine("Camera Opened");
        }
        private void OnCameraClosed(Object sender, EventArgs e)
        {
            Console.WriteLine("Camera Closed");
        }
        private void OnGrabStarted(Object sender, EventArgs e)
        {
            Console.WriteLine("Grab Started");
        }
        private PixelDataConverter converter = new PixelDataConverter();
        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            IGrabResult grabResult = e.GrabResult;
            try
            {
                ulong frameId = (ulong)grabResult.ID;
                    
                HImage image = new HImage();
                byte[] pixelData = (byte[])grabResult.PixelData;
                    
                IntPtr PointerAddr = grabResult.PixelDataPointer;
                int input_width = (int)grabResult.Width;
                int input_height = (int)grabResult.Height;
                


                Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);                
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);                
                converter.OutputPixelFormat = PixelType.BGRA8packed;
                IntPtr ptrBmp = bmpData.Scan0;
                converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                bitmap.UnlockBits(bmpData);
                HImage HalconImage = CvtBitmap2HImage(bitmap);
                Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    ImageAcquired?.Invoke(HalconImage, frameId);
                }, TaskCreationOptions.None);

            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                
            }
        }
        /// <summary>
        /// Convert Bitmap to HImage
        /// </summary>
        /// <param name="inputImage"></param>
        /// <returns></returns>
        public HImage CvtBitmap2HImage(Bitmap inputImage)
        {
            Rectangle rectangle = new Rectangle(0, 0, inputImage.Width, inputImage.Height);
            HImage HalconImage = new HImage();
            int w = inputImage.Width;
            int h = inputImage.Height;
            BitmapData bitmapData = inputImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            
            HalconImage.GenImageInterleaved(bitmapData.Scan0, "bgrx", w, h, -1, "byte", w, h, 0, 0, -1, 0);

            inputImage.UnlockBits(bitmapData);
            HImage outputHalconImage = HalconImage.CopyImage();
            HalconImage.Dispose();
            inputImage.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return outputHalconImage;
        }
        
        private void OnGrabStopped(Object sender, GrabStopEventArgs e)
        {
            Console.WriteLine("Grab Stopped");
        }
        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            DestroyCamera();
        }
        private void DestroyCamera()
        {
            try
            {
                if (camera != null)
                {
                    camera.StreamGrabber.Stop();
                }
            }
            catch (Exception e)
            {
                
            }

            // Destroy the camera object.
            try
            {
                if (camera != null)
                {
                    camera.Close();
                    camera.Dispose();
                    camera = null;
                }
            }
            catch (Exception e)
            {
                
            }

        }
        public void LoadCameraSetting()
        {
            if(camera == null)
            {
                return;
            }    
            try
            {
                Console.WriteLine(camera.Parameters[PLCamera.TriggerMode].GetValue());
                camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.UserSet1);
                camera.Parameters[PLCamera.UserSetSave].Execute();
                _gain = (HTuple)camera.Parameters[PLCamera.GainRaw].GetValue();
                RaisePropertyChanged("Gain");
                _exposure = (HTuple)camera.Parameters[PLCamera.ExposureTimeAbs].GetValue();
                RaisePropertyChanged("Exposure");
                _trigger_mode = camera.Parameters[PLCamera.TriggerMode].GetValue();
                RaisePropertyChanged("TriggerMode");
                _trigger_delay = (HTuple)camera.Parameters[PLCamera.TriggerDelayAbs].GetValue();
                RaisePropertyChanged("TriggerDelay");
                _trigger_source = (HTuple)camera.Parameters[PLCamera.TriggerSource].GetValue();
                RaisePropertyChanged("TriggerSource");
                _top = (HTuple)camera.Parameters[PLCamera.OffsetY].GetValue().ToString();
                RaisePropertyChanged("Top");
                _left = (HTuple)camera.Parameters[PLCamera.OffsetX].GetValue().ToString();
                RaisePropertyChanged("Left");
                _width = (HTuple)camera.Parameters[PLCamera.Width].GetValue().ToString();
                RaisePropertyChanged("Width");
                _height = (HTuple)camera.Parameters[PLCamera.Height].GetValue().ToString();
                RaisePropertyChanged("Height");
                _LstTriggerSource = getTriggerSource();
                RaisePropertyChanged(nameof(LstTriggerSource));
            }
            catch (Exception ex)
            {

            }
        }
        public void SaveCameraSetting()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.UserSet1);
                camera.Parameters[PLCamera.UserSetSave].Execute();
                camera.Parameters[PLCamera.UserSetDefaultSelector].SetValue(PLCamera.UserSetDefaultSelector.UserSet1);
            }
        }
        private void ReloadFeatures()
        {
            if (camera == null)
                return;
            if (!MainWindow.is_load)
            {
                try
                {
                    camera.Parameters[PLCamera.Gain].TrySetValue((HTuple)Gain);
                    camera.Parameters[PLCamera.ExposureTimeAbs].TrySetValue((HTuple)Exposure);
                    camera.Parameters[PLCamera.TriggerMode].TrySetValue((HTuple)TriggerMode);
                    camera.Parameters[PLCamera.TriggerDelayAbs].TrySetValue((HTuple)TriggerDelay);
                    camera.Parameters[PLCamera.TriggerSource].TrySetValue((HTuple)TriggerSource);
                    camera.Parameters[PLCamera.OffsetY].TrySetValue((HTuple)Top);
                    camera.Parameters[PLCamera.OffsetX].TrySetValue((HTuple)Left);
                    camera.Parameters[PLCamera.Width].TrySetValue((HTuple)Width);
                    camera.Parameters[PLCamera.Height].TrySetValue((HTuple)Height);
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
                    _gain = (HTuple)camera.Parameters[PLCamera.GainRaw].GetValue();
                    RaisePropertyChanged("Gain");
                    _exposure = (HTuple)camera.Parameters[PLCamera.ExposureTimeAbs].GetValue();
                    RaisePropertyChanged("Exposure");
                    _trigger_mode = (HTuple)camera.Parameters[PLCamera.TriggerMode].GetValue();
                    RaisePropertyChanged("TriggerMode");
                    _trigger_delay = (HTuple)camera.Parameters[PLCamera.TriggerDelayAbs].GetValue();
                    RaisePropertyChanged("TriggerDelay");
                    _trigger_source = (HTuple)camera.Parameters[PLCamera.TriggerSource].GetValue();
                    RaisePropertyChanged("TriggerSource");
                    _top = (HTuple)camera.Parameters[PLCamera.OffsetY].GetValue();
                    RaisePropertyChanged("Top");
                    _left = (HTuple)camera.Parameters[PLCamera.OffsetX].GetValue();
                    RaisePropertyChanged("Left");
                    _width = (HTuple)camera.Parameters[PLCamera.Width].GetValue();
                    RaisePropertyChanged("Width");
                    _height = (HTuple)camera.Parameters[PLCamera.Height].GetValue();
                    RaisePropertyChanged("Height");

                }
                catch (Exception ex)
                {

                }
            }

        }
        private string[] getTriggerSource()
        {
            List<string> list = new List<string>();
            if (camera != null)
            {
                var triggerSourceEnum = camera.Parameters[PLCamera.TriggerSource].GetAllValues();
                foreach (var triggerSource in triggerSourceEnum)
                {
                    list.Add(triggerSource);
                }
            }
            return list.ToArray();
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
        Basler.Pylon.Camera camera;
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            SaveParam(file, this);
        }
        bool is_run = false;
        public void OnIOCallback(object channel, EventArgs e)
        {
            //if (UsbTrigger)
            //{
            //    try
            //    {
            //        if (_trigger_delay >= 1000)
            //        {
            //            Thread.Sleep((int)(_trigger_delay / 1000));
            //        }
            //        framegrabber?.SetFramegrabberParam("TriggerSoftware", 1);
            //    }
            //    catch (Exception ex) { }
            //}
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
            }
           ));
        }



        public void Stop()
        {
            if (null == camera)
            {
                App.GlobalLogger.LogError("Basler", "No camera open.");
            }
            try
            {
                if (camera != null)
                {
                    camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;
                    camera.StreamGrabber.Stop();
                }
            }
            catch (Exception e)
            {
                App.GlobalLogger.LogError("Basler", e.ToString());
            }
        }

        public void Dispose()
        {
            
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
