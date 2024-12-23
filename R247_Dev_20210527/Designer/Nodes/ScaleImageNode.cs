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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Image","Scale Image", Icon: "Designer/icons/icons8-picture-in-picture-96.png")]
    public class ScaleImageNode : BaseNode
    {
        static ScaleImageNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ScaleImageNode>));
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
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public bool UseGPU { get; set; } = false;
        public bool UseMultithread { get; set; } = false;
        [HMIProperty("Re Initialize")]
        public IReactiveCommand ReInitialize
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    OnInitialize();
                });
            }
        }
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
        public ObservableCollection<ScaleImage> ListScaleImage { get; set; } = new ObservableCollection<ScaleImage>();
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    var image= Extensions.Functions.GetNoneEmptyHImage(ImageInput);
                    if(image== null)
                    {
                        image = new HImage("byte", 1024, 1024);
                    }
                    if (image != null)
                    {
                        ScaleImageWindow wd = new ScaleImageWindow(ListScaleImage, image, FixtureInput.Value==null?new HHomMat2D():FixtureInput.Value.Clone());
                        wd.Owner = Window.GetWindow(sender);
                        wd.Show();
                    }
                    break;
            }
        }
        public override void Run(object context)
        {

            //RegionOutput.OnNext(new HRegion(100, 100, 200.0, 200));
            ImageOutput.OnNext(RunInside(ImageInput.Value, FixtureInput.Value, context as InspectionContext));
        }

        public ScaleImageNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Scale Image";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "HImage"

            };
            this.Inputs.Add(ImageInput);
            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
            };
            this.Inputs.Add(FixtureInput);

            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image Scaled",
                PortType = "HImage"
            };     
            this.Outputs.Add(ImageOutput);
            
        }
        public override void OnInitialize()
        {
            if (UseGPU)
            {
                try
                {
                    var devices = HComputeDevice.QueryAvailableComputeDevices();
                    if (devices.Length > 0)
                    {
                        HComputeDevice device = new HComputeDevice((int)devices[0]);
                        device.InitComputeDevice("scale_image");
                        device.ActivateComputeDevice();
                    }
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                try
                {
                    var devices = HComputeDevice.QueryAvailableComputeDevices();
                    if (devices.Length > 0)
                    {
                        HComputeDevice device = new HComputeDevice((int)devices[0]);
                        device.InitComputeDevice("scale_image");
                        device.DeactivateComputeDevice();
                    }
                }
                catch (Exception ex)
                {

                }
            }
            
        }
        int scale_w;
        int scale_h;
        int scale_channel;
        string scale_image_type;
        HImage imageMask=null;
        /// <summary>
        /// support for 1 and 3 channels image only
        /// </summary>
        /// <param name="image"></param>
        public void ChangeScale(HImage image)
        {
            image.GetImageSize(out int w, out int h);
            string image_type = image.GetImageType();
            int channels =  image.CountChannels();
            if(scale_h!=h | scale_w != w  | channels!= scale_channel|imageMask==null)
            {
                scale_w = w;
                scale_h = h;
                scale_channel = channels;
                scale_image_type = image_type;
                imageMask = new HImage("float", scale_w, scale_h);
                imageMask=imageMask.GenImageProto(new HTuple(1));
                foreach(var item in ListScaleImage)
                {
                    HImage imagescale = imageMask.ReduceDomain(item.Regions.Region).ScaleImage(item.Multiply, item.Add);
                    imageMask.OverpaintGray(imagescale);
                }

                if (channels == 3)
                {
                    imageMask = imageMask.Compose3(imageMask, imageMask);
                }
                
            }
        }
        public HImage RunInside(HImage image, HHomMat2D fixture, InspectionContext e)
        {
            HImage imageOut=image.CopyImage();
            if (fixture != null)
            {
                if (UseMultithread)
                {
                    Parallel.ForEach(ListScaleImage, (item) =>
                    {
                        var trans_region = item.Regions.Region.AffineTransRegion(fixture, "nearest_neighbor");
                        HImage imagescale = imageOut.ReduceDomain(trans_region).ScaleImage(item.Multiply, item.Add);
                        imageOut.OverpaintGray(imagescale);
                    });
                }
                else
                {
                    foreach (var item in ListScaleImage)
                    {
                        var trans_region = item.Regions.Region.AffineTransRegion(fixture, "nearest_neighbor");
                        HImage imagescale = imageOut.ReduceDomain(trans_region).ScaleImage(item.Multiply, item.Add);
                        imageOut.OverpaintGray(imagescale);
                    }
                }
                
            }
            else
            {
                //var targetType = imageOut.GetImageType();
                //if (targetType == "real1")
                //{
                //    ChangeScale(imageOut);
                //    imageOut = imageOut.MultImage(imageMask, 1.0, 0);
                //}
                //else
                //{
                //    foreach (var item in ListScaleImage)
                //    {
                //        HImage imagescale = imageOut.ReduceDomain(item.Regions.Region).ScaleImage(item.Multiply, item.Add);
                //        imageOut.OverpaintGray(imagescale);
                //    }
                //}
                if (UseMultithread)
                {
                    Parallel.ForEach(ListScaleImage, (item) =>
                    {
                        HImage imagescale = imageOut.ReduceDomain(item.Regions.Region).ScaleImage(item.Multiply, item.Add);
                        imageOut.OverpaintGray(imagescale);
                    });
                }
                else
                {
                    foreach (var item in ListScaleImage)
                    {
                        HImage imagescale = imageOut.ReduceDomain(item.Regions.Region).ScaleImage(item.Multiply, item.Add);
                        imageOut.OverpaintGray(imagescale);
                    }
                }
            }

            if (ShowDisplay)
            {
                e.inspection_result.AddImage(imageOut,0,0);
            }
            return imageOut;

          
        }
    }
    public class ScaleImage:IHalconDeserializable
    {
        public CollectionOfregion Regions { get; set; } = new CollectionOfregion();
        public string Name { get; set; }
        public string DisplayColor { get; set; } = "#00ff00ff";
        public double Multiply { get; set; } = 1;
        public double Add { get; set; } = 0;
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file,this);
        }
    }
}
