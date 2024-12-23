using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Fixture","Find Pattern")]
    public class PatternNode: BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);                       
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);                       
        }
        static PatternNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<PatternNode>));
        }

        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeOutputViewModel<HHomMat2D> HomOutput { get; }

        #region Field        
        public HNCCModel Model { get; set; } = null;
        [HMIProperty("Parameters", true)]
        public PatternParameters Parameters { get; set; } = new PatternParameters();
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":             
                    PatternWindow wd = new PatternWindow(this);
                    wd.ShowDialog();
                    break;
            }
        }
        [HMIProperty("Pattern Editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public override void Run(object context)
        {
            var image = ImageInput.Value;
            if (image != null)
            {
                var hom = RunNode(image);
                var icontext = context as InspectionContext;
                if (ShowDisplay && hom != null)
                {
                    HRegion region = hom.AffineTransRegion(Parameters.TrainParam.TrainRegion, "nearest_neighbor");
                    
                    icontext.inspection_result.AddRegion(region, "green",  "margin");
                }
                if (ShowDisplay)
                {
                    icontext.inspection_result.AddRegion(Parameters.RuntimeParam.SearchRegion, "blue", "margin");
                }
                HomOutput.OnNext(hom);
            }
            else { HomOutput.OnNext(new HHomMat2D()); }
        }
        public PatternNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Pattern";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);



            HomOutput = new ValueNodeOutputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Outputs.Add(HomOutput);


        }

        #region Method
        public HHomMat2D RunNode(HImage image)
        {
            if (!Parameters.TrainParam.IsTrained) { return null; }
            if (Model == null) return null;
            HImage processImage = image;
            if (image.CountChannels() == 3)
            {
                processImage = image.Rgb1ToGray();
            }
           
            double angleStart = Math.PI * Parameters.RuntimeParam.LowerAngle / 180;
            double angleExtent = Math.PI * (Parameters.RuntimeParam.UpperAngle - Parameters.TrainParam.LowerAngle) / 180;
            Model.FindNccModel(processImage.ReduceDomain(Parameters.RuntimeParam.SearchRegion), angleStart, angleExtent, Parameters.RuntimeParam.MinScore, Parameters.RuntimeParam.NumMatches, Parameters.RuntimeParam.MaxOverlap,
               "true", Parameters.RuntimeParam.NumLevels, out HTuple row, out HTuple col, out HTuple angle, out HTuple score); ;
            if (row.Length == 0)
            {
                Console.WriteLine("Can't find pattern");
                return null;
            }
            HHomMat2D hom = new HHomMat2D();
            hom.VectorAngleToRigid(Parameters.TrainParam.OriginalRow, Parameters.TrainParam.OriginalCol, 0.0, row.D, col.D, angle.D);
            return hom;
        }
        #endregion
    }

    public class PatternParameters : INotifyPropertyChanged,IHalconDeserializable
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void Save(HFile file)
        {
            TrainParam.Save(file);
            RuntimeParam.Save(file);
        }

        public void Load(DeserializeFactory item)
        {
            TrainParam.Load(item);
            RuntimeParam.Load(item);
        }
        public TrainParam TrainParam { get; set; }

        public RuntimeParam RuntimeParam { get; set; }
        public PatternParameters()
        {
            this.TrainParam = new TrainParam();
            this.RuntimeParam = new RuntimeParam();
        }
    }

    public class TrainParam : INotifyPropertyChanged
    {
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        #region Train Prop
        int lowerAngle = -20;
        public int LowerAngle
        {
            get
            {
                return lowerAngle;
            }
            set
            {
                if (lowerAngle != value)
                {
                    lowerAngle = value;
                    RaisePropertyChanged("LowerAngle");
                }
            }
        }
        int upperAngle = 20;
        public int UpperAngle
        {
            get
            {
                return upperAngle;
            }
            set
            {
                if (upperAngle != value)
                {
                    upperAngle = value;
                    RaisePropertyChanged("UpperAngle");
                }
            }
        }

        int numLevels = 0;
        public int NumLevels
        {
            get
            {
                return numLevels;
            }
            set
            {
                if (numLevels != value)
                {
                    numLevels = value;
                    RaisePropertyChanged("NumLevels");
                }
            }
        }
        bool usePolarity = false;
        public bool UsePolarity
        {
            get
            {
                return usePolarity;
            }
            set
            {
                if (usePolarity != value)
                {
                    usePolarity = value;
                    RaisePropertyChanged("UsePolarity");
                }
            }
        }
        public double OriginalRow { get; set; }
        public double OriginalCol { get; set; }
        bool isTrained;
        public bool IsTrained
        {
            get
            {
                return isTrained;
            }
            set
            {
                if (isTrained != value)
                {
                    isTrained = value;
                    RaisePropertyChanged("IsTrained");
                }
            }
        }
        public HImage ModelImage { get; set; }
        public HRegion TrainRegion { get; set; }
        #endregion

    }

    public class RuntimeParam : INotifyPropertyChanged
    {
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            (new HTuple(LowerAngle)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(UpperAngle)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(MinScore)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(MaxOverlap)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(NumLevels)).SerializeTuple().FwriteSerializedItem(file);
            (new HRegion(SearchRegion)).SerializeRegion().FwriteSerializedItem(file);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            LowerAngle = item.DeserializeTuple();
            UpperAngle = item.DeserializeTuple();
            MinScore = item.DeserializeTuple();
            MaxOverlap = item.DeserializeTuple();
            NumLevels = item.DeserializeTuple();
            SearchRegion = item.DeserializeRegion();
        }
        public HRegion SearchRegion = new HRegion(10, 10.0, 100, 100);
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        int lowerAngle = -20;
        public int LowerAngle
        {
            get
            {
                return lowerAngle;
            }
            set
            {
                if (lowerAngle != value)
                {
                    lowerAngle = value;
                    RaisePropertyChanged("Lower_Angle");
                }
            }
        }

        int upperAngle = 20;
        public int UpperAngle
        {
            get
            {
                return upperAngle;
            }
            set
            {
                if (upperAngle != value)
                {
                    upperAngle = value;
                    RaisePropertyChanged("Upper_Angle");
                }
            }
        }

        double minScore = 0.8;
        public double MinScore
        {
            get
            {
                return minScore;
            }
            set
            {
                if (minScore != value)
                {
                    minScore = value;
                    RaisePropertyChanged("MinScore");
                }
            }
        }

        int numMatches = 1;
        public int NumMatches
        {
            get
            {
                return numMatches;
            }
            set
            {
                if (numMatches != value)
                {
                    numMatches = value;
                    RaisePropertyChanged("NumMatches");
                }
            }
        }

        double maxOverlap = 0.5;
        public double MaxOverlap
        {
            get
            {
                return maxOverlap;
            }
            set
            {
                if (maxOverlap != value)
                {
                    maxOverlap = value;
                    RaisePropertyChanged("MaxOverlap");
                }
            }
        }

        int numLevels = 0;
        public int NumLevels
        {
            get
            {
                return numLevels;
            }
            set
            {
                if (numLevels != value)
                {
                    numLevels = value;
                    RaisePropertyChanged("NumLevels");
                }
            }
        }






    }

    public class PatternResult
    {
        public int ID { get; set; }
        public int Angle { get; set; }
        public double Score { get; set; }
        public double Row { get; set; }
        public double Col { get; set; }
        public PatternResult(int ID, double Row, double Col, int Angle, double Score )
        {
            this.ID = ID;
            this.Angle = Angle;
            this.Score = Score;
            this.Row = Row;
            this.Col = Col;
        }

    }

}
