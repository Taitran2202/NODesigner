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
    [NodeInfo("Deep Learning", "Instance Segmentation", Icon: "Designer/icons/icons8-orange-100.png")]
    public class InstanceSegmentationNode : BaseNode
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
                    //AnomalyV3Window wd = new AnomalyV3Window(this);
                    //wd.Owner = Window.GetWindow(sender);
                    //wd.Show();
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
        public ValueNodeOutputViewModel<InstanceSegmentResult[]> InstanceOutput { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<double> ConfidenceThreshold { get; }
        [HMIProperty]
        public int FillBackground { get; set; } = 0;
        [HMIProperty]
        public bool DisplayInstanceRegion { get; set; } = true;
        [HMIProperty]
        public bool DisplayBoundingBox { get; set; } = true;
        [HMIProperty]
        public bool DisplayClassName { get; set; } = true;
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public AnomalyV3Config TrainConfig { get; set; }

        public string RootDir;
        
        //public ONNXAnomalyV3 Runtime { get; set; }
        public InstanceSegmentationOnnx Runtime { get; set; }
        #endregion

        static InstanceSegmentationNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<InstanceSegmentationNode>));
        }

        public InstanceSegmentationNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Instance Segmentation";
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

            InstanceOutput = new ValueNodeOutputViewModel<InstanceSegmentResult[]>()
            {
                Name = "Instances",
                PortType = "InstanceSegmentResult[]",
                Editor = new HObjectOutputValueEditorViewModel<HRegion>()
               
            };
            this.Outputs.Add(InstanceOutput);
            
            ConfidenceThreshold = new ValueNodeInputViewModel<double>()
            {
                Name = "Threshold",
                PortType = "Double",
                Editor = new DoubleValueEditorViewModel(defaultStep:0.1)
            };
            this.Inputs.Add(ConfidenceThreshold);
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
            Runtime = new InstanceSegmentationOnnx(TrainConfig.ModelDir);
            if (RegionInput.Value.Area == 0)
            {
                HImage image = new HImage("byte", 700, 700);
                RunInside(image, 0.5);
            }
            else
            {
                RegionInput.Value.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var image_croped = new HImage("byte",c2-c1,r2-r1);
                image_croped.GetImageSize(out int w, out int h);
                HRegion diffrg = new HRegion(0, 0.0, h, w);
                var subRg = diffrg.Difference(RegionInput.Value.MoveRegion(-r1, -c1));
                image_croped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
                RunInside(image_croped, 0.5);
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
            InstanceSegmentResult[] result_instances;
            HImage image = Image.Value;
            double threshold = ConfidenceThreshold.Value;
            var IContext = context as InspectionContext;
            if (inspected_region.Area.Length == 0)
            {
                result_instances = RunInside(image, threshold);
            }
            else if (inspected_region.Area == 0)
            {
                result_instances = RunInside(image, threshold);
            }
            else
            {
                inspected_region.Union1().SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                result_instances = RunInside(image_croped, threshold);
                image.GetImageSize(out int w, out int h);
                HHomMat2D translate = new HHomMat2D();
                translate=translate.HomMat2dTranslate((double)r1, c1);
                for(int i = 0; i < result_instances.Length; i++)
                {
                    result_instances[i].Region = result_instances[i].Region.AffineTransRegion(translate, "constant");
                    result_instances[i].BoundingBox.row1 += r1;
                    result_instances[i].BoundingBox.row2 += r1;
                    result_instances[i].BoundingBox.col1 += c1;
                    result_instances[i].BoundingBox.col2 += c1;
                }
                
            }



            if (IContext != null & ShowDisplay)
            {
                foreach(var item in result_instances)
                {
                    if (DisplayBoundingBox)
                    {
                        IContext.inspection_result.AddRect1("green", item.BoundingBox.row1,
                            item.BoundingBox.col1, item.BoundingBox.row2,
                            item.BoundingBox.col2);
                    }
                    if (DisplayInstanceRegion)
                    {
                        IContext.inspection_result.AddRegion(item.Region,"green");
                    }
                    if (DisplayClassName)
                    {
                        IContext.inspection_result.AddText(item.ClassID.ToString(),"black",
                            "#ffffffff", item.BoundingBox.row1, item.BoundingBox.col1,18);
                    }
                }
            }
            InstanceOutput.OnNext(result_instances);
        }
        public void ClearSession()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public InstanceSegmentResult[] RunInside(HImage input,double threshold)
        {
            if (input != null)
            {
                
                if (Runtime.State == ModelState.Unloaded)
                {
                    Runtime.LoadRecipe();
                }
                if (Runtime.State == ModelState.Error)
                {
                    return new InstanceSegmentResult[0];
                }
                if (Runtime.State == ModelState.Loaded)
                {
                    return Runtime.Predict(input, (float)threshold);
                }
            }
            return new InstanceSegmentResult[0];
        }
    }
    public class SemanticSegmentationOnnx : ONNXModel, IHalconDeserializable
    {
        public void Dispose()
        {
            ONNXSession?.Dispose();
        }

        
        public SemanticSegmentationOnnx(string dir)
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
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                SessionOptions options;
                if (Provider == ONNXProvider.TensorRT)
                {

                    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                      {
                          // Enable INT8 for QAT models, disable otherwise.
                         // { "trt_fp16_enable", "0" },
                        //{"trt_dump_subgraphs","1" },
                        //{"trt_builder_optimization_level","1" },
                           {"trt_int8_enable","1" },
                        //{"trt_detailed_build_log","1" },
                        //{"trt_int8_use_native_calibration_table","0" },
                          { "trt_engine_cache_enable", "1" },
                          {"trt_engine_cache_path",directory}
                     });
                    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                    options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
                    //options.AppendExecutionProvider_CUDA(0);
                }
                else
                {
                    int gpuDeviceId = 0; // The GPU device ID to execute on
                    OrtCUDAProviderOptions cudaOption = new OrtCUDAProviderOptions();
                    cudaOption.UpdateOptions(new Dictionary<string, string>()
                      {
                           //{"cudnn_conv_algo_search","HEURISTIC"},
                            {"device_id","0" }
                     });
                    options = SessionOptions.MakeSessionOptionWithCudaProvider(cudaOption);
                    
                }
                
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[ONNXSession.InputMetadata[input_name].Dimensions.Length - 1];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[ONNXSession.InputMetadata[input_name].Dimensions.Length - 2];

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
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width }))
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
            if(State== ModelState.Error)
            {
                return false;
            }
            try
            {
                return LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public InstanceSegmentResult[] Predict(HImage img, int classID,float confidence)
        {

            img.GetImageSize(out int originalW, out int orignalH);
            float scale = Math.Min((float)input_width / originalW, (float)input_height / orignalH);
            int nw = (int)(scale * originalW);
            int nh = (int)(scale * orignalH);
            HImage resizeImg;
            if (img.CountChannels() == 1)
            {
                resizeImg = img.Compose3(img, img).ZoomImageSize(nw, nh, "bilinear");
            }
            else
            {
                resizeImg = img.ZoomImageSize(nw, nh, "bilinear");
            }
            var paddedImage = resizeImg.ChangeFormat(input_width, input_height);
            var inputImageValues = Processing.HalconUtils.HImageToFloatArray(paddedImage.ConvertImageType("float") / 255f, 3, out _, out _);

            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,3,input_height,input_width })),
            };

            var results = ONNXSession.Run(inputs).ToList();
            var masks = (results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).ToArray();
            var numClasses = masks.Length / (input_height * input_width/4);
            int outputw = input_width/2;
            int outputh = input_height/2;
            
            InstanceSegmentResult[] result = new InstanceSegmentResult[1];
            float wRatio = (float)originalW / input_width;
            float hRatio = (float)orignalH / input_height;
            for (int i = 0; i < 1; i++)
            {
                result[i] = new InstanceSegmentResult();
                HImage maskImage = new HImage("real", outputh, outputw);
                IntPtr pointerResult = maskImage.GetImagePointer1(out HTuple type, out _, out _);
                Marshal.Copy(masks, classID * outputw * outputh, pointerResult, outputw * outputh);
                result[i].Region = maskImage.ZoomImageFactor(((float)input_width / outputw) / scale,
                    ((float)input_width / outputw) / scale, "bilinear").Threshold(0.5, 255);
                result[i].ClassID = classID;
                result[i].Region.SmallestRectangle1(out int br1,out int bc1,out int br2,out int bc2);
                result[i].BoundingBox = new Rect1(br1, bc1, br2, bc2);
                result[i].Confidence = confidence;
            };
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
    public class InstanceSegmentationOnnx : ONNXModel,IHalconDeserializable
    {
        public void Dispose()
        {
            ONNXSession?.Dispose();
        }
        public InstanceSegmentationOnnx(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name, input_conf_threshold;
        
        public string ModelDir;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
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
                input_conf_threshold = "input_conf_threshold";
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[ONNXSession.InputMetadata[input_name].Dimensions.Length-1];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[ONNXSession.InputMetadata[input_name].Dimensions.Length - 2];

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
                        NamedOnnxValue.CreateFromTensor<float>(input_conf_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{0.5f },new int[]{1})),
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
                        NamedOnnxValue.CreateFromTensor<float>(input_conf_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{0.5f },new int[]{1})),
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
        public override bool LoadRecipe()
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
        public InstanceSegmentResult[] Predict(HImage img, float confidenceThreshold)
        {
            
            img.GetImageSize(out int originalW, out int orignalH);
            float scale = Math.Min((float)input_width / originalW, (float)input_height / orignalH);
            int nw = (int)(scale * originalW);
            int nh = (int)(scale * orignalH);
            HImage resizeImg;
            if (img.CountChannels() == 1)
            {
                resizeImg = img.Compose3(img,img).ZoomImageSize(nw, nh, "bilinear");
            }
            else
            {
                resizeImg = img.ZoomImageSize(nw, nh, "bilinear");
            }           
            var paddedImage = resizeImg.ChangeFormat(input_width, input_height);
            var inputImageValues = Processing.HalconUtils.HImageToFloatArray(paddedImage.ConvertImageType("float")/255f, 3, out _, out _);

            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,3,input_height,input_width })),
                NamedOnnxValue.CreateFromTensor(input_conf_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{confidenceThreshold },new int[]{1})),
            };

            var results = ONNXSession.Run(inputs).ToList();
            var boxes = (results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).ToArray();
            var tensorMask = results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var masks = (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).ToArray();
            int numBoxes = boxes.Length / 6;
            int outputw= tensorMask.Dimensions[tensorMask.Dimensions.Length-1];
            int outputh = tensorMask.Dimensions[tensorMask.Dimensions.Length - 2];
            InstanceSegmentResult[] result = new InstanceSegmentResult[numBoxes];
            float wRatio = (float)originalW / input_width;
            float hRatio = (float)orignalH / input_height;
            for (int i = 0; i < numBoxes; i++){
                result[i] = new InstanceSegmentResult();
                HImage maskImage = new HImage("real", outputw, outputh);
                IntPtr pointerResult = maskImage.GetImagePointer1(out HTuple type, out _, out _);
                Marshal.Copy(masks, i* outputw* outputh, pointerResult, outputw * outputh);
                result[i].Region = maskImage.ZoomImageFactor(((float)input_width / outputw) / scale, ((float)input_width / outputw) / scale, "bilinear").Threshold(0.5, 255);
                result[i].ClassID = (int)boxes[i * 6 + 5];
                result[i].BoundingBox = new Rect1(boxes[i * 6 + 1]/ scale, boxes[i * 6]/ scale, boxes[i * 6 + 3]/ scale, boxes[i * 6 + 2]/ scale);
                result[i].Confidence = boxes[i * 6 + 4];
            };
            
            


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
    public class InstanceSegmentResult
    {
        public HRegion Region { get; set; }
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public double Confidence { get; set; }
        public Rect1 BoundingBox { get; set; }
    }
}
