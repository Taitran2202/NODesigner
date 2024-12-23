using DevExpress.Mvvm;
using HalconDotNet;
using PvDotNet;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace NOVisionDesigner.Designer.Accquisition.Windows.UniquaView
{
    /// <summary>
    /// Interaction logic for UniquaLiveView.xaml
    /// </summary>
    public partial class UniquaLiveView : UserControl
    {
        EBUS model;

        public object Parameter { get; set; }

        public UniquaLiveView(EBUS model)
        {
            InitializeComponent();
            
        }
        public UniquaLiveView()
        {
            InitializeComponent();
        }
        public void SetModel(EBUS model)
        {
            this.model = model;
            var ViewModel = new UniquaLiveViewModel(model);
            this.DataContext = ViewModel;
            ViewModel.window_display = window_display;

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

        public void OnNavigatedTo()
        {
            
        }

        public void OnNavigatedFrom()
        {
            
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            window_display.HalconWindow.SetWindowParam("background_color", "white");
        }
    }

    public class UniquaLiveViewModel : ReactiveObject
    {
        AcquisitionMode _acquisition_mode;
        public AcquisitionMode AcquisitionMode
        {
            get { return _acquisition_mode; }
            set { this.RaiseAndSetIfChanged(ref _acquisition_mode, value); }
        }
        string _line_trigger_mode;
        public string LineTriggerMode
        {
            get { return _line_trigger_mode; }
            set { this.RaiseAndSetIfChanged(ref _line_trigger_mode, value); }
        }
        string _frame_trigger_mode;
        public string FrameTriggerMode
        {
            get { return _frame_trigger_mode; }
            set { this.RaiseAndSetIfChanged(ref _frame_trigger_mode, value); }
        }
        string _line_format_l0;
        public string LineFormatL0
        {
            get { return _line_format_l0; }
            set { this.RaiseAndSetIfChanged(ref _line_format_l0, value); }
        }
        string _line_format_l1;
        public string LineFormatL1
        {
            get { return _line_format_l1; }
            set { this.RaiseAndSetIfChanged(ref _line_format_l1, value); }
        }
        string _line_format_l2;
        public string LineFormatL2
        {
            get { return _line_format_l2; }
            set { this.RaiseAndSetIfChanged(ref _line_format_l2, value); }
        }
        PvGenEnum _line_format_l3;
        public PvGenEnum LineFormatL3
        {
            get { return _line_format_l3; }
            set { this.RaiseAndSetIfChanged(ref _line_format_l3, value); }
        }
        string _line_format_l4;
        public string LineFormatL4
        {
            get { return _line_format_l4; }
            set { this.RaiseAndSetIfChanged(ref _line_format_l4, value); }
        }
        string _line_mode_l3;
        public string LineModeL3
        {
            get { return _line_mode_l3; }
            set { this.RaiseAndSetIfChanged(ref _line_mode_l3, value); }
        }
        List<string> _line_mode_l3_list;
        public List<string> LineModeL3List
        {
            get { return _line_mode_l3_list; }
            set { this.RaiseAndSetIfChanged(ref _line_mode_l3_list, value); }
        }
        string _line_detection_l0;
        public string LineDetectionL0
        {
            get { return _line_detection_l0; }
            set { this.RaiseAndSetIfChanged(ref _line_detection_l0, value); }
        }
        string _line_detection_l1;
        public string LineDetectionL1
        {
            get { return _line_detection_l1; }
            set { this.RaiseAndSetIfChanged(ref _line_detection_l1, value); }
        }
        string _line_detection_l2;
        public string LineDetectionL2
        {
            get { return _line_detection_l2; }
            set { this.RaiseAndSetIfChanged(ref _line_detection_l2, value); }
        }
        string _line_detection_l3;
        public string LineDetectionL3
        {
            get { return _line_detection_l3; }
            set { this.RaiseAndSetIfChanged(ref _line_detection_l3, value); }
        }
        bool _is_l3_output;
        public bool IsL3Output
        {
            get { return _is_l3_output; }
            set { this.RaiseAndSetIfChanged(ref _is_l3_output, value); }
        }
        string _line_output_level_l3;
        public string LineOutputLevelL3
        {
            get { return _line_output_level_l3; }
            set { this.RaiseAndSetIfChanged(ref _line_output_level_l3, value); }
        }
        string _OutLineSource_Line3;
        public string OutLineSourceL3
        {
            get { return _OutLineSource_Line3; }
            set { this.RaiseAndSetIfChanged(ref _OutLineSource_Line3, value); }
        }
        string _line_detection_l4;
        public string LineDetectionL4
        {
            get { return _line_detection_l4; }
            set { this.RaiseAndSetIfChanged(ref _line_detection_l4, value); }
        }
        long _line_status_l0;
        public long LineStatusL0
        {
            get { return _line_status_l0; }
            set { this.RaiseAndSetIfChanged(ref _line_status_l0, value); }
        }
        long _line_status_l1;
        public long LineStatusL1
        {
            get { return _line_status_l1; }
            set { this.RaiseAndSetIfChanged(ref _line_status_l1, value); }
        }
        long _line_status_l2;
        public long LineStatusL2
        {
            get { return _line_status_l2; }
            set { this.RaiseAndSetIfChanged(ref _line_status_l2, value); }
        }
        long _line_status_l3;
        public long LineStatusL3
        {
            get { return _line_status_l3; }
            set { this.RaiseAndSetIfChanged(ref _line_status_l3, value); }
        }
        int _line_status_l4;
        public int LineStatusL4
        {
            get { return _line_status_l4; }
            set { this.RaiseAndSetIfChanged(ref _line_status_l4, value); }
        }
        string _current_user_set;
        public string CurrentUserSet
        {
            get { return _current_user_set; }
            set { this.RaiseAndSetIfChanged(ref _current_user_set, value); }
        }
        string _current_color_bank;
        public string CurrentColorBank
        {
            get { return _current_color_bank; }
            set { this.RaiseAndSetIfChanged(ref _current_color_bank, value); }
        }
        bool _white_balance_enabled;
        public bool WhitebalanceEnabled
        {
            get { return _white_balance_enabled; }
            set { this.RaiseAndSetIfChanged(ref _white_balance_enabled, value); }
        }
        bool _is_live;
        public bool IsLive
        {
            get { return _is_live; }
            set { this.RaiseAndSetIfChanged(ref _is_live, value); }
        }
        public ICommand SaveCommand {
            get 
            { 
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Device.Parameters.ExecuteCommand("UserBankSave");
                    }catch(Exception ex)
                    {

                    }
                    
                }); 
            }

        }
        public ICommand SaveColorCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Device.Parameters.ExecuteCommand("ColorBankSave");
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        public ICommand LoadCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Device.Parameters.ExecuteCommand("UserBankRestore");
                        LoadData(Device);
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        public ICommand LoadColorCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Device.Parameters.ExecuteCommand("ColorBankRestore");
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        public ICommand WhiteBalanceCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Device.Parameters.ExecuteCommand("WhiteBalanceCalibration");
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        public ICommand StartLiveCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Model.ImageAcquired = OnImageAcquired;
                        Model.Start();
                        IsLive = true;
                        //Device.Parameters.ExecuteCommand("WhiteBalanceCalibration");
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        public ICommand StopLiveCommand
        {
            get
            {
                return ReactiveCommand.Create(() =>
                {
                    try
                    {
                        Model.Stop();
                        IsLive = false;
                        //Device.Parameters.ExecuteCommand("WhiteBalanceCalibration");
                    }
                    catch (Exception ex)
                    {

                    }

                });
            }

        }
        PvDevice _device;
        public PvDevice Device
        {
            get { return _device; }
            set { this.RaiseAndSetIfChanged(ref _device, value); }
        }
        int _frame_id;
        public int FrameId
        {
            get { return _frame_id; }
            set { this.RaiseAndSetIfChanged(ref _frame_id, value); }
        }
        int _focus_value;
        public int FocusValue
        {
            get { return _focus_value; }
            set { this.RaiseAndSetIfChanged(ref _focus_value, value); }
        }
        string _loading_message;
        public string LoadingMessage
        {
            get { return _loading_message; }
            set { this.RaiseAndSetIfChanged(ref _loading_message, value); }
        }
        bool _is_loading;
        public bool IsLoading
        {
            get { return _is_loading; }
            set { this.RaiseAndSetIfChanged(ref _is_loading, value); }
        }
        bool _is_polling;
        public bool IsPolling
        {
            get { return _is_polling; }
            set { this.RaiseAndSetIfChanged(ref _is_polling, value); }
        }
        public HSmartWindowControlWPF window_display { get; set; } 
        void OnImageAcquired(HImage image, ulong frame_count = 0, double fps = 0)
        {
            HTuple deviation, intensity;
            intensity = image.Intensity(image, out deviation);
            FocusValue = (int)((deviation.D - 80) * 100 / 40);
            window_display.HalconWindow.DispObj(image);
            FrameId++;
        }
        public EBUS Model { get; set; }
        public UniquaLiveViewModel(EBUS ebus)
        {
            Model = ebus;
            Model.ImageAcquired = OnImageAcquired;
            this.WhenAnyValue(x => x.Device).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(x =>
            {
                if (x != null)
                {
                    LoadData(x);
                    Observable.Interval(TimeSpan.FromMilliseconds(500)).Subscribe(y =>
                    {
                        if (IsPolling)
                        {
                            RefreshLineStatus(x);
                        }
                        
                    });
                    this.WhenAnyValue(y => y.FrameTriggerMode).Subscribe(y =>
                    {
                        try
                        {
                            SetSelectorFrameStart(x);

                            x.Parameters.SetEnumValue("TriggerMode", y);
                        }
                        catch(Exception ex)
                        {

                        }
                        

                    });
                    this.WhenAnyValue(y => y.LineTriggerMode).Subscribe(y =>
                    {
                        try
                        {
                            SetSelectorLineStart(x);
                            x.Parameters.SetEnumValue("TriggerMode", y);

                        }
                        catch (Exception ex)
                        {

                        }
                        

                    });
                    this.WhenAnyValue(y => y.LineFormatL0).Subscribe(y =>
                    {
                        try
                        {
                            
                            x.Parameters.SetEnumValue("LineFormat_L0", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineFormatL1).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineFormat_L1", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineFormatL2).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineFormat_L2", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineFormatL3).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineFormat_L3", y.ValueString);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineDetectionL0).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineDetectionLevel_L0", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineDetectionL1).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineDetectionLevel_L1", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineDetectionL2).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineDetectionLevel_L2", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineDetectionL3).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("LineDetectionLevel_L3", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.WhitebalanceEnabled).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetBooleanValue("WhiteBalanceEnable", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.CurrentColorBank).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("ColorBankSelector", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.CurrentUserSet).Subscribe(y =>
                    {
                        try
                        {

                            x.Parameters.SetEnumValue("UserBankSelector", y);

                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.LineModeL3).Subscribe(y =>
                    {
                        try
                        {
                            x.Parameters.SetEnumValue("LineMode_L3", y);
                           

                        }
                        catch (Exception ex)
                        {

                        }
                        IsL3Output = (y == "Output");
                        UpdateLine3(x);
                        

                    });
                    this.WhenAnyValue(y => y.LineOutputLevelL3).Subscribe(y =>
                    {
                        try
                        {
                            x.Parameters.SetEnumValue("LineOutputLevel_L3", y);


                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    this.WhenAnyValue(y => y.OutLineSourceL3).Subscribe(y =>
                    {
                        try
                        {
                            x.Parameters.SetEnumValue("OutLineSource_Line3", y);


                        }
                        catch (Exception ex)
                        {

                        }


                    });
                    
                    this.WhenAnyValue(y => y.AcquisitionMode).Subscribe(y =>
                    {
                        switch (y)
                        {
                            case AcquisitionMode.FreeRunning:
                                FrameTriggerMode = "Off";
                                LineTriggerMode = "Off";
                                break;
                            case AcquisitionMode.EncoderOnly:
                                FrameTriggerMode = "Off";
                                LineTriggerMode = "On";
                                break;
                            case AcquisitionMode.TriggerOnly:
                                FrameTriggerMode = "On";
                                LineTriggerMode = "Off";
                                break;
                            case AcquisitionMode.TriggerEncoder:
                                FrameTriggerMode = "On";
                                LineTriggerMode = "On";
                                break;
                        }
                    });

                }
            });
            if (Model.device!=null)
            {
                if (!Model.device.IsConnected)
                {
                    IsLoading = true;
                    LoadingMessage = "Connecting to camera";
                    Model.Connect();
                    IsLoading = true;
                }
            }
            else
            {
                if (Model.Device != string.Empty)
                {
                    IsLoading = true;
                    LoadingMessage = "Connecting to camera";
                    Model.Connect();
                    IsLoading = true;
                }
                
            }
            this.Device = ebus.device;

        }
        void SetSelectorLineStart(PvDotNet.PvDevice device)
        {
            device.Parameters.SetEnumValue("TriggerSelector", "LineStart");
        }
        void SetSelectorFrameStart(PvDotNet.PvDevice device)
        {
            device.Parameters.SetEnumValue("TriggerSelector", "FrameStart");
        }
        void LoadData(PvDotNet.PvDevice device)
        {
            IsLoading = true;
            LoadingMessage = "Loading camera parameters";
            RefreshTriggerModes(device);
            try
            {
                LineFormatL0 = device.Parameters.GetEnumValueAsString("LineFormat_L0");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineFormatL1 = device.Parameters.GetEnumValueAsString("LineFormat_L1");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineFormatL2 = device.Parameters.GetEnumValueAsString("LineFormat_L2");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineFormatL3 = device.Parameters.GetEnum("LineFormat_L3");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineDetectionL0 = device.Parameters.GetEnumValueAsString("LineDetectionLevel_L0");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineDetectionL1 = device.Parameters.GetEnumValueAsString("LineDetectionLevel_L1");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineDetectionL2 = device.Parameters.GetEnumValueAsString("LineDetectionLevel_L2");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineDetectionL3 = device.Parameters.GetEnumValueAsString("LineDetectionLevel_L3");

            }
            catch (Exception ex)
            {

            }
            try
            {
                CurrentUserSet = device.Parameters.GetEnumValueAsString("UserBankSelector");

            }
            catch (Exception ex)
            {

            }
            try
            {
                CurrentColorBank = device.Parameters.GetEnumValueAsString("ColorBankSelector");

            }
            catch (Exception ex)
            {

            }
            try
            {
                WhitebalanceEnabled = device.Parameters.GetBooleanValue("WhiteBalanceEnable");

            }
            catch (Exception ex)
            {

            }
            
            RefreshLine3(device);
            RefreshLineStatus(device);
            IsLoading = false;
        }

        private void RefreshLine3(PvDevice device)
        {
            try
            {
                LineModeL3List = device.Parameters.GetEnum("LineMode_L3").ToList().Select(x => x.ValueString).ToList();

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineModeL3 = device.Parameters.GetEnumValueAsString("LineMode_L3");
                IsL3Output = (LineModeL3 == "Output");
            }
            catch (Exception ex)
            {

            }
            UpdateLine3(device);
        }

        private void UpdateLine3(PvDevice device)
        {
            //try
            //{
            //    LineDetectionL3 = device.Parameters.GetEnumValueAsString("LineDetectionLevel_L3");


            //}
            //catch (Exception ex)
            //{

            //}
            //try
            //{
            //    LineFormatL3 = device.Parameters.GetEnum("LineFormat_L3");


            //}
            //catch (Exception ex)
            //{

            //}
            try
            {
                LineOutputLevelL3 = device.Parameters.GetEnum("LineOutputLevel_L3").ValueString;


            }
            catch (Exception ex)
            {

            }
            try
            {
                OutLineSourceL3 = device.Parameters.GetEnum("OutLineSource_Line3").ValueString;


            }
            catch (Exception ex)
            {

            }
        }

        private void RefreshTriggerModes(PvDevice device)
        {
            try
            {
                SetSelectorLineStart(device);
                LineTriggerMode = device.Parameters.GetEnumValueAsString("TriggerMode");
            }
            catch (Exception ex)
            {

            }
            try
            {
                SetSelectorFrameStart(device);
                FrameTriggerMode = device.Parameters.GetEnumValueAsString("TriggerMode");
            }
            catch (Exception ex)
            {

            }
            if (LineTriggerMode == "On")
            {
                if (FrameTriggerMode == "On")
                {
                    AcquisitionMode = AcquisitionMode.TriggerEncoder;
                }
                else
                {
                    AcquisitionMode = AcquisitionMode.EncoderOnly;
                }
            }
            else
            {
                if (FrameTriggerMode == "On")
                {
                    AcquisitionMode = AcquisitionMode.TriggerOnly;
                }
                else
                {
                    AcquisitionMode = AcquisitionMode.FreeRunning;
                }
            }
        }

        private void RefreshLineStatus(PvDevice device)
        {
            try
            {
                LineStatusL0 = device.Parameters.GetIntegerValue("Line0_Status");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineStatusL1 = device.Parameters.GetIntegerValue("Line1_Status");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineStatusL2 = device.Parameters.GetIntegerValue("Line2_Status");

            }
            catch (Exception ex)
            {

            }
            try
            {
                LineStatusL3 = device.Parameters.GetIntegerValue("Line3_Status");

            }
            catch (Exception ex)
            {

            }
        }
    }
    public enum AcquisitionMode
    {
        FreeRunning,EncoderOnly,TriggerOnly,TriggerEncoder
    }
}
