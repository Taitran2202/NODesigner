using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Windows;
using System.Threading;
using System.Reactive.Concurrency;
using NOVisionDesigner.Designer.Misc;
using Microsoft.ML.OnnxRuntime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning","Segmentation")]
    public class SegmentationNode : BaseNode
    {
        static SegmentationNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<SegmentationNode>));
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
        public ValueNodeInputViewModel<HHomMat2D> Fixture { get; }
        public ValueNodeInputViewModel<HRegion> ROIInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Threshold { get; }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public UNETTrainConfig TrainConfig;
        public double ImageScale { get; set; } = 1;
        public ONNXUNETSegmentation segmentation { get; set; }
        public string  RootDir;
        [HMIProperty("Segmentation Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        HImage CropImage(HImage image, HRegion region)
        {
            region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
            var imageCroped = image.CropRectangle1(Math.Max(0, row1), Math.Max(0, col1), row2, col2);
            imageCroped.GetImageSize(out int w, out int h);
            HRegion diffrg = new HRegion(0, 0.0, h, w);
            var subRg = diffrg.Difference(region.MoveRegion(-Math.Max(row1, 0), -Math.Max(0, col1)));
            imageCroped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
            return imageCroped;
        }
        public HImage Segment(HImage input,HRegion ROI,HHomMat2D fixture)
        {
            if (input != null)
            {
                if (segmentation.State == ModelState.NotFound)
                {
                    return null;
                }
                if (segmentation.State == ModelState.Unloaded)
                {
                    segmentation.LoadRecipe(segmentation.ModelDir);
                }
                if (segmentation.State == ModelState.Loaded)
                {
                    if (ROI != null)
                    {
                        if(ROI.CountObj() > 0 & ROI.Area>0)
                        {
                            
                            HRegion ROITranslate = ROI;
                            if (fixture != null)
                            {
                                ROITranslate = ROI.AffineTransRegion(fixture, "constant");
                            }
                            input.GetImageSize(out int w, out int h);
                            var imageCroped=CropImage(input, ROITranslate);
                            ROITranslate.SmallestRectangle1(out int r1,out int c1,out int r2,out int c2);
                            //var imageCroped = input.CropRectangle1(r1, c1, r2, c2);
                            var partSegment = segmentation.Infer(imageCroped, TrainConfig.Subsampling);
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D = hHomMat2D.HomMat2dTranslate((double)Math.Max(0,r1),Math.Max(0, c1));
                            partSegment=partSegment.AffineTransImageSize(hHomMat2D, "constant", w, h);
                            return partSegment.ReduceDomain(ROITranslate);
                            
                        }
                        else
                        {
                            return segmentation.Infer(input, TrainConfig.Subsampling);
                        }
                    }
                    else
                    {
                        return segmentation.Infer(input, TrainConfig.Subsampling);
                    }

                    
                    
                    
                }
            }                        
            //var region = new HRegion();
            //region.GenEmptyRegion();
            return input;
        }
        
        public override void Run(object context)
        {
            var image = Segment(ImageInput.Value,ROIInput.Value,Fixture.Value);;
            var IContext = context as InspectionContext;
            if (IContext != null)
            {
                if (image != null & ShowDisplay)
                {
                    IContext.inspection_result.AddDisplay(image, "red");
                    //base.designer.display.HalconWindow.DispObj(region);
                }
            }
            ImageOutput.OnNext(image);
        }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    Windows.SegmentationWindow wd = new Windows.SegmentationWindow(TrainConfig.ImageDir, TrainConfig.AnnotationDir, TrainConfig.SavedModelDir, null,this);
                    wd.ShowDialog();
                    break;
            }
        }

        public SegmentationNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            this.Name = "Segmentation Node";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "ImageInput",
                PortType = "Image"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(ImageInput);
            Fixture = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture",
                Editor = new HHomMat2DValueEditorViewModel()
                //Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(Fixture);
            ROIInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region of Interest",
                PortType = "HRegion",
                Editor = new RegionValueEditorViewModel()
            };
            this.Inputs.Add(ROIInput);
            Threshold = new ValueNodeInputViewModel<int>()
            {
                Name = "Threshold",
                PortType = "Fixture",
                Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(Threshold);

            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "ImageOutput",
                PortType = "HImage"

            };
            this.Outputs.Add(ImageOutput);  
            
            TrainConfig = UNETTrainConfig.Create(RootDir);
        }
        public override void OnInitialize()
        {
            segmentation = new ONNXUNETSegmentation(TrainConfig.SavedModelDir);
        }
        public override void OnLoadComplete()
        {
            if(segmentation != null)
            {
                segmentation.LoadRecipe(segmentation.ModelDir);
                if(segmentation.State == ModelState.Loaded)
                {
                    var ROI = ROIInput.Value;
                    if (ROI != null)
                    {
                        if (ROI.CountObj() > 0 & ROI.Area > 0)
                        {
                           
                            ROI.SmallestRectangle1(out int r1,out int c1,out int r2,out int c2);
                            HImage image = new HImage("byte", c2-c1,r2-r1);
                            segmentation.Infer(image, TrainConfig.Subsampling);
                        }
                        //else
                        //{
                        //    HImage image = new HImage("byte", 2592, 1944);
                        //    segmentation.Infer(image, TrainConfig.Subsampling);
                        //}
                    }
                    //else
                    //{
                    //    HImage image = new HImage("byte", 2592, 1944);
                    //    segmentation.Infer(image, TrainConfig.Subsampling);
                    //}
                    
                }
            }
            
        }
    }
    public enum ImageOrdering
    {
        channels_first, channels_last
    }
    public class UNETTrainConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageOrdering ImageOrdering { get; set; } = ImageOrdering.channels_first;
        public int Subsampling { get; set; } = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public UNETMODEL ModelName { get; set; } = UNETMODEL.mobilenetv2;
        public bool Augmentation { get; set; }
        public double StartLearningRate { get; set; } = 1e-3;
        public double EndLearningRate { get; set; } = 1e-6;
        public int FINETUNE_EPOCHS { get; set; } = 20;
        public int EPOCHS { get; set; } = 100;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public int NumChannels { get; set; } = 3;
        public SegmentAugmentation AugmentationSetting { get; set; } = new SegmentAugmentation();
        public bool UseMosaicImage { get; set; }
        /// <summary>
        /// adam, sgd, rmsprop
        /// </summary>
        public string Optimizer { get; set; } = "adam";
        public string ImageDir { get; set; }
        public int Patience { get; set; } = 20;
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 416;
        public int INPUT_HEIGHT { get; set; } = 416;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingPrecision Precision { get; set; } = TrainingPrecision.float32;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingType TrainningType { get; set; } = TrainingType.Restart;
        public bool EarlyStopping { get; set; } = false;
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public static UNETTrainConfig Create(string BaseDir)
        {

            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                UNETTrainConfig config = null;
                try
                {
                    var json = File.ReadAllText(ConfigDir);
                    config = JsonConvert.DeserializeObject<UNETTrainConfig>(json);
                    
                }catch(Exception ex)
                {
                   
                }
                if (config == null)
                {
                    config = new UNETTrainConfig(BaseDir);
                    config.Save();
                }
                else
                {
                    config.ApplyRelativeDir(BaseDir);
                    config.Save();
                }
               
                return config;
            }
            else
            {
                return new UNETTrainConfig(BaseDir);
            }

        }
        private void ApplyRelativeDir(string BaseDir)
        {
            this.BaseDir = BaseDir;
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            ImageDir = Path.Combine(BaseDir, "images");
            SavedModelDir = Path.Combine(BaseDir, "data");
            AnnotationDir = Path.Combine(BaseDir, "annotations");
            if (!System.IO.Directory.Exists(ImageDir))
            {
                System.IO.Directory.CreateDirectory(ImageDir);
            }

            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                System.IO.Directory.CreateDirectory(AnnotationDir);
            }

            if (!System.IO.Directory.Exists(SavedModelDir))
            {
                System.IO.Directory.CreateDirectory(SavedModelDir);
            }
        }
        private UNETTrainConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();
        }
        private UNETTrainConfig()
        {
            
        }
    }
    public enum UNETMODEL
    {
        mobilenet,mobilenetv2, mobilenetv3large, mobilenetv3small, resnet50,
        efficientnetb0, efficientnetb1, efficientnetb2, efficientnetb3, efficientnetb4, efficientnetb5, efficientnetb6, efficientnetb7,
        resnet101, resnet152
    }
    public enum TrainingPrecision
    {
        float32,float16
    }
    public enum TrainingType
    {
        Continuous,Restart
    }
    public class SegmentAugmentation
    {
        public bool Rotation { get; set; } = true;
        public double RotationRange { get; set; } = 0.2;
        public bool Brightness { get; set; } = true;
        public double BrightnessRange { get; set; } = 0.2;
        public bool HorizontalFlip { get; set; } = true;
        public bool VerticalFlip { get; set; } = true;
    }
    public class ONNXUNETSegmentation : ONNXModel
    {
        public string ModelDir;
        int num_channel = 3;
        public int smallest_div = 16;
        public ONNXUNETSegmentation(string ModelDir)
        {
            this.ModelDir= ModelDir;           
        }
        int input_width, input_height;
        
        InferenceSession ONNXSession;
        string input_name;
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                SessionOptions options=null;
                if (Provider == ONNXProvider.TensorRT)
                {

                    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                      {
                          // Enable INT8 for QAT models, disable otherwise.
                          { "trt_fp16_enable", "1" },
                          { "trt_engine_cache_enable", "1" },
                          {"trt_engine_cache_path",directory}
                     });
                    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                }
                else if (Provider == ONNXProvider.CUDA)
                {
                    int gpuDeviceId = 0; // The GPU device ID to execute on
                    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                }
                //else if(Provider == ONNXProvider.CPU)
                //{
                //    options = SessionOptions.(gpuDeviceId);
                //}
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                if (options != null)
                {
                    ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                }
                else
                {
                    ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"));
                }
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                //ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<byte> t1;
                if (input_width == -1 | input_height == -1)
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>(1 * smallest_div * smallest_div * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<byte>(input_name, t1.Reshape(new int[]{ 1, 3, smallest_div, smallest_div })),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>(1 * input_width * input_height * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<byte>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                
                State = ModelState.Loaded;

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
        public bool LoadRecipe(string directory)
        {
            try
            {
                return LoadOnnx(directory);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public HImage Infer(HImage imgInput,int subsampling)
        {
            if (imgInput.CountChannels() != num_channel)
            {
                if (num_channel == 1)
                {
                    imgInput = imgInput.Rgb1ToGray();
                }
                else
                {
                    imgInput = imgInput.Compose3(imgInput, imgInput);
                }
            }
            HImage imageSampling;
            
            if (subsampling > 1)
            {
                imageSampling = imgInput.ZoomImageFactor(1 / (double)subsampling, 1 / (double)subsampling,"constant");
            }
            else
            {
                imageSampling = imgInput;
            }
            HImage imagePadded = Processing.HalconUtils.PadImage(imageSampling, smallest_div, out int originalw, out int originalh);
            var array_final = Processing.HalconUtils.HImageToByteArray(imagePadded, num_channel, out int input_width, out int input_height);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[results.Count-1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>;
            
            var result = new HImage("byte", input_width, input_height);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, input_height * input_width);
            //crop if image was padded
            if (input_width != originalw | input_height != originalh)
            {
                result = result.ChangeFormat(originalw, originalh);
            }
            if (subsampling > 1)
            {
                result = result.ZoomImageFactor(subsampling, subsampling, "constant");
            }
            //imagePadded.Dispose();
            return result;

        }
    }


}
