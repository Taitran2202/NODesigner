using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob","Print Inspection")]
    public class PrintInspectionNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
           
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            
        }
        static PrintInspectionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<PrintInspectionNode>));
        }
        public PrintInspectionOptions Options { get; set; } = new PrintInspectionOptions();
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<int> ThresholdInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> HomInput { get; }

        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }

        #region Properties
        [HMIProperty("Open Print Inspection Editor")]
        public IReactiveCommand OpenEditor
        {
          get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }         
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    PrintInspectionWindow wd = new PrintInspectionWindow(this);
                    wd.ShowDialog();
                    break;
            }
        }
        public override void Run(object context)
        {
            var result = RunInside(ImageInput.Value, HomInput.Value,context as InspectionContext);            
            RegionOutput.OnNext(result);
        }
        public PrintInspectionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Print Inspection";
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
                PortType = "HRegion"
            };
            this.Outputs.Add(RegionOutput);

            ThresholdInput = new ValueNodeInputViewModel<int>()
            {
                Name = "Threshold",
                PortType = "int",
                Editor= new Editors.IntegerValueEditorViewModel()
            };
            this.Inputs.Add(ThresholdInput);

        }
        private HImage grayRef,imageRef1,imageRef2,imageRef3;
        public bool IsPrepared = false;
        public void Prepare()
        {    
            if(Options.ReferenceImage != null)
            {
                grayRef = Options.ReferenceImage.Rgb1ToGray();
                var channels = Options.ReferenceImage.CountChannels();
                if (channels == 3)
                {
                    TransformColorSpace(Options.ReferenceImage, Options.InspectionColorSpace, out imageRef1, out imageRef2, out imageRef3);
                }
                else
                {
                    imageRef1 = imageRef2= imageRef3= Options.ReferenceImage;
                }
                
            }

        }
        //int count = 0;
        public HRegion RunInside(HImage inputImage, HHomMat2D hom,InspectionContext e)
        {          
            
            if (Options != null)
            {
                if (hom == null)
                {
                    hom = new HHomMat2D();
                }
                if (Options.ReferenceImage!=null)
                {
                    if (!IsPrepared)
                    {
                        Prepare();
                        IsPrepared = true;
                    }
                    var rect2trans = AffineTranRect2(Options.InspectionRegion,hom);
                    var paddingL1 = Options.Scale * rect2trans.length1;
                    var paddingL2 = Options.Scale * rect2trans.length2;
                    rect2trans.length1 = paddingL1+rect2trans.length1;
                    rect2trans.length2 = paddingL2 + rect2trans.length2;
                    HRegion tranregion = new HRegion();
                    tranregion.GenRectangle2(rect2trans.row,rect2trans.col,rect2trans.phi,rect2trans.length1,rect2trans.length2);
                    
                    
                    Options.ReferenceImage.GetImageSize(out int w, out int h);
                    var ImageCroped=CropRect2(inputImage, rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length1, rect2trans.length2).ZoomImageSize(w,h,"constant");
                    
                    //e.inspection_result.AddDisplay(ImageCroped, "red");
                    var vector_field = grayRef.OpticalFlowMg(ImageCroped.Rgb1ToGray(), Options.Algorithm.ToString(),Options.SmoothingSigma, Options.IntergrationSigma, Options.FlowSmoothness, 8, new HTuple("default_parameters", "warp_zoom_factor"), new HTuple("fast", Options.Accuracy));
                    HImage imagediff;
                    if (ImageCroped.CountChannels() == 3)
                    {
                        var image1 = ImageCroped.Decompose3(out HImage image2, out HImage image3);
                        var image1Unwraped = image1.UnwarpImageVectorField(vector_field);
                        var image2Unwraped = image2.UnwarpImageVectorField(vector_field);
                        var image3Unwraped = image3.UnwarpImageVectorField(vector_field);
                        var image1color = image1Unwraped.TransFromRgb(image2Unwraped, image3Unwraped, out HImage image2color, out HImage image3color, Options.InspectionColorSpace.ToString());
                        //ransformColorSpace(imageUnwarp, Options.InspectionColorSpace, out HImage image1, out HImage image2, out HImage image3);
                        //var imagetest = image1Unwraped.Compose3(image2Unwraped, image3Unwraped);
                        //imagetest.WriteImage("png", 0, "C:/src/anormaly4/data/adidas_aligned/bad/" + count.ToString());
                        //count++;
                        imagediff = CompareImage3(image1color, image2color, image3color);
                    }
                    else
                    {
                        var imageUnwraped = ImageCroped.UnwarpImageVectorField(vector_field);
                        imagediff = imageRef1.AbsDiffImage(imageUnwraped,1.0);
                    }
                    
                    if (Options.ReferenceMask != null)
                    {
                        imagediff=imagediff.ReduceDomain(Options.ReferenceMask.Region);
                    }
                    HHomMat2D hHomMat2D = new HHomMat2D();
                    hHomMat2D.VectorAngleToRigid(rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length2, rect2trans.length1, 0);
                    hHomMat2D = hHomMat2D.HomMat2dInvert();
                    var defectRegion= imagediff.Threshold(ThresholdInput.Value, 255.0).AffineTransRegion(hHomMat2D, "constant");
                    if (ShowDisplay)
                    {
                        e.inspection_result.AddRegion(Options.ReferenceMask.Region.AffineTransRegion(hHomMat2D,"constant"), "blue");
                        e.inspection_result.AddRegion(GenElip(defectRegion.ClosingCircle(2.5).Connection().SelectShape("area","and",10,double.MaxValue)), "red");
                    }
                    return defectRegion;
                }
            }
            HRegion result = new HRegion();
            result.GenEmptyRegion();
            return result;
        }
        public HRegion GenElip(HRegion region)
        {
            HRegion region_elip = new HRegion();
            region_elip.GenEmptyRegion();
            for (int i = 0; i < region.CountObj(); i++)
            {
                double row, col, ra, rb, phi;
                region[i + 1].AreaCenter(out row, out col);
                ra = region[i + 1].EllipticAxis(out rb, out phi);
                HRegion cont = new HRegion();
                cont.GenEllipse(row, col, phi, ra + 5, rb + 5);
                region_elip = region_elip.ConcatObj(cont);
            }
            return region_elip;
        }
        public HImage CompareImage3(HImage image1,HImage image2,HImage image3)
        {           
            var diff1=imageRef1.AbsDiffImage(image1, 1.0/3);
            var diff2 = imageRef2.AbsDiffImage(image2, 1.0 / 3);
            var diff3 = imageRef3.AbsDiffImage(image3, 1.0 / 3);
            var totaldiff = diff1 + diff2 + diff3;
            return totaldiff;
        }
        
        public static Rect2 AffineTranRect2(Rect2 rect2,HHomMat2D fixture)
        {
            fixture.AffineTransPixel(rect2.row, rect2.col, out double rowtrans, out double coltrans);
            return new Rect2() { row = rowtrans, col = coltrans, phi = rect2.phi + Math.Atan(-fixture[1].D / fixture[0].D), length1 = rect2.length1, length2 = rect2.length2 };
        }
        public void TransformColorSpace(HImage image, ColorSpace colorSpace, out HImage image1,out HImage image2,out HImage image3)
        {
            HImage  r= image.Decompose3(out HImage g, out HImage b);
            image1= r.TransFromRgb(g,b,out image2,out image3,colorSpace.ToString());
        }
        private HImage CropRect2(HImage input,double row, double col, double phi, double length1, double length2)
        {
            HHomMat2D hHomMat2D = new HHomMat2D();
            hHomMat2D.VectorAngleToRigid(row, col, phi, length2, length1,0);
            //hHomMat2D = hHomMat2D.HomMat2dScaleLocal(scale, scale);
            var referenceImage = hHomMat2D.AffineTransImageSize(input, "constant", (int)(length1 * 2), (int)(length2 * 2));
            return referenceImage;
        }
    }
    public enum ColorSpace
    {
        rgb,yiq,yuv,argyb, ciexyz, hls, hsi, hsv, ihs, cielab, cielchab, cieluv, cielchuv, i1i2i3, ciexyz2, ciexyz3, ciexyz4, lms
    }
    public enum OpticalFlowAlgorithm
    {
        fdrig,ddraw,clg
    }
    public class PrintInspectionOptions:IHalconDeserializable
    {
        [Category("Optical Flow")]
        public double Accuracy { get; set; } = 0.5;
        [Category("Optical Flow")]
        public OpticalFlowAlgorithm Algorithm {  get; set; }
        [Category("Optical Flow")]
        public double SmoothingSigma { get; set; } = 0.8;
        [Category("Optical Flow")]
        public double IntergrationSigma { get; set; }=1;
        [Category("Optical Flow")]
        public double FlowSmoothness { get; set; } = 20.0;
        [Category("Optical Flow")]
        public double GradientConstancy { get; set; } = 5;
        [Category("Optical Flow")]
        public string MGParamName { get; set; }
        [Category("Optical Flow")]
        public string MGParamValue { get; set; }
        [Category("Comparing")]
        public ColorSpace InspectionColorSpace { get; set; }
        [Category("Comparing")]
        public HImage ReferenceImage { get; set; }
        [Category("Comparing")]
        public CollectionOfregion ReferenceMask { get; set; } =new CollectionOfregion();
        [Category("Comparing")]
        public Rect2 InspectionRegion { get;set; }
        [Category("Comparing")]
        public double Scale { get; set; }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file,this);
        }
    }

}
