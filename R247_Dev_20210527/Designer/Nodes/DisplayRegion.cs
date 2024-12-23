using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Display","Display Region")]
    public class DisplayRegion : BaseNode
    {
        static DisplayRegion()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DisplayRegion>));
        }

        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeInputViewModel<string> ColorInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public override void Run(object context)
        {
            if (ShowDisplay)
            {
                //designer.display.HalconWindow?.ClearWindow();
                if(FixtureInput.Value != null)
                {
                    (context as InspectionContext).inspection_result.AddRegion(RegionInput.Value.AffineTransRegion(FixtureInput.Value,"constant"), ColorInput.Value);
                }
                else
                {
                    (context as InspectionContext).inspection_result.AddRegion(RegionInput.Value, ColorInput.Value);
                }
                
            }
        }
        public DisplayRegion(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Display";

            this.CanBeRemovedByUser = false;

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"

            };
            this.Inputs.Add(RegionInput);

            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D",
               

            };
            this.Inputs.Add(FixtureInput);
            ColorInput = new ValueNodeInputViewModel<string>()
            {
                Name = "Color",
                PortType = "Color",
                Editor = new StringColorValueEditorViewModel()

            };
            this.Inputs.Add(ColorInput);
            //this.ResultInput.WhenAnyValue(x => x.Value).Subscribe((x) => { if (x != null) 
            //    {

            //    } 
            //});

        }
    }
}
