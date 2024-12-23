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
using NOVisionDesigner.Designer.Editors;
using System.Windows;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Measurement","Measure Lines", Icon: "Designer/icons/icons8-width-100.png",sortIndex:4)]
    public class MeasureLinesNode : BaseNode
    {
        #region properties
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            new HTuple(LineMeasures.Count).SerializeTuple().FwriteSerializedItem(file);
            foreach (var tool in LineMeasures)
            {
                tool.Save(file);
            }
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            HTuple count = item.DeserializeTuple();
            for (int i = 0; i < count; i++)
            {
                var added = new LineMeasure();
                LineMeasures.Add(added);
                added.Load(item);
            }
            Inputs.RemoveMany(Inputs.Items.Where(x => (x.PortType == "LineValue[]")&& (x.Name != "Lines Input 1")));
            foreach (var inputItem in inputItems)
            {
                Type itemtype = typeof(LineValue[]);
                Type[] paramType = new Type[] { itemtype };
                Type classType = typeof(ValueNodeInputViewModel<>);
                Type consType = classType.MakeGenericType(paramType);
                NodeInputViewModel input = (NodeInputViewModel)Activator.CreateInstance(consType, new object[] { null, null });
                input.Name = inputItem.Name;
                input.PortType = inputItem.ItemType;
                Inputs.Add(input);
            }
        }
       

        #endregion
        static MeasureLinesNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MeasureLinesNode>));
        }
        [SerializeIgnore]
        public Control PropertiesView
        {
            get
            {
                return new MeasureLineView(this);
            }
        }
        public ValueNodeInputViewModel<LineValue[]> Lines1 { get; }
        public ValueNodeInputViewModel<HImage> ReferenceImage { get; }
        public ValueNodeInputViewModel<double> ScaleRatio { get; }
        public ValueNodeOutputViewModel<bool[]> Result { get; }
        public ValueNodeOutputViewModel<double[]> Distances { get; }
        public ObservableCollection<LineMeasure> LineMeasures { get; set; } = new ObservableCollection<LineMeasure>();
        public ObservableCollection<IOItem> inputItems { get; set; } = new ObservableCollection<IOItem>();
        [HMIProperty("Measurement Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        [HMIProperty("Add line inputs")]
        public IReactiveCommand OpenLineInputWindow
        {
            get { return ReactiveCommand.Create((Control sender) =>
            {
                IOMeasurementLinesEditor wd = new IOMeasurementLinesEditor(this);
                if (sender != null)
                {
                    wd.Owner = Window.GetWindow(sender);
                }              
                wd.ShowDialog();
            }); }
        }

        [HMIProperty]
        public int GraphicFontSize { get; set; } = 14;
        [HMIProperty]
        public bool DisplayResultTable { get; set; } = true;
        [HMIProperty]
        public bool SetInspectionContext { get; set; } = true;

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    MeasureLinesWindow wd = new MeasureLinesWindow(this);
                    wd.ShowDialog();

                    break;
            }
        }
        public override void Run(object context)
        {            
            var result = RunInside(MergeLines().ToArray(), ScaleRatio.Value, (InspectionContext)context);
            if (SetInspectionContext)
            {
                if (result.Any(x => x.Result == false))
                {
                    (context as InspectionContext).result = false;
                }
            }
            
            Result.OnNext(result.Select(x=>x.Result).ToArray());
            Distances.OnNext(result.Select(x => x.Distance.Value).ToArray());
        }
        public LineValue[] MergeLines()
        {

            LineValue[] merge_lines = new LineValue[] { };
            var lineValue_inputs = Inputs.Items.Where(x => x.PortType == "LineValue[]");
            foreach (ValueNodeInputViewModel<LineValue[]> lineValue_input in lineValue_inputs)
            {
                LineValue[] line;
                if (lineValue_input.Value == null)
                {
                    line = new LineValue[0];
                }
                else line = lineValue_input.Value;
                merge_lines = merge_lines.Concat(line).ToArray();
            }
            //LineValue[] line1, line2;
            //if (Lines1.Value == null)
            //{
            //    line1 = new LineValue[0];
            //}
            //else
            //{
            //    line1 = Lines1.Value;
            //}
            //if (Lines2.Value == null)
            //{
            //    line2 = new LineValue[0];
            //}
            //else
            //{
            //    line2 = Lines2.Value;
            //}
            //var merge_lines = line1.Concat(line2).ToArray();
            return merge_lines;

        }
        public LineMeasureResult[] RunInside(LineValue[] merge_lines, double scale_ratio, InspectionContext e)
        {
            LineMeasureResult[] result = new LineMeasureResult[LineMeasures.Count];
            for(int i = 0; i < LineMeasures.Count; i++)
            {
                var measure = LineMeasures[i];
                if (measure.IsEnabled )
                {
                    if(measure.Index1 < merge_lines.Length & measure.Index2 < merge_lines.Length)
                    {
                        var distance = measure.Run(merge_lines[measure.Index1], merge_lines[measure.Index2], e, scale_ratio, ShowDisplay,GraphicFontSize) ?? 0.0D;
                        if (distance >= measure.LowerValue & distance <= measure.UpperValue)
                        {
                            result[i].Result = true;
                            result[i].Distance = distance;
                            if (DisplayResultTable)
                            {
                                e.SetResult(measure.MeasureName, true);
                            }
                            


                        }
                        else
                        {
                            
                            result[i].Result = false;
                            result[i].Distance = distance;
                            if (DisplayResultTable)
                            {
                                e.SetResult(measure.MeasureName, false);
                            }
                        }
                    }
                    else
                    {
                        result[i].Result = false;
                        result[i].Distance = 0;
                        if (DisplayResultTable)
                        {
                            e.SetResult(measure.MeasureName, false);
                        }
                    }
                    
                }
                else
                {
                    result[i].Result = true;
                    result[i].Distance = 0;
                }
                
                
            }
            return result;
            //return true;
        }
        public MeasureLinesNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            //create dir


            this.Name = "Measure Lines";
            ReferenceImage = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Reference Image",
                PortType = "HImage"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(ReferenceImage);
            ScaleRatio = new ValueNodeInputViewModel<double>()
            {
                Name = "Scale Ratio",
                PortType = "double",
                Editor = new DoubleValueEditorViewModel()
            };
            this.Inputs.Add(ScaleRatio);
            Lines1 = new ValueNodeInputViewModel<LineValue[]>()
            {
                Name = "Lines Input 1",
                PortType = "LineValue[]"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(Lines1);
            Result = new ValueNodeOutputViewModel<bool[]>()
            {
                Name = "Result",
                PortType = "bool[]"
            };
            this.Outputs.Add(Result);
            Distances = new ValueNodeOutputViewModel<double[]>()
            {
                Name = "Distances",
                PortType = "double[]",
                
            };
            this.Outputs.Add(Distances);
        }
    }
    public struct LineMeasureResult
    {
        public bool Result { get; set; }
        public double? Distance { get; set; }
    }
    public class LineMeasure : INotifyPropertyChanged
    {

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
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

        public LineMeasure()
        {

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
        int _display_offset=0;
        public int DisplayOffset
        {
            get
            {
                return _display_offset;
            }
            set
            {
                if (_display_offset != value)
                {
                    _display_offset = value;
                    RaisePropertyChanged("DisplayOffset");
                }
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
        public (double,double,double,double) GenParallelLine(double x1,double x2,double y1,double y2,double offsetPixels)
        {
            var L = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

            // This is the second line
            var x1p = x1 + offsetPixels * (y2 - y1) / L;
            var x2p = x2 + offsetPixels * (y2 - y1) / L;
            var y1p = y1 + offsetPixels * (x1 - x2) / L;
            var y2p = y2 + offsetPixels * (x1 - x2) / L;
            return (x1p,x2p,y1p,y2p);
        }
        public double? Run( LineValue line1,LineValue line2, InspectionContext e, double scale_ratio, bool showDisplay=true,int Fontsize=14)
        {
            if (line1!=null&line2!=null)
            {
                var scale_x = e.inspection_result.scale_x;
                var scale_y = e.inspection_result.scale_y;
                HHomMat2D calib_trans = new HHomMat2D();
                calib_trans = calib_trans.HomMat2dScaleLocal(1 / scale_y, 1 / scale_x);

                HTuple distance;
                HTuple row1_disp =0, row2_disp=0, col1_disp=0, col2_disp=0;
                switch (Mode)
                {
                    case "PointToPoint":
                        HTuple lower_edge_row_trans, lower_edge_col_trans;
                        calib_trans.AffineTransPixel((line1.row1+line1.row2)/2, (line1.col1+line1.col2)/2, out lower_edge_row_trans, out lower_edge_col_trans);
                        HTuple upper_edge_row_trans, upper_edge_col_trans;
                        calib_trans.AffineTransPixel((line2.row1+line2.row2)/2, (line2.col1+line2.col2)/2, out upper_edge_row_trans, out upper_edge_col_trans);
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
                distance = scale_ratio * distance;
                ActualValue = distance;

                if (DisplayOffset != 0)
                {
                    (col1_disp,col2_disp,row1_disp,row2_disp) =  GenParallelLine(col1_disp, col2_disp, row1_disp, row2_disp, DisplayOffset);
                }


                var disp_color = _color;
                var background = "#ffffffaa";
                var foreground = "black";
                if (distance > LowerValue & distance < UpperValue)
                {

                }
                else
                {
                    disp_color = "red";
                    background = "#ff0000aa";
                    foreground = "white";
                }
                if (showDisplay)
                {
                    e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1_disp, row2_disp), new HTuple(col1_disp, col2_disp)), disp_color);

                    e.inspection_result?.AddText(Math.Round(distance.D, 2).ToString(), foreground, background, (row1_disp + row2_disp) / 2, (col1_disp + col2_disp) / 2, Fontsize);
                    // display.DispText(Math.Round( distance.D,2).ToString(), "image", (row1 + row2) / 2, (col1 + col2) / 2, "black", "box_color", "#ffffffaa");
                }

                return distance;
            }
            else
                return null;
        }
        int _index1;
        public int Index1
        {
            get
            {
                return _index1;
            }
            set
            {
                if (_index1 != value)
                {
                    _index1 = value;
                    RaisePropertyChanged("Index1");
                }
            }
        }
        int _index2;
        public int Index2
        {
            get
            {
                return _index2;
            }
            set
            {
                if (_index2 != value)
                {
                    _index2 = value;
                    RaisePropertyChanged("Index2");
                }
            }
        }


    }

}
