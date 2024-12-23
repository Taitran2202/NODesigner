using DevExpress.Xpf.Charts;
using HalconDotNet;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Controls;
using NOVisionDesigner.Designer.Data;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class InspectionResultArgs
    {
        public int total, pass, fail;
        public bool result;
        public InspectionResultArgs(int total, int pass, int fail, bool result)
        {
            this.total = total;
            this.pass = pass;
            this.fail = fail;
            this.result = result;
        }

    }
    public delegate void InspectionComplete(InspectionResultArgs e);
    public delegate void OnImageAcquired(HImage image, ulong frame_count = 0,double fps=0);
    public delegate void OnDisconnected();
    public interface CameraInterface : IDisposable
    {
        bool IsRun { get; }
        string Type { get; }
        void Save(HFile file);
        void Load(DeserializeFactory item);
        void SoftwareTrigger();
        bool Connect();
        void Start();
        void Trigger();
        void Stop();
        void LiveView();
        OnImageAcquired ImageAcquired { get; set; }
        Nullable<double> GetFPS();
        bool IsHighSpeed { get; set; }
        void Reject();
        string TransformRecordPath(string record_path);
        bool IsRecordData();
        OnDisconnected OnCameraDisconnected { get; set; }
    }
    public class ResultGraphic : WindowGraphic
    {
        public int FontSize { get; set; }
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string NGBackground { get; set; } = "#ff0000ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string NGForeground { get; set; } = "#000000ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string OKBackground { get; set; } = "#00ff00ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string OKForeground { get; set; } = "#000000ff";
        public override void Display(HWindow display, InspectionContext e)
        {
            if (e.result)
            {
                display.DispText("OK", "image", Row, Column, OKForeground, new HTuple("box_color"), new HTuple(OKBackground));
            }
            else
            {
                display.DispText("NG", "image", Row, Column, NGForeground, new HTuple("box_color"), new HTuple(NGBackground));
            }
            
        }
        public override IDisplayable ToGraphic(InspectionContext e)
        {
            if (e.result)
            {
                return new DisplayText(OKForeground, "OK", OKBackground, Row, Column, 18);
                //display.DispText("OK", "image", X, Y, OKForeground, new HTuple("box_color"), new HTuple(OKBackground));
            }
            else
            {
                return new DisplayText(NGForeground, "NG", NGBackground, Row, Column, 18);
                //display.DispText("NG", "image", X, Y, NGForeground, new HTuple("box_color"), new HTuple(NGBackground));
            }
            
        }
    }
    public class FPSGraphic : WindowGraphic
    {
        public int FontSize { get; set; }
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Background { get; set; } = "#000000ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Foreground { get; set; } = "#ffffffff";
        public string StringFormat { get; set; } = "FPS: {0}";
        public override void Display(HWindow display, InspectionContext e)
        {
            try
            {
                display.DispText(string.Format(StringFormat, e.FPS), "image", Row, Column, Foreground, new HTuple("box_color"), new HTuple(Background));
            }catch(Exception ex)
            {


            }
          

        }
        public override IDisplayable ToGraphic(InspectionContext e)
        {
            try
            {
                return new DisplayText(Foreground, string.Format(StringFormat, e.FPS), Background, Row, Column, 18);
            }catch(Exception ex)
            {
                return null;
            }
            
        }
    }
    public class MultilineGraphic : WindowGraphic
    {
        public int FontSize { get; set; }
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Background { get; set; } = "#000000ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Foreground { get; set; } = "#ffffffff";
        public string StringFormat { get; set; } = "FPS: {0}";
        public override void Display(HWindow display, InspectionContext e)
        {
            try
            {
                display.DispText(string.Format(StringFormat, e.FPS), "image", Row, Column, Foreground, new HTuple("box_color"), new HTuple(Background));
            }
            catch (Exception ex)
            {


            }


        }
        public override IDisplayable ToGraphic(InspectionContext e)
        {
            try
            {
                return new DisplayText(Foreground, string.Format(StringFormat, e.FPS), Background, Row, Column, 18);
            }
            catch (Exception ex)
            {
                return null;
            }

        }
    }
    public class WindowGraphic:IHalconDeserializable
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public double Row { get; set; }
        public double Column { get; set; }
        public virtual void Display(HWindow display,InspectionContext e)
        {

        }
        public virtual IDisplayable ToGraphic(InspectionContext e)
        {
            return null;
        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class Accquisition : INotifyPropertyChanged,IHalconDeserializable,IDisposable
    {

        public ObservableCollection<WindowGraphic> GraphicsList { get; set; } = new ObservableCollection<WindowGraphic>();
        public void InsertGraphics(InspectionContext e)
        {
            e.AddGraphics(GraphicsList.Select(x => x.ToGraphic(e)));
        }
        bool _DisableImageProcessing = false;
        public bool DisableImageProcessing
        {
            get
            {
                return _DisableImageProcessing;
            }
            set
            {
                if (_DisableImageProcessing != value)
                {
                    _DisableImageProcessing = value;
                    RaisePropertyChanged("DisableImageProcessing");
                }
            }
        }
        bool _apply_rotation=false;
        public bool ApplyRotation
        {
            get
            {
                return _apply_rotation;
            }
            set
            {
                if (_apply_rotation != value)
                {
                    _apply_rotation = value;
                    RaisePropertyChanged("ApplyRotation");
                }
            }
        }

        int _orientation=0;
        public int Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    RaisePropertyChanged("Orientation");
                }
            }
        }
        public void ShowFilmstrip(Control sender)
        {
            FilmstripWindow wd = new FilmstripWindow(this);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();
        }
        public void ShowCameraSetting(Control sender)
        {
            FilmstripWindow wd = new FilmstripWindow(this);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();
        }
        public RecordImage Record { get; set; } = new RecordImage();
        static Accquisition()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.AccquisitionView(), typeof(IViewFor<Accquisition>));
        }
        bool _is_enabled = true;
        public bool IsEnabledTool
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                    RaisePropertyChanged("IsEnabledTool");
                }
            }
        }
        public void ClearAll()
        {

        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        
        double _ppm;
        [SerializeIgnore]
        public double PPM
        {
            get
            {
                return _ppm;
            }
            set
            {
                if (_ppm != value)
                {
                    _ppm = value;
                    RaisePropertyChanged("PPM");
                }
            }
        }

        public bool _millite_external_trigger = true;
        public void Reset()
        {

        }
        public bool record_pass = false;
        public bool record_fail = true;
        public void Dispose()
        {
            Interface?.Dispose();
        }
        public Calibration Calib { get; set; } = new Calibration();
        public Recorder recorder;
        public EventHandler StatusChanged;
        public HImage gImage_last_fail = new HImage();

        public EventHandler OnFailResult;
        public InspectionComplete OnInspectionComplete;
        public bool Rejector = true;
        public void Save(HFile file)
        {
            new HTuple(Interface == null ? 0 : 1).SerializeTuple().FwriteSerializedItem(file);
            if (Interface != null)
            {
                new HTuple(Interface.Type).SerializeTuple().FwriteSerializedItem(file);
                Interface.Save(file);
            }
            //Calib.Save(file);
            HelperMethods.SaveParam(file, this);
        }
        public OutputSource<ImageData> OutputImage = new OutputSource<ImageData>();
        public void Load(DeserializeFactory item)
        {
            if (item.DeserializeTuple() == 1)
            {
                string type = item.DeserializeTuple();
                switch (type)
                {

                    case "GigEVision2": Interface = new GigEVision2(); break;
                    case "GigEVision2Script": Interface = new GigEVision2Script(); break;
                    case "EBUS": Interface = new EBUS(); break;
                    case "Sapera": Interface = new Sapera(); break;
                    case "GigEVisionBasler": Interface = new GigEVisionBasler(); break;
                    case "GigEVisionHIK": Interface = new GigEVisionHIK(); break;
                    case "GigEVisionVimba": Interface = new GigEVisionVimba(basedir); break;
                    case "USB3VisionVimba": Interface = new USB3VisionVimba(basedir); break;
                    case "USB3Vision": Interface = new USB3Vision(); break;
                    default: Interface = null; break;
                }
                //MessageBox.Show("aaa");
                if (Interface != null)
                {
                    Interface.Load(item);
                }
            }
            else
            {
                Interface = null;
            }
            HelperMethods.LoadParam(item, this);
            //Calib.Load(item);
        }
        public void Load(DeserializeFactory item, string version)
        {

            string interface_s = item.DeserializeTuple();
            string DeviceName = item.DeserializeTuple();
            string TriggerMode = item.DeserializeTuple();
            string TriggerSource = item.DeserializeTuple();
            double ExposureTime = item.DeserializeTuple();
            string temp = item.DeserializeTuple();
            //MainWindow.LogList.Add("Interface -> " + interface_s);
            Calib.LoadOldVersion2(item);
        }
        

        public Queue<string> id_cam;
        double _error_rate;
        [SerializeIgnore]
        public double ErrorRate
        {
            get
            {
                return _error_rate;
            }
            set
            {
                if (_error_rate != value)
                {
                    _error_rate = value;
                    RaisePropertyChanged("ErrorRate");
                }
            }
        }
        public string ConvertDefectData(InspectionResult inspect_result)
        {
            List<DefectRegionInfo> defects = new List<DefectRegionInfo>();
            foreach (RegionInfo region in inspect_result.regions)
            {
                HTuple feature = region.region.RegionFeatures(new HTuple("row1", "column1", "row2", "column2", "area"));
                int feature_count = 0;
                if (feature.Length / 5 >= 10)
                {
                    feature_count = 10;
                }
                else
                {
                    feature_count = (int)(feature.Length / 5);
                }
                for (int i = 0; i < feature_count; i++)
                {
                    defects.Add(new DefectRegionInfo(feature[i * 5] - 1, feature[i * 5 + 1] - 1, feature[5 * i + 2] + 1, feature[5 * i + 3] + 1, region.Name, region.Type, feature[5 * i + 4] / (inspect_result.scale_x * inspect_result.scale_y)));
                }

            }
            return JsonConvert.SerializeObject(defects);
        }
        Random rnd = new Random();
        public void OpenImage(string path)
        {
            try
            {
                HImage grabbed = new HImage(path);
                if (grabbed.CountChannels() == 4)
                {
                    var im1 = grabbed.Decompose4(out HImage im2, out HImage im3, out HImage im4);
                    grabbed = im1.Compose3(im2, im3);
                }
                lock (image_lock)
                {
                    CurrentImage.Dispose();
                    CurrentImage = new ImageData(grabbed.CopyImage(),CurrentImage.FrameId,false);
                    OutputImage.OnNext(CurrentImage);
                };
            }
            catch (Exception ex)
            {

            }

        }
        public void SetImage(HImage image)
        {
            try
            {
                HImage grabbed = image;
                lock (image_lock)
                {
                    CurrentImage.Dispose();
                    CurrentImage = new ImageData(grabbed.CopyImage(), CurrentImage.FrameId, true);
                    OutputImage.OnNext(CurrentImage);
                };
            }
            catch (Exception ex)
            {

            }
        }
        public void OpenImage()
        {
            System.Windows.Forms.OpenFileDialog open = new System.Windows.Forms.OpenFileDialog();

            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Task.Run((Action)(() =>
                {

                    OpenImage(open.FileName);
                    
                }));
            }
            // throw new Exception();
        }
        public void Remove()
        {

        }
        public bool is_record = false;

        public static int tool_count = 1;

        private bool is_run = false;
        private string _name;
        [SerializeIgnore]
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
                    StatusChanged?.Invoke(value, null);
                    RaisePropertyChanged("IsRun");
                }
                
            }
        }
        #region Properties for Gige
        //public string TriggerMode
        //{
        //    get
        //    {
        //        return _trigger_mode;
        //    }

        //    set
        //    {
        //        _trigger_mode = value;
        //    }
        //}
        //public string TriggerSource
        //{
        //    get
        //    {
        //        return _trigger_source;
        //    }

        //    set
        //    {
        //        _trigger_source = value;
        //    }
        //}
        //public string DeviceName
        //{
        //    get
        //    {
        //        return _device_name;
        //    }

        //    set
        //    {
        //        _device_name = value;
        //    }
        //}
        //public int ExposureTime        {
        //    get
        //    {
        //        return _exposure_time;
        //    }

        //    set
        //    {
        //        _exposure_time = value;
        //        if (FrameGrabber.IsInitialized())
        //        {
        //            if (_acq_interface == "MILLite")
        //                return;
        //            FrameGrabber.SetFramegrabberParam("ExposureTimeAbs", value * 100);
        //        }

        //    }
        //}

        //public string Acq_interface
        //{
        //    get
        //    {
        //        return _acq_interface;
        //    }

        //    set
        //    { _acq_interface = value; }
        //}
        #endregion

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }



        public void Update(object sender)
        {
            Run();
        }
        public void Run()
        {

        }
        public void AddFailImage(HImage image)
        {

        }

        public void Stop()
        {

            try
            {
                #region for old gige
                //switch (_acq_interface)
                //{
                //    case "GigEVision":
                //        FrameGrabber.SetFramegrabberParam("do_abort_grab", 1);
                //        FrameGrabber.SetFramegrabberParam("grab_timeout", 1);
                //        break;
                //    case "MILLite":
                //       // FrameGrabber.SetFramegrabberParam("do_abort_grab", 1);
                //        FrameGrabber.SetFramegrabberParam("grab_timeout", 1);

                //        break;
                //    case "MILLiteCallback":

                //        break;
                //}
                #endregion

                Interface?.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                //MainWindow.ShowMessage(ex.Message, MessageType.Error);
            }
            IsRun = false;
            // FrameGrabber.SetFramegrabberParam("TriggerMode", "Off");

        }

        public void Connect()
        {
            try
            {
                //Interface?.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ": " + ex.Message);
            }
        }

        private string record_path = "";
        public string _record_image_format = "jpg";
        public int pass = 0;
        public int fail = 0;
        public int total = 0;
        public void ResetCount()
        {
            total = 0;
            pass = 0;
            fail = 0;
        }
        public EventHandler OnLiveStop;
        public EventHandler OnLiveImage;
        public void LiveImage()
        {
            //if (_acq_interface == "GigEVision")
            //{
            //    Task handle = Task.Run((Action)(() =>
            //    {
            //        Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //        try
            //        {

            //            FrameGrabber.SetFramegrabberParam("grab_timeout", -1);
            //            FrameGrabber.SetFramegrabberParam("TriggerMode", "On");
            //            FrameGrabber.SetFramegrabberParam("AcquisitionStart", 1);
            //            this.display.SetWindowParam("flush", "false");
            //            FrameGrabber.GrabImageStart(-1);

            //        }
            //        catch (Exception ex)
            //        {
            //            IsRun = false;
            //            return;
            //        }


            //        // IsRun = true;
            //        is_run = true;
            //        while (is_run)
            //        {
            //            try
            //            {
            //                //  MessageBox.Show("Begin online");
            //                HImage grabbed;
            //                grabbed = FrameGrabber.GrabImageAsync(-1);
            //                gImage_current = grabbed;
            //                this.display.DispObj(grabbed);
            //                this.display.FlushBuffer();
            //            }
            //            catch (Exception ex)
            //            {
            //                OnLiveStop?.Invoke(null, null);
            //                return;
            //            }
            //        }
            //        OnLiveStop?.Invoke(null, null);
            //    }));
            //    return;
            //}

            //if (_acq_interface == "MILLite")
            //{
            //    matrox_framegrabber = MatroxInterface.MatroxInterface.Instance;

            //    Task handle = Task.Run(() =>
            //    {
            //        matrox_framegrabber.GrabImageStart();
            //        IsRun = true;
            //        while (is_run)
            //        {
            //            ////try
            //            ////{
            //            //  MessageBox.Show("Begin online");
            //            try
            //            {
            //                HImage grabbed = matrox_framegrabber.GrabImage();
            //                if (grabbed == null)
            //                    continue;
            //                gImage_current = grabbed;

            //                OnLiveImage?.Invoke(grabbed, null);


            //            }
            //            catch (Exception ex)
            //            {
            //                OnLiveStop?.Invoke(null, null);
            //                IsRun = false;
            //                return;
            //                //}
            //            }
            //        }
            //        OnLiveStop?.Invoke(null, null);
            //    });
            //    return;
            //}

        }

        //public class 
        //public ChartCollectionSetting charts;

        public Task record_task;
        bool last_result = false;
        public bool limit_image = false;
        public int image_count = 0;
        public int image_max = 1000;
        public object image_lock = new object();


        public void OnCallback(HImage grabbed, ulong frame_count = 0,double fps =0)
        {
            
            lock (image_lock)
            {
                CurrentImage.Dispose();
                if (_apply_rotation)
                {
                    if (_orientation % 360 != 0)
                    {
                        CurrentImage = new ImageData(grabbed.RotateImage((double)_orientation , "constant"), frame_count, false,fps);
                    }
                    else
                    {
                        CurrentImage = new ImageData(grabbed.CopyImage(), frame_count, false,fps);
                    }
                }
                else
                {
                    CurrentImage = new ImageData(grabbed.CopyImage(), frame_count, false, fps);
                }
                
                
                OutputImage.OnNext(CurrentImage);
            }   
            grabbed.Dispose();
            GC.Collect(0, GCCollectionMode.Default, false);
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
                    RaisePropertyChanged("Interface");
                }
            }
        }

        public ImageData CurrentImage = new ImageData(new HImage(),0,false);



        public void Start()
        {
            if (Interface == null)
                return;
            IsRun = true;
            //ChangeRecordPathByDay();
            //start camera
            Interface.ImageAcquired = OnCallback;
            Interface.OnCameraDisconnected = OnDisconnected;
            try
            {
                Interface.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }
        
        public void OnDisconnected()
        {
            IsRun = false;
        }

        public void DisplayLastFailImage()
        {
            if (gImage_last_fail.CountObj() > 0)
            {
                
            }

        }
        public HImage GetLastImage()
        {
            if (CurrentImage.Image.CountObj() > 0)
            {
                return CurrentImage.Image;
            }
            else
                return null;
        }
        
        public void DisplaygImage()
        {
            if (CurrentImage.Image.IsInitialized())
            {
                Task.Run(new Action(() =>
                {
                    OutputImage.OnNext(CurrentImage);
                }
               ));
            }

        }
        public bool SaveDetail = false;
        public DatabaseRecord records;
        public class Dateandpath
        {
            public string path;
            public DateTime date;
            public Dateandpath(DateTime date, string path)
            {
                this.date = date;
                this.path = path;
            }
        }
        //string default_app_path = System.IO.Directory.GetCurrentDirectory();
        //public string record_sub_path;
        //public string record_sub_path_current_shift = "";
        //public void ChangeRecordPathByDay()
        //{
        //    if (!Directory.Exists(Record_path))
        //        Directory.CreateDirectory(Record_path);

        //    string record_sub_path = Record_path + "\\" + DateTime.Now.ToString("dd-MM-yyyy");
        //    if (!Directory.Exists(record_sub_path))
        //        Directory.CreateDirectory(record_sub_path);
        //    this.record_sub_path = record_sub_path;


        //}
        bool _reset_shift;
        public bool ResetShift
        {
            get
            {
                return _reset_shift;
            }
            set
            {
                if (_reset_shift != value)
                {
                    _reset_shift = value;
                    RaisePropertyChanged("ResetShift");
                }
            }
        }
        //public string record_day_path = "";
        //public XYDiagram2D plot;
        //public DatabaseRecord.PrintEvent PrintEvent;
        //public void ChangeRecordInShift(Shift current_shift, bool is_previous_day)
        //{
        //    string record_sub_path = Record_path + "\\" + DateTime.Now.ToString("dd-MM-yyyy");
        //    if (!Directory.Exists(record_sub_path))
        //        Directory.CreateDirectory(record_sub_path);
        //    this.record_sub_path = record_sub_path;


        //    //if (!Directory.Exists(record_sub_path))
        //    //    Directory.CreateDirectory(record_sub_path);

        //    string record_day_path;

        //    if (is_previous_day)
        //    {
        //        record_day_path = Record_path + "\\" + DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy");
        //    }
        //    else
        //    {

        //        record_day_path = record_sub_path;
        //    }
        //    this.record_day_path = record_day_path;
        //    //if (new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) > new TimeSpan(7, 29, 0) & new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) < new TimeSpan(19, 28, 0))
        //    //{
        //    //    //day shift
        //    string record_sub_path_current_shift = record_day_path + "\\" + current_shift.Name;
        //    if (!Directory.Exists(record_sub_path_current_shift))
        //        Directory.CreateDirectory(record_sub_path_current_shift);

        //    this.record_sub_path_current_shift = record_sub_path_current_shift;
        //}
        //public void ChangeRecordPathByDayShift(Shift current_shift, bool is_previous_day, bool shift_event)
        //{
        //    try
        //    {

        //        if (shift_event)
        //        {
        //            if (current_shift.IsReset)
        //                ResetCount();

        //            PrintEvent?.Invoke(records.tagfactory.StartTime, records.tagfactory.ShiftInfo);
        //            //if (current_shift.IsReset)
        //            //{

        //            //}

        //            //if (!is_previous_day)
        //            //{

        //            records.tagfactory.Update(DateTime.Now.Date.Add(current_shift.ShiftTime), current_shift.Name);
        //            //records.tagfactory.Initialize(plot);
        //            //}
        //            //else
        //            //{
        //            //    records.tagfactory.Update(DateTime.Now.Date.AddDays(-1).Add(current_shift.ShiftTime));
        //            //    //records.tagfactory.Initialize(plot);
        //            //}
        //            //records.tagfactory.ShiftInfo = current_shift.Name;
        //        }
        //        else
        //        {
        //            if (current_shift.Name == records.tagfactory.ShiftInfo)
        //            {

        //                if (records.tagfactory.StartTime.Date != current_shift.DateStart.Date)
        //                {
        //                    PrintEvent?.Invoke(records.tagfactory.StartTime, records.tagfactory.ShiftInfo);
        //                    //if (is_previous_day)    
        //                    records.tagfactory.Update((current_shift.DateStart.Add(current_shift.ShiftTime)), current_shift.Name);
        //                    //else
        //                    //    records.tagfactory.Update(DateTime.Now.Date.Add(current_shift.ShiftTime));
        //                }
        //            }
        //            else
        //            {

        //                PrintEvent?.Invoke(records.tagfactory.StartTime, records.tagfactory.ShiftInfo);
        //                //if (is_previous_day)
        //                records.tagfactory.Update((current_shift.DateStart.Add(current_shift.ShiftTime)), current_shift.Name);
        //            }
        //        }



        //        //check daily record path again
        //        string record_sub_path = Record_path + "\\" + DateTime.Now.ToString("dd-MM-yyyy");
        //        if (!Directory.Exists(record_sub_path))
        //            Directory.CreateDirectory(record_sub_path);
        //        this.record_sub_path = record_sub_path;


        //        //if (!Directory.Exists(record_sub_path))
        //        //    Directory.CreateDirectory(record_sub_path);

        //        string record_day_path;

        //        if (is_previous_day)
        //        {
        //            record_day_path = Record_path + "\\" + DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy");
        //        }
        //        else
        //        {

        //            record_day_path = record_sub_path;
        //        }
        //        this.record_day_path = record_day_path;
        //        //if (new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) > new TimeSpan(7, 29, 0) & new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) < new TimeSpan(19, 28, 0))
        //        //{
        //        //    //day shift
        //        string record_sub_path_current_shift = record_day_path + "\\" + current_shift.Name;
        //        if (!Directory.Exists(record_sub_path_current_shift))
        //            Directory.CreateDirectory(record_sub_path_current_shift);

        //        this.record_sub_path_current_shift = record_sub_path_current_shift;
        //        //}
        //        //else
        //        //{
        //        //    //night shift
        //        //    string record_sub_path_current_shift = record_day_path + "\\NightShift";
        //        //    if (!Directory.Exists(record_sub_path_current_shift))
        //        //        Directory.CreateDirectory(record_sub_path_current_shift);
        //        //    this.record_sub_path_current_shift = record_sub_path_current_shift;
        //        //}

        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}
        //public void ChangeRecordPathCustomShift()
        //{
        //    if (records.previous_shift != null)
        //    {
        //        string record_sub_path_current_shift = record_day_path + "\\" + records.previous_shift.Name;
        //        if (!Directory.Exists(record_sub_path_current_shift))
        //            Directory.CreateDirectory(record_sub_path_current_shift);
        //        this.record_sub_path_current_shift = record_sub_path_current_shift;
        //    }

        //}
        //public bool save_overlay = false;
        public string basedir;
        public Accquisition(string basedir)
        {
            //string record_directory = MainWindow.AppPath + "Report\\";
            //if (!Directory.Exists(record_directory))
            //{
            //    Directory.CreateDirectory(record_directory);
            //}

            //records = new DatabaseRecord(record_directory + "Record.db");
            //// MessageBox.Show("create database ok");
            //records.ExtendDailyTrigger = ChangeRecordPathByDay;
            //records.ExtendDailyTriggerShift = ChangeRecordPathByDayShift;

            //if (!Directory.Exists(default_app_path + "\\RecordImages\\"))
            //{
            //    Directory.CreateDirectory(default_app_path + "\\RecordImages\\");
            //}
            //Record_path = default_app_path + "\\RecordImages\\";
            //ChangeRecordPathByDay();
            recorder = new Recorder();

            this.basedir = basedir;
            // imagesource.Image.GenEmptyObj();
            // imagesource.inspect_result = new InspectionResult(imagesource.Image,calib._scale);
            try
            {
                gImage_last_fail.GenEmptyObj();
                CurrentImage.Image.GenEmptyObj();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            this.Name = "Accquisition" + tool_count.ToString();
        }

        public void Trigger()
        {
            // FrameGrabber.SetFramegrabberParam("TriggerSoftware", 1);
            Interface.Trigger();
        }


        public void Capture()
        {
            if (Interface == null)
                return;           
            IsRun = true;
            //start camera
            Interface.ImageAcquired = OnCallback;
            Interface.OnCameraDisconnected = OnDisconnected;
            Interface.Trigger();

        }

        public void OnLoadComplete()
        {

        }
    }
    public class ImageData
    {
        public HImage Image { get; set; }
        public ulong FrameId { get; set; }
        public double FPS { get; set; }
        public bool IsPlayback { get; set; }
        public ImageData(HImage Image,ulong FrameId, bool IsPlayback, double FPS=0)
        {
            this.Image = Image;
            this.FrameId = FrameId;
            this.IsPlayback = IsPlayback;
            this.FPS = FPS;
        }
        public void Dispose()
        {
            this.Image.Dispose();
        }
    }
    public class ImageWriter
    {
        ManualResetEvent mutex = new ManualResetEvent(false);

    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Tag : INotifyPropertyChanged
    {
        [JsonProperty]
        public Chart chart { get; set; }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        int _tag_value;
        [JsonProperty]
        public int TagValue
        {
            get
            {
                return _tag_value;
            }
            set
            {
                if (_tag_value != value)
                {
                    _tag_value = value;

                    RaisePropertyChanged("TagValue");
                }
            }
        }
        bool _enable = true;
        [JsonProperty]
        public bool Enable
        {
            get
            {
                return _enable;
            }
            set
            {
                if (_enable != value)
                {
                    _enable = value;
                    RaisePropertyChanged("Enable");
                }
            }
        }
        Visibility _visible_up = Visibility.Visible;
        public Visibility VisibleUp
        {
            get
            {
                return _visible_up;
            }
            set
            {
                if (_visible_up != value)
                {
                    _visible_up = value;
                    RaisePropertyChanged("VisibleUp");
                }
            }
        }
        Visibility _visible_down = Visibility.Hidden;
        public Visibility VisibleDown
        {
            get
            {
                return _visible_down;
            }
            set
            {
                if (_visible_down != value)
                {
                    _visible_down = value;
                    RaisePropertyChanged("VisibleDown");
                }
            }
        }
        double _percent_diff = 0;
        [JsonProperty]
        public double PercentDiff
        {
            get
            {
                return _percent_diff;
            }
            set
            {
                if (_percent_diff != value)
                {
                    _percent_diff = value;
                    RaisePropertyChanged("PercentDiff");
                }
            }
        }

        //public int TagValue { get; set; }
        string _tag_name;
        [JsonProperty]
        public string TagName
        {
            get
            {
                return _tag_name;
            }
            set
            {
                if (_tag_name != value)
                {
                    _tag_name = value;
                    chart.line.DisplayName = value;
                    RaisePropertyChanged("TagName");
                }
            }
        }

        //public string TagName { get; set;}
        //public double Percent { get; set; }
        double _percent;
        [JsonProperty]
        public double Percent
        {
            get
            {
                return _percent;
            }
            set
            {
                if (_percent != value)
                {
                    _percent = value;
                    RaisePropertyChanged("Percent");
                }
            }
        }

        public void ComparePercent()
        {
            PercentDiff = Math.Abs(Percent - PercentTarget);
            if (_percent_target < _percent)
            {
                VisibleDown = Visibility.Hidden;
                VisibleUp = Visibility.Visible;
            }
            else
            {
                VisibleDown = Visibility.Visible;
                VisibleUp = Visibility.Hidden;
            }
        }
        double _percent_target = 0.03;
        [JsonProperty]
        public double PercentTarget
        {
            get
            {
                return _percent_target;
            }
            set
            {
                if (_percent_target != value)
                {
                    _percent_target = value;
                    RaisePropertyChanged("PercentTarget");
                }
            }
        }
        public void Reset()
        {
            chart?.Reset();
            TagValue = 0;
            Percent = 0;
            ComparePercent();

        }
        public Tag(string Name)
        {

            chart = new Chart();
            this.TagName = Name;
        }
        public Tag()
        {
            chart = new Chart();
        }

    }
    public class Count
    {
        public double Total { get; set; } = 0;
        public double Fail { get; set; } = 0;
        public void Reset()
        {
            Total = 0;
            Fail = 0;
        }
    }

    public class DataDatePoint
    {
        public double Value { get; set; }
        public DateTime Time { get; set; }
        public DataDatePoint(DateTime date, double value)
        {
            Time = date;
            Value = value;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Chart
    {
        public void Reset()
        {
            // Summary();
            CountLow.Reset();
            CountHigh.Reset();
            Points.Clear();

        }
        public Chart()
        {
            line.DataSource = Points;
            line.ArgumentDataMember = "Time";
            line.ValueDataMember = "Value";
        }
        [JsonProperty]
        public Count CountLow { get; set; } = new Count();
        [JsonProperty]
        public Count CountHigh { get; set; } = new Count();


        public LineSeries2D line { get; set; } = new LineSeries2D() { };

        [JsonProperty]
        public List<DataDatePoint> Points { get; set; } = new List<DataDatePoint>();

        public void UpdateTotal(bool speed)
        {
            if (speed)
                CountHigh.Total++;
            else
                CountLow.Total++;
        }
        public void Add(bool speed)
        {
            if (speed)
                CountHigh.Fail++;
            else
                CountLow.Fail++;
            //Points.Add(new DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(time), Count/total));
        }

        public void Summary()
        {
            double totalcount = CountHigh.Total + CountLow.Total;
            double failcount = CountHigh.Fail + CountLow.Fail;
            CountLow.Reset();
            CountHigh.Reset();
            if (Points.Count > 144)
            {
                Points.RemoveAt(0);
            }
            if (totalcount > 0)
                Points.Add(new DataDatePoint(DateTime.Now, failcount * 100 / totalcount));
            else
            {
                Points.Add(new DataDatePoint(DateTime.Now, 0));
            }

        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class TagFactory : INotifyPropertyChanged
    {

        string _comments1;
        [JsonProperty]
        public string Comments1
        {
            get
            {
                return _comments1;
            }
            set
            {
                if (_comments1 != value)
                {
                    _comments1 = value;
                    RaisePropertyChanged("Comments1");
                }
            }
        }
        string _comments2;
        [JsonProperty]
        public string Comments2
        {
            get
            {
                return _comments2;
            }
            set
            {
                if (_comments2 != value)
                {
                    _comments2 = value;
                    RaisePropertyChanged("Comments2");
                }
            }
        }


        string _shift_info;
        [JsonProperty]
        public string ShiftInfo
        {
            get
            {
                return _shift_info;
            }
            set
            {
                if (_shift_info != value)
                {
                    _shift_info = value;
                    RaisePropertyChanged("ShiftInfo");
                }
            }
        }


        [JsonProperty]
        public int ChartMaxPercent { get; set; } = 20;
        [JsonProperty]
        public int MaxHours { get; set; } = 8;
        DateTime _start_time;
        [JsonProperty]
        public DateTime StartTime
        {
            get
            {
                return _start_time;
            }
            set
            {
                if (_start_time != value)
                {
                    _start_time = value;
                    RaisePropertyChanged("StartTime");
                }
            }
        }

        //public string Duration { get; set; } = "aaa";
        string _duration;
        [JsonProperty]
        public string Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    RaisePropertyChanged("Duration");
                }
            }
        }

        public void Summary()
        {
            TagGlobal.chart.Summary();
            foreach (Tag tag in CurrentTag)
            {
                tag.chart.Summary();
            };
            Duration = Math.Round((DateTime.Now - StartTime).TotalHours, 1).ToString();
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void UpdateTag(NotifyMessage message, bool speed)
        {
            if (message.result)
            {
                Pass++;
            }
            else
            {
                TagGlobal.chart.Add(speed);
                TagGlobal.TagValue++;
                Fail++;
            }
            TagGlobal.chart.UpdateTotal(speed);
            Total++;
            PercentPass = (double)Pass / Total;
            PercentFail = (double)Fail / Total;
            foreach (Tag tag in CurrentTag)
            {
                tag.chart.UpdateTotal(speed);
                if (!message.result)
                {
                    //foreach (string tag_detected in message.DefectTag)
                    //{
                    if (message.DefectTag.Contains(tag.TagName))
                    {
                        tag.chart.Add(speed);
                        tag.TagValue++;
                    }
                    //if (tag.TagName == tag_detected)
                    //{
                    //    //chart.Add(timenow);
                    //    tag.chart.Add(speed);
                    //    tag.TagValue++;
                    //    break;
                    //}
                    //}
                }
                tag.Percent = (double)tag.TagValue / (double)Total;
                tag.ComparePercent();
                // if (tag.Percent<tag.PercentTarget)
            }
            TagGlobal.Percent = (double)TagGlobal.TagValue / Total;
        }
        [JsonProperty]
        public ObservableCollection<Tag> CurrentTag = new ObservableCollection<Tag>();
        public Tag TagGlobal = new Tag("Global");
        public void AddTag(string tagname)
        {
            Tag newtag = new Tag(tagname);
            CurrentTag.Add(newtag);
            if (control != null)
            {
                control.Series.Add(newtag.chart.line);
                newtag.chart.line.DataSource = newtag.chart.Points;
                //}
                //(control.Axes[0] as DateTimeAxis).Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Now - TimeSpan.FromMinutes(5));
                //(control.Axes[0] as DateTimeAxis).Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Now + TimeSpan.FromMinutes(5));
            }
        }
        XYDiagram2D control;
        public void Initialize(XYDiagram2D control)
        {
            this.control = control;
            if (TagGlobal.chart.Points.Count > 144)
            {
                TagGlobal.chart.Points.RemoveRange(0, TagGlobal.chart.Points.Count - 143);
            }


            while (TagGlobal.chart.Points.Count > 0)
            {
                if (TagGlobal.chart.Points[0].Time.AddHours(24) < DateTime.Now)
                {
                    TagGlobal.chart.Points.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
            foreach (Tag tag in CurrentTag)
            {

                if (tag.chart.Points.Count > 144)
                {
                    tag.chart.Points.RemoveRange(0, tag.chart.Points.Count - 143);
                }


                while (tag.chart.Points.Count > 0)
                {
                    if (tag.chart.Points[0].Time.AddHours(24) < DateTime.Now)
                    {
                        tag.chart.Points.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }

                control.Series.Add(tag.chart.line);
            };
            //control.Axes[0].Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(StartTime);
            //control.Axes[0].Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(StartTime.AddHours(MaxHours));

            //control.Axes[1].Minimum = 0;
            //control.Axes[1].Maximum = ChartMaxPercent;

        }
        int _total;
        [JsonProperty]
        public int Total
        {
            get
            {
                return _total;
            }
            set
            {
                if (_total != value)
                {
                    _total = value;
                    RaisePropertyChanged("Total");
                }
            }
        }
        int _pass;
        [JsonProperty]
        public int Pass
        {
            get
            {
                return _pass;
            }
            set
            {
                if (_pass != value)
                {
                    _pass = value;
                    RaisePropertyChanged("Pass");
                }
            }
        }
        int _fail;
        [JsonProperty]
        public int Fail
        {
            get
            {
                return _fail;
            }
            set
            {
                if (_fail != value)
                {
                    _fail = value;
                    RaisePropertyChanged("Fail");
                }
            }
        }
        double _percent_pass;
        [JsonProperty]
        public double PercentPass
        {
            get
            {
                return _percent_pass;
            }
            set
            {
                if (_percent_pass != value)
                {
                    _percent_pass = value;
                    RaisePropertyChanged("PercentPass");
                }
            }
        }
        double _percent_fail;
        [JsonProperty]
        public double PercentFail
        {
            get
            {
                return _percent_fail;
            }
            set
            {
                if (_percent_fail != value)
                {
                    _percent_fail = value;
                    RaisePropertyChanged("PercentFail");
                }
            }
        }
        private TagFactory()
        {

        }
        public void Update(DateTime starttime, string shiftname)
        {
            Summary();
            ShiftInfo = shiftname;
            StartTime = starttime;

            TagGlobal.Reset();
            foreach (var tag in CurrentTag)
            {
                tag.Reset();
            }
            Total = 0;
            Pass = 0;
            Fail = 0;
            PercentFail = 0;
            PercentPass = 0;
            if (control != null)
            {
                //control.Axes[0].Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(starttime);
                //control.Axes[0].Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(starttime.AddHours(MaxHours));

                //control.Axes[1].Minimum = 0;
                //control.Axes[1].Maximum = ChartMaxPercent;
                //control.InvalidatePlot(true);
            }

        }
        public void Save()
        {

            if (File.Exists(MainWindow.AppPath + "tagsetting.txt"))
            {
                File.Copy(MainWindow.AppPath + "tagsetting.txt", MainWindow.AppPath + "tagsetting1.txt", true);
            }
            using (StreamWriter file = File.CreateText(MainWindow.AppPath + "tagsetting.txt"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        }
        private static TagFactory _instance;
        public static TagFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = TagFactory.Load();
                    return _instance;
                }
                else
                {
                    return _instance;
                }
            }
        }
        private static TagFactory Load()
        {
            if (File.Exists((MainWindow.AppPath + "tagsetting.txt")))
                using (StreamReader file = File.OpenText(MainWindow.AppPath + "tagsetting.txt"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    TagFactory result = null;
                    try
                    {
                        result = (TagFactory)serializer.Deserialize(file, typeof(TagFactory));
                    }
                    catch (Exception ex)
                    {

                    }

                    if (result == null)
                    {
                        if (File.Exists((MainWindow.AppPath + "tagsetting1.txt")))
                        {
                            using (StreamReader file1 = File.OpenText(MainWindow.AppPath + "tagsetting1.txt"))
                            {
                                try
                                {
                                    result = (TagFactory)serializer.Deserialize(file1, typeof(TagFactory));
                                }
                                catch (Exception ex)
                                {

                                }
                                if (result != null)
                                {
                                    file.Close();
                                    File.Copy(MainWindow.AppPath + "tagsetting1.txt", MainWindow.AppPath + "tagsetting.txt", true);

                                    return result;
                                }
                                return new TagFactory() { StartTime = DateTime.Now };
                            }
                        }
                        else
                        {
                            return new TagFactory() { StartTime = DateTime.Now };
                        }
                    }
                    return result;

                }
            return new TagFactory() { StartTime = DateTime.Now };

        }
    }
    public class DatabaseRecord : INotifyPropertyChanged, IDisposable
    {
        public TagFactory tagfactory;

        public void WriteDatabase()
        {
            if (!_is_enable)
                return;
            //if (current_id > max_record)
            //{
            //    return;
            //}
            string recordtime = DateTime.Now.ToString(DateFormat);

            //add highspeed global
            // current_id++;
            DataRow row0 = mTable.NewRow();
            row0[1] = recordtime;
            row0[2] = MainWindow.current_program_name;
            row0[3] = tagfactory.TagGlobal.TagName;
            row0[4] = tagfactory.TagGlobal.chart.CountHigh.Total;
            row0[5] = tagfactory.TagGlobal.chart.CountHigh.Fail;
            if (tagfactory.TagGlobal.chart.CountHigh.Total > 0)
            {

                row0[6] = tagfactory.TagGlobal.chart.CountHigh.Fail * 100 / tagfactory.TagGlobal.chart.CountHigh.Total;

            }
            else
            {
                row0[6] = 0;


            }
            row0[7] = true;
            row_added.Enqueue(row0);

            //add lowspeed global
            //current_id++;
            DataRow row3 = mTable.NewRow();
            row3[1] = recordtime;
            row3[2] = MainWindow.current_program_name;
            row3[3] = tagfactory.TagGlobal.TagName;
            row3[4] = tagfactory.TagGlobal.chart.CountLow.Total;
            row3[5] = tagfactory.TagGlobal.chart.CountLow.Fail;
            if (tagfactory.TagGlobal.chart.CountLow.Total > 0)
            {

                row3[6] = tagfactory.TagGlobal.chart.CountLow.Fail * 100 / tagfactory.TagGlobal.chart.CountLow.Total;

            }
            else
            {
                row3[6] = 0;


            }
            row3[7] = false;
            row_added.Enqueue(row3);


            //add other tag
            foreach (Tag tag in tagfactory.CurrentTag)
            {

                //current_id++;
                DataRow row = mTable.NewRow();
                //row[0] = current_id;
                row[1] = recordtime;
                row[2] = MainWindow.current_program_name;
                row[3] = tag.TagName;
                row[4] = tag.chart.CountHigh.Total;
                row[5] = tag.chart.CountHigh.Fail;
                if (tag.chart.CountHigh.Total > 0)
                {

                    row[6] = tag.chart.CountHigh.Fail * 100 / tag.chart.CountHigh.Total;

                }
                else
                {
                    row[6] = 0;


                }
                row[7] = true;
                row_added.Enqueue(row);

                //current_id++;
                DataRow row1 = mTable.NewRow();
                //row[0] = current_id;
                row1[1] = recordtime;
                row1[2] = MainWindow.current_program_name;
                row1[3] = tag.TagName;
                row1[4] = tag.chart.CountLow.Total;
                row1[5] = tag.chart.CountLow.Fail;
                if (tag.chart.CountLow.Total > 0)
                {

                    row1[6] = tag.chart.CountLow.Fail * 100 / tag.chart.CountLow.Total;

                }
                else
                {
                    row1[6] = 0;


                }
                row1[7] = false;
                row_added.Enqueue(row1);
            }


            //  mTable.Rows.Add(row);

            if (record_task == null | (record_task?.IsCompleted == true))
                record_task = Task.Run(new Action(() =>
                {
                    while (row_added.Count > 0)
                    {
                        DataRow row_write = row_added.Dequeue();
                        using (SqliteCommand mCmd = new SqliteCommand("INSERT INTO Records (Time,Job,Tag,Total,Fail,Percent,HighSpeed) VALUES ('" + row_write[1] + "','" + row_write[2] + "','" + row_write[3] + "'," + row_write[4] + "," + row_write[5] + "," + row_write[6] + ",'" + row_write[7] + "');", mConn))
                        {

                            mCmd.ExecuteNonQuery();


                        }
                    }

                }
                ));


        }


        public void ExportCSVFile(StreamWriter writer)
        {
            return;
            //CsvHelper.CsvWriter csv = new CsvHelper.CsvWriter(writer);
            //foreach (DataColumn column in mTableView.Columns)
            //{
            //    csv.WriteField(column.ColumnName);
            //}
            //csv.NextRecord();

            //foreach (DataRow row in mTableView.Rows)
            //{
            //    for (var i = 0; i < mTableView.Columns.Count; i++)
            //    {
            //        csv.WriteField(row[i]);
            //    }
            //    csv.NextRecord();
            //}

        }
        public string ToCsv()
        {
            StringBuilder sbData = new StringBuilder();

            // Only return Null if there is no structure.
            if (mTable.Columns.Count == 0)
                return null;

            foreach (var col in mTable.Columns)
            {
                if (col == null)
                    sbData.Append(",");
                else
                    sbData.Append("\"" + col.ToString().Replace("\"", "\"\"") + "\",");
            }

            sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);

            foreach (DataRow dr in mTable.Rows)
            {
                foreach (var column in dr.ItemArray)
                {
                    if (column == null)
                        sbData.Append(",");
                    else
                        sbData.Append("\"" + column.ToString().Replace("\"", "\"\"") + "\",");
                }
                sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);
            }

            return sbData.ToString();
        }

        public object _syncLockOrders = new object();

        //long current_id = 0;
        public Task record_task;
        bool _is_enable = false;
        public bool IsEnable
        {
            get
            {
                return _is_enable;
            }
            set
            {
                if (_is_enable != value)
                {
                    _is_enable = value;
                    RaisePropertyChanged("IsEnable");
                }
            }
        }
        public int max_record = 5000;
        public string DateFormat = "yyyy-MM-dd HH:mm:ss";
        public void WriteRecords(HTuple type, CalibarionData parameters, string image_path, bool is_rejected, string data, int PPM, HTuple tools)
        {
            return;
            if (!_is_enable)
                return;
            //if (current_id> max_record)
            //{
            //    return;
            //}
            //current_id++;
            DataRow row = mTable.NewRow();
            row[2] = MainWindow.current_program_name;
            //row[0] = current_id;
            row[1] = DateTime.Now.ToString(DateFormat);
            string type_append = string.Empty;
            for (int i = 0; i < type.Length; i++)
            {

                type_append = type_append + type[i];
                if (i < type.Length - 1)
                    type_append = type_append + "; ";
            }
            string tool_append = string.Empty;
            for (int i = 0; i < tools.Length; i++)
            {

                tool_append = tool_append + tools[i];
                if (i < tools.Length - 1)
                    tool_append = tool_append + ";";
            }
            row[3] = tool_append;
            row[4] = type_append;
            row[5] = JsonConvert.SerializeObject(parameters);
            row[6] = PPM;
            row[7] = image_path;
            row[8] = is_rejected;
            row[9] = data;
            mTable.Rows.Add(row);
            row_added.Enqueue(row);
            if (record_task == null | (record_task?.IsCompleted == true))
                record_task = Task.Run(new Action(() =>
                {
                    while (row_added.Count > 0)
                    {
                        DataRow row_write = row_added.Dequeue();
                        using (SqliteCommand mCmd = new SqliteCommand("INSERT INTO Records (Time,Job,Tools,Info,Parameters,Speed,Image,'Is Rejected',Data) VALUES ('" + row_write[1] + "','" + row_write[2] + "','" + row_write[3] + "','" + row_write[4] + "','" + row_write[5] + "'," + row_write[6] + ",'" + row_write[7] + "','" + row_write[8] + "','" + row_write[9] + "');", mConn))
                        {

                            mCmd.ExecuteNonQuery();


                        }
                    }

                }
                ));


        }
        Queue<DataRow> row_added = new Queue<DataRow>(20);
        string load_command = "CREATE TABLE IF NOT EXISTS [Records] (P_Id INTEGER PRIMARY KEY,'Time' TEXT,'Job' TEXT,'Tag' TEXT ,'Total' INT,'Fail' INT,'Percent' REAL,'HighSpeed' BOOL);";
        public DataTable LoadDataTableTemp(string path)
        {
            try
            {

                SqliteConnection mConn = new SqliteConnection("Data Source=" + path);
                mConn.Open();
                // ---------- Creating A Test Table, If Not Exists ----------
                // ----------------------------------------------------------
                // id        - Unique Counter - Key Field (Required in any table)
                // FirstName - Text
                // Age       - Integer
                // Avatar    - Blob (Binary Data Stream) For Image
                using (SqliteCommand mCmd = new SqliteCommand(load_command, mConn))
                {
                    mCmd.ExecuteNonQuery();
                }

                //SQLiteDataAdapter mAdapter = new SQLiteDataAdapter("SELECT * FROM Records", mConn);
                DataTable mTable = new DataTable();

                //mAdapter.Fill(mTable);

                return mTable;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public DataTable LoadDataTableTemp(string path, string filter)
        {
            try
            {

                SqliteConnection mConn = new SqliteConnection("Data Source=" + path);
                mConn.Open();
                // ---------- Creating A Test Table, If Not Exists ----------
                // ----------------------------------------------------------
                // id        - Unique Counter - Key Field (Required in any table)
                // FirstName - Text
                // Age       - Integer
                // Avatar    - Blob (Binary Data Stream) For Image
                using (SqliteCommand mCmd = new SqliteCommand(load_command, mConn))
                {
                    mCmd.ExecuteNonQuery();
                }

                //SQLiteDataAdapter mAdapter = new SQLiteDataAdapter("SELECT * FROM Records " + filter, mConn);
                DataTable mTable = new DataTable();

                //mAdapter.Fill(mTable);

                return mTable;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public DataTable LoadTableDate(string path)
        {
            try
            {
                if (_current_path == path)
                {
                    mTableView = _m_table;
                    return _m_table;
                }
                SqliteConnection mConn = new SqliteConnection("Data Source=" + path);
                mConn.Open();
                // ---------- Creating A Test Table, If Not Exists ----------
                // ----------------------------------------------------------
                // id        - Unique Counter - Key Field (Required in any table)
                // FirstName - Text
                // Age       - Integer
                // Avatar    - Blob (Binary Data Stream) For Image
                using (SqliteCommand mCmd = new SqliteCommand(load_command, mConn))
                {
                    mCmd.ExecuteNonQuery();
                }

                //SQLiteDataAdapter mAdapter = new SQLiteDataAdapter("SELECT * FROM Records", mConn);
                DataTable mTable = new DataTable();

                //mAdapter.Fill(mTable);
                mTableView = mTable;
                return mTable;
            }
            catch (Exception ex)
            {
                return null;
            }


        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        string _file_path = System.IO.Directory.GetCurrentDirectory() + "\\Report\\Records.db";
        public string FilePath
        {
            get
            {
                return _file_path;
            }
            set
            {
                if (_file_path != value)
                {
                    _file_path = value;
                    RaisePropertyChanged("FilePath");
                }
            }
        }
        //string _daily_record_directory;
        public DatabaseRecord(string record_file)
        {
            //if (!Directory.Exists(record_directory))
            //{
            //    Directory.CreateDirectory(record_directory);
            //}
            //this._daily_record_directory = record_directory;

            CreateDatabase(record_file);
            InitiateAsync();
            //tagfactory.AddTag("Object");
            //tagfactory.AddTag("Folded");
            //CurrentTag.Add(new Tag("Object"));
            //CurrentTag.Add(new Tag("Folded"));
            // InitiateAsyncCustomShift();
        }

        private void CreateDatabase(string record_file)
        {
            string current_csv_path = record_file;
            FilePath = current_csv_path;

            SqlInit(FilePath);
            //BindingOperations.EnableCollectionSynchronization(_m_table.DefaultView, _syncLockOrders);
            //mTableView = _m_table;


        }

        //Sql mAdapter;

        DataTable _m_table;
        public DataTable mTable
        {
            get
            {

                //try
                //{
                //    _readerWriterLock.EnterReadLock();
                //Function to acess your database and return the selected results

                return _m_table;



                //}
                //finally
                //{
                //    _readerWriterLock.ExitReadLock();
                //}





            }
            set
            {
                if (_m_table != value)
                {
                    _m_table = value;
                    RaisePropertyChanged("mTable");
                }
            }
        }
        DataTable _m_table_view;
        public DataTable mTableView
        {
            get
            {
                return _m_table_view;
            }
            set
            {
                if (_m_table_view != value)
                {
                    _m_table_view = value;
                    RaisePropertyChanged("mTableView");
                }
            }
        }
        string _current_path;
        public string CurrentPath
        {
            get
            {
                return _current_path;
            }
            set
            {
                if (_current_path != value)
                {
                    _current_path = value;
                    RaisePropertyChanged("CurrentPath");
                }
            }
        }
        SqliteConnection mConn;
        private void SqlInit(string _path)
        {
            // MessageBox.Show("start sql");
            //MessageBox.Show(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            
            mConn = new SqliteConnection("Data Source=" + _path);
            // ----------------- Opening The Connection -----------------
            // ----------------------------------------------------------
            mConn.Open();
            // ---------- Creating A Test Table, If Not Exists ----------
            // ----------------------------------------------------------
            // id        - Unique Counter - Key Field (Required in any table)
            // FirstName - Text
            // Age       - Integer
            // Avatar    - Blob (Binary Data Stream) For Image
            using (SqliteCommand mCmd = new SqliteCommand(load_command, mConn))
            {
                mCmd.ExecuteNonQuery();
            }

            //mAdapter = new SQLiteDataAdapter("SELECT * FROM Records", mConn);
            //mTable = new DataTable();
            //mAdapter.FillSchema(mTable, SchemaType.Source);
            //mAdapter.Fill(mTable);


            //new SQLiteCommandBuilder(mAdapter);


            //mTable.Columns[0].AutoIncrement = true;
            //if (mTable.Rows.Count > 0)
            //{
            //    current_id = (long)mTable.Rows[mTable.Rows.Count - 1][0];
            //}
            //else
            //{
            //    current_id = 0;
            //}
            mTableView = mTable;
            ////Set the Increment value.
            //mTable.Columns[0].AutoIncrementStep = 1;
            CurrentPath = _path;
        }
        public void Dispose()
        {
            tokenSource2?.Cancel();
            tokenSource2_shift?.Cancel();
        }
        CancellationTokenSource tokenSource2;
        CancellationTokenSource tokenSource2_shift;

        //public void ChangeDayWorker()
        //{
        //    //Change record path by day

        //    CreateDatabase(DateTime.Now, _daily_record_directory);
        //}
        DateTime current_date = DateTime.Today;
        public delegate void NoInputEvent();
        public delegate void ShiftEvent(Shift next_shift, bool is_previous_day, bool shift_event);
        public delegate void PrintEvent(DateTime date, string shiftname);
        public NoInputEvent ExtendDailyTrigger;
        public ShiftEvent ExtendDailyTriggerShift;
        async void InitiateAsync()
        {

            tokenSource2 = new CancellationTokenSource();
            while (true)
            {

                var triggerTime = DateTime.Today + new TimeSpan(24, 0, 1) - DateTime.Now;
                if (triggerTime < TimeSpan.Zero)
                    triggerTime = triggerTime.Add(new TimeSpan(24, 0, 0));
                try
                {

                    await Task.Delay((TimeSpan)triggerTime, tokenSource2.Token);
                    current_date = DateTime.Today;
                    //ChangeDayWorker();
                    ExtendDailyTrigger?.Invoke();
                }
                catch (Exception ex)
                {
                    return;
                }


            }
        }
        public ObservableCollection<Shift> lst_shift_time_config = new ObservableCollection<Shift>();
        public bool first_time = true;
        public Shift previous_shift = null;
        public bool is_previous_day = false;
        public async void InitiateAsyncCustomShift()
        {

            tokenSource2_shift = new CancellationTokenSource();

            while (true)
            {
                if (lst_shift_time_config.Count == 0)
                    return;
                var time_now = DateTime.Now - DateTime.Today;

                List<Shift> lst_shift_time = lst_shift_time_config.ToList();
                lst_shift_time.Sort((x, y) => x.ShiftTime.CompareTo(y.ShiftTime));
                int index = -1;
                Shift current_shift = null;
                Shift previous_shift = null;
                is_previous_day = false;
                for (int i = 0; i < lst_shift_time.Count; i++)
                {
                    if (time_now < lst_shift_time[i].ShiftTime)
                    {
                        current_shift = lst_shift_time[i];
                        index = i;

                        break;
                    }


                }
                TimeSpan triggertime = TimeSpan.FromHours(24);
                if (index == -1)
                {
                    triggertime = TimeSpan.FromHours(24) + lst_shift_time[0].ShiftTime - time_now;
                    current_shift = lst_shift_time[0];
                    previous_shift = lst_shift_time[lst_shift_time.Count - 1];

                }
                else
                {
                    triggertime = lst_shift_time[index].ShiftTime - time_now;

                    if (index < 1)
                    {
                        previous_shift = lst_shift_time[lst_shift_time.Count - 1];
                    }
                    else
                    {
                        previous_shift = lst_shift_time[index - 1];
                    }


                }


                if (triggertime < TimeSpan.Zero)
                    triggertime = triggertime.Add(new TimeSpan(24, 0, 0));

                if (first_time)
                {
                    if (index == 0)
                        is_previous_day = true;

                    first_time = false;
                    if (is_previous_day)
                        previous_shift.DateStart = DateTime.Now.Date.AddDays(-1);
                    else
                        previous_shift.DateStart = DateTime.Now.Date;
                    ExtendDailyTriggerShift?.Invoke(previous_shift, is_previous_day, false);
                }
                this.previous_shift = previous_shift;



                try
                {

                    await Task.Delay(triggertime, tokenSource2_shift.Token);
                    current_shift.DateStart = DateTime.Now.Date;
                    ExtendDailyTriggerShift?.Invoke(current_shift, false, true);

                    //current_date = DateTime.Today;
                    // ChangeDayWorker();
                    // ExtendDailyTrigger?.Invoke();
                }
                catch (Exception ex)
                {
                    return;
                }


            }
        }
    }
    public class Shift : HelperMethods
    {
        public string Name { get; set; } = "Unnamed shift";
        public DateTime DateStart { get; set; } = DateTime.Now;
        public TimeSpan ShiftTime { get; set; } = TimeSpan.FromHours(12);
        public bool IsReset { get; set; } = true;
        public void Save(HFile file)
        {
            SaveParam(file, this);
        }
        public void LoadSetting(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        public static Shift Load(DeserializeFactory item)
        {
            Shift loaded = new Shift();
            loaded.LoadSetting(item);
            return loaded;
        }
    }
}
