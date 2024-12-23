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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HalconDotNet;
using Microsoft.Win32;
using System.Configuration;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO.Ports;
//using NOVisionService;
using Newtonsoft.Json;
using System.Globalization;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using System.Runtime.ExceptionServices;
using System.Collections.Concurrent;
using Automation.BDaq;
using PvDotNet;
using System.Drawing;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using ReactiveUI;
using NOVisionDesigner.Designer.NodeViews;
using static AdvancedHMIDrivers.SNPXComm;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class EBUS : CameraInterfaceBase, CameraInterface,INotifyPropertyChanged
    {
        public override void OnLoadComplete()
        {
            IsRun = false;
        }
        public void SoftwareTrigger()
        {

        }
        private PvPipeline mPipeline = null;
        public PvDotNet.PvDevice device;
        static EBUS()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.EBUSView(), typeof(IViewFor<EBUS>));
        }
        public void OnOffline()
        {
            
            return;
            Task.Run(new Action(() =>
            {
                if (device != null)
                {
                    try
                    {
                        device.Parameters.SetEnumValue("SoftCtrl_Line4", 0);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }));


        }
        public void SetOnline()
        {
            return;
            Task.Run(new Action(() =>
            {
                if (device != null)
                {
                    try
                    {
                        device.Parameters.SetEnumValue("SoftCtrl_Line4", 1);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }));
        }
        public void SetOffline()
        {
            return;
            Task.Run(new Action(() =>
            {
                if (device != null)
                {
                    try
                    {
                        device.Parameters.SetEnumValue("SoftCtrl_Line4", 0);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }));
        }
        public void OnOnline()
        {
            return;
            Task.Run(new Action(() =>
            {
                if (device != null)
                {
                    try
                    {
                        device.Parameters.SetEnumValue("SoftCtrl_Line4", 1);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }));
        }
        bool _invert_reject_signal = false;
        public bool InvertRejectSignal
        {
            get
            {
                return _invert_reject_signal;
            }
            set
            {
                if (_invert_reject_signal != value)
                {
                    _invert_reject_signal = value;
                    RaisePropertyChanged("InvertRejectSignal");
                }
            }
        }


        BlockingCollection<HImage> image_buffer = new BlockingCollection<HImage>(new ConcurrentQueue<HImage>(), 5);
        //Automation.BDaq.InstantDoCtrl instantDoCtrl;
        int _trigger_delay = 0;
        public int TriggerDelay
        {
            get
            {
                return _trigger_delay;
            }
            set
            {
                if (_trigger_delay != value)
                {
                    //if (device != null)
                    //{
                    //    if (device.IsConnected)
                    //    {
                    //        try
                    //        {
                    //            device.Parameters.SetIntegerValue("FrameTriggerDelay", value);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            MessageBox.Show("Error setting parameter");
                    //            return;
                    //        }
                    //    }
                    //}

                    _trigger_delay = value;
                    RaisePropertyChanged("TriggerDelay");
                }
            }
        }

        int _image_height = 1000;
        public int ImageHeight
        {
            get
            {
                return _image_height;
            }
            set
            {
                if (_image_height != value)
                {
                    _image_height = value;
                    RaisePropertyChanged("ImageHeight");
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

        bool _reject_by_speed = false;
        public bool RejectBySpeed
        {
            get
            {
                return _reject_by_speed;
            }
            set
            {
                if (_reject_by_speed != value)
                {
                    _reject_by_speed = value;
                    RaisePropertyChanged("RejectBySpeed");
                }
            }
        }

        int _reject_speed = 0;
        public int RejectSpeed
        {
            get
            {
                return _reject_speed;
            }
            set
            {
                if (_reject_speed != value)
                {
                    _reject_speed = value;
                    RaisePropertyChanged("RejectSpeed");
                }
            }
        }


        public void Trigger()
        {

            return;
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
        // System.Timers.Timer timer = new System.Timers.Timer();
        public EBUS()
        {
            Type = "EBUS";
            

        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        

        public OnImageAcquired ImageAcquired { get; set; }

        object frame_lock = new object();
        private void OnLinkDisconnected(PvDevice aDevice)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                DXMessageBox.Show("Camera disconnected!!");
                //OnCameraDisconnected?.Invoke();
            }));
            if (is_run)
            {

                IsRun = false;
                PvResult lOperationResult = new PvResult(PvResultCode.OK);
                PvResult lResult = new PvResult(PvResultCode.OK);
                // Disable streaming after sending the AcquisitionStop command.
                device.StreamDisable();
                // Stop display thread.                   
                // Abort all buffers in the stream
                mStream.AbortQueuedBuffers();
                RemoveOldBuffer();
                

            }
            mStream = null;
            //device.Disconnect();
            device = null;
            if (mPipeline.IsStarted)
            {
                mPipeline.Stop();
            }
            mPipeline=null;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnCameraDisconnected?.Invoke();

            }));

        }
        private void OnLinkReconnected(PvDevice aDevice)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                DXMessageBox.Show("Camera reconnected!!");
                //OnCameraDisconnected?.Invoke();
            }));
        }
        Dictionary<UInt64, Bitmap> mBitmaps = new Dictionary<UInt64, Bitmap>();
        Dictionary<UInt64, System.Drawing.Imaging.BitmapData> mBitmapData = new Dictionary<UInt64, System.Drawing.Imaging.BitmapData>();
        int mWidth = 1280;
        int mHeight = 720;
        private unsafe void AttachRawBuffer(PvBuffer aBuffer)
        {
            // The bitmap is using the same size and pixel type as what the device is sending
            Bitmap lBitmap = new Bitmap(mWidth, mHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // We lock the memory before attaching it to the PvBuffer - and keep it locked for 
            // as long as the PvBuffer is held by the PvPipeline/PvStream
            System.Drawing.Imaging.BitmapData lBmpData = lBitmap.LockBits(new System.Drawing.Rectangle(0, 0, mWidth, mHeight),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                lBitmap.PixelFormat);

            // Attach the bitmap pointer to the PvBuffer
            aBuffer.Image.Attach((byte*)lBmpData.Scan0.ToPointer(), (uint)lBmpData.Width, (uint)lBmpData.Height, PvPixelType.RGB8);

            // Save bitmap and bitmap data in map, indexed by the buffer ID
            UInt64 lIndex = (UInt64)mBitmaps.Count;
            mBitmaps[lIndex] = lBitmap;
            mBitmapData[lIndex] = lBmpData;
            aBuffer.ID = lIndex;
        }
        public bool ConnectCamera()
        {
            lock (frame_lock)
            {
                if (Device == null | Device == "")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        OnCameraDisconnected?.Invoke();
                        DXMessageBox.Show("Camera is not selected!!. Please select camera in Interface Section", "ERROR",MessageBoxButton.OK,MessageBoxImage.Error);

                    }));

                    return false;
                }
                if (device == null)
                {

                    try
                    {


                        IsRun = false;

                        device = PvDevice.CreateAndConnect(Device);

                        // Open stream.
                        mStream = PvStream.CreateAndOpen(Device);
                        

                        if (mStream == null)
                        {
                            MessageBox.Show("Unable to open stream.");
                            return false;
                        }
                        // Create pipeline.
                        mPipeline = new PvPipeline(mStream);

                        if (device.Type == PvDeviceType.DeviceGEV)
                        {
                            PvDeviceGEV lDeviceGEV = device as PvDeviceGEV;
                            PvStreamGEV lStreamGEV = mStream as PvStreamGEV;

                            // Negotiate packet size
                            lDeviceGEV.NegotiatePacketSize();

                            // Set stream destination to our stream object
                            lDeviceGEV.SetStreamDestination(lStreamGEV.LocalIPAddress, lStreamGEV.LocalPort);
                            //lStreamGEV.UserModeDataReceiverThreadPriority = PvThreadPriority.Highest;
                            // Read payload size, set buffer size the pipeline will use to allocate buffers
                            mPipeline.BufferSize = device.PayloadSize;
                            mPipeline.BufferCount = BufferCount;
                            try
                            {
                                var trigger_mode = lDeviceGEV.Parameters.GetEnum("TriggerMode").ValueString;
                                if (trigger_mode != "Off")
                                {
                                    try
                                    {
                                        lDeviceGEV.Parameters.ExecuteCommand("AcquisitionStop");
                                    }
                                    catch (Exception ex)
                                    {

                                    }

                                    try
                                    {
                                        lDeviceGEV.Parameters.SetEnumValue("TriggerMode", "Off");
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    try
                                    {
                                        lDeviceGEV.Parameters.SetEnumValue("TriggerMode", trigger_mode);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    
                                    

                                    //lDeviceGEV.Parameters.SetEnumValue("TriggerMode", trigger_mode);
                                }
                                else
                                {
                                    lDeviceGEV.Parameters.ExecuteCommand("AcquisitionStop");
                                }
                                
                                
                                
                                
                            }
                            catch (Exception ex)
                            {
                                App.GlobalLogger.LogError("EBUS", ex.Message);
                            }

                        }





                        device.OnLinkDisconnected += OnLinkDisconnected;
                        device.OnLinkReconnected += OnLinkReconnected;
                    }
                    catch (Exception ex)
                    {

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            DXMessageBox.Show("Cannot connect to camera", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                        return false;
                    }

                }
                else
                {
                    if (device.IsConnected)
                    {

                    }
                    else
                    {
                        try
                        {
                            device.Connect(Device);

                        }
                        catch (Exception ex)
                        {
                            return false;
                        }
                    }

                    try
                    {
                        mStream = PvStream.CreateAndOpen(Device);
                        mPipeline = new PvPipeline(mStream);
                        if (mStream == null)
                        {
                            MessageBox.Show("Unable to open stream.");
                            return false;
                        }
                        if (device.Type == PvDeviceType.DeviceGEV)
                        {
                            PvDeviceGEV lDeviceGEV = device as PvDeviceGEV;
                            PvStreamGEV lStreamGEV = mStream as PvStreamGEV;

                            // Negotiate packet size
                            lDeviceGEV.NegotiatePacketSize();
                            // Set stream destination to our stream object
                            lDeviceGEV.SetStreamDestination(lStreamGEV.LocalIPAddress, lStreamGEV.LocalPort);
                            //lStreamGEV.UserModeDataReceiverThreadPriority = PvThreadPriority.Highest;
                            mPipeline.BufferSize = device.PayloadSize;
                            mPipeline.BufferCount = BufferCount;
                            try
                            {
                                lDeviceGEV.Parameters.ExecuteCommand("AcquisitionStop");
                                long trigger_mode = lDeviceGEV.Parameters.GetEnumValueAsInt("TriggerMode");                               
                                lDeviceGEV.Parameters.SetEnumValue("TriggerMode", 0);
                                lDeviceGEV.Parameters.SetEnumValue("TriggerMode", trigger_mode);
                                
                            }
                            catch (Exception ex)
                            {
                                App.GlobalLogger.LogError("EBUS",ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                return true;
            }
        }
        public bool Connect()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainWindow.ShowLoading("Connecting to camera!");
            }));

            bool result = ConnectCamera();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainWindow.CloseLoading();
            }));

            return result;
        }


        
        void MatroxImageAcquired(HImage image)
        {
            ImageAcquired?.Invoke(image);
        }
        bool is_run = false;
        double fps = 0;
        Task processtask;
        CancellationTokenSource process_token;
        CancellationTokenSource grab_token;
        public PvStream mStream = null;
        static uint BufferCount = 20;
        public void RemoveOldBuffer()
        {
            PvBuffer lBuffer = null;
            PvResult lOperationResult = new PvResult(PvResultCode.OK);
            PvResult lResult = new PvResult(PvResultCode.OK);
            while (mStream.RetrieveBuffer(ref lBuffer, ref lOperationResult, 100).IsOK) ;
        }
        public unsafe void GrabbingThreadNew()
        {
            try
            {
                while (image_buffer.Count > 0)
                {
                    HImage image_in_buffer;
                    image_buffer.TryTake(out image_in_buffer);
                    image_in_buffer.Dispose();
                }
            }
            catch (Exception ex)
            {

            }
            HTuple s1 = 0, s2 = 0;
            Queue<double> fps_counter = new Queue<double>(5);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            PvBuffer lBuffer = null;
            //set stream timeout 
            try
            {
                mStream.Parameters.SetIntegerValue("RequestTimeout", 5000);
                mStream.Parameters.ExecuteCommand("Reset");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //read image size
            mWidth = (int)device.Parameters.GetIntegerValue("Width");
            mHeight = (int)device.Parameters.GetIntegerValue("Height");

            try
            {

                device.Parameters.ExecuteCommand("AcquisitionStop");
                // device.Parameters.ExecuteCommand("AcquisitionStart");
                var pvresult = device.StreamEnable();

                // Start acquisition on the device
                device.Parameters.ExecuteCommand("AcquisitionStart");

                if (!pvresult.IsOK)
                    MessageBox.Show("Cannot enable stream");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot start acquisition with the following reason: " + ex.Message);
                return;
            }

            while (is_run)
            {
                PvResult lResult = mPipeline.RetrieveNextBuffer(ref lBuffer);
                if (lResult.IsOK)
                {
                    // Operation result of buffer is OK, display.
                    if (lBuffer.OperationResult.IsOK)
                    {
                        PvImage lImage = lBuffer.Image;

                        //Bitmap lBitmap = mBitmaps[lBuffer.ID];
                        //System.Drawing.Imaging.BitmapData lBitmapData = mBitmapData[lBuffer.ID];

                        // Unlock bits for the time we work on the bitmap
                        //lBitmap.UnlockBits(lBitmapData);

                        // With our simple imaging library, work on bitmap data
                        if(lImage.Width!=mWidth | lImage.Height != mHeight)
                        {
                            Console.WriteLine("Error: "+lImage.Width.ToString() + "," + lImage.Height);
                        }
                        
                        HImage image = new HImage();
                        unsafe
                        {
                            image.GenImageInterleaved((IntPtr)lImage.DataPointer, "rgb", (int)lImage.Width, (int)lImage.Height, 0, "byte", 0, 0, 0, 0, 8, 0);
                            image_buffer.TryAdd(image);

                        }
                        // Lock the bitmap data again before using the PvBuffer with the display, re-queue it in the PvStream
                        //mBitmapData[lBuffer.ID] = lBitmap.LockBits(new System.Drawing.Rectangle(0, 0, mWidth, mHeight),
                        //    System.Drawing.Imaging.ImageLockMode.ReadWrite, lBitmap.PixelFormat);

                        // Re-attach the buffer. It is not safe to assume we are getting the same pointer back
                        //lImage.Attach((byte*)mBitmapData[lBuffer.ID].Scan0.ToPointer(),
                        //    lImage.Width, lImage.Height, lImage.PixelType);

                        HOperatorSet.CountSeconds(out s2);
                        double speed_temp = 1 / (s2 - s1);
                        s1 = s2;
                        if (fps_counter.Count > 4)
                        {
                            fps_counter.Dequeue();
                        }
                        fps_counter.Enqueue(speed_temp);
                        fps = fps_counter.Average();
                    }

                    // We got a buffer (good or not) we must release it back.
                    mPipeline.ReleaseBuffer(lBuffer);
                }
            }
        }
        public unsafe void GrabbingThread()
        {
            try
            {
                while (image_buffer.Count > 0)
                {
                    HImage image_in_buffer;
                    image_buffer.TryTake(out image_in_buffer);
                    image_in_buffer.Dispose();
                }
            }
            catch (Exception ex)
            {

            }

            //set parameters
            HTuple s1 = 0, s2 = 0;
            Queue<double> fps_counter = new Queue<double>(5);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            fps_counter.Enqueue(0);
            #region remove old buffer
            //set stream timeout 
            try
            {
                mStream.Parameters.SetIntegerValue("RequestTimeout", 5000);
                mStream.Parameters.ExecuteCommand("Reset");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            #endregion
            
            //read image size
            mWidth = (int)device.Parameters.GetIntegerValue("Width");
            mHeight = (int)device.Parameters.GetIntegerValue("Height");
            //set timeout
            //setting buffer
            uint mBufferCount = (mStream.QueuedBufferMaximum < BufferCount) ? mStream.QueuedBufferMaximum : BufferCount;
            #region Fix missing 5 image at the start of Online
            mBitmaps = new Dictionary<UInt64, Bitmap>();
            mBitmapData = new Dictionary<UInt64, System.Drawing.Imaging.BitmapData>();
            #endregion
            for (UInt32 i = 0; i < mBufferCount; i++)
            {
                PvBuffer lBuffer = new PvBuffer(PvPayloadType.Image);
                mStream.QueueBuffer(lBuffer);
                AttachRawBuffer(lBuffer);

            }

            try
            {

                device.Parameters.ExecuteCommand("AcquisitionStop");
                // device.Parameters.ExecuteCommand("AcquisitionStart");
                var pvresult = device.StreamEnable();

                // Start acquisition on the device
                device.Parameters.ExecuteCommand("AcquisitionStart");

                if (!pvresult.IsOK)
                    MessageBox.Show("Cannot enable stream");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot start acquisition with the following reason: " + ex.Message);
                return;
            }
            HOperatorSet.CountSeconds(out s1);
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            while (is_run)
            {


                try
                {
                    PvBuffer lBuffer = null;
                    PvResult lOperationResult = new PvResult(PvResultCode.OK);
                    //Retrieve next buffer from acquisition pipeline          
                    PvResult lResult = mStream.RetrieveBuffer(ref lBuffer, ref lOperationResult);
                    if (!is_run)
                        return;
                    if (lResult.IsOK | lResult.Code == PvResultCode.AUTO_ABORTED | lResult.Code == PvResultCode.ERR_OVERFLOW)
                    {
                        // Operation result of buffer is OK, display.
                        if (lOperationResult.IsOK | lOperationResult.Code == PvResultCode.BUFFER_TOO_SMALL)
                        {
                            
                            PvImage lImage = lBuffer.Image;
                            
                            Bitmap lBitmap = mBitmaps[lBuffer.ID];
                            System.Drawing.Imaging.BitmapData lBitmapData = mBitmapData[lBuffer.ID];

                            // Unlock bits for the time we work on the bitmap
                            lBitmap.UnlockBits(lBitmapData);

                            // With our simple imaging library, work on bitmap data



                            HImage image = new HImage();
                            unsafe
                            {
                                image.GenImageInterleaved((IntPtr)lImage.DataPointer, "rgb", (int)mWidth, (int)mHeight, 0, "byte", 0, 0, 0, 0, 8, 0);
                                image_buffer.TryAdd(image);

                            }
                            // Lock the bitmap data again before using the PvBuffer with the display, re-queue it in the PvStream
                            mBitmapData[lBuffer.ID] = lBitmap.LockBits(new System.Drawing.Rectangle(0, 0, mWidth, mHeight),
                                System.Drawing.Imaging.ImageLockMode.ReadWrite, lBitmap.PixelFormat);

                            // Re-attach the buffer. It is not safe to assume we are getting the same pointer back
                            lImage.Attach((byte*)mBitmapData[lBuffer.ID].Scan0.ToPointer(),
                                lImage.Width, lImage.Height, lImage.PixelType);

                            HOperatorSet.CountSeconds(out s2);
                            double speed_temp = 1 / (s2 - s1);
                            s1 = s2;
                            if (fps_counter.Count > 4)
                            {
                                fps_counter.Dequeue();
                            }
                            fps_counter.Enqueue(speed_temp);
                            fps = fps_counter.Average();
                        }
                        // We have an image - do some processing (...) and VERY IMPORTANT,
                        // re-queue the buffer in the stream object.
                        mStream.QueueBuffer(lBuffer);
                    }
                }
                catch (Exception ex)
                {

                }

            }
        }
        Task grab_thread = null;
        public void Start()
        {
            if(IsRun){
                return;
            }
            lock (frame_lock)
            {
                IsRun = false;
                if (grab_thread != null)
                {
                    if (!grab_thread.IsFaulted)
                    {
                        grab_thread?.Wait();
                    }
                }
                

                if (processtask != null)
                {
                    if (!processtask.IsCompleted)
                    {
                        try
                        {
                            Stop();
                        }
                        catch (Exception ex)
                        {

                        }
                        processtask.Wait();
                    }
                }

                processtask = Task.Run(new Action(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;

                    if (device == null)
                    {
                        bool result = Connect();
                        if (!result)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                OnCameraDisconnected?.Invoke();
                                //DXMessageBox.Show("Cannot Connect to camera, please check camera name");

                            }));
                            return;
                        }

                    }
                    else
                    if (!device.IsConnected)
                    {
                        bool result = Connect();
                        if (!result)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                OnCameraDisconnected?.Invoke();
                                //DXMessageBox.Show("Cannot Connect to camera, please check camera name");

                            }));
                            return;
                        }
                    }
                    if (device.IsConnected)
                        try
                        {

                            // is_run = true;



                            try
                            {
                                //device.Parameters.SetIntegerValue("FrameTriggerDelay", TriggerDelay);
                                device.Parameters.SetIntegerValue("RescalerDivider", EncoderDiv);
                                device.Parameters.SetIntegerValue("RescalerMultiplier", EncoderMul);
                                device.Parameters.SetIntegerValue("Height", ImageHeight);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error on set camera parameter. System still run but camera setting wont set: " + ex.Message);
                            }



                            process_token?.Cancel();
                            process_token = new CancellationTokenSource();
                            grab_token?.Cancel();
                            grab_token = new CancellationTokenSource();
                            mPipeline.Start();
                            grab_thread = Task.Run(new Action(() =>
                            {
                                GrabbingThreadNew();
                            }), grab_token.Token);

                            Task.Run(new Action(() =>
                            {
                                if (InvertRejectSignal)
                                {
                                    try
                                    {
                                        device.Parameters.SetEnumValue("SoftCtrl_Line3", 1);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        //device.Parameters.SetEnumValue("SoftCtrl_Line3", 0);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                SetOnline();
                                IsRun = true;

                                while (is_run)
                                {
                                    try
                                    {
                                        HImage result;

                                        if (image_buffer.TryTake(out result, -1, process_token.Token))
                                            ImageAcquired?.Invoke(result,fps:fps);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is OperationCanceledException)
                                        {

                                            return;
                                        }
                                    }

                                }
                            }));
                            // framegrabber.SetFramegrabberParam("GainAll_PR1", Gain);



                        }
                        catch (Exception ex)
                        {
                            if (ex is HalconException)
                            {

                                return;
                            }
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(ex.Message)));
                            return;
                        }

                }
               ));
            }


            //throw new NotImplementedException();
        }

        public void Stop()
        {
            //throw new NotImplementedException();

            lock (frame_lock)
            {
                try
                {
                    IsRun = false;
                    if(device == null)
                    {
                        return;
                    }
                    SetOffline();
                    PvResult lOperationResult = new PvResult(PvResultCode.OK);
                    PvResult lResult = new PvResult(PvResultCode.OK);
                    // Stop acquisition.
                    device.Parameters.ExecuteCommand("AcquisitionStop");
                    // Disable streaming after sending the AcquisitionStop command.
                    device.StreamDisable();
                    // Stop display thread.                   
                    // Abort all buffers in the stream
                    mStream.AbortQueuedBuffers();
                    if(mPipeline.IsStarted)
                    {
                        mPipeline.Stop();
                    }
                    
                    if (grab_thread != null)
                    {
                        bool canwait = grab_thread.Wait(1000);
                        if (!canwait)
                        {
                            grab_token?.Cancel();
                            grab_token = null;
                        }
                    }

                    RemoveOldBuffer();
                    // 
                }
                catch (Exception lExc)
                {
                    MessageBox.Show(lExc.Message);
                }
                process_token?.Cancel();
            }

        }
        int _encoder_div = 1;
        public int EncoderDiv
        {
            get
            {
                return _encoder_div;
            }
            set
            {
                if (_encoder_div != value & value > 0)
                {
                    //if (device != null)
                    //{
                    //    if (device.IsConnected)
                    //    {
                    //        try
                    //        {
                    //            device.Parameters.SetIntegerValue("RescalerDivider", value);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            MessageBox.Show("Error setting parameter");
                    //            return;
                    //        }
                    //    }
                    //}
                    _encoder_div = value;
                    RaisePropertyChanged("EncoderDiv");
                }
            }
        }
        int _encoder_mul = 1;
        public int EncoderMul
        {
            get
            {
                return _encoder_mul;
            }
            set
            {
                if (_encoder_mul != value & value > 0)
                {
                    //if (device != null)
                    //{
                    //    if (device.IsConnected)
                    //    {
                    //        try
                    //        {
                    //            device.Parameters.SetIntegerValue("RescalerMultiplier", value);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            MessageBox.Show("Error setting parameter");
                    //            return;
                    //        }
                    //    }
                    //}

                    _encoder_mul = value;
                    RaisePropertyChanged("EncoderMul");
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (device != null)
                {
                    Stop();
                    device.Disconnect();
                    device.Dispose();
                    //framegrabber.CloseFramegrabber();
                }
            }
            catch (Exception ex)
            {

            }
            //try
            //{
            //    instantDoCtrl?.Dispose();
            //}

            //catch (Exception ex)
            //{

            //}
            //if (matrox_framegrabber_calback != null)
            //{
            //  matrox_framegrabber_calback.Disconnect();


            //}
        }
        double? last_speed = 0;
        public double? GetFPS()
        {
            last_speed = fps;
            if (last_speed * 60 < TransitionSpeed)
                IsHighSpeed = false;
            else
                IsHighSpeed = true;
            return last_speed;

        }
        public void PulseOutput()
        {



        }

        //public void timer_end(object sender,System.Timers.ElapsedEventArgs e)
        //{

        //}

        public void Reject()
        {
            if (RejectBySpeed)
                if (fps * 60 < RejectSpeed)
                    return;
            if (device == null)
            {
                return;
            }
            Task.Run(new Action(() =>
            {

                //instantDoCtrl?.WriteBit(1, 0, 1);
                if (InvertRejectSignal)
                {
                    device.Parameters.SetEnumValue("SoftCtrl_Line3", 0);
                    Thread.Sleep(_pulse_width);
                    device.Parameters.SetEnumValue("SoftCtrl_Line3", 1);
                }
                else
                {
                    //device?.Parameters.SetEnumValue("SoftCtrl_Line3", 1);
                    //Thread.Sleep(_pulse_width);
                    //device?.Parameters.SetEnumValue("SoftCtrl_Line3", 0);
                }

                // timer.Start();

                //instantDoCtrl?.WriteBit(1, 0, 0);

            }));

            //throw new NotImplementedException();
            //MatroxInterfaceHook.Instance.PulseReject();
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

        public void LiveView()
        {
            //throw new NotImplementedException();
        }

        double _exposure_time;
        public double ExposureTime
        {
            get
            {
                // double aaa=0;
                // MIL.MdigInquireFeature(MatroxInterface.MatroxInterfaceHook.Instance.DIGID, MIL.M_FEATURE_VALUE, "ExposureTime",MIL.M_TYPE_DOUBLE,ref aaa);
                //_exposure_time = aaa;
                return _exposure_time;
            }
            set
            {
                if (value != _exposure_time)
                {
                    _exposure_time = value;
                    //  MIL.MdigControlFeature(MatroxInterface.MatroxInterfaceHook.Instance.DIGID, MIL.M_FEATURE_VALUE, "ExposureTime", MIL.M_TYPE_DOUBLE, ref _exposure_time);
                    RaisePropertyChanged("ExposureTime");
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
                if (value != _gain)
                {
                    _gain = value;
                    RaisePropertyChanged("Gain");
                }
            }
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


    }
}
