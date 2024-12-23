using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using System.ComponentModel;
using System.Collections.ObjectModel;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.NodeViews;
using System.Runtime.InteropServices;
using NumSharp;
using NOVisionDesigner.Designer.Misc;
using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;
using DynamicData;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Windows;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Drawing;
using Microsoft.ML.OnnxRuntime;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "ORC Classifier",visible:false)]
    public class OCRForDigitsAndCharacters : BaseNode
    {
        static OCRForDigitsAndCharacters()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<OCRForDigitsAndCharacters>));
        }
        public override void OnLoadComplete()
        {
            runtime?.LoadRecipe(ModelDir);
        }
        public override void OnInitialize()
        {
            runtime = new ONNXOCRClassifier(ModelDir);
            //runtime.LoadRecipe(ModelDir);
        }
        #region Properties
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HRegion> Regions { get; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }

        public ObservableCollection<ClassifierClass1> ClassList = new ObservableCollection<ClassifierClass1>();
        public bool DisplayBox { get; set; } = true;
        public bool DisplayLabel { get; set; } = true;
        #endregion
        // base model
        public ONNXOCRClassifier runtime;
        private Dictionary<int, string> _colorForModelBase = new Dictionary<int, string>();
        private Random randomColor = new Random();


        public string ImageDir, AnnotationDir, ModelDir, ClassListDir, ClassListPath, RootDir;

        public OCRForDigitsAndCharacters(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = Path.Combine(dir, id, "OCR_Resnet50");
            ImageDir = Path.Combine(RootDir, "images");
            ModelDir = Path.Combine(RootDir, "model");
            AnnotationDir = Path.Combine(RootDir, "annotations");
            if (!System.IO.Directory.Exists(ImageDir))
            {
                System.IO.Directory.CreateDirectory(ImageDir);
            }

            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                System.IO.Directory.CreateDirectory(AnnotationDir);
            }

            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
            

            this.CanBeRemovedByUser = true;
            this.Name = "OCR Classifier";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            Regions = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region",
                Editor = new Editors.RegionValueEditorViewModel()
            };
            this.Inputs.Add(Regions);
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Bool"
            };
            this.Outputs.Add(Result);
        }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    OCRClassifierEditorWindow wd = new OCRClassifierEditorWindow(RootDir, ImageDir, AnnotationDir, ModelDir, ClassList, null, this);
                    wd.ShowDialog();
                    break;
            }
        }

        public override void Run(object context)
        {

            Result.OnNext(RunInside(Image.Value, Regions.Value, context as InspectionContext));
        }

        public bool RunInside(HImage image, HRegion Region, InspectionContext e)
        {
            if (image == null)
            {
                return false;
            }
            if (Region == null)
            {
                return false;
            }
            if (runtime.State == ModelState.NotFound)
            {
                return false;
            }
            if (runtime.State == ModelState.Unloaded)
            {
                runtime.LoadRecipe(ModelDir);
            }
            if (runtime.State == ModelState.Loaded)
            {
                HTuple channels = image.CountChannels();
                if (channels < 3)
                {
                    image = image.Compose3(image, image);
                }
                InspectionResult inspectionResult = e.inspection_result;
                var regionInspection = Region;


                HRegion regionColorSelect = new HRegion();
                regionColorSelect.GenEmptyObj();

                HRegion regionClassify = regionInspection;

                e.inspection_result.AddDisplay(regionInspection, "blue");
                HRegion threshold = regionInspection.Intersection(regionClassify).ClosingCircle((HTuple)2.5).Connection();
                regionColorSelect = threshold.SelectShape("area", "and", 10, double.MaxValue);
                if (regionColorSelect.CountObj() > 0)
                {
                    List<ClassResult> result = Classify(image, regionColorSelect, inspectionResult);
                }

                return true;
            }
            else
            {
                return false;
            }



           
        }

        public List<ClassResult> Classify(HImage image, HRegion regions, InspectionResult inspectionResult)
        {
            var w = 100;
            var h = 100;

            List<ClassResult> results = new List<ClassResult>();

            for (int i = 0; i < regions.CountObj(); i++)
            {
                int r1, c1, r2, c2;
                regions[i + 1].SmallestRectangle1(out r1, out c1, out r2, out c2);
                HHomMat2D hommat2d = new HHomMat2D();
                hommat2d.VectorToHomMat2d(new HTuple(r1, r1, r2), new HTuple(c1, c2, c2), new HTuple(0, 0, h), new HTuple(0, w, w));
                var imageCroped = image.AffineTransImageSize(hommat2d, "constant", w, h);
                var inferResult = runtime.Infer(imageCroped);
                var classIndex = inferResult.Item1;
                if (classIndex >= 0)
                {

                    results.Add(new ClassResult(new ClassifierClass1() { Name = runtime.CharList[classIndex].ToString() }, regions[i + 1], "blue", false, inferResult.Item2));
                    if (inspectionResult != null)
                    {
                        if (inferResult.Item2 > 0.5)
                        {
                            if (DisplayBox)
                            {
                                inspectionResult.AddDisplay(new HRegion((double)r1, c1, r2, c2), "blue");
                            }
                            if (DisplayLabel)
                            {
                                if (DisplayBox)
                                {
                                    inspectionResult.AddText(runtime.CharList[classIndex].ToString() + ": " + (inferResult.Item2 * 100).ToString("N0"), "black", "#ffffffff", r1, c1);
                                }
                                else
                                {
                                    inspectionResult.AddText(runtime.CharList[classIndex].ToString() + ": " + (inferResult.Item2 * 100).ToString("N0"), "black", "#ffffffff", (r1 + r2) / 2, (c1 + c2) / 2);
                                }
                            }
                        }
                        else
                        {
                            if (DisplayBox)
                            {
                                inspectionResult.AddDisplay(new HRegion((double)r1, c1, r2, c2), "red");
                            }
                            if (DisplayLabel)
                            {
                                if (DisplayBox)
                                {
                                    inspectionResult.AddText(runtime.CharList[classIndex].ToString() + ": " + (inferResult.Item2 * 100).ToString("N0"), "red", "#ffffffff", r1, c1);
                                }
                                else
                                {
                                    inspectionResult.AddText(runtime.CharList[classIndex].ToString() + ": " + (inferResult.Item2 * 100).ToString("N0"), "red", "#ffffffff", (r1 + r2) / 2, (c1 + c2) / 2);
                                }
                            }
                        }


                    }
                }
            }
            return results;
        }
    }
    public class ONNXOCRClassifier:ONNXModel
    {
        public string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        int input_width, input_height;
        public string ModelDir { get; set; }
        public ONNXOCRClassifier(string ModelDir)
        {
            this.ModelDir = ModelDir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
                State = ModelState.NotFound;
            }
            else
            {
                State = ModelState.Unloaded;
            }
            
        }
        InferenceSession ONNXSession;
        string input_name;
        public bool LoadDefaultOnnx()
        {
            var filepath = System.IO.Path.Combine(MainWindow.AppPath, "Designer",
                            "Python", "OCRForDigitsAndCharacters", "weights", "model.onnx");
            if (!System.IO.File.Exists(filepath))
            {
                return false;

            }

            return true;
        }
        public bool LoadOnnx(string directory)
        {
            try
            {
                var filepath = System.IO.Path.Combine(directory, "model.onnx");
                if (!System.IO.File.Exists(filepath))
                {
                    //try to load default model
                    var defaultModelPath = System.IO.Path.Combine(MainWindow.AppPath, "Designer",
                            "Python", "OCRForDigitsAndCharacters", "saved_model", "model.onnx");
                    if (!System.IO.File.Exists(defaultModelPath))
                    {
                        State = ModelState.NotFound;
                        return false;

                    }
                    else
                    {
                        filepath = defaultModelPath;
                    }

                }
                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(filepath, options);
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
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
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
        public (int, double) Infer(HImage imgInput)
        {
            int originalw, originalh;
            imgInput.GetImageSize(out originalw, out originalh);
            var image_resized = imgInput.ZoomImageSize(input_width, input_height, "constant");
            byte[] array_input = new byte[input_width * input_height * 3];
            string type;
            int width, height;
            IntPtr pointerR, pointerG, pointerB;
            image_resized.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
            Marshal.Copy(pointerR, array_input, 0, input_width * input_height);
            Marshal.Copy(pointerG, array_input, input_width * input_height, input_width * input_height);
            Marshal.Copy(pointerB, array_input, input_width * input_height * 2, input_width * input_height);
            NDArray NDArrayInput = new NDArray(array_input).astype(NPTypeCode.Float)/255f;
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(NDArrayInput.ToArray<float>(),new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var result_tensor = new NDArray(TensorOutput.ToArray());
            return (result_tensor.argmax(), (float)result_tensor[result_tensor.argmax()]);
        }
    }
}
