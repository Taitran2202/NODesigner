using DynamicData;
using HalconDotNet;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Camera", "Acquisition",Icon: "Designer/icons/icons8-slr-camera-60.png",sortIndex:0)]
    public class AccquisitionNode : BaseNode, IImageSourceNode
    {
        static AccquisitionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewAccquisition(), typeof(IViewFor<AccquisitionNode>));
        }
        public override List<string> GetExtendTag()
        {
            return new List<string>() { "Image" };
        }
        public override void Dispose()
        {
            Acq.Dispose();
            base.Dispose();
        }
        public void SetImage(HImage image)
        {
            Acq.SetImage(image);
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        
        public ValueNodeInputViewModel<float> fl { get; }
        public ValueNodeInputViewModel<bool> SoftwareTriggerInput { get; }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public ValueNodeInputViewModel<bool> OnlineInput { get; }
        void ShowCameraSetting(Control sender)
        {
            ViewModelViewHostWindow wd = new ViewModelViewHostWindow(Acq,this.Name);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();
        }
        void ShowGraphicEditor(Control sender)
        {
            if (ImageOutput.CurrentValue != null)
            {
                if (ImageOutput.CurrentValue.IsInitialized())
                {
                    var imageData = ImageOutput.CurrentValue.Clone();
                    WindowGraphicsEditor wd = new WindowGraphicsEditor(Acq.GraphicsList, imageData);
                    wd.ShowDialog();
                }
            }
        }
        public override void Run(object context)
        {

        }
        [HMIProperty("Graphics Editor")]
        public IReactiveCommand ShowGraphicsEditor
        {
            get { return ReactiveCommand.Create((Control sender) => ShowGraphicEditor(sender)); }
        }
        [HMIProperty("Camera Setting")]
        public IReactiveCommand ShowCameraSettingCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ShowCameraSetting(sender));}
        }
        [HMIProperty("Filmstrip")]
        public IReactiveCommand ShowFilmstripCommand
        {
            get { return ReactiveCommand.Create((Control sender) => Acq.ShowFilmstrip(sender)); }
        }
        [HMIProperty("Reload Image")]
        public IReactiveCommand ReloadImageCommand
        {
            get { return ReactiveCommand.Create((Control sender) => Acq.DisplaygImage()); }
        }
        [HMIProperty("Open Image")]
        public IReactiveCommand OpenImageCommand
        {
            get { return ReactiveCommand.Create((Control sender) => Acq.OpenImage()); }
        }
        [HMIProperty("Record Setting")]
        public IReactiveCommand RecordSettingCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ShowSetting()); }
        }
        [HMIProperty("Disable image processing")]
        public bool DisableImageProcessing
        {
            get { return Acq.DisableImageProcessing; }
            set
            {
                Acq.DisableImageProcessing = value;
            }
        }
        [HMIProperty("Show Live View")]
        public IReactiveCommand ShowLiveViewCommand
        {
            get { return ReactiveCommand.Create((Control sender) => Acq.Interface.LiveView()); }
        }
        public void ShowSetting()
        {
            RecordSettingWindow wd = new RecordSettingWindow(Acq.Record);
            wd.ShowDialog();
        }
        [SerializeIgnore]
        public ViewModelViewHost PropertiesView
        {
            get
            {

                return new ViewModelViewHost() { ViewModel = Acq,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch
                };
            }
        }
        public Designer.Accquisition.Accquisition Acq { get; set; }

        public void ResetCounter()
        {
            Total = 0;
            Pass = 0;
            Fail = 0;
            foreach(var item in designer.TagManager.TagList)
            {
                item.Value = 0;
            }
        }
        [HMIProperty("Reset Counter")]
        public IReactiveCommand ResetCounterCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ResetCounter()); }
        }
        [HMIProperty("Reset FrameID")]
        public IReactiveCommand ResetFrameID
        {
            get { return ReactiveCommand.Create((Control sender) => ImageCount=0); }
        }
        int _total;
        [HMIProperty]
        [SerializeIgnore]
        public int Total
        {
            get
            {
                return _total;
            }
            internal set
            {
                this.RaiseAndSetIfChanged(ref _total,value);
            }
        }
        int _pass;
        [HMIProperty]
        [SerializeIgnore]
        public int Pass
        {
            get
            {
                return _pass;
            }
            internal set
            {
                this.RaiseAndSetIfChanged(ref _pass, value);
            }
        }
        int _fail;
        [HMIProperty]
        [SerializeIgnore]
        public int Fail
        {
            get
            {
                return _fail;
            }
            internal set
            {
                this.RaiseAndSetIfChanged(ref _fail, value);
            }
        }
        int _image_count;
        [HMIProperty]
        [SerializeIgnore]
        public int ImageCount
        {
            get
            {
                return _image_count;
            }
            internal set
            {
                this.RaiseAndSetIfChanged(ref _image_count, value);
            }
        }
        [HMIProperty]
        public SoftwareTriggerDetectionEnum SoftwareTriggerDetectionMode { get; set; } = SoftwareTriggerDetectionEnum.LeadingEdge;
        public override void OnLoadComplete()
        {
            try
            {
                Acq?.Connect();
            }
            catch (Exception ex) {
                App.GlobalLogger.LogError("Camera", "Cannot connect camera on load complete");
            }
            
        }
        public void ResolveSavedImagePath(InspectionContext context)
        {
            string path = string.Empty, pathScreenshot = string.Empty;
            Acq.Record.UpdateDateFolder();           
            if (context.result)
            {
                if (Acq.Record.IsRecord & Acq.Record.IsRecordPass)
                {
                    var directory = Acq.Record.GetPass();
                    if (directory != string.Empty)
                    {
                        string name = "";
                        Acq.Record.PassCount++;
                        if (context.SavedName != string.Empty)
                        {
                            name = context.SavedName;
                        }
                        else
                        {
                            name = DateTime.Now.ToString(Acq.Record.ImageNameFormat);
                        }

                        path = System.IO.Path.Combine(directory, name);
                       

                        if (Acq.Record.IsRecordOverlay)
                        {
                            string directory2 = Acq.Record.GetScreenshotPass();
                            pathScreenshot = System.IO.Path.Combine(directory2, name);

                        }

                    }
                }

            }
            else
            {
                if (Acq.Record.IsRecord & Acq.Record.IsRecordFail)
                {
                    var directory = Acq.Record.GetFail();
                    if (directory != string.Empty)
                    {
                        string name = "";
                        Acq.Record.FailCount++;
                        if (context.SavedName != string.Empty)
                        {
                            name = context.SavedName;
                        }
                        else
                        {
                            name = DateTime.Now.ToString(Acq.Record.ImageNameFormat);
                        }
                        path = System.IO.Path.Combine(directory, name);
                        if (Acq.Record.IsRecordOverlay)
                        {
                            string directory2 = Acq.Record.GetScreenShotFail();
                            pathScreenshot = System.IO.Path.Combine(directory2, name);
                        }
                    }
                }
            }
            if (path != String.Empty)
            {
                context.SaveImagePath = path + "." + Acq.Record.ImageFormat;
            }
            else
            {
                context.SaveImagePath = String.Empty;
            }
            if (pathScreenshot != String.Empty)
            {
                context.SaveScreenShotPath = pathScreenshot + "." + Acq.Record.ImageFormat;
            }
            else
            {
                context.SaveScreenShotPath = String.Empty;
            }
            

        }
        public AccquisitionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Acquisition";
            Acq = new Accquisition.Accquisition(Dir);
            OnlineInput = new ValueNodeInputViewModel<bool>(ValueNodeInputViewModel<bool>.ValidationAction.PushDefaultValue,
            ValueNodeInputViewModel<bool>.ValidationAction.DontValidate)
            {
                Name = "Online/Offline",
                PortType = "Boolean",
                Independent = true,
            };
            OnlineInput.WhenAnyValue(x => x.Value).Subscribe(x =>
            {
                if (x)
                {
                    Acq.Start();
                }
                else
                {
                    Acq.Stop();
                }
            });
            this.Inputs.Add(OnlineInput);
            SoftwareTriggerInput = new ValueNodeInputViewModel<bool>(ValueNodeInputViewModel<bool>.ValidationAction.PushDefaultValue, ValueNodeInputViewModel<bool>.ValidationAction.DontValidate)
            {
                Name = "Software Trigger",
                PortType = "Boolean",
                Independent = true,
            };
            SoftwareTriggerInput.WhenAnyValue(x => x.Value).Subscribe(x =>
            {
                if (x)
                {
                    Console.WriteLine("TRUE");
                    Acq.Trigger();
                }               
            });
            this.Inputs.Add(SoftwareTriggerInput);
            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Outputs.Add(ImageOutput);
            //var pubSocket = new PublisherSocket();
            //pubSocket.Bind("tcp://127.0.0.1:3000");
            Acq.OutputImage.Subscribe(loaded =>
            {
                //Thread.CurrentThread.Priority = ThreadPriority.Highest;
                var start = DateTime.Now;
                var context = new InspectionContext(Acq,null, Acq.Calib.ScaleX, Acq.Calib.ScaleY, this.ID, loaded.FrameId,Total,Pass,Fail);
                context.FPS = loaded.FPS;
                ImageCount = (int)loaded.FrameId;
                ImageOutput.OnNext(loaded.Image);
                
                if (DisableImageProcessing)
                {
                    if (ShowDisplay)
                    {
                        designer.display?.HalconWindow.DispObj(loaded.Image);
                        designer.displayMainWindow?.HalconWindow.DispObj(loaded.Image);
                    }


                    context.inspection_result.Display(designer.display);
                    
                    context.inspection_result.Display(designer.displayMainWindow);
                    Reset();
                    designer.ProcessingTime = (DateTime.Now - start).TotalMilliseconds;
                    return;
                }

                BaseRun(context);
                if (!loaded.IsPlayback)
                {
                    ResolveSavedImagePath(context);
                }
                designer.OnComplete?.Invoke(context, null);
                if (ShowDisplay)
                {
                    designer.display?.HalconWindow.DispObj(loaded.Image);
                    designer.displayMainWindow?.HalconWindow.DispObj(loaded.Image);
                }

                Acq.InsertGraphics(context);
                context.inspection_result.Display(designer.display);
                context.inspection_result.Display(designer.displayMainWindow);

                Reset();
                designer.ProcessingTime = (DateTime.Now - start).TotalMilliseconds;
                designer.Result = context.result;
                if (loaded.IsPlayback)
                {
                    return;
                }
                
                Total++;
                
                //pubSocket.Options.SendHighWatermark = 1000;
                //pubSocket.SendMoreFrame(this.Name+".image").SendFrame(data);
                //loaded.Image.
                HImage fail_disp = null, imageCopy=null;
                if (!context.result & designer.displayData.ShowLastFail)
                {
                    fail_disp = designer.displayMainWindow.HalconWindow.DumpWindowImage();
                    HTuple w, h;
                    fail_disp.GetImageSize(out w, out h);
                    designer.displayMainWindowFail.HalconWindow.SetPart((HTuple)0, (HTuple)0, h, w);
                    designer.displayMainWindowFail.HalconWindow.DispObj(fail_disp);
                }
                if (context.result)
                {
                    Pass++;
                }
                else
                {
                    Fail++;
                    if (!context.ByPassReject)
                    {
                        Acq.Interface?.Reject();
                    }
                    
                    imageCopy = loaded.Image.CopyImage();
                    context.inspection_result.image = imageCopy;
                    designer.recorder.Add(context.inspection_result);
                }
                //Acq.Record.UpdateDateFolder();
                if (context.SaveImagePath != string.Empty)
                {
                    if (imageCopy == null)
                    {
                        imageCopy = loaded.Image.CopyImage();
                    }
                    if (RecordBuffer.Count < 10)
                    {
                        RecordBuffer.Enqueue(new SaveImageData() { Image = imageCopy, FileName = context.SaveImagePath, Format = Acq.Record.ImageFormat });
                        TriggerSaveImage();
                    }
                    

                }
                if (context.SaveScreenShotPath != string.Empty)
                {
                    if (fail_disp == null)
                    {
                        fail_disp = designer.displayMainWindow.HalconWindow.DumpWindowImage();
                    }
                    if (RecordBuffer.Count < 10)
                    {
                        RecordBuffer.Enqueue(new SaveImageData() { Image = fail_disp.CopyImage(), FileName = context.SaveScreenShotPath, Format = Acq.Record.ImageFormat });
                        TriggerSaveImage();
                    }
                    
                    //Task.Run(new Action(() =>
                    //{
                    //    fail_disp.WriteImage(Acq.Record.ImageFormat, 0,context.SaveScreenShotPath);
                    //}));
                }
                //update web hmi service
                if (false)
                {
                    byte[] data = Processing.HalconUtils.HImageJpeg(designer.displayMainWindow.HalconWindow.DumpWindowImage(), 3, out _, out _);
                    MainViewModel.Instance.HMIService?.Publish(this.Name + ".Total", Total.ToString());
                    MainViewModel.Instance.HMIService?.Publish(this.Name + ".Pass", Total.ToString());
                    MainViewModel.Instance.HMIService?.Publish(this.Name + ".Fail", Total.ToString());
                    MainViewModel.Instance.HMIService?.Publish(this.Name + ".Image", Convert.ToBase64String(data));
                }
                

            });

            SoftwareTriggerInput.ValueChanged.Buffer(2, 1).Select(b => (Previous: b[0], Current: b[1])).Subscribe(x =>
            {

                switch (SoftwareTriggerDetectionMode)
                {
                    case SoftwareTriggerDetectionEnum.LeadingEdge:
                        if (x.Previous != x.Current)
                        {
                            if (x.Current == true)
                            {
                                if (Acq.Interface != null)
                                {
                                    Acq.Interface.SoftwareTrigger();
                                }
                            }

                        }
                        break;
                    case SoftwareTriggerDetectionEnum.TrailingEdge:
                        if (x.Previous != x.Current)
                        {
                            if (x.Current == false)
                            {
                                if (Acq.Interface != null)
                                {
                                    Acq.Interface.SoftwareTrigger();
                                }
                            }

                        }
                        break;
                    case SoftwareTriggerDetectionEnum.High:
                        if (x.Current)
                        {
                            if (Acq.Interface != null)
                            {
                                Acq.Interface.SoftwareTrigger();
                            }
                        }
                        break;
                    case SoftwareTriggerDetectionEnum.Low:
                        if (!x.Current)
                        {
                            if (Acq.Interface != null)
                            {
                                Acq.Interface.SoftwareTrigger();
                            }
                        }
                        break;
                }
                if (x.Previous!=x.Current)
                {
                    if (x.Current == true)
                    {
                        if (Acq.Interface != null)
                        {
                            Acq.Interface.SoftwareTrigger();
                        }
                    }
                    
                }
                
            });

        }
        public enum SoftwareTriggerDetectionEnum
        {
            LeadingEdge,TrailingEdge,High,Low
        }

        private void TriggerSaveImage()
        {
            if (recordTask == null)
            {
                recordTask = Task.Run(new Action(() =>
                {
                    //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    while (!RecordBuffer.IsEmpty)
                    {
                        if (RecordBuffer.Count < 10)
                        {
                            if (RecordBuffer.TryDequeue(out SaveImageData imageSave))
                            {
                                try
                                {
                                    if (imageSave.Image.IsInitialized())
                                    {
                                        imageSave.Image.WriteImage(imageSave.Format, 0, imageSave.FileName);
                                    }

                                    //imageSave.Image.Dispose();
                                }catch(Exception ex)
                                {

                                }
                                
                            }
                        }
                        else
                        {
                            if (RecordBuffer.TryDequeue(out SaveImageData imageSave))
                            {
                                try
                                {
                                    if (imageSave.Image.IsInitialized())
                                    {
                                        //imageSave.Image.Dispose();
                                    }
                                }catch(Exception ex)
                                {

                                }
                                //do nothing if record buffer is higher than 5
                               
                               
                            }
                        }

                    }
                    recordTask = null;
                    //imageCopy.WriteImage(Acq.Record.ImageFormat, 0, context.SaveImagePath);
                }));
            }
            else
            {
                if (recordTask.IsCompleted)
                {
                    recordTask = Task.Run(new Action(() =>
                    {
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        while (!RecordBuffer.IsEmpty)
                        {
                            if (RecordBuffer.Count < 10)
                            {
                                if (RecordBuffer.TryDequeue(out SaveImageData imageSave))
                                {
                                    try
                                    {
                                        if (imageSave.Image.IsInitialized())
                                        {
                                            imageSave.Image.WriteImage(imageSave.Format, 0, imageSave.FileName);
                                        }
                                        imageSave.Image.Dispose();
                                    }catch(Exception ex)
                                    {

                                    }
                                    
                                }
                            }
                            else
                            {
                                if (RecordBuffer.TryDequeue(out SaveImageData imageSave))
                                {
                                    try
                                    {
                                        //do nothing if record buffer is higher than 5
                                        //imageSave.Image.Dispose();
                                        if (imageSave.Image.IsInitialized())
                                        {
                                            imageSave.Image.Dispose();
                                        }
                                    }catch(Exception ex)
                                    {

                                    }
                                    
                                }
                            }

                        }
                        recordTask = null;
                        //imageCopy.WriteImage(Acq.Record.ImageFormat, 0, context.SaveImagePath);
                    }));
                }
            }
        }

        Task recordTask;
        private ConcurrentQueue<SaveImageData> RecordBuffer = new ConcurrentQueue<SaveImageData>();
    }
    public class SaveImageData
    {
        public bool IsPass { get; set; }
        public string Format { get; set; }
        public string FileName { get; set; }
        public HImage Image { get; set; }
    }
    public interface IImageSourceNode
    {
        void SetImage(HImage image);
    }
    public class RecordImage:ReactiveObject, IHalconDeserializable
    {
        public RecordImage(string default_dir,string defaultname)
        {
            MainDir = default_dir;
            NameSub = default_dir;
            Initialize();
            
        }
        public RecordImage()
        {
            MainDir = MainViewModel.Instance.Setting.DefaultRecordPath;
            NameSub = "default_camera";
            Initialize();

        }

        private void Initialize()
        {
            DateSub = DateTime.Today.ToString(DateFolderFormat);
            NameFail = "Fail";
            NamePass = "Pass";

            this.WhenAnyValue(x => x.MainDir, x => x.NameSub, x => x.DateSub).Subscribe((x) =>
            {
                App.RecordHelper.InsertOrUpdate(System.IO.Path.Combine(x.Item1, x.Item2, x.Item3),DateTime.Now);
            });

            this.WhenAnyValue(x => x.MainDir, x => x.NameSub, x => x.DateSub, x => x.NamePass, x => x.NameFail).Subscribe((x) =>
            {
                PassDir = System.IO.Path.Combine(x.Item1,x.Item2, x.Item3, x.Item4);
                FailDir = System.IO.Path.Combine(x.Item1, x.Item2, x.Item3, x.Item5);
            });
            this.WhenAnyValue(x => x.PassDir).Subscribe((x) =>
            {
                
                if (!System.IO.Directory.Exists(x))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(x);
                        pass_dir_created = true;
                    }
                    catch (Exception ex)
                    {
                        pass_dir_created = false;
                    }

                }
                else
                {
                    pass_dir_created = true;
                }
                if (pass_dir_created)
                {
                    PassCount=System.IO.Directory.GetFiles(x).Length;
                }
                ScreenShotPassDir = System.IO.Path.Combine(x, "screenshot");
                if (!System.IO.Directory.Exists(ScreenShotPassDir))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(ScreenShotPassDir);
                        pass_dir_created = true;
                    }
                    catch (Exception ex)
                    {
                        pass_dir_created = false;
                    }

                }
                else
                {
                    pass_dir_created = true;
                }
            });
            this.WhenAnyValue(x => x.FailDir).Subscribe((x) =>
            {
                
                if (!System.IO.Directory.Exists(x))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(x);
                        
                        fail_dir_created = true;
                    }
                    catch (Exception ex)
                    {
                        fail_dir_created = false;
                    }

                }
                else
                {
                    fail_dir_created = true;
                }
                if (fail_dir_created)
                {
                    FailCount = System.IO.Directory.GetFiles(x).Length;
                }
                ScreenShotFailDir = System.IO.Path.Combine(x, "screenshot");
                if (!System.IO.Directory.Exists(ScreenShotFailDir))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(ScreenShotFailDir);

                        fail_dir_created = true;
                    }
                    catch (Exception ex)
                    {
                        fail_dir_created = false;
                    }

                }
                else
                {
                    fail_dir_created = true;
                }
            });
            this.WhenAnyValue(x => x.IsRecordOverlay).Subscribe((x) =>
            {
                if (x)
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(PassDir);
                        fail_dir_created = true;
                    }
                    catch (Exception ex)
                    {
                        fail_dir_created = false;
                    }

                }
                else
                {
                    fail_dir_created = true;
                }
            });

        }

        bool pass_dir_created = false;
        bool fail_dir_created = false;
        public bool UpdateDateFolder()
        {
            if (IsRecord)
            {
                if (IsRecordPass | IsRecordFail)
                {
                    DateSub = DateTime.Today.ToString(DateFolderFormat);
                }
            }
            return true;
            
        }
        public string GetScreenshotPass()
        {

            if (!pass_dir_created)
            {
                return string.Empty;
            }
            return ScreenShotPassDir;
        }

        public string GetPass()
        {
            
            if (!pass_dir_created)
            {
                return string.Empty;
            }
            if (App.AppSetting.LimitFolderImage)
            {
                if(_pass_count> App.AppSetting.MaxFolderImage)
                {
                    return String.Empty;
                }
            }
            return PassDir;
        }
        public string GetFail()
        {
            
            if (!fail_dir_created)
            {
                return string.Empty;
            }
            if (App.AppSetting.LimitFolderImage)
            {
                if (_fail_count > App.AppSetting.MaxFolderImage)
                {
                    return String.Empty;
                }
            }
            return FailDir;


        }
        public string GetScreenShotFail()
        {

            if (!fail_dir_created)
            {
                return string.Empty;
            }
            return ScreenShotFailDir;


        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        bool _is_record_overlay;
        public bool IsRecordOverlay
        {
            get
            {
                return _is_record_overlay;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_record_overlay, value);
            }
        }

        bool _is_record;
        public bool IsRecord
        {
            get
            {
                return _is_record;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_record, value);
            }
        }
        bool _is_record_pass;
        public bool IsRecordPass
        {
            get
            {
                return _is_record_pass;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_record_pass, value);
            }
        }
        bool _is_record_fail;
        public bool IsRecordFail
        {
            get
            {
                return _is_record_fail;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _is_record_fail, value);
            }
        }
        string _pass_dir;
        public string PassDir
        {
            get
            {
                return _pass_dir;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pass_dir, value);
            }
        }
        int _pass_count;
        [SerializeIgnore]
        [JsonIgnore]
        public int PassCount
        {
            get
            {
                return _pass_count;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pass_count, value);
            }
        }
        int _fail_count;
        [SerializeIgnore]
        [JsonIgnore]
        public int FailCount
        {
            get
            {
                return _fail_count;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fail_count, value);
            }
        }
        string _screen_shot_pass_dir;
        public string ScreenShotPassDir
        {
            get
            {
                return _screen_shot_pass_dir;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _screen_shot_pass_dir, value);
            }
        }
        string _screen_shot_fail_dir;
        public string ScreenShotFailDir
        {
            get
            {
                return _screen_shot_fail_dir;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _screen_shot_fail_dir, value);
            }
        }
        string _fail_dir;
        public string FailDir
        {
            get
            {
                return _fail_dir;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _fail_dir, value);
            }
        }
        string _name_sub;
        public string NameSub
        {
            get
            {
                return _name_sub;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _name_sub, value);
            }
        }
        string _image_format="jpg";
        public string ImageFormat
        {
            get
            {
                return _image_format;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _image_format, value);
            }
        }
        string _image_name_format = "yyyy_MM_dd-HH_mm_ss_fff";
        public string ImageNameFormat
        {
            get
            {
                return _image_name_format;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _image_name_format, value);
            }
        }
        string _date_folder_format = "yyyy_MM_dd";
        public string DateFolderFormat
        {
            get
            {
                return _date_folder_format;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _date_folder_format, value);
            }
        }
        string _date_sub;
        public string DateSub
        {
            get
            {
                return _date_sub;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _date_sub, value);
            }
        }
        string _main_dir;
        public string MainDir
        {
            get
            {
                return _main_dir;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _main_dir, value);
            }
        }
        string _name_pass;
        public string NamePass
        {
            get
            {
                return _name_pass;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _name_pass, value);
            }
        }
        string _name_fail;
        public string NameFail
        {
            get
            {
                return _name_fail;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _name_fail, value);
            }
        }

    }
}
