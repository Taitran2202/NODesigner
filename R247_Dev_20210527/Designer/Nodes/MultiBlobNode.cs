using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Extensions;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Multiple Detect Blob", Icon: "Designer/icons/icons8-particle-editor-96.png", sortIndex: 1)]
    public class MultiBlobNode : BaseNode
    {
       
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static MultiBlobNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MultiBlobNode>));
        }

        public ValueNodeInputViewModel<HImage> ImageIn { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureIn { get; }

        public ValueNodeOutputViewModel<HRegion> RegionOut { get; }

        #region Properties
        [HMIProperty("Open blob setting")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                //UserViewModel.WriteActionDatabase(this.Name, "OpenEditor", null, null, "Command", null);
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); 
            }
        }

        [HMIProperty]
        public ObservableCollection<BlobDetection> ListBlobDetection { get; set; } = new ObservableCollection<BlobDetection>();
        #endregion
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    MultiBlobWindow wd = new MultiBlobWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
            }
        }       
        public override void Run(object context)
        {
            if (ImageIn.Value == null)
            {
                return;
            }
            var fixture = FixtureIn.Value;
            if (fixture == null)
            {
                fixture = new HHomMat2D();
            }
            var image = ImageIn.Value;
            HRegion regionOut = RunInside(image,fixture,context as InspectionContext);
            RegionOut.OnNext(regionOut);
        }

        private HRegion RunInside( HImage image, HHomMat2D fixture, InspectionContext context)
        {
            HRegion regionOut = new HRegion();
            regionOut.GenEmptyRegion();
            foreach (var item in ListBlobDetection)
            {
                if (item.IsEnabled)
                {
                    var region = item.Run(image, fixture, context, ShowDisplay);
                    regionOut = regionOut.ConcatObj(region);
                }

            }

            return regionOut;
        }

        public MultiBlobNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Multiple Blob Detection";
            ImageIn = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "HImage"
            };
            this.Inputs.Add(ImageIn);

            FixtureIn = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(FixtureIn);

            RegionOut = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "HRegion"
            };
            this.Outputs.Add(RegionOut);

        }
    }
    public class BlobDetection:ReactiveObject,IHalconDeserializable
    {
        //static BlobDetection()
        //{
        //    Splat.Locator.CurrentMutable.Register(() => new BlobDetectionView(), typeof(IViewFor<BlobDetection>));
        //}
        public CollectionOfregion Regions { get; set; }=new CollectionOfregion();

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        double _lower_value=0;
        public double LowerValue
        {
            get { return _lower_value; }
            set { this.RaiseAndSetIfChanged(ref _lower_value, value); }
        }
        double _upper_value=255;
        public double UpperValue
        {
            get { return _lower_value; }
            set { this.RaiseAndSetIfChanged(ref _upper_value, value); }
        }
        int _min_area = 100;
        public int MinArea
        {
            get { return _min_area; }
            set { this.RaiseAndSetIfChanged(ref _min_area, value); }
        }
        int _max_area = int.MaxValue;
        public int MaxArea
        {
            get { return _max_area; }
            set { this.RaiseAndSetIfChanged(ref _max_area, value); }
        }
        bool _is_fill = false;
        public bool IsFill
        {
            get { return _is_fill; }
            set { this.RaiseAndSetIfChanged(ref _is_fill, value); }
        }
        bool _invert = false;
        public bool Invert
        {
            get { return _invert; }
            set { this.RaiseAndSetIfChanged(ref _invert, value); }
        }
        bool _show_region = true;
        public bool ShowRegion
        {
            get { return _show_region; }
            set { this.RaiseAndSetIfChanged(ref _show_region, value); }
        }
        bool _display_area = true;
        public bool DisplayArea
        {
            get { return _display_area; }
            set { this.RaiseAndSetIfChanged(ref _display_area, value); }
        }
        bool _display_blob_name = true;
        public bool DisplayBlobName
        {
            get { return _display_blob_name; }
            set { this.RaiseAndSetIfChanged(ref _display_blob_name, value); }
        }
        string _blob_name = "";
        public string BlobName
        {
            get { return _blob_name; }
            set { this.RaiseAndSetIfChanged(ref _blob_name, value); }
        }
        int _max_blob_count = 20;
        public int MaxBlobCount
        {
            get { return _max_blob_count; }
            set { this.RaiseAndSetIfChanged(ref _max_blob_count, value); }
        }
        double _closing = 255;
        public double Closing
        {
            get { return _closing; }
            set { this.RaiseAndSetIfChanged(ref _closing, value); }
        }
        
        
        public string RegionColor { get; set; } = "#00ff00ff"; //default green
        public string BlobColor { get; set; } = "#ff0000ff"; //default red
       
        public bool DisplayIndividual { get; set; } = true;
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        public ImageChannel Channel { get; set; }
        public bool IsEnabled { get; set; }
        public HRegion Run(HImage ImageIn,HHomMat2D FixtureIn, InspectionContext context,bool ShowDisplay)
        {
            if (ImageIn == null)
            {
                return null;
            }
            HRegion RegionInspect = Regions.Region;
            HRegion regionInspectTransform = FixtureIn != null ? FixtureIn.AffineTransRegion(RegionInspect, "nearest_neighbor") : RegionInspect;
            var result = RunInside(ImageIn, regionInspectTransform);

            if (ShowDisplay)
            {
                var displayContext =context;
                if (ShowRegion)
                {
                    displayContext.inspection_result.AddRegion(regionInspectTransform, RegionColor);
                }
                displayContext.inspection_result.AddRegion(result, BlobColor);
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
                            var displayCor = Functions.GetDisplayPosition(DisplayPosition, r1, c1, r2, c2);
                            displayContext.inspection_result.AddText(Displaytext + ":" + result[i].Area, "black", RegionColor, displayCor.row, displayCor.col);
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
                            var displayCor = Functions.GetDisplayPosition(DisplayPosition,r1, c1, r2, c2);
                            displayContext.inspection_result.AddText(Displaytext, "black", RegionColor, displayCor.row, displayCor.col);
                        }
                    }

                }
                else  //display union region
                {
                    var regionUnion = result.Union1();
                    if (regionUnion.CountObj() > 0)
                    {
                        result.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                        var displayCor = Functions.GetDisplayPosition(DisplayPosition,r1, c1, r2, c2);
                        //add bounding box around union regions
                        displayContext.inspection_result.AddRect1(RegionColor, r1, c1, r2, c2);
                        displayContext.inspection_result.AddText(Displaytext, "black", RegionColor, displayCor.row, displayCor.col);
                    }


                }
            }
            return result;
        }

        public HRegion RunInside(HImage image, HRegion regionInspectTransform)
        {
            if (image == null) { return null; }
            HImage imageChannel = GetImageChannel(image,Channel);

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
            result = result.Connection().SelectShape("area", "and", MinArea, MaxArea);

            return result;
        }

        public static HImage GetImageChannel(HImage ImageIn, ImageChannel Channel)
        {
            if (ImageIn == null)
            {
                return null;
            }
            HImage ImageOut;
            if (Channel == 0)
            {
                ImageOut = ImageIn.Rgb1ToGray();
            }
            else
            {
                HTuple channels = ImageIn.CountChannels();
                if (channels > (int)Channel)
                    ImageOut = ImageIn.AccessChannel((int)Channel);
                else
                {
                    ImageOut = ImageIn.AccessChannel(channels);
                }
            }
            return ImageOut;
        }
    }
}
