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
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Fixture","Edge Alignment", Icon: "Designer/icons/icons8-abscissa-96.png",sortIndex:2)]
    public class FindEdge2Node : BaseNode
    {
        #region properties
        public ObservableCollection<EdgeFinder> edges;
        public override void Save(HFile file)
        {
            (new HTuple(Name)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(edges.Count)).SerializeTuple().FwriteSerializedItem(file);
            foreach (EdgeFinder edge in edges)
            {
                edge.Save(file);
            }
            //imagesource name


        }
        public override void Load(DeserializeFactory item)
        {
            Name = item.DeserializeTuple();
            HTuple count = item.DeserializeTuple();
            edges.Clear();
            for (int i = 0; i < count; i++)
            {
                edges.Add(new EdgeFinder(item));
            }
        }
        #endregion
        static FindEdge2Node()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<FindEdge2Node>));
        }
        [HMIProperty("Find Edge Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<HHomMat2D> FixtureOutput { get; }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    if (ImageInput.Value != null)
                    {
                        if (ImageInput.Value != null)
                        {
                            if (ImageInput.Value.IsInitialized())
                            {
                                EdgeAlignmentEditorWindow wd = new EdgeAlignmentEditorWindow(edges, ImageInput.Value.CopyImage());
                                wd.ShowDialog();
                            }
                        }

                        
                    }
                    break;
            }
        }
        public override void Run(object context)
        {
            FixtureOutput.OnNext(RunInside(ImageInput.Value, FixtureInput.Value,context as InspectionContext));
        }
        public HHomMat2D RunInside(HImage image, HHomMat2D fixture,InspectionContext e)
        {
            if (image == null)
                return null;
            double XMove = 0, YMove = 0;
            HHomMat2D translate;
            if (fixture != null)
            {
                translate = fixture.Clone();
            }
            else
            {
                translate = new HHomMat2D();
            }
            
            HImage gray = image.Rgb1ToGray();
            foreach (EdgeFinder edge in edges)
            {
                EdgeFinderResult result = edge.Run(gray, XMove, YMove, translate);

                translate = result.translate;

                if (result.IsFound)
                {
                    HRegion rec = new HRegion();
                    rec.GenCircle(result.Row, result.Col, 6);
                    e.inspection_result.AddRegion(rec, edge.EdgeColor);
                    HXLDCont cross = new HXLDCont();
                    cross.GenCrossContourXld(0.5 * ((int)result.Row / 0.5), 0.5 * (int)(result.Col / 0.5), 30, 0);
                    //e.inspection_result.AddDisplay(cross, edge.EdgeColor);


                }
                else
                {
                    HRegion rec = new HRegion();
                    rec.GenCircle(edge.Row + YMove, edge.Col + XMove, 20);
                    //e.inspection_result.AddDisplay(rec, "red");

                }
                XMove = result.XMove;
                YMove = result.YMove;
            }
            
            return translate;
        }
        public FindEdge2Node(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            //create dir
            this.Name = "Edge Alignment";

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

            FixtureOutput = new ValueNodeOutputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "Fixture"
                //alue = Observable.Return((HHomMat2D)null)
                //Value = this.WhenAnyValue(vm => vm.ImageInput.Value, vm => vm.FixtureInput.Value).ObserveOn(Scheduler.Default)
                //    .Select(name =>
                //    {


                //        return RunInside(name.Item1, name.Item2);
                //    })

            };


            this.Outputs.Add(FixtureOutput);
            edges = new ObservableCollection<EdgeFinder>();
            //load model if exist
            //LoadRecipe(datadir);
        }
    }

    public class EdgeFinderResult
    {
        public bool IsFound;
        public double Row;
        public double Col;
        public double XMove;
        public double YMove;
        public HHomMat2D translate;
        public EdgeFinderResult(bool IsFound, double Row, double Col, double XMove, double YMove, HHomMat2D translate)
        {
            this.IsFound = IsFound;
            this.Row = Row;
            this.Col = Col;
            this.XMove = XMove;
            this.YMove = YMove;
            this.translate = translate;
        }
    }
    public class EdgeFinder : INotifyPropertyChanged
    {
        public void AdaptToImageSize(int w, int h)
        {
            edge_measure.GenMeasureRectangle2(Row, Col, Phi, Length1, Length2, w, h, (HTuple)"nearest_neighbor");
            Width = w; Height = h;
        }
        string _edge_color = "#0000ffff";
        public string EdgeColor
        {
            get
            {
                return _edge_color;
            }
            set
            {
                if (_edge_color != value)
                {
                    _edge_color = value;
                    ParameterChanged?.Invoke(this, true);
                    RaisePropertyChanged("EdgeColor");
                }
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void UpdateProperties(EdgeFinder sender, object e);
        public UpdateProperties ParameterChanged;
        double _amp;
        public double Amp
        {
            get
            {
                return _amp;
            }
            set
            {
                if (_amp != value)
                {
                    _amp = value;
                    RaisePropertyChanged("Amp");
                }
            }
        }

        public void UpdateMeasure(int w, int h, HImage image, HWindow display)
        {
            //display.ClearWindow();
            lock (edge_measure)
            {
                edge_measure.GenMeasureRectangle2(Row, Col, Phi, Length1, Length2, w, h, (HTuple)"nearest_neighbor");

                HTuple row1, col1, amp1, intdis;
                try
                {
                    edge_measure.MeasurePos(image, Sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                    if (row1.Type != HTupleType.EMPTY)
                    {
                        OriginalRow = row1[0];
                        OriginalCol = col1[0];
                        display.ClearWindow();
                        display.SetColor(_edge_color);
                        double disp_col1 = col1 + Length2;
                        double disp_col2 = col1 - Length2;
                        HHomMat2D trans = new HHomMat2D();
                        HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);
                        HTuple trans_col1, trans_row1, trans_col2, trans_row2;
                        rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
                        rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
                        last_line.row1 = trans_row1;
                        last_line.col1 = trans_col1;
                        last_line.row2 = trans_row2;
                        last_line.col2 = trans_col2;
                        Amp = amp1;
                        // display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
                        // display.DispCross(OriginalRow, OriginalCol, 12, 0);

                    }
                    else
                    {
                        last_line.row1 = null;
                        last_line.col1 = null;
                        last_line.row2 = null;
                        last_line.col2 = null;
                        Amp = 0;
                        //OriginalRow = param[0];
                        //OriginalCol = param[1];
                    }
                }
                catch (Exception ex)
                {
                    last_line.row1 = null;
                    last_line.col1 = null;
                    last_line.row2 = null;
                    last_line.col2 = null;
                    Amp = 0;
                }
            }
        }
        public int range = 0;
        public List<SeriesPoint> GetLineProfile(HImage image)
        {
            List<SeriesPoint> points = new List<SeriesPoint>();
            try
            {
                HTuple point;
                lock (edge_measure)
                {
                    point = edge_measure.MeasureProjection(image);
                }
                range = point.Length;
                HTuple funtion, smooth, devi;
                HOperatorSet.CreateFunct1dArray(point, out funtion);
                HOperatorSet.SmoothFunct1dGauss(funtion, Sigma, out smooth);
                HOperatorSet.DerivateFunct1d(smooth, "first", out devi);
                double scale = Sigma * Math.Sqrt(2 * Math.PI);
                for (int i = 0; i < point.Length; i++)
                {
                    HTuple yvalue;
                    HOperatorSet.GetYValueFunct1d(devi, i, "constant", out yvalue);
                    points.Add(new SeriesPoint(i, yvalue * scale));
                }
            }
            catch (Exception ex)
            {
                return new List<SeriesPoint>();
            }
            return points;
        }
        public void UpdateMeasure(HTuple param, int w, int h, HImage image, HWindow display)
        {
            lock (edge_measure)
            {
                edge_measure.GenMeasureRectangle2((HTuple)param[0], param[1], param[2], param[3], param[4], w, h, (HTuple)"nearest_neighbor");
            }
            Row = param[0];
            Col = param[1];
            Phi = param[2];
            Length1 = param[3];
            Length2 = param[4];
            HTuple row1, col1, amp1, intdis;
            try
            {
                lock (edge_measure)
                {
                    edge_measure.MeasurePos(image, Sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                }
                if (row1.Type != HTupleType.EMPTY)
                {
                    OriginalRow = row1[0];
                    OriginalCol = col1[0];
                    //display.ClearWindow();
                    //display.SetColor(_edge_color);
                    double disp_col1 = col1 + Length2;
                    double disp_col2 = col1 - Length2;
                    HHomMat2D trans = new HHomMat2D();
                    HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);
                    HTuple trans_col1, trans_row1, trans_col2, trans_row2;
                    rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
                    rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
                    //display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
                    //display.DispCross(OriginalRow, OriginalCol, 12, 0);
                    last_line.row1 = trans_row1;
                    last_line.col1 = trans_col1;
                    last_line.row2 = trans_row2;
                    last_line.col2 = trans_col2;
                    Amp = amp1;
                }
                else
                {
                    OriginalRow = param[0];
                    OriginalCol = param[1];
                    last_line.row1 = null;
                    last_line.col1 = null;
                    last_line.row2 = null;
                    last_line.col2 = null;
                    Amp = 0;
                }
            }
            catch (Exception ex)
            {
                last_line.row1 = null;
                last_line.col1 = null;
                last_line.row2 = null;
                last_line.col2 = null;
                Amp = 0;
            }

        }
        public void DisplayLastGraphic(HWindow display)
        {
            if (trans_col1 == null)
            {
                display.ClearWindow();
            }
            else
            {
                display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
                display.DispCross(OriginalRow, OriginalCol, 12, 0);
            }
        }
        HTuple trans_col1, trans_row1, trans_col2, trans_row2;
        public bool last_result;
        public LineValue last_line = new LineValue();
        public void DispLineResult(double row1, double col1, HWindow display, ref LineValue line)
        {
            double disp_col1 = col1 + Length2;
            double disp_col2 = col1 - Length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

            rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
            display.SetColor("green");
            display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
            line.row1 = trans_row1;
            line.row2 = trans_row2;
            line.col1 = trans_col1;
            line.col2 = trans_col2;
        }
        public void DispLineResult(double row1, double col1, InspectionContext e, ref LineValue line)
        {
            double disp_col1 = col1 + Length2;
            double disp_col2 = col1 - Length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

            rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
            HXLDCont line_disp = new HXLDCont(new HTuple(trans_row1, trans_row2), new HTuple(trans_col1, trans_col2));
            e.inspection_result?.AddDisplay(line_disp, EdgeColor);
            //display.SetColor("green");
            //display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
            line.row1 = trans_row1;
            line.row2 = trans_row2;
            line.col1 = trans_col1;
            line.col2 = trans_col2;
        }
        public bool ManualUpdateMeasureWithResult(HTuple param, HHomMat2D translate, int w, int h, HImage image, HWindow display, ref LineValue lineValue)
        {
            HTuple row_tr, col_tr, phi_tr;
            translate.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row_tr, out col_tr);

            HTuple row1 = param[0];
            HTuple col1 = param[1];
            Row = row_tr;
            Col = col_tr;
            Phi = param[2];
            Length1 = param[3];
            Length2 = param[4];
            // translate.HomMat2dInvert().AffineTransPixel(param[0], param[1], out _row, out _col);
            e_row = row1;
            e_col = col1;
            //  OriginalRow = row1[0];
            //  OriginalCol = col1[0];
            //display.ClearWindow();
            //display.SetColor(_edge_color);
            double disp_col1 = col1 + Length2;
            double disp_col2 = col1 - Length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

            rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
            lineValue.row1 = trans_row1;
            lineValue.col1 = trans_col1;
            lineValue.row2 = trans_row2;
            lineValue.col2 = trans_col2;
            // display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
            //display.DispCross(OriginalRow, OriginalCol, 12, 0);
            last_result = true;
            last_line = lineValue;
            return true;

        }
        public double e_row, e_col;
        public bool UpdateMeasureWithResult(HTuple param, HHomMat2D translate, int w, int h, HImage image, HWindow display, ref LineValue lineValue)
        {
            HTuple row_tr, col_tr, phi_tr;
            translate.HomMat2dInvert().AffineTransPixel(param[0], param[1], out row_tr, out col_tr);

            //translate.HomMat2dInvert()
            lock (edge_measure)
            {
                edge_measure.GenMeasureRectangle2((HTuple)row_tr, col_tr, param[2], param[3], param[4], w, h, (HTuple)"nearest_neighbor");
            }
            Row = row_tr;
            Col = col_tr;
            Phi = param[2];
            Length1 = param[3];
            Length2 = param[4];
            HTuple row1, col1, amp1, intdis;
            try
            {
                HTuple row_trans, col_trans;
                translate.AffineTransPixel(this.Row, this.Col, out row_trans, out col_trans);
                lock (edge_measure)
                {
                    edge_measure.TranslateMeasure(row_trans, col_trans);

                    edge_measure.MeasurePos(image, Sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                }
                if (row1.Type != HTupleType.EMPTY)
                {
                    //translate.HomMat2dInvert().AffineTransPixel(row1[0], col1[0], out _row, out _col);
                    e_row = row1[0];
                    e_col = col1[0];
                    //  OriginalRow = row1[0];
                    //  OriginalCol = col1[0];
                    //display.ClearWindow();
                    //display.SetColor(_edge_color);
                    double disp_col1 = col1 + Length2;
                    double disp_col2 = col1 - Length2;
                    HHomMat2D trans = new HHomMat2D();
                    HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

                    rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
                    rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
                    lineValue.row1 = trans_row1;
                    lineValue.col1 = trans_col1;
                    lineValue.row2 = trans_row2;
                    lineValue.col2 = trans_col2;
                    // display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
                    //display.DispCross(OriginalRow, OriginalCol, 12, 0);
                    last_result = true;
                    last_line = lineValue;
                    return true;

                }
                else
                {
                    trans_col1 = trans_row1 = trans_col2 = trans_row2 = null;
                    lineValue.row1 = null;
                    lineValue.col1 = null;
                    lineValue.row2 = null;
                    lineValue.col2 = null;
                    translate.HomMat2dInvert().AffineTransPixel(param[0], param[1], out _original_row, out _original_col);
                    last_line = lineValue;
                    last_result = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                lineValue.row1 = null;
                lineValue.col1 = null;
                lineValue.row2 = null;
                lineValue.col2 = null;
                last_result = false;
                last_line = lineValue;
                return false;
            }

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
        public override string ToString()
        {
            return Name;
        }

        double _original_row;
        public double OriginalRow
        {
            get
            {
                return _original_row;
            }
            set
            {
                if (_original_row != value)
                {
                    _original_row = value;
                    RaisePropertyChanged("OriginalRow");
                }
            }
        }
        double _original_col;
        public double OriginalCol
        {
            get
            {
                return _original_col;
            }
            set
            {
                if (_original_col != value)
                {
                    _original_col = value;
                    RaisePropertyChanged("OriginalCol");
                }
            }
        }
        public HMeasure edge_measure;

        double _row;
        public double Row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row != value)
                {
                    _row = value;
                    RaisePropertyChanged("Row");
                }
            }
        }
        double _col;
        public double Col
        {
            get
            {
                return _col;
            }
            set
            {
                if (_col != value)
                {
                    _col = value;
                    RaisePropertyChanged("Col");
                }
            }
        }
        double _phi;
        public double Phi
        {
            get
            {
                return _phi;
            }
            set
            {
                if (_phi != value)
                {
                    _phi = value;
                    RaisePropertyChanged("Phi");
                }
            }
        }
        double _length1;
        public double Length1
        {
            get
            {
                return _length1;
            }
            set
            {
                if (_length1 != value)
                {
                    _length1 = value;
                    RaisePropertyChanged("Length1");
                }
            }
        }
        double _length2;
        public double Length2
        {
            get
            {
                return _length2;
            }
            set
            {
                if (_length2 != value)
                {
                    _length2 = value;
                    RaisePropertyChanged("Length2");
                }
            }
        }

        double _width;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    RaisePropertyChanged("Width");
                }
            }
        }
        double _height;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    RaisePropertyChanged("Height");
                }
            }
        }
        double _sigma = 1;
        public double Sigma
        {
            get
            {
                return _sigma;
            }
            set
            {
                if (value < (Length1) / 4)
                {
                    if (_sigma != value)
                    {

                        _sigma = value;

                        ParameterChanged?.Invoke(this, null);
                        RaisePropertyChanged("Sigma");
                    }
                }
                else
                {
                    _sigma = (int)((Length1) / 4);
                    ParameterChanged?.Invoke(this, null);
                    RaisePropertyChanged("Sigma");
                }

            }
        }
        double _threshold;
        public double Threshold
        {
            get
            {
                return _threshold;
            }
            set
            {
                if (_threshold != value)
                {
                    _threshold = value;
                    ParameterChanged?.Invoke(this, null);
                    RaisePropertyChanged("Threshold");
                }
            }
        }
        string _transition;
        public string Transition
        {
            get
            {
                return _transition;
            }
            set
            {
                if (_transition != value)
                {
                    _transition = value;
                    ParameterChanged?.Invoke(this, null);
                    RaisePropertyChanged("Transition");
                }
            }
        }
        string _select;
        public string Select
        {
            get
            {
                return _select;
            }
            set
            {
                if (_select != value)
                {
                    _select = value;
                    ParameterChanged?.Invoke(this, null);
                    RaisePropertyChanged("Select");
                }
            }
        }






        public void Save(HFile file)
        {
            (new HTuple("update_manual")).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Name)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Sigma)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Threshold)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Transition)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Select)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Width)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Height)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Row)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Col)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Phi)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Length1)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(Length2)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(OriginalRow)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(OriginalCol)).SerializeTuple().FwriteSerializedItem(file);
            (new HTuple(IsManual)).SerializeTuple().FwriteSerializedItem(file);
            edge_measure.SerializeMeasure().FwriteSerializedItem(file);
        }
        public void Load(DeserializeFactory item)
        {
            string temp = item.DeserializeTuple();
            if (temp == "update_manual")
            {
                Name = item.DeserializeTuple();
                _sigma = item.DeserializeTuple();
                Threshold = item.DeserializeTuple();
                Transition = item.DeserializeTuple();
                Select = item.DeserializeTuple();
                Width = item.DeserializeTuple();
                Height = item.DeserializeTuple();
                Row = item.DeserializeTuple();
                Col = item.DeserializeTuple();
                Phi = item.DeserializeTuple();
                Length1 = item.DeserializeTuple();
                Length2 = item.DeserializeTuple();
                OriginalRow = item.DeserializeTuple();
                OriginalCol = item.DeserializeTuple();
                IsManual = item.DeserializeTuple();
                edge_measure = item.DeserializeMeasure();
                Sigma = _sigma;
            }
            else
            {
                Name = temp;
                _sigma = item.DeserializeTuple();
                Threshold = item.DeserializeTuple();
                Transition = item.DeserializeTuple();
                Select = item.DeserializeTuple();
                Width = item.DeserializeTuple();
                Height = item.DeserializeTuple();
                Row = item.DeserializeTuple();
                Col = item.DeserializeTuple();
                Phi = item.DeserializeTuple();
                Length1 = item.DeserializeTuple();
                Length2 = item.DeserializeTuple();
                OriginalRow = item.DeserializeTuple();
                OriginalCol = item.DeserializeTuple();
                edge_measure = item.DeserializeMeasure();
                Sigma = _sigma;
            }



        }

        public EdgeFinder(int width, int height)
        {

            Name = "Unknow";
            this.Width = width;
            this.Height = height;

            Threshold = 10;
            Row = 500;
            Col = 500;
            Phi = 0;
            Length1 = 100;
            Length2 = 100;
            Transition = "positive";
            Select = "first";
            Sigma = 5;
            edge_measure = new HMeasure(Row, Col, Phi, Length1, Length2, width, height, "nearest_neighbor");

        }
        public EdgeFinder(DeserializeFactory item)
        {

            string temp = item.DeserializeTuple();
            if (temp == "update_manual")
            {
                Name = item.DeserializeTuple();
                _sigma = item.DeserializeTuple();
                Threshold = item.DeserializeTuple();
                Transition = item.DeserializeTuple();
                Select = item.DeserializeTuple();
                Width = item.DeserializeTuple();
                Height = item.DeserializeTuple();
                Row = item.DeserializeTuple();
                Col = item.DeserializeTuple();
                Phi = item.DeserializeTuple();
                Length1 = item.DeserializeTuple();
                Length2 = item.DeserializeTuple();
                OriginalRow = item.DeserializeTuple();
                OriginalCol = item.DeserializeTuple();
                IsManual = item.DeserializeTuple();
                edge_measure = item.DeserializeMeasure();
                Sigma = _sigma;
            }
            else
            {
                Name = temp;
                _sigma = item.DeserializeTuple();
                Threshold = item.DeserializeTuple();
                Transition = item.DeserializeTuple();
                Select = item.DeserializeTuple();
                Width = item.DeserializeTuple();
                Height = item.DeserializeTuple();
                Row = item.DeserializeTuple();
                Col = item.DeserializeTuple();
                Phi = item.DeserializeTuple();
                Length1 = item.DeserializeTuple();
                Length2 = item.DeserializeTuple();
                OriginalRow = item.DeserializeTuple();
                OriginalCol = item.DeserializeTuple();
                edge_measure = item.DeserializeMeasure();
                Sigma = _sigma;
            }
        }
        bool _is_manual = false;
        public bool IsManual
        {
            get
            {
                return _is_manual;
            }
            set
            {
                if (_is_manual != value)
                {
                    _is_manual = value;
                    RaisePropertyChanged("IsManual");
                    ParameterChanged?.Invoke(this, null);

                }
            }
        }

        public void FindEdge(HImage image, HHomMat2D translate, InspectionContext e, out double row_result, out double col_result)
        {
            int w, h;
            image.GetImageSize(out w, out h);
            HTuple row_trans, col_trans;
            translate.AffineTransPixel(this.Row, this.Col, out row_trans, out col_trans);
            if (IsManual)
            {
                row_result = row_trans; col_result = col_trans;

                double disp_col1 = col_trans + Length2;
                double disp_col2 = col_trans - Length2;
                HHomMat2D trans = new HHomMat2D();
                HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row_trans, (double)col_trans);

                rotate.AffineTransPixel(row_trans, disp_col1, out trans_row1, out trans_col1);
                rotate.AffineTransPixel(row_trans, disp_col2, out trans_row2, out trans_col2);

                return;
            }
            if (w == Width && h == Height)
            {
                HTuple row, col, amp, dis;
                row = new HTuple();
                col = new HTuple();

                //some bug throw access violent exception which is cannot catch
                lock (image)
                {
                    edge_measure.TranslateMeasure(row_trans, col_trans);


                    try
                    {

                        edge_measure.MeasurePos(image, Sigma, Threshold, Transition, Select, out row, out col, out amp, out dis);


                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (row.Type != HTupleType.EMPTY)
                {
                    //display.SetColor("blue");
                    //display.DispRectangle2(row_trans.D, col_trans.D, Phi, Length1, Length2);
                    row_result = row;
                    col_result = col;

                    double disp_col1 = col + Length2;
                    double disp_col2 = col - Length2;
                    HHomMat2D trans = new HHomMat2D();
                    HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row, (double)col);

                    rotate.AffineTransPixel(row, disp_col1, out trans_row1, out trans_col1);
                    rotate.AffineTransPixel(col, disp_col2, out trans_row2, out trans_col2);
                    //lineValue.row1 = trans_row1;
                    //lineValue.col1 = trans_col1;
                    //lineValue.row2 = trans_row2;
                    //lineValue.col2 = trans_col2;
                }
                else
                {
                    HRegion rec = new HRegion();
                    rec.GenRectangle2(row_trans.D, col_trans.D, Phi, Length1, Length2);
                    e.inspection_result?.AddDisplay(rec, "red");

                    row_result = -1;
                    col_result = -1;
                }
            }
            else
            {
                //display.SetColor("red");
                //display.DispRectangle2(row_trans.D, col_trans.D, Phi, Length1, Length2);
                AdaptToImageSize(w, h);
                row_result = -1;
                col_result = -1;
            }
        }

        public EdgeFinderResult Run(HImage image, double XMove, double YMove, HHomMat2D translate)
        {
            int w, h;
            image.GetImageSize(out w, out h);
            if (w != Width | h != Height)
            {
                AdaptToImageSize(w, h);
            }
            if (w == Width && h == Height)
            {
                HTuple row, col, amp = 0, dis;
                row = new HTuple();
                col = new HTuple();
                edge_measure.TranslateMeasure(YMove + this.Row, XMove + this.Col);
                try
                {
                    edge_measure.MeasurePos(image, Sigma, Threshold, Transition, Select, out row, out col, out amp, out dis);
                }
                catch (Exception ex)
                {
                    return new EdgeFinderResult(false, 0, 0, 0, 0, translate);
                }
                edge_measure.TranslateMeasure(-YMove + this.Row, -XMove + this.Col);

                if (row.Type != HTupleType.EMPTY)
                {
                    double row_t, col_t;
                    translate.AffineTransPixel(OriginalRow, OriginalCol, out row_t, out col_t);
                    translate = translate.HomMat2dTranslate((row[0].D) - row_t, (col[0].D) - col_t);
                    Amp = amp;
                    return new EdgeFinderResult(true, row[0], col[0], (col[0].D) - col_t, (row[0].D) - row_t, translate);
                }
                else
                {
                    Amp = 0;
                    return new EdgeFinderResult(false, 0, 0, 0, 0, translate);
                }
            }
            else
            {
                return new EdgeFinderResult(false, 0, 0, 0, 0, translate);
            }
        }

    }

}
