using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HalconDotNet;
using System.ComponentModel;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Misc;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for EditMeasurementWindow.xaml
    /// </summary>
    public partial class EditMeasurementWindow : ThemedWindow,INotifyPropertyChanged
    {
        HWindow display;
        HImage image;
        HImage image_gray;
        int w, h;
        Measure measure;
        HDrawingObject draw1, draw2;
        LineValue line1 = new LineValue(), line2= new LineValue();
        LineValue line_distance = new LineValue();
        HHomMat2D translate;
        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        public EditMeasurementWindow(HImage ref_image, Measure measure, HHomMat2D translate)
        {
            image = ref_image;
            image.GetImageSize(out w, out h);
            image_gray = image.Rgb1ToGray();
            this.measure = measure;
            if (translate == null)
                translate = new HHomMat2D();
            this.translate = translate;
            measure.Create(w, h);
            HTuple low_row_trans, low_col_trans;
            translate.AffineTransPixel(measure.lower_edge.Row, measure.lower_edge.Col, out low_row_trans, out low_col_trans);
            HTuple up_row_trans, up_col_trans;
            translate.AffineTransPixel(measure.upper_edge.Row, measure.upper_edge.Col, out up_row_trans, out up_col_trans);
            
            
           draw1= new HDrawingObject(low_row_trans, low_col_trans, measure.lower_edge.Phi, measure.lower_edge.Length1, measure.lower_edge.Length2);
                
           draw1.OnResize(OnResize);
           draw1.OnDrag(OnResize);
            draw1.OnSelect(OnSelect);

            draw2 = new HDrawingObject(up_row_trans, up_col_trans, measure.upper_edge.Phi, measure.upper_edge.Length1, measure.upper_edge.Length2);

            draw2.OnResize(OnResize);
            draw2.OnDrag(OnResize);
            draw2.OnSelect(OnSelect);

            InitializeComponent();
            this.DataContext = measure;

            series.Points?.Clear();
            series.Points.AddRange(Points);

        }
        bool result1, result2;
        private void Sld_brushsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (line_pos != null & line_neg != null)
            {
                line_pos.Value = e.NewValue;
                line_neg.Value = -e.NewValue;
            }

        }
        public void RefreshGraphic()
        {
            display.ClearWindow();
           
            DisplayLine(line1);
            DisplayLine(line2);
            display.SetColor("green");
            DisplayLine(line_distance);
        }
        public void DisplayLine(LineValue line)
        {
            if (line.row1 !=null)
            {
                
                display.DispLine(line.row1, line.col1, line.row2, line.col2);
            }
        }
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));

            

          
           
           

            if (drawid.ID == draw1.ID)
            {
               if (!measure.lower_edge.IsManual)
               result1= measure.lower_edge.UpdateMeasureWithResult(param,translate, w, h, image_gray, display,ref line1);
               else
                    result1 = measure.lower_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);
                Points = measure.lower_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }
            else
            {
                if (!measure.upper_edge.IsManual)
              result2=  measure.upper_edge.UpdateMeasureWithResult(param,translate, w, h, image_gray, display,ref line2);
                else
                    result2 = measure.upper_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
                Points = measure.upper_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }


            if (result1 & result2)
            {
                measure.CalculateDistanceOriginal(ref line_distance,translate);
            }
            else
            {
                line_distance.row1 = null;
            }
            RefreshGraphic();
        }
        double _distance;
        public double Distance
        {
            get
            {
                return _distance;
            }
            set
            {
                if (_distance != value)
                {
                    _distance = value;
                    RaisePropertyChanged("Distance");
                }
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Edge_PropertyChanged(EdgeFinder sender, object e)
        {
            if (e is bool)
            {
               
                return;
            }
            // sender.UpdateMeasure(w, h, image_gray, display);
            if (sender == measure.upper_edge)
            {
                HTuple param = draw2.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
                if (!sender.IsManual)
                    sender.UpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
                else
                    sender.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
            }
            else
            {
                HTuple param = draw1.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
                if (!sender.IsManual)

                    sender.UpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);
                else
                    sender.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);
            }
            measure.CalculateDistanceOriginal(ref line_distance, translate);
            

            Points = sender.GetLineProfile(image_gray);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);

            }
            ));
            RefreshGraphic();
        }
        public void OnSetup(HDrawingObject drawid)
        {
            drawid.SetDrawingObjectParams("color", "blue");

            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));


            if (drawid.ID == draw1.ID)
            {
                

                measure.lower_edge.ParameterChanged = Edge_PropertyChanged;
                measure.upper_edge.ParameterChanged = null;
                stack_edge.DataContext = this.measure.lower_edge;
                Points = measure.lower_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }
            else
            {
              

                measure.upper_edge.ParameterChanged = Edge_PropertyChanged;
                measure.lower_edge.ParameterChanged = null;
                stack_edge.DataContext = this.measure.upper_edge;
                Points = measure.upper_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }
            if (drawid.ID == draw1.ID)
            {
                if (!measure.lower_edge.IsManual)
                    result1 = this.measure.lower_edge.UpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);
                else
                    result1 = this.measure.lower_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);
            }
            if (drawid.ID == draw2.ID)
            {
                if (!measure.upper_edge.IsManual)
                    result2 = this.measure.upper_edge.UpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
                else
                    result2 = measure.upper_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
            }

            if (result1 & result2)
            {
                measure.CalculateDistanceOriginal(ref line_distance, translate);
            }
            else
            {
                line_distance.row1 = null;
            }
            RefreshGraphic();
        }
        public void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {
           
            drawid.SetDrawingObjectParams("color", "blue");
           
                HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));

         
            if (drawid.ID == draw1.ID)
            {
                if (!measure.lower_edge.IsManual)
                    result1=  this.measure.lower_edge.UpdateMeasureWithResult(param,translate, w, h, image_gray, display,ref line1);
                else
                    result1 = this.measure.lower_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line1);

                measure.lower_edge.ParameterChanged = Edge_PropertyChanged;
                measure.upper_edge.ParameterChanged = null;
                stack_edge.DataContext = this.measure.lower_edge;
                Points = measure.lower_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }
            else
            {
                if (!measure.upper_edge.IsManual)
                    result2 = this.measure.upper_edge.UpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);
                else
                    result2 = measure.upper_edge.ManualUpdateMeasureWithResult(param, translate, w, h, image_gray, display, ref line2);

                measure.upper_edge.ParameterChanged = Edge_PropertyChanged;
                measure.lower_edge.ParameterChanged = null;
                stack_edge.DataContext = this.measure.upper_edge;
                Points = measure.upper_edge.GetLineProfile(image_gray);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    series.Points.Clear();
                    series.Points.AddRange(Points);
                    //line_thresh.Y = edges[index].Threshold;
                }
                ));
            }
            if (result1 & result2)
            {
                measure.CalculateDistanceOriginal(ref line_distance,translate);
            }
            else
            {
                line_distance.row1 = null;
            }
            RefreshGraphic();
        }
        private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void btn_zoom_out_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_zoom_in_Click(object sender, RoutedEventArgs e)
        {
           
        }

        

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.ShowInTaskbar = false;

                //this.Owner = Application.Current.MainWindow;
            }
            catch (Exception ex)
            {

            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           if (cmb_mode.SelectedItem!=null)
            {
                try
                {
                    measure.CalculateDistanceOriginal(ref line_distance, translate);
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void ThemedWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                measure.lower_edge.ParameterChanged = null;
                measure.upper_edge.ParameterChanged = null;
            }
            catch (Exception ex)
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetLineWidth(1);
                display.AttachBackgroundToWindow(image);
                window_display.SetFullImagePart(null);
                display.AttachDrawingObjectToWindow(draw1);
                display.AttachDrawingObjectToWindow(draw2);
                window_display.SetFullImagePart(null);
                OnSetup(draw1);
                //draw1.SetDrawingObjectParams("color", "blue");
                //stack_edge.DataContext = measure.lower_edge;
                //measure.lower_edge.ParameterChanged = Edge_PropertyChanged;
                
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    
}
