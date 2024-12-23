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
//using System.Text;
//using System.Threading.Tasks;
//using Tensorflow;
//using Tensorflow.Keras.Layers;
//using Tensorflow.Util;
//using static Tensorflow.KerasApi;
//using static Tensorflow.Binding;
using System.IO;
//using System.Drawing;
//using Tensorflow.Keras.Optimizers;
//using Tensorflow.Keras.ArgsDefinition;
//using Tensorflow.Keras.Engine;
//using System.Windows;
//using System.Threading;
//using System.Reactive.Concurrency;
using NOVisionDesigner.Designer.Misc;
using Microsoft.ML.OnnxRuntime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Windows.Controls;
using System.Windows;
using NOVisionDesigner.Designer.Extensions;
using System.ComponentModel;
using NOVisionDesigner.Designer.Windows;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "LDC Edge Detection")]
    public class LDCEdgeDetection : BaseNode
    {
        static LDCEdgeDetection()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<LDCEdgeDetection>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        public ValueNodeInputViewModel<HImage> ImageIn { get; }
        public ValueNodeInputViewModel<HRegion> RegionIn { get; }
        public ValueNodeOutputViewModel<HImage> ImageOut { get; }
        public LDCTrainConfig TrainConfig;
        public ONNXLDCEdgeDetection Runtime { get; set; }
        [HMIProperty("Model Loader")]
        public IReactiveCommand ModelLoader
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    ONNXModelLoaderWindow wd = new ONNXModelLoaderWindow(Runtime);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                }
            );
            }
        }
        [HMIProperty("Background Fill Value")]
        public int FillValue { get; set; } = 0;
        //public string ImageDir, AnnotationDir, ModelDir, TrainConfigDir;
        [HMIProperty("Edge Detection Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public HImage Detect(HImage imageIn, HRegion regionIn)
        {
            if (imageIn != null)
            {
                if (Runtime.State == ModelState.NotFound)
                {
                    return null;
                }
                if (Runtime.State == ModelState.Unloaded)
                {
                    Runtime.LoadRecipe();
                }
                if (Runtime.State == ModelState.Loaded)
                {
                    if (regionIn != null)
                    {
                        if (regionIn.CountObj() > 0 & regionIn.Area > 0)
                        {                           
                            imageIn.GetImageSize(out int w, out int h);
                            var imageCroped = Functions.CropImageWithRegionTranslate(imageIn, regionIn, FillValue);
                            regionIn.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                            //var imageCroped = input.CropRectangle1(r1, c1, r2, c2);
                            var partSegment = Runtime.Infer(imageCroped);
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D = hHomMat2D.HomMat2dTranslate((double)Math.Max(0, r1), Math.Max(0, c1));
                            partSegment = partSegment.AffineTransImageSize(hHomMat2D, "nearest_neighbor", w, h);
                            return partSegment.ReduceDomain(regionIn);

                            //HRegion ROITranslate = regionIn;
                            //imageIn.GetImageSize(out int w, out int h);
                            //ROITranslate.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                            //var imageCroped = imageIn.CropRectangle1(r1, c1, r2, c2);
                            //var partSegment = Runtime.Infer(imageCroped);
                            //HHomMat2D hHomMat2D = new HHomMat2D();
                            //hHomMat2D = hHomMat2D.HomMat2dTranslate((double)r1, c1);
                            //partSegment = partSegment.AffineTransImageSize(hHomMat2D, "constant", w, h);
                            //return partSegment.ReduceDomain(ROITranslate);

                        }
                        else
                        {
                            return Runtime.Infer(imageIn);
                        }
                    }
                    else
                    {
                        return Runtime.Infer(imageIn);
                    }




                }
            }
            //var region = new HRegion();
            //region.GenEmptyRegion();
            return imageIn;
        }

        public override void Run(object context)
        {
            if (ImageIn.Value == null)
            {
                return;
            }
            var image = Detect(ImageIn.Value, RegionIn.Value); ;
            var IContext = context as InspectionContext;
            if (IContext != null)
            {
                if (image != null & ShowDisplay)
                {
                    IContext.inspection_result.AddDisplay(image, "red");
                    //base.designer.display.HalconWindow.DispObj(region);
                }
            }
            ImageOut.OnNext(image);
        }
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    Windows.LDCEdgeDetectionWindow wd = new Windows.LDCEdgeDetectionWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
            }
        }

        public LDCEdgeDetection(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Edge Detection";
            ImageIn = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image In",
                PortType = "HImage"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(ImageIn);
            RegionIn = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "HRegion",
                Editor = new RegionValueEditorViewModel()
            };
            this.Inputs.Add(RegionIn);
            ImageOut = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image Out",
                PortType = "HImage"
            };
            this.Outputs.Add(ImageOut);
            //TrainConfigDir = System.IO.Path.Combine(Dir, "TrainConfig.txt");
            
            TrainConfig = LDCTrainConfig.Create(Dir);
            //if (!System.IO.File.Exists(TrainConfigDir))
            //{
            //    TrainConfig.Save();
            //}
        }
        public override void OnInitialize()
        {
            Runtime = new ONNXLDCEdgeDetection(TrainConfig.MODEL_DIR);
        }
        public override void OnLoadComplete()
        {
            if (Runtime != null)
            {
                Runtime.LoadRecipe();
                if (Runtime.State == ModelState.Loaded)
                {
                    var ROI = RegionIn.Value;
                    if (ROI != null)
                    {
                        if (ROI.CountObj() > 0 & ROI.Area > 0)
                        {

                            ROI.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                            HImage image = new HImage("byte", c2 - c1, r2 - r1);
                            Runtime.Infer(image);
                        }
                    //    else
                    //    {
                    //        HImage image = new HImage("byte", 2592, 1944);
                    //        Runtime.Infer(image);
                    //    }
                    }
                    //else
                    //{
                    //    HImage image = new HImage("byte", 2592, 1944);
                    //    Runtime.Infer(image);
                    //}

                }
            }

        }
    }
    public class LDCTrainConfig
    {
        public int NUM_WORKERS { get; set; } = 2;
        [JsonConverter(typeof(StringEnumConverter))]
        public LearningRateType LR_TYPE { get; set; } = LearningRateType.Cosine;
        [Description("\"A-B-C\" means learning rate will reduce 10 times after A steps, B steps and C steps.\nAffects only LR_TYPE set as Multistep.")]
        public string LR_STEPS { get; set; } = "10-16";
        public double WD { get; set; } = 1e-4;
        public double lmbda { get; set; } = 1.1;
        public double THRESHOLD { get; set; } = 0.3;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TRAINING_TYPE { get; set; } = TrainResume.Scratch;
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckPointEnum CheckPoint { get; set; } = CheckPointEnum.Latest;
        public double TRAIN_SIZE { get; set; } = 0.9;
        [JsonConverter(typeof(StringEnumConverter))]
        public OptimizerType OPTIMIZER { get; set; } = OptimizerType.adam;
        public double LR_INIT { get; set; } = 5e-3;
        public int INPUT_WIDTH { get; set; } = 512;
        public int INPUT_HEIGHT { get; set; } = 512;
        public int EPOCHS { get; set; }
        public int BATCH_SIZE { get; set; } = 1;
        [Description("Stop training session if model do not improve loss after \"n\" times")]
        public int PATIENCE { get; set; } = 25;
        public string LOG_DIR { get; set; }
        public string TEST_DIR { get; set; }
        public string MODEL_DIR { get; set; } 
        public string ROOT_DIR { get; set; }
        public string MASK_DIR { get; set; }
        public string ANNOTATION_DIR { get; set; }
        public string EvaluationDir { get; set; }
        public string IMAGE_DIR { get; set; }
        public bool EnableAugmentation { get; set; } = false;
        public Albumentations Augmentations { get; set; } = new Albumentations();
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public static LDCTrainConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<LDCTrainConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new LDCTrainConfig(BaseDir);
            }

        }
        private LDCTrainConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();

        }

        private void ApplyRelativeDir(string BaseDir)
        {
            ROOT_DIR = BaseDir;
            LOG_DIR = System.IO.Path.Combine(BaseDir, "logs");
            IMAGE_DIR = System.IO.Path.Combine(BaseDir, "images");
            MODEL_DIR = System.IO.Path.Combine(BaseDir, "data");
            MASK_DIR = System.IO.Path.Combine(BaseDir, "masks");
            ANNOTATION_DIR = System.IO.Path.Combine(BaseDir, "annotations");
            EvaluationDir = System.IO.Path.Combine(BaseDir, "evaluation");
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            this.BaseDir = BaseDir;
            if (!System.IO.Directory.Exists(MODEL_DIR))
            {
                Directory.CreateDirectory(MODEL_DIR);
            }
            if (!System.IO.Directory.Exists(IMAGE_DIR))
            {
                Directory.CreateDirectory(IMAGE_DIR);
            }
            if (!System.IO.Directory.Exists(MASK_DIR))
            {
                Directory.CreateDirectory(MASK_DIR);
            }
            if (!System.IO.Directory.Exists(ANNOTATION_DIR))
            {
                Directory.CreateDirectory(ANNOTATION_DIR);
            }
            if (!System.IO.Directory.Exists(EvaluationDir))
            {
                Directory.CreateDirectory(EvaluationDir);
            }
            if (!System.IO.Directory.Exists(LOG_DIR))
            {
                Directory.CreateDirectory(LOG_DIR);
            }
        }

        private LDCTrainConfig()
        {

        }
    }
    public enum LearningRateType
    {
        Multistep,
        Cosine
    }
    public enum OptimizerType
    {
        adam,
        sgd
    }
    public class ONNXLDCEdgeDetection : ONNXModel,IHalconDeserializable
    {
        public string ModelDir;
        int num_channel = 3;
        public int smallest_div = 16;
        public ONNXLDCEdgeDetection(string ModelDir)
        {
            this.ModelDir = ModelDir;
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
                SessionOptions options = CreateProviderOption(directory);
                //if (Provider == ONNXProvider.TensorRT)
                //{

                //    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                //    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                //      {
                //          // Enable INT8 for QAT models, disable otherwise.
                //          { "trt_fp16_enable", "1" },
                //          { "trt_engine_cache_enable", "1" },
                //          {"trt_engine_cache_path",directory}
                //     });
                //    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                //}
                //else
                //{
                //    int gpuDeviceId = 0; // The GPU device ID to execute on
                //    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //}
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (input_width == -1 | input_height == -1)
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * smallest_div * smallest_div * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, smallest_div, smallest_div })),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
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
        bool is_loading = false;
        public override bool LoadRecipe()
        {
            if (is_loading)
            {
                return false;
            }
            is_loading = true;
            bool result = false;
            try
            {
                result = LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                result = false;
            }
            is_loading = false;
            return result;
        }
        public HImage Infer(HImage imgInput)
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
            imgInput.GetImageSize(out HTuple originalw, out HTuple originalh);
            HImage resized_image = imgInput.ZoomImageSize(input_width, input_height, "bilinear");
            var normalized_image = Normalize(resized_image);
            var array_final = Processing.HalconUtils.HImageToFloatArray(normalized_image, num_channel, out int w, out int h);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[results.Count - 1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;

            var result = new HImage("float", input_width, input_height);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, input_height * input_width);
            //crop if image was padded
            result = (result * 255).ConvertImageType("byte");
            if (input_width != originalw | input_height != originalh)
            {
                result = result.ZoomImageSize(originalw, originalh, "nearest_neighbor");
            }
            resized_image.Dispose();
            normalized_image.Dispose();
            
            return result;

        }
        public HImage Normalize(HImage imageInput)
        {
            var image=imageInput.ConvertImageType("real")/255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }

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