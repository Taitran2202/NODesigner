using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace NOVisionDesigner.Designer.Windows.Calibration
{
    /// <summary>
    /// Interaction logic for Calibration2Window.xaml
    /// </summary>
    public partial class Calibration2Window : Window,INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        bool _segment1;
        public bool Segment1
        {
            get
            {
                return _segment1;
            }
            set
            {
                if (_segment1 != value)
                {
                    _segment1 = value;
                    RaisePropertyChanged("Segment1");
                }
            }
        }
        bool _segment2;
        public bool Segment2
        {
            get
            {
                return _segment2;
            }
            set
            {
                if (_segment2 != value)
                {
                    _segment2 = value;
                    RaisePropertyChanged("Segment2");
                }
            }
        }
        bool _segment3;
        public bool Segment3
        {
            get
            {
                return _segment3;
            }
            set
            {
                if (_segment3 != value)
                {
                    _segment3 = value;
                    RaisePropertyChanged("Segment3");
                }
            }
        }
        bool _segment4;
        public bool Segment4
        {
            get
            {
                return _segment4;
            }
            set
            {
                if (_segment4 != value)
                {
                    _segment4 = value;
                    RaisePropertyChanged("Segment4");
                }
            }
        }
        bool _segment5;
        public bool Segment5
        {
            get
            {
                return _segment5;
            }
            set
            {
                if (_segment5 != value)
                {
                    _segment5 = value;
                    RaisePropertyChanged("Segment5");
                }
            }
        }
        string _header_message;
        public string HeaderMessage
        {
            get
            {
                return _header_message;
            }
            set
            {
                if (_header_message != value)
                {
                    _header_message = value;
                    RaisePropertyChanged("HeaderMessage");
                }
            }
        }

        bool _manual;
        public bool Manual
        {
            get
            {
                return _manual;
            }
            set
            {
                if (_manual != value)
                {
                    _manual = value;
                    RaisePropertyChanged("Manual");
                }
            }
        }


        public Calibration2Window(Misc.Calibration calib, HImage image)
        {


            this.calib = calib;

            if (image.CountObj() == 0)
            {
                this.image = new HImage("byte", 2048, 1024);

            }
            else
            {
                this.image = image;
            }

            this.image.GetImageSize(out actual_w, out actual_h);
            if (actual_w.Type != HTupleType.EMPTY)
            {
                calib.ActualWidth = actual_w;
                calib.ActualHeight = actual_h;



                image_gray = this.image.Rgb1ToGray();
            }
            InitializeComponent();
            grid_side_content.DataContext = calib;
            this.DataContext = this;
            checkBox1.DataContext = this;
            
        }
        int step_index = 1;
        Measure measurex, measurey;
        HDrawingObject draw1, draw2;
        HDrawingObject draw3, draw4;
        bool result1, result2;
        bool result3, result4;
        LineValue line1 = new LineValue(), line2 = new LineValue();
        LineValue line_distance = new LineValue();
        LineValue line3 = new LineValue(), line4 = new LineValue();
        LineValue line_distancey = new LineValue();
        HWindow display;
        HImage image, image_gray;
        Misc.Calibration calib;

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            step_index++;
            GotoStep(step_index);
        }
        public void GotoStep(int index)
        {
            switch (index){
                case 1:Step1();break;
                case 2: Step2(); break;
                case 3: Step3(); break;
            }
        }
        private void btn_pre_Click(object sender, RoutedEventArgs e)
        {
            step_index--;
            GotoStep(step_index);
        }
        public void Step1()
        {
            Segment1 = true;
            Segment2 = false;
            Segment3 = false;
            Segment4 = false;
            Segment5 = false;
            HeaderMessage = "X axis calibration";
            if (draw3 != null)
            {
                display.DetachDrawingObjectFromWindow(draw3);
                display.DetachDrawingObjectFromWindow(draw4);
                //draw3 = null;
               // draw4 = null;
            }
            AddXAxis();
        }
        public void Step2()
        {
            Segment1 = true;
            Segment2 = true;
            Segment3 = false;
            Segment4 = false;
            Segment5 = false;
            HeaderMessage = "Y axis calibration";
            if (draw1 != null)
            {
                display.DetachDrawingObjectFromWindow(draw1);
                display.DetachDrawingObjectFromWindow(draw2);
                //draw1 = null;
                //draw2 = null;
            }

            AddYAxis();
        }
        public void Step3()
        {
            Segment1 = true;
            Segment2 = true;
            Segment3 = true;
            Segment4 = false;
            Segment5 = false;
            Clear();
        }
        public void Clear()
        {

        }
        HTuple actual_w, actual_h;
        public void AddYAxis()
        {
            if (draw3 == null)
            {
                measurey = new Measure(new Misc.Calibration());
                measurey.Create(actual_w, actual_h);
                draw3 = new HDrawingObject();
                draw4 = new HDrawingObject();
                draw3.CreateDrawingObjectRectangle2(measurey.lower_edge.Row, measurey.lower_edge.Col, measurey.lower_edge.Phi, measurey.lower_edge.Length1, measurey.lower_edge.Length2);
                draw4.CreateDrawingObjectRectangle2(measurey.upper_edge.Row, measurey.upper_edge.Col, measurey.upper_edge.Phi, measurey.upper_edge.Length1, measurey.upper_edge.Length2);

                draw3.OnSelect(OnDrawingObjectChangeY);
                draw3.OnResize(OnDrawingObjectChangeY);
                draw3.OnDrag(OnDrawingObjectChangeY);
                draw4.OnSelect(OnDrawingObjectChangeY);
                draw4.OnResize(OnDrawingObjectChangeY);
                draw4.OnDrag(OnDrawingObjectChangeY);
                display.AttachDrawingObjectToWindow(draw3);
                display.AttachDrawingObjectToWindow(draw4);
                draw3.SetDrawingObjectParams("color", "green");
                draw4.SetDrawingObjectParams("color", "green");
            }
            else
            {
                display.AttachDrawingObjectToWindow(draw3);
                display.AttachDrawingObjectToWindow(draw4);
            }
        }
        public void AddXAxis()
        {
            if (draw1 == null)
            {
                measurex = new Measure(new Misc.Calibration());
                measurex.Create(actual_w, actual_h);
                draw1 = new HDrawingObject();
                draw2 = new HDrawingObject();
                draw1.CreateDrawingObjectRectangle2(measurex.lower_edge.Row, measurex.lower_edge.Col, measurex.lower_edge.Phi, measurex.lower_edge.Length1, measurex.lower_edge.Length2);
                draw2.CreateDrawingObjectRectangle2(measurex.upper_edge.Row, measurex.upper_edge.Col, measurex.upper_edge.Phi, measurex.upper_edge.Length1, measurex.upper_edge.Length2);

                draw1.OnSelect(OnDrawingObjectChange);
                draw1.OnResize(OnDrawingObjectChange);
                draw1.OnDrag(OnDrawingObjectChange);
                draw2.OnSelect(OnDrawingObjectChange);
                draw2.OnResize(OnDrawingObjectChange);
                draw2.OnDrag(OnDrawingObjectChange);
                display.AttachDrawingObjectToWindow(draw1);
                display.AttachDrawingObjectToWindow(draw2);
                draw1.SetDrawingObjectParams("color", "blue");
                draw2.SetDrawingObjectParams("color", "blue");
            }
            else
            {
                display.AttachDrawingObjectToWindow(draw1);
                display.AttachDrawingObjectToWindow(draw2);
            }
        }
        public void OnDrawingObjectChange(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            cmb_mode.DataContext = measurex;
            if (drawid.ID == draw1.ID)
            {
                LineValue lineValueRef = new LineValue();
                if (Manual)
                    result1 = measurex.lower_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref lineValueRef);
                else
                    result1 = measurex.lower_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref lineValueRef);
                if (result1)
                {
                    double x = (lineValueRef.col2.Value + lineValueRef.col1.Value) / 2;
                    double y = (lineValueRef.row1.Value + lineValueRef.row2.Value) / 2;
                    line1.row1 = 0;
                    line1.col1 = x;
                    line1.row2 = actual_h;
                    line1.col2 = x;
                }

                stack_edge.DataContext = measurex.lower_edge;
            }
            if (drawid.ID == draw2.ID)
            {
                LineValue lineValueRef = new LineValue();
                if (Manual)
                    result2 = measurex.upper_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref lineValueRef);
                else
                    result2 = measurex.upper_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref lineValueRef);
                if (result2)
                {
                    double x = (lineValueRef.col2.Value + lineValueRef.col1.Value) / 2;
                    double y = (lineValueRef.row1.Value + lineValueRef.row2.Value) / 2;
                    line2.row1 = 0;
                    line2.col1 = x;
                    line2.row2 = actual_h;
                    line2.col2 = x;
                }
                
                stack_edge.DataContext = measurex.upper_edge;
            }


            if (result1 & result2)
            {
                measurex.CalculateDistanceOriginal(ref line_distance, new HHomMat2D());
            }
            else
            {
                line_distance.row1 = null;
            }
            RefreshGraphic();
            calib.LengthPixel = measurex.ActualValue;
        }
        public void RefreshGraphic()
        {
            display.ClearWindow();
            DisplayLine(line1);

            DisplayLine(line2);
            DisplayLine(line_distance);

            DisplayLine(line3);
            DisplayLine(line4);
            DisplayLine(line_distancey);
        }
        public void DisplayLine(LineValue line)
        {
            if (line.row1 != null)
            {
                display.DispLine(line.row1, line.col1, line.row2, line.col2);
            }
        }

        public void OnDrawingObjectChangeY(HDrawingObject drawid, HWindow window, string type)
        {




            cmb_mode.DataContext = measurey;
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));

            if (drawid.ID == draw3.ID)
            {
                if (Manual)
                    result3 = measurey.lower_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line3);
                else
                    result3 = measurey.lower_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line3);

                stack_edge.DataContext = measurey.lower_edge;
            }
            if (drawid.ID == draw4.ID)
            {
                if (Manual)
                    result4 = measurey.upper_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line4);
                else
                    result4 = measurey.upper_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line4);

                stack_edge.DataContext = measurey.upper_edge;
            }


            if (result3 & result4)
            {
                measurey.CalculateDistanceOriginal(ref line_distancey, new HHomMat2D());
            }
            else
            {
                line_distancey.row1 = null;
            }
            RefreshGraphic();
            calib.LengthPixelY = measurey.ActualValue;
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            try
            {
                display.AttachBackgroundToWindow(image);
                window_display.SetFullImagePart(null);
                Step1();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
    }
}
