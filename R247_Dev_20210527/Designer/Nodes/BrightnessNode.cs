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
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Image","Brightness", Icon: "Designer/icons/icons8-brightness-100.png",sortIndex:5)]
    public class BrightnessNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static BrightnessNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<BrightnessNode>));
        }

        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HHomMat2D> Fixture { get; }
        public ValueNodeInputViewModel<HRegion> Region { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<double> Mean { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<double> Deviation { get; }

        #region Properties
        [HMIProperty("Default Editor")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender));
            }
        }

        [HMIProperty]
        public string DisplayColor { get; set; } = "#00ff00ff";
        [HMIProperty]
        public bool ShowRegion { get; set; } = true;
        [HMIProperty]
        public bool ShowText { get; set; } = true;
        [HMIProperty]
        public string Prefix { get; set; } = "Brightness: ";
        [HMIProperty]
        public int DisplayFontSize { get; set; } = 12;
        [HMIProperty]
        public double RangeLower { get; set; } = 100;
        [HMIProperty]
        public double RangeHigher { get; set; } = 200;
        [HMIProperty]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        [HMIProperty]
        public ImageChannel Channel { get; set; }
        #endregion
        
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    //Default editor
                    break;
            }
        }
        public (int row,int col) GetDisplayPosition(int r1,int c1,int r2,int c2)
        {
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return (r2, c1);
                    case TextPosition.Top:
                    return (r1, c1);
                    case TextPosition.Left:
                    return (r2, c1);
                    default:
                    return (r1, c2);
            }
        }
        public override void Run(object context)
        {
            var result = RunInside(Image.Value, Fixture.Value, Region.Value);
            
            if (ShowDisplay)
            {
                InspectionContext inspectionContext = context as InspectionContext;
                if (ShowRegion)
                {
                    inspectionContext.inspection_result.AddRegion(result.CheckRegion, DisplayColor);
                }
                if (ShowText)
                {
                    result.CheckRegion.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                    var textLocation = GetDisplayPosition(r1, c1, r2, c2);
                    if(result.mean>= RangeLower & result.mean <= RangeHigher)
                    {
                        inspectionContext.inspection_result.AddText(Prefix + Math.Round(result.mean, 2),
                        "black", "#ffffffff", textLocation.row, textLocation.col, DisplayFontSize);
                    }
                    else
                    {
                        inspectionContext.inspection_result.AddText(Prefix + Math.Round(result.mean, 2),
                        "black", "#ff0000ff", textLocation.row, textLocation.col, DisplayFontSize);
                    }
                    
                }
               
                
            }
            Mean.OnNext(Math.Round(result.mean,2));
            Deviation.OnNext(Math.Round(result.deviation,2));
        }
        public BrightnessNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Brightness";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);
            Region = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region",
                Editor = new RegionValueEditorViewModel()
            };
            this.Inputs.Add(Region);

            Fixture = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(Fixture);

            Mean = new ValueNodeOutputViewModel<double>()
            {
                Name = "Mean",
                PortType = "double",
                Editor = new DefaultOutputValueEditorViewModel<double>()

            };
            this.Outputs.Add(Mean);
            Deviation = new ValueNodeOutputViewModel<double>()
            {
                Name = "Deviation",
                PortType = "double",
                Editor = new DefaultOutputValueEditorViewModel<double>()

            };
            this.Outputs.Add(Deviation);


        }

        public (HRegion CheckRegion,double mean,double deviation) RunInside(HImage image, HHomMat2D fixture,HRegion region)
        {        
            HRegion regionInspectTransform = fixture != null ? fixture.AffineTransRegion(region, "nearest_neighbor") : region;
            if (image == null) { return (regionInspectTransform, 0, 0); }
            HImage imageChannel = GetImageChannel(image);

            double mean = imageChannel.Intensity(regionInspectTransform, out double deviation);
            return (regionInspectTransform,mean, deviation);           
        }
        public HImage GetImageChannel(HImage image_original)
        {
            if (image_original == null)
            {
                return null;
            }
            HImage image;
            if (Channel == 0)
            {
                image = image_original.Rgb1ToGray();
            }
            else
            {
                HTuple channels = image_original.CountChannels();
                if (channels > (int)Channel)
                    image = image_original.AccessChannel((int)Channel);
                else
                {
                    image = image_original.AccessChannel(channels);
                }
            }
            return image;
        }
    }
}
