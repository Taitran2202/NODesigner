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
using NOVisionDesigner.Designer.Accquisition.Windows;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class USB3VisionVimba : BaseVimbaInterface
    {
        static USB3VisionVimba()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVisionVimbaView(), typeof(IViewFor<USB3VisionVimba>));
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
        public USB3VisionVimba(string basedir):base(basedir)
        {
            Type = "USB3VisionVimba";
            CommonFeatures = new ObservableCollection<string>() { "TriggerMode", "TriggerDelay", "TriggerSource", "ExposureTime", "Gain", "OffsetX", "OffsetY", "Width", "Height" };
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
                //frameId = (ulong)(frame.AncillaryData.Buffer[0] << 24 | frame.AncillaryData.Buffer[1] << 16 | frame.AncillaryData.Buffer[2] << 8 | frame.AncillaryData.Buffer[3]);
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


        public override bool Connect()
        {
            try
            {
                if (camera != null)
                {
                    camera.Close();
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                VimbaSystem = new Vimba();
                VimbaSystem.Startup();
                camera = VimbaSystem.OpenCameraByID(Device, VmbAccessModeType.VmbAccessModeFull);
                //Update parameter on connect
                if (camera != null)
                {
                    ListFeatures.Clear();
                    //ListCommonFeatures.Clear();
                    foreach (Feature f in camera.Features)
                    {
                        var feature = new CustomVimbaFeature(f);
                        ListFeatures.Add(feature);
                        if (CommonFeatures.Any(x => x == feature.Name))
                            ListCommonFeatures.Add(feature);
                        if (feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsVolatile))
                        {
                            var fswCreated = Observable.FromEvent<Feature.OnFeatureChangeHandler, Feature>(handler =>
                            {
                                Feature.OnFeatureChangeHandler fsHandler = (sender) => handler(sender);
                                return fsHandler;
                            },
                            fsHandler => f.OnFeatureChanged += fsHandler,
                            fsHandler => f.OnFeatureChanged -= fsHandler);

                            fswCreated.Throttle(TimeSpan.FromMilliseconds(200)).ObserveOn(Application.Current.Dispatcher)
                                .Subscribe(e => FeatureChanged(e));
                        }
                    }
                    ReloadFeatures();
                    //LoadCommonFeatures();
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
                    App.GlobalLogger.LogError("USB3VisionVimba", "Cannot connect camera");

                    OnCameraDisconnected?.Invoke();
                    //Connected = Disconnect();
                    return false;
                }
                App.GlobalLogger.LogError("USB3VisionVimba", "Cannot connect camera");
                //MessageBox.Show(ex.Message);
                OnCameraDisconnected?.Invoke();
                Connected = Disconnect();
                return false;
            }

            return true;
        }
        private void FeatureChanged(Feature feature)
        {
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
                for (int i = 0; i < ListCommonFeatures.Count; i++)
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
                        ListCommonFeatures[i] = ListFeatures.FirstOrDefault(x => x.Name == ListCommonFeatures[i].Name);
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

            try
            {
                //Gain = GetFeature("Gain");
                //Exposure = GetFeature("ExposureTime");
                //TriggerMode = GetFeature("TriggerMode");
                //TriggerDelay = GetFeature("TriggerDelay");
                //TriggerSource = GetFeature("TriggerSource");
                //Top = GetFeature("OffsetX");
                //Left = GetFeature("OffsetY");
                //Width = GetFeature("Width");
                //Height = GetFeature("Height");
            }
            catch (Exception ex)
            {

            }

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
}
