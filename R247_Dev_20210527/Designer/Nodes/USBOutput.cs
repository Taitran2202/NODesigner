using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.ComponentModel;
using NOVisionDesigner.Designer.Misc;
using DevExpress.Xpf.Charts;
using System.Collections.ObjectModel;
using NOVisionDesigner.Designer.Windows;
using Automation.BDaq;
using NOVisionDesigner.Designer.Editors;
using System.Timers;
using static NOVisionDesigner.Designer.Misc.InspectionContext;
using System.Windows;
using System.Threading;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("USB","USB 5800 Ouput Module")]
    public class USBOutput : BaseNode
    {
        static USBOutput()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<USBOutput>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {

            HelperMethods.LoadParam(item, this);
        }
        public override void OnLoadComplete()
        {
            if (IOControl != null && SelectedDevice!=null)
            {
                bool can_connected = false;
                var start = DateTime.Now;
                while (!can_connected)
                {
                    try
                    {
                        
                        IOControl.SelectedDevice = new DeviceInformation(SelectedDevice);
                        can_connected = true;
                    }
                    catch (Exception ex)
                    {
                        NOVisionDesigner.App.GlobalLogger.LogError("USB IO ERROR", ex.Message);
                        //Thread.Sleep(2000);
                        break;
                    }
                    if (DateTime.Now - start > TimeSpan.FromSeconds(30))
                    {
                        can_connected = true;
                    }
                }
                
                
            }
        }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public ValueNodeInputViewModel<bool> Enable { get; set; }
        //public ValueNodeInputViewModel<bool> ResultInput { get; set; }

        public InstantDoCtrl IOControl { get; set; }
        public ObservableCollection<BitValue> PortStates { get; set; } = new ObservableCollection<BitValue>();
        public string SelectedDevice { get; set; }

        int _pulse_duration=50;
        public int PulseDuration
        {
            get
            {
                return _pulse_duration;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _pulse_duration, value);
            }
        }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    Windows.USBIOOutput wd = new Windows.USBIOOutput( this);
                    wd.ShowDialog();
                    break;
            }
        }
        public static object iolock = new object();
        
        public override void Run(object context)
        {

            RunInside(Enable.Value);
            Result.OnNext(true);
            
        }

        public USBOutput(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "USB output";
            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                Editor = new BoolValueEditorViewModel(),
                PortType = "Boolean"
            };
            this.Inputs.Add(Enable);
            Enable.WhenAnyValue(x => x.Value).Subscribe(x =>
            {

                RunInside(x);
            });

            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Boolean"
            };
            this.Outputs.Add(Result);
            try
            {
                IOControl = new InstantDoCtrl();
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("USBOutput", "Cannot initialize");
            }
        }
        public bool ResultSignal;
        public void RunInside(bool input)
        {

            foreach (var item in PortStates)
            {
                if (item.Activation!=input)
                {
                    continue;
                }
                if (item.Duration > 0)
                {
                    lock (iolock)
                    {
                        IOControl.WriteBit(item.PortIndex, item.Index, item.State ? (byte)1 : (byte)0);
                    }
                    
                    Task.Run(new Action(() =>
                    {
                        Thread.Sleep(Math.Max(20, item.Duration));
                        lock (iolock)
                        {
                            IOControl.WriteBit(item.PortIndex, item.Index, !item.State ? (byte)1 : (byte)0);
                        }

                    }));
                }
                else
                {
                    lock (iolock)
                    {
                        IOControl.WriteBit(item.PortIndex, item.Index, item.State ? (byte)1 : (byte)0);
                    }

                }


            }
        }     
    }
    public class PortState:IHalconDeserializable
    {
        public int PortIndex { get; set; }
        public List<BitValue> BitStates { get; set; } = new List<BitValue>();

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class BitValue:IHalconDeserializable
    {
        public int Duration { get; set; }
        public int Index { get; set; }
        public bool State { get; set; }
        public int PortIndex { get; set; }
        public bool Activation { get; set; }
        public bool Visible { get; set; } = true;
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
