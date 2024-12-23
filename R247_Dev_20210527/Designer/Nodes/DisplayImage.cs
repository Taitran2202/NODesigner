using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Extensions;
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
    [NodeInfo("Display","Display Image")]
    public class DisplayImage : BaseNode
    {
        static DisplayImage()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DisplayImage>));
        }

        public ValueNodeInputViewModel<HImage> ResultInput { get; }

        public DisplayImage(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Display";

            this.CanBeRemovedByUser = false;

            ResultInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"

            };          
            this.Inputs.Add(ResultInput);
            this.ResultInput.WhenAnyValue(x => x.Value).Subscribe((x) => { if (x != null) 
                {
                    if (ShowDisplay)
                    {
                        designer.display.HalconWindow?.DispObj(x);
                    }
                } 
            });

        }


    }
}
