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
using Microsoft.ML.OnnxRuntime;
using NOVisionDesigner.Designer.Extensions;
using Newtonsoft.Json.Converters;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning","Deep Classifier", Icon: "Designer/icons/icons8-objects-100.png",sortIndex:3)]
    public class DeepClassifier : BaseNode
    {
        static DeepClassifier()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DeepClassifier>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            try
            {
                //SaveClassList(System.IO.Path.Combine(Dir,"classlist.txt"));
                SaveClassList(TrainConfig.classList_path);
            }
            catch(Exception ex)
            {
                
            }
            
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            try
            {
                //LoadClassList(System.IO.Path.Combine(Dir, "classlist.txt"));
                LoadClassList(TrainConfig.classList_path);

            }
            catch (Exception ex)
            {
            }
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        //public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeInputViewModel<HRegion> Regions { get; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public ValueNodeOutputViewModel<ClassResult[]> RegionOutput { get; set; }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    ClassifierEditorWindow wd = new ClassifierEditorWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                     wd.Show();
                    break;
            }
        }
        [HMIProperty("Classifier Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        public List<Augmentation> listAugmentation { get; set; } = new List<Augmentation>();
        public ObservableCollection<ClassifierClass1> ClassList = new ObservableCollection<ClassifierClass1>();
        public ClassifierONNX Runtime { get; set; } 
        public bool DisplayBox { get; set; } = true;
        public bool DisplayLabel { get; set; } = true;
        public string ImageDir, AnnotationDir,  ResultDir, ModelDir,ConfigDir, ClasslistDir,EvaluationDir;
        public DeepClassifierConfig TrainConfig { get; set; }
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
            var result = RunInside(ImageInput.Value, Regions.Value, context as InspectionContext);
            Result.OnNext(result.Item2);
            RegionOutput.OnNext(result.Item1);
        }
        
        public DeepClassifier(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            

            this.CanBeRemovedByUser = true;
            this.Name = "Deep Classifier";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);

            //RegionInput = new ValueNodeInputViewModel<HRegion>()
            //{
            //    Name = "Region"
            //};
            //this.Inputs.Add(RegionInput);
            Regions = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region",
                Editor = new RegionValueEditorViewModel()
            };
            this.Inputs.Add(Regions);
            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Bool"
            };
            this.Outputs.Add(Result);
            RegionOutput = new ValueNodeOutputViewModel<ClassResult[]>()
            {
                Name = "Region Output",
                PortType = "ClassResult[]"
            };
            this.Outputs.Add(RegionOutput);
            //CreateDefaultDir(Dir);
            TrainConfig = DeepClassifierConfig.Create(Dir);
            //tf.Context.Config.GpuOptions.PerProcessGpuMemoryFraction = 0.3;
        }
        public void CreateDefaultDir(string dir)
        {           
            ImageDir = System.IO.Path.Combine(dir, "Classify", "images");
            ModelDir = System.IO.Path.Combine(dir, "Classify", "data");
            AnnotationDir = System.IO.Path.Combine(dir, "Classify", "annotations");
            ConfigDir = System.IO.Path.Combine(dir, "TrainConfig.txt");
            ClasslistDir = System.IO.Path.Combine(dir, "Classify", "classList");
            EvaluationDir = System.IO.Path.Combine(dir, "evaluation");
            if (!System.IO.Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            if (!System.IO.Directory.Exists(ModelDir))
            {
                Directory.CreateDirectory(ModelDir);
            }
            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            if (!System.IO.Directory.Exists(ClasslistDir))
            {
                Directory.CreateDirectory(ClasslistDir);
            }
            if (!System.IO.Directory.Exists(EvaluationDir))
            {
                Directory.CreateDirectory(EvaluationDir);
            }
        }
        public override void OnInitialize()
        {
            Runtime = new ClassifierONNX(TrainConfig.ModelDir);
            //runtime.LoadRecipe(ModelDir);
        }
        public override void Dispose()
        {
            base.Dispose();
            Runtime.Dispose();
        }
        public void Record()
        {
            var image = ImageInput.Value.Clone();
            var filename = Functions.RandomFileName(TrainConfig.ImageDir);           
            image.WriteImage("png", 0, filename + ".png");
        }
        public InspectionContext context;
        public CollectionOfregion Region { get; set; }
        bool _interactive_region = false;
        public bool InteractiveRegion
        {
            get
            {
                return _interactive_region;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _interactive_region, value);
            }
        }
        public override void OnLoadComplete()
        {
            Runtime?.LoadRecipe(TrainConfig.ModelDir);
        }
        public (ClassResult[],bool) RunInside(HImage image, HRegion Region, InspectionContext e)
        {
            //if ((image != null) &(region ==null)){
            //    HTuple w, h;
            //    image.GetImageSize(out w, out h);
            //    return false;
            //}
            var region_output = new ClassResult[] { };
            if (image == null)
            {
                return (region_output,false);
            }
            if (Runtime.State == ModelState.NotFound)
            {
                return (region_output,false);
            }
            if (Runtime.State == ModelState.Unloaded)
            {
                Runtime.LoadRecipe(TrainConfig.ModelDir);
            }
            if (Runtime.State == ModelState.Loaded)
            {
                HTuple channels = image.CountChannels();
                if (channels < 3)
                {
                    image = image.Compose3(image, image);
                    //return false;
                }
                HTuple message = new HTuple();
                bool i_result = true;
                InspectionResult inspection_result = e.inspection_result;
                var region_inspection = new HRegion();
                //Minh comment
                //if (!_interactive_region)
                //    region_inspection = Region.Region;
                if (Region == null)
                {
                    return (region_output,false);
                }
                else
                {
                    region_inspection = Region;
                }
                //End Minh comment
                HRegion region_color_select = new HRegion();
                region_color_select.GenEmptyObj();
                HRegion regionClassify = region_inspection;
                if (e != null & ShowDisplay)
                {
                    e.inspection_result.AddRegion(region_inspection, "blue");
                }
                
                HRegion threshold = region_inspection.Intersection(regionClassify).ClosingCircle((HTuple)2.5).Connection();
                region_color_select = threshold.SelectShape("area", "and", 10, 99999999);
                if (region_color_select.CountObj() > 0)
                {
                    List<ClassResult> result = Classify(image, region_color_select, inspection_result);
                    region_output = result.ToArray();
                    for (int i = 0; i < result.Count; i++)
                    {
                        inspection_result.Add(result[i].regions, result[i].Color, this.Name, "Classifier");
                        inspection_result.Add(result[i].regions, result[i].Color, this.Name, "Classifier");
                        if (!result[i].NG)
                        {

                            continue;
                        }
                        if (result[i].Confidence > result[i].TargetClass.ConfidentThreshold / 100.0)
                        {
                            e.result &= false;
                            i_result = false;
                        }
                    }


                }
                return (region_output,i_result);
            }
            return (region_output,false);
        }
        public HRegion GenSmallestRect(HRegion region)
        {
            HRegion region_rect = new HRegion();
            region_rect.GenEmptyRegion();
            for (int i = 0; i < region.CountObj(); i++)
            {
                int r1, c1, r2, c2;
                region[i + 1].SmallestRectangle1(out r1, out c1, out r2, out c2);
                HRegion cont = new HRegion();
                cont.GenRectangle1((double)r1, c1, r2, c2);
                region_rect = region_rect.ConcatObj(cont);
            }
            return region_rect;
        }
        public double GetMaxArea(HRegion input)
        {
            HTuple Areas = input.Area;
            HTuple sort = Areas.TupleSortIndex();
            if (sort.Type != HTupleType.EMPTY)
            {
                return Math.Round((double)Areas[(int)sort[(int)(sort.Length - 1)]] / (1), 2);
            }
            else return 0;
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
                hommat2d.VectorToHomMat2d(new HTuple(r1, r1, r2), new HTuple(c1, c2, c2), new HTuple(0, 0, r2-r1), new HTuple(0, c2-c1, c2-c1));
                var imagecroped = image.AffineTransImageSize(hommat2d, "constant", c2-c1, r2-r1);

                var inferresult = Runtime.Infer(imagecroped);
                var classIndex = inferresult.Item1;
                if (classIndex >= 0 & classIndex <ClassList.Count)
                {
                    results.Add(new ClassResult(ClassList[classIndex], regions[i + 1], ClassList[classIndex].Color, ClassList[classIndex].NG, inferresult.Item2));

                    if (inspectionResult != null & ShowDisplay)
                    {
                        if(inferresult.Item2 > ClassList[classIndex].ConfidentThreshold / 100.0)
                        {
                            if (DisplayBox)
                            {
                                inspectionResult.AddRegion(new HRegion((double)r1, c1, r2, c2), ClassList[classIndex].Color);
                            }
                            if (DisplayLabel)
                            {
                                if (DisplayBox)
                                {
                                    inspectionResult.AddText(ClassList[classIndex].Name + ": " + (inferresult.Item2 * 100).ToString("N0"), "black", "#ffffffff", r1, c1);
                                }
                                else
                                {
                                    inspectionResult.AddText(ClassList[classIndex].Name + ": " + (inferresult.Item2 * 100).ToString("N0"), "black", "#ffffffff", (r1 + r2) / 2, (c1 + c2) / 2);
                                }
                            }
                        }
                        else
                        {
                            if (DisplayBox)
                            {
                                inspectionResult.AddRegion(new HRegion((double)r1, c1, r2, c2), "red");
                            }
                            if (DisplayLabel)
                            {
                                if (DisplayBox)
                                {
                                    inspectionResult.AddText(ClassList[classIndex].Name + ": " + (inferresult.Item2 * 100).ToString("N0"), "red", "#ffffffff", r1, c1);
                                }
                                else
                                {
                                    inspectionResult.AddText(ClassList[classIndex].Name + ": " + (inferresult.Item2 * 100).ToString("N0"), "red", "#ffffffff", (r1 + r2) / 2, (c1 + c2) / 2);
                                }
                            }
                        }
                        
                    }
                }
            }
            return results;
        }
        


        
        
        public void SaveClassList(string path)
        {
            if (path == null)
            {
                return;
            }
            JObject[] data = new JObject[ClassList.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new JObject();
                data[i]["NG"] = ClassList[i].NG;
                data[i]["Color"] = ClassList[i].Color;
                data[i]["Name"] = ClassList[i].Name;
                data[i]["ConfidentThreshold"] = ClassList[i].ConfidentThreshold;

            }
   
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data));
        }

        public void LoadClassList(string path)
        {
            ClassList.Clear();
            try
            {
                if (System.IO.File.Exists(path))
                {
                    var datatxt = System.IO.File.ReadAllText(path);
                    var data = JsonConvert.DeserializeObject<JObject[]>(datatxt);
                    foreach (var item in data)
                    {
                        string Name = item["Name"].ToString();
                        string Color = item["Color"].ToString();
                        bool NG = item["NG"].ToObject<bool>();
                        int confidentThreshold = item["ConfidentThreshold"].ToObject<int>();
                        ClassList.Add(new ClassifierClass1 { Name = Name, Color = Color, NG = NG,ConfidentThreshold = confidentThreshold });
                    }
                }
                
            }
            catch
            {
                return;
            }
        }
    }
    public class ClassifierONNX:ONNXModel
    {
        int input_width, input_height;
        public string ModelDir { get; set; }
        public ClassifierONNX(string ModelDir)
        {
            this.ModelDir = ModelDir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
            else
            {
                            }
        }
        InferenceSession ONNXSession;
        string input_name;
        public void Dispose()
        {
            ONNXSession?.Dispose();
        }
        bool InputFloat = true;
        public bool LoadOnnx(string directory)
        {
            try
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
                }catch(Exception ex)
                {

                }
                if (ONNXSession == null)
                {
                    //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                    {
                        State = ModelState.NotFound;
                        return false;
                    }
                    ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"));
                }
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];
                    if (ONNXSession.InputMetadata[input_name].ElementType == typeof(byte))
                    {
                        InputFloat = false;
                    }
                    else
                    {
                        InputFloat = true;
                    }
                }
                if (InputFloat)
                {
                    Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;

                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    Microsoft.ML.OnnxRuntime.Tensors.Tensor<byte> t1;

                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>(1 * input_width * input_height * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<byte>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
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
        public HImage Normalize(HImage imageInput)
        {
            HImage image = imageInput.ConvertImageType("float") / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
        public (int, double) InferByte(HImage imgInput)
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
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<byte>(array_input,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var result_tensor = new NDArray(TensorOutput.ToArray());
            return (result_tensor.argmax(), (float)result_tensor[result_tensor.argmax()]);
        }
        public (int, double) Infer(HImage imgInput)
        {
            if (InputFloat)
            {
                return InferFloat(imgInput);
            }
            else
            {
                return InferByte(imgInput);
            }
        }
        public (int, double) InferFloat(HImage imgInput)
        {
            int originalw, originalh;
            imgInput.GetImageSize(out originalw, out originalh);
            var ratio = (float)input_width / Math.Max(originalh, originalw);
            var new_width = (int)(originalw * ratio);
            var new_height = (int)(originalh * ratio);
            //var image_resized = imgInput.ZoomImageSize(new_width, new_height, "constant");
            //var hommat2d = new HHomMat2D().HomMat2dTranslate((double)(input_height - new_height) / 2, (input_width - new_width) / 2);
            //var image_padded = image_resized.ProjectiveTransImageSize(hommat2d, "nearest_neighbor", input_width, input_height, "false").FullDomain();
            //image_padded.WriteImage("png", 0, "D:/image.png");
            //image_padded = image_padded.ConvertImageType("float");
            //var normalized = Normalize(image_padded);
            //float[] array_input = new float[input_width * input_height * 3];
            float[] array_input = Processing.HalconUtils.HImageToFloatArray(imgInput.ZoomImageSize(input_width,input_height,"bilinear").ConvertImageType("float"), 3, out int _, out int _);
            //string type;
            //int width, height;
            //IntPtr pointerR, pointerG, pointerB;
            //image_padded.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
            //Marshal.Copy(pointerR, array_input, 0, input_width * input_height);
            //Marshal.Copy(pointerG, array_input, input_width * input_height, input_width * input_height);
            //Marshal.Copy(pointerB, array_input, input_width * input_height * 2, input_width * input_height);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_input,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var result_tensor  = new NDArray(TensorOutput.ToArray());
            return (result_tensor.argmax(), (float)result_tensor[result_tensor.argmax()]);
        }
    }
    public class DeepClassifierConfig
    {
        public string ModelName { get; set; } = "wide_resnet50_2";
        public int Epoch { get; set; } = 100;
        public int FinetuneEpoch { get; set; } = 10;
        public string ModelDir { get; set; }
        public string ImageDir { get; set; }
        public string EvaluationDir { get; set; }
        public string AnnotationDir { get; set; }
        public string PredictDir { get; set; }
        public int Batchsize { get; set; } = 2;
        public string ResultDir { get; set; }
        public string ClassListDir { get; set; }
        public int Patience { get; set; } = 2;
        public double INIT_LR { get; set; } = 1e-3;
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public string ClassList { get; set; }
        public Albumentations Augmentations { get; set; } = new Albumentations();
        public string classList_path { get; set; }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public string DatasetType { get; set; } = "novision";
        public bool InputAsChannelLast { get; set; } = false;
        public int SaveInterval { get; set; } = 10;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TrainType { get; set; } = TrainResume.New;
        [JsonConverter(typeof(StringEnumConverter))]
        public CheckPointEnum CheckPoint { get; set; } = CheckPointEnum.Latest;
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public static DeepClassifierConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            DeepClassifierConfig option;
            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<DeepClassifierConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                option = config;
            }
            else
            {
                option = new DeepClassifierConfig(BaseDir);
            }
            
            return option;

        }
        public DeepClassifierConfig(string BaseDir)
        {
            ApplyRelativeDir(BaseDir);
            Save();
        }
        private void ApplyRelativeDir(string BaseDir)
        {
            ImageDir = System.IO.Path.Combine(BaseDir, "Classify", "images");
            ModelDir = System.IO.Path.Combine(BaseDir, "Classify", "data");
            AnnotationDir = System.IO.Path.Combine(BaseDir, "Classify", "annotations");
            ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");
            ClassListDir = System.IO.Path.Combine(BaseDir, "Classify", "classList");
            classList_path = System.IO.Path.Combine(ClassListDir, "classList.txt");
            EvaluationDir = System.IO.Path.Combine(BaseDir, "evaluation");
            if (!System.IO.Directory.Exists(ImageDir))
            {
                Directory.CreateDirectory(ImageDir);
            }
            if (!System.IO.Directory.Exists(ModelDir))
            {
                Directory.CreateDirectory(ModelDir);
            }
            if (!System.IO.Directory.Exists(AnnotationDir))
            {
                Directory.CreateDirectory(AnnotationDir);
            }
            if (!System.IO.Directory.Exists(ClassListDir))
            {
                Directory.CreateDirectory(ClassListDir);
            }
            if (!System.IO.Directory.Exists(EvaluationDir))
            {
                Directory.CreateDirectory(EvaluationDir);
            }
            this.BaseDir = BaseDir;
        }
        public DeepClassifierConfig()
        {

        }
    }
    public class Albumentations
    {
        public bool EnableRandomResizedCrop { get; set; }
        public bool EnableRandomRotate90 { get; set; }
        public bool EnableRotation { get; set; }
        public bool VerticalFlip { get; set; }
        public bool HorizontalFlip { get; set; }
        public bool EnableBrightness { get; set; }

        public int RandomRotate90Factor { get; set; } = 1;

        public int RotationVariation { get; set; } = 20;

        public int Brightness { get; set; } = 20;
        public int Contrast { get; set; } = 20;

        public Range RandomCropResizeScale { get; set; } = new Range() { Start = 0.08, Stop = 1.0 };
        public Range RandomCropResizeRatio { get; set; } = new Range() { Start = 0.75, Stop = 1.3 };
    }
    public class Range
    {
        public double Start { get; set; }
        public double Stop { get; set; }
    }
}