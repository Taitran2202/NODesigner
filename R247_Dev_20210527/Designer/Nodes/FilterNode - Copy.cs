using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Image","Paint Region")]
    public class PaintRegionNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        static PaintRegionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<PaintRegionNode>));
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeInputViewModel<int> GrayValue { get; }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    break;
            }
        }
        public PaintRegionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Paint Region";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "HImage"
            };
            this.Inputs.Add(ImageInput);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);

            GrayValue = new ValueNodeInputViewModel<int>()
            {
                Name = "Gray Value",
                PortType = "Int",
                Editor = new IntegerValueEditorViewModel() { Value=255}
            };
            this.Inputs.Add(GrayValue);
            ImageOutput = new ValueNodeOutputViewModel<HImage>
            {
                Name = "Image Output",
                PortType = "HImage"
            };
            this.Outputs.Add(ImageOutput);
        }
        public override void Run(object context)
        {
            var result = RunInside(ImageInput.Value, RegionInput.Value, GrayValue.Value);
            if (ShowDisplay)
            {
                (context as InspectionContext).inspection_result.AddDisplay(result,"green");
            }
            ImageOutput.OnNext(result);
        }
        public HImage RunInside(HImage image, HRegion region, int grayvalue)
        {
            HImage result;
            try
            {
                int channel = image.CountChannels();
                double[] paintValue = new double[channel];
                for(int i = 0; i < channel; i++)
                {
                    paintValue[i] = grayvalue;
                }

                result =image.PaintRegion(region.Union1(), new HTuple(paintValue), new HTuple("fill"));
            }
            catch (Exception ex)
            {
                result = null;
            }
            //if (result.Column.DArr.Length >0)
            //    e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1, row2), new HTuple(col1, col2)), _color);
            //else
            //    e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1, row2), new HTuple(col1, col2)), "red");
            //e.inspection_result?.AddText(Math.Round(distance.D, 2).ToString(), "black", "#ffffffaa", (row1 + row2) / 2, (col1 + col2) / 2);
            return result;
        }
    }


}
