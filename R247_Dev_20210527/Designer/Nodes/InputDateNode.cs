using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Input","DateTime")]
    public class InputDateNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static InputDateNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<InputDateNode>));
        }
        public ValueNodeOutputViewModel<DateTime> Output { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<DateTime> Input { get; }
        public ValueNodeInputViewModel<bool> Enable { get; }

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
            
            Output.OnNext(Input.Value);
        }
        public InputDateNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Date Input";

            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                PortType = "boolean"
            };
            this.Inputs.Add(Enable);

            Input = new ValueNodeInputViewModel<DateTime>()
            {
                Name = "InputDate",
                PortType = "DateTime",
                Editor = new DateTimeValueEditorViewModel()
            };
            this.Inputs.Add(Input);
           

            Output = new ValueNodeOutputViewModel<DateTime>()
            {
                Name = "OutputDate",
                PortType = "DateTime"
            };
            this.Outputs.Add(Output);
            Enable.ValueChanged.Subscribe(x =>
            {
                if (this.NodeType == NodeType.Event)
                {
                    this.Run(null);
                }
            });
        }

       
        
    }
    
}
