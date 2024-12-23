using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.ComponentModel;
using NOVisionDesigner.Designer.Misc;
using DevExpress.Xpf.Charts;
using System.Collections.ObjectModel;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Designer.PropertiesViews;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using NOVisionDesigner.Designer.Editors;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Timer","Timer")]
    public class TimerBlock : BaseNode
    {
        #region properties
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }

        

        #endregion
        static TimerBlock()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<TimerBlock>));
        }
        public ValueNodeInputViewModel<int> IntervalInput { get; }
        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        public ValueNodeOutputViewModel<bool> Output { get; }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    break;
            }
        }
        public override void Run(object context)
        {
            
        }

        public TimerBlock(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {

            this.Name = "Timer";
            
            EnabledInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enabled",
                PortType = "boolean"

            };
            this.Inputs.Add(EnabledInput);
            IntervalInput = new ValueNodeInputViewModel<int>()
            {
                Name = "Interval",
                PortType = "Integer",
                Editor= new IntegerValueEditorViewModel(500)

            };
            this.Inputs.Add(IntervalInput);
            Output = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Bool"

            };
            this.Outputs.Add(Output);

            EnabledInput.WhenAnyValue(x => x.Value).Subscribe(x =>
            {
                if (x)
                {
                    
                    IsRunning = true;
                    if (TimerTask == null)
                    {
                        tokenSource = new CancellationTokenSource();
                        TimerTask = Task.Run(() =>
                        {
                            while (IsRunning)
                            {
                                Output.Value.OnNext(true);
                                Thread.Sleep(IntervalInput.Value);
                            }
                            
                        }, tokenSource.Token);
                    }
                }
                else
                {
                    IsRunning = false;
                    if (TimerTask != null)
                    {
                        if (!TimerTask.IsCompleted)
                        {
                            TimerTask.Wait(IntervalInput.Value);
                        }
                        if (TimerTask.IsCompleted)
                        {
                            TimerTask = null;
                            return;
                        }
                        else
                        {
                            TimerTask.Wait(IntervalInput.Value);
                        }
                        if (!TimerTask.IsCompleted)
                        {
                            if (tokenSource != null)
                            {
                                tokenSource.Cancel();
                            }
                        }
                    }
                    
                }
            });

        }
        CancellationTokenSource tokenSource;
        public bool IsRunning { get; set; }
        Task TimerTask;
        public bool RunInside(int interval)
        {

            return true;
        }
    }

    public class TimerTool : INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Save(HFile file)
        {
            
            (new HTuple(TimerName)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(DueTime)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(PeriodTime)).SerializeTuple().FwriteSerializedItem(file);

        }
        public TimerTool(DeserializeFactory item, TimerBlock node)
        {
            TimerName = item.DeserializeTuple();
            DueTime = item.DeserializeTuple();
            PeriodTime = item.DeserializeTuple();
            this.node = node;
            SetTimer();
            SetTimerDisplay();
        }


        bool _output;
        public bool OutPut
        {
            get
            {
                return _output;
            }
            set
            {
                if (_output != value)
                {
                    _output = value;
                    RaisePropertyChanged("TimerTick");
                }
            }
        }

        bool _isStart;
        public bool isStart
        {
            get
            {
                return _isStart;
            }
            set
            {
                if (_isStart != value)
                {
                    _isStart = value;
                    RaisePropertyChanged("isStart");
                }
            }
        }

        bool _isStop = true;
        public bool isStop
        {
            get
            {
                return _isStop;
            }
            set
            {
                if (_isStop != value)
                {
                    _isStop = value;
                    RaisePropertyChanged("isStop");
                }
            }
        }
        string _timer_name = "No name";
        public string TimerName
        {
            get
            {
                return _timer_name;
            }
            set
            {
                if (_timer_name != value)
                {
                    _timer_name = value;
                    RaisePropertyChanged("TimerName");
                }
            }
        }
        int _due_time = 100;
        public int DueTime
        {
            get
            {
                return _due_time;
            }
            set
            {
                if (_due_time != value)
                {
                    _due_time = value;
                    RaisePropertyChanged("DueTime");
                }
            }
        }
        int _period_time = 100;
        public int PeriodTime
        {
            get
            {
                return _period_time;
            }
            set
            {
                if (_period_time != value)
                {
                    _period_time = value;
                    RaisePropertyChanged("PeriodTime");
                }
            }
        }
        public TimerBlock node;
        public TimerTool(TimerBlock node)
        {
            this.node = node;
            SetTimer();
            SetTimerDisplay();
        }
        public void SetTimer()
        {
            //timerFalling = new Timer();
            //timerFalling.Interval = PeriodTime;

            //timerRising = new Timer();
            //timerRising.Interval = DueTime;

            //timerFalling.Tick += TimerFalling_Tick;
            //timerRising.Tick += TimerRising_Tick;
        }
        public void SetTimerDisplay()
        {
            //timerDisplay = new Timer();
            //timerDisplay.Interval = 50;
            //timerDisplay.Tick += TimerDisplay_Tick;
        }

        public int maxTimeDisplay = 5000;
        private void TimerDisplay_Tick(object sender, EventArgs e)
        {
            if (this.DataPoints.Count > 0)
            {
                if ((DateTime.Now - this.DataPoints[0].Argument).TotalMilliseconds > maxTimeDisplay)
                {
                    this.DataPoints.RemoveAt(0);
                    if (OutPut)
                    {
                        this.DataPoints.Add(new DataPoint(DateTime.Now, 1));
                    }
                    else
                    {
                        this.DataPoints.Add(new DataPoint(DateTime.Now, 0));
                    }
             
                }
                else
                {
                    if (OutPut)
                    {
                        this.DataPoints.Add(new DataPoint(DateTime.Now, 1));
                    }
                    else
                    {
                        this.DataPoints.Add(new DataPoint(DateTime.Now, 0));
                    }
                }
            }
            else
            {
                if (OutPut)
                {
                    this.DataPoints.Add(new DataPoint(DateTime.Now, 1));
                }
                else
                {
                    this.DataPoints.Add(new DataPoint(DateTime.Now, 0));
                }
            }
        }

        private void TimerRising_Tick(object sender, EventArgs e)
        {
            OutPut = false;
            this.node.Output.OnNext(OutPut);

            //timerRising.Stop();
            //timerFalling.Start();
        }

        private void TimerFalling_Tick(object sender, EventArgs e)
        {
            OutPut = true;
            this.node.Output.OnNext(OutPut);

            //timerRising.Start();
            //timerFalling.Stop();
        }
        public ObservableCollection<DataPoint> DataPoints { get; } = new ObservableCollection<DataPoint>();

        public class DataPoint
        {
            public DateTime Argument { get; set; }
            public int Value { get; set; }

            public DataPoint(DateTime argument, int value)
            {
                this.Argument = argument;
                this.Value = value;
            }

        }
    }


}
 



