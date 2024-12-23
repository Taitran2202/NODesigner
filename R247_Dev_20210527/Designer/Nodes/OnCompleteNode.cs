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
    [NodeInfo("Event", "OnComplete")]
    public class OnCompleteNode : BaseNode
    {
        static OnCompleteNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<OnCompleteNode>));
        }
        public ValueNodeOutputViewModel<bool> InspectionResult { get; }
        public override void Run(object context)
        {

        }
        public void OnComplete(object sender, EventArgs e)
        {
            var context = sender as InspectionContext;
            InspectionResult.OnNext(context.result);
            BaseRun(context);
            Reset();
        }
        public OnCompleteNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "OnComplete";
            InspectionResult = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Inspection Result",
                PortType = "Boolean"
            };
            this.Outputs.Add(InspectionResult);
            designer.OnComplete += OnComplete;
        }
        public override void Dispose()
        {
            designer.OnComplete -= OnComplete;
            base.Dispose();
        }
    }

}
