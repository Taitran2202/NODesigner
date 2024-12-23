using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
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
    [NodeInfo("Filter","Image Filtering")]
    public class FilterNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            filter.Save(file);
            filter.Regions.Save(file);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            filter.Load(item);
            filter.Regions.Load(item);
        }

        static FilterNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<FilterNode>));
        }
        MultiImageFilter filter = new MultiImageFilter();
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> HomInput { get; }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        [HMIProperty("Image Filter Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    FilterWindow wd = new FilterWindow(ImageInput.Value, HomInput.Value, filter);
                    wd.ShowDialog();
                    break;
            }
        }
        public FilterNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Filter";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);

            HomInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
            };
            this.Inputs.Add(HomInput);

            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "HImage"
                //Value = this.WhenAnyValue(vm => vm.ImageInput.Value, vm => vm.HomInput.Value)
                //.Select(name =>
                //{
                //    if (name.Item1 != null)
                //    {
                //        HRegion result = RunNode(name.Item1, name.Item2, LowerValue, UpperValue);
                //                    //if (ShowDisplay && result != null)
                //                    //{
                //                    //    designer.display.HalconWindow.SetDraw("margin");
                //                    //    designer.display.HalconWindow.SetLineWidth(2.0);
                //                    //    designer.display.HalconWindow.SetColor("green");
                //                    //    designer.display.HalconWindow?.DispObj(result);
                //                    //}
                //                    return RunNode(name.Item1, name.Item2, LowerValue, UpperValue);
                //    }
                //    return null;
                //})
            };
            this.Outputs.Add(ImageOutput);
        }
        public override void Run(object context)
        {
            ImageOutput.OnNext(RunInside(ImageInput.Value, HomInput.Value, filter));
        }
        public HImage RunInside(HImage image, HHomMat2D hom, MultiImageFilter filter)
        {
            HImage result;
            try
            {
                  result = filter.Update(image, hom);
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
