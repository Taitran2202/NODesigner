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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Blob Filtering")]
    public class RegionFilterNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }
        static RegionFilterNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<RegionFilterNode>));
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        //public ValueNodeInputViewModel<HRegion> Searchregion { get; }
        //public ValueNodeInputViewModel<HHomMat2D> HomInput { get; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }
        #region Properties
        public ObservableCollection<SelectShape> Filters { get; set; } = new ObservableCollection<SelectShape>();
        public string Operation { get; set; } = "and";
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    //var image = ImageInput.Value.CountChannels() > 1 ? ImageInput.Value.Rgb1ToGray() : ImageInput.Value;
                    RegionFilterWindow wd = new RegionFilterWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.ShowDialog();
                    break;
            }
        }
        public override void Run(object context)
        {
           var InspectionContext = context as InspectionContext;
            var filteredRegion = RunInside(RegionInput.Value);
            if (ShowDisplay)
            {
                InspectionContext.inspection_result.AddRegion(filteredRegion, "red");
            }
            RegionOutput.OnNext(filteredRegion);
        }
        public RegionFilterNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Blob Filter";
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
            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Filtered Region",
                PortType = "HRegion"
            };
            this.Outputs.Add(RegionOutput);
        }

        public HRegion RunInside(HRegion Input)
        {
            if (Filters.Count == 0)
            {
                return Input;
            }
            var features_array = new List<string>();
            var max_array = new List<double>();
            var min_array = new List<double>();
            foreach (var item in Filters)
            {
                if (item.Min <= item.Max)
                {
                    features_array.Add(item.Feature);
                    max_array.Add(item.Max);
                    min_array.Add(item.Min);
                }
            }
            if (features_array.Count == 0)
            {
                return Input;
            }

            return Input.Connection().SelectShape(features_array.ToArray(), Operation, min_array.ToArray(), max_array.ToArray());
            
            

            

        }
    }

    public class SelectShape:IHalconDeserializable
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public string Feature { get; set; } = "area";

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
