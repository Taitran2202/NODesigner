﻿using DynamicData;
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
    [NodeInfo("Deep Learning", "RYOLOv4",visible:true)]
    public class RYOLOv4 : BaseNode
    {
        static RYOLOv4()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<RYOLOv4>));
        }

        #region properties
        public ValueNodeInputViewModel<HImage> Image { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> MaxOutputSize { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> IOUThreshold { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> ConfidenceThreshold { get; set; }
        public ValueNodeOutputViewModel<Rect2[]> Result { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        [HMIProperty("RYOLOv4 editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public RYOLOV4NONNXRuntime runtime;
        public RYOLOV4TrainConfig TrainConfig;
        #endregion
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public string RootDir;
        public override void OnInitialize()
        {
            runtime = new RYOLOV4NONNXRuntime(TrainConfig.SavedModelDir);
        }

        public RYOLOv4(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {

            this.Name = "RYOLOv4";

            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            MaxOutputSize = new ValueNodeInputViewModel<int>()
            {
                Name = "max output size",
                Editor = new IntegerValueEditorViewModel(),
                PortType = "Number"
            };
            this.Inputs.Add(MaxOutputSize);

            IOUThreshold = new ValueNodeInputViewModel<int>()
            {
                Name = "iou threshold",
                Editor = new IntegerValueEditorViewModel(),
                PortType = "Number"
            };
            this.Inputs.Add(IOUThreshold);

            ConfidenceThreshold = new ValueNodeInputViewModel<int>()
            {
                Name = "confidence threshold",
                Editor = new IntegerValueEditorViewModel(),
                PortType = "Number"
            };
            this.Inputs.Add(ConfidenceThreshold);
            
            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);

            Result = new ValueNodeOutputViewModel<Rect2[]>()
            {
                Name = "OuputBoxes",
                PortType = "Rect2[]"
            };
            this.Outputs.Add(Result);

            RootDir = Path.Combine(dir, id, "RYOLOv4");
            TrainConfig = RYOLOV4TrainConfig.Create(RootDir);
        }
        public void Record()
        {
            var image = Image.Value.Clone();
            var filename = Functions.RandomFileName(TrainConfig.ImageDir);
            image.WriteImage("png", 0, filename + ".png");
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
            Result.OnNext(RunInside(Image.Value, MaxOutputSize.Value, IOUThreshold.Value, ConfidenceThreshold.Value, context as InspectionContext));
        }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    RYOLOv4Window wd = new RYOLOv4Window(this);
                    listAugmentation = wd.listAugmentation;
                    wd.ShowDialog();
                    break;
            }
        }

        public Rect2[] RunInside(HImage img,int maxOutputSize, int iouThreshold, int confidenceThreshold, InspectionContext context)
        {
            if (img == null)
            {
                return null;
            }
            if (runtime.State == ModelState.NotFound)
            {
                return null;
            }
            if (runtime.State ==ModelState.Unloaded)
            {
                runtime.LoadRecipe(TrainConfig.SavedModelDir);
            }
            if(runtime.State == ModelState.Loaded)
            {                
                // new code
                var result = runtime.PredictRYOLO(img, TrainConfig.Classes.Count, maxOutputSize, iouThreshold, confidenceThreshold);
                int[] shape = result.shape; // no_boxes, xywhaconfcls
                if (shape is null)
                    return null;
                if(shape[0] == 0)
                    return null;
                List<Rect2> rectList = new List<Rect2>();
                for (int i = 0; i < shape[0]; i++)
                {

                    //NDArray xyxyScoreClass = result[string.Format("{0}, 0:6", i)];
                    //float x0 = xyxyScoreClass[0];
                    //float y0 = xyxyScoreClass[1];
                    //float x1 = xyxyScoreClass[2];
                    //float y1 = xyxyScoreClass[3];
                    //float confidence = xyxyScoreClass[4];
                    //float classes = xyxyScoreClass[5];
                    //int cls = (int)classes;
                    //HRegion region = new HRegion();
                    //region.GenRectangle1((HTuple)y0, (HTuple)x0, (HTuple)y1, (HTuple)x1);
                    //context.inspection_result.AddRegion(region, TrainConfig.Classes[cls].Color);
                    //string text = string.Format("{0}: {1:0.00f}", TrainConfig.Classes[cls].Name, confidence * 100);
                    //context.inspection_result.AddText(text, "black", "#ffffffff", (HTuple)y0, (HTuple)x0);
                    //float _area = (y1 - y0) * (x1 - x0);
                    //rectList.Add(new Rect1(y0, x0, y1, x1, TrainConfig.Classes[cls].id));

                    NDArray xywhaScoreClass = result[string.Format("{0}, 0:7", i)];
                    float xc = xywhaScoreClass[0];
                    float yc = xywhaScoreClass[1];
                    float w = xywhaScoreClass[2];
                    float h = xywhaScoreClass[3];
                    float angle = xywhaScoreClass[4];
                    float confidence = xywhaScoreClass[5];
                    float classes = xywhaScoreClass[6];
                    int cls = (int)classes;
                    HRegion region = new HRegion();
                    region.GenRectangle2((HTuple)yc, (HTuple)xc, (HTuple)(angle * -1.0f), (HTuple)(w / 2.0f), (HTuple)(h / 2.0f));
                    context.inspection_result.AddRegion(region, TrainConfig.Classes[cls].Color);
                    string text = string.Format("{0}: {1:0.00f}", TrainConfig.Classes[cls].Name, confidence * 100);
                    //context.inspection_result.AddText(text, "black", "#ffffffff", (HTuple)yc, (HTuple)xc);
                    rectList.Add(new Rect2(yc, xc, (angle * -1.0f), w / 2.0f, h / 2.0f, TrainConfig.Classes[cls].id));
                }
                return rectList.ToArray();
            }
            return null;
        }
    }
    public class RYOLOV4TrainConfig
    {
        public ObservableCollection<YOLOClass> Classes { get; set; } = new ObservableCollection<YOLOClass>();
        public bool TRAIN_YOLO_TINY { get; set; }
        public bool DATA_AUG { get; set; }
        public double LR_INIT { get; set; } = 1e-2;
        public double LR_END { get; set; } = 1e-6;
        public int WARMUP_EPOCHS { get; set; } = 2;
        public int WARMUP_STEPS { get; set; } = 0;
        public int FINETUNE_EPOCHS { get; set; } = 20;
        public int[] STRIDES { get; set; }
        public int ANCHOR_PER_SCALE { get; set; }
        public double IOU_LOSS_THRESH { get; set; }
        public int EPOCHS { get; set; } = 100;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public string WeightDir { get; set; }
        public string PreTrainModelPath { get; set; }
        public string TrainLogDir { get; set; }
        public bool UseMosaicImage { get; set; }
        /// <summary>
        /// adam, sgd, rmsprop
        /// </summary>
        public string Optimizer { get; set; } = "adam";
        public string ImageDir { get; set; }
        public string ResultDir { get; set; }
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 608;
        public int INPUT_HEIGHT { get; set; } = 608;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        public double LABEL_SMOOTHING { get; set; } = 0.2;
        public double LEAKY_RELU { get; set; } = 0.1;
        public int MAX_OUTPUT_SIZE { get; set; } = 20;
        public double IOU_THRESHOLD { get; set; } = 0.5;
        public double CONFIDENCE_THRESHOLD { get; set; } = 0.5;
        public double BATCH_NORM_EPSILON { get; set; } = 1e-5;
        public double BATCH_NORM_DECAY { get; set; } = 0.9;
        public string Precision { get; set; } = "float32";
        public string TrainningType { get; set; } = "transfer";
        public bool VisualizeLearningProcess { get; set; }
        public bool EarlyStopping { get; set; } = false;
        public string ModelType { get; set; } = "YOLO";
        public RectangleMask Mask { get; set; } = new RectangleMask();
        public bool UseMask { get; set; }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public string ClassNameDir { get; set; }
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this,Formatting.Indented));
        }
        public static RYOLOV4TrainConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<RYOLOV4TrainConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new RYOLOV4TrainConfig(BaseDir);
            }

        }
        private RYOLOV4TrainConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();
        }
        private RYOLOV4TrainConfig()
        {

        }
          
        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = System.IO.Path.Combine(BaseDir, "images");
            AnnotationDir = System.IO.Path.Combine(BaseDir, "annotations");
            ResultDir = System.IO.Path.Combine(BaseDir, "result");
            SavedModelDir = System.IO.Path.Combine(BaseDir, "model");
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            PreTrainModelPath = System.IO.Path.Combine("Designer", "Python", "YOLOv4", "weights");
            WeightDir = System.IO.Path.Combine(SavedModelDir, "weights");
            ClassNameDir = System.IO.Path.Combine(AnnotationDir, "className" + ".names");
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
            if (!System.IO.Directory.Exists(SavedModelDir))
            {
                Directory.CreateDirectory(SavedModelDir);
            }
            if (!System.IO.Directory.Exists(WeightDir))
            {
                Directory.CreateDirectory(WeightDir);
            }
        }
    }
    public class RYOLOV4NONNXRuntime:ONNXModel
    {
        int input_height, input_width;
        //int numClasses;
        public string ModelDir { get; internal set; }
        public RYOLOV4NONNXRuntime(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
       
        InferenceSession ONNXSession;
        string input_name;
        string input_conf_threshold, input_iou_threshold, input_max_outputs;
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
                input_conf_threshold = "input_conf_threshold";
                input_iou_threshold = "input_iou_threshold";
                input_max_outputs = "input_max_outputs";
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
                    NamedOnnxValue.CreateFromTensor<float>(input_conf_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{0.5f },new int[]{1,1})),
                    NamedOnnxValue.CreateFromTensor<float>(input_iou_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{0.5f },new int[]{1,1})),
                    NamedOnnxValue.CreateFromTensor<int>(input_max_outputs,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<int>(new int[]{1 },new int[]{1,1}))
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
            //float dw = (input_width - resizeRatio * targetW) / 2;
            //float dh = (input_width - resizeRatio * targetH) / 2;
            //float dw = (input_width - resizeRatio * targetW);
            //float dh = (input_width - resizeRatio * targetH);

            boxes["..., 0:4:2"] = 1.0 * (boxes["..., 0:4:2"]) / resizeRatio;
            boxes["..., 1:4:2"] = 1.0 * (boxes["..., 1:4:2"]) / resizeRatio;
            return boxes;
        }

        public NDArray PredictRYOLO(HImage img, int numClasses, int maxOutputSize, int iouThreshold, int confidenceThreshold)
        {
            var _iouThreshold = iouThreshold / 100.0f;
            var _confidenceThreshold = confidenceThreshold / 100.0f;
            img.GetImageSize(out int originalW, out int orignalH);
            var inputImageValues = ImagePreprocessUpdate(img);

            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,input_height,input_width,3 })),
                NamedOnnxValue.CreateFromTensor(input_conf_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{_confidenceThreshold },new int[]{1,1})),
                NamedOnnxValue.CreateFromTensor(input_iou_threshold,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{_iouThreshold },new int[]{1,1})),
                NamedOnnxValue.CreateFromTensor(input_max_outputs,  new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<int>(new int[]{maxOutputSize },new int[]{1,1}))
            };

            //var _iouThreshold = iouThreshold / 100.0f;
            //var _confidenceThreshold = confidenceThreshold / 100.0f;
            //img.GetImageSize(out int originalW, out int orignalH);
            //var inputImageValues = ImagePreprocessUpdate(img);
            //var inputs = new List<NamedOnnxValue>()
            //{
            //    NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(inputImageValues,new int[]{1,input_height,input_width,3 }))
            //};
            var results = ONNXSession.Run(inputs).ToList();
            //var predict = new NDArray[results.Count];
            //for (int i = 0; i < results.Count; i++)
            //{
            //    var name = results[i].Name;
            //    var shape = ONNXSession.OutputMetadata[name].Dimensions;
                
            //    predict[i] = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { -1, shape[4] })); // bs=1, no_boxes, xywhaconf,cls
            //}

            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            //var predict_box = np.concatenate(predict, 0); // no_boxes, xywhaconfcls
            var boxes1 = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { -1, 7 }));
            //NDArray detectionResultDict = RNonMaxSuppression(predict_box, numClasses, maxOutputSize, _iouThreshold, _confidenceThreshold);
            //if (detectionResultDict.size == 0)
            //{
            //    return new NDArray(NPTypeCode.Float);
            //}
            if (boxes1.size > 0)
            {
                return ResizeBack(boxes1, originalW, orignalH);
            }
            

            NDArray boxes = new NDArray(NPTypeCode.Float);
            //int num_boxes = 0;
            //for (int i = 0; i < 2; i++)
            //{
            //    var name = results[i].Name;
            //    var shape = ONNXSession.OutputMetadata[name].Dimensions;              
            //    if (i == 0)
            //    {
            //        var TensorOutput = results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            //        boxes = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { 1, -1, shape[2] }));
            //    }
            //    else
            //    {
            //        num_boxes = (results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<int>).GetValue(0);
            //        if (num_boxes == 0)
            //            return new NDArray(NPTypeCode.Float);
            //    }               
            //}
            //boxes = boxes[string.Format("0, :{0}, :", num_boxes)];
            //boxes = ResizeBack(boxes, originalW, orignalH);
            return boxes;
        }
        public NDArray CalculateIOU(NDArray boxes1, NDArray boxes2)
        {
            NDArray boxes1_area = boxes1["..., 2"] * boxes1["..., 3"];
            NDArray boxes2_area = boxes2["..., 2"] * boxes2["..., 3"];

            NDArray newBoxes1 = np.concatenate(new NDArray[] {boxes1["..., :2"] - boxes1["..., 2:"] * 0.5,
                                boxes1["..., :2"] + boxes1["..., 2:"] * 0.5}, -1);
            NDArray newBoxes2 = np.concatenate(new NDArray[] {boxes2["..., :2"] - boxes2["..., 2:"] * 0.5,
                                boxes2["..., :2"] + boxes2["..., 2:"] * 0.5}, -1);

            NDArray left_up = np.maximum(newBoxes1["..., :2"], newBoxes2["..., :2"]);
            NDArray right_down = np.minimum(newBoxes1["..., 2:"], newBoxes2["..., 2:"]);

            NDArray inter_section = np.maximum(right_down - left_up, 0.0);

            NDArray inter_area = inter_section["..., 0"] * inter_section["..., 1"];

            NDArray union_area = boxes1_area + boxes2_area - inter_area + 1e-9;

            return inter_area / union_area;
        }
        public NDArray RNonMaxSuppression(NDArray inputs, int numClasses,
            int maxOutputSize, float iOUThreshold, float confidenceThreshold)
        {
            int[] shape = inputs.shape; // no_boxes, xywhaconfcls

            NDArray boxes = inputs; // no_boxes, xywhaconfcls
            NDArray mask = boxes[":, 5"] > confidenceThreshold;
            boxes = boxes[boxes[":, 5"] > confidenceThreshold];

            if (boxes.shape[0] == 0)
                return new NDArray(NPTypeCode.Float);
            
            NDArray classes;

            if (numClasses != 1)
            {
                classes = np.argmax(boxes[":, 6:"], -1);
            }
            else
            {
                classes = np.zeros(boxes[":, 6:"].size);
            }

            classes = np.expand_dims(classes, -1).astype(NPTypeCode.Float);
            boxes = np.concatenate(new NDArray[] { boxes[":, :6"], classes }, -1);

            List<NDArray> finalResult = new List<NDArray>();

            for (int cls = 0; cls < numClasses; cls++)
            {
                mask = boxes[":, 6"] == cls;
                var condition = mask.GetData<bool>().Where(data => data == true).ToList();
                if (condition.Count > 0)
                {
                    NDArray classBoxes = boxes[boxes[":, 6"] == cls];
                    if (classBoxes.size == 0)
                        continue;
                    NDArray boxesCoords = classBoxes[":,:4"].astype(NPTypeCode.Float);
                    NDArray anglesBoxes = classBoxes[":, 4"].astype(NPTypeCode.Float);
                    NDArray boxesConfScores = classBoxes[":, 5"].astype(NPTypeCode.Float);
                    NDArray order = np.argsort<int[]>(boxesConfScores);

                    List<NDArray> boxesKeep = new List<NDArray>();

                    while (order.size > 0)
                    {
                        int idx = order[-1];
                        boxesKeep.Add(classBoxes[idx].astype(NPTypeCode.Float).reshape(1, 7));
                        order = order[":-1"];

                        if (order.size == 0)
                        {
                            break;
                        }

                        NDArray chooseBoxes = boxesCoords[idx].reshape(new int[] {-1, 4});
                        NDArray chooseAnglesBoxes = anglesBoxes[idx];
                        NDArray rBoxes = boxesCoords[order].astype(NPTypeCode.Float);
                        NDArray rAnglesBoxes = anglesBoxes[order].astype(NPTypeCode.Float);

                        NDArray IoU = CalculateIOU(chooseBoxes, rBoxes);
                        mask = IoU * np.abs(np.cos(chooseAnglesBoxes - rAnglesBoxes)) < iOUThreshold;
                        if (mask.size == 1 && mask.GetData<bool>()[0])
                        {
                        }
                        else
                        {
                            order = order[mask == true];
                        }
                    }
                    NDArray boxesClsKeep = np.concatenate(boxesKeep.ToArray(), 0); // no_boxes, xywhaconfcls
                    finalResult.Add(boxesClsKeep);
                }
             }

            return np.concatenate(finalResult.ToArray(), 0); // no_boxes, xywhaconfcls
        }


        public Dictionary<int, Dictionary<int, NDArray>> Predict(HImage img, int numClasses, int maxOutputSize, int iouThreshold, int confidenceThreshold)
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

            for (int i = 0; i < results.Count; i++)
            {
                var name = results[i].Name;
                var shape = ONNXSession.OutputMetadata[name].Dimensions;
                var TensorOutput = results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                outputs[i] = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { 1, -1, shape[4] }));
            }

            var result = np.concatenate(outputs, 1);

            //var detectionResult = BuildBoxes(result, numClasses, originalW, orignalH);
            //var bboxes = BatchNonMaximalSuppression(detectionResult[0], confidenceThreshold / 100f, iouThreshold / 100f);

            var detectionResult = BuildBoxes(result, numClasses, originalW, orignalH);
            Dictionary<int, Dictionary<int, NDArray>> detectionResultDict = NonMaxSuppression(detectionResult, numClasses, maxOutputSize, _iouThreshold, _confidenceThreshold);
            return detectionResultDict;
        }
        
        public float[] ImagePreprocessUpdate(HImage img)
        {
            img.GetImageSize(out int originalW, out int originalH);
            int ih = input_height;
            int iw = input_width;

            float scale = Math.Min((float)iw / originalW, (float)ih / originalH);
            int nw = (int)(scale * originalW);
            int nh = (int)(scale * originalH);
            
            byte[] arrInput = new byte[iw * ih * 3];
            var resizedImg1 = img.ZoomImageSize(nw, nh, "bilinear");
            var paddedImage = resizedImg1.ChangeFormat(iw, ih);
            // convert HImage to NDArray
            string type;
            int width, height;

            if (paddedImage.CountChannels() == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                paddedImage.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                Marshal.Copy(pointerR, arrInput, 0, iw * ih);
                Marshal.Copy(pointerG, arrInput, iw * ih, iw * ih);
                Marshal.Copy(pointerB, arrInput, 2* iw * ih, iw * ih);
            }
            else
            {
                IntPtr pointerGray;
                pointerGray = paddedImage.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, arrInput, 0, iw * ih);
                Marshal.Copy(pointerGray, arrInput, iw * ih, iw * ih);
                Marshal.Copy(pointerGray, arrInput, 2 * iw * ih, iw * ih);
            }

            NDArray arrFinal = (new NDArray(arrInput, new NumSharp.Shape(1,3, ih, iw), order: 'F'));
            NDArray inputImageValues = np.transpose(arrFinal, new int[] { 0, 2, 3, 1 });
            NDArray currResizedImg = inputImageValues[0];
            //currResizedImg = currResizedImg / 255.0f;

            //Shape shape = new Shape(new int[] { ih, iw, 3 });
            //NDArray imgPaded = np.full(shape, 0.0);

            //int dw = (iw - nw) / 2;
            //int dh = (ih - nh) / 2;
            //string slice = string.Format("{0}:{1},{2}:{3},:", dh, nh + dh, dw, nw + dw);
            //imgPaded[slice] = currResizedImg;
            //imgPaded.astype(NPTypeCode.Float);
            currResizedImg = currResizedImg / 255f;

            //return np.expand_dims(imgPaded.astype(NPTypeCode.Float), 0);
            return np.expand_dims(currResizedImg.astype(NPTypeCode.Float), 0).ToArray<float>();
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
            byte[] arrInputR = new byte[nw * nh];
            byte[] arrInputG = new byte[nw * nh];
            byte[] arInputB = new byte[nw * nh];

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

            //int dw = (iw - nw) / 2;
            //int dh = (ih - nh) / 2;
            int dw = (iw - nw);
            int dh = (ih - nh);
            string slice = string.Format("{0}:{1},{2}:{3},:", dh, nh + dh, dw, nw + dw);
            imgPaded[slice] = currResizedImg;
            imgPaded.astype(NPTypeCode.Float);
            imgPaded = imgPaded / 255.0f;

            return np.expand_dims(imgPaded.astype(NPTypeCode.Float), 0);
        }

        public NDArray BuildBoxes(NDArray inputs, int numClasses, int targetW, int targetH)
        {
            float resizeRatio = Math.Min((float)input_width / targetW, (float)input_height / targetH);
            float dw = (input_width - resizeRatio * targetW)/2 ;
            float dh = (input_width - resizeRatio * targetH)/2 ;

            NDArray detectResults = inputs;
            int[] shape = detectResults.shape;
            Shape reshape = new Shape(new int[] { shape[0], shape[1], 1 });

            NDArray centerX = detectResults[":,:,0"].reshape(reshape);
            NDArray centerY = detectResults[":,:,1"].reshape(reshape);
            NDArray width = detectResults[":,:,2"].reshape(reshape);
            NDArray height = detectResults[":,:,3"].reshape(reshape);
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

        public Dictionary<int, Dictionary<int, NDArray>> NonMaxSuppression(NDArray inputs, int numClasses,
            int maxOutputSize, float iOUThreshold, float confidenceThreshold)
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


                        NDArray x1 = boxesCoords[":, 0"].astype(NPTypeCode.Float);
                        NDArray y1 = boxesCoords[":, 1"].astype(NPTypeCode.Float);
                        NDArray x2 = boxesCoords[":, 2"].astype(NPTypeCode.Float);
                        NDArray y2 = boxesCoords[":, 3"].astype(NPTypeCode.Float);
                        NDArray areas = np.multiply(np.subtract(x2, x1), np.subtract(y2, y1));
                        NDArray order = np.argsort<NDArray>(boxesConfScores);

                        List<NDArray> boxesKeep = new List<NDArray>();
                        while (order.size > 0)
                        {
                            int idx = order[-1];
                            boxesKeep.Add(classBoxes[idx]);
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

        public List<int> NMS(NDArray dets,double thresh)
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

                var inds =np.nonzero(ovr[ovr < thresh])[0];
                order = order[inds + 1];
            };
            return keep;   
        }
        public NDArray BatchNonMaximalSuppression(NDArray prediction,double conf_threshold= 0.5,double iou_threshold= 0.25)
        {
            var candidates = prediction[prediction["..., 4"] > (float)conf_threshold];
            var boxes = xywh2xyxy(candidates);
            var keep = NMS(candidates, iou_threshold);
            return candidates[new NDArray(keep.ToArray())];
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