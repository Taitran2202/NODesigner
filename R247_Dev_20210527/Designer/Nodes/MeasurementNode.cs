using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.ComponentModel;
using NOVisionDesigner.Designer.Misc;
using DevExpress.Xpf.Charts;
using System.Collections.ObjectModel;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Designer.PropertiesViews;
using System.Windows.Controls;
using NOVisionDesigner.Designer.Extensions;
using NodeNetwork.ViewModels;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Measurement","Measure Distance")]
    public class MeasurementNode : BaseNode
    {
        #region properties
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            new HTuple(measures.Count).SerializeTuple().FwriteSerializedItem(file);
            foreach (Measure tool in measures)
            {
                tool.Save(file);
            }
        }
        [SerializeIgnore]
        public Control PropertiesView { get {

                return new MeasurementView(this);
            }  }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            HTuple count = item.DeserializeTuple();
            for (int i = 0; i < count; i++)
            {
                Measure added = new Measure(calib);
                measures.Add(added);
                added.Load(item);
            }
        }
       
        public ObservableCollection<Measure> measures = new ObservableCollection<Measure>();

        #endregion
        static MeasurementNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MeasurementNode>));
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<bool> Result { get; }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
              

                    break;
            }
        }
        private MeasurementDataCollection expressionData = new MeasurementDataCollection();

        public override IExpressionData ExpressionData => expressionData;
        public override void Run(object context)
        {
            Result.OnNext(RunInside(ImageInput.Value, FixtureInput.Value, (InspectionContext)context));
        }
        
        public bool RunInside(HImage image, HHomMat2D fixture, InspectionContext e)
        {
            if (!IsEnabled|image==null)
                return false;
            List<MeasurementData> result = new List<MeasurementData>();
            bool i_result = true;
            foreach (Measure tool in measures)
            {
                if (!tool.IsEnabled)
                    continue;
                double? distance = tool.Run(image,fixture, e);
                var m_result = new MeasurementData();
                m_result.Name = tool.MeasureName;
                if (distance == null)
                {
                    i_result &= false;

                    if (tool.EnableTag)
                    {
                        if (!e.DefectTag.Contains(tool.Tag))
                            e.DefectTag.Add(tool.Tag);
                    }
                    if (tool.EnabledOutput)
                    {
                        e.AddOutput(tool.Output);
                    }
                    e.SetResult(this.Name + "-" + tool.MeasureName, false);
                   
                }
                else
                {
                    m_result.Distance = distance.Value;
                    if (distance > tool.LowerValue & distance < tool.UpperValue)
                    {
                        i_result &= true;
                        e.SetResult(this.Name + "-" + tool.MeasureName, true);
                    }
                    else
                    {
                        i_result &= false;
                        if (tool.EnableTag)
                        {
                            if (!e.DefectTag.Contains(tool.Tag))
                                e.DefectTag.Add(tool.Tag);
                        }
                        e.SetResult(this.Name + "-" + tool.MeasureName, false);
                    }
                }
                result.Add(m_result);
            }
            e.result &= i_result;
            return i_result;
        }
        public MeasurementNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            //create dir


            this.Name = "Measurement";
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
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Result",
                PortType = "Bool"
                //Editor = new IntegerValueEditorViewModel()
                //Value = Observable.Zip(ImageInput.ValueChanged, FixtureInput.ValueChanged, (image, fixture) => (image, fixture)).ObserveOn(Scheduler.Default).filer.Select((v) =>
                //    {
                //        RunInside(ImageInput.Value, FixtureInput.Value, (InspectionContext)context);
                //    })
            };
            

            this.Outputs.Add(Result);
            //edges = new ObservableCollection<EdgeFinder>();
            //load model if exist
            //LoadRecipe(datadir);
        }
    }
    public class MeasurementDataCollection:INotifyPropertyChanged,IExpressionData
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        object _data = new List<MeasurementData>();
        public object Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    RaisePropertyChanged("Data");
                }
            }
        }

        //object IExpressionData.Data { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
    public class MeasurementData
    {
        //public bool Result { get; set; }
        public double Distance { get; set; }
        public string Message { get; set; } = "";
        public string Name { get; set; } 
    }
    public class Test
    {
        public int Count { get; set; } = 10;
        public int Area { get; set; } = 20;
    }
    public class Measure : INotifyPropertyChanged
    {

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            new HTuple(upper_edge != null ? 1 : 0, lower_edge != null ? 1 : 0).SerializeTuple().FwriteSerializedItem(file);
            if (upper_edge != null)
            {
                upper_edge.Save(file);
            }
            if (lower_edge != null)
            {
                lower_edge.Save(file);
            }
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            HTuple valid = item.DeserializeTuple();
            if (valid[0] == 1)
            {
                upper_edge = new EdgeFinder(item);
            }
            if (valid[1] == 1)
            {
                lower_edge = new EdgeFinder(item);
            }
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


                    _output = value;
                    RaisePropertyChanged("Output");
                }
            }
        }

        bool _enable_tag = false;
        public bool EnableTag
        {
            get
            {
                return _enable_tag;
            }
            set
            {
                if (_enable_tag != value)
                {
                    _enable_tag = value;
                    RaisePropertyChanged("EnableTag");
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
                    _tag = value;
                    RaisePropertyChanged("Tag");
                }
            }
        }

        string _color = "#ffff00ff";
        public string Color
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
                    RaisePropertyChanged("Color");
                }
            }
        }

        bool _is_enable = true;
        public bool IsEnabled
        {
            get
            {
                return _is_enable;
            }
            set
            {
                if (_is_enable != value)
                {
                    _is_enable = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }

        public Measure(Calibration calib)
        {
            this.calib = calib;
        }

        Calibration calib;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        string _measure_name = "No name";
        public string MeasureName
        {
            get
            {
                return _measure_name;
            }
            set
            {
                if (_measure_name != value)
                {
                    _measure_name = value;
                    RaisePropertyChanged("MeasureName");
                }
            }
        }
        double _lower_value = 0;
        public double LowerValue
        {
            get
            {
                return _lower_value;
            }
            set
            {
                if (_lower_value != value)
                {
                    _lower_value = value;
                    RaisePropertyChanged("LowerValue");
                }
            }
        }
        double _upper_value = 100;
        public double UpperValue
        {
            get
            {
                return _upper_value;
            }
            set
            {
                if (_upper_value != value)
                {
                    _upper_value = value;
                    RaisePropertyChanged("UpperValue");
                }
            }
        }
        double _actual_value;
        public double ActualValue
        {
            get
            {
                return _actual_value;
            }
            set
            {
                if (_actual_value != value)
                {
                    _actual_value = value;
                    RaisePropertyChanged("ActualValue");
                }
            }
        }
        public EdgeFinder lower_edge, upper_edge;

        public void Create(int w, int h)
        {
            if (lower_edge == null)
            {
                lower_edge = new EdgeFinder(w, h);

            }
            if (upper_edge == null)
            {
                upper_edge = new EdgeFinder(w, h);
            }
        }
        string _mode = "PointToLine";
        public string Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode != value)
                {
                    _mode = value;

                    RaisePropertyChanged("Mode");
                }
            }
        }
        public double? Run(HImage image, HHomMat2D translate, InspectionContext e)
        {
            //lower_edge.Run()
           
            if (lower_edge == null | upper_edge == null)
            {
                return null;
            }
            double row1, col1, row2, col2;
            LineValue line1 = new LineValue(), line2 = new LineValue();
            if (translate == null)
            {
                translate = new HHomMat2D();
            }
            HImage image_gray = image.Rgb1ToGray();

            lower_edge.FindEdge(image_gray, translate, e, out row1, out col1);
            upper_edge.FindEdge(image_gray, translate, e, out row2, out col2);

            if (row1 != -1 & row2 != -1)
            {

                var scale_x = e.inspection_result.scale_x;
                var scale_y = e.inspection_result.scale_y;
                HHomMat2D calib_trans = new HHomMat2D();
                calib_trans = calib_trans.HomMat2dScaleLocal(1 / scale_y, 1 / scale_x);

                upper_edge.DispLineResult(row2, col2, e, ref line2);
                lower_edge.DispLineResult(row1, col1, e, ref line1);
                HTuple distance;
                switch (Mode)
                {
                    case "PointToPoint":
                        HTuple lower_edge_row_trans, lower_edge_col_trans;
                        calib_trans.AffineTransPixel(row1, col1, out lower_edge_row_trans, out lower_edge_col_trans);
                        HTuple upper_edge_row_trans, upper_edge_col_trans;
                        calib_trans.AffineTransPixel(row2, col2, out upper_edge_row_trans, out upper_edge_col_trans);
                        HOperatorSet.DistancePp(lower_edge_row_trans, lower_edge_col_trans, upper_edge_row_trans, upper_edge_col_trans, out distance);
                        //HOperatorSet.DistancePp(row1, col1, row2, col2, out distance);

                        break;
                    case "PointToLine":
                        HTuple row1_t, col1_t, l2_row1, l2_col1, l2_col2, l2_row2;
                        calib_trans.AffineTransPixel(row1, col1, out row1_t, out col1_t);
                        calib_trans.AffineTransPixel(line2.row1, line2.col1, out l2_row1, out l2_col1);
                        calib_trans.AffineTransPixel(line2.row2, line2.col2, out l2_row2, out l2_col2);
                        HOperatorSet.DistancePl(row1_t, col1_t, l2_row1, l2_col1, l2_row2, l2_col2, out distance);
                        break;
                    case "LineToPoint":
                        HTuple row2_t, col2_t, l1_row1, l1_col1, l1_col2, l1_row2;
                        calib_trans.AffineTransPixel(row2, col2, out row2_t, out col2_t);
                        calib_trans.AffineTransPixel(line1.row1, line1.col1, out l1_row1, out l1_col1);
                        calib_trans.AffineTransPixel(line1.row2, line1.col2, out l1_row2, out l1_col2);

                        HOperatorSet.DistancePl(row2_t, col2_t, l1_row1, l1_col1, l1_row2, l1_col2, out distance);
                        break;


                    default: distance = 0; break;
                }

                ActualValue = distance;
                if (distance > LowerValue & distance < UpperValue)
                    e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1, row2), new HTuple(col1, col2)), _color);
                else
                {
                    e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1, row2), new HTuple(col1, col2)), "red");
                    e.inspection_result?.Add(new HXLDCont(new HTuple(row1, row2), new HTuple(col1, col2)), "red", "NG", "measurement");
                }
                //display.SetColor(_color);              
                //display.DispLine(row1, col1, row2, col2);

                //if (line1.row1!=null)
                //{
                //    display.DispLine(line1.row1, line1.col1, line1.row2, line1.col2);
                //}
                //if (line2.row1 != null)
                //{
                //    display.DispLine(line2.row1, line2.col1, line2.row2, line2.col2);
                //}
                e.inspection_result?.AddText(Math.Round(distance.D, 2).ToString(), "black", "#ffffffaa", (row1 + row2) / 2, (col1 + col2) / 2);
                // display.DispText(Math.Round( distance.D,2).ToString(), "image", (row1 + row2) / 2, (col1 + col2) / 2, "black", "box_color", "#ffffffaa");
                return distance;
            }
            else
                return null;
        }
        public LineValue GetMeasureLine(LineValue line1, LineValue line2)
        {
            if (line1 != null & line2 != null)
            {
                var calib_trans = new HHomMat2D();
                HTuple distance;
                HTuple row1_disp = 0, row2_disp = 0, col1_disp = 0, col2_disp = 0;
                switch (Mode)
                {
                    case "PointToPoint":
                        HTuple lower_edge_row_trans, lower_edge_col_trans;
                        calib_trans.AffineTransPixel((line1.row1 + line1.row2) / 2, (line1.col1 + line1.col2) / 2, out lower_edge_row_trans, out lower_edge_col_trans);
                        HTuple upper_edge_row_trans, upper_edge_col_trans;
                        calib_trans.AffineTransPixel((line2.row1 + line2.row2) / 2, (line2.col1 + line2.col2) / 2, out upper_edge_row_trans, out upper_edge_col_trans);
                        HOperatorSet.DistancePp(lower_edge_row_trans, lower_edge_col_trans, upper_edge_row_trans, upper_edge_col_trans, out distance);
                        row1_disp = (line1.row1 + line1.row2) / 2;
                        row2_disp = (line2.row1 + line2.row2) / 2;
                        col1_disp = (line1.col1 + line1.col2) / 2;
                        col2_disp = (line2.col1 + line2.col2) / 2;
                        //HOperatorSet.DistancePp(row1, col1, row2, col2, out distance);

                        break;
                    case "PointToLine":
                        HTuple row1_t, col1_t, l2_row1, l2_col1, l2_col2, l2_row2;
                        calib_trans.AffineTransPixel((line1.row1 + line1.row2) / 2, (line1.col1 + line1.col2) / 2, out row1_t, out col1_t);
                        calib_trans.AffineTransPixel(line2.row1, line2.col1, out l2_row1, out l2_col1);
                        calib_trans.AffineTransPixel(line2.row2, line2.col2, out l2_row2, out l2_col2);
                        HOperatorSet.DistancePl(row1_t, col1_t, l2_row1, l2_col1, l2_row2, l2_col2, out distance);
                        row1_disp = (line1.row1 + line1.row2) / 2;
                        col1_disp = (line1.col1 + line1.col2) / 2;
                        HOperatorSet.ProjectionPl(row1_disp, col1_disp, line2.row1, line2.col1, line2.row2, line2.col2, out row2_disp, out col2_disp);
                        break;
                    case "LineToPoint":
                        HTuple row2_t, col2_t, l1_row1, l1_col1, l1_col2, l1_row2;
                        calib_trans.AffineTransPixel((line2.row1 + line2.row2) / 2, (line2.col1 + line2.col2) / 2, out row2_t, out col2_t);
                        calib_trans.AffineTransPixel(line1.row1, line1.col1, out l1_row1, out l1_col1);
                        calib_trans.AffineTransPixel(line1.row2, line1.col2, out l1_row2, out l1_col2);
                        row1_disp = (line2.row1 + line2.row2) / 2;
                        col1_disp = (line2.col1 + line2.col2) / 2;
                        HOperatorSet.DistancePl(row2_t, col2_t, l1_row1, l1_col1, l1_row2, l1_col2, out distance);
                        HOperatorSet.ProjectionPl(row1_disp, col1_disp, l1_row1, l1_col1, l1_row2, l1_col2, out row2_disp, out col2_disp);
                        break;


                    default: distance = 0; break;
                }
                
                ActualValue = distance;




                return new LineValue() { row1 = row1_disp, row2 = row2_disp, col1 = col1_disp, col2 = col2_disp };
            }
            else
                return null;
        }
        public double CalculateDistanceOriginal(ref LineValue line_distance, HHomMat2D translate)
        {

            if (upper_edge.last_result & lower_edge.last_result)
            {
                HHomMat2D calib_trans = new HHomMat2D();
                calib_trans = calib_trans.HomMat2dScaleLocal(1 / calib.ScaleY, 1 / calib.ScaleX);
                HTuple row1, col1, row2, col2;


                row1 = lower_edge.e_row; col1 = lower_edge.e_col;
                row2 = upper_edge.e_row; col2 = upper_edge.e_col;
                LineValue line1 = lower_edge.last_line;
                LineValue line2 = upper_edge.last_line;
                HTuple distance;
                HTuple row1_disp = 0, row2_disp = 0, col1_disp = 0, col2_disp = 0;
                switch (Mode)
                {
                    case "PointToPoint":
                        HTuple lower_edge_row_trans, lower_edge_col_trans;
                        calib_trans.AffineTransPixel(row1, col1, out lower_edge_row_trans, out lower_edge_col_trans);
                        HTuple upper_edge_row_trans, upper_edge_col_trans;
                        calib_trans.AffineTransPixel(row2, col2, out upper_edge_row_trans, out upper_edge_col_trans);
                        HOperatorSet.DistancePp(lower_edge_row_trans, lower_edge_col_trans, upper_edge_row_trans, upper_edge_col_trans, out distance);
                        row1_disp = (line1.row1 + line1.row2) / 2;
                        row2_disp = (line2.row1 + line2.row2) / 2;
                        col1_disp = (line1.col1 + line1.col2) / 2;
                        col2_disp = (line2.col1 + line2.col2) / 2;
                        break;
                    case "PointToLine":
                        HTuple row1_t, col1_t, l2_row1, l2_col1, l2_col2, l2_row2;
                        calib_trans.AffineTransPixel(row1, col1, out row1_t, out col1_t);
                        calib_trans.AffineTransPixel(line2.row1, line2.col1, out l2_row1, out l2_col1);
                        calib_trans.AffineTransPixel(line2.row2, line2.col2, out l2_row2, out l2_col2);
                        HOperatorSet.DistancePl(row1_t, col1_t, l2_row1, l2_col1, l2_row2, l2_col2, out distance);

                        row1_disp = (line1.row1 + line1.row2) / 2;
                        col1_disp = (line1.col1 + line1.col2) / 2;
                        HOperatorSet.ProjectionPl(row1_disp, col1_disp, line2.row1, line2.col1, line2.row2, line2.col2, out row2_disp, out col2_disp);
                        //HOperatorSet.DistancePl(lower_edge.Row, lower_edge.Col, upper_edge.last_line.row1, upper_edge.last_line.col1, upper_edge.last_line.row2, upper_edge.last_line.col2, out distance);


                        break;
                    case "LineToPoint":
                        HTuple row2_t, col2_t, l1_row1, l1_col1, l1_col2, l1_row2;
                        calib_trans.AffineTransPixel(row2, col2, out row2_t, out col2_t);
                        calib_trans.AffineTransPixel(line1.row1, line1.col1, out l1_row1, out l1_col1);
                        calib_trans.AffineTransPixel(line1.row2, line1.col2, out l1_row2, out l1_col2);

                        HOperatorSet.DistancePl(row2_t, col2_t, l1_row1, l1_col1, l1_row2, l1_col2, out distance);
                        row1_disp = (line2.row1 + line2.row2) / 2;
                        col1_disp = (line2.col1 + line2.col2) / 2;
                        HOperatorSet.DistancePl(row2_t, col2_t, l1_row1, l1_col1, l1_row2, l1_col2, out distance);
                        HOperatorSet.ProjectionPl(row1_disp, col1_disp, l1_row1, l1_col1, l1_row2, l1_col2, out row2_disp, out col2_disp);
                        // HOperatorSet.DistancePl(upper_edge.Row, upper_edge.Col, lower_edge.last_line.row1, lower_edge.last_line.col1, lower_edge.last_line.row2, lower_edge.last_line.col2, out distance);


                        break;


                    default: distance = 0; break;
                }
                //HTuple row1_trans, col1_trans, row2_trans, col2_trans;
                //translate.AffineTransPixel(row1,col1, out row1_trans, out col1_trans);
                //translate.AffineTransPixel(row2, col2, out row2_trans, out col2_trans);
                line_distance.row1 = row1_disp;
                line_distance.col1 = col1_disp;
                line_distance.row2 = row2_disp;
                line_distance.col2 = col2_disp;
                ActualValue = distance;

                return distance;
            }
            else
            {
                line_distance.row1 = null;
                ActualValue = 0;
                return 0;
            }
        }
    }

}
