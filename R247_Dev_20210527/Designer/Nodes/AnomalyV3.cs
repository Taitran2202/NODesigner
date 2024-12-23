using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using Microsoft.ML.OnnxRuntime;
using System.Linq;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json.Converters;
using NOVisionDesigner.Designer.Extensions;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "Anomaly Detection", Icon: "Designer/icons/icons8-orange-100.png")]
    public class AnomalyV3 : BaseNode
    {
        
        public override void OnLoadComplete()
        {
            //segmentation.ReloadRecipe();
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);    
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }
        #region Properties
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    AnomalyV3Window wd = new AnomalyV3Window(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
                case "import":
                    ImportModelMethod();
                    break;
                case "export":
                    ExportModelMethod();
                    break;
            }

        }
        void ImportModelMethod()
        {
            OpenFileDialog wd = new OpenFileDialog();
            if (wd.ShowDialog() == true)
            {
                foreach (var file in wd.FileNames)
                {
                    if (file.Contains(".onnx"))
                    {
                        System.IO.File.Copy(file, System.IO.Path.Combine(TrainConfig.ModelDir, "model.onnx"), true);
                        Runtime.LoadRecipe();
                    }
                }

            }
        }
        void ExportModelMethod()
        {
            SaveFileDialog wd = new SaveFileDialog
            {
                FileName = "model.onnx"
            };
            if (wd.ShowDialog() == true)
            {
                var modelPath = System.IO.Path.Combine(TrainConfig.ModelDir, "model.onnx");
                System.IO.File.Copy(modelPath, wd.FileName, true);

            }
        }
        [HMIProperty("Anomaly Editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        [HMIProperty("Model Loader")]
        public IReactiveCommand ModelLoader
        {
            get { return ReactiveCommand.Create((Control sender) =>
                {
                    ONNXModelLoaderWindow wd = new ONNXModelLoaderWindow(Runtime);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                }
            ); }
        }
        [HMIProperty("Export Model")]
        public IReactiveCommand ExportModel
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("export", sender)); }
        }
        [HMIProperty("Import Model")]
        public IReactiveCommand ImportModel
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("import", sender)); }
        }
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<double> Threshold { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<double> AnomalyScore { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<HImage> AnomalyMap { get; }
        [HMIProperty]
        public int FillBackground { get; set; } = 0;
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public AnomalyV3Config TrainConfig { get; set; }

        public string RootDir;
        
        //public ONNXAnomalyV3 Runtime { get; set; }
        public ONNXTFFastflow Runtime { get; set; }
        #endregion

        static AnomalyV3()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<AnomalyV3>));
        }

        public AnomalyV3(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Anomaly Detection";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Anomaly Region",
                PortType = "Region",
                Editor = new HObjectOutputValueEditorViewModel<HRegion>()
               
            };
            this.Outputs.Add(RegionOutput);
            AnomalyScore = new ValueNodeOutputViewModel<double>()
            {
                Name = "Anomaly Score",
                PortType = "double",
                Editor = new DefaultOutputValueEditorViewModel<double>()

            };
            this.Outputs.Add(AnomalyScore);
            AnomalyMap = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Anomaly Map",
                PortType = "HImage"
                //Editor = new DefaultOutputValueEditorViewModel<HImage>()

            };
            this.Outputs.Add(AnomalyMap);
            Threshold = new ValueNodeInputViewModel<double>()
            {
                Name = "Threshold",
                PortType = "Double",
                Editor = new DoubleValueEditorViewModel(defaultStep:0.1)
            };
            this.Inputs.Add(Threshold);
            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);

            TrainConfig = AnomalyV3Config.Create(RootDir);
        }
        public override void OnInitialize()
        {
            Runtime = new ONNXTFFastflow(TrainConfig.ModelDir);
            if (RegionInput.Value.Area == 0)
            {
                HImage image = new HImage("byte", 700, 700);
                Segment(image, 0.5, out double _,out HImage _);
            }
            else
            {
                RegionInput.Value.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var image_croped = new HImage("byte",c2-c1,r2-r1);
                image_croped.GetImageSize(out int w, out int h);
                HRegion diffrg = new HRegion(0, 0.0, h, w);
                var subRg = diffrg.Difference(RegionInput.Value.MoveRegion(-r1, -c1));
                image_croped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
                //image_croped.WriteImage("png", 0, "D://image.png");
                Segment(image_croped, 0.5, out double _,out HImage _);
            }
        }
        
        
        public override void Dispose()
        {
            base.Dispose();
            Runtime.Dispose();
        }
        public void Record()
        {
            if (Image.Value != null)
            {
                var image = Image.Value.Clone();
                var filename = Functions.RandomFileName(TrainConfig.NormalDir);
                var region = Functions.GetNoneEmptyRegion(RegionInput.Value);
                var imageCropped = Functions.CropImageWithRegion(image, region, FillBackground);
                imageCropped.WriteImage("png", 0, filename + ".png");
                if (region != null)
                {
                    region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                    row1 = Math.Max(0, row1);
                    col1 = Math.Max(0, col1);
                    var FileNameBase = System.IO.Path.GetFileName(filename);
                    BlobMaskGeneration.SaveMask(FileNameBase + ".png.good", imageCropped, region.MoveRegion(-row1,-col1), TrainConfig.MaskDir);
                }
                
                //RegionInput.Value.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                //var imageCroped = image.CropRectangle1(row1, col1, row2, col2);
                //imageCroped.GetImageSize(out int w, out int h);
                //HRegion diffrg = new HRegion(0, 0.0, h, w);
                //var subRg = diffrg.Difference(RegionInput.Value.MoveRegion(-row1, -col1));
                //imageCroped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
                
            }
            
        }
        public override void Run(object context)
        {
            if (Image.Value == null)
            {
                return;
            }
            if(RecordMode.Value == ToolRecordMode.RecordOnly)
            {
                Record();
                return;
            }
            if (RecordMode.Value == ToolRecordMode.RecordAndRun)
            {
                Record();
            }

            HRegion inspected_region = RegionInput.Value;
            HRegion result_region;
            HImage annomalyMap;
            HImage image = Image.Value;
            double threshold = Threshold.Value;
            double score = 0;
            var IContext = context as InspectionContext;
            if (inspected_region.Area.Length == 0)
            {
                result_region = Segment(image, threshold, out score,out annomalyMap).ClosingCircle(2.5).Connection();
            }
            else if (inspected_region.Area == 0)
            {
                result_region = Segment(image, threshold, out score,out annomalyMap).ClosingCircle(2.5).Connection();
            }
            else
            {
                inspected_region.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                //var image_croped = image.CropRectangle1(r1, c1, r2, c2);
                //image_croped.GetImageSize(out int w, out int h);
                //HRegion diffrg = new HRegion(0, 0.0, h, w);
                //var subRg = diffrg.Difference(RegionInput.Value.MoveRegion(-r1, -c1));
                //image_croped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
                //image_croped.WriteImage("png", 0, "D://image.png");
                result_region = Segment(image_croped, threshold, out score, out annomalyMap).ClosingCircle(2.5).MoveRegion(r1, c1).Connection();        
                image.GetImageSize(out int w, out int h);
                HHomMat2D translate = new HHomMat2D();
                translate=translate.HomMat2dTranslate((double)r1, c1);
                annomalyMap=annomalyMap.AffineTransImageSize(translate, "bilinear", w, h);
            }



            if (IContext != null)
            {
                if (inspected_region != null)
                {
                    IContext.inspection_result.AddRegion(inspected_region, "green");
                }
                if (result_region != null & ShowDisplay)
                {
                    IContext.inspection_result.AddRegion(result_region, "red");
                }
            }
            RegionOutput.OnNext(result_region);
            AnomalyScore.OnNext(Math.Round(score,3));
            AnomalyMap.OnNext(annomalyMap);
        }
        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public HRegion Segment(HImage input,double threshold,out double score, out HImage anomalyMap)
        {
            score = 0;
            if (input != null)
            {
                
                if (Runtime.State == ModelState.Unloaded)
                {
                    Runtime.LoadRecipe();
                }
                if (Runtime.State == ModelState.Error)
                {
                    var regionResult = new HRegion();
                    regionResult.GenEmptyRegion();
                    input.GetImageSize(out int w, out int h);
                    anomalyMap = new HImage("real",w,h);
                    return regionResult;
                }
                if (Runtime.State == ModelState.Loaded)
                {
                    anomalyMap = Runtime.Infer(input, out score);
                    return anomalyMap.Threshold(threshold, double.MaxValue);
                }
            }
            var region = new HRegion();
            region.GenEmptyRegion();
            input.GetImageSize(out int oriw, out int orih);
            anomalyMap = new HImage("real", oriw, orih);
            return region;
        }
    }
    public enum ToolRecordMode
    {
        RunOnly,RecordOnly,RecordAndRun
    }
    public class ONNXAnomalyV3 : ONNXModel,IHalconDeserializable
    {
        
        public ONNXAnomalyV3(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name;
        
        public string ModelDir;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        public double Threshold { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                if (System.IO.File.Exists(System.IO.Path.Combine(directory,"result","prediction.txt")))
                {
                    JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(directory, "result", "prediction.txt")));
                    Min = data["min"].Value<float>();
                    Max = data["max"].Value<float>();
                    Threshold = data["threshold"].Value<float>();
                }
                SessionOptions options;
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
                else
                {
                    int gpuDeviceId = 0; // The GPU device ID to execute on
                    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                }
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
                    input_width = 224;
                    input_height = 224;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * 224 * 224 * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, 224, 224 })),
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
        public bool LoadRecipe()
        {
            try
            {
                return LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public HImage InferFloat(HImage imgInput)
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
            imgInput.GetImageSize(out int originalW, out int originalH);
            var image_resize = imgInput.ZoomImageSize(input_width, input_height, "constant");
            var imageNormalize = Normalize(image_resize);
            var array_final = Processing.HalconUtils.HImageToFloatArray(imageNormalize, num_channel, out int _, out int _);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;

            double predict_score = 0;
            if (results.Count == 2)
            {
                predict_score = (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).FirstOrDefault();
            }
            else
            {
                predict_score = TensorOutput.Max();
            }
            //return a list 

            predict_score = ((predict_score - Threshold) / (Max - Min)) + 0.5;
            var score = predict_score;

            var result = new HImage("float", TensorOutput.Dimensions[3], TensorOutput.Dimensions[2]);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, width * height);
            //crop if image was padded

            result = (result - Threshold) / (Max - Min) + 0.5;
            return result.ZoomImageSize(originalW, originalH, "constant");

        }
        public HImage Infer(HImage imgInput,out double score, bool normalize = true)
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
            imgInput.GetImageSize(out int originalW, out int originalH);
            var image_resize = imgInput.ZoomImageSize(input_width, input_height, "constant");
            HImage imageNormalize;
            if (normalize)
            {
                imageNormalize = Normalize(image_resize);
            }
            else
            {
                imageNormalize = image_resize.ConvertImageType("float");
            }
            var array_final = Processing.HalconUtils.HImageToFloatArray(imageNormalize, num_channel, out int _, out int _);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            double predict_score = 0;
            if (results.Count == 2)
            {
                predict_score= (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).FirstOrDefault();
            }
            else
            {
                predict_score = TensorOutput.Max();
            }
            //return a list 

            predict_score = ((predict_score - Threshold) / (Max - Min)) + 0.5;
            score = predict_score;
            var result = new HImage("float", TensorOutput.Dimensions[3], TensorOutput.Dimensions[2]);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, width * height);
            //crop if image was padded
            //result = result.ScaleImageMax();
            //result.WriteImage("tiff", 0, "D:/1.tiff");
            //result.max
            //result = (result - Threshold) / (Max - Min) + 0.5;
            return result.ZoomImageSize(originalW, originalH, "constant");
            
        }
        public HImage Normalize(HImage imageInput)
        {
            HImage image = imageInput.ConvertImageType("float") / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
        public HImage NormalizeFloat(HImage imageInput)
        {
            HImage image = imageInput / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
        public unsafe HRegion BuildRegions(NDArray results, NDArray img, float threshold, int originalW, int originalH)
        {
            NDArray hotmap = results[0];

            //NDArray prediction = np.any(hotmap > threshold);

            HImage hotmapHalcon = new HImage("float", hotmap.shape[1], hotmap.shape[0], new IntPtr(hotmap.Unsafe.Address));

            hotmapHalcon = hotmapHalcon.GaussFilter(3);

            HImage resizedHotmapHalcon = hotmapHalcon.ZoomImageSize(originalW, originalH, "bilinear");

            return resizedHotmapHalcon.Threshold(threshold, double.MaxValue);
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
    public class ONNXTFFastflow : ONNXModel, IHalconDeserializable
    {

        public ONNXTFFastflow(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name;

        public string ModelDir;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        [Browsable(false)]
        public double Threshold { get; set; }
        [Browsable(false)]
        public double Min { get; set; }
        [Browsable(false)]
        public double Max { get; set; }
        public void Dispose()
        {
            ONNXSession?.Dispose();
        }
        bool is_loading = false;
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                //if (System.IO.File.Exists(System.IO.Path.Combine(directory, "result", "prediction.txt")))
                //{
                //    JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(directory, "result", "prediction.txt")));
                //    Min = data["min"].Value<float>();
                //    Max = data["max"].Value<float>();
                //    Threshold = data["threshold"].Value<float>();
                //}
                SessionOptions options= CreateProviderOption(directory);
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
                //else if(Provider == ONNXProvider.CUDA)
                //{
                //    int gpuDeviceId = 0; // The GPU device ID to execute on
                //    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //}
                
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
                
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (input_width == -1 | input_height == -1)
                {
                    input_width = 224;
                    input_height = 224;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * 224 * 224 * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, 224, 224 })),
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
                this.State = ModelState.Error;
                return false;
            }

            return true;
        }
        public override bool LoadRecipe()
        {
            if(is_loading)
            {
                return false;
            }
            is_loading = true;
            bool result = false;
            try
            {
                result= LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                result= false;
            }
            is_loading = false;
            return result;
        }
        public HImage Infer(HImage imgInput, out double score, bool normalize = true)
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
            imgInput.GetImageSize(out int originalW, out int originalH);
            var image_resize = imgInput.ZoomImageSize(input_width, input_height, "bilinear");
            HImage imageNormalize;
            //if (normalize)
            //{
            //     imageNormalize = Normalize(image_resize);
            //}
            //else
            //{
            //    imageNormalize = image_resize.ConvertImageType("float");
            //}

            //image_resize.MinMaxGray(image_resize, 0, out double MinGray, out double MaxGray, out double RangeGray);
            imageNormalize = image_resize.ConvertImageType("real") / 255.0;
            var array_final = Processing.HalconUtils.HImageToFloatArray(imageNormalize, num_channel, out int _, out int _);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            //double predict_score = 0;
            
            int outputw, outputh;
            //if (TensorOutput.Dimensions.Length == 4)
            //{
            //    outputw = TensorOutput.Dimensions[3];
            //    outputh = TensorOutput.Dimensions[2];
            //}
            //else
            //{
            if (TensorOutput.Dimensions.Length != 2)
            {
                outputw = TensorOutput.Dimensions[TensorOutput.Dimensions.Length-1];
                outputh = TensorOutput.Dimensions[TensorOutput.Dimensions.Length-2];
            }
            else
            {
                outputw = TensorOutput.Dimensions[1];
                outputh = TensorOutput.Dimensions[0];
            }
                
            //}
            var result = new HImage("real", outputw, outputh);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, width * height);
            //crop if image was padded
            //result = result.ScaleImageMax();
            //var result1 = result * 255;
            //result1 = result1.ConvertImageType("byte");
            //result1.WriteImage("tiff", 0, "D:/predict.tiff");
            //result.max
            //result = (result - Threshold) / (Max - Min) + 0.5;
            //result.get
            if (results.Count == 2)
            {
                score = (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).FirstOrDefault();
            }
            else
            {
                result.MinMaxGray(result, 0, out double min, out score, out double range);
            }
            //return a list 

            //predict_score = ((predict_score - Threshold) / (Max - Min)) + 0.5;
            
            return result.ZoomImageSize(originalW, originalH, "constant");

        }
        public HImage Normalize(HImage imageInput)
        {
            HImage image = imageInput.ConvertImageType("float") / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
        public HImage NormalizeFloat(HImage imageInput)
        {
            HImage image = imageInput / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
        public unsafe HRegion BuildRegions(NDArray results, NDArray img, float threshold, int originalW, int originalH)
        {
            NDArray hotmap = results[0];

            //NDArray prediction = np.any(hotmap > threshold);

            HImage hotmapHalcon = new HImage("float", hotmap.shape[1], hotmap.shape[0], new IntPtr(hotmap.Unsafe.Address));

            hotmapHalcon = hotmapHalcon.GaussFilter(3);

            HImage resizedHotmapHalcon = hotmapHalcon.ZoomImageSize(originalW, originalH, "bilinear");

            return resizedHotmapHalcon.Threshold(threshold, double.MaxValue);
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
    public enum FAPMModelType
    {
        small,medium,large,xlarge
    }
    public enum EfficientADModelType
    {
        small, medium, large
    }
    public enum EfficientADDetectionType
    {
        all, structure
    }
    public class EfficientADModelConfig
    {
        public string WEIGHTS_DIR { get; set; }
        public int PATIENCE { get; set; } = 0;
        public EfficientADModelType MODEL_TYPE { get; set; } = EfficientADModelType.small;
        public EfficientADDetectionType DETECTION_TYPE { get; set; } = EfficientADDetectionType.all;
        public int EVAL_INTERVAL { get; set; } = 1;
    }
    public class DeepKNNModelConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public FAPMModelType MODEL_TYPE { get; set; } = FAPMModelType.medium;
        [Description("Distance between good and bad sample. Higer value reduce false detection.\nMinimum: 1")]
        public int EPOCHS_FOR_FINE_TUNING { get; set; } = 100;
        public int EPOCHS_FOR_KNN { get; set; } = 200;
        public double CoresetSamplingRatio { get; set; } = 0.01;
        public bool FINE_TUNING_BACKBONE { get; set; } = true;
        public int K { get; set; } = 5;
    }
    public class FAPMModelConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public FAPMModelType MODEL_TYPE { get; set; } = FAPMModelType.medium;
        [Description("Distance between good and bad sample. Higer value reduce false detection.\nMinimum: 1")]
        public double DISTANCE_GAIN_FOR_KNN { get; set; } = 1.02;
        [Description("Number of image in memory")]
        public int N_SAMPLES { get; set; } = 100;
        public double PERCENT_RETAINED { get; set; } = 0.5;
        public double CoresetSamplingRatio { get; set; } = 0.01;
        public bool USING_CROP_IMAGE { get; set; } = false;
        public int PatchWidth { get; set; } = 32;
        public int PatchHeight { get; set; } = 32;
        public int K { get; set; } = 1;
        public int X1 { get; set; } = 0;
        public int Y1 { get; set; } = 0;
        public int X2 { get; set; } = 256;
        public int Y2 { get; set; } = 256;
        public bool UPDATE_THRESHOLD { get; set; } = false;
    }
    public enum TrainResume
    {
        Resume,New,Scratch
    }
    public enum YOLOTrainType
    {
        resume, transfer, scratch
    }
    public enum CheckPointEnum
    {
        Latest,Best
    }
    public class YOLONASSegmentationConfig
    {
        public int EPOCHS { get; set; } = 1;
        public bool EnableAugmentation { get; set; } = false;
        public string ModelDir { get; set; }
        public string ResultDir { get; set; }
        public string ImageDir { get; set; }
        public string AnnotationDir { get; set; }
        public string AnnotationFile { get; set; }
        public string DatasetFormat { get; set; } = "folderv2";
        public string ModelType { get; set; } = "s";
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 640;
        public int INPUT_HEIGHT { get; set; } = 640;
        public string Precision { get; set; } = "float32";
        public bool USE_HISTOGRAM { get; set; } = true;
        public int NUM_BATCHES_FOR_PQT { get; set; } = 2;
        public string CALIB_METHOD { get; set; } = "percentile";
        public int EPOCHS_FOR_QAT { get; set; } = 1;
        public bool USE_MODEL_INT8 { get; set; } = true;
        public Albumentations Augmentations { get; set; } = new Albumentations();
        public double LR_INIT { get; set; } = 1e-4;
        public int PATIENCE { get; set; } = 10;
        [JsonConverter(typeof(StringEnumConverter))]
        public YOLOTrainType TrainType { get; set; } = YOLOTrainType.transfer;
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckPointEnum CheckPoint { get; set; } = CheckPointEnum.Latest;
        public double MIN_LR { get; set; } = 0.0001;
        public int NUM_WORKERS { get; set; } = 2;

        public string EvaluationDir { get; set; }
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public static YOLONASSegmentationConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                try
                {
                    var json = File.ReadAllText(ConfigDir);
                    var config = JsonConvert.DeserializeObject<YOLONASSegmentationConfig>(json);
                    config.ApplyRelativeDir(BaseDir);
                    config.Save();
                    return config;
                }catch(Exception ex)
                {
                    return new YOLONASSegmentationConfig(BaseDir);
                }
                
            }
            else
            {
                return new YOLONASSegmentationConfig(BaseDir);
            }

        }
        private YOLONASSegmentationConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();

        }

        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = System.IO.Path.Combine(BaseDir, "images");
            AnnotationDir = System.IO.Path.Combine(BaseDir, "annotations");
            AnnotationFile = System.IO.Path.Combine(BaseDir, "images","_annotation.coco.json");
            ResultDir = System.IO.Path.Combine(BaseDir, "result");
            EvaluationDir = System.IO.Path.Combine(BaseDir, "evaluation");
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            ModelDir = BaseDir;
            this.BaseDir = BaseDir;
            if (!System.IO.Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            
            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            if (!System.IO.Directory.Exists(EvaluationDir))
            {
                Directory.CreateDirectory(EvaluationDir);
            }
            if (!System.IO.Directory.Exists(ResultDir))
            {
                Directory.CreateDirectory(ResultDir);
            }
        }

        private YOLONASSegmentationConfig()
        {

        }
    }
    public class AnomalyV3Config
    {
        public int Epoch { get; set; } = 1;
        public bool EnableAugmentation { get; set; } = false;
        public string ModelDir { get; set; }
        public string ResultDir { get; set; }
        public string ImageDir { get; set; }
        public string UnknownDir { get; set; }
        public string NormalDir { get; set; }
        public string AnomalyDir { get; set; }
        public string MaskDir { get; set; }
        public string AnnotationDir { get; set; }
        public string DatasetFormat { get; set; } = "folderv2";
        public string ModelName { get; set; } = "patchcore";
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public string Precision { get; set; } = "float32";
        public bool InputAsChannelLast { get; set; } = false;
        public bool ExportWithNomalize { get; set; } = false;
        public Albumentations Augmentations { get; set; } = new Albumentations();
        [Description("Feature Extraction Model : resnet18, resnet34, resnet50, wide_resnet50_2")]
        public string BACKBONE { get; set; } = "wide_resnet50_2";
        public double LR_INIT { get; set; } = 1e-3;
        public int PATIENCE { get; set; } = 5;
        public double BIAS_GAIN_FOR_BAD_IMAGES { get; set; } = 10.0;
        public int N_SAMPLES { get; set; } = 5;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TrainType { get; set; } = TrainResume.New;
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckPointEnum CheckPoint { get; set; } = CheckPointEnum.Latest;
        #region Memsegv3 parameters
        public double MIN_LR { get; set; } = 0.0001;
        public int EVAL_INTERVAL { get; set; } = 100;
        public int NUM_WORKERS { get; set; } = 2;
        public int MEMORY_SAMPLE { get; set; } = 10;
        public bool VisualizeAugmentations { get; set; } = false;
        [Description("Higher value generate more type of anomalies. Default value : 6")]
        public int PerlinScale { get; set; } = 6;
        [Description("Higher value generate more type of anomalies. Default value : 0")]
        public int MinPerlinScale { get; set; } = 0;
        [Description("Smaller value generate more anomalies. Default value : 0.5")]
        public double PerlinNoiseThreshold { get; set; } = 0.5;
        [Description("Default value : 8")]
        public int StructureGridSize { get; set; } = 8;
        [Description("Default value : 1")]
        public double TransparencyRangeHigh { get; set; } = 1;
        [Description("Smaller value generate more anomalies. Default value : 0.5")]
        public double TransparencyRangeLow { get; set; } = 0.5;
        [Description("Percent of good image use to testing. Defaul value : 1")]
        public double TestRatio { get; set; } = 1;
        #endregion
        public FAPMModelConfig FAPMConfig { get; set; } = new FAPMModelConfig();
        public DeepKNNModelConfig DeepKNNConfig { get; set; } = new DeepKNNModelConfig();
        public EfficientADModelConfig EfficientADConfig { get; set; } = new EfficientADModelConfig();

        public string EvaluationDir { get; set; }
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public static AnomalyV3Config Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<AnomalyV3Config>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new AnomalyV3Config(BaseDir);
            }

        }
        private AnomalyV3Config(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();

        }

        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = System.IO.Path.Combine(BaseDir, "images");
            MaskDir = System.IO.Path.Combine(BaseDir, "masks");
            AnnotationDir = System.IO.Path.Combine(BaseDir, "annotations");
            ResultDir = System.IO.Path.Combine(BaseDir, "result");
            NormalDir = System.IO.Path.Combine(ImageDir, "good");
            UnknownDir = System.IO.Path.Combine(ImageDir, "unknown");
            AnomalyDir = System.IO.Path.Combine(ImageDir, "bad");
            EvaluationDir= System.IO.Path.Combine(BaseDir, "evaluation");
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            ModelDir = BaseDir;
            this.BaseDir = BaseDir;
            if (!System.IO.Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            if (!System.IO.Directory.Exists(MaskDir))
            {
                Directory.CreateDirectory(MaskDir);
            }
            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            if (!System.IO.Directory.Exists(EvaluationDir))
            {
                Directory.CreateDirectory(EvaluationDir);
            }
            if (!System.IO.Directory.Exists(NormalDir))
            {
                Directory.CreateDirectory(NormalDir);
            }
            if (!System.IO.Directory.Exists(AnomalyDir))
            {
                Directory.CreateDirectory(AnomalyDir);
            }
            if (!System.IO.Directory.Exists(UnknownDir))
            {
                Directory.CreateDirectory(UnknownDir);
            }
            if (!System.IO.Directory.Exists(ResultDir))
            {
                Directory.CreateDirectory(ResultDir);
            }
        }

        private AnomalyV3Config()
        {

        }
    }
}
