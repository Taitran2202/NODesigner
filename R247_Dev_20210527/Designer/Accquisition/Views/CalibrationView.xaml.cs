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

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for CalibrationView.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for CalibrationView.xaml
    /// </summary>
    public partial class CalibrationView : DevExpress.Xpf.Core.ThemedWindow, INotifyPropertyChanged
    {
        HWindow display;
        Calibration calib;
        HImage image;
        HImage image_gray;
        HTuple actual_w, actual_h;
        public CalibrationView(Calibration calib, HImage image)
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
            this.DataContext = calib;
            checkBox1.DataContext = this;
        }
        HRegion region = new HRegion();

        //private void num_inner_radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        //{
        //    if (e.NewValue.HasValue)
        //    {
        //        calib.inner_radius = (double)e.NewValue;

        //    }
        //}
        //private void num_outer_radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        //{
        //    if (e.NewValue.HasValue)
        //    {
        //        calib.outter_radius = (double)e.NewValue;

        //    }
        //}

        //private void num_start_phi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        //{
        //    if (e.NewValue.HasValue)
        //    {
        //        calib.start_phi = (double)e.NewValue;

        //    }
        //}

        //private void num_end_phi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        //{
        //    if (e.NewValue.HasValue)
        //    {
        //        calib.end_phi = (double)e.NewValue;

        //    }
        //}

        private void num_length_cen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                calib.Lengthmm = (double)e.NewValue;
                if (calib.LengthPixel > 0)
                {

                    if (calib.Lengthmm != 0)
                    {
                        calib.ScaleX = calib.LengthPixel / calib.Lengthmm;

                    }

                }
            }
        }

        private void num_length_pixel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                calib.LengthPixel = (double)e.NewValue;
                if (calib.Lengthmm > 0)
                {

                    if (calib.Lengthmm != 0)
                        calib.ScaleX = calib.LengthPixel / calib.Lengthmm;
                }
            }
        }
        HDrawingObject draw1, draw2;
        HDrawingObject draw3, draw4;
        Measure measurex, measurey;
        private void btn_draw_pixel_Click(object sender, RoutedEventArgs e)
        {
            if (draw1 == null)
            {
                measurex = new Measure(new Calibration());
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
        }
        bool result1, result2;
        bool result3, result4;
        LineValue line1 = new LineValue(), line2 = new LineValue();
        LineValue line_distance = new LineValue();
        LineValue line3 = new LineValue(), line4 = new LineValue();
        LineValue line_distancey = new LineValue();
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

        private void num_length_cen1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                calib.LengthmmY = (double)e.NewValue;
                if (calib.LengthPixelY > 0)
                {

                    if (calib.LengthmmY != 0)
                    {
                        calib.ScaleY = calib.LengthPixelY / calib.LengthmmY;

                    }

                }
            }
        }

        private void num_length_pixel1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                calib.LengthPixelY = (double)e.NewValue;
                if (calib.LengthmmY > 0)
                {

                    if (calib.LengthmmY != 0)
                        calib.ScaleY = calib.LengthPixelY / calib.LengthmmY;
                }
            }
        }

        public void DisplayLine(LineValue line)
        {
            if (line.row1 != null)
            {
                display.DispLine(line.row1, line.col1, line.row2, line.col2);
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _manual = true;

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.Owner = Application.Current.MainWindow;
                this.ShowInTaskbar = false;
            }
            catch (Exception ex)
            {

            }
        }

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
        
        public void OnDrawingObjectChange(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            cmb_mode.DataContext = measurex;
            if (drawid.ID == draw1.ID)
            {
                if (Manual)
                    result1 = measurex.lower_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line1);
                else
                    result1 = measurex.lower_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line1);

                stack_edge.DataContext = measurex.lower_edge;
            }
            if (drawid.ID == draw2.ID)
            {
                if (Manual)
                    result2 = measurex.upper_edge.ManualUpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line2);
                else
                    result2 = measurex.upper_edge.UpdateMeasureWithResult(param, new HHomMat2D(), actual_w, actual_h, image_gray, display, ref line2);

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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            window_display.HalconWindow.DispObj(image);
        }

        //private void btn_draw_test_Click(object sender, RoutedEventArgs e)
        //{
        //    calib.TranImage(image).DispObj(display);
        //}

        //private void btn_select_inner_Click(object sender, RoutedEventArgs e)
        //{
        //    display.ClearWindow();
        //    window_display.HMouseDown += window_display_HMouseDown;
        //    display.ClearWindow();
        //    HXLDCont cont = region.GenContourRegionXld("border").SegmentContoursXld("lines_circles", 5, 80, 80);
        //    display.SetColored(12);
        //    cont.DispObj(display);
        //}
        //private void btn_select_outter_Click(object sender, RoutedEventArgs e)
        //{
        //    display.ClearWindow();
        //    window_display.HMouseDown += window_display_HMouseDown_outter;
        //    display.ClearWindow();
        //    HXLDCont cont = region.GenContourRegionXld("border").SegmentContoursXld("lines_circles", 5, 80, 80);
        //    display.SetColored(12);
        //    cont.DispObj(display);
        //}

        //private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        //{
        //    display.ClearWindow();
        //    HXLDCont cont = region.GenContourRegionXld("border").SegmentContoursXld("lines_circles", 5, 80, 80);
        //    HRegion interactive_region = new HRegion(e.Row, e.Column, 10);
        //    for (int i = 1; i < cont.CountObj(); i++)
        //    {
        //        if (cont.SelectObj(i).GenRegionContourXld("filled").Intersection(interactive_region).Area > 0)
        //        {
        //            HTuple row, col, radius, startp, endp, porder;
        //            cont.SelectObj(i).FitCircleContourXld("algebraic", -1, 0, 0, 3, 2, out row, out col, out radius, out startp, out endp, out porder);
        //            HXLDCont circle = new HXLDCont();
        //            circle.GenCircleContourXld(row, col, radius, startp, endp, porder, 1);
        //            display.SetColor("green");
        //            display.DispObj(cont.SelectObj(i));
        //            display.SetColor("blue");
        //            display.DispObj(circle);
        //            calib.inner_radius = radius / calib.Scale;
        //            //num_inner_radius.Value = calib.inner_radius;
        //            break;
        //        }
        //    }

        //    window_display.HMouseDown -= window_display_HMouseDown;

        //}
        //private void window_display_HMouseDown_outter(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        //{
        //    display.ClearWindow();
        //    HXLDCont cont = region.GenContourRegionXld("border").SegmentContoursXld("lines_circles", 5, 80, 80);
        //    HRegion interactive_region = new HRegion(e.Row, e.Column, 10);
        //    for (int i = 1; i < cont.CountObj(); i++)
        //    {
        //        if (cont.SelectObj(i).GenRegionContourXld("filled").Intersection(interactive_region).Area > 0)
        //        {
        //            HTuple row, col, radius, startp, endp, porder;
        //            cont.SelectObj(i).FitCircleContourXld("algebraic", -1, 0, 0, 3, 2, out row, out col, out radius, out startp, out endp, out porder);
        //            HXLDCont circle = new HXLDCont();
        //            circle.GenCircleContourXld(row, col, radius, startp, endp, porder, 1);
        //            display.SetColor("green");
        //            display.DispObj(cont.SelectObj(i));
        //            display.SetColor("blue");
        //            display.DispObj(circle);
        //            calib.outter_radius = radius / calib.Scale;
        //            //num_outer_radius.Value = calib.outter_radius;
        //            break;
        //        }
        //    }
        //    window_display.HMouseDown -= window_display_HMouseDown_outter;
        //}

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_draw_pixel_y_Click(object sender, RoutedEventArgs e)
        {
            if (draw3 == null)
            {
                measurey = new Measure(new Calibration());
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
        }
    }
}
