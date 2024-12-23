using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NOVisionDesigner.Designer.Editors;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.IO;
using OpticalFlowCudaCV;
using NumSharp;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using NOVisionDesigner.Designer.Extensions;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Blob", "Deep PrintInspection")]
    public class DeepPrintInspectionNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
           
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            
        }
        static DeepPrintInspectionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DeepPrintInspectionNode>));
        }
        public DeepPrintInspectionOptions Options { get; set; } = new DeepPrintInspectionOptions();

        public DeepPrintTrainOption TrainConfig { get; set; } 
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<double> ThresholdInput { get; }
        public ValueNodeOutputViewModel<double> AnomalyScore { get; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; }
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }

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
                    DeepPrintInspectionWindow wd = new DeepPrintInspectionWindow(this);
                    wd.ShowDialog();
                    break;
            }
        }
        public override void OnInitialize()
        {
            runtime = new ONNXAnomalyV3(ModelDir);
            //runtime.LoadOnnx(ResultDir);
        }
        public string ImageDir,GoodDir,BadDir,GoodAlignDir,BadAlignDir,ResultDir,ModelDir;
        public ONNXAnomalyV3 runtime;
        public void CreateDefaultDir(string dir)
        {
            ImageDir = System.IO.Path.Combine(dir, "images");
            GoodDir = System.IO.Path.Combine(dir, "images","good");
            BadDir = System.IO.Path.Combine(dir, "images", "bad");
            GoodAlignDir = System.IO.Path.Combine(dir, "images", "good_align");
            BadAlignDir = System.IO.Path.Combine(dir, "images", "bad_align");           
            ModelDir = System.IO.Path.Combine(dir, "model");
            ResultDir = System.IO.Path.Combine(dir,"model", "result");
            if (!System.IO.Directory.Exists(ImageDir))
            {
                System.IO.Directory.CreateDirectory(ImageDir);
            }
            if (!System.IO.Directory.Exists(GoodDir))
            {
                System.IO.Directory.CreateDirectory(GoodDir);
            }

            if (!System.IO.Directory.Exists(BadDir))
            {
                System.IO.Directory.CreateDirectory(BadDir);
            }
            if (!System.IO.Directory.Exists(GoodAlignDir))
            {
                System.IO.Directory.CreateDirectory(GoodAlignDir);
            }
            if (!System.IO.Directory.Exists(BadAlignDir))
            {
                System.IO.Directory.CreateDirectory(BadAlignDir);
            }
            if (!System.IO.Directory.Exists(ResultDir))
            {
                System.IO.Directory.CreateDirectory(ResultDir);
            }
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }

        }
        public HHomMat2D RunPatternNode(HImage image)
        {
            if (!Options.Parameters.TrainParam.IsTrained) { return null; }
            if (Options.Model == null) return null;
            image = image.Rgb1ToGray();
            double angleStart = Math.PI * Options.Parameters.RuntimeParam.LowerAngle / 180;
            double angleExtent = Math.PI * (Options.Parameters.RuntimeParam.UpperAngle - Options.Parameters.TrainParam.LowerAngle) / 180;
            Options.Model.FindNccModel(image.ReduceDomain(Options.Parameters.RuntimeParam.SearchRegion), angleStart, angleExtent, Options.Parameters.RuntimeParam.MinScore, Options.Parameters.RuntimeParam.NumMatches, Options.Parameters.RuntimeParam.MaxOverlap,
               "true", Options.Parameters.RuntimeParam.NumLevels, out HTuple row, out HTuple col, out HTuple angle, out HTuple score); ;
            if (row.Length == 0)
            {
                Console.WriteLine("Can't find pattern");
                return null;
            }
            HHomMat2D hom = new HHomMat2D();
            hom.VectorAngleToRigid(Options.Parameters.TrainParam.OriginalRow, Options.Parameters.TrainParam.OriginalCol, 0.0, row.D, col.D, angle.D);
            return hom;
        }
        public void Record()
        {
            var image = ImageInput.Value.Clone();
            var filename = Functions.RandomFileName(GoodDir);
            image.WriteImage("bmp", 0, filename +".bmp") ;
        }
        public override void Run(object context)
        {
            if(RecordMode.Value == ToolRecordMode.RecordOnly)
            {
                Record();
                return;
            }
            if (RecordMode.Value == ToolRecordMode.RecordAndRun)
            {
                Record();
            }
            var result = RunInside(ImageInput.Value, ThresholdInput.Value, out double score, context as InspectionContext);            
            RegionOutput.OnNext(result);
            AnomalyScore.OnNext(Math.Round(score,3));
        }
        public DeepPrintInspectionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Deep Print Inspection";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);


            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "HRegion"
            };
            this.Outputs.Add(RegionOutput);

            ThresholdInput = new ValueNodeInputViewModel<double>()
            {
                Name = "Threshold",
                PortType = "double",
                Editor= new Editors.DoubleValueEditorViewModel(defaultValue:1,defaultStep:0.1)
            };
            this.Inputs.Add(ThresholdInput);
            AnomalyScore = new ValueNodeOutputViewModel<double>()
            {
                Name = "Score",
                PortType = "double",
                Editor = new DefaultOutputValueEditorViewModel<double>()

            };
            this.Outputs.Add(AnomalyScore);
            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);

            CreateDefaultDir(Dir);

            TrainConfig = DeepPrintTrainOption.Create(this);
        }
        private HImage grayRef,grayRefFloat,imageRef1,imageRef2,imageRef3;
        OpticalFlowCudaCV.OpticalFlowBrox opticalFlow;
        
        //OpticalflowProvider opticalFlow;
        //private Mat grayRefMat;
        public bool IsPrepared = false;
        public void Prepare()
        {    
            if(Options.ReferenceImage != null)
            {
                grayRef = Options.OpticalFlowImage.Rgb1ToGray();
                //var ptr = grayRef.GetImagePointer1(out string type, out int w, out int h);
                //grayRefMat = new Mat(h, w, MatType.CV_8UC1, ptr);
                grayRefFloat = Options.ReferenceImage.ConvertImageType("float");
                TransformColorSpace(grayRefFloat, Options.InspectionColorSpace, out imageRef1, out imageRef2, out imageRef3);
            }
            
            

        }
        //int count = 0;
        public HRegion RunInside(HImage inputImage,double Threshold, out double score, InspectionContext e)
        {
           
            if (runtime.State == ModelState.Unloaded)
            {
                runtime.LoadRecipe();
            }
            if (runtime.State == ModelState.Error)
            {   
                score = 0;
                return null;
            }
            if (runtime.State == ModelState.Loaded)
            {
                if (Options != null)
                {
                    
                    if (Options.ReferenceImage != null)
                    {
                        if (!IsPrepared)
                        {
                            Prepare();
                            IsPrepared = true;
                        }
                        var hom = RunPatternNode(inputImage);
                        var rect2trans = AffineTranRect2(Options.InspectionRegion, hom);
                        var paddingL1 = Options.Scale * rect2trans.length1;
                        var paddingL2 = Options.Scale * rect2trans.length2;
                        rect2trans.length1 = paddingL1 + rect2trans.length1;
                        rect2trans.length2 = paddingL2 + rect2trans.length2;
                        HRegion tranregion = new HRegion();
                        tranregion.GenRectangle2(rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length1, rect2trans.length2);


                        Options.OpticalFlowImage.GetImageSize(out int w, out int h);
                        var ImageCroped = CropRect2(inputImage, rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length1, rect2trans.length2).ZoomImageSize(w, h, "constant");

                        //e.inspection_result.AddDisplay(ImageCroped, "red");
                        HImage image1color, image2color, image3color;
                        RunOpticalFlow(ImageCroped, out image1color, out image2color, out image3color);
                        //ransformColorSpace(imageUnwarp, Options.InspectionColorSpace, out HImage image1, out HImage image2, out HImage image3);
                        //var imagetest = image1Unwraped.Compose3(image2Unwraped, image3Unwraped);
                        //image1color.WriteImage("png", 0, "C:/1.png");
                        //count++;
                       
                        var imagediff = RunDeepCompare(image1color, image2color, image3color, out score);

                        HHomMat2D hHomMat2D = new HHomMat2D();
                        hHomMat2D.VectorAngleToRigid(rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length2, rect2trans.length1, 0);
                        hHomMat2D = hHomMat2D.HomMat2dInvert(); 
                        var regionDiff = imagediff.Threshold(Threshold, double.MaxValue).AffineTransRegion(hHomMat2D, "constant");

                        regionDiff = regionDiff.ClosingCircle(3.5).Connection().SelectShape("area", "and",1000,9999999);

                        if (ShowDisplay )
                        {
                            e.inspection_result.AddRegion(Options.ReferenceMask.Region.AffineTransRegion(hHomMat2D, "constant"), "blue");
                            e.inspection_result.AddRegion(regionDiff, "red");
                        }

                        return regionDiff;
                    }
                }
            }
            HRegion result = new HRegion();
            result.GenEmptyRegion();
            score = 0;
            return result;
        }

        private void HalconOpticalFlow(HImage ImageCroped, out HImage image1color, out HImage image2color, out HImage image3color)
        {
            var vector_field = grayRef.OpticalFlowMg(ImageCroped.Rgb1ToGray(), Options.Algorithm.ToString(), Options.SmoothingSigma, Options.IntergrationSigma, Options.FlowSmoothness, 8, new HTuple("default_parameters", "warp_zoom_factor"), new HTuple("fast", Options.Accuracy));
            //vector_field.GetImageSize(out int tw, out int th);
            grayRef.WriteImage("bmp", 0, "D:\\test1.bmp");
            ImageCroped.WriteImage("bmp", 0, "D:\\test2.bmp");



            var image1 = ImageCroped.Decompose3(out HImage image2, out HImage image3);
            var image1Unwraped = image1.UnwarpImageVectorField(vector_field);
            var image2Unwraped = image2.UnwarpImageVectorField(vector_field);
            var image3Unwraped = image3.UnwarpImageVectorField(vector_field);
            //image1color = image1Unwraped.TransFromRgb(image2Unwraped, image3Unwraped, out image2color, out image3color, Options.InspectionColorSpace.ToString());
            if (Options.InspectionColorSpace != ColorSpace.rgb)
            {
                image1color = image1Unwraped.TransFromRgb(image2Unwraped, image3Unwraped, out image2color, out image3color, Options.InspectionColorSpace.ToString());
            }
            else
            {
                image1color = image1Unwraped;
                image2color = image2Unwraped;
                image3color = image3Unwraped;
            }
        }

        public HRegion RunPrintCheck(HImage inputImage, HHomMat2D transform, InspectionContext e)
        {

            if (Options != null)
            {
                if (transform == null)
                {
                    transform = new HHomMat2D();
                }
                if (Options.ReferenceImage != null)
                {
                    if (!IsPrepared)
                    {
                        Prepare();
                        IsPrepared = true;
                    }
                    Rect2 rect2trans;
                    HImage image1color, image2color, image3color;
                    FindAlignedImage(inputImage, transform, out rect2trans, out image1color, out image2color, out image3color);
                    //ransformColorSpace(imageUnwarp, Options.InspectionColorSpace, out HImage image1, out HImage image2, out HImage image3);
                    //var imagetest = image1Unwraped.Compose3(image2Unwraped, image3Unwraped);
                    //imagetest.WriteImage("png", 0, "C:/src/anormaly4/data/adidas_aligned/bad/" + count.ToString());
                    //count++;
                    var imagediff = RunDeepCompare(image1color, image2color, image3color, out double score);
                    HHomMat2D hHomMat2D = new HHomMat2D();
                    hHomMat2D.VectorAngleToRigid(rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length2, rect2trans.length1, 0);
                    hHomMat2D = hHomMat2D.HomMat2dInvert();
                    if (ShowDisplay)
                    {
                        e.inspection_result.AddRegion(Options.ReferenceMask.Region.AffineTransRegion(hHomMat2D, "constant"), "blue");
                    }
                    return imagediff.Threshold(ThresholdInput.Value, 255.0).AffineTransRegion(hHomMat2D, "constant");
                }
            }
            HRegion result = new HRegion();
            result.GenEmptyRegion();
            return result;
        }

        public void FindAlignedImage(HImage inputImage, HHomMat2D hom, out Rect2 rect2trans, out HImage image1color, out HImage image2color, out HImage image3color)
        {
            if (Options.OpticalFlowImage != null)
            {
                if (!IsPrepared)
                {
                    Prepare();
                    IsPrepared = true;
                }
            }
            rect2trans = AffineTranRect2(Options.InspectionRegion, hom);
            var paddingL1 = Options.Scale * rect2trans.length1;
            var paddingL2 = Options.Scale * rect2trans.length2;
            rect2trans.length1 = paddingL1 + rect2trans.length1;
            rect2trans.length2 = paddingL2 + rect2trans.length2;
            HRegion tranregion = new HRegion();
            tranregion.GenRectangle2(rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length1, rect2trans.length2);


            Options.OpticalFlowImage.GetImageSize(out int w, out int h);
            var ImageCroped = CropRect2(inputImage, rect2trans.row, rect2trans.col, rect2trans.phi, rect2trans.length1, rect2trans.length2).ZoomImageSize(w, h, "constant");

            //e.inspection_result.AddDisplay(ImageCroped, "red");
            RunOpticalFlow(ImageCroped, out image1color, out image2color, out image3color);
            
        }
        public void RunOpticalFlow(HImage input, out HImage output1, out HImage output2, out HImage output3)
        {
            if(Options.OpticalflowProvider== OpticalflowProvider.Cuda)
            {
                CUDAOpticalFlow(input, out output1, out output2, out output3);
            }
            else
            {
                HalconOpticalFlow(input,out output1,out output2,out output3);
            }
            
        }
        public void CUDAOpticalFlow(HImage input,out HImage output1,out HImage output2,out HImage output3)
        {
            //opticalFlow = new FarnebackOpticalFlowGPU(winSize: 20, polyN: 7, polySigma: 1.5);
            if (opticalFlow == null)
            {
                opticalFlow = new OpticalFlowBrox(alpha:Options.SmoothingSigma,gamma:Options.GradientConstancy,scale_factor:0.5,inner_iterations:10);
            }
            HImage im1 = input.Decompose3(out HImage im2, out HImage im3);
            var grayInput = input.Rgb1ToGray();
            var ptr= grayInput.GetImagePointer1(out string type, out int w, out int h);
            var ptrImgRef = grayRef.GetImagePointer1(out string _, out int _, out int _);
            var start = DateTime.Now;
            var map = opticalFlow.Calc(ptrImgRef, ptr, w, h);
            Console.WriteLine((DateTime.Now - start).TotalMilliseconds);
            output1 = new HImage("byte", w, h);
            output2 = new HImage("byte", w, h);
            output3 = new HImage("byte", w, h);
            var ptrout1 = output1.GetImagePointer1(out string _, out int _, out int _);
            var ptrout2 = output2.GetImagePointer1(out string _, out int _, out int _);
            var ptrout3 = output3.GetImagePointer1(out string _, out int _, out int _);
            opticalFlow.Remap(map, im1.GetImagePointer1(out string _, out int _, out int _) , ptrout1, w, h);
            opticalFlow.Remap(map, im2.GetImagePointer1(out string _, out int _, out int _), ptrout2, w, h);
            opticalFlow.Remap(map, im3.GetImagePointer1(out string _, out int _, out int _), ptrout3, w, h);
            if(Options.InspectionColorSpace != ColorSpace.rgb)
            {
                output1 = output1.TransFromRgb(output2, output3, out output2, out output3, Options.InspectionColorSpace.ToString());
            }
            
        }

        public HImage RunDeepCompare(HImage image1color, HImage image2color, HImage image3color, out double score)
        {
            //HImage image = imageInput.ConvertImageType("float") / 255.0;
            //HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            //image1 = (image1 - 0.485) / 0.229;
            //image2 = (image2 - 0.456) / 0.224;
            //image3 = (image3 - 0.406) / 0.225;

            var imdiff1 = (image1color.ConvertImageType("float") - imageRef1)/(255*0.229);
            var imdiff2 = (image2color.ConvertImageType("float") - imageRef2) / (255 * 0.224);
            var imdiff3 = (image3color.ConvertImageType("float") - imageRef3) / (255 * 0.225);
            var imagediff = imdiff1.Compose3(imdiff2, imdiff3);
            imagediff.GetImageSize(out int w, out int h);
            HRegion diffrg = new HRegion(0, 0.0, h, w);
            var subRg = diffrg.Difference(Options.ReferenceMask.Region);
            imagediff.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
            //(imagediff).ConvertImageType("byte").WriteImage("bmp", 0, "D:/1.bmp");
            
            var deepdiff= runtime.Infer(imagediff,out score,false);
            Console.WriteLine(score);

            if (Options.ReferenceMask != null)
            {
                deepdiff = deepdiff.ReduceDomain(Options.ReferenceMask.Region);
            }
            return deepdiff;
            
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
            if (colorSpace != ColorSpace.rgb)
            {
                HImage r = image.Decompose3(out HImage g, out HImage b);
                image1 = r.TransFromRgb(g, b, out image2, out image3, colorSpace.ToString());
            }
            else
            {
                image1 = image.Decompose3(out image2, out image3);
            }
            
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
    public class DeepPrintInspectionOptions : IHalconDeserializable
    {
        [Category("Optical Flow")]
        public double Accuracy { get; set; } = 0.5;
        [Category("Optical Flow")]
        public OpticalFlowAlgorithm Algorithm { get; set; }
        [Category("Optical Flow")]
        public double SmoothingSigma { get; set; } = 0.8;
        [Category("Optical Flow")]
        public double IntergrationSigma { get; set; } = 1;
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
        public HImage OpticalFlowImage { get; set; }
        [Category("Comparing")]
        public CollectionOfregion ReferenceMask { get; set; } = new CollectionOfregion();
        [Category("Comparing")]
        public Rect2 InspectionRegion { get; set; }
        [Category("Comparing")]
        public double Scale { get; set; }
        public OpticalflowProvider OpticalflowProvider { get; set; } = OpticalflowProvider.Halcon;
        public HNCCModel Model { get; set; } = null;
        public PatternParameters Parameters { get; set; } = new PatternParameters();

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class DeepPrintTrainOption
    {
        public string ImageDir { get; set; }
        public string ModelDir { get; set; }
        public string GoodDir { get; set; }
        public string BadDir { get; set; }
        public string NormalDir { get; set; }
        public string AnomalyDir { get; set; }
        public string ResultDir { get; set; }
        public int Epoch { get; set; } = 20;
        [JsonConverter(typeof(StringEnumConverter))]
        public AnomalibModelName ModelName { get; set; } = AnomalibModelName.fastflow;
        public string ConfigDir;
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this,Formatting.Indented));
        }
        public static DeepPrintTrainOption Create(DeepPrintInspectionNode node)
        {
            var ConfigDir = System.IO.Path.Combine(node.Dir, "TrainConfig.txt");
            DeepPrintTrainOption option;
            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<DeepPrintTrainOption>(json);
                option= config;
            }
            else
            {
                option= new DeepPrintTrainOption();
            }
            option.ApplyRelativeDir(node);
            option.ConfigDir = ConfigDir;
            return option;

        }
        private void ApplyRelativeDir(DeepPrintInspectionNode node)
        {
            GoodDir = node.GoodDir;
            BadDir = node.BadDir;
            NormalDir = node.GoodAlignDir;
            AnomalyDir = node.BadAlignDir;
            ResultDir = node.ResultDir;
            ModelDir = node.ModelDir;
            ImageDir = node.ImageDir;
        }
        public DeepPrintTrainOption()
        {

        }
    }
    public enum AnomalibModelName
    {
        patchcore,fastflow,padim
    }
    public enum OpticalflowProvider
    {
        Halcon,Cuda
    }
    
}
