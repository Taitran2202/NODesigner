using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "CheckDifferenceMultiModelNode", visible: false)]
    public class CheckDifferenceMultiModelNode : BaseNode
    {
        static CheckDifferenceMultiModelNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<CheckDifferenceMultiModelNode>));
        }

        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<HRegion> Region { get; set; }
        public CheckDifferenceModel Model { get; set; } = new CheckDifferenceModel();
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    TrainCheckDifferenceModelWindow wd = new TrainCheckDifferenceModelWindow(Model, ImageInput.Value.Clone());
                    wd.ShowDialog();
                    break;
            }
        }
        [HMIProperty("Classifier Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }

        public override void Run(object context)
        {

            Region.OnNext(RunInside(ImageInput.Value, FixtureInput.Value, context as InspectionContext));
        }

        public CheckDifferenceMultiModelNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.CanBeRemovedByUser = true;
            this.Name = "Print Inspection";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);
            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(FixtureInput);

            Region = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Output",
                PortType = "HRegion"
            };
            this.Outputs.Add(Region);

            //tf.Context.Config.GpuOptions.PerProcessGpuMemoryFraction = 0.3;
        }

        public InspectionContext context;
        public HRegion RunInside(HImage image, HHomMat2D fixture, InspectionContext e)
        {
            HImage imageProcess;
            if (image == null)
            {
                return new HRegion();
            }           
            HTuple channels = image.CountChannels();
            if (channels < 3)
            {
                imageProcess = image.Compose3(image, image);
            }
            else
            {
                imageProcess = image;
            }
            Model.calib = new Calibration();
            var result = Model.Run(imageProcess, null, e);
            if (result == null)
            {
                return new HRegion();
            }
            else
            {
                return result;
            }
            //Model.Run(image,)
            
        }
    }
    public class CheckDifferenceModel : HelperMethods, INotifyPropertyChanged
    {

        

        public CheckDifferenceModel()
        {


            CheckDiffGrayModel = new CheckDifferenceProfileGray();
            SearchRegion = new HRegion();
            SearchRegion.GenEmptyRegion();         
            ModelRegion.GenEmptyRegion();
            SearchRegion.GenEmptyRegion();
            CheckingRegion.GenEmptyRegion();
        }


        public HRegion ModelRegion
        {
            get
            {
                return _model_region;
            }
            set
            {
                _model_region = value;

            }
        }
        public HRegion SearchRegion
        {
            get { return _search_region; }
            set
            {
                _search_region = value;
                SearchRegionAff = value;

            }
        }

        public HRegion CheckingRegion
        {
            get
            {
                return _checking_region;
            }
            set
            {
                _checking_region = value;

            }
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                // MainWindow.WriteActionCsv(this.Name + "." + prop + " = " + value.ToString(), "Change Parameter");
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        HRegion _model_region = new HRegion(), _search_region = new HRegion(), _checking_region = new HRegion();

        private CheckDifferenceProfileGray _gray_model;
        public CheckDifferenceProfileGray CheckDiffGrayModel
        {
            get
            {
                return _gray_model;
            }
            set
            {
                if (_gray_model != value)
                {
                    _gray_model = value;
                    RaisePropertyChanged("CheckDiffGrayModel");
                }
            }
        }

        internal void TrainModel(HImage grayImage, HRegion region)
        {
            CheckDiffGrayModel.TrainModel(grayImage, region);
        }


        bool _display_graphics;
        public bool DisplayGraphics
        {
            get
            {
                return _display_graphics;
            }
            set
            {
                if (_display_graphics != value)
                {
                    _display_graphics = value;
                    RaisePropertyChanged("DisplayGraphics");
                }
            }
        }

        #endregion // Properties

        HRegion SearchRegionAff;

        public void DispElip(HWindow display,HRegion region)
        {
            int count = 0;
            for (int i = 0; i < region.CountObj(); i++)
            {
                double row, col, ra, rb, phi;
                region[i + 1].AreaCenter(out row, out col);
                ra = region[i + 1].EllipticAxis(out rb, out phi);
                display.DispEllipse(row, col, phi, ra + 20, rb + 20);
                count++;
                if (count == 100)
                {
                    break;
                }
            }
        }


        public Calibration calib;
        public HRegion Run(HImage image, HHomMat2D fixture, InspectionContext e)
        {
            
            InspectionResult inspection_result = e.inspection_result;
            if (ModelRegion == null || SearchRegion == null || CheckingRegion == null)
            {
                
                return null;
            }

            if (fixture != null)
            {
                SearchRegionAff = SearchRegion.AffineTransRegion(fixture, "constant");
            }
            else
            {
                SearchRegionAff = SearchRegion;
            }



            HRegion diff = new HRegion();
            HRegion regionss = new HRegion();
            if (CheckDiffGrayModel.Trained)
            {

                try
                {
                    diff = CheckDiffGrayModel.Run(image, SearchRegionAff, out regionss, calib);
                }
                catch (Exception)
                {
                    
                }
            }
            else
            {
                
            }
            if (diff != null)
            {


                regionss = regionss.Connection();


                if (diff.CountObj() > 0)
                {
                    e.inspection_result.AddRegion(SearchRegionAff, "red");
                    e.inspection_result.AddRegion(ModelRegion, "red");
                    e.inspection_result.AddRegion(regionss, "red");
                    e.result &= false;
                    return regionss;
                }
                else
                {
                    e.inspection_result.AddRegion(SearchRegionAff, "green");
                    return null;
                }




            }
            return null;

        }

        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            SaveParam(file, this);

        }
    }
    public class CheckDifferenceProfileGray : HelperMethods, INotifyPropertyChanged
    {

        #region Bindingbase
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        public void Save(HFile file)
        {

            SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {

            LoadParam(item, this);

        }


        private int id;
        public int ID
        {
            get
            {
                return id;
            }
            set
            {
                if (id != value)
                {
                    id = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }


        private long _processing_time = 0;
        public long ProcessingTime
        {
            get
            {
                return _processing_time;
            }
            set
            {
                if (_processing_time != value)
                {
                    _processing_time = value;
                    RaisePropertyChanged("ProcessingTime");
                }
            }
        }


        public CheckDifferenceParameterGray Params { get; set; } = new CheckDifferenceParameterGray();

        HImage reference_image;
        public HImage ReferenceImage
        {
            get
            {
                return reference_image;
            }
            set
            {
                if (reference_image != value)
                {
                    reference_image = value;
                    RaisePropertyChanged("ReferenceImage");
                }
            }
        }


        private HRegion _ref_region = new HRegion(10, 10, 100, 100.0);
        public HRegion RefRegion
        {
            get
            {
                return _ref_region;
            }
            set
            {
                if (_ref_region != value)
                {
                    _ref_region = value;
                    RaisePropertyChanged("RefRegion");
                }
            }
        }


        private HRegion _ref_region_origin = new HRegion(10, 10, 100, 100.0);
        public HRegion RefRegionOrigin
        {
            get
            {
                return _ref_region_origin;
            }
            set
            {
                if (_ref_region_origin != value)
                {
                    _ref_region_origin = value;
                    RaisePropertyChanged("RefRegionOrigin");
                }
            }
        }


        private HRegion _search_region = new HRegion(10, 10, 100, 100.0);
        public HRegion SearchRegion
        {
            get
            {
                return _search_region;
            }
            set
            {
                if (_search_region != value)
                {
                    _search_region = value;
                    RaisePropertyChanged("SearchRegion");
                }
            }
        }



        public HShapeModel shapeModel { get; set; } = new HShapeModel();

        private double _origin_row;
        public double OriginRow
        {
            get
            {
                return _origin_row;
            }
            set
            {
                if (_origin_row != value)
                {
                    _origin_row = value;
                    RaisePropertyChanged("OriginRow");
                }
            }
        }

        private double _origin_col;
        public double OriginCol
        {
            get
            {
                return _origin_col;
            }
            set
            {
                if (_origin_col != value)
                {
                    _origin_col = value;
                    RaisePropertyChanged("OriginCol");
                }
            }
        }




        private HImage _mask_image;
        public HImage MaskImage
        {
            get
            {
                return _mask_image;
            }
            set
            {
                if (_mask_image != value)
                {
                    _mask_image = value;
                    RaisePropertyChanged("MaskImage");
                }
            }
        }


        public HImage CreateMaskImage(HImage image)
        {
            var a = image.SobelAmp("thin_sum_abs", 3).BinomialFilter(3, 3);
            a.MinMaxGray(a, 0, out double min, out double max, out double range);
            a = a.ScaleImage(35.0 / range, min * 35.0 / range);
            return a;
        }
        public HRegion ErrorRegion;
        public bool Trained { get; set; }
        public void TrainModel(HImage image, HRegion region)
        {
            try
            {

                ReferenceImage = image.ReduceDomain(region.DilationRectangle1(10, 10)).CropDomain().Rgb1ToGray();
                shapeModel.CreateShapeModel(ReferenceImage.Rgb1ToGray().BinomialFilter(3, 3), "auto", -0.17, 0.34, "auto", "none", "use_polarity", new HTuple(15, 30, 5), (HTuple)10);
                ReferenceImage.GetImageSize(out int w, out int h);
                RefRegion = new HRegion(0, 0.0, h - 1, w - 1);
                //   var var_image = ReferenceImage.SobelAmp("sum_sqrt", 3);
                //  VariationModel.PrepareDirectVariationModel(ReferenceImage, var_image, 50.0, 50);
                OriginRow = h / 2;
                OriginCol = w / 2;
                MaskImage = CreateMaskImage(ReferenceImage);

                Trained = true;

            }
            catch (Exception)
            {
                MessageBox.Show("Error", "Train Error");
                Trained = false;
            }

        }

        public bool CanFind = false;

        Calibration calib;
        public HRegion Run(HImage image, HRegion region, out HRegion diff_trans_region, Calibration calib)
        {
            this.calib = calib;
            if (region == null)
            {
                region = SearchRegion;
            }

            ////HHomMat2D hom = new HHomMat2D();
            //shapeModel.FindShapeModel(compare_image.Rgb1ToGray().ReduceDomain(copare_region).BinomialFilter(5, 5), out HTuple row, out HTuple col, out HTuple angle, out HTuple score);
            diff_trans_region = new HRegion();
            diff_trans_region.GenEmptyRegion();

            if (Trained == false)
            { 

                return null;
            }

            try
            {

                HImage CompareCrop = image.ReduceDomain(region).CropDomain().Rgb1ToGray();
                region.AreaCenter(out double r1, out double c1);
                CompareCrop.GetImageSize(out int w1, out int h1);

                HHomMat2D hom1 = new HHomMat2D();
                hom1.VectorAngleToRigid(r1, c1, 0, h1 / 2, w1 / 2, 0);
                HHomMat2D hom_scale = new HHomMat2D();
                hom_scale = hom_scale.HomMat2dScale((double)Params.Scale / 100, (double)Params.Scale / 100, 0.0, 0);

                shapeModel.SetShapeModelParam(new HTuple("border_shape_models"), new HTuple("true"));
                int s = shapeModel.GetShapeModelParams(out double b, out double c, out double d, out HTuple e, out HTuple f, out HTuple f2, out string g, out int h);







                shapeModel.FindShapeModel(CompareCrop, (HTuple)(-0.17), (HTuple)0.35, new HTuple((double)Params.PatternScore / 100.0), (HTuple)1, (HTuple)0, (HTuple)"interpolation", new HTuple(0), (HTuple)0.75, out HTuple row, out HTuple col, out HTuple angle, out HTuple score);
                // CompareCrop.WriteImage("bmp", 255, "compare2.bmp");

                HHomMat2D hom = new HHomMat2D();
                if (score.Type != HTupleType.EMPTY)
                {
                    CanFind = true;
                    hom.VectorAngleToRigid(row.D, col.D, angle.D, OriginRow, OriginCol, 0);
                    HImage ref_scale = ReferenceImage.Rgb1ToGray();

                    HImage _image = CompareCrop.AffineTransImage(hom, "nearest_neighbor", "false").ReduceDomain(RefRegion).CropDomain();

                    HImage optical_image;

                    if (Params.Scale != 100)
                    {

                        ref_scale = ReferenceImage.Rgb1ToGray().ZoomImageFactor((double)Params.Scale / 100, (double)Params.Scale / 100, "bicubic");

                        _image = CompareCrop.AffineTransImage(hom, "nearest_neighbor", "false").ReduceDomain(RefRegion).CropDomain().ZoomImageFactor((double)Params.Scale / 100, (double)Params.Scale / 100, "bicubic");//.BinomialFilter(Params.Blur, Params.Blur);


                    }





                    bool result = OpticalFlowTrans(ref_scale, _image, out HImage vector_field, out optical_image);

                    //_image.WriteImage("bmp", 255, "compare.bmp");
                    //ref_scale.WriteImage("bmp", 255, "ref.bmp");
                    FindDifference(ref_scale.BinomialFilter(Params.Blur, Params.Blur), optical_image.BinomialFilter(Params.Blur, Params.Blur), vector_field, out HRegion diff, out HRegion diff_trans);
                    //HImage images = new HImage();
                    //images.GenImageConst("byte", 1500, 1500);
                    //images.PaintRegion(diff,128,)


                    HRegion a = diff_trans.AffineTransRegion(hom_scale.HomMat2dInvert(), "nearest_neighbor").AffineTransRegion(hom.HomMat2dInvert(), "nearest_neighbor").AffineTransRegion(hom1.HomMat2dInvert(), "nearest_neighbor");
                    diff_trans_region = a;

                    return diff;
                }
                else
                {
                    diff_trans_region = null;
                    CanFind = false;

                    return null;
                }



            }
            catch (Exception ex)
            {

                diff_trans_region = null;
                CanFind = false;
                return null;
            }


        }
        public void FindDifference(HImage ref_image, HImage compare_image, HImage vector_field, out HRegion diff, out HRegion diff_trans)
        {
            HRegion RedDiff, RedDiffTrans;

            ref_image.GetImageSize(out HTuple w, out HTuple h);
            HRegion region = new HRegion(10, 10, h - 10, w - 10);

            HImage diff_img = ref_image.AbsDiffImage(compare_image, 1.0).SubImage(MaskImage.ZoomImageFactor((double)Params.Scale / 100, (double)Params.Scale / 100, "bicubic"), 1.0, 0);


            HRegion out_region = new HRegion();
            ref_image.GetImageSize(out int w1, out int h1);
            out_region.GenRectangle1(0, 0.0, h1 - 1, w1 - 1);
            out_region = out_region.ErosionCircle(Math.Min(w1 / 100, h1 / 100) + 1.0);


            RedDiff = diff_img.ReduceDomain(out_region.ErosionRectangle1(10, 10)).Threshold(Params.Threshold, 255.0);
            RedDiff = RedDiff.Connection().SelectShape("area", "and", Params.ErrorSize * calib.ScaleX * calib.ScaleY, 100000000).Union1();
            HImage red_img_trans = diff_img.PaintRegion(RedDiff.DilationCircle(2.5), 255.0, "fill").UnwarpImageVectorField(vector_field).GrayErosionRect(3, 3).ReduceDomain(out_region); ;

            RedDiffTrans = red_img_trans.Threshold(Params.Threshold, 255.0);

            diff = RedDiff;
            diff_trans = RedDiffTrans.Connection().SelectShape("area", "and", Params.ErrorSize * calib.ScaleX * calib.ScaleY, 100000000).Union1();


        }
        public bool OpticalFlowTrans(HImage ref_gray_image, HImage compare_gray_image, out HImage vector_field, out HImage optical_image)
        {


            HImage r, g, b;

            r = ref_gray_image;
            ref_gray_image.GetImageSize(out int w1, out int h1);
            compare_gray_image.GetImageSize(out int w2, out int h2);

            vector_field = ref_gray_image.OpticalFlowMg(compare_gray_image, "fdrig", 0.8, 1, Params.Smoothness, 8, new HTuple("default_parameters", "warp_zoom_factor"), new HTuple("fast", Params.Accuracy / 100.0));
            TransformImageVectorField(compare_gray_image, vector_field, out HImage red);

            HImage VRow = vector_field.VectorFieldToReal(out HImage VCol);
            VRow = VRow.InvertImage();
            VCol = VCol.InvertImage();
            vector_field = VRow.RealToVectorField(VCol, "vector_field_relative");
            optical_image = red;
            return true;
        }


        public void TransformImageVectorField(HImage gray_image, HImage vector_field, out HImage red)
        {
            red = gray_image.UnwarpImageVectorField(vector_field);
        }

        public HRegion TransRegionVectorField(HRegion regions, HImage vector_field)
        {
            HRegion trans_region = new HRegion();
            trans_region.GenEmptyObj();

            regions.AreaCenter(out HTuple row, out HTuple collum);
            HImage VRow = vector_field.VectorFieldToReal(out HImage VCol);
            VRow = VRow.InvertImage();
            VCol = VCol.InvertImage();
            for (int i = 0; i < regions.CountObj(); i++)
            {
                HTuple r = VRow.GetGrayvalInterpolated(row[i].D, collum[i], "bilinear");
                HTuple c = VCol.GetGrayvalInterpolated(row[i].D, collum[i], "bilinear");
                HHomMat2D hom = new HHomMat2D();
                hom.VectorAngleToRigid(row[i], collum[i], new HTuple(0), r, c, 0);
                HRegion trans = regions[i + 1].AffineTransRegion(hom, "nearest_neighbor");
                trans_region = trans_region.ConcatObj(trans);

            }
            return trans_region;

        }
    }




    public class CheckDifferenceParameterGray : HelperMethods, INotifyPropertyChanged
    {

        #region Bindingbase
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }


        public void Save(HFile file)
        {

            SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {

            LoadParam(item, this);

        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        public CheckDifferenceParameterGray()
        {

        }


        private int pattern_score = 20;
        public int PatternScore
        {
            get
            {
                return pattern_score;
            }
            set
            {
                if (pattern_score != value)
                {
                    pattern_score = value;
                    RaisePropertyChanged("PatternScore");
                }
            }
        }


        public void CopyFrom(CheckDifferenceParameterGray gray)
        {

            this.Threshold = gray.Threshold;
            this.Accuracy = gray.Accuracy;
            this.Blur = gray.Blur;
            this.ErrorSize = gray.ErrorSize;
            this.MinScore = gray.MinScore;
            this.Preset = gray.Preset;
            this.Scale = gray.Scale;
            this.Smoothness = gray.Smoothness;
            this.Threshold = gray.Threshold;
            this.SearchExpand = gray.SearchExpand;
            this.PatternScore = gray.PatternScore;

        }


        private bool _manual_configure = false;
        public bool ManualConfigure
        {
            get
            {
                return _manual_configure;
            }
            set
            {
                if (_manual_configure != value)
                {
                    _manual_configure = value;
                    RaisePropertyChanged("ManualConfigure");
                }
            }
        }

        int _error_threshold = 60;
        public int Threshold
        {
            get
            {
                return _error_threshold;
            }
            set
            {
                if (_error_threshold != value)
                {
                    _error_threshold = value;
                    RaisePropertyChanged("Threshold");
                }
            }
        }


        private int _search_expand = 100;
        public int SearchExpand
        {
            get
            {
                return _search_expand;
            }
            set
            {
                if (_search_expand != value)
                {
                    _search_expand = value;
                    RaisePropertyChanged("SearchExpand");
                }
            }
        }

        double _error_size = 0.5;
        public double ErrorSize
        {
            get
            {
                return _error_size;
            }
            set
            {
                if (_error_size != value)
                {
                    _error_size = value;
                    RaisePropertyChanged("ErrorSize");
                }
            }
        }



        private string _preset = "Big";
        public string Preset
        {
            get
            {
                return _preset;
            }
            set
            {
                if (_preset != value)
                {
                    _preset = value;
                    RaisePropertyChanged("Preset");
                }
            }
        }



        int _compare_image_blur = 3;
        public int Blur
        {
            get
            {
                return _compare_image_blur;
            }
            set
            {
                if (_compare_image_blur != value)
                {
                    _compare_image_blur = value;
                    RaisePropertyChanged("Blur");
                }
            }
        }


        int scale = 100;
        public int Scale
        {
            get
            {
                return scale;
            }
            set
            {
                if (scale != value)
                {
                    scale = value;
                    RaisePropertyChanged("Scale");
                }
            }
        }


        int _optical_smoothness = 30;
        public int Smoothness
        {
            get
            {
                return _optical_smoothness;
            }
            set
            {
                if (_optical_smoothness != value)
                {
                    _optical_smoothness = value;
                    RaisePropertyChanged("Smoothness");
                }
            }
        }


        int _min_score = 60;
        public int MinScore
        {
            get
            {
                return _min_score;
            }
            set
            {
                if (_min_score != value)
                {
                    _min_score = value;
                    RaisePropertyChanged("MinScore");
                }
            }
        }
        int _accuracy = 40;
        public int Accuracy
        {
            get
            {
                return _accuracy;
            }
            set
            {
                if (_accuracy != value)
                {
                    _accuracy = value;
                    RaisePropertyChanged("Accuracy");
                }
            }
        }
    }
}
