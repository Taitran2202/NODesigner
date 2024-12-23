using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeNetwork.Toolkit.ValueNode;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using Microsoft.ML.OnnxRuntime;
using System.Windows.Controls;
using NOVisionDesigner.Designer.Extensions;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "RYOLO",visible:true)]
    public class RYOLO : BaseNode
    {
        static RYOLO()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<RYOLO>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        #region properties
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeOutputViewModel<RYOLOOutput[]> Fixture { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public RYOLOONNXRuntime runtime;
        public RYOLOTrainConfig TrainConfig;
        #endregion

        public string ImageDir, AnnotationDir, ModelDir, RootDir, TrainConfigDir;
        public double OriginalRow { get; set; }
        public double OriginalColumn { get; set; }
        public override void OnInitialize()
        {
            runtime = new RYOLOONNXRuntime(ModelDir);
        }
        public override void Dispose()
        {
            base.Dispose();
            runtime.Dispose();
        }
        [HMIProperty("RYOLO Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public RYOLO(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = Path.Combine(dir, id, "RYOLO");
            ImageDir = Path.Combine(RootDir, "images");
            ModelDir = Path.Combine(RootDir, "model");
            AnnotationDir = Path.Combine(RootDir, "annotations");
            TrainConfigDir = System.IO.Path.Combine(RootDir, "TrainConfig.txt");
            if (!Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            if (!Directory.Exists(ModelDir))
            {
                Directory.CreateDirectory(ModelDir);
            }
            if (!Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            TrainConfig = RYOLOTrainConfig.Create(RootDir);
            if (!System.IO.File.Exists(TrainConfigDir))
            {
                TrainConfig.Save();
            }
            this.CanBeRemovedByUser = true;
            this.Name = "RYOLO";

            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);

            Fixture = new ValueNodeOutputViewModel<RYOLOOutput[]>()
            {
                Name = "ListFixtures",
                PortType = "RYOLOOutput[]"
            };
            this.Outputs.Add(Fixture);
        }
        public void Record()
        {
            var image = Image.Value.Clone();
            var filename = Functions.RandomFileName(TrainConfig.ImageDir);
            image.WriteImage("png", 0, filename + ".png");
        }
        public override void OnLoadComplete()
        {
            //runtime = new RYOLOONNXRuntime(ModelDir);
            if (runtime == null)
            {
                return;
            }
            if (runtime.State == ModelState.NotFound)
            {
                return;
            }
            if (runtime.State == ModelState.Unloaded)
            {
                runtime.LoadRecipe(ModelDir);
            }
            if (runtime.State == ModelState.Loaded)
            {
                var img = new HImage("byte", 8192, 900);
                var result = runtime.PredictV2(img);
            }
        }
        public override void Run(object context)
        {
            if (RecordMode.Value == ToolRecordMode.RecordOnly)
            {
                Record();
                return;
            }
            if (RecordMode.Value == ToolRecordMode.RecordAndRun)
            {
                Record();
            }
            Fixture.OnNext(RunInside(Image.Value, context as InspectionContext));
        }

        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    RYOLOEditorWindow wd = new RYOLOEditorWindow(this);
                    listAugmentation = wd.listAugmentation;
                    wd.ShowDialog();
                    break;
            }
        }

        public RYOLOOutput[] RunInside(HImage img, InspectionContext context)
        {
            RYOLOOutput[] output;
            if (img == null)
            {
                return null;
            }
            if (runtime.State == ModelState.NotFound)
            {
                return null;
            }
            if (runtime.State == ModelState.Unloaded)
            {
                runtime.LoadRecipe(ModelDir);
            }
            if (runtime.State == ModelState.Loaded)
            {

                var result = runtime.PredictV2(img);
                if (result.Count == 0)
                {
                    return null;
                }
                
                var reshaped = result[0][0].reshape(new int[] { -1, 6 }); // bs x cls_id x (num_boxes, 5) voi 5: xc, yc, cos, sin, cls_id
                output = new RYOLOOutput[reshaped.shape[0]];
                for (int i = 0; i < reshaped.shape[0]; i++)
                {
                    output[i] = new RYOLOOutput() { X = (float)reshaped[i][0], Y = (float)reshaped[i][1], Sin = (float)reshaped[i][2], Cos = (float)reshaped[i][3], Confidence = (float)reshaped[i][4], ClassId = (float)reshaped[i][5] };
                }
                if (reshaped.shape is null)
                    return null;
                if (ShowDisplay)
                {
                    for (int i = 0; i < reshaped.shape[0]; i++)
                    {
                        var row = (double)(float)reshaped[i][1];
                        var col = (double)(float)reshaped[i][0];
                        var circle = new HRegion();
                        circle.GenCircle(row, col, 320.0);
                        context.inspection_result.AddRegion(circle, "blue");
                    }
                }
                
                return output;



            }
            return null;
        }   
    }
    public class RYOLOOutput
    {
        public double X { get; set; }
        public double Y{ get; set; }
        public double Cos { get; set; }
        public double Sin { get; set; }
        public double Confidence { get; set; }
        public double ClassId { get; set; }
    }
    public class RYOLOTrainConfig
    {
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public double LR_INIT { get; set; } = 1e-2;
        public double LR_END { get; set; } = 1e-6;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double LABEL_SMOOTHING { get; set; } = 0.2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        public int WARMUP_EPOCHS { get; set; } = 2;
        public int WARMUP_STEPS { get; set; } = 0;
        public ObservableCollection<YOLOClass> Classes { get; set; } = new ObservableCollection<YOLOClass>();
        public bool TRAIN_YOLO_TINY { get; set; } = false;
        public int EPOCHS { get; set; } = 100;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public string WeightDir { get; set; }
        public string PreTrainModelPath { get; set; }
        public string TrainLogDir { get; set; }
        public bool DATA_AUG { get; set; } = false;
        public bool UseMosaicImage { get; set; } = false;
        public int BATCH_SIZE { get; set; } = 4;
        /// <summary>
        /// adam, sgd, rmsprop
        /// </summary>
        public string Optimizer { get; set; } = "adam";
        public string TrainningType { get; set; } = "transfer";
        public bool VisualizeLearningProcess { get; set; } = true;
        public string ImageDir { get; set; }
        public string ResultDir { get; set; }
        /// <summary>
        /// YOLO RYOLO
        /// </summary>
        public string ModelType { get; } = "RYOLO";
        public string TrainConfigDir { get; set; }
        public string BaseDir { get; set; }
        public void Save()
        {
            System.IO.File.WriteAllText(TrainConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public static RYOLOTrainConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                try
                {
                    var config = JsonConvert.DeserializeObject<RYOLOTrainConfig>(json);
                    config.ApplyRelativeDir(BaseDir);
                    config.Save();
                    return config;
                }
                catch (Exception ex)
                {
                    return new RYOLOTrainConfig(BaseDir);
                }


            }
            else
            {
                return new RYOLOTrainConfig(BaseDir);
            }

        }
        private RYOLOTrainConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();
        }
        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = Path.Combine(BaseDir, "images");
            SavedModelDir = Path.Combine(BaseDir, "model");
            AnnotationDir = Path.Combine(BaseDir, "annotations");
            WeightDir = System.IO.Path.Combine(SavedModelDir, "weights");
            
            ResultDir = System.IO.Path.Combine(SavedModelDir, "TrainingResult");
            
            TrainConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            if (!Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            if (!Directory.Exists(SavedModelDir))
            {
                Directory.CreateDirectory(SavedModelDir);
            }
            if (!Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            if (!System.IO.Directory.Exists(WeightDir))
            {
                System.IO.Directory.CreateDirectory(WeightDir);
            }
            if (!System.IO.Directory.Exists(ResultDir))
            {
                System.IO.Directory.CreateDirectory(ResultDir);
            }
            this.BaseDir = BaseDir;
        }
        private RYOLOTrainConfig()
        {

        }
    }
    public class RYOLOONNXRuntime : ONNXModel
    {
        int input_height, input_width;
        //int numClasses;
        public string ModelDir { get; internal set; }
        public RYOLOONNXRuntime(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }

        InferenceSession ONNXSession;
        string input_name;
        public void Dispose()
        {
            ONNXSession.Dispose();
        }
        public bool LoadOnnx(string directory)
        {
            try
            {

                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                //input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                input_name = "input_yolov4";


                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[2];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[1];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;

                t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * 3);

                var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, input_height, input_width, 3 })),
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



        //public int[] ModelSize = new int[] { 416, 416 };
        //public int originalW, originalH;
        public NDArray ResizeBack(NDArray boxes, int targetW, int targetH)
        {
            float resizeRatio = Math.Min((float)input_width / targetW, (float)input_width / targetH);
            float dw = (input_width - resizeRatio * targetW) / 2;
            float dh = (input_width - resizeRatio * targetH) / 2;

            boxes["..., 0:4:2"] = 1.0 * (boxes["..., 0:4:2"] - dw) / resizeRatio;
            boxes["..., 1:4:2"] = 1.0 * (boxes["..., 1:4:2"] - dh) / resizeRatio;
            return boxes;
        }
        public Dictionary<int, Dictionary<int, NDArray>> Predict(HImage img, float iouThreshold=5.0f, float confidenceThreshold=0.5f)
        {
            var _iouThreshold = iouThreshold / 100.0f;
            var _confidenceThreshold = confidenceThreshold / 100.0f;
            img.GetImageSize(out int originalW, out int orignalH);
            var inputImageValues = ImagePreprocess(img).ToArray<float>();

            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,input_height,input_width,3 }))
            };

            var results = ONNXSession.Run(inputs).ToList();
            NDArray[] outputs = new NDArray[results.Count];
            int numClasses=0;
            for (int i = 3; i < results.Count; i++)
            {
                var name = results[i].Name;
                var shape = ONNXSession.OutputMetadata[name].Dimensions;
                var TensorOutput = results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                outputs[i] = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { 1, -1, shape[3] }));
                numClasses = shape[3] - 5;
            }

            var result = np.concatenate(outputs, 1);

            //var detectionResult = BuildBoxes(result, numClasses, originalW, orignalH);
            //var bboxes = BatchNonMaximalSuppression(detectionResult[0], confidenceThreshold / 100f, iouThreshold / 100f);

            var detectionResult = BuildBoxes(result, numClasses, originalW, orignalH);
            Dictionary<int, Dictionary<int, NDArray>> detectionResultDict = NonMaxSuppression(detectionResult, numClasses, _iouThreshold, _confidenceThreshold);
            //bboxes[":,0"] = bboxes[":,0"] * originalW / input_width;
            //bboxes[":,1"] = bboxes[":,1"] * orignalH / input_height;
            //bboxes[":,2"]= bboxes[":,2"] * originalW / input_width;
            //bboxes[":,3"]= bboxes[":,3"] * orignalH / input_height;
            //Dictionary<int, Dictionary<int, NDArray>> detectionResultDict = NonMaxSuppression(detectionResult, numClasses, maxOutputSize, _iouThreshold, _confidenceThreshold);
            return detectionResultDict;
        }
        public Dictionary<int, Dictionary<int, NDArray>> NonMaxSuppression(NDArray inputs, int numClasses,
         float iOUThreshold, float confidenceThreshold)
        {
            int[] shape = inputs.shape;
            Dictionary<int, Dictionary<int, NDArray>> boxesDictsWithBatch = new Dictionary<int, Dictionary<int, NDArray>>();
            for (int i = 0; i < shape[0]; i++)
            {
                NDArray boxes = inputs[i];
                NDArray mask = boxes[":, 4"] > confidenceThreshold;
                boxes = boxes[boxes[":, 4"] > confidenceThreshold];
                if (boxes.size == 0)
                {
                    continue;
                }
                NDArray classes;
                if (numClasses != 1)
                {
                    classes = np.argmax(boxes[":, 5:"], -1);
                }
                else
                {
                    classes = np.zeros(boxes[":, 5:"].size);
                }

                classes = np.expand_dims(classes, -1).astype(NPTypeCode.Float);
                boxes = np.concatenate(new NDArray[] { boxes[":, :5"], classes }, -1);
                Dictionary<int, NDArray> boxesDict = new Dictionary<int, NDArray>();
                for (int cls = 0; cls < numClasses; cls++)
                {
                    mask = boxes[":, 5"] == cls;
                    var condition = mask.GetData<bool>().Where(data => data == true).ToList();
                    if (condition.Count > 0)
                    {
                        NDArray classBoxes = boxes[boxes[":, 5"] == cls];
                        if (classBoxes.size == 0)
                            continue;
                        NDArray boxesCoords = classBoxes[":,:4"];
                        NDArray boxesConfScores = classBoxes[":, 4"].astype(NPTypeCode.Float);


                        NDArray x1 = boxesCoords[":, 0"].astype(NPTypeCode.Float)-input_width/2;
                        NDArray y1 = boxesCoords[":, 1"].astype(NPTypeCode.Float)-input_height/2;
                        NDArray x2 = boxesCoords[":, 0"].astype(NPTypeCode.Float)+input_width/2;
                        NDArray y2 = boxesCoords[":, 1"].astype(NPTypeCode.Float)+input_height/2;
                        NDArray areas = np.multiply(np.subtract(x2, x1), np.subtract(y2, y1));
                        NDArray order = np.argsort<NDArray>(boxesConfScores);

                        List<NDArray> boxesKeep = new List<NDArray>();
                        while (order.size > 0)
                        {
                            int idx = order[-1];
                            NDArray chosenBox = classBoxes[idx].astype(NPTypeCode.Float);
                            boxesKeep.Add(chosenBox);
                            order = order[":-1"];

                            if (order.size == 0)
                            {
                                break;
                            }

                            NDArray xx1 = x1[order].astype(NPTypeCode.Float);
                            NDArray yy1 = y1[order].astype(NPTypeCode.Float);
                            NDArray xx2 = x2[order].astype(NPTypeCode.Float);
                            NDArray yy2 = y2[order].astype(NPTypeCode.Float);

                            xx1 = np.maximum(xx1, x1[idx]);
                            yy1 = np.maximum(yy1, y1[idx]);
                            xx2 = np.minimum(xx2, x2[idx]);
                            yy2 = np.minimum(yy2, y2[idx]);


                            NDArray w = np.subtract(xx2, xx1);
                            NDArray h = np.subtract(yy2, yy1);

                            w = np.clip(w, 0.0, float.MaxValue);
                            h = np.clip(h, 0.0, float.MaxValue);

                            NDArray inter = np.multiply(w, h);
                            NDArray remAreas = areas[order].astype(NPTypeCode.Float);
                            NDArray union = np.add(np.subtract(remAreas, inter), areas[idx]);
                            NDArray IoU = np.divide(inter, union);
                            mask = IoU < iOUThreshold;
                            if (mask.size == 1 && mask.GetData<bool>()[0])
                            {
                            }
                            else
                            {
                                order = order[mask == true];
                            }
                        }
                        NDArray boxesClsKeep = np.concatenate(boxesKeep.ToArray());
                        boxesDict.Add(cls, boxesClsKeep);
                    }
                }
                boxesDictsWithBatch.Add(i, boxesDict);
            }
            return boxesDictsWithBatch;
        }
        public NDArray BuildBoxes(NDArray inputs, int numClasses, int targetW, int targetH)
        {
            float resizeRatio = Math.Min((float)input_width / targetW, (float)input_height / targetH);
            float dw = (input_width - resizeRatio * targetW) / 2;
            float dh = (input_width - resizeRatio * targetH) / 2;

            NDArray detectResults = inputs;
            int[] shape = detectResults.shape;
            Shape reshape = new Shape(new int[] { shape[0], shape[1], 1 });

            NDArray centerX = detectResults[":,:,0"].reshape(reshape);
            NDArray centerY = detectResults[":,:,1"].reshape(reshape);
            NDArray width = ((detectResults[":,:,1"] + 112) - (detectResults[":,:,0"] - 112)).reshape(reshape);
            NDArray height = ((detectResults[":,:,1"] + 112) - (detectResults[":,:,0"] - 112)).reshape(reshape);
            NDArray confidence = detectResults[":,:,4"].reshape(reshape);

            reshape = new Shape(new int[] { shape[0], shape[1], numClasses });
            NDArray classes = detectResults[":,:,5:"].reshape(reshape);

            NDArray arr2 = np.ones(new int[] { shape[0], shape[1], 1 }) * 2;
            NDArray widthDiv2 = np.divide(width, arr2);
            NDArray heightDiv2 = np.divide(height, arr2);

            NDArray arrResizeRatio = np.ones(new int[] { shape[0], shape[1], 1 }) * resizeRatio;
            NDArray arrDW = np.ones(new int[] { shape[0], shape[1], 1 }) * dw;
            NDArray arrDH = np.ones(new int[] { shape[0], shape[1], 1 }) * dh;

            NDArray topLeftX = np.subtract(centerX, widthDiv2);
            topLeftX = np.divide(np.subtract(topLeftX, arrDW), arrResizeRatio).astype(NPTypeCode.Float);
            NDArray topLeftY = np.subtract(centerY, heightDiv2);
            topLeftY = np.divide(np.subtract(topLeftY, arrDH), arrResizeRatio).astype(NPTypeCode.Float);
            NDArray bottomRightX = np.add(centerX, widthDiv2);
            bottomRightX = np.divide(np.subtract(bottomRightX, arrDW), arrResizeRatio).astype(NPTypeCode.Float);
            NDArray bottomRightY = np.add(centerY, heightDiv2);
            bottomRightY = np.divide(np.subtract(bottomRightY, arrDH), arrResizeRatio).astype(NPTypeCode.Float);

            NDArray[] nDArray = new NDArray[]
            {
                topLeftX, topLeftY, bottomRightX, bottomRightY, confidence, classes
            };

            NDArray boxes = np.concatenate(nDArray, -1);
            return boxes;
        }

        public NDArray ResizeBackV2(NDArray boxes, int targetW, int targetH)
        {
            float resizeRatio = Math.Min((float)input_width / targetW, (float)input_height / targetH);
            float dw = (input_width - resizeRatio * targetW) / 2;
            float dh = (input_height - resizeRatio * targetH) / 2;

            boxes["..., 0"] = 1.0 * (boxes["..., 0"] - dw) / resizeRatio;
            boxes["..., 1"] = 1.0 * (boxes["..., 1"] - dh) / resizeRatio;
            return boxes;
        }

        public Dictionary<int, Dictionary<int, NDArray>> NonMaxSuppressionV2(NDArray inputs, int numClasses,
         float iOUThreshold=5.0f, float confidenceThreshold=0.5f)
        {
            int[] shape = inputs.shape;
            Dictionary<int, Dictionary<int, NDArray>> boxesDictsWithBatch = new Dictionary<int, Dictionary<int, NDArray>>();
            for (int i = 0; i < shape[0]; i++)
            {
                NDArray boxes = inputs[i];
                NDArray mask = boxes[":, 4"] > confidenceThreshold;
                boxes = boxes[boxes[":, 4"] > confidenceThreshold];
                if (boxes.size == 0)
                {
                    continue;
                }
                NDArray classes;
                if (numClasses != 1)
                {
                    classes = np.argmax(boxes[":, 5:"], -1);
                }
                else
                {
                    classes = np.zeros(boxes[":, 5:"].size);
                }

                classes = np.expand_dims(classes, -1).astype(NPTypeCode.Float);
                boxes = np.concatenate(new NDArray[] { boxes[":, :5"], classes }, -1);
                Dictionary<int, NDArray> boxesDict = new Dictionary<int, NDArray>();
                for (int cls = 0; cls < numClasses; cls++)
                {
                    mask = boxes[":, 5"] == cls;
                    var condition = mask.GetData<bool>().Where(data => data == true).ToList();
                    if (condition.Count > 0)
                    {
                        NDArray classBoxes = boxes[boxes[":, 5"] == cls];
                        if (classBoxes.size == 0)
                            continue;
                        NDArray boxesCoords = classBoxes[":,:4"];
                        NDArray boxesConfScores = classBoxes[":, 4"].astype(NPTypeCode.Float);

                        NDArray order = np.argsort<NDArray>(boxesConfScores);

                        List<NDArray> boxesKeep = new List<NDArray>();
                        while (order.size > 0)
                        {
                            int idx = order[-1];
                            NDArray chosenBox = classBoxes[idx];
                            boxesKeep.Add(chosenBox);
                            order = order[":-1"];

                            if (order.size == 0)
                            {
                                break;
                            }

                            NDArray box = classBoxes[order].astype(NPTypeCode.Float);
                            NDArray IoU = np.sum(np.abs(box["..., :2"] - chosenBox["..., :2"]), axis:-1).astype(NPTypeCode.Float);
                            mask = IoU > iOUThreshold;
                            if (mask.size == 1 && mask.GetData<bool>()[0])
                            {
                            }
                            else
                            {
                                order = order[mask == true];
                            }
                        }
                        NDArray boxesClsKeep = np.concatenate(boxesKeep.ToArray());
                        boxesDict.Add(cls, boxesClsKeep);
                    }
                }
                boxesDictsWithBatch.Add(i, boxesDict);
            }
            return boxesDictsWithBatch;
        }

        public Dictionary<int, Dictionary<int, NDArray>> PredictV2(HImage img)
        {
            img.GetImageSize(out int originalW, out int orignalH);
            var inputImageValues = ImagePreprocess(img).ToArray<float>();

            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,input_height,input_width,3 }))
            };

            var results = ONNXSession.Run(inputs).ToList(); // b x w x h x c
            //NDArray boxes = new NDArray(NPTypeCode.Float);
            NDArray[] outputs = new NDArray[3];
            int numClasses = 0;
            for (int i = 0; i < 3; i++)
            {
                var name = results[i].Name;
                var shape = ONNXSession.OutputMetadata[name].Dimensions; // b x w x h x c

                var TensorOutput = results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                outputs[i] = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { 1, -1, shape[3] }));
                numClasses = shape[3] - 5;
            }

            var result = np.concatenate(outputs, 1); // b x num_boxes x c

            result = ResizeBackV2(result, originalW, orignalH);

            Dictionary<int, Dictionary<int, NDArray>> detectionResultDict = NonMaxSuppressionV2(result, numClasses,80f,0.5f);

            return detectionResultDict;
        }
        
        public NDArray ImagePreprocess(HImage img)
        {
            img.GetImageSize(out int originalW, out int originalH);
            int ih = input_height;
            int iw = input_width;

            float scale = Math.Min((float)iw / originalW, (float)ih / originalH);
            int nw = (int)(scale * originalW);
            int nh = (int)(scale * originalH);

            var resizedImg = img.ZoomImageSize(nw, nh, "bilinear");

            byte[] arrInput = new byte[nw * nh * 3];

            // convert HImage to NDArray
            string type;
            int width, height;

            if (resizedImg.CountChannels() == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                resizedImg.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                Marshal.Copy(pointerR, arrInput, 0, nw * nh);
                Marshal.Copy(pointerG, arrInput, nw * nh, nw * nh);
                Marshal.Copy(pointerB, arrInput, 2* nw * nh, nw * nh);
            }
            else
            {
                IntPtr pointerGray;
                pointerGray = resizedImg.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, arrInput, 0, nw * nh);
                Marshal.Copy(pointerGray, arrInput, nw * nh, nw * nh);
                Marshal.Copy(pointerGray, arrInput, 2* nw * nh, nw * nh);
            }
            NDArray arrFinal = (new NDArray(arrInput, new NumSharp.Shape(1, 3, nh, nw), order: 'F'));
            NDArray inputImageValues = np.transpose(arrFinal, new int[] { 0, 2, 3, 1 });
            NDArray currResizedImg = inputImageValues[0];


            Shape shape = new Shape(new int[] { ih, iw, 3 });
            NDArray imgPaded = np.full(shape, 0.0);

            int dw = (iw - nw) / 2;
            int dh = (ih - nh) / 2;
            string slice = string.Format("{0}:{1},{2}:{3},:", dh, nh + dh, dw, nw + dw);
            imgPaded[slice] = currResizedImg;
            imgPaded.astype(NPTypeCode.Float);
            imgPaded = imgPaded / 255.0f;

            return np.expand_dims(imgPaded.astype(NPTypeCode.Float), 0);
        }

        

        public List<int> NMS(NDArray dets, double thresh)
        {
            if (dets.size == 0)
            {
                return new List<int>();
            }
            var x1 = dets[":,0"];
            var y1 = dets[":, 1"];
            var x2 = dets[":, 2"];
            var y2 = dets[":, 3"];
            var scores = dets[":, 4"];

            var areas = (x2 - x1 + 1) * (y2 - y1 + 1);
            var order = scores.argsort<int>();
            var keep = new List<int>();
            while (order.size > 1)
            {
                var i = order[0];
                keep.Add(i);
                var xx1 = np.maximum(x1[i], x1[order["1:"]]);
                var yy1 = np.maximum(y1[i], y1[order["1:"]]);
                var xx2 = np.minimum(x2[i], x2[order["1:"]]);
                var yy2 = np.minimum(y2[i], y2[order["1:"]]);

                var w = np.maximum(0.0, xx2 - xx1 + 1);
                var h = np.maximum(0.0, yy2 - yy1 + 1);
                var inter = w * h;
                var ovr = inter / (areas[i] + areas[order["1:"]] - inter);

                var inds = np.nonzero(ovr[ovr < thresh])[0];
                order = order[inds + 1];
            };
            return keep;
        }
        
        public NDArray xywh2xyxy(NDArray box)
        {
            var x1 = box["..., 0: 1"] - box["..., 2: 3"] / 2;
            var y1 = box["..., 1: 2"] - box["..., 3: 4"] / 2;
            var x2 = box["..., 0: 1"] + box["..., 2: 3"] / 2;
            var y2 = box["..., 1: 2"] + box["..., 3: 4"] / 2;
            var output = np.concatenate(new NDArray[] { x1, y1, x2, y2, box["..., 5:6"] }, axis: -1);
            return output;
        }

    }
}
