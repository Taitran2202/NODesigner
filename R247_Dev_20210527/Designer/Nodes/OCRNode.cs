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
using NOVisionDesigner.Designer.Python;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NOVisionDesigner.Designer.Processing;
using Newtonsoft.Json.Converters;
using Microsoft.ML.OnnxRuntime;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning","OCR",visible:true)]
    public class OCRNode : BaseNode
    {
        
        static OCRNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<OCRNode>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        public override void OnLoadComplete()
        {
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public ValueNodeOutputViewModel<List<CharacterResult>> TextOutput { get; set; }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    OCRWindow wd = new OCRWindow(this);
                    wd.ShowDialog();
                    break;
            }
        }
        [HMIProperty("OCR Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        public ONNXOCRCRAFTDetector detector;
        public ONNXOCRClassifierV1 classifier;
        public string ImageDir, AnnotationDir, ModelDir, RootDir, DetectionTrainConfigDir,ClassificationTrainConfigDir;
        public OCRDetectionTrainConfig DetectionTrainConfig;
        public OCRClassifierTrainConfig ClassificationTrainConfig;
        public bool DisplayBox { get; set; } = true;
        public bool DisplayLabel { get; set; } = true;
        #region Properties
        bool _rotatedText =false;
        public bool RotatedText
        {
            get
            {
                return _rotatedText ;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _rotatedText, value);
            }
        }
        bool _slantedText=false;
        public bool SlantedText
        {
            get
            {
                return _slantedText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _slantedText, value);
            }
        }

        int _min_width=20;
        public int MinWidth
        {
            get
            {
                return _min_width;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _min_width, value);
            }
        }

        int _text_threshold = 10;
        public int TextThreshold
        {
            get
            {
                return _text_threshold;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _text_threshold, value);
            }
        }
        int _link_threshold = 10;
        public int LinkThreshold
        {
            get
            {
                return _link_threshold;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _link_threshold, value);
            }
        }
        int _confidence = 10;
        public int Confidence
        {
            get
            {
                return _confidence;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _confidence, value);
            }
        }
        int _min_size = 20;
        public int MinSize
        {
            get
            {
                return _min_size;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _min_size, value);
            }
        }
        int _text_height = 29;
        public int TextHeight
        {
            get
            {
                return _text_height;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _text_height, value);
            }
        }
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
        #endregion
        public override void Run(object context)
        {
            if(detector.State == ModelState.Unloaded)
            {
                detector.LoadRecipe();
            }
            if (classifier.State == ModelState.Unloaded)
            {
                classifier.LoadRecipe();
            }
            if (detector.State== ModelState.Loaded)
            {
                var result = RunInside(ImageInput.Value, RegionInput.Value, context as InspectionContext);
                Result.OnNext(result.Item1);
                TextOutput.OnNext(result.Item4);
            }
            else
            {
                Result.OnNext(false);
                TextOutput.OnNext(new List<CharacterResult>());
            }
            
        }
        public OCRNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "OCR";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);
            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region",
                Editor = new RegionValueEditorViewModel(),
            };
            this.Inputs.Add(RegionInput);
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Bool",
            };
            this.Outputs.Add(Result);
            TextOutput = new ValueNodeOutputViewModel<List<CharacterResult>>()
            {
                Name = "Text",
                PortType = "List<CharacterResult>"
            };
            this.Outputs.Add(TextOutput);

            RootDir = Path.Combine(dir, id);
            
            ImageDir = Path.Combine(RootDir, "images");
            ModelDir = Path.Combine(RootDir, "data");
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

            var detectorDir = System.IO.Path.Combine(ModelDir, "detector");
            DetectionTrainConfigDir = System.IO.Path.Combine(detectorDir, "TrainConfig.txt");
            if (!System.IO.Directory.Exists(detectorDir))
            {
                System.IO.Directory.CreateDirectory(detectorDir);
            }
            DetectionTrainConfig = OCRDetectionTrainConfig.Create(DetectionTrainConfigDir, detectorDir, ImageDir, AnnotationDir);
            if (!System.IO.File.Exists(DetectionTrainConfigDir))
            {
                DetectionTrainConfig.Save(DetectionTrainConfigDir);
            }

            var classifierDir = System.IO.Path.Combine(ModelDir, "classifier");
            ClassificationTrainConfigDir = System.IO.Path.Combine(classifierDir, "TrainConfig.txt");
            if (!System.IO.Directory.Exists(classifierDir))
            {
                System.IO.Directory.CreateDirectory(classifierDir);
            }
            ClassificationTrainConfig = OCRClassifierTrainConfig.Create(ClassificationTrainConfigDir, classifierDir, ImageDir, AnnotationDir);
            if (!System.IO.File.Exists(ClassificationTrainConfigDir))
            {
                DetectionTrainConfig.Save(ClassificationTrainConfigDir);
            }
        }
        public override void OnInitialize()
        {
            detector = new ONNXOCRCRAFTDetector(Path.Combine(ModelDir, "detector"));
            detector.LoadRecipe();
            classifier = new ONNXOCRClassifierV1(Path.Combine(ModelDir, "classifier"));
        }
        public (bool, HTuple, HRegion,List<CharacterResult>) RunInside(HImage image, HRegion Region, InspectionContext e)
        {
            HRegion result_region = new HRegion();
            result_region.GenEmptyRegion();
            if (image == null)
            {
                return (false, null, result_region,null);
            }
            if (!(detector.State == ModelState.Loaded & classifier.State == ModelState.Loaded))
            {
                return (false, null, result_region,null);
            }
            HImage image_infer;
            HTuple row1R=0, col1R=0, row2R=0, col2R=0;
            image.GetImageSize(out HTuple imW, out HTuple imH);
            HRegion mask=null;
            //crop image to region
            if (Region.CountObj() > 0)
            {
                Region.Union1().SmallestRectangle1(out  row1R, out  col1R, out  row2R, out  col2R);
                row1R = Math.Max(row1R, 0);
                col1R = Math.Max(col1R, 0);
                if(row1R<imH& col1R < imW)
                {
                    image_infer = image.CropRectangle1(row1R, col1R, row2R, col2R);
                    mask = Region.MoveRegion(-row1R, -col1R);
                }
                else
                {
                    return (false, null, result_region,null);
                }
                
                
            }
            else
            {
                image_infer = image;
            }           
            var detectorOutput = detector.Infer(image_infer,DetectionTrainConfig.Subsampling);
            HImage center_image = detectorOutput.centerImage;
            HImage box_image = detectorOutput.boxImage;

            HRegion text_region = box_image.Threshold(TextThreshold, 999999.0).Intersection(mask);
            HRegion link_region = center_image.Threshold(LinkThreshold, 999999.0).Intersection(mask);
            HRegion union_regions = text_region.Union2(link_region).Connection();

            double scalex = 1;
            double scaley = 1;
            // align text if rotate base on link
            List<Rect2> detectionBox = DecodeBox2(image_infer,union_regions, text_region, link_region, TextThreshold, LinkThreshold, MinSize, scalex, scaley, TextHeight,MinWidth);
            if (!detectionBox.Any()) { return (false, null, result_region,null); }
            List<HImage> charlist = CropCharRect2(image_infer, detectionBox);
            var classified = classifier.InferWithMaxBatch(charlist);
            HTuple text = new HTuple();
            List<CharacterResult> translateBox = new List<CharacterResult>();
            for (int i = 1; i <= union_regions.CountObj(); i++)
            {
                HRegion union_region = union_regions.SelectObj(i);
                union_region.SmallestRectangle1(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                result_region = result_region.Union2(new HRegion((row1+ row1R.D) *scaley, (col1+col1R.D) * scalex, (row2+ row1R.D)  * scaley, (col2+col1R.D) * scalex));
                string box_string = "";
                for (int j = 0; j < classified.Shape[0]; j++)
                {
                    if (detectionBox[j].group != i) { continue; }
                    var result = classified[j];
                    int charindex = result.argmax();
                    Rect2 rect = detectionBox[j];
                    
                    if ((float)result[charindex] >= (float)Confidence / 100.0)
                    {
                        box_string += classifier.CharList[charindex];
                        HRegion hRegion = new HRegion();
                        translateBox.Add(new CharacterResult(new Rect2(rect.row + row1R.D, rect.col + col1R.D, rect.phi, rect.length1, rect.length2),
                        classifier.CharList[charindex].ToString()));
                        if (ShowDisplay)
                        {
                            
                            if (DisplayBox)
                            {
                                hRegion.GenRectangle2(rect.row + row1R.D, rect.col + col1R.D, rect.phi, rect.length1, rect.length2);
                                e.inspection_result.AddDisplay(hRegion, "red");
                            }
                            if (DisplayLabel)
                            {
                                e.inspection_result.AddText(classifier.CharList[charindex].ToString(), "blue", "#00ff00ff", rect.row + row1R.D, rect.col + col1R.D);
                            }
                            
                        }
                        
                    }
                    else
                    {
                        box_string += "?";
                        HRegion hRegion = new HRegion();
                        hRegion.GenRectangle2(rect.row+row1R.D, rect.col+col1R.D, rect.phi, rect.length1, rect.length2);
                        translateBox.Add(new CharacterResult(new Rect2(rect.row + row1R.D, rect.col + col1R.D, rect.phi, rect.length1, rect.length2),
                         "?"));
                        if (ShowDisplay)
                        {
                            if (DisplayBox)
                            {
                                e.inspection_result.AddDisplay(hRegion, "red");
                            }
                            if (DisplayLabel)
                            {
                                e.inspection_result.AddText("?", "blue", "#00ff00ff", rect.row + row1R.D, rect.col + col1R.D);
                            }
                            
                        };
                        
                    }
                }
                if (box_string != "") { text.Append(box_string); }
            }
            return (true, text, result_region,translateBox);
        }
        private static List<HImage> CropCharRect1(HImage image, List<Rect1> detectionBox)
        {
            List<HImage> charlist = new List<HImage>();
            foreach (var rect in detectionBox)
            {
                HImage charimage = image.CropRectangle1(rect.row1, rect.col1, rect.row2, rect.col2);
                charlist.Add(charimage.InvertImage());
            }
            return charlist;
        }
        private static List<HImage> CropCharRect2(HImage image, List<Rect2> detectionBox)
        {
            List<HImage> charlist = new List<HImage>();
            foreach (var rect in detectionBox)
            {
                HImage charimage;
                if (rect.length2 < rect.length1)
                {
                    var rotatedAngle = rect.phi + Math.PI / 2;
                    HHomMat2D hHomMat2D = new HHomMat2D();
                    hHomMat2D.VectorAngleToRigid(rect.row, rect.col, 0, rect.length1, rect.length2, -rotatedAngle);
                    charimage = hHomMat2D.AffineTransImageSize(image, "constant", (int)(rect.length2 * 2), (int)(rect.length1 * 2));

                }
                else
                {
                    var rotatedAngle = rect.phi;
                    HHomMat2D hHomMat2D = new HHomMat2D();
                    hHomMat2D.VectorAngleToRigid(rect.row, rect.col, 0, rect.length2, rect.length1, -rotatedAngle);
                    charimage = hHomMat2D.AffineTransImageSize(image, "constant", (int)(rect.length1 * 2), (int)(rect.length2 * 2));
                }

                charlist.Add(charimage);
                //charimage.WriteImage("tiff", 0, @"D:\"+rect.Item2.ToString()+".tiff");
            }

            return charlist;
        }

        
        public List<Rect1> DecodeBox(HRegion union_regions, HImage textmap, HImage linkmap, int text_threshold, int link_threshold, int min_size, double scalex, double scaley, int text_height)
        {
            List<Rect1> rects = new List<Rect1>();
            HRegion text_region = textmap.Threshold(text_threshold, 999999.0);
            HRegion link_region = linkmap.Threshold(link_threshold, 999999.0);

            union_regions.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
            text_region = text_region.Difference(link_region).Connection();
            var height_scale = text_height / scaley;
            for (int i = 1; i <= union_regions.Area.Length; i++)
            {
                HRegion text_line = union_regions.SelectObj(i).Intersection(text_region).Connection();
                text_line = text_line.SortRegion("character", "true", "column");
                for (int j = 1; j <= text_line.Area.Length; j++)
                {

                    HRegion region = text_line.SelectObj(j);
                    if (region.Area.I < min_size) { continue; }
                    region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                    var regionw = col2 - col1;
                    var regionh = row2 - row1;
                    if (regionw == 0 | regionh == 0)
                    {
                        continue;
                    }
                    var niter = (int)(Math.Sqrt(region.Area * Math.Min(regionw, regionh) / (regionw * regionh)) * 2);
                    (var sx, var ex, var sy, var ey) = (col1 - niter, col1 + regionw + niter + 1, row1 - niter, row1 + regionh + niter + 1);
                    var dilated = region.DilationRectangle1(1 + niter, 1 + niter);
                    dilated.SmallestRectangle1(out row1, out col1, out row2, out col2);
                    if ((row2 - row1) < height_scale)
                    {
                        //expand height from center
                        var center_row = (row2 + row1) / 2;
                        row1 = (int)(center_row - height_scale / 2);
                        row2 = (int)(center_row + height_scale / 2);
                        //row2 = row1 + text_height; 
                    }
                    //boundary check
                    if (row1 < 0)
                    {
                        row1 = 0;
                    }
                    if (col1 < 0)
                    {
                        col1 = 0;
                    }
                    rects.Add(new Rect1(row1*scaley,col2*scalex,row2*scaley,col2*scalex));
                }
            }
            return rects;
        }
        double IOU(Rect1 rec1,Rect1 rec2)
        {
            var w1 = rec1.col2 - rec1.col1;
            var w2 = rec2.col2 - rec2.col1;
            var h1 = rec1.row2 - rec1.row1;
            var h2 = rec2.row2 - rec2.row1;
            var inter_w = (w1 + w2) - (Math.Max(rec1.col2, rec2.col2) - Math.Min(rec1.col1, rec2.col1));
            var inter_h = (h1 + h2) - (Math.Max(rec1.row2, rec2.row2) - Math.Min(rec1.row1, rec2.row1));
            if (inter_h <= 0 | inter_w <= 0){
                return 0;
            }
            var inter = inter_w * inter_h;
            var union = w1 * h1 + w2 * h2 - inter;
            return inter / union;
        }
        public List<Rect2> DecodeBox2(HImage image, HRegion character_groups, HRegion text_region, HRegion link_region, int text_threshold, int link_threshold, int min_size, double scalex, double scaley, int text_height, int text_width)
        {
            List<Rect2> group_rect2 = new List<Rect2>();
            List<Rect1> non_group_rect1 = new List<Rect1>();
            //HRegion text_region = textmap.Threshold(text_threshold, 999999.0);
            //HRegion link_region = linkmap.Threshold(link_threshold, 999999.0);
            var height_scale = text_height / scaley;
            var width_scale = text_width / scalex;
            //for (int i= 1; i <= character_groups.CountObj();i++)
            //{
            //    HRegion group = character_groups.SelectObj(i);

            //    //var image_rotated =hHomMat2D.AffineTransImageSize(image, "constant" ,(int)length1*2, (int)length2*2);
            //    //image_rotated.WriteImage("tiff", 0, @"D:\box_rotated"+i.ToString()+".tiff");
            //    //box_image.WriteImage("tiff", 0, @"D:\box.tiff");
            //}
            //character_groups.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
            text_region = text_region.Difference(link_region).Connection();
           
            for (int i = 1; i <= character_groups.CountObj(); i++)
            {
                List<Rect1> resultbox = new List<Rect1>();
                HRegion group = character_groups.SelectObj(i);
                if (group.Area < min_size)
                {
                    continue;
                }
                HRegion text_line = group.Intersection(text_region).Connection();

                text_line = text_line.SortRegion("character", "true", "column");

                text_line.AreaCenter(out HTuple center_rows, out HTuple center_cols);
                if (text_line.CountObj() == 0)
                {
                    continue;
                }
                var Grouptransform = GetTransformLineBased(height_scale, group, center_rows, center_cols);

                bool group_is_rotated = false;
                if (text_line.CountObj() > 2)
                {
                    group_is_rotated = true;
                }
                
                for (int j = 1; j <= text_line.CountObj(); j++)
                {
                    HRegion region = text_line.SelectObj(j);
                    if (group_is_rotated)
                    {
                        region = region.AffineTransRegion(Grouptransform.transform, "constant");
                    }
                    if (region.Area.I < min_size) { continue; }
                    region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                    var regionw = col2 - col1;
                    var regionh = row2 - row1;
                    if (regionw == 0 | regionh == 0)
                    {
                        continue;
                    }
                    var niter = (int)(Math.Sqrt(region.Area * Math.Min(regionw, regionh) / (regionw * regionh)) * 2);
                    (var sx, var ex, var sy, var ey) = (col1 - niter, col1 + regionw + niter + 1, row1 - niter, row1 + regionh + niter + 1);
                    var dilated = region.DilationRectangle1(1 + niter, 1 + niter);
                    dilated.SmallestRectangle1(out row1, out col1, out row2, out col2);
                    if ((row2 - row1) < height_scale)
                    {
                        //expand height from center
                        var center_row = (row2 + row1) / 2;
                        row1 = (int)(center_row - height_scale / 2);
                        row2 = (int)(center_row + height_scale / 2);
                        //row2 = row1 + text_height; 
                    }
                    if ((col2 - col1) < width_scale)
                    {
                        //expand height from center
                        var center_col = (col2 + col1) / 2;
                        col1 = (int)(center_col - width_scale / 2);
                        col2 = (int)(center_col + width_scale / 2);
                        //row2 = row1 + text_height; 
                    }
                    //boundary check
                    if (row1 < 0)
                    {
                        row1 = 0;
                    }
                    if (col1 < 0)
                    {
                        col1 = 0;
                    }
                    resultbox.Add(new Rect1(row1, col1, row2, col2,i));
                }
                //resolve overlap box
                List<Rect1> noneOverlapBoxes = RemoveOverlap(resultbox);
                //add result
                foreach (var box in noneOverlapBoxes)
                {
                    if (group_is_rotated)
                    {

                        Grouptransform.transform.HomMat2dInvert().AffineTransPixel((box.row1 + box.row2) / 2, (box.col1 + box.col2) / 2, out double rowcenter, out double colcenter);
                        var phi = Grouptransform.theta;
                        group_rect2.Add(new Rect2(rowcenter, colcenter, phi, (box.col2 - box.col1) / 2, (box.row2 - box.row1) / 2, i));
                    }
                    else
                    {
                        non_group_rect1.Add(box);
                        
                    }
                }
            }
            //remove overlap of non group
            List<Rect1> nonGroup_noOverlapBoxes = RemoveOverlap(non_group_rect1);
            foreach(var box in nonGroup_noOverlapBoxes)
            {
                group_rect2.Add(new Rect2((box.row1 + box.row2) / 2, (box.col1 + box.col2) / 2, 0, (box.col2 - box.col1) / 2, (box.row2 - box.row1) / 2, box.group));
            }

            return group_rect2;
        }

        private List<Rect1> RemoveOverlap(List<Rect1> resultbox)
        {
            List<Rect1> noneOverlapBoxes = new List<Rect1>();
            while (resultbox.Count > 0)
            {
                List<Rect1> overlaped = new List<Rect1>();
                var box = resultbox[0];

                resultbox.Remove(box);
                foreach (var nextbox in resultbox)
                {
                    if (IOU(box, nextbox) > 0.2)
                    {
                        overlaped.Add(nextbox);
                    }
                }

                if (overlaped.Count > 0)
                {
                    foreach (var removeBox in overlaped)
                    {
                        resultbox.Remove(removeBox);
                    }
                    overlaped.Add(box);
                    var row1 = overlaped.Average(x => x.row1);
                    var row2 = overlaped.Average(x => x.row2);
                    var col1 = overlaped.Average(x => x.col1);
                    var col2 = overlaped.Average(x => x.col2);
                    noneOverlapBoxes.Add(new Rect1(row1, col1, row2, col2));
                }
                else
                {
                    noneOverlapBoxes.Add(box);
                }
            }

            return noneOverlapBoxes;
        }

        public static double GenerateLinearBestFit(double[] rows,double[] cols, out double a, out double b)
        {
            
            int numPoints = rows.Length;
            if (numPoints < 2)
            {
                a = 0;
                b = 0;
                return 0;
            }
            double meanX = cols.Average();
            double meanY = rows.Average();
            double sumXSquared = 0, sumXY = 0 ;
            for (int i = 0; i < numPoints; i++)
            {
                sumXSquared += cols[i]*cols[i];
                sumXY += rows[i]*cols[i];
            }
           

            a = (sumXY / numPoints - meanX * meanY) / (sumXSquared / numPoints - meanX * meanX);
            b = (a * meanX - meanY);
            double error = 0;
            for (int i = 0; i < numPoints; i++)
            {
                error += (rows[i]- a * cols[i] + b) * (rows[i] - a * cols[i] + b);
            }
            return error;
        }
        private static (HHomMat2D transform, double rx, double ry, double theta) GetTransformLineBased(double height_scale, HRegion group,HTuple rows,HTuple cols)
        {
            var error=GenerateLinearBestFit(rows.DArr, cols.DArr, out double a, out double b);
            group.SmallestRectangle2(out double row, out double col, out double phi, out double length1, out double length2);
            if (error>50)
            {
                group.SmallestRectangle1(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                var niter1 = (int)(Math.Sqrt(group.Area * Math.Min(col2 - col1, row2 - row1) / ((col2 - col1) * (row2 - row1))) * 2);
                var width = col2 - col1 + (1 + niter1) / 2;
                var height = row2 - row1 + (1 + niter1) / 2;
                HHomMat2D hHomMat2D = new HHomMat2D();

                return (hHomMat2D, 0, 0, 0); ;
            }
            else
            {
                 HOperatorSet.AngleLx(-b, 0, a - b, 1, out HTuple angle);
                phi = angle;
                if ((length2 * 2) < height_scale)
                {
                    //expand height from center
                    //var center_row = (row2 + row1) / 2;
                    //row1 = (int)(center_row - height_scale / 2);
                    //row2 = (int)(center_row + height_scale / 2);
                    //row2 = row1 + text_height; 
                    length2 = height_scale / 2;
                }
                var regionh = length2 * 2;
                var regionw = length1 * 2;
                var niter = (int)(Math.Sqrt(group.Area * Math.Min(length1, regionh) / (regionw * regionh)) * 2);
                length1 += (1 + niter) / 2;
                length2 += (1 + niter) / 2;
                //var dilated = region.DilationRectangle1(1 + niter, 1 + niter);
                HHomMat2D hHomMat2D = new HHomMat2D();
                hHomMat2D.VectorAngleToRigid(row, col, phi, row, col, 0);
                return (hHomMat2D, 0, 0, phi);
            }



        }

        private static (HHomMat2D transform,double rx,double ry,double theta) GetTransformRegionBased(double height_scale, HRegion group)
        {
       
            group.SmallestRectangle2(out double row, out double col, out double phi, out double length1, out double length2);
            if(length1*2>height_scale & length2 > height_scale )
            {
                group.SmallestRectangle1(out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                var niter1 = (int)(Math.Sqrt(group.Area * Math.Min(col2-col1, row2-row1) / ((col2-col1) * (row2-row1))) * 2);
                var width =  col2-col1 + (1 + niter1) / 2;
                var height = row2-row1 +(1 + niter1) / 2;
                HHomMat2D hHomMat2D = new HHomMat2D();

                return (hHomMat2D, 0, 0, 0); ;
            }
            else
            {
                if ((length2 * 2) < height_scale)
                {
                    //expand height from center
                    //var center_row = (row2 + row1) / 2;
                    //row1 = (int)(center_row - height_scale / 2);
                    //row2 = (int)(center_row + height_scale / 2);
                    //row2 = row1 + text_height; 
                    length2 = height_scale / 2;
                }
                var regionh = length2 * 2;
                var regionw = length1 * 2;
                var niter = (int)(Math.Sqrt(group.Area * Math.Min(length1, regionh) / (regionw * regionh)) * 2);
                length1 += (1 + niter) / 2;
                length2 += (1 + niter) / 2;
                //var dilated = region.DilationRectangle1(1 + niter, 1 + niter);
                HHomMat2D hHomMat2D = new HHomMat2D();
                hHomMat2D.VectorAngleToRigid(row, col, phi, length2, length1, 0);
                return (hHomMat2D, row - length2, col - length1, phi);
            }
            
            
            
        }

        
    }
    public enum OCRDETECTIONMODEL
    {
        mobilenetv2,vgg16
    }
    public enum OCRCLASSIFICATIONMODEL
    {
        resnet,small,medium
    }
    public class OCRClassifierTrainConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageOrdering ImageOrdering { get; set; } = ImageOrdering.channels_first;
        public int Subsampling { get; set; } = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public OCRCLASSIFICATIONMODEL ModelName { get; set; } = OCRCLASSIFICATIONMODEL.resnet;
        public bool Augmentation { get; set; }
        public double StartLearningRate { get; set; } = 1e-3;
        public double EndLearningRate { get; set; } = 1e-6;
        public int FINETUNE_EPOCHS { get; set; } = 20;
        public int Epochs { get; set; } = 100;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public int NumChannels { get; set; } = 3;
        public SegmentAugmentation AugmentationSetting { get; set; } = new SegmentAugmentation();
        public string Optimizer { get; set; } = "adam";
        public string ImageDir { get; set; }
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 32;
        public int INPUT_HEIGHT { get; set; } = 32;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingPrecision Precision { get; set; } = TrainingPrecision.float32;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingType TrainningType { get; set; } = TrainingType.Restart;
        public bool EarlyStopping { get; set; } = false;
        public void Save(string dir)
        {
            System.IO.File.WriteAllText(dir, JsonConvert.SerializeObject(this));
        }
        public static OCRClassifierTrainConfig Create(string dir, string modelDir, string imageDir, string annotationDir)
        {

            if (System.IO.File.Exists(dir))
            {
                var json = File.ReadAllText(dir);
                try
                {
                    var config = JsonConvert.DeserializeObject<OCRClassifierTrainConfig>(json);
                    return config;
                }
                catch (Exception ex)
                {
                    return new OCRClassifierTrainConfig(modelDir, imageDir, annotationDir); ;
                }


            }
            else
            {
                return new OCRClassifierTrainConfig(modelDir, imageDir, annotationDir);
            }

        }
        private OCRClassifierTrainConfig(string modelDir, string imageDir, string annotationDir)
        {
            SavedModelDir = modelDir;
            ImageDir = imageDir;
            AnnotationDir = annotationDir;
        }
        private OCRClassifierTrainConfig()
        {

        }
    }
    public class OCRDetectionTrainConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageOrdering ImageOrdering { get; set; } = ImageOrdering.channels_first;
        public int Subsampling { get; set; } = 1;
        [JsonConverter(typeof(StringEnumConverter))]
        public OCRDETECTIONMODEL ModelName { get; set; } = OCRDETECTIONMODEL.mobilenetv2;
        public bool Augmentation { get; set; }
        public double StartLearningRate { get; set; } = 1e-3;
        public double EndLearningRate { get; set; } = 1e-6;
        public int FINETUNE_EPOCHS { get; set; } = 20;
        public int Epochs { get; set; } = 100;
        public string AnnotationDir { get; set; }
        public string SavedModelDir { get; set; }
        public int NumChannels { get; set; } = 3;
        public SegmentAugmentation AugmentationSetting { get; set; } = new SegmentAugmentation();
        public string Optimizer { get; set; } = "adam";
        public string ImageDir { get; set; }
        public int BATCH_SIZE { get; set; } = 4;
        public int INPUT_WIDTH { get; set; } = 416;
        public int INPUT_HEIGHT { get; set; } = 416;
        public int LEARNING_RATE_LEVELS { get; set; } = 2;
        public int LEARNING_RATE_STEPS { get; set; } = 2;
        public double WARMUP_LEARNING_RATE { get; set; } = 1e-6;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingPrecision Precision { get; set; } = TrainingPrecision.float32;
        [JsonConverter(typeof(StringEnumConverter))]
        public TrainingType TrainningType { get; set; } = TrainingType.Restart;
        public bool EarlyStopping { get; set; } = false;
        public void Save(string dir)
        {
            System.IO.File.WriteAllText(dir, JsonConvert.SerializeObject(this));
        }
        public static OCRDetectionTrainConfig Create(string dir, string modelDir, string imageDir, string annotationDir)
        {

            if (System.IO.File.Exists(dir))
            {
                var json = File.ReadAllText(dir);
                try
                {
                    var config = JsonConvert.DeserializeObject<OCRDetectionTrainConfig>(json);
                    return config;
                }
                catch (Exception ex)
                {
                    return new OCRDetectionTrainConfig(modelDir, imageDir, annotationDir); ;
                }


            }
            else
            {
                return new OCRDetectionTrainConfig(modelDir, imageDir, annotationDir);
            }

        }
        private OCRDetectionTrainConfig(string modelDir, string imageDir, string annotationDir)
        {
            SavedModelDir = modelDir;
            ImageDir = imageDir;
            AnnotationDir = annotationDir;
        }
        private OCRDetectionTrainConfig()
        {

        }
    }
    public class CharacterResult:IHalconDeserializable
    {
        public Rect2 Box { get; set; }
        public string ClassName { get; set; }
        public CharacterResult(Rect2 Box, string ClassName)
        {
            this.Box = Box;
            this.ClassName = ClassName;
        }
        public CharacterResult()
        {

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
    public class Rect2:IHalconDeserializable
    {
        public Rect2(double row, double col, double phi, double length1, double length2, int group = -1)
        {
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.length1 = length1;
            this.length2 = length2;
            this.group = group;
        }
        public int group { get; set; }
        public double row { get; set; }
        public double col { get; set; }
        public double phi { get; set; }
        public double length1 { get; set; }
        public double length2 { get; set; }
        public Rect2()
        {

        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file,this);
        }
    }
    public class EllipseData : IHalconDeserializable
    {
        public EllipseData(double row, double col, double phi, double radius1, double radius2, int group = -1)
        {
            this.row = row;
            this.col = col;
            this.phi = phi;
            this.radius1 = radius1;
            this.radius2 = radius2;
            this.group = group;
        }
        public int group { get; set; }
        public double row { get; set; }
        public double col { get; set; }
        public double phi { get; set; }
        public double radius1 { get; set; }
        public double radius2 { get; set; }
        public EllipseData()
        {

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
    public class CircleData : IHalconDeserializable
    {
        public CircleData(double row, double col, double radius,int group = -1)
        {
            this.row = row;
            this.col = col;
            this.radius = radius;
            this.group = group;
        }
        public int group { get; set; }
        public double row { get; set; }
        public double col { get; set; }
        public double radius { get; set; }
        public CircleData()
        {

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
    public class Rect1
    {
        public Rect1(double row1, double col1, double row2, double col2,int group=-1)
        {
            this.row1 = row1;
            this.col1 = col1;
            this.row2 = row2;
            this.col2 = col2;
            this.group = group;
        }
        public int group { get; set; }
        public double row1 { get; set; }
        public double col1 { get; set; }
        public double row2 { get; set; }
        public double col2 { get; set; }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    
    public interface IOCRClassifier
    {
        string ModelDir { get; }
        string CharList { get; }
        bool IsLoaded { get; set; }
        NDArray Infer(List<HImage> imgInput);
        bool Reload();
        void TrainPython(string configDir, Action<TrainingArgs> TrainUpdate);
    }
    public class ONNXOCRClassifierV1 : ONNXModel
    {
        
        int num_channel = 3;
        int input_width=32, input_height=32;
        int num_output_class = 0;
        string input_name;
        public string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"; //default char list, actual char list need to
                                                                                                   //be load in model meta data yaml
        public string ModelDir { get; set; }
        public static int MAX_BATCH_SIZE=16;
        public ONNXOCRClassifierV1(string ModelDir)
        {
            this.ModelDir = ModelDir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        InferenceSession ONNXSession;
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
                //OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                //ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                //      {
                //          // Enable INT8 for QAT models, disable otherwise.
                //          { "trt_fp16_enable", "1" },
                //          { "trt_engine_cache_enable", "1" },
                //            {"trt_engine_cache_path",ModelDir}
                //     });
                var options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                ONNXSession = new InferenceSession(System.IO.Path.Combine(directory, "model.onnx"), options);
                input_name = ONNXSession.InputMetadata.Keys.FirstOrDefault();
               
                if (input_name != null)
                {
                    input_width = ONNXSession.InputMetadata[input_name].Dimensions[3];
                    input_height = ONNXSession.InputMetadata[input_name].Dimensions[2];

                }
                var output_name = ONNXSession.OutputMetadata.Keys.FirstOrDefault();
                if (output_name != null)
                {
                    num_output_class = ONNXSession.OutputMetadata[output_name].Dimensions[1];

                }

                Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> t1;
                if (input_width == -1 | input_height == -1)
                {
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * 32 * 32 * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, 32, 32 })),
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
        public void CopyDefaultModel()
        {
            string PretrainModelDir = System.IO.Path.Combine(MainWindow.AppPath, "Designer", "Python", "OCR", "classify", "weights", "resnet");

            var ModelPath = System.IO.Path.Combine(ModelDir, "model.onnx");
            var PretrainPath = System.IO.Path.Combine(PretrainModelDir, "model.onnx");
            if (!System.IO.File.Exists(ModelPath))
            {
                System.IO.File.Copy(PretrainPath, ModelPath);
            }
            var MetaPath = System.IO.Path.Combine(ModelDir, "meta.yaml");
            var PretrainMetaPath = System.IO.Path.Combine(PretrainModelDir, "meta.yaml");
            if (!System.IO.File.Exists(MetaPath))
            {
                System.IO.File.Copy(PretrainMetaPath, MetaPath);
            }
        }
        public void LoadModelMetaData()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var order = deserializer.Deserialize<ModelMetaData>(File.ReadAllText(System.IO.Path.Combine(ModelDir,"meta.yaml")));
            this.CharList = order.CharList;
        }
        public class ModelMetaData
        {
            public string CharList { get; set; }
        }
        public void ClearTensorRTProfile()
        {
            var engineFiles = Directory.GetFiles(ModelDir);//, ".engine|.profile");
            foreach (var file in engineFiles)
            {
                if(file.EndsWith(".engine")| file.EndsWith(".profile"))
                {
                    System.IO.File.Delete(file);
                }
                
            }
        }
        public bool LoadRecipe()
        {
            try
            {
                CopyDefaultModel();
                return LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public NDArray InferWithMaxBatch(List<HImage> imgInput)
        {
            int imageCount = imgInput.Count;
            
            int n_run = (int)Math.Ceiling((double)imageCount / MAX_BATCH_SIZE);
            float[] results = new float[imageCount * num_output_class];
            for(int i=0; i < n_run; i++)
            {
                var images = imgInput.Skip(i * MAX_BATCH_SIZE).Take(MAX_BATCH_SIZE);
                var result = Infer(images);
                result.CopyTo(results, i* MAX_BATCH_SIZE * num_output_class);
            }
            NDArray features_output = new NDArray(results, new NumSharp.Shape(imageCount,num_output_class), order: 'F');
            return features_output;
        }
        public float[] Infer(IEnumerable<HImage> imgInput)
        {
            int imagesCount = imgInput.Count();
            float[] array_inputs = new float[imagesCount * input_width * input_height * num_channel];
            int i = 0;
            foreach (HImage image in imgInput)
            {
                HImage img = image;
                if (image.CountChannels() != num_channel)
                {
                    if (num_channel == 1)
                    {
                        img = image.Rgb1ToGray();
                    }
                    else
                    {
                        img = image.Compose3(image, image);
                    }
                }
                var image_resized = img.ZoomImageSize(input_width, input_height, "constant").ConvertImageType("float");
                string type;
                int width, height;
                if (num_channel == 3)
                {
                    IntPtr pointerR, pointerG, pointerB;
                    image_resized.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                    Marshal.Copy(pointerR, array_inputs, i, input_width * input_height);
                    Marshal.Copy(pointerG, array_inputs, i + input_width * input_height, input_width * input_height);
                    Marshal.Copy(pointerB, array_inputs, i + input_width * input_height * 2, input_width * input_height);
                }
                else
                {
                    IntPtr pointerGray = image_resized.GetImagePointer1(out type, out width, out height);
                    Marshal.Copy(pointerGray, array_inputs, i, input_width * input_height);
                }
                i += input_width * input_height * num_channel;
            }
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_inputs,new int[]{ imagesCount, 3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var output = results[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            var TensorOutput = output.ToArray(); //channel last
            //NDArray features_output = new NDArray(TensorOutput, new NumSharp.Shape(output.Dimensions.ToArray()), order: 'F');
            return TensorOutput;

        }
    }   
    public class OCRClassifierModel
    {
        static readonly Dictionary<string, string> Models = new Dictionary<string, string>()
        {
            { "resnet", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" },
            { "small", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" },
            { "medium", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" }
        };
    }

   
    public enum ONNXProvider
    {
        CUDA, TensorRT,CPU
    }
    public class ONNXOCRCRAFTDetector: ONNXModel
    {
        public string ModelDir;
        int num_channel = 3;
        public int smallest_div = 16;
        public ONNXOCRCRAFTDetector(string ModelDir)
        {
            this.ModelDir = ModelDir;
            if (!System.IO.Directory.Exists(ModelDir))
            {
                System.IO.Directory.CreateDirectory(ModelDir);
            }
        }
        int input_width, input_height;

        InferenceSession ONNXSession;
        string input_name;

        public bool LoadOnnx(string directory)
        {
            try
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(directory, "model.onnx")))
                {
                    State = ModelState.NotFound;
                    return false;
                }
                SessionOptions options;
                //if (Provider == ONNXProvider.TensorRT)
                //{
                    
                //    OrtTensorRTProviderOptions ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                //    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                //      {
                //          // Enable INT8 for QAT models, disable otherwise.
                //          { "trt_fp16_enable", "1" },
                //          { "trt_engine_cache_enable", "1" },
                //          {"trt_engine_cache_path",ModelDir}
                //     });
                //    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                //}
                //else
                //{
                    int gpuDeviceId = 0; // The GPU device ID to execute on
                    options = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                //}
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
                    t1 = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(1 * smallest_div * smallest_div * 3);

                    var inputs = new List<NamedOnnxValue>()
                    {
                        NamedOnnxValue.CreateFromTensor<float>(input_name, t1.Reshape(new int[]{ 1, 3, smallest_div, smallest_div })),
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
        public void PostTrainEvent()
        {
            if(Provider == ONNXProvider.TensorRT)
            {
                var engineFiles = Directory.GetFiles(ModelDir);//, ".engine|.profile");
                foreach (var file in engineFiles)
                {
                    if (file.EndsWith(".engine") | file.EndsWith(".profile"))
                    {
                        System.IO.File.Delete(file);
                    }

                }
            }

        }
        public void CopyDefaultModel()
        {
            var ModelPath = System.IO.Path.Combine(ModelDir, "model.onnx");
            var PretrainPath = System.IO.Path.Combine(MainWindow.AppPath, "Designer", "Python","OCR","detection", "craft", "weights", "model.onnx");
            if (!System.IO.File.Exists(ModelPath))
            {
                System.IO.File.Copy( PretrainPath, ModelPath);
            }
        }
        public bool LoadRecipe()
        {
            try
            {
                CopyDefaultModel();
                return LoadOnnx(ModelDir);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public (HImage boxImage,HImage centerImage) Infer(HImage imgInput, int subsampling)
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
            HImage imageSampling;

            if (subsampling > 1)
            {
                imageSampling = imgInput.ZoomImageFactor(1 / (double)subsampling, 1 / (double)subsampling, "constant");
            }
            else
            {
                imageSampling = imgInput;
            }
            HImage imagePadded = Processing.HalconUtils.PadImage(imageSampling, smallest_div, out int originalw, out int originalh);
            imagePadded = imagePadded.ConvertImageType("float");
            var array_final = Processing.HalconUtils.HImageToFloatArray(imagePadded, num_channel, out int input_width, out int input_height);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor(input_name,new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,input_height,input_width }))
            };
            var results = ONNXSession.Run(inputs).ToList();
            var TensorOutput = (results[results.Count - 1].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).ToArray(); //channel last
            //convert channel last to first
            NDArray nDArray = (new NDArray(TensorOutput,new Shape(TensorOutput.Length/2, 2))*255).transpose(new int[] { 1,0}); //float32
            
            var boxImage = new HImage("float", input_width, input_height);
            var centerImage = new HImage("float", input_width, input_height);
            IntPtr pointerBox = boxImage.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);
            IntPtr pointerCenter = centerImage.GetImagePointer1(out HTuple type1, out HTuple width1, out HTuple height1);
            //Marshal.Copy(TensorOutput, 0, pointerBox, input_height * input_width);
            //Marshal.Copy(TensorOutput, width*height, pointerCenter, input_height * input_width);
            nDArray[0].CopyTo(pointerBox);
            nDArray[1].CopyTo(pointerCenter);
            //crop if image was padded
            if (input_width != originalw | input_height != originalh)
            {
                boxImage = boxImage.ChangeFormat(originalw, originalh);
                centerImage = centerImage.ChangeFormat(originalw, originalh);
            }
            if (subsampling > 1)
            {
                boxImage = boxImage.ZoomImageFactor(subsampling, subsampling, "constant");
                centerImage = centerImage.ZoomImageFactor(subsampling, subsampling, "constant");
            }

            return (boxImage.ConvertImageType("byte"),centerImage.ConvertImageType("byte"));

        }
    }
}