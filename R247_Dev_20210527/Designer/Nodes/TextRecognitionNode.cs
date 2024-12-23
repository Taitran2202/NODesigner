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
using Microsoft.Win32;
using Newtonsoft.Json.Converters;
using NOVisionDesigner.Designer.Extensions;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.PropertiesViews;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "Text Recognition", Icon: "Designer/icons/icons8-abc-96.png")]
    public class TextRecognitionNode : BaseNode
    {
        
        public override void OnLoadComplete()
        {
            try
            {
                if (Warmup)
                {
                    if (Runtime.State == ModelState.Unloaded)
                    {
                        Runtime.LoadRecipe();
                        Runtime.Initialize(MaxTextWidth);
                    }
                    if (Runtime.State == ModelState.Error)
                    {
                        App.GlobalLogger.LogError(this.Name, "Cannot warmup due to model loading error!");
                        return;
                    }
                    if (Runtime.State == ModelState.Loaded)
                    {
                        HImage dumpyImage = new HImage("byte", 128, 32);
                        List<HImage> images = new List<HImage>();
                        for (int i = 0; i < Math.Min(20, Math.Max(1, WarmupBatch)); i++)
                        {
                            images.Add(dumpyImage);
                        }
                        var result = Runtime.InferBatch(images, MaxTextWidth);
                    }
                }
            }catch(Exception ex)
            {

            }
            
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);    
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    TextRecognitionWindow wd = new TextRecognitionWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
            }

        }
        [SerializeIgnore]
        public Control PropertiesView
        {
            get
            {
                return new TextRecognitionPropertiesView(this);
            }
        }
        [HMIProperty("Model Loader")]
        public IReactiveCommand ModelLoader
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    ONNXModelLoaderWindow wd = new ONNXModelLoaderWindow(Runtime);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                }
            );
            }
        }
        [HMIProperty("Train Window")]
        public IReactiveCommand OpenTrainWindowCommand
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    OnCommand("editor",sender);
                    
                });
            }
        }
        [HMIProperty("Upload Model")]
        public IReactiveCommand UploadModelCommand
        {
            get { return ReactiveCommand.Create((Control sender) => UploadModel()); }
        }
        
        [HMIProperty("Change Model")]
        public IReactiveCommand ChangeModelCommand
        {
            get { return ReactiveCommand.Create((Control sender) =>
            {
                Deeplearning.Windows.PPOCR.ModelSelectionWindow wd = new Deeplearning.Windows.PPOCR.ModelSelectionWindow(Runtime);
                if (sender != null)
                {
                    wd.Owner = Window.GetWindow(sender);
                }
               
                wd.ShowDialog();
            }); }
        }
        #region Properties
        [HMIProperty]
        [Description("Increase text area by a ratio.")]
        public double ExpandRatio { get; set; } = 1.5;
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<bool> Rotated { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeOutputViewModel<string[]> TextOutput { get; set; }
        public ValueNodeOutputViewModel<Rect2[]> BoxOutput { get; set; }
        public ValueNodeOutputViewModel<CharacterResult[]> TextsBoxesOutput { get; set; }
        public TextRecognitionConfig TrainConfig { get; set; }
        [HMIProperty]
        [Description("Text display position")]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        [HMIProperty]
        [Description("Text display offset X")]
        public int DisplayOffsetX { get; set; } = 0;
        [HMIProperty]
        [Description("Text display offset Y")]
        public int DisplayOffsetY { get; set; } = 0;
        [HMIProperty]
        [Description("Text color")]
        public string TextForegroundColor { get; set; } = "#000000ff";
        [HMIProperty]
        [Description("Text background color")]
        public string TextBackgroundColor { get; set; } = "#ffffffff";
        [HMIProperty]
        [Description("Text fontsize")]
        public int TextFontsize { get; set; } = 16;
        [HMIProperty]
        [Description("Text bounding box border color")]
        public string BoxColor { get; set; } = "#00ff00ff";
        public string RootDir;
        /// <summary>
        /// Allow to run the text recognition model after loading.
        /// </summary>
        [Description("Allow to run the text recognition model after loading.")]
        public bool Warmup { get; set; } = false;
        /// <summary>
        /// Number of batches use for warmup.
        /// </summary>
        [Description("Number of batches use for warmup.")]
        public int WarmupBatch { get; set; } = 5;
        /// <summary>
        /// Maximum text width in pixel to read.
        /// </summary>
        [Description("Maximum text width in pixel to read.")]
        public int MaxTextWidth { get; set; } = 500;

        public PPOCRV3TextRecognition Runtime { get; set; }
        #endregion
        void UploadModel()
        {
            OpenFileDialog wd = new OpenFileDialog();
            if(wd.ShowDialog()== true)
            {
                foreach(var file in wd.FileNames)
                {
                    if (file.Contains(".onnx"))
                    {
                        System.IO.File.Copy(file,System.IO.Path.Combine(this.RootDir,"model.onnx"), true);
                    }
                    if (file.Contains("dict_file.txt"))
                    {
                        System.IO.File.Copy(file, System.IO.Path.Combine(this.RootDir, "dict_file.txt"), true);
                    }
                }
                
            }
        }
        static TextRecognitionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<TextRecognitionNode>));
        }

        public TextRecognitionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Text Recognition";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Texts Region",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);
            Rotated = new ValueNodeInputViewModel<bool>()
            {
                Name = "Rotated",
                Editor = new BoolValueEditorViewModel(),
                PortType = "boolean",
                Visibility = NodeNetwork.ViewModels.EndpointVisibility.AlwaysHidden
            };
            this.Inputs.Add(Rotated);

            TextOutput = new ValueNodeOutputViewModel<string[]>()
            {
                Name = "Text",
                PortType = "string"
            };
            this.Outputs.Add(TextOutput);
            BoxOutput = new ValueNodeOutputViewModel<Rect2[]>()
            {
                Name = "Text Boxes",
                PortType = "Rect2"
            };
            this.Outputs.Add(BoxOutput);
            TextsBoxesOutput = new ValueNodeOutputViewModel<CharacterResult[]>()
            {
                Name = "Text&Boxes",
                PortType = "CharacterResult[]"
            };
            this.Outputs.Add(TextsBoxesOutput);

            TrainConfig = TextRecognitionConfig.Create(RootDir);
        }
        
        public override void OnInitialize()
        {
            Runtime = new PPOCRV3TextRecognition(TrainConfig.ModelDir);
            
        }
        
        public override void Run(object context)
        {
            var text = ReadText(Image.Value,RegionInput.Value);
            var IContext = context as InspectionContext;
            if (ShowDisplay)
            {
                foreach(var item in text)
                {
                    IContext.inspection_result.AddRect2(BoxColor, item.Box.row, item.Box.col, item.Box.phi, item.Box.length1, item.Box.length2);
                    //IContext.inspection_result.AddText(item.ClassName, "black", "#fffffff0", item.Box.row + item.Box.length2, item.Box.col -item.Box.length1) ;
                }
                foreach (var item in text)
                {
                    if (!Rotated.Value)
                    {
                        var textlocation = Functions.GetDisplayPosition(DisplayPosition, item.Box.row - item.Box.length2, item.Box.col - item.Box.length1, item.Box.row + item.Box.length2, item.Box.col + item.Box.length1);
                        IContext.inspection_result.AddText(item.ClassName,TextForegroundColor, TextBackgroundColor, textlocation.row+DisplayOffsetY, textlocation.col+DisplayOffsetX, TextFontsize);
                    }
                    else
                    {
                        var textlocation = Functions.GetDisplayPosition(DisplayPosition,item.Box.row - item.Box.length2, item.Box.col - item.Box.length1, item.Box.row + item.Box.length2, item.Box.col + item.Box.length1);
                        IContext.inspection_result.AddText(item.ClassName, TextForegroundColor, TextBackgroundColor, textlocation.row-40+DisplayOffsetY,textlocation.col+DisplayOffsetX, TextFontsize);
                    }
                    //IContext.inspection_result.AddRect2("green", item.Box.row, item.Box.col, item.Box.phi, item.Box.length1, item.Box.length2);
                    
                }
            }
            TextOutput.OnNext(text.Select(x => x.ClassName.Replace(" ", "")).ToArray());
            BoxOutput.OnNext(text.Select(x => x.Box).ToArray());
            TextsBoxesOutput.OnNext(text.Select(x => new CharacterResult() { ClassName = x.ClassName.Replace(" ", ""), Box = x.Box }).ToArray());
        }



        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public static List<Rect2> HRegionToRect2(HRegion regionInput)
        {
            List<Rect2> resultBoxes = new List<Rect2>();
            if (regionInput.CountObj() == 0)
            {
                return resultBoxes;
            }
            regionInput.SmallestRectangle2(out HTuple r, out HTuple c, out HTuple phi, out HTuple l1, out HTuple l2);
            var count = r.Length;

            for (int i = 0; i < count; i++)
            {
                if (l1[i].D == 0 | l2[i].D == 0)
                {
                    continue;
                }
                double distance = regionInput[i + 1].Area * 1.5 / ((l1[i] + l2[i]) * 2);
                l1[i] = l1[i] + distance / 2;
                l2[i] = l2[i] + distance / 2;
                double row = r[i];
                double col = c[i];
                double phiIndex = phi[i];
                double length1 = l1[i];
                double length2 = l2[i];
                resultBoxes.Add(new Rect2() { row = r[i], col = c[i], length1 = l1[i], length2 = l2[i], phi = phi[i] });
            }
            return resultBoxes;
        }
        public List<CharacterResult> ReadText(HImage input, HRegion regionInput)
        {
            List<CharacterResult> detectedText = new List<CharacterResult>();
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
                    if (regionInput.CountObj() == 0)
                    {
                        return new List<CharacterResult>();
                    }
                    regionInput.SmallestRectangle2(out HTuple r, out HTuple c, out HTuple phi, out HTuple l1,out HTuple l2);
                    var count = r.Length;
                    List<HImage> textGroup = new List<HImage>();
                    List<Rect2> resultBoxes = new List<Rect2>();
                    for (int i = 0; i < count; i++)
                    {
                        if(l1[i].D==0 | l2[i].D== 0)
                        {
                            continue;
                        }
                        double distance = regionInput[i+1].Area * ExpandRatio / ( (l1[i] + l2[i])*2);
                        l1[i] = l1[i] + distance / 2;
                        l2[i] = l2[i] + distance / 2;
                        HImage textArea;
                        double row = r[i];
                        double col = c[i];
                        double phiIndex = phi[i];
                        double length1 = l1[i];
                        double length2 = l2[i];

                        if (length1 < length2)
                        {
                            var rotatedAngle = phiIndex + Math.PI / 2;
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D.VectorAngleToRigid(row, col, 0, length1, length2, -rotatedAngle);
                            textArea = hHomMat2D.AffineTransImageSize(input, "constant", (int)(length2 * 2), (int)(length1 * 2));

                        }
                        else
                        {
                            var rotatedAngle = phiIndex;
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D.VectorAngleToRigid(row, col, 0, length2, length1, -rotatedAngle);
                            textArea = hHomMat2D.AffineTransImageSize(input, "constant", (int)(length1 * 2), (int)(length2 * 2));
                        }
                        resultBoxes.Add(new Rect2() { row = r[i], col = c[i], length1 = l1[i], length2 = l2[i], phi = phi[i] });
                        if (Rotated.Value)
                        {
                            textGroup.Add(textArea.RotateImage(180.0,"constant"));
                        }
                        else
                        {
                            textGroup.Add(textArea);
                        }
                        
                        //textArea.WriteImage("bmp", 0, @"D:\test.bmp");
                        
                    }
                    var result = Runtime.InferBatch(textGroup,MaxTextWidth);
                    for (int i = 0; i < result.Count; i++)
                    {
                        detectedText.Add(new CharacterResult() { Box = resultBoxes[i], ClassName = result[i] });
                    }
                    

                    return detectedText;

                }
            }
            //var region = new HRegion();
            //region.GenEmptyRegion();
            return new List<CharacterResult>();
        }
    }

    [NodeInfo("Deep Learning", "Text Recognition 2",visible:false)]
    public class TextRecognitionNodeParseq : BaseNode
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
            HelperMethods.LoadParam(item, this);
        }
        [HMIProperty("Upload Model")]
        public IReactiveCommand UploadModelCommand
        {
            get { return ReactiveCommand.Create((Control sender) => UploadModel()); }
        }
        [HMIProperty("Change Model")]
        public IReactiveCommand ChangeModelCommand
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    //Deeplearning.Windows.PPOCR.ModelSelectionWindow wd = new Deeplearning.Windows.PPOCR.ModelSelectionWindow(Runtime);
                    //if (sender != null)
                    //{
                    //    wd.Owner = Window.GetWindow(sender);
                    //}

                    //wd.ShowDialog();
                });
            }
        }
        #region Properties
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<bool> Rotated { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeOutputViewModel<string[]> TextOutput { get; set; }
        public ValueNodeOutputViewModel<Rect2[]> BoxOutput { get; set; }
        public TextRecognitionConfig TrainConfig { get; set; }
        [HMIProperty]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        public (int row, int col) GetDisplayPosition(int r1, int c1, int r2, int c2)
        {
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return (r2, c1);
                case TextPosition.Top:
                    return (r1, c1);
                case TextPosition.Left:
                    return (r2, c1);
                default:
                    return (r1, c2);
            }
        }
        public (int row, int col) GetDisplayPosition(double r1, double c1, double r2, double c2)
        {
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return ((int)r2, (int)c1);
                case TextPosition.Top:
                    return ((int)r1, (int)c1);
                case TextPosition.Left:
                    return ((int)r2, (int)c1);
                default:
                    return ((int)r1, (int)c2 + 10);
            }
        }
        public string RootDir;

        public ParsegTextRecognition Runtime { get; set; }
        #endregion
        void UploadModel()
        {
            OpenFileDialog wd = new OpenFileDialog();
            if (wd.ShowDialog() == true)
            {
                foreach (var file in wd.FileNames)
                {
                    if (file.Contains(".onnx"))
                    {
                        System.IO.File.Copy(file, System.IO.Path.Combine(this.RootDir, "model.onnx"), true);
                    }
                    if (file.Contains("dict_file.txt"))
                    {
                        System.IO.File.Copy(file, System.IO.Path.Combine(this.RootDir, "dict_file.txt"), true);
                    }
                }

            }
        }
        static TextRecognitionNodeParseq()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<TextRecognitionNodeParseq>));
        }

        public TextRecognitionNodeParseq(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Text Recognition";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);

            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Texts Region",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);
            Rotated = new ValueNodeInputViewModel<bool>()
            {
                Name = "Rotated",
                Editor = new BoolValueEditorViewModel(),
                PortType = "boolean"
            };
            this.Inputs.Add(Rotated);

            TextOutput = new ValueNodeOutputViewModel<string[]>()
            {
                Name = "Text",
                PortType = "string"
            };
            this.Outputs.Add(TextOutput);
            BoxOutput = new ValueNodeOutputViewModel<Rect2[]>()
            {
                Name = "Text Boxes",
                PortType = "Rect2"
            };
            this.Outputs.Add(BoxOutput);

            TrainConfig = TextRecognitionConfig.Create(RootDir);
        }
        public override void OnInitialize()
        {
            Runtime = new ParsegTextRecognition(TrainConfig.ModelDir);
        }
        public override void Run(object context)
        {
            var text = ReadText(Image.Value, RegionInput.Value);
            var IContext = context as InspectionContext;
            if (ShowDisplay)
            {
                foreach (var item in text)
                {
                    IContext.inspection_result.AddRect2("green", item.Box.row, item.Box.col, item.Box.phi, item.Box.length1, item.Box.length2);
                    //IContext.inspection_result.AddText(item.ClassName, "black", "#fffffff0", item.Box.row + item.Box.length2, item.Box.col -item.Box.length1) ;
                }
                foreach (var item in text)
                {
                    if (!Rotated.Value)
                    {
                        var textlocation = GetDisplayPosition(item.Box.row - item.Box.length2, item.Box.col - item.Box.length1, item.Box.row + item.Box.length2, item.Box.col + item.Box.length1);
                        IContext.inspection_result.AddText(item.ClassName, "black", "#fffffff0", textlocation.row, textlocation.col, 16);
                    }
                    else
                    {
                        var textlocation = GetDisplayPosition(item.Box.row - item.Box.length2, item.Box.col - item.Box.length1, item.Box.row + item.Box.length2, item.Box.col + item.Box.length1);
                        IContext.inspection_result.AddText(item.ClassName, "black", "#fffffff0", textlocation.row - 40, textlocation.col, 16);
                    }
                    //IContext.inspection_result.AddRect2("green", item.Box.row, item.Box.col, item.Box.phi, item.Box.length1, item.Box.length2);

                }
            }
            TextOutput.OnNext(text.Select(x => x.ClassName.Replace(" ", "")).ToArray());
            BoxOutput.OnNext(text.Select(x => x.Box).ToArray());
        }


        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    //AnomalyV3Window wd = new AnomalyV3Window(this);
                    //wd.ShowDialog();
                    break;
            }

        }

        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        
        public List<CharacterResult> ReadText(HImage input, HRegion regionInput)
        {
            List<CharacterResult> detectedText = new List<CharacterResult>();
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
                    if (regionInput.CountObj() == 0)
                    {
                        return new List<CharacterResult>();
                    }
                    regionInput.SmallestRectangle2(out HTuple r, out HTuple c, out HTuple phi, out HTuple l1, out HTuple l2);
                    var count = r.Length;
                    List<HImage> textGroup = new List<HImage>();
                    List<Rect2> resultBoxes = new List<Rect2>();
                    for (int i = 0; i < count; i++)
                    {
                        if (l1[i].D == 0 | l2[i].D == 0)
                        {
                            continue;
                        }
                        double distance = regionInput[i + 1].Area * 1.5 / ((l1[i] + l2[i]) * 2);
                        l1[i] = l1[i] + distance / 2;
                        l2[i] = l2[i] + distance / 2;
                        HImage textArea;
                        double row = r[i];
                        double col = c[i];
                        double phiIndex = phi[i];
                        double length1 = l1[i];
                        double length2 = l2[i];

                        if (length1 < length2)
                        {
                            var rotatedAngle = phiIndex + Math.PI / 2;
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D.VectorAngleToRigid(row, col, 0, length1, length2, -rotatedAngle);
                            textArea = hHomMat2D.AffineTransImageSize(input, "constant", (int)(length2 * 2), (int)(length1 * 2));

                        }
                        else
                        {
                            var rotatedAngle = phiIndex;
                            HHomMat2D hHomMat2D = new HHomMat2D();
                            hHomMat2D.VectorAngleToRigid(row, col, 0, length2, length1, -rotatedAngle);
                            textArea = hHomMat2D.AffineTransImageSize(input, "constant", (int)(length1 * 2), (int)(length2 * 2));
                        }
                        resultBoxes.Add(new Rect2() { row = r[i], col = c[i], length1 = l1[i], length2 = l2[i], phi = phi[i] });
                        if (Rotated.Value)
                        {
                            textGroup.Add(textArea.RotateImage(180.0, "constant"));
                        }
                        else
                        {
                            textGroup.Add(textArea);
                        }

                        //textArea.WriteImage("bmp", 0, @"D:\test.bmp");

                    }
                    var result = Runtime.InferBatch(textGroup);
                    for (int i = 0; i < result.Count; i++)
                    {
                        detectedText.Add(new CharacterResult() { Box = resultBoxes[i], ClassName = result[i] });
                    }


                    return detectedText;

                }
            }
            //var region = new HRegion();
            //region.GenEmptyRegion();
            return new List<CharacterResult>();
        }
    }
    public interface ITextRecognition
    {
        bool LoadRecipe();
        List<string> Infer(List<HImage> imgInput);
    }
    public enum PPOCRRecognitionModel
    {
        Small,Large,Parseg
    }
    public class PPOCRV3TextRecognition : ONNXModel,IHalconDeserializable
    {
        public static Dictionary<PPOCRRecognitionModel, (string ModelName, string DictionaryName)> Map = new Dictionary<PPOCRRecognitionModel, (string ModelName, string DictionaryName)>()
        {
            {PPOCRRecognitionModel.Small,("en_PP-OCRv3_rec.onnx","ppocr_keys_v1.txt") },
            {PPOCRRecognitionModel.Large,("ch_ppocr_server_v2.0_rec.onnx","ppocr_keys_v1.txt") },
            {PPOCRRecognitionModel.Parseg,("parseq_rec.onnx","parseq_dict.txt") },
        };


        public void LoadDefaultModel()
        {

        }
        public void Wramup(int batchcount)
        {

        }
        public PPOCRV3TextRecognition(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        public int FixedWidth { get; set; } = -1;
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
            var g_pointer = r.GetImagePointer1(out type, out w, out h);
            var b_pointer = r.GetImagePointer1(out type, out w, out h);
            Marshal.Copy(r_pointer, result, 0, nw * nh);
            Marshal.Copy(g_pointer, result, nw * nh, nw * nh);
            Marshal.Copy(b_pointer, result, 2 * nw * nh, nw * nh);
            return result;
        }
        //public (float[] output,int resizeW,int resizeH) ResizeNormImgSvtrLCNet(HImage img)
        //{
        //    int dstWidth, resizeWidth;
        //    FindOptimalResize(img,out resizeWidth, out dstWidth );
        //    var resizedImg = img.ZoomImageSize(resizeWidth, input_height, "bilinear");
        //    var imageFloat = resizedImg.ConvertImageType("float");
        //    imageFloat = imageFloat / 255;
        //    imageFloat -= 0.5;
        //    imageFloat /= 0.5;
        //    imageFloat = imageFloat.ChangeFormat(dstWidth, input_height);
        //    float[] result = Processing.HalconUtils.HImageToFloatArray(imageFloat, 3, out int outw, out int outh);
        //    return (result, dstWidth, input_height);
        //}
        public float[] ResizeNormalize(HImage image, int resizeW,int resizeH,int dstW,int dstH)
        {
            var resizedImg = image.ZoomImageSize(resizeW, resizeH, "bilinear");
            //resizedImg.WriteImage("bmp", 0, "D:/test2.bmp");
            resizedImg =resizedImg.ChangeFormat(dstW, dstH);
            resizedImg=resizedImg.FullDomain();
            var fillRegion = new HRegion();
            fillRegion.GenRectangle1(0.0, resizeW, dstH, dstW);
            resizedImg.OverpaintRegion(fillRegion.ConcatObj(fillRegion).ConcatObj(fillRegion),new HTuple(255,255,255), "fill");
            //resizedImg.WriteImage("bmp", 0, "D:/test.bmp");
            var imageFloat = resizedImg.ConvertImageType("float");
            imageFloat = imageFloat / 255;
            imageFloat -= 0.5;
            imageFloat /= 0.5;
            //imageFloat = imageFloat.ChangeFormat(dstW, dstH);
            //imageFloat.OverpaintRegion(new HRegion(0, dstW - resizeW, resizeH, dstW),255,"");
            float[] result = Processing.HalconUtils.HImageToFloatArray(imageFloat, 3, out int outw, out int outh);
            return result;
        }

        private void FindOptimalResize(HImage img,out int dstWidth,int MaxWidth,out int resizeWidth)
        {
            img.GetImageSize(out int w, out int h);
            var max_wh_ratio = input_width * 1.0 / input_height;
            var wh_ratio = w / (float)(h);
            max_wh_ratio = Math.Max(max_wh_ratio, wh_ratio);
            //dstWidth = (int)((input_height * max_wh_ratio));           
            dstWidth = MaxWidth;
            if (Math.Ceiling(input_height * wh_ratio) > dstWidth)
            {
                resizeWidth = dstWidth;
            }
            else
            {
                resizeWidth = (int)(Math.Ceiling(input_height * wh_ratio));
            }
        }

        public (int resize_w, int resize_h) CalculateResize(int w,int h,int limit_side_len =960,int MinDiv = 32)
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
            return (resize_w,resize_h);
        }
        public string ModelDir;
        int MaxDimension = 960;
        int MinDiv = 32;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        int num_char= 6625;
        string[] TextDict;
        void LoadDictFile(string fileName)
        {
            TextDict = System.IO.File.ReadLines(fileName).ToArray();
        }
        public void CopyModel(PPOCRRecognitionModel ModelName,string targetDir)
        {
            if (Map.ContainsKey(ModelName))
            {
                var data = Map[ModelName];
                var PublicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var modelsDirectory = System.IO.Path.Combine(PublicDirectory, ".novision", "models");
                var ModelPath = System.IO.Path.Combine(modelsDirectory, data.ModelName);
                var DictionaryPath = System.IO.Path.Combine(modelsDirectory, data.DictionaryName);
                if (System.IO.File.Exists(ModelPath) & System.IO.File.Exists(DictionaryPath))
                {
                    System.IO.File.Copy(ModelPath, System.IO.Path.Combine(targetDir,"model.onnx"),true);
                    System.IO.File.Copy(DictionaryPath, System.IO.Path.Combine(targetDir, "dict_file.txt"),true);
                }
            }
            
            
        }
        private bool LoadOnnx(string directory)
        {
            try
            {
                var dictfile = System.IO.Path.Combine(directory, "dict_file.txt");
                var modelFile = System.IO.Path.Combine(directory, "model.onnx");
                if(!System.IO.File.Exists(dictfile) | !System.IO.File.Exists(modelFile))
                {
                    CopyModel(PPOCRRecognitionModel.Small, directory);
                }
                LoadDictFile(dictfile);
                SessionOptions options = CreateProviderOption(directory);
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
                    num_channel = ONNXSession.InputMetadata[input_name].Dimensions[1];

                }
                if (input_height == -1)
                {
                    input_height = 32;
                }
                if (input_width != -1)
                {
                    FixedWidth = input_width;
                }
                else
                {
                    FixedWidth = -1;
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
        public void Initialize(int MaxWidth)
        {
            try
            {
                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;

                if (FixedWidth == -1)
                {
                    input_width = MaxWidth;

                }
                else
                {
                    input_width = FixedWidth;
                }

                t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * num_channel);

                var inputs = new List<NamedOnnxValue>()
                {
                    NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, num_channel, input_height, input_width })),
                };

                using (var results = ONNXSession.Run(inputs))
                {

                }
            }catch(Exception ex)
            {

            }
            
            
        }
        public List<string> InferBatch(List<HImage> imgInput,int MaxWidth)
        {
            int SelectedWidth = MaxWidth;
            if (FixedWidth != -1)
            {
                SelectedWidth = FixedWidth;
            }
            if (imgInput.Count == 0)
            {
                return new List<string>();
            }
            List<string> result = new List<string>();
            List<int> resizeWidths = new List<int>();
            List<int> dstWidths = new List<int>();
            if (input_width == -1)
            {
                for (int i = 0; i < imgInput.Count; i++)
                {
                    var img = imgInput[i];
                    if (img.CountChannels() != num_channel)
                    {
                        if (num_channel == 1)
                        {
                            imgInput[i] = img.Rgb1ToGray();
                        }
                        else
                        {
                            imgInput[i] = img.Compose3(img, img);
                        }
                    }
                    FindOptimalResize(img, out int dstWidth, SelectedWidth, out int resizeWidth);
                    resizeWidths.Add(resizeWidth);
                    dstWidths.Add(dstWidth);

                }
            }
            else
            {
                for (int i = 0; i < imgInput.Count; i++)
                {
                    var img = imgInput[i];
                    if (img.CountChannels() != num_channel)
                    {
                        if (num_channel == 1)
                        {
                            imgInput[i] = img.Rgb1ToGray();
                        }
                        else
                        {
                            imgInput[i] = img.Compose3(img, img);
                        }
                    }
                    FindOptimalResize(img, out int dstWidth, SelectedWidth,out int resizeWidth);
                    dstWidths.Add(dstWidth);
                    resizeWidths.Add(resizeWidth);
                }
            }
            
            var maxWidth = dstWidths.Max();
            var enDict = TextDict.Select(x => x[0] <= 255).ToArray();
            NDArray filtered = new NDArray(enDict);
            for (int i = 0; i < imgInput.Count; i++)
            {
                var img = imgInput[i];
                var imageNormalize = ResizeNormalize(img,resizeWidths[i],input_height, maxWidth, input_height);
                var inputs = new List<NamedOnnxValue>()
                {
                    NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(imageNormalize,new int[]{1,num_channel, input_height,maxWidth }))
                };
                var results = ONNXSession.Run(inputs).ToList();
                var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                NDArray textArray = new NDArray(TensorOutput.ToArray(), new int[] { (int)TensorOutput.Length }).reshape(-1, TensorOutput.Dimensions[2]);
                var textIndex = np.argmax(textArray*filtered, 1);
                var textIndexInt = textIndex.ToArray<int>();
                var selection = np.ones(new Shape(textIndex.size), typeof(bool));
                selection["1:"] = (textIndex["1:"] == textIndex[":-1"]) == false;
                //selection = (selection & ((textIndex == 0) ==false).astype(NPTypeCode.Boolean));
                var selectionBool = selection.ToArray<bool>();
                string text = "";
                List<float> confidence = new List<float>();
                for (int j = 0; j < textIndexInt.Length; j++)
                {
                    if ((textIndexInt[j] == 0) )
                    {
                        continue;
                    }
                    if (selectionBool[j])
                    {
                        text = text + TextDict[textIndex[j]];
                        confidence.Add(textArray[j][textIndexInt[j]]);
                    }


                }
                

                result.Add(text);
                Console.WriteLine(text + "    [{0}]", string.Join(", ", confidence));

            }

            
            return result;

        }
        //public string Infer(HImage imgInput)
        //{
        //    if (imgInput.CountChannels() != num_channel)
        //    {
        //        if (num_channel == 1)
        //        {
        //            imgInput = imgInput.Rgb1ToGray();
        //        }
        //        else
        //        {
        //            imgInput = imgInput.Compose3(imgInput, imgInput);
        //        }
        //    }
        //    var imageNormalize = ResizeNormImgSvtrLCNet(imgInput);
        //    var inputs = new List<NamedOnnxValue>()
        //    {
        //        NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(imageNormalize.output,new int[]{1,num_channel, imageNormalize.resizeH, imageNormalize.resizeW }))
        //    };
        //    var results = ONNXSession.Run(inputs).ToList();
        //    var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
        //    NDArray textArray = new NDArray(TensorOutput.ToArray(), new int[] { (int)TensorOutput.Length }).reshape(-1,num_char);
        //    var textIndex = np.argmax(textArray, 1);
        //    var textIndexInt = textIndex.ToArray<int>();
        //    var selection = np.ones(new Shape(textIndex.size), typeof(bool));
        //    selection["1:"] = (textIndex["1:"] == textIndex[":-1"])==false;
        //    //selection = (selection & ((textIndex == 0) ==false).astype(NPTypeCode.Boolean));
        //    var selectionBool = selection.ToArray<bool>();
        //    string text = "";

        //    for (int i = 0; i < textIndexInt.Length; i++)
        //    {
        //        if (textIndexInt[i] == 0)
        //        {
        //            continue;
        //        }
        //        if (selectionBool[i])
        //        {
        //            text = text + TextDict[textIndex[i]];
        //        }
               
                
        //    }
        //    return text;

        //}

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class ParsegTextRecognition : ONNXModel
    {
        public static Dictionary<PPOCRRecognitionModel, (string ModelName, string DictionaryName)> Map = new Dictionary<PPOCRRecognitionModel, (string ModelName, string DictionaryName)>()
        {
            {PPOCRRecognitionModel.Small,("en_PP-OCRv3_rec.onnx","ppocr_keys_v1.txt") },
            {PPOCRRecognitionModel.Large,("ch_ppocr_server_v2.0_rec.onnx","ppocr_keys_v1.txt") },
            {PPOCRRecognitionModel.Parseg,("parseq_rec.onnx","parseq_dict.txt") },
        };


        public void LoadDefaultModel()
        {

        }
        public ParsegTextRecognition(string dir)
        {
            ModelDir = dir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
        string input_name;

        public float[] ResizeNormalize(HImage image, int resizeW, int resizeH, int dstW, int dstH)
        {
            var resizedImg = image.ZoomImageSize(resizeW, resizeH, "bilinear");
            var imageFloat = resizedImg.ConvertImageType("float");
            imageFloat = imageFloat / 255;
            imageFloat -= 0.5;
            imageFloat /= 0.5;
            imageFloat = imageFloat.ChangeFormat(dstW, dstH);
            float[] result = Processing.HalconUtils.HImageToFloatArray(imageFloat, 3, out int outw, out int outh);
            return result;
        }

        private void FindOptimalResize(HImage img, out int dstWidth, out int resizeWidth)
        {
            img.GetImageSize(out int w, out int h);
            var max_wh_ratio = input_width * 1.0 / input_height;
            var wh_ratio = w / (float)(h);
            max_wh_ratio = Math.Max(max_wh_ratio, wh_ratio);
            dstWidth = (int)((input_height * max_wh_ratio));
            if (Math.Ceiling(input_height * wh_ratio) > dstWidth)
            {
                resizeWidth = dstWidth;
            }
            else
            {
                resizeWidth = (int)(Math.Ceiling(input_height * wh_ratio));
            }
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
        string[] TextDict ;
        int EOS_IDX = 0;
        void LoadDictFile()
        {
            //TextDict = System.IO.File.ReadLines(fileName).ToArray();
            string BaseCharList = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\\\"#$%&'()*+,-./:;<=>?@[\\\\]^_`{|}~";
            TextDict = BaseCharList.ToList().Select(x=>x.ToString()).Append("[B]").Append("[P]").Prepend("[E]").ToArray();
        }
        public void CopyModel(PPOCRRecognitionModel ModelName, string targetDir)
        {
            if (Map.ContainsKey(ModelName))
            {
                var data = Map[ModelName];
                var PublicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var modelsDirectory = System.IO.Path.Combine(PublicDirectory, ".novision", "models");
                var ModelPath = System.IO.Path.Combine(modelsDirectory, data.ModelName);
                var DictionaryPath = System.IO.Path.Combine(modelsDirectory, data.DictionaryName);
                if (System.IO.File.Exists(ModelPath) & System.IO.File.Exists(DictionaryPath))
                {
                    System.IO.File.Copy(ModelPath, System.IO.Path.Combine(targetDir, "model.onnx"), true);
                    System.IO.File.Copy(DictionaryPath, System.IO.Path.Combine(targetDir, "dict_file.txt"), true);
                }
            }


        }
        private bool LoadOnnx(string directory)
        {
            try
            {
                
                var modelFile = System.IO.Path.Combine(directory, "model.onnx");
                if ( !System.IO.File.Exists(modelFile))
                {
                    CopyModel(PPOCRRecognitionModel.Parseg, directory);
                }
                LoadDictFile();
                int gpuDeviceId = 0; // The GPU device ID to execute on
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(modelFile, options);
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
                    input_width = 320;
                    input_height = 48;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * num_channel);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, num_channel, input_height, input_width })),
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
        /// <summary>
        /// shape = number of char
        /// </summary>
        /// <param name="singleTextArray"></param>
        string DecodeParseg(NDArray singleTextArray)
        {
            var eos_idx = singleTextArray.ToArray<int>().IndexOf(EOS_IDX);
            string result = "";
            if (eos_idx != -1)
            {
                var ids = singleTextArray[":"+eos_idx.ToString()].ToArray<int>();
                
                for(int i = 0; i < ids.Length; i++)
                {
                    result += TextDict[ids[i]];
                }
            }
            else
            {
                var ids = singleTextArray.ToArray<int>();

                for (int i = 0; i < ids.Length; i++)
                {
                    result += TextDict[ids[i]];
                }
            }
            return result;
        }
        public List<string> InferBatch(List<HImage> imgInput)
        {
            if (imgInput.Count == 0)
            {
                return new List<string>();
            }
            List<string> result = new List<string>();
            List<int> resizeWidths = new List<int>();
            List<int> dstWidths = new List<int>();
            if (input_width == -1)
            {
                for (int i = 0; i < imgInput.Count; i++)
                {
                    var img = imgInput[i];
                    if (img.CountChannels() != num_channel)
                    {
                        if (num_channel == 1)
                        {
                            imgInput[i] = img.Rgb1ToGray();
                        }
                        else
                        {
                            imgInput[i] = img.Compose3(img, img);
                        }
                    }
                    FindOptimalResize(img, out int dstWidth, out int resizeWidth);
                    resizeWidths.Add(resizeWidth);
                    dstWidths.Add(dstWidth);

                }
            }
            else
            {
                for (int i = 0; i < imgInput.Count; i++)
                {
                    var img = imgInput[i];
                    if (img.CountChannels() != num_channel)
                    {
                        if (num_channel == 1)
                        {
                            imgInput[i] = img.Rgb1ToGray();
                        }
                        else
                        {
                            imgInput[i] = img.Compose3(img, img);
                        }
                    }
                    dstWidths.Add(input_width);
                    resizeWidths.Add(input_width);
                }
            }

            var maxWidth = dstWidths.Max();
            //var enDict = TextDict.Select(x => x[0] <= 255).ToArray();
            //NDArray filtered = new NDArray(enDict);
            for (int i = 0; i < imgInput.Count; i++)
            {
                var img = imgInput[i];
                var imageNormalize = ResizeNormalize(img, resizeWidths[i], input_height, maxWidth, input_height);
                var inputs = new List<NamedOnnxValue>()
                {
                    NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(imageNormalize,new int[]{1,num_channel, input_height,maxWidth }))
                };
                var results = ONNXSession.Run(inputs).ToList();
                var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
                NDArray textArray = new NDArray(TensorOutput.ToArray(), new int[] { (int)TensorOutput.Length }).reshape(-1, TensorOutput.Dimensions[2]);
                var textIndex = np.argmax(textArray , 1);
                


                result.Add(DecodeParseg(textIndex));

            }


            return result;

        }
        

    }
    public class ONNXTextRecognition : ONNXModel
    {
        public void LoadDefaultModel()
        {
            
        }
        public ONNXTextRecognition(string dir)
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
            var r_pointer =r.GetImagePointer1(out HTuple type, out HTuple w, out HTuple h);
            var g_pointer = r.GetImagePointer1(out  type, out  w, out  h);
            var b_pointer = r.GetImagePointer1(out  type, out  w, out  h);

            Marshal.Copy(r_pointer, result, 0, nw * nh);
            Marshal.Copy(g_pointer, result, nw * nh, nw * nh);
            Marshal.Copy(b_pointer, result, 2 * nw * nh, nw * nh);

            return result;
        }
        public string ModelDir;
        int input_width = 224, input_height = 224;
        int num_channel = 3;
        string[] TextDict;
        string EOF = "<BOS/EOS>";
        int maxLen = 40;
        void LoadDictFile(string fileName)
        {
            TextDict = System.IO.File.ReadLines(fileName).ToArray();
        }
        public bool LoadOnnx(string directory)
        {
            try
            {
                var dictfile = System.IO.Path.Combine(directory, "dict_file.txt");

                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    if(!System.IO.File.Exists("Designer/Python/OCRV2/CRNN/model.onnx"))
                    {
                        State = ModelState.NotFound;
                        return false;
                    }
                    else
                    {
                        directory = "Designer/Python/OCRV2/CRNN";
                        dictfile = "Designer/Python/OCRV2/CRNN/dict_file.txt";
                    }  
                }
                LoadDictFile(dictfile);
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
                    input_width = 100;
                    input_height = 32;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * num_channel);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, num_channel, input_height, input_width })),
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
        public string Infer(HImage imgInput)
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
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,num_channel,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            NDArray textArray = new NDArray(TensorOutput.ToArray(), new int[] {26,37});
            var textArrayInt = np.argmax(textArray,1).ToArray<int>();
            string text = "";
            
            for(int i = 0; i < textArrayInt.Length; i++)
            {
                if (textArrayInt[i] == 0)
                {
                    continue;
                }
                if (TextDict[textArrayInt[i-1]] != EOF)
                {
                    text = text + TextDict[textArrayInt[i]-1];
                }
            }
            return text;

        }
        public HImage Normalize(HImage imageInput)
        {
            if (num_channel == 3)
            {
                HImage image = imageInput.ConvertImageType("float") / 255.0;
                HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
                image1 = (image1 - 0.485) / 0.229;
                image2 = (image2 - 0.456) / 0.224;
                image3 = (image3 - 0.406) / 0.225;
                return image1.Compose3(image2, image3);
            }
            else
            {
                HImage image = imageInput.ConvertImageType("float")/255f;
                //image = (image - 127) / 127;
                return image;
            }

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
    public class TextOrientationConfig
    {
        public int Epoch { get; set; } = 100;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TrainType { get; set; } = TrainResume.New;
        public string ModelDir { get; set; }
        public string PaddleOCRDir { get; set; } = "C:/src/PaddleOCR";
        public string PretrainDir { get; set; } = "C:/paddle_pretrain/en_PP-OCRv3_rec_train/best_accuracy";
        public string ResultDir { get; set; }
        public string ImageDir { get; set; }
        public string TrainDir { get; set; }
        public string AnnotationDir { get; set; }
        public string ModelName { get; set; } = "en-pp-ocrv3-rec";
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
        public static TextOrientationConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<TextOrientationConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new TextOrientationConfig(BaseDir);
            }

        }
        private TextOrientationConfig(string BaseDir)
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

        private TextOrientationConfig()
        {

        }
    }
    public class TextRecognitionConfig
    {
        public int Epoch { get; set; } = 100;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainResume TrainType { get; set; } = TrainResume.New;
        public string ModelDir { get; set; }
        public string PaddleOCRDir { get; set; } = "C:/src/PaddleOCR";
        public string PretrainDir { get; set; } = "C:/paddle_pretrain/en_PP-OCRv3_rec_train/best_accuracy";
        public string ResultDir { get; set; }
        public string ImageDir { get; set; }
        public string TrainDir { get; set; }
        public string AnnotationDir { get; set; }
        public string ModelName { get; set; } = "paddle-rec";
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 224;
        public int INPUT_HEIGHT { get; set; } = 224;
        public string Precision { get; set; } = "float32";
        public string ExtendDataDir { get; set; } 
        public void Save()
        {
            System.IO.File.WriteAllText(ConfigDir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public string ConfigDir { get; set; }
        public string BaseDir { get; set; }
        public static TextRecognitionConfig Create(string BaseDir)
        {
            var ConfigDir = System.IO.Path.Combine(BaseDir, "TrainConfig.txt");

            if (System.IO.File.Exists(ConfigDir))
            {
                var json = File.ReadAllText(ConfigDir);
                var config = JsonConvert.DeserializeObject<TextRecognitionConfig>(json);
                config.ApplyRelativeDir(BaseDir);
                config.Save();
                return config;
            }
            else
            {
                return new TextRecognitionConfig(BaseDir);
            }

        }
        private TextRecognitionConfig(string BaseDir)
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

        private TextRecognitionConfig()
        {

        }
    }
}

