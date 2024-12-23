using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HalconDotNet;
using System.ComponentModel;
using System.Collections.ObjectModel;
using NOVisionDesigner.UserControls;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Windows;

namespace NOVisionDesigner.UserControls
{
    public partial class FeaturesClassifier: INotifyPropertyChanged
    {
        bool _is_enabled_tool = true;
        public bool IsEnabledTool
        {
            get
            {
                return _is_enabled_tool;
            }
            set
            {
                if (_is_enabled_tool != value)
                {
                    _is_enabled_tool = value;
                    RaisePropertyChanged("IsEnabledTool");
                }
            }
        }
        public void ClearAll()
        {
            //tools.Clear();
        }
        string _name;
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
        public void Save(HFile file)
        {

            HelperMethods.SaveParam(file, this);

        }
       
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<ClassID> _lst_class = new ObservableCollection<ClassID>();
        public ObservableCollection<ClassID> LstClass
        {
            get
            {
                return _lst_class;
            }
            set
            {
                if (_lst_class != value)
                {
                    _lst_class = value;
                    RaisePropertyChanged("LstClass");
                }
            }
        }

        HClassTrainData train_datas;
        ObservableCollection<string> _lst_selected_features;
        public ObservableCollection<string> LstSelectedFeatures
        {
            get
            {
                return _lst_selected_features;
            }
            set
            {
                if (_lst_selected_features != value)
                {
                    _lst_selected_features = value;
                    RaisePropertyChanged("LstSelectedFeatures");
                }
            }
        }

        public HClassMlp MLP { get => mlp; set => mlp = value; }
        public HClassTrainData TrainData { get => train_datas; set => train_datas = value; }
        int selected_feature_length=0;
        List<string> lst_features_name = new List<string>() { "area", "width", "height", "ra", "rb", "phi", "roundness", "num_sides", "num_connected", "num_holes", "area_holes", "max_diameter", "orientation", "outer_radius", "inner_radius", "inner_width", "inner_height", "circularity", "compactness", "convexity", "rectangularity", "anisometry", "bulkiness", "struct_factor", "dist_mean", "dist_deviation", "euler_number", "rect2_phi", "rect2_len1", "rect2_len2", "contlength", "porosity", "gray_mean", "gray_deviation", "gray_plane_deviation", "gray_anisotropy", "gray_entropy", "gray_hor_proj", "gray_vert_proj", "gray_hor_proj_histo", "gray_vert_proj_histo", "grad_dir_histo", "edge_density", "edge_density_histogram", "edge_density_pyramid_2", "edge_density_pyramid_3", "edge_density_pyramid_4", "edge_density_histogram_pyramid_2", "edge_density_histogram_pyramid_3", "edge_density_histogram_pyramid_4", "cooc", "cooc_pyramid_2", "cooc_pyramid_3", "cooc_pyramid_4"};
        List<int> lst_features_length = new List<int>() { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 20, 20, 20, 20, 20, 1, 4, 2, 3, 4, 8, 12, 16, 4, 8, 12, 16};
        public FeaturesClassifier()
        {
            TrainData = new HClassTrainData(new HTuple(lst_features_length.ToArray()).TupleSum().I);
           // HTuple features_name = new HTuple("area", "width", "height", "ra", "rb", "phi", "roundness", "num_sides", "num_connected", "num_holes", "area_holes", "max_diameter", "orientation", "outer_radius", "inner_radius", "inner_width", "inner_height", "circularity", "compactness", "convexity", "rectangularity", "anisometry", "bulkiness", "struct_factor", "dist_mean", "dist_deviation", "euler_number", "rect2_phi", "rect2_len1", "rect2_len2", "contlength", "porosity", "cielab_mean", "cielab_dev", "hls_mean", "hls_dev", "rgb_mean", "rgb_dev");
           // HTuple feature_length = new HTuple(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 3, 3, 3, 3);
             TrainData.SetFeatureLengthsClassTrainData(new HTuple(lst_features_length.ToArray()),new HTuple(lst_features_name.ToArray()));
            //train_datas.SetFeatureLengthsClassTrainData(feature_length, features_name);
             LstSelectedFeatures = new ObservableCollection<string>(lst_features_name);
                
        }
        
        HClassMlp mlp;
        public void AutoSelectFeatures()
        {
            HTuple selected_features;
            HTuple score;
            
            MLP= TrainData.SelectFeatureSetMlp("greedy", new HTuple(), new HTuple(), out selected_features, out score);
            LstSelectedFeatures = new ObservableCollection<string>(selected_features.ToSArr());
            selected_feature_length = 0;
            for (int i = 0; i < LstSelectedFeatures.Count; i++)
            {
                selected_feature_length = selected_feature_length + lst_features_length[lst_features_name.IndexOf(LstSelectedFeatures[i])];
            }

        }
        public void AddTrainData(HRegion region,HImage image,int index)
        {
            HTuple result;
            calculate_features(region, image, new HTuple(lst_features_name.ToArray()), out result);
            TrainData.AddSampleClassTrainData("feature_column", result, index);
        }
        public ClassID Classify(HRegion region,HImage image)
        {
    
            HTuple result,confidence;
           
            calculate_features(region, image, new HTuple(_lst_selected_features.ToArray<string>()), out result);
           int result_class = MLP?.ClassifyClassMlp(result.TupleReal(),1, out confidence);
            return  _lst_class[result_class];
        }
        public List<ClassResult> ClassifyRegions(HRegion region,HImage image)
        {
            HTuple result, confidence;
            List<ClassResult> class_result = new List<ClassResult>();
            calculate_features(region, image, new HTuple(_lst_selected_features.ToArray<string>()), out result);
            HTuple result_class = new HTuple();
            ClassResult[] final_result = new ClassResult[LstClass.Count];
            for (int i=0;i<LstClass.Count;i++)
            {
                final_result[i] = new ClassResult(null,null,null,false,0) ;
            }
            for (int i=0;i<region.CountObj();i++)
            {
               result_class =  MLP?.ClassifyClassMlp(result.TupleSelectRange(i*selected_feature_length,(i+1)* selected_feature_length-1).TupleReal(), new HTuple(1), out confidence);
               if ( final_result[result_class].regions==null)
                {
                    final_result[result_class].regions = region[i+1];
                    final_result[result_class].TargetClass.Name = LstClass[result_class].Name;
                    final_result[result_class].Color = LstClass[result_class].DisplayColor;
                }
               else
                {
                    final_result[result_class].regions = final_result[result_class].regions.ConcatObj(region[i]);
                }
            }
            
            //return null;
            for (int i=0;i<final_result.Length;i++)
            {
                if (final_result[i].regions!=null)
                {
                    class_result.Add(final_result[i]);
                }
            }
            return class_result;
        }
        public void OnLoadComplete()
        {
            // throw new NotImplementedException();
        }

        public void Load(DeserializeFactory item)
        {
            //new HTuple("Name", Name).SerializeTuple().FwriteSerializedItem(file);
            //new HTuple("TrainData", TrainData.IsInitialized());
            //if (TrainData.IsInitialized())
            //{
            //    TrainData.SerializeClassTrainData().FwriteSerializedItem(file);
            //}
            //new HTuple("LstClass", LstClass.Count).SerializeTuple().FwriteSerializedItem(file);
            //foreach (ClassID _id in LstClass)
            //{
            //    _id.Save(file);
            //}
            //new HTuple("LstSelectedFeatures").SerializeTuple().FwriteSerializedItem(file);
            //new HTuple(LstSelectedFeatures.ToArray<string>()).SerializeTuple().FwriteSerializedItem(file);

            //new HTuple("Mlp", Mlp.IsInitialized()).SerializeTuple().FwriteSerializedItem(file);
            //if (mlp.IsInitialized())
            //{
            //    mlp.SerializeClassMlp().FwriteSerializedItem(file);
            //}
            HelperMethods.LoadParam(item, this);


            selected_feature_length = 0;
            for (int i = 0; i < LstSelectedFeatures.Count; i++)
            {
                selected_feature_length = selected_feature_length + lst_features_length[lst_features_name.IndexOf(LstSelectedFeatures[i])];
            }
        }

        public void Run()
        {
            // throw new NotImplementedException();
        }

