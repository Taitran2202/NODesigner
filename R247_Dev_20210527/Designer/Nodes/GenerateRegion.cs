using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Create Region", Icon: "Designer/icons/icons8-picture-in-picture-96.png")]
    public class GenerateRegion : BaseNode
    {
        static GenerateRegion()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<GenerateRegion>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            

        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            

            
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }

        [HMIProperty("Edit region")]
        public IReactiveCommand EditRegion
        {
            get
            {
                return  ReactiveCommand.Create((Control sender) =>
                {
                    OnCommand("editor",sender);
                });
            }
        }
        public CollectionOfregion regions { get; set; } = new CollectionOfregion();
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    var imageInput = NOVisionDesigner.Designer.Extensions.Functions.GetNoneEmptyHImage(ImageInput);
                    HImage image;
                    if (imageInput == null)
                    {
                        image = new HImage("byte", 512, 512);
                        image.OverpaintRegion(image, 128.0, "fill");
                    }
                    else
                    {
                        image = imageInput;
                    }
                    if (image != null)
                    {
                        if (image.IsInitialized())
                        {
                            if (regions == null)
                            {
                                regions = new CollectionOfregion();
                            }
                            WindowRegionWindowInteractive wd = new WindowRegionWindowInteractive(image, regions, FixtureInput.Value);
                            wd.Owner = Window.GetWindow(sender);
                            wd.Show();
                        }
                        

                    }
                    break;
            }
        }
        public override void Run(object context)
        {

            //RegionOutput.OnNext(new HRegion(100, 100, 200.0, 200));
            RegionOutput.OnNext(RunInside(regions, FixtureInput.Value, context as InspectionContext));
        }

        public GenerateRegion(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Create Region";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"

            };
            this.Inputs.Add(ImageInput);
            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
            };
            this.Inputs.Add(FixtureInput);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };     
            this.Outputs.Add(RegionOutput);
            
        }
        public HRegion RunInside(CollectionOfregion regions , HHomMat2D fixture, InspectionContext e)
        {
            if (regions != null)
            {
                HRegion trans_region;
                if (fixture != null)
                {

                    trans_region = regions.Region.AffineTransRegion(fixture, "nearest_neighbor");


                }
                else
                {
                    trans_region = regions.Region;
                }
                if (ShowDisplay)
                {
                    e.inspection_result.AddRegion(trans_region, regions.Color);
                }
                
                return trans_region;
            }
            else
            {
                return null;
            }    
          
        }
    }
}
