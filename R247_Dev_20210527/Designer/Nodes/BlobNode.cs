using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Detect Blob", Icon: "Designer/icons/icons8-particle-editor-96.png", sortIndex: 1)]
    public class BlobNode : BaseNode
    {
        CollectionOfregion _regions = new CollectionOfregion();
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            _regions.Save(file);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            _regions.Load(item);
        }
        static BlobNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<BlobNode>));
        }

        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> HomInput { get; }
        public ValueNodeInputViewModel<int> Threshold { get; }

        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }
        public string TypeNG;

        #region Properties
        [HMIProperty("Open blob setting")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                UserViewModel.WriteActionDatabase(this.Name, "OpenEditor", null, null, "Command", null);
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); 
            }
        }        

        [HMIProperty]
        public double LowerValue { get; set; } = 0;
        [HMIProperty]
        public double UpperValue { get; set; } = 255;
        [HMIProperty]
        public int MinArea { get; set; } = 100;
        [HMIProperty]
        public int MaxArea { get; set; } = 9999999;
        [HMIProperty]
        public bool IsFill { get; set; } = false;
        [HMIProperty]
        public bool Invert { get; set; } = false;
        [HMIProperty]
        public bool ShowRegion { get; set; } = true;
        [HMIProperty]
        public double Closing { get; set; } = 1;
        [HMIProperty]
        public bool DisplayArea { get; set; } = false;
        [HMIProperty]
        public bool DisplayBlobName { get; set; } = false;
        [HMIProperty]
        public string BlobName { get; set; } = "";
        [HMIProperty]
        public int MaxBlobCount { get; set; } = 20;
        public string DisplayColor { get; set; } = "#00ff00ff";
        public string TextForeground { get; set; } = "#000000ff";
        public string TextBackground { get; set; } = "#ffffffff";
        [HMIProperty]
        public bool DisplayIndividual { get; set; } =true;
        [HMIProperty]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        [HMIProperty]

        public CollectionOfregion Regions
        {
            get
            {
                return _regions;
            }
            set
            {
                if (_regions != value)
                {
                    _regions = value;
                    //RaisePropertyChanged("Regions");
                }
            }
        }
        public ImageChannel Channel { get; set; }
        #endregion
        
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    BlobWindow wd = new BlobWindow(this);
                    wd.ShowDialog();
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
            if (ImageInput.Value == null)
            {
                return;
            }
            HRegion RegionInspect = _regions.Region;
            HRegion regionInspectTransform = HomInput.Value != null ? HomInput.Value.AffineTransRegion(RegionInspect, "nearest_neighbor") : RegionInspect;
            var result = RunInside(ImageInput.Value, HomInput.Value, regionInspectTransform);
            
            if (ShowDisplay)
            {
                var displayContext = (context as InspectionContext);
                if (ShowRegion)
                {
                    displayContext.inspection_result.AddRegion(regionInspectTransform, DisplayColor);
                }
                displayContext.inspection_result.AddRegion(result, "red");
                var Displaytext = DisplayBlobName ? BlobName : "";
                if (DisplayIndividual)
                {
                    if (DisplayArea) //only apply if Display invidiual are true
                    {
                        for (int i = 1; i < result.CountObj() + 1; i++)
                        {
                            if (i > MaxBlobCount)
                            {
                                break;
                            }
                            result[i].SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                            var displayCor = GetDisplayPosition(r1, c1, r2, c2);
                            displayContext.inspection_result.AddText(Displaytext + ":" + result[i].Area, TextForeground, TextBackground, displayCor.row, displayCor.col);
                        }

                    }
                    else
                    {
                        for (int i = 1; i < result.CountObj() + 1; i++)
                        {
                            if (i > MaxBlobCount)
                            {
                                break;
                            }
                            result[i].SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                            var displayCor = GetDisplayPosition(r1, c1, r2, c2);
                            displayContext.inspection_result.AddText(Displaytext,TextBackground, TextBackground, displayCor.row, displayCor.col);
                        }
                    }

                }
                else  //display union region
                {
                    var regionUnion = result.Union1();
                    if (regionUnion.CountObj() > 0)
                    {
                        result.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                        var displayCor = GetDisplayPosition(r1, c1, r2, c2);
                        //add bounding box around union regions
                        displayContext.inspection_result.AddRect1(DisplayColor, r1, c1, r2, c2);
                        displayContext.inspection_result.AddText(Displaytext, TextForeground, TextBackground, displayCor.row, displayCor.col);
                    }
                    

                }
            }
            RegionOutput.OnNext(result);
        }
        public BlobNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Blob Detection";
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
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(HomInput);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };
            this.Outputs.Add(RegionOutput);

        }

        public HRegion RunInside(HImage image, HHomMat2D transform,HRegion regionInspectTransform)
        {
            if(image == null) { return null; }
            HImage imageChannel = GetImageChannel(image);
           
            HRegion result = new HRegion();
            result.GenEmptyRegion();
            
            if (Invert)
            {
                result = regionInspectTransform.Difference(imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue)).ClosingCircle(Closing);
            }
            else
            {
                if (Closing < 0.5)
                {
                    result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue);
                }
                else
                {
                    result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue).ClosingCircle(Closing);
                }
                
            }
            if (IsFill)
            {
                result = result.FillUp();
            }
            result= result.Connection().SelectShape("area", "and", MinArea, MaxArea);

            return result;
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
    public enum ImageChannel
    {
        Gray = 0, Red = 1, Green = 2, Blue = 3
    }
    public enum TextPosition
    {
        Left,Top,Right,Bottom
    }
}
