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
using NOVisionDesigner.Designer.Nodes;
using System.Net.Sockets;

namespace NOVisionDesigner.Services.ViewModel
{
    [ServiceInfo("Job control","HostLink UDP")]
    public class HostLinkUDPViewModel : ApplicationService
    {
        NOVisionDesigner.Designer.Nodes.HostLinkUDP HostLinkClient = new HostLinkUDP();
        [JsonProperty]
        public string Name { get; set; } = "HOST LINK UDP";
        static HostLinkUDPViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new HostLinkUDPView(), typeof(IViewFor<HostLinkUDPViewModel>));
        }
        public override void Dispose()
        {
            Stop();
        }
        public HostLinkUDPViewModel()
        {

        }
        public void UpdateJobList()
        {
            JobNameList = MainViewModel.Instance.GetJobList();
        }
        private List<string> _JobNameList;
        public List<string> JobNameList 
        {
            get
            {
                return _JobNameList;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _JobNameList, value);
            }
        }
        NOVisionDesigner.ViewModel.GlobalTagManager VisionTagManager = NOVisionDesigner.ViewModel.GlobalTagManager.Instance;
        public HostLinkUDPViewModel(string ServiceDir,string id):base(ServiceDir,id)
        {
            this.WhenAnyValue(x => x.IPAddress).Subscribe(x =>
            {
                if (x != null)
                    HostLinkClient.Connect(x, Port);
            });
            this.WhenAnyValue(x => x.Port).Subscribe(x =>
            {
                if (x >= 0)
                    HostLinkClient.Connect(IPAddress, x);
            });
            JobAddress = "";
            WriteTagList = new ObservableCollection<TagLink>();
            ReadTagList = new ObservableCollection<TagLink>();
            JobTable = new ObservableCollection<KeyString>();
            Load();
        }
        string lastError = "";
        public bool ReadInt(UdpClient Client,string Address, out int Value, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}.L\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            // dataR.Remove(EndByte);
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    Value = 0;
                    return false;
                }
                if (int.TryParse(result, out int result2))
                {
                    Value = result2;
                    return true;

                }
                else
                {
                    Value = 0;
                    return false;
                }
            }
            else
            {
                Value = 0;
                return false;
            }


        }
        void CheckJob()
        {
            try
            {
                HostLinkClient.Connect(_ip_address, _port);
                if (HostLinkClient.Connected == false)
                {
                    return;
                }
                if (EnableLoadJob)
                {
                    if (JobAddress != "")
                    {
                        if(ReadInt(HostLinkClient.Client,JobAddress, out int Value))
                        {
                            CurrentJobValue = Value;
                            ConnectionErrorCount = 0;
                            var selectedJob = JobTable.FirstOrDefault(x => x.Key == Value);
                            if (selectedJob != null)
                            {
                                if (selectedJob.Value != MainViewModel.Instance.CurrentJob.JobName)
                                {
                                    var resutl = MainViewModel.Instance.LoadJob(selectedJob.Value).Result;
                                    if (!resutl)
                                    {
                                        App.GlobalLogger.LogError("HostLink UDP", "Load job: " + selectedJob.Value + " from address: " + JobAddress + " fail!");
                                    }
                                    else
                                    {
                                        App.GlobalLogger.LogInfo("HostLink UDP", "Load job: " + selectedJob.Value + " from address: " + JobAddress + " successfully!");
                                    }
                                }
                            }
                        }
                        else
                        {
                            CurrentJobValue = -1;
                        }
                        
                        
                    }
                }
                
                //foreach(var item in ReadTagList)
                //{
                //    if (item.IsEnabled)
                //    {
                //        if (item.Source != String.Empty & item.Target != String.Empty)
                //        {
                //            var data = HostLinkClient.read(item.Source);
                //            VisionTagManager.SetValue(item.Target, data);
                //        }
                //    }
                    
                //}
                //foreach(var item in WriteTagList)
                //{
                //    if (item.IsEnabled)
                //    {
                //        if(item.Source != String.Empty & item.Target != String.Empty)
                //        {
                //            var VisionData = VisionTagManager.GetTag(item.Target);
                //            if (VisionData != null)
                //            {
                //                if (VisionData.Type == TagType.Int)
                //                {
                //                    MyMicroLogix.WriteData(item.Source, ((IntTag)VisionData).IntValue);
                //                }
                //                if (VisionData.Type == TagType.Float)
                //                {
                //                    MyMicroLogix.WriteData(item.Source, (float)((FloatTag)VisionData).FloatValue);
                //                }
                //                if (VisionData.Type == TagType.String)
                //                {
                //                    MyMicroLogix.WriteData(item.Source, ((StringTag)VisionData).StringValue);
                //                }
                //                if (VisionData.Type == TagType.Bool)
                //                {
                //                    MyMicroLogix.WriteData(item.Source, ((BoolTag)VisionData).BoolValue ? 1 : 0);
                //                }


                //            }
                //        }
                        
                //    }
                    
                    
                //}
              
            }catch (Exception ex)
            {
                if (ex.Message != lastError)
                {
                    App.GlobalLogger.LogError("HostLink UDP-CheckJob", ex.Message);
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
        int _current_job_value = -1;
        public int CurrentJobValue
        {
            get
            {
                return _interval;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _current_job_value, value);
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
        int _port;
        [JsonProperty]
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _port, value);
            }
        }
        string _job_address;
        [JsonProperty]
        public string JobAddress
        {
            get
            {
                return _job_address;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _job_address, value);
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
        [JsonProperty]
        public ObservableCollection<KeyString> JobTable { get; set; }
        public override void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public override void Load()
        {
            try
            {
                if (System.IO.File.Exists(ConfigDir)){
                    var loadObj = JsonConvert.DeserializeObject<HostLinkUDPViewModel>(System.IO.File.ReadAllText(ConfigDir));
                    this.IPAddress = loadObj.IPAddress;
                    this.JobAddress = loadObj.JobAddress;
                    this.Port = loadObj.Port;
                    this.Interval = loadObj.Interval;
                    this.Name = loadObj.Name;
                    this.IsEnabled = loadObj.IsEnabled;
                    this.WriteTagList = loadObj.WriteTagList;
                    this.ReadTagList = loadObj.ReadTagList;
                    this.JobTable = loadObj.JobTable;
                    this.EnableLoadJob = loadObj.EnableLoadJob;
                    if (ReadTagList == null)
                    {
                        ReadTagList = new ObservableCollection<TagLink>();

                    }
                    if (WriteTagList == null)
                    {
                        WriteTagList = new ObservableCollection<TagLink>();
                    }
                    if (JobTable == null)
                    {
                        JobTable = new ObservableCollection<KeyString>();
                    }
                }
                

            }catch(Exception ex)
            {

            }
            
        }
    }
    public class KeyString
    {
        public int Key { get; set; }
        public string Value { get; set; }
    }
}