        public void Remove()
        {
            // throw new NotImplementedException();

        }
       
    }
    public partial class FeaturesClassifier
    {
        #region HalconExports
        // Procedures 
        // Chapter: Classification / Misc
        // Short Description: Calculate edge density. 
        public void calc_feature_edge_density(HObject ho_Region, HObject ho_Image, out HTuple hv_Feature)
        {



            // Local iconic variables 

            HObject ho_RegionUnion, ho_ImageReduced, ho_EdgeAmplitude = null;

            // Local control variables 

            HTuple hv_Area = null, hv_Row = null, hv_Column = null;
            HTuple hv_Width = null, hv_Height = null, hv_AreaGray = new HTuple();
            HTuple hv_ZeroIndex = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
            hv_Feature = new HTuple();
            try
            {
                //
                //Calculate the edge density, i.e.
                //the ratio of the edge amplitudes to the area of the region.
                //
                ho_RegionUnion.Dispose();
                HOperatorSet.Union1(ho_Region, out ho_RegionUnion);
                ho_ImageReduced.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, ho_RegionUnion, out ho_ImageReduced);
                HOperatorSet.AreaCenter(ho_Region, out hv_Area, out hv_Row, out hv_Column);
                HOperatorSet.GetImageSize(ho_ImageReduced, out hv_Width, out hv_Height);
                if ((int)((new HTuple(hv_Width.TupleGreater(1))).TupleAnd(new HTuple(hv_Height.TupleGreater(
                    1)))) != 0)
                {
                    ho_EdgeAmplitude.Dispose();
                    HOperatorSet.SobelAmp(ho_ImageReduced, out ho_EdgeAmplitude, "sum_abs", 3);
                    HOperatorSet.AreaCenterGray(ho_Region, ho_EdgeAmplitude, out hv_AreaGray,
                        out hv_Row, out hv_Column);
                    hv_ZeroIndex = hv_Area.TupleFind(0);
                    if ((int)(new HTuple(hv_ZeroIndex.TupleNotEqual(-1))) != 0)
                    {
                        if (hv_Area == null)
                            hv_Area = new HTuple();
                        hv_Area[hv_ZeroIndex] = 1;
                        if (hv_AreaGray == null)
                            hv_AreaGray = new HTuple();
                        hv_AreaGray[hv_ZeroIndex] = 0;
                    }
                    hv_Feature = hv_AreaGray / hv_Area;
                }
                else
                {
                    hv_Feature = HTuple.TupleGenConst(new HTuple(hv_Area.TupleLength()), 0.0);
                }
                ho_RegionUnion.Dispose();
                ho_ImageReduced.Dispose();
                ho_EdgeAmplitude.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_RegionUnion.Dispose();
                ho_ImageReduced.Dispose();
                ho_EdgeAmplitude.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate gray-value projections of polar-transformed image regions. 
        public void calc_feature_polar_gray_proj(HObject ho_Region, HObject ho_Image,
            HTuple hv_Mode, HTuple hv_Width, HTuple hv_Height, out HTuple hv_Features)
        {




            // Local iconic variables 

            HObject ho_RegionSelected = null, ho_PolarTransImage = null;
            HObject ho_EdgeAmplitude = null, ho_ImageAbs = null;

            // Local control variables 

            HTuple hv_NumRegions = null, hv_Index = null;
            HTuple hv_Row = new HTuple(), hv_Column = new HTuple();
            HTuple hv_Radius = new HTuple(), hv_HorProjection = new HTuple();
            HTuple hv_VertProjection = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionSelected);
            HOperatorSet.GenEmptyObj(out ho_PolarTransImage);
            HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
            HOperatorSet.GenEmptyObj(out ho_ImageAbs);
            try
            {
                //
                //Calculate gray-value projections of
                //polar-transformed image regions.
                //
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                hv_Features = new HTuple();
                HTuple end_val6 = hv_NumRegions;
                HTuple step_val6 = 1;
                for (hv_Index = 1; hv_Index.Continue(end_val6, step_val6); hv_Index = hv_Index.TupleAdd(step_val6))
                {
                    ho_RegionSelected.Dispose();
                    HOperatorSet.SelectObj(ho_Region, out ho_RegionSelected, hv_Index);
                    HOperatorSet.SmallestCircle(ho_RegionSelected, out hv_Row, out hv_Column,
                        out hv_Radius);
                    ho_PolarTransImage.Dispose();
                    HOperatorSet.PolarTransImageExt(ho_Image, out ho_PolarTransImage, hv_Row,
                        hv_Column, 0, (new HTuple(360)).TupleRad(), 0, ((hv_Radius.TupleConcat(
                        1))).TupleMax(), hv_Width, hv_Height, "bilinear");
                    //
                    if ((int)(new HTuple(hv_Mode.TupleEqual("hor_gray"))) != 0)
                    {
                        HOperatorSet.GrayProjections(ho_PolarTransImage, ho_PolarTransImage, "simple",
                            out hv_HorProjection, out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_HorProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert_gray"))) != 0)
                    {
                        HOperatorSet.GrayProjections(ho_PolarTransImage, ho_PolarTransImage, "simple",
                            out hv_HorProjection, out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_VertProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("hor_sobel_amp"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "sum_abs",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_HorProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert_sobel_amp"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "sum_abs",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_VertProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("hor_sobel_x"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "x_binomial",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_HorProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert_sobel_x"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "x_binomial",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_VertProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("hor_sobel_y"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "y_binomial",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_HorProjection);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert_sobel_y"))) != 0)
                    {
                        ho_EdgeAmplitude.Dispose();
                        HOperatorSet.SobelAmp(ho_PolarTransImage, out ho_EdgeAmplitude, "y_binomial",
                            3);
                        ho_ImageAbs.Dispose();
                        HOperatorSet.AbsImage(ho_EdgeAmplitude, out ho_ImageAbs);
                        HOperatorSet.GrayProjections(ho_ImageAbs, ho_ImageAbs, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        hv_Features = hv_Features.TupleConcat(hv_VertProjection);
                    }
                    else
                    {
                        throw new HalconException(("Unknown Mode: " + hv_Mode) + " in calc_feature_polar_proj");
                    }
                }
                ho_RegionSelected.Dispose();
                ho_PolarTransImage.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_ImageAbs.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_RegionSelected.Dispose();
                ho_PolarTransImage.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_ImageAbs.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Auxiliary procedure for get_features. 
        public void append_names_or_groups_pyramid(HTuple hv_Mode, HTuple hv_Groups, HTuple hv_CurrentName,
            HTuple hv_Names, HTuple hv_NameRegExp, HTuple hv_AccumulatedResults, out HTuple hv_ExtendedResults)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_BelongsToGroup = new HTuple(), hv_TmpNames = new HTuple();
            HTuple hv_J = new HTuple(), hv_FirstOccurrence = new HTuple();
            HTuple hv_Names_COPY_INP_TMP = hv_Names.Clone();

            // Initialize local and output iconic variables 
            //
            //Auxiliary procedure used only by get_features and get_custom_features
            //
            hv_ExtendedResults = hv_AccumulatedResults.Clone();
            if ((int)(new HTuple(hv_Mode.TupleEqual("get_names"))) != 0)
            {
                hv_BelongsToGroup = (new HTuple(((hv_Groups.TupleFind(hv_CurrentName))).TupleNotEqual(
                    -1))).TupleOr(new HTuple(hv_CurrentName.TupleEqual("all")));
                if ((int)(hv_CurrentName.TupleRegexpTest(hv_NameRegExp)) != 0)
                {
                    hv_Names_COPY_INP_TMP = hv_CurrentName.Clone();
                }
                else if ((int)(hv_BelongsToGroup.TupleNot()) != 0)
                {
                    hv_Names_COPY_INP_TMP = new HTuple();
                }
                hv_TmpNames = new HTuple();
                for (hv_J = 0; (int)hv_J <= (int)((new HTuple(hv_Names_COPY_INP_TMP.TupleLength()
                    )) - 1); hv_J = (int)hv_J + 1)
                {
                    hv_FirstOccurrence = (new HTuple((new HTuple(hv_AccumulatedResults.TupleLength()
                        )).TupleEqual(0))).TupleOr(new HTuple(((hv_AccumulatedResults.TupleFind(
                        hv_Names_COPY_INP_TMP.TupleSelect(hv_J)))).TupleEqual(-1)));
                    if ((int)(hv_FirstOccurrence) != 0)
                    {
                        //Output in "get_names" mode is the name of the feature
                        hv_TmpNames = hv_TmpNames.TupleConcat(hv_Names_COPY_INP_TMP.TupleSelect(
                            hv_J));
                    }
                }
                hv_ExtendedResults = new HTuple();
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_TmpNames);
            }
            else if ((int)(new HTuple(hv_Mode.TupleEqual("get_groups"))) != 0)
            {
                hv_ExtendedResults = new HTuple();
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_Groups);
            }

            return;
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate a feature on different image pyramid levels. 
        public void calc_feature_pyramid(HObject ho_Region, HObject ho_Image, HTuple hv_FeatureName,
            HTuple hv_NumLevels, out HTuple hv_Feature)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_ImageZoom = null, ho_RegionZoom = null;

            // Local control variables 

            HTuple hv_Zoom = null, hv_NumRegions = null;
            HTuple hv_I = new HTuple(), hv_Features = new HTuple();
            HTuple hv_FeatureLength = new HTuple(), hv_Step = new HTuple();
            HTuple hv_Indices = new HTuple(), hv_J = new HTuple();
            HTuple hv_Start = new HTuple(), hv_End = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_ImageZoom);
            HOperatorSet.GenEmptyObj(out ho_RegionZoom);
            try
            {
                //
                //Calculate a feature for different pyramid levels
                //
                hv_Zoom = 0.5;
                hv_Feature = new HTuple();
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                if ((int)(new HTuple(hv_NumRegions.TupleGreater(0))) != 0)
                {
                    HTuple end_val7 = hv_NumLevels;
                    HTuple step_val7 = 1;
                    for (hv_I = 1; hv_I.Continue(end_val7, step_val7); hv_I = hv_I.TupleAdd(step_val7))
                    {
                        if ((int)(new HTuple(hv_I.TupleGreater(1))) != 0)
                        {
                            {
                                HObject ExpTmpOutVar_0;
                                HOperatorSet.ZoomImageFactor(ho_ImageZoom, out ExpTmpOutVar_0, hv_Zoom,
                                    hv_Zoom, "constant");
                                ho_ImageZoom.Dispose();
                                ho_ImageZoom = ExpTmpOutVar_0;
                            }
                            {
                                HObject ExpTmpOutVar_0;
                                HOperatorSet.ZoomRegion(ho_RegionZoom, out ExpTmpOutVar_0, hv_Zoom, hv_Zoom);
                                ho_RegionZoom.Dispose();
                                ho_RegionZoom = ExpTmpOutVar_0;
                            }
                            calculate_features(ho_RegionZoom, ho_ImageZoom, hv_FeatureName, out hv_Features);
                        }
                        else
                        {
                            ho_ImageZoom.Dispose();
                            HOperatorSet.CopyObj(ho_Image, out ho_ImageZoom, 1, 1);
                            ho_RegionZoom.Dispose();
                            HOperatorSet.CopyObj(ho_Region, out ho_RegionZoom, 1, hv_NumRegions);
                            calculate_features(ho_RegionZoom, ho_ImageZoom, hv_FeatureName, out hv_Features);
                            hv_FeatureLength = (new HTuple(hv_Features.TupleLength())) / hv_NumRegions;
                            hv_Step = hv_NumLevels * hv_FeatureLength;
                        }
                        hv_Indices = new HTuple();
                        HTuple end_val20 = hv_NumRegions - 1;
                        HTuple step_val20 = 1;
                        for (hv_J = 0; hv_J.Continue(end_val20, step_val20); hv_J = hv_J.TupleAdd(step_val20))
                        {
                            hv_Start = (hv_J * hv_Step) + ((hv_I - 1) * hv_FeatureLength);
                            hv_End = (hv_Start + hv_FeatureLength) - 1;
                            hv_Indices = hv_Indices.TupleConcat(HTuple.TupleGenSequence(hv_Start,
                                hv_End, 1));
                        }
                        if (hv_Feature == null)
                            hv_Feature = new HTuple();
                        hv_Feature[hv_Indices] = hv_Features;
                    }
                }
                ho_ImageZoom.Dispose();
                ho_RegionZoom.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_ImageZoom.Dispose();
                ho_RegionZoom.Dispose();

                throw HDevExpDefaultException;
            }
        }
        
        // Chapter: Classification / Misc
        // Short Description: Calculate edge density histogram feature. 
        public void calc_feature_edge_density_histogram(HObject ho_Region, HObject ho_Image,
            HTuple hv_NumBins, out HTuple hv_Feature)
        {




            // Local iconic variables 

            HObject ho_Channel1 = null, ho_EdgeAmplitude = null;
            HObject ho_RegionSelected = null;

            // Local control variables 

            HTuple hv_ImageWidth = null, hv_ImageHeight = null;
            HTuple hv_NumRegions = null, hv_J = new HTuple(), hv_Area = new HTuple();
            HTuple hv_Row = new HTuple(), hv_Column = new HTuple();
            HTuple hv_Histo = new HTuple(), hv_BinSize = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Channel1);
            HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
            HOperatorSet.GenEmptyObj(out ho_RegionSelected);
            try
            {
                //
                //Calculate the edge density histogram, i.e.
                //the ratio of the edge amplitude histogram to the area of the region.
                //
                hv_Feature = new HTuple();
                HOperatorSet.GetImageSize(ho_Image, out hv_ImageWidth, out hv_ImageHeight);
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                if ((int)((new HTuple(hv_ImageWidth.TupleGreater(1))).TupleAnd(new HTuple(hv_ImageHeight.TupleGreater(
                    1)))) != 0)
                {
                    ho_Channel1.Dispose();
                    HOperatorSet.AccessChannel(ho_Image, out ho_Channel1, 1);
                    ho_EdgeAmplitude.Dispose();
                    HOperatorSet.SobelAmp(ho_Channel1, out ho_EdgeAmplitude, "sum_abs", 3);
                    HTuple end_val10 = hv_NumRegions;
                    HTuple step_val10 = 1;
                    for (hv_J = 1; hv_J.Continue(end_val10, step_val10); hv_J = hv_J.TupleAdd(step_val10))
                    {
                        ho_RegionSelected.Dispose();
                        HOperatorSet.SelectObj(ho_Region, out ho_RegionSelected, hv_J);
                        HOperatorSet.AreaCenter(ho_RegionSelected, out hv_Area, out hv_Row, out hv_Column);
                        if ((int)(new HTuple(hv_Area.TupleGreater(0))) != 0)
                        {
                            HOperatorSet.GrayHistoRange(ho_RegionSelected, ho_EdgeAmplitude, 0, 255,
                                hv_NumBins, out hv_Histo, out hv_BinSize);
                            hv_Feature = hv_Feature.TupleConcat((hv_Histo.TupleReal()) / (hv_Histo.TupleSum()
                                ));
                        }
                        else
                        {
                            hv_Feature = ((hv_Feature.TupleConcat(1.0))).TupleConcat(HTuple.TupleGenConst(
                                hv_NumBins - 1, 0.0));
                        }
                    }
                }
                else
                {
                    hv_Feature = HTuple.TupleGenConst(hv_NumRegions * hv_NumBins, 0.0);
                }
                ho_Channel1.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_RegionSelected.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_Channel1.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_RegionSelected.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Auxiliary procedure for get_custom_features and get_features. 
        public void append_names_or_groups(HTuple hv_Mode, HTuple hv_Name, HTuple hv_Groups,
            HTuple hv_CurrentName, HTuple hv_AccumulatedResults, out HTuple hv_ExtendedResults)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_FirstOccurrence = new HTuple(), hv_BelongsToGroup = new HTuple();
            // Initialize local and output iconic variables 
            //
            //Auxiliary procedure used only by get_features and get_custom_features
            //
            hv_ExtendedResults = hv_AccumulatedResults.Clone();
            if ((int)(new HTuple(hv_Mode.TupleEqual("get_names"))) != 0)
            {
                hv_FirstOccurrence = (new HTuple((new HTuple(hv_AccumulatedResults.TupleLength()
                    )).TupleEqual(0))).TupleOr(new HTuple(((hv_AccumulatedResults.TupleFind(
                    hv_Name))).TupleEqual(-1)));
                hv_BelongsToGroup = (new HTuple(((((hv_Name.TupleConcat(hv_Groups))).TupleFind(
                    hv_CurrentName))).TupleNotEqual(-1))).TupleOr(new HTuple(hv_CurrentName.TupleEqual(
                    "all")));
                if ((int)(hv_FirstOccurrence.TupleAnd(hv_BelongsToGroup)) != 0)
                {
                    //Output in "get_names" mode is the name of the feature
                    hv_ExtendedResults = new HTuple();
                    hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                    hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_Name);
                }
            }
            else if ((int)(new HTuple(hv_Mode.TupleEqual("get_groups"))) != 0)
            {
                hv_ExtendedResults = new HTuple();
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_Groups);
            }

            return;
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate color intensity features. 
        public void calc_feature_color_intensity(HObject ho_Region, HObject ho_Image,
            HTuple hv_ColorSpace, HTuple hv_Mode, out HTuple hv_Feature)
        {




            // Local iconic variables 

            HObject ho_R, ho_G, ho_B, ho_I1 = null, ho_I2 = null;
            HObject ho_I3 = null;

            // Local control variables 

            HTuple hv_Channels = null, hv_Mean1 = new HTuple();
            HTuple hv_Deviation1 = new HTuple(), hv_Mean2 = new HTuple();
            HTuple hv_Deviation2 = new HTuple(), hv_Mean3 = new HTuple();
            HTuple hv_Deviation3 = new HTuple(), hv_Tmp1 = new HTuple();
            HTuple hv_Tmp2 = new HTuple(), hv_Tmp3 = new HTuple();
            HTuple hv_NumRegions = null, hv_Index = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_R);
            HOperatorSet.GenEmptyObj(out ho_G);
            HOperatorSet.GenEmptyObj(out ho_B);
            HOperatorSet.GenEmptyObj(out ho_I1);
            HOperatorSet.GenEmptyObj(out ho_I2);
            HOperatorSet.GenEmptyObj(out ho_I3);
            hv_Feature = new HTuple();
            try
            {
                //
                //Calculate color features
                //
                //Transform an RGB image into the given ColorSpace
                //and calculate the mean gray value and the deviation
                //for all three channels.
                //
                HOperatorSet.CountChannels(ho_Image, out hv_Channels);
                if ((int)(new HTuple(hv_Channels.TupleNotEqual(3))) != 0)
                {
                    throw new HalconException((((("Error when calculating feature " + hv_ColorSpace) + "_") + hv_Mode)).TupleConcat(
                        "Please use a 3-channel RGB image or remove color feature from the list."));
                }
                ho_R.Dispose(); ho_G.Dispose(); ho_B.Dispose();
                HOperatorSet.Decompose3(ho_Image, out ho_R, out ho_G, out ho_B);
                if ((int)(new HTuple(hv_ColorSpace.TupleEqual("rgb"))) != 0)
                {
                    HOperatorSet.Intensity(ho_Region, ho_R, out hv_Mean1, out hv_Deviation1);
                    HOperatorSet.Intensity(ho_Region, ho_G, out hv_Mean2, out hv_Deviation2);
                    HOperatorSet.Intensity(ho_Region, ho_B, out hv_Mean3, out hv_Deviation3);
                }
                else
                {
                    ho_I1.Dispose(); ho_I2.Dispose(); ho_I3.Dispose();
                    HOperatorSet.TransFromRgb(ho_R, ho_G, ho_B, out ho_I1, out ho_I2, out ho_I3,
                        hv_ColorSpace);
                    HOperatorSet.Intensity(ho_Region, ho_I1, out hv_Mean1, out hv_Deviation1);
                    HOperatorSet.Intensity(ho_Region, ho_I2, out hv_Mean2, out hv_Deviation2);
                    HOperatorSet.Intensity(ho_Region, ho_I3, out hv_Mean3, out hv_Deviation3);
                }
                if ((int)(new HTuple(hv_Mode.TupleEqual("mean"))) != 0)
                {
                    hv_Tmp1 = hv_Mean1.Clone();
                    hv_Tmp2 = hv_Mean2.Clone();
                    hv_Tmp3 = hv_Mean3.Clone();
                }
                else if ((int)(new HTuple(hv_Mode.TupleEqual("deviation"))) != 0)
                {
                    hv_Tmp1 = hv_Deviation1.Clone();
                    hv_Tmp2 = hv_Deviation2.Clone();
                    hv_Tmp3 = hv_Deviation3.Clone();
                }
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                if ((int)(new HTuple(hv_NumRegions.TupleGreater(0))) != 0)
                {
                    hv_Index = HTuple.TupleGenSequence(0, (3 * hv_NumRegions) - 1, 3);
                    if (hv_Feature == null)
                        hv_Feature = new HTuple();
                    hv_Feature[hv_Index] = hv_Tmp1;
                    if (hv_Feature == null)
                        hv_Feature = new HTuple();
                    hv_Feature[1 + hv_Index] = hv_Tmp2;
                    if (hv_Feature == null)
                        hv_Feature = new HTuple();
                    hv_Feature[2 + hv_Index] = hv_Tmp3;
                }
                else
                {
                    hv_Feature = new HTuple();
                }
                ho_R.Dispose();
                ho_G.Dispose();
                ho_B.Dispose();
                ho_I1.Dispose();
                ho_I2.Dispose();
                ho_I3.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_R.Dispose();
                ho_G.Dispose();
                ho_B.Dispose();
                ho_I1.Dispose();
                ho_I2.Dispose();
                ho_I3.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate gray-value projections and their histograms. 
        public void calc_feature_gray_proj(HObject ho_Region, HObject ho_Image, HTuple hv_Mode,
            HTuple hv_Size, out HTuple hv_Feature)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_RegionTmp = null, ho_RegionMoved = null;
            HObject ho_ImageTmp = null;

            // Local control variables 

            HTuple hv_NumRegions = null, hv_Index = null;
            HTuple hv_RowsTmp = new HTuple(), hv_ColumnsTmp = new HTuple();
            HTuple hv_HorProjectionFilledUp = new HTuple(), hv_VertProjectionFilledUp = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Column1 = new HTuple();
            HTuple hv_Row2 = new HTuple(), hv_Column2 = new HTuple();
            HTuple hv_ScaleHeight = new HTuple(), hv_ScaleWidth = new HTuple();
            HTuple hv_HorProjection = new HTuple(), hv_VertProjection = new HTuple();
            HTuple hv_HorProjectionFilledUpFront = new HTuple(), hv_VertProjectionFilledUpFront = new HTuple();
            HTuple hv_Histo = new HTuple(), hv_BinSize = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionTmp);
            HOperatorSet.GenEmptyObj(out ho_RegionMoved);
            HOperatorSet.GenEmptyObj(out ho_ImageTmp);
            try
            {
                //
                //Calculate gray-value projections and their histograms
                //
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                hv_Feature = new HTuple();
                //
                HTuple end_val6 = hv_NumRegions;
                HTuple step_val6 = 1;
                for (hv_Index = 1; hv_Index.Continue(end_val6, step_val6); hv_Index = hv_Index.TupleAdd(step_val6))
                {
                    ho_RegionTmp.Dispose();
                    HOperatorSet.SelectObj(ho_Region, out ho_RegionTmp, hv_Index);
                    //Test empty region
                    HOperatorSet.GetRegionPoints(ho_RegionTmp, out hv_RowsTmp, out hv_ColumnsTmp);
                    if ((int)(new HTuple((new HTuple(hv_RowsTmp.TupleLength())).TupleEqual(0))) != 0)
                    {
                        hv_HorProjectionFilledUp = HTuple.TupleGenConst(hv_Size, -1.0);
                        hv_VertProjectionFilledUp = HTuple.TupleGenConst(hv_Size, -1.0);
                    }
                    else
                    {
                        //Zoom image and region to Size x Size pixels
                        HOperatorSet.SmallestRectangle1(ho_RegionTmp, out hv_Row1, out hv_Column1,
                            out hv_Row2, out hv_Column2);
                        ho_RegionMoved.Dispose();
                        HOperatorSet.MoveRegion(ho_RegionTmp, out ho_RegionMoved, -hv_Row1, -hv_Column1);
                        ho_ImageTmp.Dispose();
                        HOperatorSet.CropRectangle1(ho_Image, out ho_ImageTmp, hv_Row1, hv_Column1,
                            hv_Row2, hv_Column2);
                        hv_ScaleHeight = (hv_Size.TupleReal()) / ((hv_Row2 - hv_Row1) + 1);
                        hv_ScaleWidth = (hv_Size.TupleReal()) / ((hv_Column2 - hv_Column1) + 1);
                        {
                            HObject ExpTmpOutVar_0;
                            HOperatorSet.ZoomImageFactor(ho_ImageTmp, out ExpTmpOutVar_0, hv_ScaleWidth,
                                hv_ScaleHeight, "constant");
                            ho_ImageTmp.Dispose();
                            ho_ImageTmp = ExpTmpOutVar_0;
                        }
                        ho_RegionTmp.Dispose();
                        HOperatorSet.ZoomRegion(ho_RegionMoved, out ho_RegionTmp, hv_ScaleWidth,
                            hv_ScaleHeight);
                        //Calculate gray value projection
                        HOperatorSet.GrayProjections(ho_RegionTmp, ho_ImageTmp, "simple", out hv_HorProjection,
                            out hv_VertProjection);
                        //Fill up projection in case the zoomed region is smaller than
                        //Size x Size pixels due to interpolation effects
                        HOperatorSet.SmallestRectangle1(ho_RegionTmp, out hv_Row1, out hv_Column1,
                            out hv_Row2, out hv_Column2);
                        hv_HorProjectionFilledUpFront = new HTuple();
                        hv_HorProjectionFilledUpFront = hv_HorProjectionFilledUpFront.TupleConcat(HTuple.TupleGenConst(
                            (new HTuple(0)).TupleMax2(hv_Row1), -1.0));
                        hv_HorProjectionFilledUpFront = hv_HorProjectionFilledUpFront.TupleConcat(hv_HorProjection);
                        hv_HorProjectionFilledUp = new HTuple();
                        hv_HorProjectionFilledUp = hv_HorProjectionFilledUp.TupleConcat(hv_HorProjectionFilledUpFront);
                        hv_HorProjectionFilledUp = hv_HorProjectionFilledUp.TupleConcat(HTuple.TupleGenConst(
                            hv_Size - (new HTuple(hv_HorProjectionFilledUpFront.TupleLength())), -1.0));
                        hv_VertProjectionFilledUpFront = new HTuple();
                        hv_VertProjectionFilledUpFront = hv_VertProjectionFilledUpFront.TupleConcat(HTuple.TupleGenConst(
                            (new HTuple(0)).TupleMax2(hv_Column1), -1.0));
                        hv_VertProjectionFilledUpFront = hv_VertProjectionFilledUpFront.TupleConcat(hv_VertProjection);
                        hv_VertProjectionFilledUp = new HTuple();
                        hv_VertProjectionFilledUp = hv_VertProjectionFilledUp.TupleConcat(hv_VertProjectionFilledUpFront);
                        hv_VertProjectionFilledUp = hv_VertProjectionFilledUp.TupleConcat(HTuple.TupleGenConst(
                            hv_Size - (new HTuple(hv_VertProjectionFilledUpFront.TupleLength())),
                            -1.0));
                    }
                    if ((int)(new HTuple(hv_Mode.TupleEqual("hor"))) != 0)
                    {
                        hv_Feature = hv_Feature.TupleConcat(hv_HorProjectionFilledUp);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert"))) != 0)
                    {
                        hv_Feature = hv_Feature.TupleConcat(hv_VertProjectionFilledUp);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("hor_histo"))) != 0)
                    {
                        HOperatorSet.TupleHistoRange(hv_HorProjectionFilledUp, 0, 255, hv_Size,
                            out hv_Histo, out hv_BinSize);
                        hv_Feature = hv_Feature.TupleConcat(hv_Histo);
                    }
                    else if ((int)(new HTuple(hv_Mode.TupleEqual("vert_histo"))) != 0)
                    {
                        HOperatorSet.TupleHistoRange(hv_VertProjectionFilledUp, 0, 255, hv_Size,
                            out hv_Histo, out hv_BinSize);
                        hv_Feature = hv_Feature.TupleConcat(hv_Histo);
                    }
                }
                ho_RegionTmp.Dispose();
                ho_RegionMoved.Dispose();
                ho_ImageTmp.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_RegionTmp.Dispose();
                ho_RegionMoved.Dispose();
                ho_ImageTmp.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: This procedure contains all relevant information about the supported features. 
        public void get_features(HObject ho_Region, HObject ho_Image, HTuple hv_Namelist,
            HTuple hv_Mode, out HTuple hv_Output)
        {




            // Local iconic variables 

            // Local control variables 

            HTuple hv_EmptyRegionResult = null, hv_AccumulatedResults = null;
            HTuple hv_CustomResults = null, hv_NumRegions = null, hv_ImageWidth = null;
            HTuple hv_ImageHeight = null, hv_I = null, hv_CurrentName = new HTuple();
            HTuple hv_Name = new HTuple(), hv_Groups = new HTuple();
            HTuple hv_Feature = new HTuple(), hv_ExpDefaultCtrlDummyVar = new HTuple();
            HTuple hv_ExtendedResults = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Ra = new HTuple();
            HTuple hv_Rb = new HTuple(), hv_Phi = new HTuple(), hv_Distance = new HTuple();
            HTuple hv_Sigma = new HTuple(), hv_Roundness = new HTuple();
            HTuple hv_Sides = new HTuple(), hv_NumConnected = new HTuple();
            HTuple hv_NumHoles = new HTuple(), hv_Diameter = new HTuple();
            HTuple hv_Row = new HTuple(), hv_Column = new HTuple();
            HTuple hv_Anisometry = new HTuple(), hv_Bulkiness = new HTuple();
            HTuple hv_StructureFactor = new HTuple(), hv_Length1 = new HTuple();
            HTuple hv_Length2 = new HTuple(), hv_ContLength = new HTuple();
            HTuple hv_AreaHoles = new HTuple(), hv_Area = new HTuple();
            HTuple hv_Min = new HTuple(), hv_Max = new HTuple(), hv_Range = new HTuple();
            HTuple hv_Mean = new HTuple(), hv_Deviation = new HTuple();
            HTuple hv_Entropy = new HTuple(), hv_Anisotropy = new HTuple();
            HTuple hv_Size = new HTuple(), hv_NumBins = new HTuple();
            HTuple hv_NameRegExp = new HTuple(), hv_Names = new HTuple();
            HTuple hv_NumPyramids = new HTuple(), hv_Energy = new HTuple();
            HTuple hv_Correlation = new HTuple(), hv_Homogeneity = new HTuple();
            HTuple hv_Contrast = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Projection = new HTuple(), hv_Start = new HTuple();
            HTuple hv_Histo = new HTuple(), hv_BinSize = new HTuple();
            // Initialize local and output iconic variables 
            //*********************************************************
            //Feature procedure
            //Contains the names, properties and calculation of
            //all supproted features.
            //It consists of similar blocks for each feature.
            //
            //If you like to add your own features, please use
            //the external procedure get_custom_features.hdvp
            //in the HALCON procedures/templates directory.
            //*********************************************************
            //
            //Insert location of your custom procedure here
            //
            HOperatorSet.GetSystem("empty_region_result", out hv_EmptyRegionResult);
            HOperatorSet.SetSystem("empty_region_result", "true");
            hv_AccumulatedResults = new HTuple();
            hv_CustomResults = new HTuple();
            HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
            HOperatorSet.GetImageSize(ho_Image, out hv_ImageWidth, out hv_ImageHeight);
            //
            for (hv_I = 0; (int)hv_I <= (int)((new HTuple(hv_Namelist.TupleLength())) - 1); hv_I = (int)hv_I + 1)
            {
                hv_CurrentName = hv_Namelist.TupleSelect(hv_I);
                //
                get_custom_features(ho_Region, ho_Image, hv_CurrentName, hv_Mode, out hv_CustomResults);
                hv_AccumulatedResults = hv_AccumulatedResults.TupleConcat(hv_CustomResults);
                //
                //
                //************************************
                //HALCON REGION FEATURES
                //************************************
                //
                //************************************
                //BASIC
                //************************************
                //** area ***
                hv_Name = "area";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.AreaCenter(ho_Region, out hv_Feature, out hv_ExpDefaultCtrlDummyVar,
                        out hv_ExpDefaultCtrlDummyVar);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** width ***
                hv_Name = "width";
                hv_Groups = "region";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestRectangle1(ho_Region, out hv_Row1, out hv_Column1, out hv_Row2,
                        out hv_Column2);
                    hv_Feature = (hv_Column2 - hv_Column1) + 1;
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** height ***
                hv_Name = "height";
                hv_Groups = "region";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestRectangle1(ho_Region, out hv_Row1, out hv_Column1, out hv_Row2,
                        out hv_Column2);
                    hv_Feature = (hv_Row2 - hv_Row1) + 1;
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** ra ***
                hv_Name = "ra";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxis(ho_Region, out hv_Ra, out hv_Rb, out hv_Phi);
                    hv_Feature = hv_Ra.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** rb ***
                hv_Name = "rb";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxis(ho_Region, out hv_Ra, out hv_Rb, out hv_Phi);
                    hv_Feature = hv_Rb.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** phi ***
                hv_Name = "phi";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxis(ho_Region, out hv_Ra, out hv_Rb, out hv_Phi);
                    hv_Feature = hv_Phi.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** roundness ***
                hv_Name = "roundness";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Roundness(ho_Region, out hv_Distance, out hv_Sigma, out hv_Roundness,
                        out hv_Sides);
                    hv_Feature = hv_Roundness.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** num_sides ***
                hv_Name = "num_sides";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Roundness(ho_Region, out hv_Distance, out hv_Sigma, out hv_Roundness,
                        out hv_Sides);
                    hv_Feature = hv_Sides.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** num_connected ***
                hv_Name = "num_connected";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.ConnectAndHoles(ho_Region, out hv_NumConnected, out hv_NumHoles);
                    hv_Feature = hv_NumConnected.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** num_holes ***
                hv_Name = "num_holes";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.ConnectAndHoles(ho_Region, out hv_NumConnected, out hv_NumHoles);
                    hv_Feature = hv_NumHoles.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** area_holes ***
                hv_Name = "area_holes";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.AreaHoles(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** max_diameter ***
                hv_Name = "max_diameter";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.DiameterRegion(ho_Region, out hv_Row1, out hv_Column1, out hv_Row2,
                        out hv_Column2, out hv_Diameter);
                    hv_Feature = hv_Diameter.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** orientation ***
                hv_Name = "orientation";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.OrientationRegion(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //SHAPE
                //************************************
                //
                //************************************
                //** outer_radius ***
                hv_Name = "outer_radius";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestCircle(ho_Region, out hv_Row, out hv_Column, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** inner_radius ***
                hv_Name = "inner_radius";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.InnerCircle(ho_Region, out hv_Row, out hv_Column, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** inner_width ***
                hv_Name = "inner_width";
                hv_Groups = "region";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.InnerRectangle1(ho_Region, out hv_Row1, out hv_Column1, out hv_Row2,
                        out hv_Column2);
                    hv_Feature = (hv_Column2 - hv_Column1) + 1;
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** inner_height ***
                hv_Name = "inner_height";
                hv_Groups = "region";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.InnerRectangle1(ho_Region, out hv_Row1, out hv_Column1, out hv_Row2,
                        out hv_Column2);
                    hv_Feature = (hv_Row2 - hv_Row1) + 1;
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** circularity ***
                hv_Name = "circularity";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Circularity(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** compactness ***
                hv_Name = "compactness";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Compactness(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** convexity ***
                hv_Name = "convexity";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Convexity(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** rectangularity ***
                hv_Name = "rectangularity";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Rectangularity(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** anisometry ***
                hv_Name = "anisometry";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Eccentricity(ho_Region, out hv_Anisometry, out hv_Bulkiness,
                        out hv_StructureFactor);
                    hv_Feature = hv_Anisometry.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** bulkiness ***
                hv_Name = "bulkiness";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Eccentricity(ho_Region, out hv_Anisometry, out hv_Bulkiness,
                        out hv_StructureFactor);
                    hv_Feature = hv_Bulkiness.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** struct_factor ***
                hv_Name = "struct_factor";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Eccentricity(ho_Region, out hv_Anisometry, out hv_Bulkiness,
                        out hv_StructureFactor);
                    hv_Feature = hv_StructureFactor.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** dist_mean ***
                hv_Name = "dist_mean";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Roundness(ho_Region, out hv_Distance, out hv_Sigma, out hv_Roundness,
                        out hv_Sides);
                    hv_Feature = hv_Distance.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** dist_deviation ***
                hv_Name = "dist_deviation";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Roundness(ho_Region, out hv_Distance, out hv_Sigma, out hv_Roundness,
                        out hv_Sides);
                    hv_Feature = hv_Sigma.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** euler_number ***
                hv_Name = "euler_number";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EulerNumber(ho_Region, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** rect2_phi ***
                hv_Name = "rect2_phi";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestRectangle2(ho_Region, out hv_Row, out hv_Column, out hv_Phi,
                        out hv_Length1, out hv_Length2);
                    hv_Feature = hv_Phi.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** rect2_len1 ***
                hv_Name = "rect2_len1";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestRectangle2(ho_Region, out hv_Row, out hv_Column, out hv_Phi,
                        out hv_Length1, out hv_Length2);
                    hv_Feature = hv_Length1.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** rect2_len2 ***
                hv_Name = "rect2_len2";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.SmallestRectangle2(ho_Region, out hv_Row, out hv_Column, out hv_Phi,
                        out hv_Length1, out hv_Length2);
                    hv_Feature = hv_Length2.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** contlength ***
                hv_Name = "contlength";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Contlength(ho_Region, out hv_ContLength);
                    hv_Feature = hv_ContLength.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //REGION FEATURES
                //************************************
                //MISC
                //************************************
                //** porosity ***
                hv_Name = "porosity";
                hv_Groups = new HTuple();
                hv_Groups[0] = "region";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.AreaHoles(ho_Region, out hv_AreaHoles);
                    HOperatorSet.AreaCenter(ho_Region, out hv_Area, out hv_Row, out hv_Column);
                    if ((int)(new HTuple(hv_Area.TupleEqual(0))) != 0)
                    {
                        hv_Feature = 0.0;
                    }
                    else
                    {
                        hv_Feature = (hv_AreaHoles.TupleReal()) / (hv_Area + hv_AreaHoles);
                    }
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //HALCON GRAY VALUE FEATURES
                //************************************
                //BASIC
                //************************************
                //
                //** gray_area ***
                hv_Name = "gray_area";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.AreaCenterGray(ho_Region, ho_Image, out hv_Area, out hv_Row,
                        out hv_Column);
                    hv_Feature = hv_Area.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_ra ***
                hv_Name = "gray_ra";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxisGray(ho_Region, ho_Image, out hv_Ra, out hv_Rb,
                        out hv_Phi);
                    hv_Feature = hv_Ra.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_rb ***
                hv_Name = "gray_rb";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxisGray(ho_Region, ho_Image, out hv_Ra, out hv_Rb,
                        out hv_Phi);
                    hv_Feature = hv_Rb.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_phi ***
                hv_Name = "gray_phi";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EllipticAxisGray(ho_Region, ho_Image, out hv_Ra, out hv_Rb,
                        out hv_Phi);
                    hv_Feature = hv_Phi.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_min ***
                hv_Name = "gray_min";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.MinMaxGray(ho_Region, ho_Image, 0, out hv_Min, out hv_Max, out hv_Range);
                    hv_Feature = hv_Min.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_max ***
                hv_Name = "gray_max";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.MinMaxGray(ho_Region, ho_Image, 0, out hv_Min, out hv_Max, out hv_Range);
                    hv_Feature = hv_Max.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_range ***
                hv_Name = "gray_range";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.MinMaxGray(ho_Region, ho_Image, 0, out hv_Min, out hv_Max, out hv_Range);
                    hv_Feature = hv_Range.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //TEXTURE
                //************************************
                //
                //************************************
                //** gray_mean ***
                hv_Name = "gray_mean";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Intensity(ho_Region, ho_Image, out hv_Mean, out hv_Deviation);
                    hv_Feature = hv_Mean.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_deviation ***
                hv_Name = "gray_deviation";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.Intensity(ho_Region, ho_Image, out hv_Mean, out hv_Deviation);
                    hv_Feature = hv_Deviation.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_plane_deviation ***
                hv_Name = "gray_plane_deviation";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.PlaneDeviation(ho_Region, ho_Image, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_anisotropy ***
                hv_Name = "gray_anisotropy";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EntropyGray(ho_Region, ho_Image, out hv_Entropy, out hv_Anisotropy);
                    hv_Feature = hv_Anisotropy.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_entropy ***
                hv_Name = "gray_entropy";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    HOperatorSet.EntropyGray(ho_Region, ho_Image, out hv_Entropy, out hv_Anisotropy);
                    hv_Feature = hv_Entropy.Clone();
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_hor_proj ***
                hv_Name = "gray_hor_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Size = 20;
                    calc_feature_gray_proj(ho_Region, ho_Image, "hor", hv_Size, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_vert_proj ***
                hv_Name = "gray_vert_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Size = 20;
                    calc_feature_gray_proj(ho_Region, ho_Image, "vert", hv_Size, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_hor_proj_histo ***
                hv_Name = "gray_hor_proj_histo";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Size = 20;
                    calc_feature_gray_proj(ho_Region, ho_Image, "hor_histo", hv_Size, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** gray_vert_proj_histo ***
                hv_Name = "gray_vert_proj_histo";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Size = 20;
                    calc_feature_gray_proj(ho_Region, ho_Image, "vert_histo", hv_Size, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** grad_dir_histo ***
                hv_Name = "grad_dir_histo";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_NumBins = 20;
                    calc_feature_grad_dir_histo(ho_Region, ho_Image, hv_NumBins, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** edge_density ***
                hv_Name = "edge_density";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_edge_density(ho_Region, ho_Image, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** edge_density_histogram ***
                hv_Name = "edge_density_histogram";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_NumBins = 4;
                    calc_feature_edge_density_histogram(ho_Region, ho_Image, hv_NumBins, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** edge_density_pyramid ***
                hv_NameRegExp = "edge_density_pyramid_([234])";
                hv_Names = new HTuple("edge_density_pyramid_") + HTuple.TupleGenSequence(2, 4, 1);
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(hv_CurrentName.TupleRegexpTest(hv_NameRegExp)) != 0)
                {
                    //** Calculate feature ***
                    hv_NumPyramids = ((hv_CurrentName.TupleRegexpMatch(hv_NameRegExp))).TupleNumber()
                        ;
                    calc_feature_pyramid(ho_Region, ho_Image, "edge_density", hv_NumPyramids,
                        out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups_pyramid(hv_Mode, hv_Groups, hv_CurrentName, hv_Names,
                    hv_NameRegExp, hv_AccumulatedResults, out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** edge_density_histogram_pyramid ***
                hv_NameRegExp = "edge_density_histogram_pyramid_([234])";
                hv_Names = new HTuple("edge_density_histogram_pyramid_") + HTuple.TupleGenSequence(
                    2, 4, 1);
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                hv_Groups[2] = "rot_invar";
                hv_Groups[3] = "scale_invar";
                //****************
                if ((int)(hv_CurrentName.TupleRegexpTest(hv_NameRegExp)) != 0)
                {
                    //** Calculate feature ***
                    hv_NumPyramids = ((hv_CurrentName.TupleRegexpMatch(hv_NameRegExp))).TupleNumber()
                        ;
                    calc_feature_pyramid(ho_Region, ho_Image, "edge_density_histogram", hv_NumPyramids,
                        out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups_pyramid(hv_Mode, hv_Groups, hv_CurrentName, hv_Names,
                    hv_NameRegExp, hv_AccumulatedResults, out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //** cooc ***
                hv_Name = "cooc";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                //****************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Feature = new HTuple();
                    HOperatorSet.CoocFeatureImage(ho_Region, ho_Image, 6, 0, out hv_Energy, out hv_Correlation,
                        out hv_Homogeneity, out hv_Contrast);
                    if ((int)(new HTuple(hv_NumRegions.TupleGreater(0))) != 0)
                    {
                        hv_Index = HTuple.TupleGenSequence(0, (4 * hv_NumRegions) - 1, 4);
                        if (hv_Feature == null)
                            hv_Feature = new HTuple();
                        hv_Feature[hv_Index] = hv_Energy;
                        if (hv_Feature == null)
                            hv_Feature = new HTuple();
                        hv_Feature[1 + hv_Index] = hv_Correlation;
                        if (hv_Feature == null)
                            hv_Feature = new HTuple();
                        hv_Feature[2 + hv_Index] = hv_Homogeneity;
                        if (hv_Feature == null)
                            hv_Feature = new HTuple();
                        hv_Feature[3 + hv_Index] = hv_Contrast;
                    }
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** cooc_pyramid ***
                hv_NameRegExp = "cooc_pyramid_([234])";
                hv_Names = new HTuple("cooc_pyramid_") + HTuple.TupleGenSequence(2, 4, 1);
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "texture";
                //****************
                if ((int)(hv_CurrentName.TupleRegexpTest(hv_NameRegExp)) != 0)
                {
                    //** Calculate feature ***
                    hv_NumPyramids = ((hv_CurrentName.TupleRegexpMatch(hv_NameRegExp))).TupleNumber()
                        ;
                    calc_feature_pyramid(ho_Region, ho_Image, "cooc", hv_NumPyramids, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups_pyramid(hv_Mode, hv_Groups, hv_CurrentName, hv_Names,
                    hv_NameRegExp, hv_AccumulatedResults, out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //
                //************************************
                //
                //************************************
                //POLAR TRANSFORM FEATURES
                //************************************
                //
                //************************************
                //** polar_gray_proj ***
                hv_Name = "polar_gray_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Width = 100;
                    hv_Height = 40;
                    calc_feature_polar_gray_proj(ho_Region, ho_Image, "hor_gray", hv_Width, hv_Height,
                        out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** polar_grad_proj ***
                hv_Name = "polar_grad_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Width = 100;
                    hv_Height = 40;
                    calc_feature_polar_gray_proj(ho_Region, ho_Image, "hor_sobel_amp", hv_Width,
                        hv_Height, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** polar_grad_x_proj ***
                hv_Name = "polar_grad_x_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Width = 100;
                    hv_Height = 40;
                    calc_feature_polar_gray_proj(ho_Region, ho_Image, "hor_sobel_x", hv_Width,
                        hv_Height, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** polar_grad_y_proj ***
                hv_Name = "polar_grad_y_proj";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Width = 100;
                    hv_Height = 40;
                    calc_feature_polar_gray_proj(ho_Region, ho_Image, "hor_sobel_y", hv_Width,
                        hv_Height, out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** polar_gray_proj_histo ***
                hv_Name = "polar_gray_proj_histo";
                hv_Groups = new HTuple();
                hv_Groups[0] = "gray";
                hv_Groups[1] = "rot_invar";
                hv_Groups[2] = "scale_invar";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    hv_Width = 100;
                    hv_Height = 40;
                    calc_feature_polar_gray_proj(ho_Region, ho_Image, "vert_gray", hv_Width,
                        hv_Height, out hv_Projection);
                    hv_NumBins = 20;
                    hv_Feature = new HTuple();
                    HTuple end_val1093 = hv_NumRegions;
                    HTuple step_val1093 = 1;
                    for (hv_Index = 1; hv_Index.Continue(end_val1093, step_val1093); hv_Index = hv_Index.TupleAdd(step_val1093))
                    {
                        hv_Start = (hv_Index - 1) * hv_Width;
                        HOperatorSet.TupleHistoRange(hv_Projection.TupleSelectRange(hv_Start, (hv_Start + hv_Width) - 1),
                            0, 255, hv_NumBins, out hv_Histo, out hv_BinSize);
                        hv_Feature = hv_Feature.TupleConcat(hv_Histo);
                    }
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //COLOR FEATURES
                //************************************
                //
                //************************************
                //** cielab_mean ***
                hv_Name = "cielab_mean";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "cielab", "mean", out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** cielab_dev ***
                hv_Name = "cielab_dev";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "cielab", "deviation",
                        out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** hls_mean ***
                hv_Name = "hls_mean";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "hls", "mean", out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** hls_dev ***
                hv_Name = "hls_dev";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "hls", "deviation", out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** rgb_mean ***
                hv_Name = "rgb_mean";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "rgb", "mean", out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
                //************************************
                //
                //************************************
                //** rgb_dev ***
                hv_Name = "rgb_dev";
                hv_Groups = "color";
                //*************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //** Calculate feature ***
                    calc_feature_color_intensity(ho_Region, ho_Image, "rgb", "deviation", out hv_Feature);
                    //*************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_AccumulatedResults, out hv_ExtendedResults);
                    hv_AccumulatedResults = hv_ExtendedResults.Clone();
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_AccumulatedResults,
                    out hv_ExtendedResults);
                hv_AccumulatedResults = hv_ExtendedResults.Clone();
            }
            hv_Output = hv_AccumulatedResults.Clone();
            HOperatorSet.SetSystem("empty_region_result", hv_EmptyRegionResult);

            return;
        }

        // Chapter: Classification / Misc
        // Short Description: Auxiliary procedure for get_custom_features and get_features. 
        public void append_length_or_values(HTuple hv_Mode, HTuple hv_Feature, HTuple hv_AccumulatedResults,
            out HTuple hv_ExtendedResults)
        {



            // Local iconic variables 
            // Initialize local and output iconic variables 
            hv_ExtendedResults = new HTuple();
            //
            //Auxiliary procedure used only by get_features and get_custom_features
            //
            if ((int)(new HTuple(hv_Mode.TupleEqual("get_lengths"))) != 0)
            {
                //Output in "get_lengths" mode is the length of the feature
                hv_ExtendedResults = new HTuple();
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(new HTuple(hv_Feature.TupleLength()
                    ));
            }
            else if ((int)(new HTuple(hv_Mode.TupleEqual("calculate"))) != 0)
            {
                //Output in "calculate" mode is the feature vector
                hv_ExtendedResults = new HTuple();
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_AccumulatedResults);
                hv_ExtendedResults = hv_ExtendedResults.TupleConcat(hv_Feature);
            }
            else
            {
                hv_ExtendedResults = hv_AccumulatedResults.Clone();
            }

            return;
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate the gradient direction histogram. 
        public void calc_feature_grad_dir_histo(HObject ho_Region, HObject ho_Image, HTuple hv_NumBins,
            out HTuple hv_Feature)
        {




            // Local iconic variables 

            HObject ho_Channel1, ho_RegionSelected = null;
            HObject ho_ImageReduced = null, ho_EdgeAmplitude = null, ho_EdgeDirection = null;

            // Local control variables 

            HTuple hv_NumRegions = null, hv_Index = null;
            HTuple hv_Histo = new HTuple(), hv_BinSize = new HTuple();
            HTuple hv_Sum = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Channel1);
            HOperatorSet.GenEmptyObj(out ho_RegionSelected);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
            HOperatorSet.GenEmptyObj(out ho_EdgeDirection);
            try
            {
                //
                //Calculate gradient direction histogram
                //
                ho_Channel1.Dispose();
                HOperatorSet.AccessChannel(ho_Image, out ho_Channel1, 1);
                HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                hv_Feature = new HTuple();
                HTuple end_val6 = hv_NumRegions;
                HTuple step_val6 = 1;
                for (hv_Index = 1; hv_Index.Continue(end_val6, step_val6); hv_Index = hv_Index.TupleAdd(step_val6))
                {
                    ho_RegionSelected.Dispose();
                    HOperatorSet.SelectObj(ho_Region, out ho_RegionSelected, hv_Index);
                    ho_ImageReduced.Dispose();
                    HOperatorSet.ReduceDomain(ho_Channel1, ho_RegionSelected, out ho_ImageReduced
                        );
                    ho_EdgeAmplitude.Dispose(); ho_EdgeDirection.Dispose();
                    HOperatorSet.SobelDir(ho_ImageReduced, out ho_EdgeAmplitude, out ho_EdgeDirection,
                        "sum_abs_binomial", 3);
                    HOperatorSet.GrayHistoRange(ho_RegionSelected, ho_EdgeDirection, 0, 179,
                        hv_NumBins, out hv_Histo, out hv_BinSize);
                    hv_Sum = hv_Histo.TupleSum();
                    if ((int)(new HTuple(hv_Sum.TupleNotEqual(0))) != 0)
                    {
                        hv_Feature = hv_Feature.TupleConcat((hv_Histo.TupleReal()) / hv_Sum);
                    }
                    else
                    {
                        hv_Feature = hv_Feature.TupleConcat(hv_Histo);
                    }
                }
                ho_Channel1.Dispose();
                ho_RegionSelected.Dispose();
                ho_ImageReduced.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_EdgeDirection.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_Channel1.Dispose();
                ho_RegionSelected.Dispose();
                ho_ImageReduced.Dispose();
                ho_EdgeAmplitude.Dispose();
                ho_EdgeDirection.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Describe and calculate user-defined features to be used in conjunction with the calculate_feature_set procedure library. 
        public void get_custom_features(HObject ho_Region, HObject ho_Image, HTuple hv_CurrentName,
            HTuple hv_Mode, out HTuple hv_Output)
        {




            // Local iconic variables 

            HObject ho_RegionSelected = null, ho_Contours = null;
            HObject ho_ContoursSelected = null, ho_ContoursSplit = null;

            // Local control variables 

            HTuple hv_TmpResults = null, hv_Name = null;
            HTuple hv_Groups = null, hv_Feature = new HTuple(), hv_NumRegions = new HTuple();
            HTuple hv_I = new HTuple(), hv_NumContours = new HTuple();
            HTuple hv_NumLines = new HTuple(), hv_J = new HTuple();
            HTuple hv_NumSplit = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionSelected);
            HOperatorSet.GenEmptyObj(out ho_Contours);
            HOperatorSet.GenEmptyObj(out ho_ContoursSelected);
            HOperatorSet.GenEmptyObj(out ho_ContoursSplit);
            try
            {
                //
                //This procedure can be used to extend the functionality
                //of the calculate_feature_set procedure library by
                //user-defined features.
                //
                //Instructions:
                //
                //1. Find the template block at the beginning the procedure
                //(marked by comments) and duplicate it.
                //
                //2. In the copy edit the two marked areas as follows:
                //
                //2.1. Feature name and groups:
                //Assign a unique identifier for your feature to the variable "Name".
                //Then, assign the groups that you want your feature to belong to
                //to the variable "Groups".
                //
                //2.2. Feature calculation:
                //Enter the code that calculates your feature and
                //assign the result to the variable "Feature".
                //
                //3. Test
                //Use the "test_feature" procedure to check,
                //if the feature is calculated correctly.
                //If the procedure throws an exception,
                //maybe the order of the feature vector is wrong
                //(See note below).
                //
                //4. Integration
                //- Save your modified procedure get_custom_features.hdvp
                //  to a location of your choice.
                //  (We recommend not to overwrite the template.)
                //- Make sure, that your version of get_custom_procedures
                //  is included in the procedure directories of HDevelop.
                //  (Choose Procedures -> Manage Procedures -> Directories -> Add from the HDevelop menu bar.)
                //
                //Note:
                //The current implementation supports region arrays as input.
                //In that case, multi-dimensional feature vectors are simply concatenated.
                //Example: The feature "center" has two dimensions [Row,Column].
                //If an array of three regions is passed, the correct order of the "Feature" variable is
                //[Row1, Column1, Row2, Column2, Row3, Column3].
                //
                hv_TmpResults = new HTuple();
                //************************************************
                //************************************************
                //**** Copy the following template block     *****
                //**** and edit the two marked code sections *****
                //**** to add user-defined features          *****
                //************************************************
                //************************************************
                //
                //***************************************
                //*********** TEMPLATE BLOCK ************
                //***************************************
                //
                //********************************************************************
                //** Section 1:
                //** Enter unique feature name and groups to which it belongs here ***
                hv_Name = "custom_feature_numlines";
                hv_Groups = "custom";
                //** Enter unique feature name and groups above this line ************
                //********************************************************************
                if ((int)(new HTuple(hv_Name.TupleEqual(hv_CurrentName))) != 0)
                {
                    //******************************************************
                    //** Section 2:
                    //** Enter code to calculate feature here **************
                    hv_Feature = new HTuple();
                    HOperatorSet.CountObj(ho_Region, out hv_NumRegions);
                    HTuple end_val69 = hv_NumRegions;
                    HTuple step_val69 = 1;
                    for (hv_I = 1; hv_I.Continue(end_val69, step_val69); hv_I = hv_I.TupleAdd(step_val69))
                    {
                        ho_RegionSelected.Dispose();
                        HOperatorSet.SelectObj(ho_Region, out ho_RegionSelected, hv_I);
                        ho_Contours.Dispose();
                        HOperatorSet.GenContourRegionXld(ho_RegionSelected, out ho_Contours, "border");
                        HOperatorSet.CountObj(ho_Contours, out hv_NumContours);
                        hv_NumLines = 0;
                        HTuple end_val74 = hv_NumContours;
                        HTuple step_val74 = 1;
                        for (hv_J = 1; hv_J.Continue(end_val74, step_val74); hv_J = hv_J.TupleAdd(step_val74))
                        {
                            ho_ContoursSelected.Dispose();
                            HOperatorSet.SelectObj(ho_Contours, out ho_ContoursSelected, hv_J);
                            ho_ContoursSplit.Dispose();
                            HOperatorSet.SegmentContoursXld(ho_ContoursSelected, out ho_ContoursSplit,
                                "lines", 5, 2, 1);
                            HOperatorSet.CountObj(ho_ContoursSplit, out hv_NumSplit);
                            hv_NumLines = hv_NumLines + hv_NumSplit;
                        }
                        hv_Feature = hv_Feature.TupleConcat(hv_NumLines);
                    }
                    //** Enter code to calculate feature above this line ***
                    //******************************************************
                    append_length_or_values(hv_Mode, hv_Feature, hv_TmpResults, out hv_TmpResults);
                }
                append_names_or_groups(hv_Mode, hv_Name, hv_Groups, hv_CurrentName, hv_TmpResults,
                    out hv_TmpResults);
                //
                //************************************
                //****** END OF TEMPLATE BLOCK *******
                //************************************
                //
                hv_Output = hv_TmpResults.Clone();
                ho_RegionSelected.Dispose();
                ho_Contours.Dispose();
                ho_ContoursSelected.Dispose();
                ho_ContoursSplit.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_RegionSelected.Dispose();
                ho_Contours.Dispose();
                ho_ContoursSelected.Dispose();
                ho_ContoursSplit.Dispose();

                throw HDevExpDefaultException;
            }
        }

        // Chapter: Classification / Misc
        // Short Description: Calculate one or more features of a given image and/or region. 
        public void calculate_features(HObject ho_Region, HObject ho_Image, HTuple hv_FeatureNames,
            out HTuple hv_Features)
        {



            // Initialize local and output iconic variables 
            //
            //Calculate features given in FeatureNames
            //for the input regions in Region
            //(if needed supported by the underlying
            //gray-value or color image Image).
            //
            get_features(ho_Region, ho_Image, hv_FeatureNames, "calculate", out hv_Features);

            return;
        }
        #endregion
    }
    public class ClassID:INotifyPropertyChanged
    {
        public void Save(HFile file)
        {
            new HTuple(DisplayColor, ID, Name).SerializeTuple().FwriteSerializedItem(file);   
        }
        public void Load(DeserializeFactory item)
        {
            HTuple loaded = item.DeserializeTuple();
            DisplayColor = loaded[0];
            ID = loaded[1];
            Name = loaded[2];
        }

        string _color="#ff0000ff";
        public string DisplayColor
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    RaisePropertyChanged("DisplayColor");
                }
            }
        }

        public ClassID(int ID)
        {
            this.ID = ID;
        }
        public ClassID(DeserializeFactory item)
        {
            Load(item);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        string _name;
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
        int _id;
        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

    }
}