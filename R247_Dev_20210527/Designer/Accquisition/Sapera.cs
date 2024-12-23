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
using DALSA.SaperaLT.SapClassBasic;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class Sapera : HelperMethods,CameraInterface,INotifyPropertyChanged
    {
        public void SoftwareTrigger()
        {

        }
        public SapAcquisition m_Acquisition;
        public SapGio GPIO;
        public SapBuffer m_Buffers; 
        public SapFeature features;
        public SapAcqDevice devices;
        public SapAcqToBuf m_Xfer;
        CameraLinkProcessing multiCameraLinkProcessing;
        public bool IsRun { get { return is_run; } }
        static Sapera()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.SaperaView(), typeof(IViewFor<Sapera>));
        }
        public void DestroysFeatureAndDeviceObjects(SapAcqDevice acq, SapFeature feat)
        {
            if (acq != null)
            {
                acq.Destroy();
                acq.Dispose();
            }
            if (feat != null)
            {
                feat.Destroy();
                feat.Dispose();
            }

        }
        public bool feature_created = false;
        public bool CreateFeatures(SapLocation framegrabber)
        {
            if (!feature_created)
            {
                if (framegrabber!= null)
                {
                    devices = new SapAcqDevice(framegrabber);
                    if (!devices.Create())
                    {
                        DestroysFeatureAndDeviceObjects(devices, features);
                        devices = null;
                        return false;
                    }
                    features = new SapFeature(framegrabber);
                    if (!features.Create())
                    {
                        DestroysFeatureAndDeviceObjects(devices, features);
                        features = null;
                        return false;
                    }
                    
                    feature_created = true;
                    return true;
                }
                else { return false; }
            }
            return true;

        }
        public bool CreateIO(SapLocation io_resouces)
        {
            GPIO = new SapGio(io_resouces);
            if (!GPIO.Create())
                return false;          
            return true;

        }
        private bool CreateObjects()
        {
            bool m_online = true;
            if (m_online)
            {
                m_Buffers.Count = 10;
                m_Buffers.Width = m_Acquisition.XferParams.Width;
                m_Buffers.Height = m_Acquisition.XferParams.Height;
                m_Buffers.Format = m_Acquisition.XferParams.Format;
                m_Buffers.PixelDepth = m_Acquisition.XferParams.PixelDepth;
            }
            if (m_Buffers != null && !m_Buffers.Initialized)
            {
                if (m_Buffers.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
                m_Buffers.Clear();
            }
           
            if (m_Xfer != null && !m_Xfer.Initialized)
            {
                if (m_Xfer.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
            }

            multiCameraLinkProcessing = new CameraLinkProcessing(m_Buffers, ProcessMaster);
            if (!multiCameraLinkProcessing.Create())
            {
                DestroyObjects();
                return false;
            }    
            return true;
        }
        public void ProcessMaster(SapBuffer buffer)
        {
            HImage imageR = new HImage();
            HImage imageG = new HImage();
            HImage imageB = new HImage();
            unsafe
            {
                imageR.GenImageConst("byte", buffer.Width, buffer.Height);
                imageG.GenImageConst("byte", buffer.Width, buffer.Height);
                imageB.GenImageConst("byte", buffer.Width, buffer.Height);
                var ptr=imageR.GetImagePointer1(out string type, out int w, out int h);
                var ptg = imageG.GetImagePointer1(out  type, out  w, out  h);
                var ptb = imageB.GetImagePointer1(out  type, out  w, out  h);
                buffer.Page = 0;
                var result1=buffer.Read(0, buffer.Width * buffer.Height, ptr);
                buffer.Page = 1;
                var result2 = buffer.Read(0, buffer.Width * buffer.Height, ptg);
                buffer.Page = 2;
                var result3 = buffer.Read(0, buffer.Width * buffer.Height, ptb);
                
            }

            ImageAcquired?.Invoke(imageR.Compose3(imageG,imageB), buffer.CounterStamp);            
        }
        public void xfer_XferNotifyMaster(object sender, SapXferNotifyEventArgs argsNotify)
        {

            if (argsNotify.Trash)
                return;

            if (argsNotify.EventType == SapXferPair.XferEventType.EndOfFrame)
                multiCameraLinkProcessing.Execute();
            //return;

            //CheckMergeImages();
        }
        public bool CreateNewObjects(SapLocation m_ServerLocation)
        {
            if (SapBuffer.IsBufferTypeSupported(m_ServerLocation, SapBuffer.MemoryType.ScatterGather))
            {
                m_Buffers = new SapBufferWithTrash();                      
            }
            else
            {
                m_Buffers = new SapBufferWithTrash();
            }
            m_Xfer = new SapAcqToBuf(m_Acquisition, m_Buffers);
            m_Xfer.Pairs[0].CounterStampTimeBase = SapXferPair.XferCounterStampTimeBase.Line;
            m_Xfer.Pairs[0].EventType = SapXferPair.XferEventType.EndOfFrame;
            m_Xfer.XferNotify += xfer_XferNotifyMaster;
            m_Xfer.XferNotifyContext = 0;
            m_Xfer.AutoEmpty = false;
            if (!CreateObjects())
            {
                DisposeObjects();
                return false;
            }

            return true;
        }
        public bool IsConnected;
        public bool Connect()
        {

            SapManager.DisplayStatusMode = SapManager.StatusMode.Log;
            int server_count = SapManager.GetServerCount();
            List<SapLocation> framegrabber = new List<SapLocation>();
            List<SapLocation> device = new List<SapLocation>();
            List<SapLocation> io_resouce = new List<SapLocation>();
            for (int i = 0; i < server_count; i++)
            {
                if (SapManager.GetResourceCount(i, SapManager.ResourceType.Acq) > 1)
                {
                    //Console.WriteLine(SapManager.GetResourceName(i,SapManager.ResourceType.Acq,1));

                    framegrabber.Add(new SapLocation(SapManager.GetServerName(i), 1));
                }
                if (SapManager.GetResourceCount(i, SapManager.ResourceType.AcqDevice) > 0)
                {
                    //Console.WriteLine(SapManager.GetResourceName(i,SapManager.ResourceType.Acq,1));

                    device.Add(new SapLocation(SapManager.GetServerName(i), 0));
                }
                if (SapManager.GetResourceCount(i, SapManager.ResourceType.Gio) > 0)
                {
                    //Console.WriteLine(SapManager.GetResourceName(i,SapManager.ResourceType.Acq,1));

                    io_resouce.Add(new SapLocation(SapManager.GetServerName(i), 0));
                }
            }
            if (framegrabber.Count > 0)
            {
                //CreateFeatures(device[0]);
                m_Acquisition = new SapAcquisition(framegrabber[0], ConfigurationPath);
                if (m_Acquisition != null && !m_Acquisition.Initialized)
                {
                    m_Acquisition.EnableEvent(SapAcquisition.AcqEventType.LinkError);
                    m_Acquisition.EventType = SapAcquisition.AcqEventType.LinkError;
                    m_Acquisition.AcqNotify += ((sender, e) => { Console.WriteLine("Framegrabber 0: " + e.EventType); });
                    if (m_Acquisition.Create() == false)
                    {
                        App.GlobalLogger.LogError("Sapera", "Cannot create m_Acquisition object");
                        DestroyObjects();
                        return false;
                    }

                }

                
                CreateIO(io_resouce[0]);
                CreateNewObjects(framegrabber[0]);
                IsConnected = true;
                try
                {
                    ReloadFeatures();
                }
                catch (Exception ex)
                {

                }

                return true;
            }
            else
            {
                App.GlobalLogger.LogError("Sapera","Cannot connect to framegrabber");
                return false;
            }
        }
        private void DestroyObjects()
        {
            if (m_Xfer != null && m_Xfer.Grabbing)
                m_Xfer.Abort();
            if (m_Xfer!= null && m_Xfer.Initialized)
                m_Xfer.Destroy();
            if (m_Buffers != null && m_Buffers.Initialized)
                m_Buffers.Destroy();
            if (m_Acquisition != null && m_Acquisition.Initialized)
                m_Acquisition.Destroy();
            if (GPIO != null)
            {
                if(GPIO.Initialized)
                GPIO.Destroy();
            }
            if (devices != null && devices.Initialized)
            {
                
                devices.Destroy();
            }
            if (features != null && features.Initialized)
            {
                features.Destroy();
            }
            
        }

        private void DisposeObjects()
        {
            if (m_Xfer != null)
            { m_Xfer.Dispose(); }
            if (m_Buffers != null)
            { m_Buffers.Dispose(); }

            //if (buffer_all != null)
            //{ buffer_all.Dispose(); buffer_all = null; }

            if (m_Acquisition != null)
            { m_Acquisition.Dispose(); }
        }
        bool _trigger_mode;
        public bool TriggerMode
        {
            get
            {
                return _trigger_mode;
            }
            set
            {
                if (_trigger_mode != value)
                {
                    if (SetTriggerMode())
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
                    if (SetExposure(value))
                    {
                        _exposure = value;
                        RaisePropertyChanged("Exposure");
                    }

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
                        m_Acquisition?.SoftwareTrigger(SapAcquisition.SoftwareTriggerType.ExternalFrame);
                    }
                    catch (Exception ex) { }
                },()=> true));
            }
        }
        #endregion
        bool _live_mode;
        public bool LiveMode
        {
            get
            {
                return _live_mode;
            }
            set
            {
                if (_live_mode != value)
                {
                    _live_mode = value;
                    if (_live_mode)
                    {
                        SetLiveMode();
                    }
                    else
                    {
                        SetTriggerMode();
                    }
                }
            }
        }
        private bool SetLiveMode()
        {
            //this.m_Acquisition[0]?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.EXT_FRAME_TRIGGER_ENABLE, 0, true);
            try
            {
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_ENABLE, 0, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.EXT_LINE_TRIGGER_ENABLE, 0, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.INT_LINE_TRIGGER_ENABLE, 1, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.EXT_FRAME_TRIGGER_ENABLE, 0, true);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
            
        }
        private bool SetTriggerMode()
        {
            try
            {


                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.INT_LINE_TRIGGER_ENABLE, 0, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.EXT_LINE_TRIGGER_ENABLE, 0, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_ENABLE, 1, true);
                this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.EXT_FRAME_TRIGGER_ENABLE, 1, true);
                return true;
            }catch (Exception ex)
            {
                return false;
            }

        }

        
        
        string _configuration_path;
        public string ConfigurationPath
        {
            get
            {
                return _configuration_path;
            }
            set
            {
                if (_configuration_path != value)
                {
                    _configuration_path = value;
                    RaisePropertyChanged("ConfigurationPath");
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

        public Sapera()
        {
            Type = "Sapera";

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

        

        private void ReloadFeatures()
        {
            if (m_Acquisition == null)
                return;
            if (MainWindow.is_load)
            {
                try
                {
                    

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
                    
                    _exposure = GetExposure();
                    RaisePropertyChanged("Exposure");
                    

                }
                catch (Exception ex)
                {

                }
            }
            //read only values
           

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
            SaveParam(file, this);
        }
        public bool SetEncoderDrop(int value)
        {
            if (this.m_Acquisition == null)
                return false;
            this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_DROP, value, true);
            return true;
        }
        public bool SetEncoderMul(int value)
        {
            if (this.m_Acquisition == null)
                return false;
            this.m_Acquisition?.SetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_MULTIPLY, value, true);
            return true;
        }
        public int EncoderDrop()
        {
            int paramvalue = 0;
            this.m_Acquisition?.GetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_DROP, out paramvalue);
            return paramvalue;
        }
        public int EncoderMul()
        {
            int paramvalue = 0;
            this.m_Acquisition?.GetParameter(DALSA.SaperaLT.SapClassBasic.SapAcquisition.Prm.SHAFT_ENCODER_MULTIPLY, out paramvalue);
            return paramvalue;
        }
        public double GetExposure()
        {
            double value = 0;
            devices?.GetFeatureValue("ExposureTime", out value);
            return value;
        }
        int max_exposure = 200;
        int min_exposure = 3;
        public bool SetExposure(double value)
        {
            if (value > max_exposure)
            {
                value = max_exposure;
            }
            if (value < min_exposure)
            {
                value = min_exposure;
            }
            return devices?.SetFeatureValue("ExposureTime", value) == true;

        }
        
        bool is_run = false;
        ManualResetEvent is_stopping = new ManualResetEvent(true);
        public void Start()
        {
            if (!is_stopping.WaitOne(10000))
            {
                return ;
            }
            if (!IsConnected)
            {
                if (!Connect())
                    return ;
                IsConnected = true;
            }

            if (!m_Xfer.Grab())
            {
                m_Xfer.Grab();
            }
            return ;
        }




        public void Stop()
        {
            is_stopping.Reset();
            if (m_Xfer != null)
            {

                m_Xfer.Freeze();
                if (!m_Xfer.Wait(10000))
                {
                    m_Xfer.Abort();
                }
            }
            is_stopping.Set();
        }

        public void Dispose()
        {
            try
            {
                DestroyObjects();
            }catch(Exception ex)
            {

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
            //if (framegrabber != null)
            //{
            //    return;
            //    Task.Run(new Action(() =>
            //    {

            //        //instantDoCtrl?.WriteBit(1, 0, 1);
            //        framegrabber.SetFramegrabberParam("SyncOutLevels", 1);
            //        Thread.Sleep(_pulse_width);
            //        framegrabber.SetFramegrabberParam("SyncOutLevels", 0);
            //        // timer.Start();

            //        //instantDoCtrl?.WriteBit(1, 0, 0);

            //    }));
            //}
        }

        public string TransformRecordPath(string record_path)
        {
            return record_path;
        }

        public void LiveView()
        {
            //throw new NotImplementedException();
        }
    }
    public delegate void OnMultiCameraLinkProcessing(SapBuffer buffer);
    public class CameraLinkProcessing : SapProcessing
    {
        OnMultiCameraLinkProcessing processHandler;
        public CameraLinkProcessing(SapBuffer processBuffer, OnMultiCameraLinkProcessing processHandler, SapProcessingDoneHandler processDoneHandler = null) : base(processBuffer)
        {
            this.processHandler = processHandler;
            base.ProcessingDoneEnable = true;
            base.ProcessingDone += processDoneHandler;
            this.AutoEmpty = true;
            //this.AutoEmpty
        }
        public override bool Run()
        {
            this.processHandler?.Invoke(base.Buffer);
            return true;
        }
    }
}
