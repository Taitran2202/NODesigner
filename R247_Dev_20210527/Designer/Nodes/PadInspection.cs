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
using System.Collections.ObjectModel;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.PropertiesViews;
using System.Runtime.Remoting.Contexts;
using MySqlX.XDevAPI.Common;
using MoreLinq;
using NOVisionDesigner.Designer.Python;
using NvAPIWrapper.GPU;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Threading;
using Org.BouncyCastle.Asn1.Cms;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "Pad Inspection", Icon: "Designer/icons/icons8-orange-100.png")]
    public class PadInspection : BaseNode
    {
        public PadInspectionRecorder Recoder { get; set; }
        static PadInspection()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<PadInspection>));
        }
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
        private static void CreateIfNotExist(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }
        /// <summary>
        /// MVTecAD format
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="Format"></param>
        private void ExportDatasetFolder(string directory, string Format = "png")
        {
            CreateIfNotExist(directory);
            //create train and test folder, ground truth
            string trainFolder = System.IO.Path.Combine(directory, "train", "good");
            string testGoodFolder = System.IO.Path.Combine(directory, "test", "good");
            string testBadFolder = System.IO.Path.Combine(directory, "test", "bad");
            string gtFolder = System.IO.Path.Combine(directory, "ground_truth", "bad");
            CreateIfNotExist(trainFolder);
            CreateIfNotExist(testGoodFolder);
            CreateIfNotExist(testBadFolder);
            CreateIfNotExist(gtFolder);
            //int totalFile = ListImage.Count;
            //int count = 0;
            //foreach (var ImageFile in ListImage.Where(x => x.Tag == "good"))
            //{
            //    var extension = System.IO.Path.GetExtension(ImageFile.FullPath);
            //    System.IO.File.Copy(ImageFile.FullPath, System.IO.Path.Combine(trainFolder, count.ToString() + extension));
            //    count++;
            //}
            //foreach (var ImageFile in ListImage.Where(x => x.Tag == "bad"))
            //{
            //    var extension = System.IO.Path.GetExtension(ImageFile.FullPath);
            //    System.IO.File.Copy(ImageFile.FullPath, System.IO.Path.Combine(testBadFolder, count.ToString() + extension));
            //    var annotationFile = GetAnnotationPath(ImageFile);
            //    if (System.IO.File.Exists(annotationFile))
            //    {
            //        var extension1 = System.IO.Path.GetExtension(annotationFile);
            //        System.IO.File.Copy(annotationFile, System.IO.Path.Combine(gtFolder, count.ToString() + "_mask" + extension));
            //    }
            //    count++;
            //}

        }
        #region Properties
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    PadAnomalyWindow wd = new PadAnomalyWindow(this);
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                    break;
                case "import":
                    ImportModelMethod();
                    break;
                case "export":
                    ExportModelMethod();
                    break;
            }

        }
        [SerializeIgnore]
        public Control PropertiesView
        {
            get
            {
                return new PadInspectionView(this);
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
                        System.IO.File.Copy(file, System.IO.Path.Combine(AnomalyConfig.ModelDir, "model.onnx"), true);
                        AnomalyRuntime.LoadRecipe();
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
                var modelPath = System.IO.Path.Combine(AnomalyConfig.ModelDir, "model.onnx");
                System.IO.File.Copy(modelPath, wd.FileName, true);

            }
        }
        [HMIProperty("Anomaly Editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        [HMIProperty("Anomaly Model Loader")]
        public IReactiveCommand AnomalyModelLoader
        {
            get { return ReactiveCommand.Create((Control sender) =>
                {
                    ONNXModelLoaderWindow wd = new ONNXModelLoaderWindow(AnomalyRuntime,true);
                    wd.Title = "Anomaly Model Loader";
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                }
            ); }
        }
        [HMIProperty("Instance Model Loader")]
        public IReactiveCommand InstanceModelLoader
        {
            get
            {
                return ReactiveCommand.Create((Control sender) =>
                {
                    ONNXModelLoaderWindow wd = new ONNXModelLoaderWindow(InstanceRuntime,false);
                    wd.Title = "Instance Model Loader";
                    wd.Owner = Window.GetWindow(sender);
                    wd.Show();
                }
            );
            }
        }
        //[HMIProperty("Export Model")]
        //public IReactiveCommand ExportModel
        //{
        //    get { return ReactiveCommand.Create((Control sender) => OnCommand("export", sender)); }
        //}
        //[HMIProperty("Import Model")]
        //public IReactiveCommand ImportModel
        //{
        //    get { return ReactiveCommand.Create((Control sender) => OnCommand("import", sender)); }
        //}
        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeOutputViewModel<HRegion> PadRegion { get; }
        public CollectionOfregion Regions { get; set; } = new CollectionOfregion();
        [HMIProperty]
        public int FillBackground { get; set; } = 0;
        [HMIProperty]
        public ValueNodeInputViewModel<ToolRecordMode> RecordMode { get; }
        public AnomalyV3Config AnomalyConfig { get; set; }
        public YOLONASSegmentationConfig InstanceConfig { get; set; }
        public string RootDir;

        public ObservableCollection<PadDefectTool> PadDefectTools { get; set; } = new ObservableCollection<PadDefectTool> ();
        public SemanticSegmentationOnnx InstanceRuntime { get; set; }
        public PadAnnomalyRuntime AnomalyRuntime { get; set; }
        #endregion
        public ObservableCollection<string> ClassNames { get; set; } = new ObservableCollection<string>();
        [HMIProperty("Retrain Anomaly")]
        public IReactiveCommand RetrainAnomalyCommand
        {
            get { return ReactiveCommand.Create((Control sender) => RetrainAnomaly(sender)); }
        }
        [HMIProperty("Instance Window")]
        public IReactiveCommand InstanceWindowCommand
        {
            get { return ReactiveCommand.Create((Control sender) => InstanceWindow(sender)); }
        }
        [HMIProperty("Recorder Window")]
        public IReactiveCommand RecorderWindowCommand
        {
            get { return ReactiveCommand.Create((Control sender) => RecorderWindow(sender)); }
        }
        void RecorderWindow(Control sender)
        {

            PadInspectionRecorderWindow wd = new PadInspectionRecorderWindow(this);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();

        }
        public void ReloadAnomaly()
        {
            var LoadRuntime = new PadAnnomalyRuntime(AnomalyConfig.ModelDir);
            LoadRuntime.LoadRecipe();
            try
            {
                HImage image = new HImage("byte", 700, 700);
                LoadRuntime.Infer(image,out double score);
                var oldRuntime = AnomalyRuntime;
                AnomalyRuntime = LoadRuntime;
                Task.Run(new Action(() =>
                {
                    Thread.Sleep(2000);
                    oldRuntime.Dispose();
                }));
                
            }
            catch(Exception ex)
            {

            }
            
        }
        void RetrainAnomaly(Control sender)
        {

            LoadingWindow wd = new LoadingWindow(this);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();
            
        }
        void InstanceWindow(Control sender)
        {

            PadInstanceWindow wd = new PadInstanceWindow(this);
            wd.Owner = Window.GetWindow(sender);
            wd.Show();

        }
        [HMIProperty("Pad border offset")]
        public double PadBorderOffset { get; set; } = 0;
        [HMIProperty("Minimum Pad Region")]
        public double MinPadRegion { get; set; } = 400;
        [HMIProperty("Pad detect threshold")]
        public double InstanceConfidence { get; set; } = 0.5;
        [HMIProperty("Region fit angle")]
        public int FitAngle { get; set; } = 4;
        [HMIProperty("Region fit max error")]
        public double FitError { get; set; } = 1;
        [HMIProperty("Region fit max distance")]
        public double FitDistance { get; set; } = 5;
        [HMIProperty("Set context result")]
        public bool SetInspectionContext { get; set; }
        [HMIProperty("Display result table")]
        public bool DisplayResultTable { get; set; }
        [HMIProperty("Enable pad crop")]
        public bool EnablePadCrop { get; set; }
        public HImage GetImageChannel(HImage image)
        {
            return image;
        }
        public InstanceSegmentResult[] FindInstances(HImage input, double threshold)
        {
            if (input != null)
            {

                if (InstanceRuntime.State == ModelState.Unloaded)
                {
                    InstanceRuntime.LoadRecipe();
                }
                if (InstanceRuntime.State == ModelState.Error)
                {
                    return new InstanceSegmentResult[0];
                }
                if (InstanceRuntime.State == ModelState.Loaded)
                {
                    return InstanceRuntime.Predict(input,0, (float)threshold);
                }
            }
            return new InstanceSegmentResult[0];
        }
        public PadInspection(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            RootDir = System.IO.Path.Combine(dir, id);
            if (!System.IO.Directory.Exists(RootDir))
            {
                System.IO.Directory.CreateDirectory(RootDir);
            }
            this.CanBeRemovedByUser = true;
            this.Name = "Pad Inspection";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);
            PadRegion = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Pad Region",
                PortType = "Region",
                Editor = new HObjectOutputValueEditorViewModel<HRegion>()

            };
            this.Outputs.Add(PadRegion);
            RecordMode = new ValueNodeInputViewModel<ToolRecordMode>()
            {
                Name = "Record Mode",
                PortType = "ToolRecordMode",
                Editor = new EnumValueEditorViewModel<ToolRecordMode>()
            };
            this.Inputs.Add(RecordMode);
            AnomalyConfigDir = System.IO.Path.Combine(RootDir, "Anomaly");
            InstanceConfigDir = System.IO.Path.Combine(RootDir, "Instance");
            System.IO.Directory.CreateDirectory(AnomalyConfigDir);
            System.IO.Directory.CreateDirectory(InstanceConfigDir);
            AnomalyConfig = AnomalyV3Config.Create(AnomalyConfigDir);
            InstanceConfig = YOLONASSegmentationConfig.Create(InstanceConfigDir);
        }
        public string InstanceConfigDir { get; set; }
        public string AnomalyConfigDir { get; set; }
        public override void OnInitialize()
        {
            try
            {
                Recoder = new PadInspectionRecorder();
            }catch(Exception ex)
            {

            }
            
            InstanceRuntime = new SemanticSegmentationOnnx(InstanceConfig.ModelDir);
            AnomalyRuntime = new PadAnnomalyRuntime(AnomalyConfig.ModelDir);
            HImage instanceImage = new HImage("byte", 640, 640);
            InstanceRuntime.ContinuousLoadRecipe();
            InstanceRuntime.Predict(instanceImage, 0, 0.5f);

            if (Regions.Region.Area == 0)
            {
                HImage image = new HImage("byte", 700, 700);
                Segment(image);
            }
            else
            {
                Regions.Region.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var image_croped = new HImage("byte",c2-c1,r2-r1);
                image_croped.GetImageSize(out int w, out int h);
                HRegion diffrg = new HRegion(0, 0.0, h, w);
                var subRg = diffrg.Difference(Regions.Region.MoveRegion(-r1, -c1));
                image_croped.OverpaintRegion(subRg, new HTuple(0, 0, 0), "fill");
                Segment(image_croped);
            }
        }
        
        
        public override void Dispose()
        {
            base.Dispose();
            AnomalyRuntime.Dispose();
            InstanceRuntime.Dispose();
        }
        public NurbData ExtractPad(HImage imageCropped)
        {
            var instances = FindInstances(imageCropped, 0.5);
            var pad = instances.FirstOrDefault(x => x.ClassID == 0);
            if (pad != null)
            {
                pad.Region = pad.Region.Connection().SelectShapeStd("max_area", 70.0);
                pad.Region = RegionToSmoothPolygon(pad.Region);
                pad.Region.GetRegionPolygon(1.0, out HTuple rows, out HTuple cols);
                double[] weights = new double[rows.Length-1];
                for(int i=0;i<rows.Length-1; i++)
                {
                    weights[i] = 15.0;
                }
                return new NurbData() { rows= rows,cols= cols,weights=weights };
            }
            else
            {
                return new NurbData();
            }
        }
        public void Record()
        {
            if (Image.Value != null)
            {
                var image = Image.Value.Clone();
                var filename = Functions.RandomFileName(AnomalyConfig.NormalDir);
                var region = Functions.GetNoneEmptyRegion(Regions.Region);
                var imageCropped = Functions.CropImageWithRegion(image, region, FillBackground);
                var instances = FindInstances(imageCropped, 0.5);
                var pad = instances.FirstOrDefault(x => x.ClassID == 0);                
                if (pad != null)
                {
                    pad.Region = pad.Region.Connection().SelectShapeStd("max_area", 70.0).ErosionCircle(PadBorderOffset);
                    pad.Region = RegionToSmoothPolygon(pad.Region);
                    var padImage = Extensions.Functions.CropImageWithRegionTranslate(imageCropped, pad.Region, fillRegion: EnablePadCrop);
                    padImage.WriteImage("png", 0, filename + ".png");
                }
                
                
            }
            
        }
        public HImage GetPadImage(HImage image)
        {
            HRegion inspected_region = Regions.Region;
            HImage  padImage = null, InspectImage;
            int r1 = 0, c1 = 0;


            if (inspected_region.Area.Length == 0)
            {
                InspectImage = image;
            }
            else if (inspected_region.Area == 0)
            {
                InspectImage = image;

            }
            else
            {
                inspected_region.Union1().SmallestRectangle1(out r1, out c1, out int r2, out int c2);
                var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                InspectImage = image_croped;

            }

            var result = FindInstances(InspectImage, 0.5);
            HHomMat2D translate = new HHomMat2D();
            translate = translate.HomMat2dTranslate((double)r1, c1);

            for (int i = 0; i < result.Length; i++)
            {
                result[i].Region = result[i].Region.AffineTransRegion(translate, "constant");
                result[i].BoundingBox.row1 += r1;
                result[i].BoundingBox.row2 += r1;
                result[i].BoundingBox.col1 += c1;
                result[i].BoundingBox.col2 += c1;
            }


            if (result.Length != 0)
            {
                var padInstances = result.Where(x => x.ClassID == 0);
                if (padInstances.Count() > 0)
                {
                    var maxPad = padInstances.MaxBy(x => x.Region.Area);
                    if (maxPad.Count() > 0)
                    {
                        var padInstance = maxPad.First();
                        if (PadBorderOffset > 0)
                        {
                            padInstance.Region = padInstance.Region.Connection().SelectShapeStd("max_area", 70.0).ErosionCircle(PadBorderOffset);
                        }

                        padInstance.Region = RegionToSmoothPolygon(padInstance.Region);
                        padImage = Extensions.Functions.CropImageWithRegionTranslate(image, padInstance.Region, fillValue: FillBackground, reduceDomain: true, fillRegion: EnablePadCrop);
                    }
                }
            }
            return padImage;
        }
        public (HImage map,HImage pad) GetAnomalyMap(HImage image)
        {
            HRegion inspected_region = Regions.Region;
            HImage anomalyMap = null, padImage=null, InspectImage;
            int r1 = 0, c1 = 0;


            if (inspected_region.Area.Length == 0)
            {
                InspectImage = image;
            }
            else if (inspected_region.Area == 0)
            {
                InspectImage = image;

            }
            else
            {
                inspected_region.Union1().SmallestRectangle1(out r1, out c1, out int r2, out int c2);
                var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                InspectImage = image_croped;

            }

            var result = FindInstances(InspectImage, 0.5);
            HHomMat2D translate = new HHomMat2D();
            translate = translate.HomMat2dTranslate((double)r1, c1);

            for (int i = 0; i < result.Length; i++)
            {
                result[i].Region = result[i].Region.AffineTransRegion(translate, "constant");
                result[i].BoundingBox.row1 += r1;
                result[i].BoundingBox.row2 += r1;
                result[i].BoundingBox.col1 += c1;
                result[i].BoundingBox.col2 += c1;
            }


            if (result.Length != 0)
            {
                var padInstances = result.Where(x => x.ClassID == 0);
                if (padInstances.Count() > 0)
                {
                    var maxPad = padInstances.MaxBy(x => x.Region.Area);
                    if (maxPad.Count() > 0)
                    {
                        var padInstance = maxPad.First();
                        if (PadBorderOffset > 0)
                        {
                            padInstance.Region = padInstance.Region.Connection().SelectShapeStd("max_area", 70.0).ErosionCircle(PadBorderOffset);
                        }
                        
                        padInstance.Region = RegionToSmoothPolygon(padInstance.Region);
                        padImage = Extensions.Functions.CropImageWithRegionTranslate(image, padInstance.Region, fillValue: FillBackground, reduceDomain: true,fillRegion:EnablePadCrop);
                        anomalyMap = Segment(padImage).ReduceDomain(padImage.GetDomain());
                    }
                }
            }
            return (anomalyMap, padImage);
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
            HRegion inspected_region = Regions.Region;
            
            HImage image = Image.Value;
            HImage anomalyMap=null,padImage=null,InspectImage;
            HRegion ExtractedPadRegion = null;
            var IContext = context as InspectionContext;
            int r1=0, c1=0;


            if (inspected_region.Area.Length == 0)
            {
                InspectImage = image;
            }
            else if (inspected_region.Area == 0)
            {
                InspectImage = image;

            }
            else
            {
                inspected_region.Union1().SmallestRectangle1(out  r1, out  c1, out int r2, out int c2);
                var image_croped = Extensions.Functions.CropImageWithRegionTranslate(image, inspected_region.Union1());
                InspectImage = image_croped; 
                     
            }

            var result = FindInstances(InspectImage, 0.5);
            HHomMat2D translate = new HHomMat2D();
            translate = translate.HomMat2dTranslate((double)r1, c1);
            HHomMat2D translatePad = new HHomMat2D();
            
            for (int i = 0; i < result.Length; i++)
            {
                result[i].Region = result[i].Region.AffineTransRegion(translate, "constant");
                result[i].BoundingBox.row1 += r1;
                result[i].BoundingBox.row2 += r1;
                result[i].BoundingBox.col1 += c1;
                result[i].BoundingBox.col2 += c1;
            }
            
            
            if (result.Length != 0)
            {
                var padInstances = result.Where(x => x.ClassID == 0);
                if (padInstances.Count() > 0)
                {
                    var maxPad = padInstances.MaxBy(x => x.Region.Area);
                    if (maxPad.Count() > 0)
                    {

                        var padInstance = maxPad.First();
                        padInstance.Region = padInstance.Region.Connection().SelectShapeStd("max_area", 70.0).ErosionCircle(PadBorderOffset);
                        //IContext.inspection_result.AddRegion(padInstance.Region.Clone(), "blue");
                        if (padInstance.Region.Area < MinPadRegion)
                        {
                            if (DisplayResultTable) { 

                              IContext.SetResult(this.Name + "-Missing Pad", false);
       
                            }
                            if (SetInspectionContext)
                            {
                                IContext.result &= false;
                            }
                            return;
                        }
                        try
                        {
                            padInstance.Region = RegionToSmoothPolygon(padInstance.Region);
                        }
                        catch(Exception ex)
                        {
                            if (DisplayResultTable)
                            {

                                IContext.SetResult(this.Name + "-Cannot Extract Pad", false);

                            }
                            if (SetInspectionContext)
                            {
                                IContext.result &= false;
                            }
                            return;
                        }
                        ExtractedPadRegion = padInstance.Region;
                        padInstance.Region.SmallestRectangle1(out int padr, out int padc, out _, out _);
                        padImage = Extensions.Functions.CropImageWithRegionTranslate(image, padInstance.Region,fillValue:FillBackground, reduceDomain: true, fillRegion: EnablePadCrop);
                        anomalyMap = Segment(padImage);
                        anomalyMap = anomalyMap.ReduceDomain(padImage.GetDomain());
                        IContext.inspection_result.AddRegion(padInstance.Region, "blue");
                        translatePad = translatePad.HomMat2dTranslate((double)padr, (double)padc);
                    }
                }
                
            }
            bool finalResult = true;
            HRegion DefectRegion=null;
            HHomMat2D invertTrans = translatePad.HomMat2dInvert();
            if (anomalyMap !=null)
            {
                foreach(var item in PadDefectTools)
                {
                    if (item.IsEnabled)
                    {
                        var region = item.Run(anomalyMap, translatePad, IContext);
                        bool resultBool = region.CountObj() > 0;
                        if (resultBool)
                        {
                            if(DefectRegion == null)
                            {
                                DefectRegion = region.AffineTransRegion(invertTrans, "constant");
                            }
                            else
                            {
                                DefectRegion = DefectRegion.ConcatObj(region.AffineTransRegion(invertTrans, "constant"));
                            }
                        }
                        finalResult = finalResult & !resultBool;
                        if (DisplayResultTable)
                        {
                            if (resultBool)
                            {
                                IContext.SetResult(this.Name + "-" + item.ToolName, false);
                            }
                            else
                            {
                                IContext.SetResult(this.Name + "-" + item.ToolName, true);
                            }
                        }
                        if (SetInspectionContext)
                        {
                            IContext.result &= !resultBool;
                        }
                    }
                    

                }
                if (!finalResult)
                {
                    if(padImage != null)
                    {
                        Recoder.Add(new PadInspectionResult((int)IContext.total)
                        {
                            Image = padImage.Clone(),
                            IsInputImage = false,
                            IsNormal = finalResult,
                            DefectRegion = DefectRegion
                        }); ;
                    }
                    
                }
                
            }
            else
            {
                Recoder.Add(new PadInspectionResult((int)IContext.total)
                {
                    Image = image.Clone(),
                    IsInputImage = true,
                    IsNormal = finalResult,
                    DefectRegion = DefectRegion
                }); ; 
            }
            if(ExtractedPadRegion != null)
            {
                PadRegion.OnNext(ExtractedPadRegion);
            }
        }

        HRegion RegionToSmoothPolygon(HRegion region)
        {
            //return region;
            region.GetRegionPolygon(1.0, out HTuple rows, out HTuple cols);
            HXLDCont cont = new HXLDCont();
            cont.GenContourNurbsXld(rows, cols, "auto", "auto", FitAngle, FitError, FitDistance);
            return cont.GenRegionContourXld("filled");
        }
        HRegion RegionToPolygon(HRegion region)
        {
            region.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
            HRegion hRegion = new HRegion();
            hRegion.GenRegionPolygonFilled(rows, cols);
            return hRegion;
        }
        public void ClearSession()
        {
            //clear tensorflow session everytime retrain

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public HImage Segment(HImage input)
        {
            if (input != null)
            {
                
                if (AnomalyRuntime.State == ModelState.Unloaded)
                {
                    AnomalyRuntime.LoadRecipe();
                }
                if (AnomalyRuntime.State == ModelState.Error)
                {

                }
                if (AnomalyRuntime.State == ModelState.Loaded)
                {
                    return AnomalyRuntime.Infer(input,out double score);
                }
                input.GetImageSize(out int w, out int h);
                return new HImage("real", w, h);
            }
            else
            {
                return null;
            }
            
        }
    }
    public class PadAnnomalyRuntime : ONNXModel, IHalconDeserializable
    {
        public HImage ResizeWithPadding(HImage image,int new_width,int new_height,int originalW,int orignalH)
        {
            float scale = Math.Min((float)new_width / originalW, (float)new_height / orignalH);
            int nw = (int)(scale * originalW);
            int nh = (int)(scale * orignalH);
            HImage resizeImg;
            if (image.CountChannels() == 1)
            {
                resizeImg = image.Compose3(image, image).ZoomImageSize(nw, nh, "bilinear");
            }
            else
            {
                resizeImg = image.ZoomImageSize(nw, nh, "bilinear");
            }
            return resizeImg.ChangeFormat(new_width, new_height);
        }
        public PadAnnomalyRuntime(string dir)
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
        public void Dispose()
        {
            ONNXSession?.Dispose();
        }
        bool is_loading = false;
        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                //if (System.IO.File.Exists(System.IO.Path.Combine(directory, "result", "prediction.txt")))
                //{
                //    JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(directory, "result", "prediction.txt")));
                //    Min = data["min"].Value<float>();
                //    Max = data["max"].Value<float>();
                //    Threshold = data["threshold"].Value<float>();
                //}
                SessionOptions options = CreateProviderOption(directory);
                //if (Provider == ONNXProvider.TensorRT)
                //{

                //    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                //    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                //      {
                //          // Enable INT8 for QAT models, disable otherwise.
                //          { "trt_fp16_enable", "1" },
                //          { "trt_engine_cache_enable", "1" },
                //          {"trt_engine_cache_path",directory}
                //     });
                //    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                //}
                //else if(Provider == ONNXProvider.CUDA)
                //{
                //    int gpuDeviceId = 0; // The GPU device ID to execute on
                //    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //}

                //else if(Provider == ONNXProvider.CPU)
                //{
                //    options = SessionOptions.(gpuDeviceId);
                //}
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
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

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (input_width == -1 | input_height == -1)
                {
                    input_width = 224;
                    input_height = 224;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * 224 * 224 * 3);
                    //var t2 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 );
                    //t2.SetValue(0, 1.05f);
                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, 224, 224 })),
                        //NamedOnnxValue.CreateFromTensor<float>("inpute_gain",  t2),
                    };

                    using (var results = ONNXSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * input_width * input_height * 3);
                    var t2 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1);
                    t2.SetValue(0, 1.05f);
                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3,input_height, input_width })),
                        //NamedOnnxValue.CreateFromTensor<float>("inpute_gain",  t2),
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
        public override bool ContinuousLoadRecipe()
        {
            if (is_loading)
            {
                return false;
            }
            is_loading = true;
            bool result = false;
            try
            {
                if (ONNXSession != null)
                {
                    ONNXSession.Dispose();
                }
                if (!System.IO.File.Exists(System.IO.Path.Combine(ModelDir, "model.onnx")))
                {
                    if (State != ModelState.Loaded)
                    {
                        State = ModelState.NotFound;
                    }
                    return false;
                }
                //if (System.IO.File.Exists(System.IO.Path.Combine(directory, "result", "prediction.txt")))
                //{
                //    JObject data = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(directory, "result", "prediction.txt")));
                //    Min = data["min"].Value<float>();
                //    Max = data["max"].Value<float>();
                //    Threshold = data["threshold"].Value<float>();
                //}
                SessionOptions options = CreateProviderOption(ModelDir);
                //if (Provider == ONNXProvider.TensorRT)
                //{

                //    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                //    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                //      {
                //          // Enable INT8 for QAT models, disable otherwise.
                //          { "trt_fp16_enable", "1" },
                //          { "trt_engine_cache_enable", "1" },
                //          {"trt_engine_cache_path",directory}
                //     });
                //    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                //}
                //else if(Provider == ONNXProvider.CUDA)
                //{
                //    int gpuDeviceId = 0; // The GPU device ID to execute on
                //    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //}

                //else if(Provider == ONNXProvider.CPU)
                //{
                //    options = SessionOptions.(gpuDeviceId);
                //}
                //int gpuDeviceId = 0; // The GPU device ID to execute on
                //var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                InferenceSession newOnnxSession;
                if (options != null)
                {
                    newOnnxSession = new InferenceSession(System.IO.Path.Combine(ModelDir, "model.onnx"), options);
                }
                else
                {
                    newOnnxSession = new InferenceSession(System.IO.Path.Combine(ModelDir, "model.onnx"));
                }

                string new_input_name = newOnnxSession.InputMetadata.Keys.FirstOrDefault();
                int new_input_width=224, new_input_height=224;
                if (new_input_name != null)
                {
                    new_input_width = newOnnxSession.InputMetadata[new_input_name].Dimensions[3];
                    new_input_height = newOnnxSession.InputMetadata[new_input_name].Dimensions[2];

                }


                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (new_input_width == -1 | new_input_height == -1)
                {
                    new_input_width = 224;
                    new_input_height = 224;
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * 224 * 224 * 3);
                    var t2 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1);
                    t2.SetValue(0, 1.05f);
                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, 224, 224 })),
                        //NamedOnnxValue.CreateFromTensor<float>("inpute_gain",  t2),
                    };

                    using (var results = newOnnxSession.Run(inputs))
                    {

                    }
                }
                else
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * new_input_width * new_input_height * 3);
                    var t2 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1);
                    t2.SetValue(0, 1.05f);
                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(new_input_name, t1.Reshape(new int[]{ 1, 3,new_input_height, new_input_width })),
                        //NamedOnnxValue.CreateFromTensor<float>("inpute_gain",  t2),
                    };

                    using (var results = newOnnxSession.Run(inputs))
                    {


                    }
                }
                this.input_height = new_input_height;
                this.input_width = new_input_width;
                this.input_name = new_input_name;
                ONNXSession = newOnnxSession;
                State = ModelState.Loaded;
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            is_loading = false;
            return result;

        }
        public override bool LoadRecipe()
        {
            if (is_loading)
            {
                return false;
            }
            is_loading = true;
            bool result = false;
            try
            {
                result = LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                result = false;
            }
            is_loading = false;
            return result;
        }
        public HImage Infer(HImage imgInput, out double score, bool normalize = true)
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
            //var image_resize = ResizeWithPadding(imgInput, input_width, input_height, originalW, originalH);
            HImage imageNormalize;
            //if (normalize)
            //{
            //     imageNormalize = Normalize(image_resize);
            //}
            //else
            //{
            //    imageNormalize = image_resize.ConvertImageType("float");
            //}

            //image_resize.MinMaxGray(image_resize, 0, out double MinGray, out double MaxGray, out double RangeGray);
            imageNormalize = image_resize.ConvertImageType("real") / 255.0;
            var array_final = Processing.HalconUtils.HImageToFloatArray(imageNormalize, num_channel, out int _, out int _);
            var t2 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1);
            t2.SetValue(0, 1.05f);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width })),
                //NamedOnnxValue.CreateFromTensor<float>("inpute_gain",  t2)
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            //double predict_score = 0;

            int outputw, outputh;
            //if (TensorOutput.Dimensions.Length == 4)
            //{
            //    outputw = TensorOutput.Dimensions[3];
            //    outputh = TensorOutput.Dimensions[2];
            //}
            //else
            //{
            if (TensorOutput.Dimensions.Length != 2)
            {
                outputw = TensorOutput.Dimensions[TensorOutput.Dimensions.Length - 1];
                outputh = TensorOutput.Dimensions[TensorOutput.Dimensions.Length - 2];
            }
            else
            {
                outputw = TensorOutput.Dimensions[1];
                outputh = TensorOutput.Dimensions[0];
            }

            //}
            var result = new HImage("real", outputw, outputh);
            IntPtr pointerResult = result.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(TensorOutput.ToArray(), width * height, pointerResult, width * height);
            //crop if image was padded
            //result = result.ScaleImageMax();
            //var result1 = result * 255;
            //result1 = result1.ConvertImageType("byte");
            //result1.WriteImage("tiff", 0, "D:/predict.tiff");
            //result.max
            //result = (result - Threshold) / (Max - Min) + 0.5;
            //result.get
            //if (results.Count == 2)
            //{
            //    score = (results[1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).FirstOrDefault();
            //}
            //else
            //{
            //    result.MinMaxGray(result, 0, out double min, out score, out double range);
            //}
            //return a list 

            //predict_score = ((predict_score - Threshold) / (Max - Min)) + 0.5;
            result.MinMaxGray(result, 0, out double min, out score, out double range);
            return result.ZoomImageSize(originalW, originalH, "constant");

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
        public HImage NormalizeFloat(HImage imageInput)
        {
            HImage image = imageInput / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
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

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public enum InspectionSideEnum
    {
        Inside,Outside
    }
    public class PadDefectTool : IHalconDeserializable
    {
        public string ToolName { get; set; }
        public CollectionOfregion Regions { get; set; } = new CollectionOfregion();
        public bool IsEnabled { get; set; } = true;
        public bool ShowDisplay { get; set; } = true;
        [HMIProperty]
        public double LowerValue { get; set; } = 0;
        [HMIProperty]
        public double UpperValue { get; set; } = 999999;
        [HMIProperty]
        public int MinArea { get; set; } = 100;
        [HMIProperty]
        public int MaxArea { get; set; } = 9999999;
        [HMIProperty]
        public bool IsFill { get; set; } = false;
        [HMIProperty]
        public bool Invert { get; set; } = false;
        [HMIProperty]
        public bool ShowRegion { get; set; } = true;
        [HMIProperty]
        public bool RemoveOuterRegion { get; set; } = true;
        [HMIProperty]
        public double Closing { get; set; } = 1;
        [HMIProperty]
        public bool DisplayArea { get; set; } = false;
        [HMIProperty]
        public bool DisplayBlobName { get; set; } = false;
        [HMIProperty]
        public int MaxBlobCount { get; set; } = 20;
        [HMIProperty]
        public double ReduceBorder { get; set; } = 2.5;
        public string DisplayColor { get; set; } = "#00ff00ff";
        public string TextForeground { get; set; } = "#000000ff";
        public InspectionSideEnum InspectionSide { get; set; } = InspectionSideEnum.Inside;
        public string TextBackground { get; set; } = "#ffffffff";
        [HMIProperty]
        public TextPosition DisplayPosition { get; set; } = TextPosition.Bottom;
        [HMIProperty]
        public ImageChannel Channel { get; set; }
        public HImage GetImageChannel(HImage image_original)
        {
            if (image_original == null)
            {
                return null;
            }
            HImage image;
            if (Channel == 0)
            {
                image = image_original.Rgb1ToGray();
            }
            else
            {
                HTuple channels = image_original.CountChannels();
                if (channels > (int)Channel)
                    image = image_original.AccessChannel((int)Channel);
                else
                {
                    image = image_original.AccessChannel(channels);
                }
            }
            return image;
        }
        public static (int row, int col) GetDisplayPosition(TextPosition DisplayPosition, int r1, int c1, int r2, int c2)
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
        public HRegion Run(HImage image,HHomMat2D transform, InspectionContext context)
        {
            if (image == null)
            {
                HRegion region = new HRegion();
                region.GenEmptyObj();
                return region;
            }
            HRegion RegionInspect = Regions.Region;
            HRegion regionInspectTransform = RegionInspect;
            var result = RunInside(image, regionInspectTransform,out HRegion ProcessedRegion).AffineTransRegion(transform,"constant");
            
            if (ShowDisplay)
            {
                var displayContext = context;
                if (ShowRegion)
                {
                    displayContext.inspection_result.AddRegion(regionInspectTransform.AffineTransRegion(transform, "constant"), DisplayColor);
                    displayContext.inspection_result.AddRegion(ProcessedRegion.AffineTransRegion(transform, "constant"), DisplayColor);
                }
                displayContext.inspection_result.AddRegion(result, "red");
                var Displaytext = DisplayBlobName ? ToolName : "";
                if (DisplayArea) //only apply if Display invidiual are true
                {
                    for (int i = 1; i < result.CountObj() + 1; i++)
                    {
                        if (i > MaxBlobCount)
                        {
                            break;
                        }
                        result[i].SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                        var displayCor = GetDisplayPosition(DisplayPosition, r1, c1, r2, c2);
                        if (DisplayBlobName)
                        {
                            displayContext.inspection_result.AddText(Displaytext + ":" + context.PixelValueToCalibrationValue(result[i].Area).ToString(), TextForeground, TextBackground, displayCor.row, displayCor.col);
                        }
                        else
                        {
                            displayContext.inspection_result.AddText(context.PixelValueToCalibrationValue(result[i].Area).ToString(), TextForeground, TextBackground, displayCor.row, displayCor.col);
                        }

                    }

                }
                else
                {
                    for (int i = 1; i < result.CountObj() + 1; i++)
                    {
                        if (i > MaxBlobCount)
                        {
                            break;
                        }
                        result[i].SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                        var displayCor = GetDisplayPosition(DisplayPosition, r1, c1, r2, c2);
                        displayContext.inspection_result.AddText(Displaytext, TextBackground, TextBackground, displayCor.row, displayCor.col);
                    }
                }
            }
            return result;
        }
        public HRegion RunInside(HImage image, HRegion regionInspectTransform,out HRegion ReduceBorderDomain)
        {
            
            if (image == null) { ReduceBorderDomain = null; return null; }
            HImage imageChannel;
            var OriginalDomain = image.GetDomain();
            //regionInspectTransform = regionInspectTransform.Intersection(OriginalDomain);
            HRegion result = new HRegion();
            result.GenEmptyRegion();
            ReduceBorderDomain = OriginalDomain;
            if (ReduceBorder > 0.5)
            {
                if (InspectionSide == InspectionSideEnum.Outside)
                {
                    ReduceBorderDomain = OriginalDomain.Difference(OriginalDomain.ErosionCircle(ReduceBorder));
                }
                else
                {
                    ReduceBorderDomain = OriginalDomain.ErosionCircle(ReduceBorder);
                }

                imageChannel = image.ReduceDomain(ReduceBorderDomain);
            }
            else
            {
                imageChannel = image;
            }
            if (InspectionSide == InspectionSideEnum.Outside)
            {

            }
            
            if (Closing < 0.5)
            {
                result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue);
            }
            else
            {
                result = imageChannel.ReduceDomain(regionInspectTransform).Threshold(LowerValue, UpperValue).ClosingCircle(Closing);
            }
            if (IsFill)
            {
                result = result.FillUp();
            }
            if (Invert)
            {
                result = regionInspectTransform.Difference(result.Connection().SelectShape("area", "and", MinArea, MaxArea).Union1());
            }

            result = result.Connection().SelectShape("area", "and", MinArea, MaxArea);

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
    
    public class PadInspectionRecorder:INotifyPropertyChanged,IHalconDeserializable
    {
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _is_recording;

        public bool IsRecording
        {
            get
            {
                return _is_recording;
            }
            set
            {
                if (_is_recording != value)
                {
                    _is_recording = value;
                    RaisePropertyChanged("IsRecording");
                }
            }
        }
        public int IDCounter;
        public bool IsView = false;
        public int max_record = 20;
        public EventHandler OnAddQueue;
        public ObservableQueue<PadInspectionResult> ResultRecoderQueue;
        private PadInspectionResult LastSelected = null;
        private PadInspectionResult WaitToDispose = null;
        public bool IsSaving = false;
        object _lock= new object();
        public PadInspectionRecorder()
        {
            ResultRecoderQueue = new ObservableQueue<PadInspectionResult>(20);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                BindingOperations.EnableCollectionSynchronization(ResultRecoderQueue, _lock);
            }));
            //BindingOperations.EnableCollectionSynchronization(ResultRecoderQueue, _lock);
            max_record = 20;
        }
        public void Add(PadInspectionResult result)
        {
            if (!IsRecording)
            {
                return;
            }
            if (IsSaving)
                return;
            if (!IsView)
            {

                //ResultRecoderQueue.Dequeue()?.Dispose();
                ResultRecoderQueue.Enqueue(result)?.Dispose();
            }
            else
            {
                PadInspectionResult temp = ResultRecoderQueue.Enqueue(result);
                if (temp != LastSelected)
                {
                    temp.Dispose();
                }
                else
                {
                    WaitToDispose = LastSelected;
                }
                //ResultRecoderQueue.Enqueue(result);
            }



        }
        public PadInspectionResult GetResult(int index)
        {
            if (WaitToDispose != null)
            {
                if (WaitToDispose == LastSelected)
                {
                    WaitToDispose.Dispose();
                    WaitToDispose = null;

                }
            }
            PadInspectionResult value = ResultRecoderQueue.ElementAt(index);
            LastSelected = value;
            return value;
        }

    }
    public class PadInspectionResult:INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _is_added=false;

        public bool IsAdded
        {
            get
            {
                return _is_added;
            }
            set
            {
                if (_is_added != value)
                {
                    _is_added = value;
                    RaisePropertyChanged("IsAdded");
                }
            }
        }
        private bool _is_normal;

        public bool IsNormal
        {
            get
            {
                return _is_normal;
            }
            set
            {
                if (_is_normal != value)
                {
                    _is_normal = value;
                    RaisePropertyChanged("IsNormal");
                }
            }
        }     
        public bool IsInputImage { get; set; } = false;
        public int ID { get; set; }
        public HImage Image { get; set; }
        public HRegion DefectRegion { get; set; }
        public void Dispose()
        {
            Image?.Dispose();
            DefectRegion?.Dispose();
        }
        public PadInspectionResult(int ID)
        {
            this.ID = ID;
        }
    }

}
