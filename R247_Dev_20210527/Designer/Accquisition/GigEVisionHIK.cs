using Basler.Pylon;
using HalconDotNet;
using MvCamCtrl.NET;
using MvCamCtrl.NET.CameraParams;
using NOVisionDesigner.Designer.Misc;
using OpenCvSharp.WpfExtensions;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class GigEVisionHIK : CameraHelper, CameraInterface
    {
        static GigEVisionHIK()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVisionHIKView(), typeof(IViewFor<GigEVisionHIK>));
        }
        public GigEVisionHIK()
        {
            Type = "GigEVisionHIK";            
            //TriggerSoftwareCommand = new RelayCommand(ExecuteMyCommand, CanExecuteMyCommand);
        }
        public void SoftwareTrigger()
        {
        }
        public void LiveView()
        {
        }
        public OnImageAcquired ImageAcquired { get; set; }
        bool is_run = false;
        private CCamera camera;
        public bool IsRun { get { return is_run; } }
        #region gige properties
        string _trigger_source;
        Thread m_hReceiveThread = null;
        System.Drawing.Imaging.PixelFormat m_enBitmapPixelFormat = System.Drawing.Imaging.PixelFormat.DontCare;
        /*public event PropertyChangedEventHandler PropertyChanged;
        
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }*/
        public string TriggerSource
        {
            get
            {
                return _trigger_source;
            }
            set
            {
                if(value != _trigger_source)
                {
                    _trigger_source = value;
                    switch(_trigger_source)
                    {
                        case "Software":
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            break;
                        case "Line0":
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            break;
                        case "Line1":
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE1);
                            break;
                        case "Line2":
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE2);
                            break;
                        case "Line3":
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE3);
                            break;
                        default:
                            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            break;
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
                    _top = value;
                    RaisePropertyChanged(nameof(Top));
                    int nRet = camera.SetIntValue("OffsetY", Int32.Parse(_top));
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set top fail!");
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
                    _left = value;
                    RaisePropertyChanged(nameof(Left));
                  
                    int nRet = camera.SetIntValue("OffsetX", Int32.Parse(_left));
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set left fail!");
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
                    _width = value;
                    RaisePropertyChanged(nameof(Width));
                    int nRet = camera.SetIntValue("Width", Int32.Parse(_width));
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set left fail!");
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
                    _height = value;
                    RaisePropertyChanged(nameof(Height));
                    int nRet = camera.SetIntValue("Height", Int32.Parse(_height));
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set left fail!");
                    }
                }
            }
        }
        private string[] _LstTriggerSource = new string[] { "Software", "Line0", "Line1", "Line2", "Line3" };
        public string[] LstTriggerSource
        {
            get
            {
                return _LstTriggerSource;
            }
            set
            {
                if (_LstTriggerSource != value && camera != null)
                {
                    _LstTriggerSource = value;
                    RaisePropertyChanged(nameof(LstTriggerSource));
                }
            }
        }

        

        uint _trigger_mode = 0;
        public uint TriggerMode
        {
            get
            {
                return _trigger_mode;
            }
            set
            {
                if (_trigger_mode != value && camera != null)
                {
                    _trigger_mode = value;
                    int nRet = camera.SetEnumValue("TriggerMode", _trigger_mode);
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set trigger mode fail!");
                    }
                    RaisePropertyChanged(nameof(TriggerMode));
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
                    _exposure = value;
                    int nRet = camera.SetFloatValue("ExposureTime", (float)_exposure);
                    if (nRet != CErrorDefine.MV_OK)
                    {
                        Console.WriteLine("Set exposure fail!");
                    }
                    RaisePropertyChanged("Exposure");
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

                }
            }
        }
        
        #endregion
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
        public string TransformRecordPath(string record_path)
        {
            if (RecordOnMinimum)
            {
                if (last_speed * 60 < MinimumSpeed)
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
        public void Dispose()
        {

        }
        public void Trigger()
        {

            //wd.Show();
            camera.SetEnumValue("TriggerMode", (uint)MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
            camera.SetEnumValue("TriggerSource", (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
            int nRet = camera.SetCommandValue("TriggerSoftware");
           
            if (CErrorDefine.MV_OK != nRet)
            {
                Console.WriteLine("FAIL TRIGGER SOFTWARE");
            }
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


        private void DestroyCamera()
        {
            try
            {
                if (camera != null)
                {
                    camera.CloseDevice();
                    camera.DestroyHandle();
                    camera = null;
                }
            }
            catch (Exception e)
            {

            }



        }

        public bool Connect()
        {
            // framegrabber = new HFramegrabber("GenICamTL", 0, 0, 0, 0, 0, 0, "progressive", -1, "default", -1, "false", "default", "default", 0, -1);
            if (camera != null)
            {
                DestroyCamera();
            }

            try
            {
                CCameraInfo device = new CCameraInfo();
                List<CCameraInfo> m_ltDeviceList = new List<CCameraInfo>();
                m_ltDeviceList.Clear();
                int nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref m_ltDeviceList);
                for (int i = 0; i < m_ltDeviceList.Count; i++)
                {
                    CGigECameraInfo gigeInfo = (CGigECameraInfo)m_ltDeviceList[i];
                    if(gigeInfo.chModelName + "(" + gigeInfo.chSerialNumber + ")" == Device)
                        device = gigeInfo;
                }
                
                camera = new CCamera();
                nRet = camera.CreateHandle(ref device);
                if (CErrorDefine.MV_OK != nRet)
                {
                    return false;
                }

                nRet = camera.OpenDevice();
                if (CErrorDefine.MV_OK != nRet)
                {
                    camera.DestroyHandle();
                    return false;
                }
                LoadCameraSetting();
                m_hReceiveThread = new Thread(ReceiveThreadProcess);
                m_hReceiveThread.Start();
                is_run = true;
                nRet = camera.StartGrabbing();

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
        public void LoadCameraSetting()
        {
            CFloatValue pcFloatValue = new CFloatValue();
            CEnumValue pcEnumValue = new CEnumValue();
            CEnumEntry pcEntryValue = new CEnumEntry();
            //int nRet = CErrorDefine.MV_OK;

           
            int nRet = camera.GetEnumValue("TriggerMode", ref pcEnumValue);
            _trigger_mode = pcEnumValue.CurValue;
            RaisePropertyChanged(nameof(TriggerMode));
            if (CErrorDefine.MV_OK == nRet)
            {
                if ((uint)MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON == pcEnumValue.CurValue)
                {
                    nRet = camera.GetEnumValue("TriggerSource", ref pcEnumValue);
                    if (CErrorDefine.MV_OK == nRet)
                    {
                       switch (pcEnumValue.CurValue) 
                       {
                            case (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE:
                                _trigger_source = "Software";
                                break;
                            case (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0:
                                _trigger_source = "Line0";
                                break;
                            case (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE1:
                                _trigger_source = "Line1";
                                break;
                            case (uint)MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE2:
                                _trigger_source = "Line2";
                                break;
                            default: _trigger_source = "Software";
                                break;
                        }
                    }
                    RaisePropertyChanged(nameof(TriggerSource));
                }
            }
             

            
            nRet = camera.GetFloatValue("ExposureTime", ref pcFloatValue);
           
            if (CErrorDefine.MV_OK == nRet)
            {
                _exposure = double.Parse(pcFloatValue.CurValue.ToString("F1"));
                RaisePropertyChanged(nameof(Exposure));
            }
            CIntValue pcWidth = new CIntValue();

            nRet = camera.GetIntValue("Width", ref pcWidth);
            if (CErrorDefine.MV_OK == nRet)
            {
                _width = pcWidth.CurValue.ToString("F1");
                RaisePropertyChanged(nameof(Width));
            }
            CIntValue pcHeight = new CIntValue();
            nRet = camera.GetIntValue("Height", ref pcHeight);
            if (CErrorDefine.MV_OK == nRet)
            {
                _height = pcHeight.CurValue.ToString("F1");
                RaisePropertyChanged(nameof(Height));
            }
            CIntValue pcOffsetX = new CIntValue();
            nRet = camera.GetIntValue("OffsetX", ref pcOffsetX);
            if (CErrorDefine.MV_OK == nRet)
            {
                _left = pcOffsetX.CurValue.ToString("F1");
                RaisePropertyChanged(nameof(Left));
                Console.WriteLine("Load Success");
            }
            else
            {
                Console.WriteLine("Load OffsetX Fail!");
            }
            CIntValue pcOffsetY = new CIntValue();
            nRet = camera.GetIntValue("OffsetY", ref pcOffsetY);
            if (CErrorDefine.MV_OK == nRet)
            {
                _top = pcOffsetY.CurValue.ToString("F1");
                RaisePropertyChanged(nameof(Top));
            }

            nRet = camera.GetFloatValue("Gain", ref pcFloatValue);
            if (CErrorDefine.MV_OK == nRet)
            {
                _gain = (float) pcFloatValue.CurValue;
                RaisePropertyChanged(nameof(Gain));
            }
            CEnumValue pcPixelFormat = new CEnumValue();
            nRet = camera.GetEnumValue("PixelFormat", ref pcPixelFormat);
            if (CErrorDefine.MV_OK != nRet)
            {
                Console.WriteLine("Can't read Pixcel format");
                return;
            }

            if ((Int32)MvGvspPixelType.PixelType_Gvsp_Undefined == (Int32)pcPixelFormat.CurValue)
            {
                Console.WriteLine("Unknown Pixel Format!", CErrorDefine.MV_E_UNKNOW);
                return;
            }
            else if (IsMono((MvGvspPixelType)pcPixelFormat.CurValue))
            {
                m_enBitmapPixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            }
            else
            {
                m_enBitmapPixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            }

            if (null != m_pcBitmap)
            {
                m_pcBitmap.Dispose();
                m_pcBitmap = null;
            }
            m_pcBitmap = new Bitmap((Int32)pcWidth.CurValue, (Int32)pcHeight.CurValue, m_enBitmapPixelFormat);

            
            if (System.Drawing.Imaging.PixelFormat.Format8bppIndexed == m_enBitmapPixelFormat)
            {
                ColorPalette palette = m_pcBitmap.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                m_pcBitmap.Palette = palette;
            }


        }
        private Boolean IsMono(MvGvspPixelType enPixelType)
        {
            switch (enPixelType)
            {
                case MvGvspPixelType.PixelType_Gvsp_Mono1p:
                case MvGvspPixelType.PixelType_Gvsp_Mono2p:
                case MvGvspPixelType.PixelType_Gvsp_Mono4p:
                case MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MvGvspPixelType.PixelType_Gvsp_Mono8_Signed:
                case MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                case MvGvspPixelType.PixelType_Gvsp_Mono14:
                case MvGvspPixelType.PixelType_Gvsp_Mono16:
                    return true;
                default:
                    return false;
            }
        }
        CImage m_pcImgForDriver;
        Bitmap m_pcBitmap = null;
        
        private static Object BufForDriverLock = new Object();
        CFrameSpecInfo m_pcImgSpecInfo;
        public void ReceiveThreadProcess()
        {
            int nRet = CErrorDefine.MV_OK;

            while (is_run)
            {
                CFrameout pcFrameInfo = new CFrameout();
                CDisplayFrameInfo pcDisplayInfo = new CDisplayFrameInfo();
                CPixelConvertParam pcConvertParam = new CPixelConvertParam();

                nRet = camera.GetImageBuffer(ref pcFrameInfo, 1000);
                if (nRet == CErrorDefine.MV_OK)
                {
                    lock (BufForDriverLock)
                    {
                        m_pcImgForDriver = pcFrameInfo.Image.Clone() as CImage;
                        m_pcImgSpecInfo = pcFrameInfo.FrameSpec;

                        pcConvertParam.InImage = pcFrameInfo.Image;
                        if (System.Drawing.Imaging.PixelFormat.Format8bppIndexed == m_pcBitmap.PixelFormat)
                        {
                            pcConvertParam.OutImage.PixelType = MvGvspPixelType.PixelType_Gvsp_Mono8;
                            camera.ConvertPixelType(ref pcConvertParam);
                        }
                        else
                        {
                            pcConvertParam.OutImage.PixelType = MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
                            camera.ConvertPixelType(ref pcConvertParam);
                        }
                        
                        int w = pcConvertParam.InImage.Width;
                        int h = pcConvertParam.InImage.Height;

                        BitmapData m_pcBitmapData = m_pcBitmap.LockBits(new Rectangle(0, 0,w, h), ImageLockMode.ReadWrite, m_pcBitmap.PixelFormat);
                        Marshal.Copy(pcConvertParam.OutImage.ImageData, 0, m_pcBitmapData.Scan0, (Int32)pcConvertParam.OutImage.ImageData.Length);
                        m_pcBitmap.UnlockBits(m_pcBitmapData);
                        
                        IntPtr pBitmap = m_pcBitmapData.Scan0;
                        HImage hImage = new HImage();
                        hImage.GenImage1("byte", w, h, pBitmap);

                       
                        Task.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Priority = ThreadPriority.Highest;
                            ImageAcquired?.Invoke(hImage, pcFrameInfo.FrameSpec.FrameNum);
                        }, TaskCreationOptions.None);

                        
                    }
                    
                    
                    camera.DisplayOneFrame(ref pcDisplayInfo);
                    camera.FreeImageBuffer(ref pcFrameInfo);
                }
            }
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
                else
                {
                    is_run = true;
                }    
            }
           ));
        }



        public void Stop()
        {
            if (null == camera)
            {
                App.GlobalLogger.LogError("HIK Camera", "No camera open.");
            }
            try
            {
                if (camera != null)
                {
                    is_run = false;
                }
            }
            catch (Exception e)
            {
                App.GlobalLogger.LogError("Basler", e.ToString());
            }
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
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            SaveParam(file, this);
        }
        public double? GetFPS()
        {
            return 0;
        }
        public void Reject()
        {

        }
    }
}
