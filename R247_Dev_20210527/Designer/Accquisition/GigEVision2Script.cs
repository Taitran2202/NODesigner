using HalconDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NOVisionDesigner.Designer.Accquisition
{
    public class GigEVision2Script : HelperMethods,CameraInterface,INotifyPropertyChanged
    {
        public void SoftwareTrigger()
        {

        }
        public bool IsRun { get { return is_run; } }
        static GigEVision2Script()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.GigEVision2ScriptView(), typeof(IViewFor<GigEVision2Script>));
        }
        public ScriptHost ConnectScript { get; set; } = new ScriptHost();
        public ScriptHost StartScript { get; set; } = new ScriptHost();
        public ScriptHost StopScript { get; set; } = new ScriptHost();
        public ScriptHost RejectScript { get; set; } = new ScriptHost();
        #region commands
        private ICommand _ConnectionScriptCommand;
        public ICommand ConnectionScriptCommand
        {
            get
            {
                return _ConnectionScriptCommand ?? (_ConnectionScriptCommand = new CommandHandler((result) =>
                {
                    Windows.AcqScriptWindow wd = new Windows.AcqScriptWindow(this, ConnectScript);
                    wd.Show();
                }, () => true));
            }
        }
        private ICommand _StartScriptCommand;
        public ICommand StartScriptCommand
        {
            get
            {
                return _StartScriptCommand ?? (_StartScriptCommand = new CommandHandler((result) =>
                {
                    Windows.AcqScriptWindow wd = new Windows.AcqScriptWindow(this, StartScript);
                    wd.Show();
                }, () => true));
            }
        }
        private ICommand _StopScriptCommand;
        public ICommand StopScriptCommand
        {
            get
            {
                return _StopScriptCommand ?? (_StopScriptCommand = new CommandHandler((result) =>
                {
                    Windows.AcqScriptWindow wd = new Windows.AcqScriptWindow(this, StopScript);
                    wd.Show();
                }, () => true));
            }
        }
        private ICommand _RejectScriptCommand;
        public ICommand RejectScriptCommand
        {
            get
            {
                return _RejectScriptCommand ?? (_RejectScriptCommand = new CommandHandler((result) =>
                {
                    Windows.AcqScriptWindow wd = new Windows.AcqScriptWindow(this, RejectScript);
                    wd.Show();
                }, () => true));
            }
        }
        #endregion

        public HTuple GetFeatures(HTuple paramName)
        {
            if (CodeContext.framegrabber == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    return CodeContext.framegrabber.GetFramegrabberParam(paramName);

                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }
        public HTuple GetFeature(string paramName)
        {
            if (CodeContext.framegrabber == null)
            {
                return 0;
            }
            else
            {
                try
                {
                    return CodeContext.framegrabber.GetFramegrabberParam(paramName);

                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
        }
        public bool SetFeature(string paramName, HTuple value)
        {
            if (CodeContext.framegrabber == null)
            {
                return false;
            }
            else
            {
                try
                {
                    CodeContext.framegrabber.SetFramegrabberParam((HTuple)paramName, value);
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
        }
        public OnDisconnected OnCameraDisconnected { get; set; }
        public bool IsHighSpeed { get; set; } = true;
        public bool IsRecordData()
        {
            return true;
        }

        int _time_out=-1;
        public int TimeOut
        {
            get
            {
                return _time_out;
            }
            set
            {
                if (_time_out != value)
                {
                    _time_out = value;
                    RaisePropertyChanged("TimeOut");
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
        AcquisitionCodeContext CodeContext { get; set; }
        public GigEVision2Script()
        {
            Type = "GigEVision2Script";
            CodeContext = new AcquisitionCodeContext();
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
            try
            {
                if (CodeContext.framegrabber != null)
                {
                    HOperatorSet.CloseFramegrabber(CodeContext.framegrabber);
                }

                // framegrabber.CloseFramegrabber();
            }
            catch (Exception ex)
            {

            }
            if (Device == null |ConnectScript==null)
            {
                return false;
            }

            try
            {
                ConnectScript.Script?.Invoke(CodeContext);
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
                    App.GlobalLogger.LogError("GigeVision2", "Cannot connect camera");

                    OnCameraDisconnected?.Invoke();
                    return false;
                }
                App.GlobalLogger.LogError("GigeVision2", "Cannot connect camera");
                MessageBox.Show(ex.Message);
            }

            return true;
        }

        
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);

        }
        public void OnLoadComplete()
        {
            try
            {
                ConnectScript.BuildScript();
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("GigEVision2Script", "Fail to build connection script!");
            }
            try
            {
                StartScript.BuildScript();
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("GigEVision2Script", "Fail to build start script!");
            }
            try
            {
                StopScript.BuildScript();
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("GigEVision2Script", "Fail to build stop script!");
            }
            try
            {
                RejectScript.BuildScript();
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("GigEVision2Script", "Fail to build reject script!");
            }
        }
        //HFramegrabber framegrabber;
        public void Save(HFile file)
        {
            //throw new NotImplementedException();
            SaveParam(file, this);
        }
        bool is_run = false;
        public void OnIOCallback(object channel, EventArgs e)
        {
            
        }
        public ulong FrameID { get; set; }
        public void Start()
        {
            Task.Run(new Action(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                if (CodeContext.framegrabber == null)
                {
                    this.Connect();
                }
                try
                {
                    if (CodeContext.framegrabber == null)
                        return;
                    try
                    {
                        StartScript.Script.Invoke(CodeContext);
                    }catch(Exception ex)
                    {

                    }

                    CodeContext.framegrabber.GrabImageStart(-1);
                    CodeContext.framegrabber.SetFramegrabberParam("grab_timeout", TimeOut);
                    is_run = true;
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    while (is_run)
                    {
                        HImage image = null;
                        try
                        {
                            image = CodeContext.framegrabber.GrabImageAsync(-1);
                        }
                        catch (Exception ex)
                        {

                        }

                        if (image != null)
                        {
                            FrameID++;
                            ImageAcquired?.Invoke(image,FrameID);
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
            is_run = false;
            CodeContext.framegrabber?.SetFramegrabberParam("grab_timeout", 1000);
            CodeContext.framegrabber?.SetFramegrabberParam("do_abort_grab", "true");
            try
            {
                StopScript?.Script.Invoke(CodeContext);
            }catch(Exception ex)
            {

            }
        }

        public void Dispose()
        {
            if (CodeContext.framegrabber != null)
            {
                try
                {
                    Stop();
                }
                catch (Exception ex)
                {

                }
                try
                {
                    CodeContext.framegrabber.Dispose();
                }
                catch (Exception ex)
                {

                }

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
            //script to do something when reject
            
        }

        public string TransformRecordPath(string record_path)
        {
            return String.Empty;
        }

        public void LiveView()
        {
            //throw new NotImplementedException();
        }
    }
    public class AcquisitionCodeContext
    {
        public HFramegrabber framegrabber;
    }
    public class ScriptHost:IHalconDeserializable
    {
        public ScriptRunner<object> Script { get; set; }
        public string ScriptText { get; set; }
        public void BuildScript()
        {
            var context = new AcquisitionCodeContext();
            var result = CSharpScript.Create(ScriptText, options: ScriptOptions.Default
                .WithImports("HalconDotNet", "NodeNetwork.Toolkit.ValueNode", "System")
                .AddReferences(Assembly.GetAssembly(typeof(HalconDotNet.HalconAPI)), Assembly.GetAssembly(typeof(System.AttributeTargets)))
                .AddReferences(Assembly.GetAssembly(typeof(NodeNetwork.Toolkit.NodeTemplate)),
                Assembly.GetAssembly(typeof(System.Net.Http.HttpClient)),
                Assembly.GetAssembly(typeof(Newtonsoft.Json.JsonConvert)),
                Assembly.GetAssembly(typeof(NOVisionDesigner.Designer.Nodes.USBOutput)),
                Assembly.GetAssembly(typeof(NumSharp.NDArray))
                ), typeof(CodeContext));
            var diagnostics = result.Compile();
            if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
            {

                Script= null;
            }
            Script= result.CreateDelegate();
        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
