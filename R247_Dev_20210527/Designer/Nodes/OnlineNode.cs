using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
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
    [NodeInfo("Event", "Online")]
    public class OnlineNode : BaseNode
    {
        static OnlineNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<OnlineNode>));
        }
        public ValueNodeOutputViewModel<bool> OnlineStateOutput { get; }
        public override void Run(object context)
        {

        }
        public void OnOnlineChanged(object sender, EventArgs e)
        {
            OnlineStateOutput.OnNext((bool)sender);
        }
        public OnlineNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Online State";
            OnlineStateOutput = new ValueNodeOutputViewModel<bool>()
            {
                Name = "State",
                PortType = "Boolean"
            };
            this.Outputs.Add(OnlineStateOutput);
            MainViewModel.OnlineChanged += OnOnlineChanged;
        }
        public override void Dispose()
        {
            MainViewModel.OnlineChanged -= OnOnlineChanged;
            base.Dispose();
        }
    }

}
