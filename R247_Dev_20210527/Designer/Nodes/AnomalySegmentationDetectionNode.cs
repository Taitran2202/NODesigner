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

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "Anomaly Detection V2", visible: false)]
    public class AnomalySegmentationDetectionNode : BaseNode
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
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; set; }
        public ValueNodeInputViewModel<int> Threshold { get; }
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public TrainConfig TrainConfig { get; set; }

        public string ImageDir, ModelDir, RootDir,TrainConfigDir;
        
        public ONNXAnomalySegmentation segmentation;
        #endregion

        static AnomalySegmentationDetectionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<AnomalySegmentationDetectionNode>));
        }

        public AnomalySegmentationDetectionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            ModelDir = System.IO.Path.Combine(RootDir, "model");
            ImageDir = System.IO.Path.Combine(RootDir, "images");
            TrainConfigDir = System.IO.Path.Combine(RootDir, "TrainConfig.txt");
            
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }

            if (!System.IO.Directory.Exists(ImageDir))
            {
                System.IO.Directory.CreateDirectory(ImageDir);
            }

            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
            TrainConfig = TrainConfig.Create(TrainConfigDir,ModelDir,ImageDir);
            if (!System.IO.File.Exists(TrainConfigDir))
            {
                TrainConfig.Save(TrainConfigDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "AS Model";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "RegionInput",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };
            this.Outputs.Add(RegionOutput);
            Threshold = new ValueNodeInputViewModel<int>()
            {
                Name = "Threshold",
                PortType = "Fixture",
                Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(Threshold);
            
        }
        public override void OnInitialize()
        {
            segmentation = new ONNXAnomalySegmentation(ModelDir);
        }
        public override void Run(object context)
        {
            HRegion inspected_region=null,region=null;
            if (TrainConfig.UseMask)
            {
                if(RegionInput.Value!=null)
                    inspected_region = RegionInput.Value.MoveRegion(-TrainConfig.Mask.row1, -TrainConfig.Mask.col1);
                else
                    inspected_region =null;
                var image_cropped = Image.Value.CropRectangle1(TrainConfig.Mask.row1, TrainConfig.Mask.col1, TrainConfig.Mask.row2, TrainConfig.Mask.col2);
                region = Segment(image_cropped, Threshold.Value, inspected_region).MoveRegion(TrainConfig.Mask.row1, TrainConfig.Mask.col1);
            }
            else
            {
                region = Segment(Image.Value, Threshold.Value, inspected_region);
            }
            
            var IContext = context as InspectionContext;
            if(RegionInput.Value != null)
            {
                IContext.inspection_result.AddDisplay(RegionInput.Value, "green");
            }
            if (IContext != null)
            {
                if (region != null & ShowDisplay)
                {
                    IContext.inspection_result.AddRegion(region, "red");
                    //base.designer.display.HalconWindow.DispObj(region);
                }
            }
            RegionOutput.OnNext(region);
        }


        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    AnomalySegmentationDetectionEditorWindow wd = new AnomalySegmentationDetectionEditorWindow(ImageDir, ModelDir, null, this, listAugmentation);
                    listAugmentation = wd.listAugmentation;
                    wd.ShowDialog();
                    break;
            }
        }

        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public HRegion Segment(HImage input, int threshold, HRegion regionInput)
        {
            if (input != null)
            {
                if (segmentation.State == ModelState.Unloaded)
                {
                    segmentation.LoadRecipe(segmentation.ModelDir);
                }
                if (segmentation.State == ModelState.Error)
                {
                    return null;
                }
                if (segmentation.State == ModelState.Loaded)
                {
                    var result = segmentation.Infer(input,threshold).ClosingCircle(2.5).Intersection(regionInput).Connection();
                    return result;

                }
            }
            //var region = new HRegion();
            //region.GenEmptyRegion();
            return input;
        }
    }
    public class ONNXAnomalySegmentation : ONNXModel
    {
        public ONNXAnomalySegmentation(string dir)
        {
            ModelDir = System.IO.Path.Combine(dir, "saved_model");
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name;
        public NDArray ImagePreprocess(HImage img, int[] targetSize)
        {

            int nw = targetSize[0];
            int nh = targetSize[1];

            var resizedImg = img.ZoomImageSize(nw, nh, "bilinear");
            byte[] arrInput = new byte[nw * nh * 3];
            // convert HImage to NDArray
            string type;
            int width, height;
            IntPtr pointerR, pointerG, pointerB;
            resizedImg.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
            Marshal.Copy(pointerR, arrInput, 0, nw * nh);
            Marshal.Copy(pointerG, arrInput, nw * nh, nw * nh);
            Marshal.Copy(pointerB, arrInput, 2 * nw * nh, nw * nh);

            NDArray arrFinal = (new NDArray(arrInput, new NumSharp.Shape(1, 3, nh, nw), order: 'F'));
            NDArray inputImageValues = np.transpose(arrFinal, new int[] { 0, 2, 3, 1 });
            inputImageValues = inputImageValues / 255.0f;
            return inputImageValues;
        }
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
                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
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
        public HRegion Infer(HImage imgInput,double threshold)
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
            imgInput.GetImageSize(out int originalW,out int originalH);
            var image_resize = imgInput.ZoomImageSize(input_width, input_height, "constant").ConvertImageType("float") / 255.0;
            var array_final = Processing.HalconUtils.HImageToFloatArray(image_resize, num_channel, out int _, out int _);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;

            var result = new HImage("float", TensorOutput.Dimensions[1], TensorOutput.Dimensions[2]);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), 0, pointerResult, width * height);
            //crop if image was padded
            var resultRegion = result.ZoomImageSize(originalW, originalH, "constant").GaussFilter(3).Threshold(threshold,double.MaxValue);

            return resultRegion;

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
    }
    
    public class RectangleMask
    {
        public int row1 { get; set; }
        public int col1 { get; set; }
        public int row2 { get; set; }
        public int col2 { get; set; }
        public RectangleMask()
        {

        }
    }
    public class TrainConfig
    {
        public int Epoch { get; set; } = 100;
        public string SaveModelDir { get; set; }
        public string WeightDir { get; set; }
        public string PreTrainModelPath { get; set; }
        public string PATH { get; set; }
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public double LEAKYRELU_ALPHA { get; set; } = 0.1;
        public string Precision { get; set; } = "float32";
        public string DatasetType { get; set; } = "Sequence";
        public bool EarlyStopping { get; set; } = false;
        public RectangleMask Mask { get; set; } = new RectangleMask();
        public bool UseMask { get; set; }
        public void Save(string dir)
        {
            System.IO.File.WriteAllText(dir, JsonConvert.SerializeObject(this));
        }
        public static TrainConfig Create(string dir,string modelDir,string imageDir)
        {
           
            if (System.IO.File.Exists(dir))
            {
                var json = File.ReadAllText(dir);
                var config = JsonConvert.DeserializeObject<TrainConfig>(json);
                return config;
            }
            else
            {
                return new TrainConfig(modelDir,imageDir);
            }

        }
        private TrainConfig(string modelDir,string imageDir)
        {
            SaveModelDir = System.IO.Path.Combine(modelDir, "saved_model");
            WeightDir = System.IO.Path.Combine(modelDir, "weights");
            PreTrainModelPath = System.IO.Path.Combine("Designer", "Python", "AnomalySegmentationDetection", "weights", "vgg16_weights.h5");
            PATH = imageDir;
           
        }
        private TrainConfig()
        {

        }
    }
}
