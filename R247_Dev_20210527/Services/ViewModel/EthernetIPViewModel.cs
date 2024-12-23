using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MfgControl.AdvancedHMI.Drivers;
using AdvancedHMIDrivers;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer;
using HalconDotNet;
using System.Threading;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Services.View;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace NOVisionDesigner.Services.ViewModel
{
    [ServiceInfo("Job control","RSLogix")]
    public class RSLogixServiceViewModel: ApplicationService
    {
        [JsonProperty]
        public string Name { get; set; } = "RSLOGIX";
        static RSLogixServiceViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new RSLogixServiceView(), typeof(IViewFor<RSLogixServiceViewModel>));
        }
        public override void Dispose()
        {
            Stop();
        }
        public RSLogixServiceViewModel()
        {

        }
        NOVisionDesigner.ViewModel.GlobalTagManager VisionTagManager = NOVisionDesigner.ViewModel.GlobalTagManager.Instance;
        public RSLogixServiceViewModel(string ServiceDir,string id):base(ServiceDir,id)
        {
            MyMicroLogix = new EthernetIPforCLXComm();
            this.WhenAnyValue(x => x.IPAddress).Subscribe(x =>
            {
                if(x != null)
                MyMicroLogix.IPAddress = x;
            });
            TagJob = "";
            WriteTagList = new ObservableCollection<TagLink>();
            ReadTagList = new ObservableCollection<TagLink>();
            Load();
        }
        string lastError = "";
        void CheckJob()
        {
            try
            {
                if (MyMicroLogix.IPAddress == null )
                {
                    return;
                }
                if (EnableLoadJob)
                {
                    if (TagJob != "")
                    {
                        var currentJob = MyMicroLogix.ReadAny(TagJob);
                        ConnectionErrorCount = 0;
                        if (currentJob != MainViewModel.Instance.CurrentJob.JobName)
                        {
                            var resutl = MainViewModel.Instance.LoadJob(currentJob).Result;
                            if (!resutl)
                            {
                                App.GlobalLogger.LogError("RSLogixService", "Load job: " + currentJob + " from tag: " + TagJob + " fail!");
                            }
                            else
                            {
                                App.GlobalLogger.LogError("RSLogixService", "Load job: " + currentJob + " from tag: " + TagJob + " successfully!");
                            }
                        }
                    }
                }
                
                foreach(var item in ReadTagList)
                {
                    if (item.IsEnabled)
                    {
                        if (item.Source != String.Empty & item.Target != String.Empty)
                        {
                            var data = MyMicroLogix.ReadAny(item.Source);
                            VisionTagManager.SetValue(item.Target, data);
                        }
                    }
                    
                }
                foreach(var item in WriteTagList)
                {
                    if (item.IsEnabled)
                    {
                        if(item.Source != String.Empty & item.Target != String.Empty)
                        {
                            var VisionData = VisionTagManager.GetTag(item.Target);
                            if (VisionData != null)
                            {
                                if (VisionData.Type == TagType.Int)
                                {
                                    MyMicroLogix.WriteData(item.Source, ((IntTag)VisionData).IntValue);
                                }
                                if (VisionData.Type == TagType.Float)
                                {
                                    MyMicroLogix.WriteData(item.Source, (float)((FloatTag)VisionData).FloatValue);
                                }
                                if (VisionData.Type == TagType.String)
                                {
                                    MyMicroLogix.WriteData(item.Source, ((StringTag)VisionData).StringValue);
                                }
                                if (VisionData.Type == TagType.Bool)
                                {
                                    MyMicroLogix.WriteData(item.Source, ((BoolTag)VisionData).BoolValue ? 1 : 0);
                                }


                            }
                        }
                        
                    }
                    
                    
                }
              
            }catch (Exception ex)
            {
                if (ex.Message != lastError)
                {
                    App.GlobalLogger.LogError("EthernetIPViewModel-CheckJob", ex.Message);
                }
                lastError = ex.Message;
                ConnectionErrorCount++;
            }
            
        }
        Task CheckTask = null;
        bool _is_running = false;
        public int ConnectionErrorCount = 0;

        public override void Start()
        {
            if (IsEnabled)
            {
                if (State != ServiceState.Running & State != ServiceState.Initializing)
                {

                    CheckTask = Task.Run(() =>
                    {
                        State = ServiceState.Running;
                        _is_running = true;
                        ConnectionErrorCount = 0;
                        while (_is_running&State== ServiceState.Running)
                        {
                            //stop checking if cannot connect after trying for 10 times.
                            if (ConnectionErrorCount > 10)
                            {
                                Message = DateTime.Now.ToString("dd-MM-yy  HH:mm:ss") +": Stop service with reason: cannot connect to PLC at: " + IPAddress.ToString() + " after 10 times.";
                                break;
                            }
                            CheckJob();
                            Thread.Sleep(_interval);
                        }
                        State = ServiceState.Stopped;
                    }).ContinueWith((task) => { State = ServiceState.Stopped; });

                }
            }
        }

        public override void Stop()
        {
            if (CheckTask != null)
            {
               
                if (!CheckTask.IsCompleted)
                {
                    _is_running = false;
                    CheckTask.Wait();
                }
            }
            
        }
        int _interval=1000;
        [JsonProperty]
        public int Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _interval, value);
            }
        }
        string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _message, value);
            }
        }
        string _ip_address;
        [JsonProperty]
        public string IPAddress
        {
            get
            {
                return _ip_address;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _ip_address, value);
            }
        }

        public EthernetIPforCLXComm MyMicroLogix;
        string _tag_job;
        [JsonProperty]
        public string TagJob
        {
            get
            {
                return _tag_job;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _tag_job, value);
            }
        }
        bool _enable_load_job;
        [JsonProperty]
        public bool EnableLoadJob
        {
            get
            {
                return _enable_load_job;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _enable_load_job, value);
            }
        }
        [JsonProperty]
        public ObservableCollection<TagLink> ReadTagList { get; set; }
        [JsonProperty]
        public ObservableCollection<TagLink> WriteTagList { get; set; }
        public override void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public override void Load()
        {
            try
            {
                if (System.IO.File.Exists(ConfigDir)){
                    var loadObj = JsonConvert.DeserializeObject<RSLogixServiceViewModel>(System.IO.File.ReadAllText(ConfigDir));
                    this.IPAddress = loadObj.IPAddress;
                    this.TagJob = loadObj.TagJob;
                    this.Interval = loadObj.Interval;
                    this.Name = loadObj.Name;
                    this.IsEnabled = loadObj.IsEnabled;
                    this.WriteTagList = loadObj.WriteTagList;
                    this.ReadTagList = loadObj.ReadTagList;
                    if (ReadTagList == null)
                    {
                        ReadTagList = new ObservableCollection<TagLink>();

                    }
                    if (WriteTagList == null)
                    {
                        WriteTagList = new ObservableCollection<TagLink>();
                    }
                }
                

            }catch(Exception ex)
            {

            }
            
        }
    }
    public class TagLink
    {
        public bool IsEnabled { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
    }
}
