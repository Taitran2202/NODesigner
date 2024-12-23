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
    [NodeInfo("Measurement","Find Lines", Icon: "Designer/icons/icons8-diagonal-lines-80.png")]
    public class FindLinesNode : BaseNode
    {
        #region properties
        public ObservableCollection<LineFinder> edges = new ObservableCollection<LineFinder>();
        public override void Save(HFile file)
        {
            (new HTuple(Name)).SerializeTuple().FwriteSerializedItem(file);

            (new HTuple(edges.Count)).SerializeTuple().FwriteSerializedItem(file);
            foreach (LineFinder edge in edges)
            {
                edge.Save(file);
            }
            //imagesource name


        }
        [HMIProperty("Find Lines Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        public override void Load(DeserializeFactory item)
        {
            Name = item.DeserializeTuple();
            HTuple count = item.DeserializeTuple();
            edges.Clear();
            for (int i = 0; i < count; i++)
            {
                edges.Add(new LineFinder(item));
            }
        }
        #endregion
        static FindLinesNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<FindLinesNode>));
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HHomMat2D> FixtureInput { get; }
        public ValueNodeOutputViewModel<LineValue[]> Lines { get; }

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    if (ImageInput.Value != null)
                    {
                        HImage image=null;
                        HHomMat2D fixture=null;
                        try
                        {
                            if (ImageInput.Value.IsInitialized())
                            {
                                image = ImageInput.Value.Clone();
                            }
                            if (FixtureInput.Value != null)
                            {
                                fixture = FixtureInput.Value.Clone();
                            }
                            else
                            {
                                fixture = new HHomMat2D();
                            }
                        }catch(Exception ex)
                        {

                        }
                        if(image != null)
                        {
                            FindLinesWindow wd = new FindLinesWindow(edges, image, fixture);
                            wd.ShowDialog();
                        }
                        
                    }
                    break;
            }
        }
        public override void Run(object context)
        {
            var channels = ImageInput.Value.CountChannels();
            if (channels == 4)
            {
                var im1 = ImageInput.Value.Decompose4(out HImage im2, out HImage im3, out HImage im4);
                var image = im1.Compose3(im2, im3);
                Lines.OnNext(RunInside(image, FixtureInput.Value, context as InspectionContext));
            }
            else
            {
                Lines.OnNext(RunInside(ImageInput.Value, FixtureInput.Value, context as InspectionContext));
            }
            
        }
        public static LineValue GenLine(double row1,double col1,double length1,double length2,double phi)
        {
            
            double trans_row1, trans_col1, trans_row2, trans_col2;
            double disp_col1 = col1 + length2;
            double disp_col2 = col1 - length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dRotate((double)phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

            rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
            //display.SetColor("green");
            //display.DispLine(trans_row1, trans_col1, trans_row2, trans_col2);
            return new LineValue()
            {
                row1 = trans_row1,
                row2 = trans_row2,
                col1 = trans_col1,
                col2 = trans_col2
            };

        }
       
        public LineValue[] RunInside(HImage image, HHomMat2D fixture,InspectionContext e)
        {
            double displaySize = 10;
            if (image == null)
                return null;
            LineValue[] lineValues = new LineValue[edges.Count];
            HHomMat2D translate;
            HImage gray = image.Rgb1ToGray();
            if (fixture != null)
            {
                translate = fixture.Clone();
            }
            else
            {
                translate = new HHomMat2D();
            }
            for (int i=0;i<edges.Count;i++)
            {

                var line = edges[i].Run(gray, translate);
                if (line.IsFound)
                {
                    //HRegion rect = new HRegion();
                    //rect.GenRectangle2(edges[i].Row + tx, edges[i].Col + ty, edges[i].Phi + phi, edges[i].Length1, edges[i].Length2);
                    //e.inspection_result.AddDisplay(rect, "yellow");
                    //HRegion rect1 = new HRegion();
                    //rect1.GenRectangle2(edges[i].Row, edges[i].Col, edges[i].Phi, edges[i].Length1, edges[i].Length2);
                    //rect1 = rect1.AffineTransRegion(translate, "nearest_neighbor");
                    //e.inspection_result.AddDisplay(rect1, "green");
                    HRegion rec = new HRegion();
                    rec.GenCircle((line.Line.row1+ line.Line.row2)/2, (line.Line.col1 + line.Line.col2) / 2, displaySize);
                    e.inspection_result.AddDisplay(rec, edges[i].EdgeColor);
                    HXLDCont cross = new HXLDCont();
                    cross.GenCrossContourXld((line.Line.row1 + line.Line.row2) / 2, (line.Line.col1 + line.Line.col2) / 2, displaySize, 3.14/4);
                    e.inspection_result.AddDisplay(cross, edges[i].EdgeColor);
                    lineValues[i] = line.Line;
                   
                }
                else
                {
                    //HRegion rect = new HRegion();
                    //rect.GenRectangle2(edges[i].Row + tx, edges[i].Col + ty, edges[i].Phi + phi, edges[i].Length1, edges[i].Length2);
                    //e.inspection_result.AddDisplay(rect, "yellow");
                    //HRegion rect1 = new HRegion();
                    //rect1.GenRectangle2(edges[i].Row, edges[i].Col, edges[i].Phi, edges[i].Length1, edges[i].Length2);
                    //rect1=rect1.AffineTransRegion(translate, "nearest_neighbor");
                    //e.inspection_result.AddDisplay(rect1, "green");
                    HRegion rec = new HRegion();
                    rec.GenCircle((line.Line.row1 + line.Line.row2) / 2, (line.Line.col1 + line.Line.col2) / 2, displaySize);
                    lineValues[i] = null;
                    e.inspection_result.AddDisplay(rec, "red");
                    
                }
            }
            
            return lineValues;
        }
        public FindLinesNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            //create dir
            this.Name = "Find Lines";

            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "ImageInput",
                PortType = "HImage"
                //Editor = new StringValueEditorViewModel()
            };
            this.Inputs.Add(ImageInput);

            FixtureInput = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
                //Editor = new IntegerValueEditorViewModel()
            };
            this.Inputs.Add(FixtureInput);

            Lines = new ValueNodeOutputViewModel<LineValue[]>()
            {
                Name = "Lines",
                PortType = "LineValue[]"
            };

            this.Outputs.Add(Lines);
        }
    }

    public class LineFinder : INotifyPropertyChanged
    {
        public void AdaptToImageSize(int w, int h)
        {
            Finder.GenMeasureRectangle2(Row, Col, Phi, Length1, Length2, w, h, (HTuple)"nearest_neighbor");
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
        public delegate void UpdateProperties(LineFinder sender, object e);
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
            lock (Finder)
            {
                Finder.GenMeasureRectangle2(Row, Col, Phi, Length1, Length2, w, h, (HTuple)"nearest_neighbor");

                HTuple row1, col1, amp1, intdis;
                try
                {
                    Finder.MeasurePos(image, Sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                    if (row1.Type != HTupleType.EMPTY)
                    {
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
            if (Finder == null)
            {
                return points;
            }
            try
            {
                HTuple point;
                lock (Finder)
                {
                    point = Finder.MeasureProjection(image);
                }
                range = point.Length;
                HTuple funtion, smooth, devi;
                HOperatorSet.CreateFunct1dArray(point, out funtion);
                HOperatorSet.NumPointsFunct1d(funtion, out HTuple length);
                double sigma = Math.Min((length - 2) / 7.8, Sigma);
                HOperatorSet.SmoothFunct1dGauss(funtion, sigma, out smooth);
                HOperatorSet.DerivateFunct1d(smooth, "first", out devi);
                double scale = sigma * Math.Sqrt(2 * Math.PI);
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
            lock (Finder)
            {
                Finder.GenMeasureRectangle2((HTuple)param[0], param[1], param[2], param[3], param[4], w, h, (HTuple)"nearest_neighbor");
            }

            Row = param[0];
            Col = param[1];
            Phi = param[2];
            Length1 = param[3];
            Length2 = param[4];
            HTuple row1, col1, amp1, intdis;
            try
            {
                lock (Finder)
                {
                    Finder.MeasurePos(image, Sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                }
                Row = param[0];
                Col = param[1];
                if (row1.Type != HTupleType.EMPTY)
                {
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
                }
                else
                {

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
        public bool ManualUpdateMeasureWithResult(HTuple param, HHomMat2D transform, int w, int h, HImage image, HWindow display, ref LineValue lineValue)
        {
            if (transform != null)
            {
                double sx = transform.HomMat2dToAffinePar(out double sy, out double phi, out double theta, out double tx, out double ty);
                Row = param[0] - ty;
                Col = param[1] - tx;
                Phi = param[2] - theta;
                Length1 = param[3];
                Length2 = param[4];

            }
            else
            {
                Row = param[0];
                Col = param[1];
                Phi = param[2];
                Length1 = param[3];
                Length2 = param[4];
            }
            e_row = param[0];
            e_col = param[1];
            double disp_col1 = param[1] + Length2;
            double disp_col2 = param[1] - Length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)param[0], (double)param[1]);

            rotate.AffineTransPixel(param[0], disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(param[0], disp_col2, out trans_row2, out trans_col2);
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
        public bool UpdateMeasureWithResult(HTuple param, HHomMat2D transform, int w, int h, HImage image, HWindow display)
        {
            if (Finder == null)
            {
                return false;
            }
            LineValue lineValue = new LineValue();
            if (transform != null)
            {
                double sx = transform.HomMat2dToAffinePar(out double sy, out double phi, out double theta, out double tx, out double ty);
                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out double rowtrans, out double coltrans);
                Row = rowtrans;
                Col = coltrans;
                Phi = param[2]-phi;
                Length1 = param[3];
                Length2 = param[4];

            }
            else
            {
                Row = param[0];
                Col = param[1];
                Phi = param[2];
                Length1 = param[3];
                Length2 = param[4];
            }
            //translate.HomMat2dInvert()
            lock (Finder)
            {
                Finder.GenMeasureRectangle2((HTuple)param[0], param[1], param[2], param[3], param[4], w, h,"nearest_neighbor");
            }
            
            HTuple row1, col1, amp1, intdis;
            try
            {
                lock (Finder)
                {
                    double sigma = Math.Min(Sigma, Math.Max(0, (Length1 - 2) / 7.8));
                    Finder.MeasurePos(image, sigma, Threshold, Transition, Select, out row1, out col1, out amp1, out intdis);
                }
                if (row1.Type != HTupleType.EMPTY)
                {
                    if (Offset != 0)
                    {
                        row1 = row1 + Offset * Math.Cos(Phi + Math.PI / 2);
                        col1 = col1 + Offset * Math.Sin(Phi + Math.PI / 2);
                    }
                    e_row = row1[0];
                    e_col = col1[0];
                    double disp_col1 = col1 + Length2;
                    double disp_col2 = col1 - Length2;
                    HHomMat2D trans = new HHomMat2D();
                    HHomMat2D rotate = trans.HomMat2dRotate((double)param[2] + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);

                    rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
                    rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);
                    lineValue.row1 = trans_row1;
                    lineValue.col1 = trans_col1;
                    lineValue.row2 = trans_row2;
                    lineValue.col2 = trans_col2;
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

        public override string ToString()
        {
            return Name;
        }

        public HMeasure Finder { get; set; }
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
                if (_sigma != value)
                {
                    _sigma = value;
                    ParameterChanged?.Invoke(this, null);
                    RaisePropertyChanged("Sigma");
                }              

            }
        }
        double _offset;
        public double Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                if (_offset != value)
                {
                    _offset = value;
                    RaisePropertyChanged("Offset");
                    ParameterChanged?.Invoke(this, null);
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
            HelperMethods.SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public LineFinder(int width, int height)
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
            Finder = new HMeasure(Row, Col, Phi, Length1, Length2, width, height, "nearest_neighbor");

        }
        public LineFinder(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            if (Finder == null)
            {
                Finder = new HMeasure(Row, Col, Phi, Length1, Length2, (int)Width, (int)Height, "nearest_neighbor");
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
        


        public LineFinderResult Run(HImage image,HHomMat2D transform)
        {
            int w, h;
            image.GetImageSize(out w, out h);
            if (transform == null)
            {
                transform = new HHomMat2D();
            }
            double sx = transform.HomMat2dToAffinePar(out double sy, out double phi, out double theta, out double tx, out double ty);
            transform.AffineTransPixel(Row, Col, out double rowtrans, out double coltrans);
            if (w == Width && h == Height)
            {
                HTuple row,  col,  amp,  dis;
                try
                {


                    lock (Finder)
                    {
                        Finder.GenMeasureRectangle2(rowtrans, coltrans, Phi + phi, Length1, Length2, (int)Width, (int)Height, "nearest_neighbor");
                        double sigma = Math.Min(Sigma,Math.Max(0, (Length1 - 2) / 7.8));
                        Finder.MeasurePos(image, sigma, Threshold, Transition, Select, out  row, out  col, out  amp, out  dis);
                    }
                    
                    
                }
                catch (Exception ex)
                {
                    return new LineFinderResult(false, FindLinesNode.GenLine(rowtrans,coltrans,Length1,Length2,Phi+phi));
                }
                if (row.Type != HTupleType.EMPTY)
                {
                    Amp = 0;
                    if (Offset != 0)
                    {
                        row = row+ Offset*Math.Cos(Phi+Math.PI/2);
                        col =col+ Offset*Math.Sin(Phi+Math.PI/2);
                    }
                    return new LineFinderResult(true, FindLinesNode.GenLine(row,col, Length1, Length2, Phi + phi));
                }
                else
                {
                    Amp = 0;
                    return new LineFinderResult(false, FindLinesNode.GenLine(rowtrans, coltrans, Length1, Length2, Phi + phi));
                }

            }
            else
            {
                return new LineFinderResult(false, FindLinesNode.GenLine(rowtrans, coltrans, Length1, Length2, Phi + phi));
            }
        }

    }
    public class LineFinderResult
    {
        public bool IsFound;
        public LineValue Line { get; set; }
        public LineFinderResult(bool IsFound, LineValue Line)
        {
            this.IsFound = IsFound;
            this.Line = Line;
        }
    }

}
