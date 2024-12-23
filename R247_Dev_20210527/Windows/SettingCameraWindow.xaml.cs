using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Helper;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for SettingCameraWindow.xaml
    /// </summary>
    public partial class SettingCameraWindow : Window
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region Field
        VimbaHelper m_VimbaHelper = null;
        HImage image;
        HWindow display = null;
        CameraModel Camera = null;
        public bool isStart = false;
        bool _m_Acquiring = false;

        public bool m_Acquiring
        {
            get
            {
                return _m_Acquiring;
            }
            set
            {
                if (_m_Acquiring != value)
                {
                    _m_Acquiring = value;
                    if (value)
                    {
                        btnStart.Content = "Stop";
                        cboxAcqMode.IsEnabled = false;
                        cboxMode.IsEnabled = false;
                    }
                    else
                    {
                        btnStart.Content = "Start";
                        cboxAcqMode.IsEnabled = true;
                        cboxMode.IsEnabled = true;
                    }
                    RaisePropertyChanged("m_Acquiring");
                }
            }
        }

        bool _enableBtn = true;
        public bool EnableBtn
        {
            get
            {
                return _enableBtn;
            }
            set
            {
                if (_enableBtn != value)
                {
                    _enableBtn = value;
                    RaisePropertyChanged("EnableBtn");
                }
            }
        }

        string _btnPlayName = "Start";
        public string BtnPlayName
        {
            get
            {
                return _btnPlayName;
            }
            set
            {
                if (_btnPlayName != value)
                {
                    _btnPlayName = value;
                    RaisePropertyChanged("BtnPlayName");
                }
            }
        }

        double _exposure = 0.5;

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
                    if (value < 50) { value = 50; }
                    else if (value > 1000000) { value = 1000000; }
                    _exposure = value;
                    m_VimbaHelper.m_Camera.Features["ExposureTime"].FloatValue = value;
                    RaisePropertyChanged("Exposure");
                }
            }
        }

        double _gain = 0;
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
                    if (value < 0) { value = 0; }
                    else if (value > 20) { value = 20; }
                    _gain = value;
                    m_VimbaHelper.m_Camera.Features["Gain"].FloatValue = value;
                    RaisePropertyChanged("Gain");
                }
            }
        }

        int _sourceIndex = 0;
        public int SourceIndex
        {
            get
            {
                return _sourceIndex;
            }
            set
            {
                if (_sourceIndex != value)
                {
                    _sourceIndex = value;
                    m_VimbaHelper.m_Camera.Features["TriggerSource"].EnumValue = SourceList[value];
                    RaisePropertyChanged("SourceIndex");
                }
            }
        }

        int _activIndex = 0;
        public int ActivIndex
        {
            get
            {
                return _activIndex;
            }
            set
            {
                if (_activIndex != value)
                {
                    _activIndex = value;
                    m_VimbaHelper.m_Camera.Features["TriggerActivation"].EnumValue = ActivList[value];
                    RaisePropertyChanged("ActivIndex");
                }
            }
        }

        int _selectorIndex = 0;
        public int SelectorIndex
        {
            get
            {
                return _selectorIndex;
            }
            set
            {
                if (_selectorIndex != value)
                {
                    _selectorIndex = value;
                    m_VimbaHelper.m_Camera.Features["TriggerSelector"].EnumValue = SelectorList[value];
                    RaisePropertyChanged("SelectorIndex");
                }
            }
        }

        int _triggerModeIndex = 0;
        public int TriggerModeIndex
        {
            get
            {
                return _triggerModeIndex;
            }
            set
            {
                if (_triggerModeIndex != value)
                {
                    _triggerModeIndex = value;
                    try
                    {
                        m_VimbaHelper.m_Camera.Features["TriggerMode"].EnumValue = TriggerModeList[value];
                    }
                    catch (Exception)
                    {
                    }
                    RaisePropertyChanged("TriggerModeIndex");
                }
            }
        }

        int _acqModeIndex = 0;
        public int AcqModeIndex
        {
            get
            {
                return _acqModeIndex;
            }
            set
            {
                if (_acqModeIndex != value)
                {
                    _acqModeIndex = value;
                    try
                    {
                        m_VimbaHelper.m_Camera.Features["AcquisitionMode"].EnumValue = AcqModeList[value];
                    }
                    catch (Exception)
                    {
                    }
                    RaisePropertyChanged("AcqModeIndex");
                }
            }
        }
        ObservableCollection<string> _sourceList;
        public ObservableCollection<string> SourceList
        {
            get
            {
                return _sourceList;
            }
            set
            {
                if (_sourceList != value)
                {
                    _sourceList = value;
                    RaisePropertyChanged("SourceList");
                }
            }
        }

        ObservableCollection<string> _acqModeList;
        public ObservableCollection<string> AcqModeList
        {
            get
            {
                return _acqModeList;
            }
            set
            {
                if (_acqModeList != value)
                {
                    _acqModeList = value;
                    RaisePropertyChanged("AcqModeList");
                }
            }
        }

        ObservableCollection<string> _activList;
        public ObservableCollection<string> ActivList
        {
            get
            {
                return _activList;
            }
            set
            {
                if (_activList != value)
                {
                    _activList = value;
                    RaisePropertyChanged("ActivList");
                }
            }
        }

        ObservableCollection<string> _selectorList;
        public ObservableCollection<string> SelectorList
        {
            get
            {
                return _selectorList;
            }
            set
            {
                if (_selectorList != value)
                {
                    _selectorList = value;
                    RaisePropertyChanged("SelectorList");
                }
            }
        }

        ObservableCollection<string> _triggerModeList;
        public ObservableCollection<string> TriggerModeList
        {
            get
            {
                return _triggerModeList;
            }
            set
            {
                if (_triggerModeList != value)
                {
                    _triggerModeList = value;
                    RaisePropertyChanged("TriggerModeList");
                }
            }
        }
        #endregion
        public SettingCameraWindow(CameraModel Camera)
        {
            InitializeComponent();
            this.Camera = Camera;
            InitListFeatures();
            GetCurrentFeaturesValue();
            m_VimbaHelper.m_FrameReceivedHandler += this.OnFrameReceived;
            m_Acquiring = Camera.isPause ? false : true;
            this.DataContext = this;
        }

        #region Method
        public void InitListFeatures()
        {
            this.m_VimbaHelper = Camera.m_VimbaHelper;
            Exposure = m_VimbaHelper.m_Camera.Features["ExposureTime"].FloatValue;
            Gain = m_VimbaHelper.m_Camera.Features["Gain"].FloatValue;
            var source = m_VimbaHelper.m_Camera.Features["TriggerSource"].EnumValues.ToList();
            SourceList = new ObservableCollection<string>(source);
            var activ = m_VimbaHelper.m_Camera.Features["TriggerActivation"].EnumValues.ToList();
            ActivList = new ObservableCollection<string>(activ);
            var selector = m_VimbaHelper.m_Camera.Features["TriggerSelector"].EnumValues.ToList();
            SelectorList = new ObservableCollection<string>(selector);
            var triggerMode = m_VimbaHelper.m_Camera.Features["TriggerMode"].EnumValues.ToList();
            TriggerModeList = new ObservableCollection<string>(triggerMode);
            var acqMode = m_VimbaHelper.m_Camera.Features["AcquisitionMode"].EnumValues.ToList();
            AcqModeList = new ObservableCollection<string>(acqMode);
        }
        public void GetCurrentFeaturesValue()
        {
            SourceIndex = SourceList.IndexOf(m_VimbaHelper.m_Camera.Features["TriggerSource"].EnumValue);
            ActivIndex = ActivList.IndexOf(m_VimbaHelper.m_Camera.Features["TriggerActivation"].EnumValue);
            SelectorIndex = SelectorList.IndexOf(m_VimbaHelper.m_Camera.Features["TriggerSelector"].EnumValue);
            TriggerModeIndex = TriggerModeList.IndexOf(m_VimbaHelper.m_Camera.Features["TriggerMode"].EnumValue);
            AcqModeIndex = AcqModeList.IndexOf(m_VimbaHelper.m_Camera.Features["AcquisitionMode"].EnumValue);
        }

        public void Start()
        {
            Camera.Pause();
            m_Acquiring = Camera.isPause ? false : true;
        }


        #endregion

        #region Views
        private void Setting_Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Setting_Grid.Focus();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (Camera.isStart)
            {
                m_VimbaHelper.m_FrameReceivedHandler -= this.OnFrameReceived;
                m_Acquiring = false;
            }
            this.Hide();
        }

        private void btnSoftTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (m_VimbaHelper == null || m_VimbaHelper.m_Camera == null)
            {
                var id = new Random().Next(1, 5);
                image = new HImage($"{id}.bmp");
            }
            else
            {
                m_VimbaHelper.TriggerSoftwareTrigger();
            }
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("fill");
            if (image != null)
            {
                display.DispObj(image);
            }
        }

        #endregion
        #region Event Hanlder
        private void OnFrameReceived(object sender, FrameEventArgs args)
        {
            // Start an async invoke in case this method was not
            // called by the GUI thread.

            if (true == Camera.isStart)
            {
                HImage image = args.Image;
                if (null != image && display != null)
                {
                    display.DispObj(image);
                }
            }
        }
        #endregion

    }
}
