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
using System.Windows.Controls;
using Newtonsoft.Json.Converters;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "Text Detection", Icon: "Designer/icons/icons8-term-100.png")]
    public class TextDetectionNode : BaseNode
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
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeOutputViewModel<HRegion> TextRegion { get; set; }
        public ValueNodeInputViewModel<double> Threshold { get; }
        public ValueNodeInputViewModel<double> MinSize { get; }
        public TextDetectionConfig TrainConfig { get; set; }

        public string RootDir;
        
        public PPOCRV3TextDetection Runtime { get; set; }
        #endregion

        static TextDetectionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<TextDetectionNode>));
        }

        public TextDetectionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Text Detection";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region of interest",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);

            TextRegion = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Text Regions",
                PortType = "HRegion"
            };
            this.Outputs.Add(TextRegion);
            Threshold = new ValueNodeInputViewModel<double>()
            {
                Name = "Threshold",
                PortType = "Double",
                Editor = new DoubleValueEditorViewModel(defaultValue:100)
            };
            this.Inputs.Add(Threshold);
            MinSize = new ValueNodeInputViewModel<double>()
            {
                Name = "Minimum Fragment Size",
                PortType = "Double",
                Editor = new DoubleValueEditorViewModel()
            };
            this.Inputs.Add(MinSize);

            TrainConfig = TextDetectionConfig.Create(RootDir);
        }
        public override void OnInitialize()
        {
            Runtime = new PPOCRV3TextDetection(TrainConfig.ModelDir);
        }
        public HRegion DetectText(HImage input, HRegion regionInput)
        {
            if (input != null)
            {
                if (Runtime.State == ModelState.Unloaded)
                {
                    Runtime.LoadRecipe();
                }
                if (Runtime.State == ModelState.Error)
                {
                    return null;
                }
                if (Runtime.State == ModelState.Loaded)
                {
                    regionInput.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                    var imgCroped = input.CropRectangle1(r1, c1, r2, c2);
                    imgCroped.GetImageSize(out int oriW, out int oriH);

                    var result = Runtime.Infer(imgCroped);
                    result.GetImageSize(out int resizeW, out int resizeH);
                    var region_thresh = result.Threshold(Threshold.Value, double.MaxValue).ZoomRegion(oriW*1.0/resizeW,oriH*1.0/resizeH).MoveRegion(r1,c1);
                    
                    return region_thresh.Connection().SelectShape("area","and",MinSize.Value,double.MaxValue);

                }
            }
            var region = new HRegion();
            region.GenEmptyRegion();
            return region;
        }
        public override void Run(object context)
        {

           var textRegion = DetectText(Image.Value,RegionInput.Value);
            var IContext = context as InspectionContext;
            if (ShowDisplay)
            {
                if (RegionInput.Value != null)
                {
                    IContext.inspection_result.AddDisplay(RegionInput.Value, "green");
                    //RegionInput.Value.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                }
               
            }
            
            TextRegion.OnNext(textRegion);
        }
        [HMIProperty("Train Window")]
        public IReactiveCommand OpenTrainWindow
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    OnCommand("editor", sender);
                });
            }
        }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    TextDetectionWindow wd = new TextDetectionWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
            }

        }

        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
       
    }
    public class TextDetectionConfig
    {
        public int Epoch { get; set; } = 100;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TrainType { get; set; } = TrainResume.New;
        public string ModelDir { get; set; }
        public string PaddleOCRDir { get; set; } = "C:/src/PaddleOCR";
        public string PretrainDir { get; set; } = "C:/paddle_pretrain/en_PP-OCRv3_det_distill_train/student";
        public string ResultDir { get; set; }
        public string ImageDir { get; set; }
        public string TrainDir { get; set; }
        public string AnnotationDir { get; set; }
        public string ModelName { get; set; } = "paddle-det";
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public string Precision { get; set; } = "float32";
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public static TextDetectionConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<TextDetectionConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new TextDetectionConfig(BaseDir);
            }

        }
        private TextDetectionConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();

        }

        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = System.IO.Path.Combine(BaseDir, "images");
            AnnotationDir = System.IO.Path.Combine(BaseDir, "annotations");
            ResultDir = System.IO.Path.Combine(BaseDir, "result");
            TrainDir = System.IO.Path.Combine(BaseDir, "train");
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

            if (!System.IO.Directory.Exists(ResultDir))
            {
                Directory.CreateDirectory(ResultDir);
            }
            if (!System.IO.Directory.Exists(TrainDir))
            {
                Directory.CreateDirectory(TrainDir);
            }
        }

        private TextDetectionConfig()
        {

        }
    }
    public interface ITextDetection
    {
        bool LoadRecipe();
        HImage Infer(HImage imgInput);
    }

    public class PPOCRV3TextDetection : ONNXModel,ITextDetection
    {
        public void LoadDefaultModel()
        {

        }
        public PPOCRV3TextDetection(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name;
        public float[] ImagePreprocess(HImage img, int[] targetSize)
        {
            int nw = targetSize[0];
            int nh = targetSize[1];
            var resizedImg = img.ZoomImageSize(nw, nh, "bilinear");
            var imageFloat = resizedImg.ConvertImageType("float");
            //mean std normalize mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]
            imageFloat = imageFloat / 255f;
            var r = imageFloat.Decompose3(out HImage g, out HImage b);
            r = (r - 0.485) / 0.229;
            g = (g - 0.456) / 0.224;
            b = (b - 0.406) / 0.225;
            float[] result = new float[nw * nh * 3];
            var r_pointer = r.GetImagePointer1(out HTuple type, out HTuple w, out HTuple h);
            var g_pointer = g.GetImagePointer1(out type, out w, out h);
            var b_pointer = b.GetImagePointer1(out type, out w, out h);
            Marshal.Copy(r_pointer, result, 0, nw * nh);
            Marshal.Copy(g_pointer, result, nw * nh, nw * nh);
            Marshal.Copy(b_pointer, result, 2 * nw * nh, nw * nh);
            return result;
        }
       
        public (int resize_w, int resize_h) CalculateResize(int w, int h, int limit_side_len = 960, int MinDiv = 32)
        {
            double ratio = 1;
            if (Math.Max(h, w) > limit_side_len)
            {
                if (h > w)
                {
                    ratio = (float)limit_side_len / h;
                }
                else
                {
                    ratio = (float)limit_side_len / w;
                }

            }
            else
            {
                ratio = 1;
            }
            int resize_h = (int)(h * ratio);
            int resize_w = (int)(w * ratio);

            resize_h = Math.Max((int)(Math.Round((double)resize_h / MinDiv) * MinDiv), MinDiv);
            resize_w = Math.Max((int)(Math.Round((double)resize_w / MinDiv) * MinDiv), MinDiv);
            return (resize_w, resize_h);
        }
        public string ModelDir;
        int MaxDimension = 960;
        int MinDiv = 32;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        public bool LoadOnnx(string directory)
        {
            try
            {
                

                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    if (!System.IO.File.Exists(System.IO.Path.Combine(MainWindow.AppPath,"Designer/Python/OCR/detection/ppocrv3/model.onnx")))
                    {
                        State = ModelState.NotFound;
                        return false;
                    }
                    else
                    {
                        directory = "Designer/Python/OCR/detection/ppocrv3";
                        
                    }
                }
                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];
                    num_channel = ONNXSession.InputMetadata[input_name].Dimensions[1];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (input_width == -1 | input_height == -1)
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * MinDiv * MinDiv * num_channel);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, num_channel, MinDiv, MinDiv })),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * num_channel);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, num_channel, input_height, input_width })),
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
            imgInput.GetImageSize(out int w, out int h);
            var resizeParam = CalculateResize(w, h, MaxDimension, MinDiv);
            var imageNormalize = ImagePreprocess(imgInput,new int[] {resizeParam.resize_w,resizeParam.resize_h });
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(imageNormalize,new int[]{1,num_channel, resizeParam.resize_h, resizeParam.resize_w }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var result = new HImage("float", resizeParam.resize_w, resizeParam.resize_h);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple _, out HTuple _);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, resizeParam.resize_w*resizeParam.resize_h);
            
            return result*255;

        }


    }

}
