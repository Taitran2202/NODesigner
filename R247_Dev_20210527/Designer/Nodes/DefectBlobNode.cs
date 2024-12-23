using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.PropertiesViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Detect Defect Blob", Icon: "Designer/icons/icons8-particle-editor-96.png", sortIndex: 1)]
    public class DefectBlobNode : BaseNode
    {
        public ObservableCollection<DefectBlobTool> ToolList { get; set; } = new ObservableCollection<DefectBlobTool>();
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            
        }
        static DefectBlobNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DefectBlobNode>));
        }

        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }

        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }

        #region Properties
        [HMIProperty("Set context result")]
        public bool SetInspectionContext { get; set; }
        [HMIProperty("Display result table")]
        public bool DisplayResultTable { get; set; }
        [HMIProperty("Open blob setting")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                //UserViewModel.WriteActionDatabase(this.Name, "OpenEditor", null, null, "Command", null);
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); 
            }
        }  
        #endregion

        [SerializeIgnore]
        public Control PropertiesView
        {
            get
            {
                return new DefectBlobView(this);
            }
        }

        public override void Run(object context)
        {
            if (ImageInput.Value == null)
            {
                return;
            }
            HHomMat2D transform = FixtureInput.Value;
            HRegion results = new HRegion();
            results.GenEmptyObj();
            var InspectionContext = context as InspectionContext;
            foreach (var item in ToolList)
            {
                if (item.IsEnabled)
                {
                    var result = item.Run(ImageInput.Value, transform, context as InspectionContext);
                    bool resultBool = result.CountObj() > 0;
                    if (DisplayResultTable)
                    {
                        if (resultBool)
                        {
                            InspectionContext.SetResult(this.Name + "-" + item.ToolName, false);
                        }
                        else
                        {
                            InspectionContext.SetResult(this.Name + "-" + item.ToolName, true);
                        }
                    }
                    if (SetInspectionContext)
                    {
                        InspectionContext.result &= !resultBool;
                    }
                    
                    results=results.ConcatObj(result);
                }
               
            }
            RegionOutput.OnNext(results);
               
        }
        public DefectBlobNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Blob Detection";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);

            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(FixtureInput);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };
            this.Outputs.Add(RegionOutput);

        }

        

    }
    public class DefectBlobTool:IHalconDeserializable
    {
        public string ToolName { get; set; }
        public CollectionOfregion Regions { get; set; } = new CollectionOfregion();
        public bool IsEnabled { get; set; }=true;
        public bool ShowDisplay { get; set; } = true;
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
        public bool DisplayIndividual { get; set; } = true;
        [HMIProperty]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        [HMIProperty]
        public ImageChannel Channel { get; set; }
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
        public static (int row, int col) GetDisplayPosition(TextPosition DisplayPosition, int r1, int c1, int r2, int c2)
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
        public HRegion Run(HImage image, HHomMat2D transform,InspectionContext context)
        {
            if (image == null)
            {
                HRegion region = new HRegion();
                region.GenEmptyObj();
                return region;
            }
            HRegion RegionInspect = Regions.Region;
            HRegion regionInspectTransform = transform != null ? transform.AffineTransRegion(RegionInspect, "nearest_neighbor") : RegionInspect;
            var result = RunInside(image,  regionInspectTransform);

            if (ShowDisplay)
            {
                var displayContext = context;
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
                            var displayCor = GetDisplayPosition(DisplayPosition,r1, c1, r2, c2);
                            if (DisplayBlobName)
                            {
                                displayContext.inspection_result.AddText(Displaytext + ":" + result[i].Area, TextForeground, TextBackground, displayCor.row, displayCor.col);
                            }
                            else
                            {
                                displayContext.inspection_result.AddText( result[i].Area, TextForeground, TextBackground, displayCor.row, displayCor.col);
                            }
                            
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
                            var displayCor = GetDisplayPosition(DisplayPosition,r1, c1, r2, c2);
                            displayContext.inspection_result.AddText(Displaytext, TextBackground, TextBackground, displayCor.row, displayCor.col);
                        }
                    }

                }
                else  //display union region
                {
                    var regionUnion = result.Union1();
                    if (regionUnion.CountObj() > 0)
                    {
                        result.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                        var displayCor = GetDisplayPosition(DisplayPosition,r1, c1, r2, c2);
                        //add bounding box around union regions
                        displayContext.inspection_result.AddRect1(DisplayColor, r1, c1, r2, c2);
                        displayContext.inspection_result.AddText(Displaytext, TextForeground, TextBackground, displayCor.row, displayCor.col);
                    }


                }
            }
            return result;
        }
        public HRegion RunInside(HImage image,  HRegion regionInspectTransform)
        {
            if (image == null) { return null; }
            HImage imageChannel = GetImageChannel(image);

            HRegion result = new HRegion();
            result.GenEmptyRegion();
            if (Closing < 0.5)
            {
                result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue);
            }
            else
            {
                result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue).ClosingCircle(Closing);
            }
            if (IsFill)
            {
                result = result.FillUp();
            }
            if (Invert)
            {
                result =regionInspectTransform.Difference(result.Connection().SelectShape("area", "and", MinArea, MaxArea).Union1());
                //result = regionInspectTransform.Difference(imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue)).ClosingCircle(Closing);
            }
            //else
            //{
            //    if (Closing < 0.5)
            //    {
            //        result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue);
            //    }
            //    else
            //    {
            //        result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue).ClosingCircle(Closing);
            //    }

            //}
            
            result = result.Connection().SelectShape("area", "and", MinArea, MaxArea);

            return result;
        }

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
