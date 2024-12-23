using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Misc", "Command")]
    public class CommandNode : BaseNode
    {
        static CommandNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<CommandNode>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        public ValueNodeOutputViewModel<bool> Trigger { get; }
        public override void Run(object context)
        {

        }
        
        [HMIProperty("Command")]
        public IReactiveCommand CommandButton
        {
            get { return ReactiveCommand.Create((Control sender) => CommandSource.OnNext(true)); }
        }
        public OutputSource<bool> CommandSource = new OutputSource<bool>();
        
        public CommandNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Command";
            Trigger = new ValueNodeOutputViewModel<bool>()
            {
                Name = "True/False",
                PortType = "boolean"
            };
            this.Outputs.Add(Trigger);
            CommandSource.Subscribe(loaded =>
            {
                //var start = DateTime.Now;
                
                Trigger.OnNext(true);
                
                
            });
           


        }
    }
   
}
