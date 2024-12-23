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
    [NodeInfo("Deep Learning", "YOLO v6", visible: false)]
    public class YOLOv6: BaseNode
    {
        static YOLOv6()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<YOLOv6>));
        }

        #region properties
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<int> MaxOutputSize { get; set; }
        public ValueNodeInputViewModel<int> IOUThreshold { get; set; }
        public ValueNodeInputViewModel<int> ConfidenceThreshold { get; set; }
        public ValueNodeOutputViewModel<Rect1[]> Result { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public YOLOV6NONNXRuntime runtime;
        public YOLOV6TrainConfig TrainConfig;
        #endregion

        public string ImageDir, AnnotationDir, ModelDir, RootDir, TrainConfigDir;

        public override void OnInitialize()
        {
            runtime = new YOLOV6NONNXRuntime(ModelDir);
        }

        public YOLOv6(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = Path.Combine(dir, id, "YOLOv6");
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
            TrainConfig = YOLOV6TrainConfig.Create(TrainConfigDir, ModelDir, ImageDir, AnnotationDir);
            if (!System.IO.File.Exists(TrainConfigDir))
            {
                TrainConfig.Save(TrainConfigDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "YOLO v6";

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

            Result = new ValueNodeOutputViewModel<Rect1[]>()
            {
                Name = "OuputBoxes",
                PortType = "Rect1[]"
            };
            this.Outputs.Add(Result);
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

        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    //YOLOv6EditorWindow wd = new YOLOv6EditorWindow(this);
                    //listAugmentation = wd.listAugmentation;
                    //wd.ShowDialog();
                    break;
            }
        }

        public Rect1[] RunInside(HImage img, int maxOutputSize, int iouThreshold, int confidenceThreshold, InspectionContext context)
        {
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
                var result = runtime.Predict(img, TrainConfig.Classes.Count, maxOutputSize, iouThreshold, confidenceThreshold);
                int[] shape = result.shape;
                if (shape is null)
                    return null;
                if (shape[0] == 0)
                    return null;
                List<Rect1> rectList = new List<Rect1>();
                for (int i = 0; i < shape[0]; i++)
                {
                    NDArray xyxyScoreClass = result[string.Format("{0}, 0:6", i)];
                    float x0 = xyxyScoreClass[0];
                    float y0 = xyxyScoreClass[1];
                    float x1 = xyxyScoreClass[2];
                    float y1 = xyxyScoreClass[3];
                    float confidence = xyxyScoreClass[4];
                    float classes = xyxyScoreClass[5];
                    int cls = (int)classes;
                    HRegion region = new HRegion();
                    region.GenRectangle1((HTuple)y0, (HTuple)x0, (HTuple)y1, (HTuple)x1);
                    context.inspection_result.AddRegion(region, TrainConfig.Classes[cls].Color);
                    string text = string.Format("{0}: {1:0.00f}", TrainConfig.Classes[cls].Name, confidence * 100);
                    context.inspection_result.AddText(text, "black", "#ffffffff", (HTuple)y0, (HTuple)x0);
                    float _area = (y1 - y0) * (x1 - x0);
                    rectList.Add(new Rect1(y0, x0, y1, x1));
                }
                return rectList.ToArray();
            }
            return null;
        }
    }

    public class YOLOV6TrainConfig
    {
        public ObservableCollection<ClassifierClass1> Classes { get; set; } = new ObservableCollection<ClassifierClass1>();
        public bool DATA_AUG { get; set; }
        public double LR_INIT { get; set; } = 1e-3;
        public double LR_END { get; set; } = 1e-6;
        public int WARMUP_EPOCHS { get; set; } = 2;
        public int WARMUP_STEPS { get; set; } = 0;
        public int[] STRIDES { get; set; }
        public int ANCHOR_PER_SCALE { get; set; }
        public double IOU_LOSS_THRESH { get; set; }
        public int EPOCHS { get; set; } = 40;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public string WeightDir { get; set; }
        public string PreTrainModelPath { get; set; }
        public string TrainLogDir { get; set; }
        public bool UseMosaicImage { get; set; }
        public string Optimizer { get; set; } = "adam";
        public string ImageDir { get; set; }
        public string ResultDir { get; set; }
        public int BATCH_SIZE { get; set; } = 8;
        public int INPUT_WIDTH { get; set; } = 640;
        public int INPUT_HEIGHT { get; set; } = 640;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        public double LABEL_SMOOTHING { get; set; } = 0.2;
        public int MAX_OUTPUT_SIZE { get; set; } = 50;
        public double IOU_THRESHOLD { get; set; } = 0.5;
        public double CONFIDENCE_THRESHOLD { get; set; } = 0.5;
        public string TrainningType { get; set; } = "transfer";
        public bool VisualizeLearningProcess { get; set; }
        public string ModelType { get; set; } = "YOLOv6n";
        public string LOSS_BBOXES_TYPE { get; set; } = "siou";
        public void Save(string dir)
        {
            System.IO.File.WriteAllText(dir, JsonConvert.SerializeObject(this));
        }

        public static YOLOV6TrainConfig Create(string dir, string modelDir, string imageDir, string annotationDir)
        {
            if (System.IO.File.Exists(dir))
            {
                var json = File.ReadAllText(dir);
                try
                {
                    var config = JsonConvert.DeserializeObject<YOLOV6TrainConfig>(json);
                    return config;
                }
                catch (Exception ex)
                {
                    return new YOLOV6TrainConfig(modelDir, imageDir, annotationDir);
                }
            }
            else
            {
                return new YOLOV6TrainConfig(modelDir, imageDir, annotationDir);
            }
        }

        private YOLOV6TrainConfig(string modelDir, string imageDir, string annotationDir)
        {
            SavedModelDir = modelDir;
            WeightDir = System.IO.Path.Combine(modelDir, "weights");
            if (!System.IO.Directory.Exists(WeightDir))
            {
                System.IO.Directory.CreateDirectory(WeightDir);
            }
            ResultDir = System.IO.Path.Combine(modelDir, "TrainningResult");
            if (!System.IO.Directory.Exists(ResultDir))
            {
                System.IO.Directory.CreateDirectory(ResultDir);
            }
            PreTrainModelPath = System.IO.Path.Combine("Designer", "Python", "YOLOv6", "weights");
            ImageDir = imageDir;
            AnnotationDir = annotationDir;
        }

        private YOLOV6TrainConfig()
        {

        }
    }

    public class YOLOV6NONNXRuntime : ONNXModel
    {
        int input_height, input_width;
        //int numClasses;
        public string ModelDir { get; internal set; }
        public YOLOV6NONNXRuntime(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }

        InferenceSession ONNXSession;
        string input_name;
        public bool LoadOnnx(string directory)
        {
            try
            {

                int gpuDeviceId = 0;
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = "input_yolov6";


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

        public NDArray ResizeBack(NDArray boxes, int targetW, int targetH)
        {
            float resizeRatio = Math.Min((float)input_width / targetW, (float)input_width / targetH);
            float dw = (input_width - resizeRatio * targetW) / 2;
            float dh = (input_width - resizeRatio * targetH) / 2;

            boxes["..., 0:4:2"] = 1.0 * (boxes["..., 0:4:2"] - dw) / resizeRatio;
            boxes["..., 1:4:2"] = 1.0 * (boxes["..., 1:4:2"] - dh) / resizeRatio;
            return boxes;
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
                Marshal.Copy(pointerB, arrInput, 2 * nw * nh, nw * nh);
            }
            else
            {
                IntPtr pointerGray;
                pointerGray = resizedImg.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, arrInput, 0, nw * nh);
                Marshal.Copy(pointerGray, arrInput, nw * nh, nw * nh);
                Marshal.Copy(pointerGray, arrInput, 2*nw * nh, nw * nh);
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

        public NDArray Predict(HImage img, int numClasses, int maxOutputSize, int iouThreshold, int confidenceThreshold)
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
            NDArray boxes = new NDArray(NPTypeCode.Float);
            int num_boxes = 0;
            for (int i = 0; i < 2; i++)
            {
                var name = results[i].Name;
                var shape = ONNXSession.OutputMetadata[name].Dimensions;

                if (i == 0)
                {
                    var TensorOutput = results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                    boxes = new NDArray(TensorOutput.ToArray()).reshape(new Shape(new int[] { 1, -1, shape[2] }));
                }
                else
                {
                    num_boxes = (results[i].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<int>).GetValue(0);
                    if (num_boxes == 0)
                        return new NDArray(NPTypeCode.Float);
                }

            }

            boxes = boxes[string.Format("0, :{0}, :", num_boxes)];

            boxes = ResizeBack(boxes, originalW, orignalH);

            return boxes;
        }
    }
}
