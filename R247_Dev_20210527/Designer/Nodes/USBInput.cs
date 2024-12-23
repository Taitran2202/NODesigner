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
using System.Threading;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("USB", "USB 5800 Input Module")]
    public class USBInput : BaseNode
    {
        static USBInput()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<USBInput>));
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
            if (IOControl != null && SelectedDevice != null)
            {
                try
                {
                    IOControl.SelectedDevice = new DeviceInformation(SelectedDevice);
                }
                catch (Exception ex)
                {

                }

            }
        }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public ValueNodeInputViewModel<bool> Enable { get; set; }
        //public ValueNodeInputViewModel<bool> ResultInput { get; set; }

        public InstantDiCtrl IOControl { get; set; }
        public ObservableCollection<BitValue> PortStates { get; set; } = new ObservableCollection<BitValue>();
        public string SelectedDevice { get; set; }


        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    Windows.USBInput wd = new Windows.USBInput(this);
                    wd.ShowDialog();
                    break;
            }
        }
        public static object iolock = new object();

        public override void Run(object context)
        {

            Result.OnNext(RunInside(Enable.Value));

        }

        public USBInput(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "USB Input";
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
                IOControl = new InstantDiCtrl();
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("USB Input", "Cannot initialize");
            }



        }



        public bool ResultSignal;
        public bool RunInside(bool input)
        {
            
            if(PortStates.Count(x=>x.Activation == input) == 0)
            {
                return false;
            }
            bool result = true;
            foreach (var item in PortStates)
            {
                if (item.Activation != input)
                {
                    continue;
                }
                IOControl.ReadBit(item.PortIndex, item.Index, out byte data);
                if((data>0) != item.State)
                {
                    result = false;
                }  
            }
            return result;
        }
    }
}
