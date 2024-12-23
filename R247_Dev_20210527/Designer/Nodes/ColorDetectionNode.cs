using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.PropertiesViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.UserControls;
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
    [NodeInfo("Deep Learning","Color Detection", Icon: "Designer/icons/icons8-color-palette-60.png")]
    public class ColorDetectionNode : BaseNode
    {
        #region properties
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);                                       
        }
        [SerializeIgnore]
        public Control PropertiesView
        {
            get
            {
                return new ColorDetectionView(this);
            }
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        [HMIProperty("Train Window")]
        public IReactiveCommand ShowTrainWindowCommand
        {
            get { return ReactiveCommand.Create((Control sender) => ShowTrainWindow(sender)); }
        }
        [HMIProperty("Edit Region")]
        public IReactiveCommand ShowRegionEditor
        {
            get { return ReactiveCommand.Create((Control sender) => ShowRegionWindow(sender)); }
        }
        public ColorDetection ColorDetection { get; set; } = new ColorDetection();

        #endregion
        static ColorDetectionNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ColorDetectionNode>));
        }
        [HMIProperty("Region Border Offset")]
        public double RegionBorderOffset { get; set; } = 0;
        [HMIProperty("Display result table")]
        public bool DisplayResultTable { get; set; } = true;
        [HMIProperty("Set Inspection Context")]
        public bool SetInspectionContext { get; set; } = true;
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeInputViewModel<FeaturesClassifier> ClassifierInput { get; }
        //public ValueNodeOutputViewModel<bool> Result { get; }
        public ValueNodeOutputViewModel<HRegion> RegionOutput { get; set; }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    ShowTrainWindow(sender);
                    //if (ImageInput.Value != null)
                    //{
                    //    EdgeAlignmentEditorWindow wd = new EdgeAlignmentEditorWindow(edges, ImageInput.Value.CopyImage());
                    //    wd.ShowDialog();
                    //}

                    break;
            }
        }
        void ShowRegionWindow(Control sender)
        {
            try
            {
                if (ImageInput.Value != null)
                {

                    WindowRegionWindowInteractive draw;
                    if (FixtureInput.Value != null)
                        draw = new WindowRegionWindowInteractive(ImageInput.Value.CopyImage(), ColorDetection.Region, FixtureInput.Value.Clone());
                    else
                        draw = new WindowRegionWindowInteractive(ImageInput.Value.CopyImage(), ColorDetection.Region, new HHomMat2D());
                    draw.Owner = Window.GetWindow(sender);
                    draw.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void ShowTrainWindow(Control sender)
        {
            try
            {
                if (ImageInput.Value == null)
                {
                    return;
                }
                if (ImageInput.Value.CountObj() > 0)
                {
                    try
                    {
                        HTuple channels = ImageInput.Value.CountChannels();
                        if (channels < 3)
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        return;
                    }
                    TrainColorMlpWindow wd = new TrainColorMlpWindow(ColorDetection.ClassMLP, ColorDetection.ClassLUTColor, ImageInput.Value.CopyImage(), ColorDetection);
                    wd.Owner = Window.GetWindow(sender);
                    wd.ShowDialog();
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }
        }
        public override void Run(object context)
        {
            var IContext = context as InspectionContext;
            if (RegionBorderOffset <= 0)
            {
                var DetectRegion = RunInside(ImageInput.Value, RegionInput.Value, FixtureInput.Value, ShowDisplay, IContext);
                if (SetInspectionContext)
                {
                    if (DetectRegion.CountObj() > 0)
                    {
                        IContext.result &= false;
                        if (DisplayResultTable)
                        {

                            IContext.SetResult(this.Name, false);

                        }
                    }
                    else
                    {
                        if (DisplayResultTable)
                        {

                            IContext.SetResult(this.Name , true);

                        }
                    }
                }
                
                RegionOutput.OnNext(DetectRegion);
            }
            else
            {
                var Detectedregion = RunInside(ImageInput.Value, RegionInput.Value.ErosionCircle(RegionBorderOffset),
                    FixtureInput.Value, ShowDisplay, IContext);
                if (SetInspectionContext)
                {
                    if (Detectedregion.CountObj() > 0)
                    {
                        IContext.result &= false;
                        if (DisplayResultTable)
                        {

                            IContext.SetResult(this.Name, false);

                        }
                    }
                    else
                    {
                        if (DisplayResultTable)
                        {

                            IContext.SetResult(this.Name, true);

                        }
                    }
                } 
                
                RegionOutput.OnNext(Detectedregion);
            }
            
        }
        public HRegion RunInside(HImage image,HRegion ROI, HHomMat2D fixture,bool ShowDisplay, InspectionContext e)
        {
            if (!IsEnabled | image == null)
                return null;
            HRegion region_output = ColorDetection.Run(image,ROI, fixture, ShowDisplay, e);
            return region_output;
        }
        public ColorDetectionNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            //create dir
            ColorDetection.calib = calib;

            this.Name = "Color Detection";

            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(ImageInput);

            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
                //Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(FixtureInput);
            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Region",
                Editor = new RegionValueEditorViewModel(),
                PortType = "HRegion"
            };
            this.Inputs.Add(RegionInput);
            ClassifierInput = new ValueNodeInputViewModel<FeaturesClassifier>()
            {
                Name = "Classifier",
                PortType = "ColorDetectInput" ///Chua biet Type
                //Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(ClassifierInput);
            //Result = new ValueNodeOutputViewModel<bool>()
            //{
            //    Name = "Result",
            //    //Editor = new IntegerValueEditorViewModel()
            //    //Value = Observable.Zip(ImageInput.ValueChanged, FixtureInput.ValueChanged, (image, fixture) => (image, fixture)).ObserveOn(Scheduler.Default).filer.Select((v) =>
            //    //    {
            //    //        RunInside(ImageInput.Value, FixtureInput.Value, (InspectionContext)context);
            //    //    })
            //};
            RegionOutput = new ValueNodeOutputViewModel<HRegion>()
            {
                Name = "Region",
                PortType = "Region"
            };

            this.Outputs.Add(RegionOutput);
            //edges = new ObservableCollection<EdgeFinder>();
            //load model if exist
            //LoadRecipe(datadir);         
        }
    }
    public class ColorDetection : INotifyPropertyChanged,IHalconDeserializable
    {
        double _rejection_threshold = 0.5;
        public double RejectionThreshold
        {
            get
            {
                return _rejection_threshold;
            }
            set
            {
                if (_rejection_threshold != value)
                {
                    _rejection_threshold = value;
                    RaisePropertyChanged("RejectionThreshold");
                }
            }
        }
        bool _export_region = false;
        public bool ExportRegion
        {
            get
            {
                return _export_region;
            }
            set
            {
                if (_export_region != value)
                {
                    _export_region = value;
                    RaisePropertyChanged("ExportRegion");
                }
            }
        }
        bool _interactive_region = true;
        public bool InteractiveRegion
        {
            get
            {
                return _interactive_region;
            }
            set
            {
                if (_interactive_region != value)
                {
                    _interactive_region = value;
                    RaisePropertyChanged("InteractiveRegion");
                }
            }
        }

        bool _enabled_tag = false;
        public bool EnabledTag
        {
            get
            {
                return _enabled_tag;
            }
            set
            {
                if (_enabled_tag != value)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Enable Tag", _enabled_tag.ToString(), value.ToString(), "Change Parameter");

                    _enabled_tag = value;
                    RaisePropertyChanged("EnabledTag");
                }
            }
        }

        double _sensitive = 20;
        public double Sensitive
        {
            get
            {
                return _sensitive;
            }
            set
            {
                if (_sensitive != value)
                {
                    _sensitive = value;
                    RaisePropertyChanged("Sensitive");
                }
            }
        }

        public void ClearAll()
        {
            tools.Clear();
        }
        public HClassMlp ClassTexture { get; set; }


        private FeaturesClassifier  classifier;
        public void SetClassifier(FeaturesClassifier classifier)
        {
            this.classifier = classifier;
            RefreshRejectionClass();
        }
        public void RefreshRejectionClass()
        {
            if (classifier != null)
            {
                foreach (ClassID class_id in classifier.LstClass)
                {
                    if (LstRejectionClass.Any(x => x.Name == class_id.Name))
                    {

                    }
                    else
                    {
                        LstRejectionClass.Add(new RejectionClass() { Name = class_id.Name, IsChecked = true });
                    }


                }
                foreach (RejectionClass reject_class in LstRejectionClass)
                {
                    if (classifier.LstClass.Any(x => x.Name == reject_class.Name))
                    {

                    }
                    else
                    {
                        LstRejectionClass.Remove(reject_class);
                    }
                }

            }
        }

        ObservableCollection<RejectionClass> _lst_rejection_class = new ObservableCollection<RejectionClass>();
        public ObservableCollection<RejectionClass> LstRejectionClass
        {
            get
            {
                return _lst_rejection_class;
            }
            set
            {
                if (_lst_rejection_class != value)
                {
                    _lst_rejection_class = value;
                    RaisePropertyChanged("LstRejectionClass");
                }
            }
        }

        void RaisePropertyChanged(string prop, object value)
        {
            if (PropertyChanged != null)
            {
                //MainWindow.WriteActionDatabase(this.Name + "." + prop + " = " + value.ToString(), "Change Parameter");
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _is_enabled = true;
        public bool IsEnabledTool
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                    RaisePropertyChanged("IsEnabledTool");
                }
            }
        }
        bool _invert = false;
        public bool Invert
        {
            get
            {
                return _invert;
            }
            set
            {
                if (_invert != value)
                {
                    _invert = value;
                    RaisePropertyChanged("Invert");
                }
            }
        }

        bool _enable_reduce_size = false;
        public bool EnableReduceSize
        {
            get
            {
                return _enable_reduce_size;
            }
            set
            {
                if (_enable_reduce_size != value)
                {
                    _enable_reduce_size = value;
                    RaisePropertyChanged("EnableReduceSize", value);
                }
            }
        }
        string _tag;
        public string Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                if (_tag != value)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Tag", _tag, value, "Change Parameter");

                    _tag = value;
                    RaisePropertyChanged("Tag");
                }
            }
        }

        bool _enable_discoloration = true;
        public bool EnableDiscoloration
        {
            get
            {
                return _enable_discoloration;
            }
            set
            {
                if (_enable_discoloration != value)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Enable", _enable_discoloration.ToString(), value.ToString(), "Change Parameter");
                    _enable_discoloration = value;
                    RaisePropertyChanged("EnableDiscoloration", value);

                }
            }
        }

        //public InputBlock<HImage, HHomMat2D> InputBlock;
        double _min_discoloration = 1;
        public double MinDiscoloration
        {
            get
            {
                return _min_discoloration;
            }
            set
            {
                if (_min_discoloration != value)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Min Defect Size", _min_discoloration.ToString(), value.ToString(), "Change Parameter");
                    _min_discoloration = value;
                    RaisePropertyChanged("MinDiscoloration");
                }
            }
        }
        //public Output<HRegion> OutputRegion { get; set; }

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
                    //MainWindow.WriteActionDatabase(this.Name, "Name", _name, value, "Rename Parameter");
                    _name = value;
                    RaisePropertyChanged("Name", value);
                }
            }
        }
        public CollectionOfregion Region { get; set; }




        private HClassMlp class_mlp;
        public HClassLUT ClassLUTColor { get; set; }


        public HClassMlp ClassMLP
        {
            get
            {
                return class_mlp;
            }

            set
            {

                class_mlp = value;
            }
        }




        public Calibration calib;
        public string ClassifierName { get; set; }
        public void Save(HFile file)
        {
            (new HTuple(Name)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(tool_num)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(MinDiscoloration)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(ClosingRadius)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(InteractiveRegion)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(EnabledOutput)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(EnabledTag)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Invert)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Output)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Tag)).SerializeTuple().FwriteSerializedItem(file);
            if (EnableDiscoloration)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);

            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);

            }
            if (EnableReduceSize)
            {
                (new HTuple(1)).SerializeTuple().FwriteSerializedItem(file);

            }
            else
            {
                (new HTuple(0)).SerializeTuple().FwriteSerializedItem(file);

            }
            class_mlp.SerializeClassMlp().FwriteSerializedItem(file);
            Region.Save(file);
            ClassifierName = classifier?.Name;
            HelperMethods.SaveParam(file, this);

            //imagesource name


        }
        double _closing_radius = 2.5;
        public double ClosingRadius
        {
            get
            {
                return _closing_radius; ;
            }
            set
            {
                if (_closing_radius != value & value > 0)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Closing Circle", _closing_radius.ToString(), value.ToString(), "Change Parameter");

                    _closing_radius = value;
                    RaisePropertyChanged("ClosingRadius");
                }
            }
        }

        public void Load(DeserializeFactory item)
        {
            Name = item.DeserializeTuple();
            tool_num = item.DeserializeTuple();
            MinDiscoloration = item.DeserializeTuple();
            ClosingRadius = item.DeserializeTuple();
            InteractiveRegion = item.DeserializeTuple();
            EnabledOutput = item.DeserializeTuple();
            EnabledTag = item.DeserializeTuple();
            Invert = item.DeserializeTuple();
            Output = item.DeserializeTuple();
            Tag = item.DeserializeTuple();

            HTuple temp = item.DeserializeTuple();
            if (temp == 1)
            {
                EnableDiscoloration = true;
            }
            else
            {
                EnableDiscoloration = false;
            }

            temp = item.DeserializeTuple();
            if (temp == 1)
            {
                EnableReduceSize = true;
            }
            else
            {
                EnableReduceSize = false;
            }

            class_mlp = item.DeserializeClassMlp();
            if (ClassLUTColor == null)
            {
                ClassLUTColor = new HClassLUT(class_mlp, new HTuple(), new HTuple());
            }
            else
            {
                ClassLUTColor.CreateClassLutMlp(class_mlp, new HTuple(), new HTuple());
            }
            Region.Load(item);
            HelperMethods.LoadParam(item, this);

        }
        public void Load(DeserializeFactory item, string version)
        {
            switch (version)
            {
                case "1.00":
                    IsEnabledTool = true;
                    Name = item.DeserializeTuple();
                    tool_num = item.DeserializeTuple();
                    MinDiscoloration = item.DeserializeTuple();
                    item.DeserializeTuple();

                    HTuple temp = item.DeserializeTuple();
                    if (temp == 1)
                    {
                        EnableDiscoloration = true;
                    }
                    else
                    {
                        EnableDiscoloration = false;
                    }


                    temp = item.DeserializeTuple();
                    if (temp == 1)
                    {

                    }
                    else
                    {

                    }


                    temp = item.DeserializeTuple();
                    if (temp == 1)
                    {
                        EnableReduceSize = true;
                    }
                    else
                    {
                        EnableReduceSize = false;
                    }

                    class_mlp = item.DeserializeClassMlp();
                    item.DeserializeClassMlp();

                    Region.Load(item);
                    new CollectionOfregion().Load(item);

                    HTuple imagesoure_null = item.DeserializeTuple();
                    if (imagesoure_null == 1)
                    {
                        item.DeserializeTuple();
                    }

                    HTuple fixture_null = item.DeserializeTuple();
                    if (fixture_null == 1)
                    {
                        item.DeserializeTuple();
                    }

                    return;
                case "1.01":
                    HelperMethods.LoadParam(item, this);
                    return;
            }

            HelperMethods.LoadParam(item, this);

        }




        public void Remove()
        {

            tools.Remove(tool_num);
        }

        public double GetMaxArea(HRegion input)
        {
            HTuple Areas = input.Area;
            HTuple sort = Areas.TupleSortIndex();
            if (sort.Type != HTupleType.EMPTY)
            {
                return Math.Round((double)Areas[(int)sort[(int)(sort.Length - 1)]] / (calib.ScaleY * calib._scale_x), 2);
            }
            else return 0;
        }
        public HRegion Run(HImage image,HRegion ROI, HHomMat2D fixture,bool  ShowDisplay, InspectionContext e)
        {
            HTuple channels = image.CountChannels();
            if (channels < 3)
                return null;
            bool i_result = true;
            bool use_fixture = false;
            if (fixture != null)
            {
                use_fixture = true;
            }
            InspectionResult inspection_result = e.inspection_result;
            //HRegion region_inspection = RegionNew;
            //if (!_interactive_region)
            HRegion region_inspection = ROI;
            if (use_fixture)
            {
                region_inspection = fixture.AffineTransRegion(region_inspection, "nearest_neighbor");
            }
            // HRegion region_color = new HRegion();
            HRegion region_color_select = new HRegion();
            region_color_select.GenEmptyObj();
            if (IsEnabledTool)
            {
                if (EnableReduceSize)
                {
                    HRegion region_color = ClassLUTColor.ClassifyImageClassLut(image.ZoomImageFactor(0.5, 0.5, "constant").ReduceDomain(region_inspection.ZoomRegion(0.5, 0.5))).SelectObj(2).ZoomRegion(2, 2).ClosingCircle(_closing_radius).Connection();
                    region_color_select = region_color.SelectShape("area", "and", calib._scale_x * calib.ScaleY * MinDiscoloration, 99999999);
                }
                else
                {
                    HRegion region_color = ClassLUTColor.ClassifyImageClassLut(image.ReduceDomain(region_inspection)).SelectObj(2).ClosingCircle(_closing_radius).Connection();
                    //HXLDCont lines = data.Item1.ReduceDomain(region_inspection).LinesColor(1.5, 15, 20, "true", "true").UnionCollinearContoursXld(10, 1, 2, 0.1, "attr_keep").SelectContoursXld("contour_length", 100, 999999999, 100, 200).GenRegionContourXld("filled").Connection();

                    region_color_select = region_color.SelectShape("area", "and", calib._scale_x * calib.ScaleY * MinDiscoloration, 99999999);
                }
                if (ExportRegion)
                {
                    region_color_select.FillUp().Union1();
                }
                else
                {
                    if (!Invert)
                    {
                        if (region_color_select.CountObj() > 0)
                        {
                            if (classifier == null)
                            {
                                e.inspection_result.Add(region_color_select, "red", this.Name, "margin");
                                double defect_area = GetMaxArea(region_color_select);
                                //e.result &= false;

                                //  display.SetColor("red");
                                if (ShowDisplay)
                                {
                                    e.inspection_result.AddRegion(GenElip(region_color_select), "red");
                                }
                                i_result = false;
                                if (_enabled_tag)
                                    e.DefectTag.Add(Tag);
                                if (_enable_output)
                                    e.AddOutput(Output);
                            }
                            else
                            {
                                //display.SetColor("red");
                                List<ClassResult> result = classifier.ClassifyRegions(region_color_select, image);
                                for (int i = 0; i < result.Count; i++)
                                {

                                    if (_lst_rejection_class.First(x => x.Name == result[i].TargetClass.Name)?.IsChecked == false)
                                        continue;
                                    e.inspection_result.Add(result[i].regions, result[i].Color, this.Name, "margin");
                                    double defect_area = GetMaxArea(result[i].regions);
                                    if (ShowDisplay)
                                    {
                                        e.inspection_result.AddRegion(GenElip(result[i].regions), result[i].Color);

                                    }
                                
                                    //display.SetColor(result[i].Color);
                                    //DispElip(result[i].regions);
                                   // e.result &= false;

                                    i_result = false;
                                }

                                // classifier.ClassifyRegions(region_color_select, imagesource.Image);
                            }
                        }
                        
                        if (ShowDisplay)
                        {
                            e.inspection_result.AddRegion(region_inspection, Region.Color);
                        }
                       
                    }
                    else
                    {
                        if (region_color_select.CountObj() == 0)
                        {
                            i_result = false;
                            e.inspection_result.AddRegion(region_inspection, "red");
                        }
                        else
                        {
                            if (ShowDisplay)
                            {
                                e.inspection_result.AddRegion(region_inspection, Region.Color);
                            }
                          
                            i_result = true;
                        }
                    }
                }

            }
            Result = i_result;
            if (e.result)
            {

            }
            return region_color_select;
        }
        public HRegion GenElip(HRegion region)
        {
            HRegion region_elip = new HRegion();
            region_elip.GenEmptyRegion();
            for (int i = 0; i < region.CountObj(); i++)
            {
                double row, col, ra, rb, phi;
                region[i + 1].AreaCenter(out row, out col);
                ra = region[i + 1].EllipticAxis(out rb, out phi);
                HRegion cont = new HRegion();
                cont.GenEllipse(row, col, phi, ra + 20, rb + 20);
                region_elip = region_elip.ConcatObj(cont);
            }
            return region_elip;
        }
        public static List<int> tools = new List<int>();
        int tool_num;
        public HWindow display;
        bool _result;
        public bool Result
        {
            get
            {
                return _result;
            }
            set
            {
                if (_result != value)
                {
                    _result = value;
                    RaisePropertyChanged("Result");
                }
            }
        }
        public ColorDetection()
        {
            Region = new CollectionOfregion();
            Task.Run(new Action(() =>
            {
                ClassTexture = new HClassMlp(3, 10, 2, "softmax", "normalization", 10, 42);
                ClassMLP = new HClassMlp(3, 10, 2, "softmax", "normalization", 10, 42);
                if (ClassLUTColor == null)
                {

                    ClassLUTColor = new HClassLUT(class_mlp, new HTuple(), new HTuple());
                }
                else
                {

                    ClassLUTColor.CreateClassLutMlp(class_mlp, new HTuple(), new HTuple());
                }
            }));
            for (int i = 1; i < 100; i++)
            {
                if (!tools.Contains(i))
                {
                    tools.Add(i);
                    tool_num = i;
                    this.Name = "ColorDetection" + i.ToString();
                    break;
                }
            }
            Tag = this.Name;
        }
        bool _enable_output = false;
        public bool EnabledOutput
        {
            get
            {
                return _enable_output;
            }
            set
            {
                if (_enable_output != value)
                {
                    _enable_output = value;
                    RaisePropertyChanged("EnabledOutput");
                }
            }
        }
        int _output = 0;
        public int Output
        {
            get
            {
                return _output;
            }
            set
            {
                if (_output != value)
                {
                    //MainWindow.WriteActionDatabase(this.Name, "Output", _output.ToString(), value.ToString(), "Change Parameter");

                    _output = value;
                    RaisePropertyChanged("Output");
                }
            }
        }
        bool _disable_border = false;
        public bool DisableBorder
        {
            get
            {
                return _disable_border;
            }
            set
            {
                if (_disable_border != value)
                {
                    _disable_border = value;
                    RaisePropertyChanged("DisableBorder");
                }
            }
        }
        public void Run()
        {
            throw new NotImplementedException();
        }
    }
    public class RejectionClass
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public RejectionClass(DeserializeFactory item)
        {
            Load(item);
        }
        public RejectionClass()
        {

        }
        public void Load(DeserializeFactory item)
        {
            HTuple loaded = item.DeserializeTuple();

            Name = loaded[0];
            IsChecked = loaded[1] == 1;
        }
        public void Save(HFile file)
        {
            new HTuple(Name, IsChecked ? 1 : 0).SerializeTuple().FwriteSerializedItem(file);
        }
    }
    
}
