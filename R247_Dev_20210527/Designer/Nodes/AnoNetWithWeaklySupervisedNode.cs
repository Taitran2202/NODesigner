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
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "AnoNet With Weakly Supervised", visible: false)]
    public class AnoNetWithWeaklySupervisedNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        #region properties
        private string _detail;
        public string Detail
        {
            get
            {
                return _detail;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _detail, value);
            }
        }

        private bool _interactionRegion = false;
        public bool InteractionRegion
        {
            get
            {
                return _interactionRegion;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _interactionRegion, value);
            }
        }

        private bool _isEnableTool = true;
        public bool IsEnableTool
        {
            get
            {
                return _isEnableTool;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isEnableTool, value);
            }
        }

        private bool _result;
        public bool Result
        {
            get
            {
                return _result;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _result, value);
            }
        }

        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HHomMat2D> Fixture { get; }
        public ValueNodeInputViewModel<int> Threshold { get; set; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; set; }

        public ValueNodeOutputViewModel<float> Score { get; set; }

        public AnoNetOnnx anoNetModel;
        #endregion
        static AnoNetWithWeaklySupervisedNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<AnoNetWithWeaklySupervisedNode>));
        }

        public AnoNetWithWeaklySupervisedNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.CanBeRemovedByUser = true;
            this.Name = "Anonet";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };

            this.Inputs.Add(Image);

            Fixture = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                Editor = new HHomMat2DValueEditorViewModel(),
                PortType = "Fixture"
            };
            this.Inputs.Add(Fixture);

            Threshold = new ValueNodeInputViewModel<int>()
            {
                Name = "Threshold",
                Editor = new IntegerValueEditorViewModel(),
                PortType = "Number"
            };
            this.Inputs.Add(Threshold);

            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };
            this.Outputs.Add(RegionOutput);

            Score = new ValueNodeOutputViewModel<float>()
            {
                Name = "Score",
                PortType = "float"
            };
            this.Outputs.Add(Score);
            anoNetModel = new AnoNetOnnx(dir, id);
        }

        public override void Run(object context)
        {
            HRegion region;
            float score;
            (region, score) = Segment(Image.Value, Fixture.Value, Threshold.Value, context as InspectionContext, InteractionRegion);

            Score.OnNext(score);

            var IContext = context as InspectionContext;
            if (IContext != null)
            {
                if (region != null & ShowDisplay)
                {
                    //string color = "";
                    IContext.inspection_result.AddDisplay(region, "red");
                    string message = string.Format("Score: {0}", score);
                    IContext.inspection_result.AddText(message, "red", "red", 0.0, 0.0, 12, false);
                }
            }
            RegionOutput.OnNext(region);
        }

        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    AnoNetWithWeaklySupervisedWindow wd = new AnoNetWithWeaklySupervisedWindow(anoNetModel.ImageDir, 
                                                                anoNetModel.AnnotationDir, anoNetModel.CameraDir, this, null);
                    wd.ShowDialog();
                    break;
            }
        }

        public (HRegion, float) Segment(HImage input, HHomMat2D fixture, int threshold, InspectionContext context, bool interactionRegion)
        {
            if (input != null)
            {
                if (anoNetModel.State == ModelState.Unloaded)
                {
                    anoNetModel.ReloadRecipe();
                }
                if (anoNetModel.State == ModelState.Error)
                {
                    return (null, 0.0f);
                }
                if (anoNetModel.State == ModelState.Loaded)
                {
                    var result = anoNetModel.Predict(input, fixture, threshold, context, interactionRegion);
                    return result;

                }
            }
            return (input, 0.0f);
        }
    }

   
    public class AnoNetOnnx:ONNXModel
    {
        #region properties
        public HRegion NewRegion { get; set; } = new HRegion();
        public CollectionOfregion Region { get; set; } = new CollectionOfregion();
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public TrainConfigForAnoNet TrainConfig { get; set; }

        public bool DisplayBox { get; set; } = true;
        public bool DisplayLabel { get; set; } = true;

        public string ImageDir, ModelDir, RootDir, TrainConfigPath, WeightDir, AnnotationDir, GoodDir, BadDir, CameraDir, ResultDir;


        public double ClosingCircle = 2.5;
        public double MinimunArea = 1;
        string input_name;
        int input_width,input_height;
        public bool LoadOnnx(string directory)
        {
            try
            {

                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory,TrainConfig.ModelOpitions,"model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;

                t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * 3);

                var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1,3, input_height, input_width })),
                    };

                using (var results = ONNXSession.Run(inputs))
                {

                }
                State = ModelState.Loaded;

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
        // onnx
        InferenceSession ONNXSession;
        #endregion

        public AnoNetOnnx(string dir, string id)
        {
            RootDir = System.IO.Path.Combine(dir, id, "anomaly_model");
            ModelDir = System.IO.Path.Combine(RootDir, "saved_model");
            WeightDir = System.IO.Path.Combine(RootDir, "weights");
            ImageDir = System.IO.Path.Combine(RootDir, "images");
            ResultDir = System.IO.Path.Combine(RootDir, "result");
            GoodDir = System.IO.Path.Combine(ImageDir, "good");
            BadDir = System.IO.Path.Combine(ImageDir, "bad");
            CameraDir = System.IO.Path.Combine(RootDir, "camera");
            AnnotationDir = System.IO.Path.Combine(RootDir, "annotaions");
            TrainConfigPath = System.IO.Path.Combine(RootDir, "TrainConfig.txt");

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

            if (!System.IO.Directory.Exists(WeightDir))
            {
                System.IO.Directory.CreateDirectory(WeightDir);
            }

            if (!System.IO.Directory.Exists(ResultDir))
            {
                System.IO.Directory.CreateDirectory(ResultDir);
            }

            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                System.IO.Directory.CreateDirectory(AnnotationDir);
            }

            if (!System.IO.Directory.Exists(GoodDir))
            {
                System.IO.Directory.CreateDirectory(GoodDir);
            }

            if (!System.IO.Directory.Exists(BadDir))
            {
                System.IO.Directory.CreateDirectory(BadDir);
            }

            if (!System.IO.Directory.Exists(CameraDir))
            {
                System.IO.Directory.CreateDirectory(CameraDir);
            }

            TrainConfig = TrainConfigForAnoNet.Create(TrainConfigPath);

            TrainConfig.SavedModelDir = ModelDir;
            TrainConfig.CheckpointDir = WeightDir;
            TrainConfig.MasksDir = AnnotationDir;
            TrainConfig.ImagesDir = ImageDir;
            TrainConfig.ResultDir = ResultDir;
        }

        public bool LoadRecipe(string directory)
        {
            try
            {
                if (State == ModelState.NotFound)
                {
                    return false;
                }
                return LoadOnnx(directory);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ReloadRecipe()
        {
            return LoadRecipe(ModelDir);
        }

        

        

        public (HRegion, float) Predict(HImage img, HHomMat2D fixture, int threshold, InspectionContext context, bool interactionRegion)
        {
            if (img == null || fixture == null)
            {
                return (new HRegion(), 0.0f);
            }

            HRegion regionColorSelect = new HRegion();
            regionColorSelect.GenEmptyObj();
            HTuple channels = img.CountChannels();

            HTuple message = new HTuple();
            bool use_fixture = false;
            if (fixture != null)
                use_fixture = true;
            InspectionResult inspectionResult = context.inspection_result;
            HRegion regionInspection = NewRegion;

            if (!interactionRegion)
                regionInspection = Region.Region;

            if (use_fixture)
                regionInspection = fixture.AffineTransRegion(Region.Region, "nearest_neighbor");

            int[] ModelSize = new int[2] { TrainConfig.ImageHeight, TrainConfig.ImageWidth };
            img.GetImageSize(out int originalW, out int originalH);
            float[] inputImageValues = ImagePreprocess(img, ModelSize);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();


            float score = (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).GetValue(0);

            //context.message = context.message.TupleConcat(message);
            var map = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;

            regionColorSelect = BuildRegions(map, threshold / 100.0f,originalW,originalH);
            return (regionColorSelect, score);
        }

        public float[] ImagePreprocess(HImage img, int[] targetSize)
        {
            
            int nw = targetSize[0];
            int nh = targetSize[1];
            if (img.CountChannels() == 1 && TrainConfig.Channels == 3)
            {
                img = img.Compose3(img, img);
            }
            var resizedImg = img.ZoomImageSize(nw, nh, "bilinear");
            resizedImg = resizedImg.ConvertImageType("float")/255.0;
            return Processing.HalconUtils.HImageToFloatArray(resizedImg, 3, out int input_width, out int input_height,swapChannel:true);
           
        }

        public unsafe HRegion BuildRegions(Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> results, float threshold,int originalW,int originalH)
        {
            var hotmap = results;
            
            HImage hotmapHalcon = new HImage("float", results.Dimensions[2], results.Dimensions[1]);
            IntPtr pointerResult = hotmapHalcon.GetImagePointer1(out HTuple type, out HTuple _, out HTuple _);
            Marshal.Copy(results.ToArray(), 0, pointerResult, input_height * input_width);
            if (results.Dimensions[2] != originalW || results.Dimensions[1] != originalH)
            {
                HImage resizedHotmapHalcon = hotmapHalcon.ZoomImageSize(originalW, originalH, "nearest_neighbor");
                return resizedHotmapHalcon.Threshold(threshold, double.MaxValue);
            }

            return hotmapHalcon.Threshold(threshold, double.MaxValue);
        }
    }

    public class TrainConfigForAnoNet
    {
        public int ImageWidth { get; set; } = 512;
        public int ImageHeight { get; set; } = 512;
        public int Channels { get; set; } = 3;
        public int BatchSize { get; set; } = 16;
        public int EpochsForSeg { get; set; } = 10;
        public int EpochsForCls { get; set; } = 5;
        public string CheckpointDir { get; set; }
        public string SavedModelDir { get; set; }
        public bool TrainSeg { get; set; } = true;
        public bool Augmentation { get; set; } = true;
        public bool Monitor { get; set; } = true;
        public string Transfer { get; set; } = "scratch";
        public string ModelOpitions { get; set; }
        public string ImagesDir { get; set; }
        public string MasksDir { get; set; }
        public string ResultDir { get; set; }
        

        public void Save(string path)
        {
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
        public static TrainConfigForAnoNet Create(string path)
        {
            if (System.IO.File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<TrainConfigForAnoNet>(json);
                return config;
            }
            else
            {
                return new TrainConfigForAnoNet();
            }

        }
        private TrainConfigForAnoNet()
        {

        }
    }
}




