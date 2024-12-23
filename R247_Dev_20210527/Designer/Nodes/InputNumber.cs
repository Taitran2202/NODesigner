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
    [NodeInfo("Input","Number")]
    public class InputNumberNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static InputNumberNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<InputNumberNode>));
        }
        [HMIProperty]
        public ValueNodeOutputViewModel<double> Output { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<double> Number { get; }
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
            
            Output.OnNext(Number.Value);
        }
        public InputNumberNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Text Input";

            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                PortType = "boolean"
            };
            this.Inputs.Add(Enable);

            Number = new ValueNodeInputViewModel<double>()
            {
                Name = "Number",
                PortType = "double",
                Editor = new DoubleValueEditorViewModel(1,0.1)
            };
            this.Inputs.Add(Number);
           

            Output = new ValueNodeOutputViewModel<double>()
            {
                Name = "Number",
                PortType = "double"
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
