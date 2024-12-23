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
    [NodeInfo("Input","String")]
    public class InputStringNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static InputStringNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<InputStringNode>));
        }
        [HMIProperty]
        public ValueNodeOutputViewModel<string> Output { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Text { get; }
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
            
            Output.OnNext(Text.Value);
        }
        public InputStringNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Text Input";

            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                PortType = "boolean"
            };
            this.Inputs.Add(Enable);

            Text = new ValueNodeInputViewModel<string>()
            {
                Name = "Text",
                PortType = "String",
                Editor = new StringValueEditorViewModel(false)
            };
            this.Inputs.Add(Text);
           

            Output = new ValueNodeOutputViewModel<string>()
            {
                Name = "Text",
                PortType = "string"
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
