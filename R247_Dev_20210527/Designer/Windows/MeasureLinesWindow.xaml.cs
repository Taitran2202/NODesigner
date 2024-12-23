using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
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
using System.Reactive.Linq;
using DynamicData;
using HalconDotNet;
using System.Collections.Concurrent;
using DevExpress.Xpf.Core;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for MeasureLinesWindow.xaml
    /// </summary>
    public partial class MeasureLinesWindow : Window
    {
        MeasureLinesNode node;
        LineValue[] Lines= new LineValue[0];
        HRegion[] LineRegion = new HRegion[0];
        FixedSizedQueue<LineValue> SelectedLine = new FixedSizedQueue<LineValue>(2);
        public string Mode { get; set; } = "PointToLine";
        double scale_ratio = 1;
        private double Angulo(double x1, double y1, double x2, double y2)
        {
            //double degrees;
            double dx = x1 - x2;
            double dy = y1 - y2;
            double deg = Math.Atan2(dy, dx);
            //if (deg < 0) { deg += Math.PI*2; }
            // Avoid divide by zero run values.
            //if (x2 - x1 == 0)
            //{
            //    if (y2 > y1)
            //        degrees = Math.PI/2;
            //    else
            //        degrees = Math.PI*3/2;
            //}
            //else
            //{
            //    // Calculate angle from offset.
            //    double riseoverrun = (double)(y2 - y1) / (double)(x2 - x1);
            //    degrees = Math.Atan(riseoverrun);
            //    //degrees = radians * ((double)180 / Math.PI);

            //    // Handle quadrant specific transformations.       
            //    if ((x2 - x1) < 0 || (y2 - y1) < 0)
            //        degrees += Math.PI;
            //    if ((x2 - x1) > 0 && (y2 - y1) < 0)
            //        degrees -= Math.PI;
            //    if (degrees < 0)
            //        degrees += 2*Math.PI;
            //}
            return deg;
        }
        double LineLength(LineValue line)
        {
            return Math.Sqrt((line.row1.Value - line.row2.Value) * (line.row1.Value - line.row2.Value) + (line.col1.Value - line.col2.Value) * (line.col1.Value - line.col2.Value));
        }
        HRegion GenRectangleFromLine(LineValue line)
        {
            HRegion region = new HRegion();
            region.GenRectangle2((line.row1.Value + line.row2.Value) / 2, (line.col1.Value + line.col2.Value) / 2,
                -Angulo(line.col1.Value, line.row1.Value, line.col2.Value, line.row2.Value),  LineLength(line) / 2,5);
            return region;
        }
        public MeasureLinesWindow(MeasureLinesNode node)
        {
            InitializeComponent();
            this.node = node;
            scale_ratio = node.ScaleRatio.Value;
            Lines = this.node.MergeLines().Clone() as LineValue[];
            LineRegion = new HRegion[Lines.Length];
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i] != null)
                {
                    //HOperatorSet.GenRegionLine(out HObject line, Lines[i].row1, Lines[i].col1, Lines[i].row2, Lines[i].col2);

                    LineRegion[i] = GenRectangleFromLine(Lines[i]);
                }
                else
                {
                    LineRegion[i] = new HRegion();
                    LineRegion[i].GenEmptyRegion();
                }
                
            }
            window_display.HMouseDown += Window_display_HMouseDown;
            lst_view.ItemsSource = node.LineMeasures;
            lst_view.SelectionChanged += Lst_view_SelectionChanged;
            

        }
        private void btn_zoom_in_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, 120);
        }
        private void btn_zoom_out_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, -120);
        }
        private void btn_zoom_fit_click(object sender, RoutedEventArgs e)
        {
            window_display.SetFullImagePart(); 
        }
        private void Lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = (sender as ListBox).SelectedItem as LineMeasure;
            if (vm != null)
            {
                try
                {
                    DispOverlay();
                    if (node.LastContext == null)
                    {
                        return;
                    }
                    if (vm.Index1 >= Lines.Length | vm.Index2 >= Lines.Length)
                    {
                        return;
                    }
                    var scale_x = (node.LastContext as InspectionContext).inspection_result.scale_x;
                    var scale_y = (node.LastContext as InspectionContext).inspection_result.scale_y;
                    var context = new InspectionContext(null,null, scale_x, scale_y, "");

                    vm.Run(Lines[vm.Index1], Lines[vm.Index2], context, scale_ratio);
                    context.inspection_result.Display(window_display);
                }
                catch (Exception ex)
                {

                }

            }
        }

        string _color = "red";
        public double? Run(LineValue line1, LineValue line2, InspectionContext e, string Mode, double scale_ratio)
        {
            if (line1 != null & line2 != null)
            {

                var scale_x = e.inspection_result.scale_x;
                var scale_y = e.inspection_result.scale_y;
                HHomMat2D calib_trans = new HHomMat2D();
                calib_trans = calib_trans.HomMat2dScaleLocal(1 / scale_y, 1 / scale_x);

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
                distance = scale_ratio * distance;
                e.inspection_result?.AddDisplay(new HXLDCont(new HTuple(row1_disp, row2_disp), new HTuple(col1_disp, col2_disp)), _color);
 

                e.inspection_result?.AddText(Math.Round(distance.D, 2).ToString(), "black", "#ffffffaa", (row1_disp + row2_disp) / 2, (col1_disp + col2_disp) / 2);
                // display.DispText(Math.Round( distance.D,2).ToString(), "image", (row1 + row2) / 2, (col1 + col2) / 2, "black", "box_color", "#ffffffaa");
                return distance;
            }
            else
                return 0;
        }
        private void Window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            for(int i = 0; i < LineRegion.Length; i++)
            {
                if (LineRegion[i].TestRegionPoint(e.Row, e.Column) > 0)
                {
                    SelectedLine.Enqueue(Lines[i]);
                    DispOverlay();
                    Calculate();
                    break;
                }
            }
            
        }
        public void Calculate()
        {
            if (SelectedLine.Count == 2)
            {
                var line1 = SelectedLine.ElementAt(0);
                var line2 = SelectedLine.ElementAt(1);
                if (node.LastContext != null)
                {
                    var scale_x = (node.LastContext as InspectionContext).inspection_result.scale_x;
                    var scale_y = (node.LastContext as InspectionContext).inspection_result.scale_y;
                    var displayContext = new InspectionContext(null,null, scale_x, scale_y, "");
                    Run(line1, line2, displayContext, Mode, scale_ratio);
                    displayContext.inspection_result.Display(window_display);
                }
                else
                {
                    var displayContext = new InspectionContext(null,null, 1, 1, "");
                    Run(line1, line2, displayContext, Mode, scale_ratio);
                    displayContext.inspection_result.Display(window_display);
                }
                
                
            }
        }
        public void DispOverlay()
        {
            window_display.HalconWindow.ClearWindow();
            window_display.HalconWindow.SetColor("blue");
            for (int i = 0; i< LineRegion.Length; i++)
            {
                window_display.HalconWindow.DispRegion(LineRegion[i]);
            }
            //display selected line
            window_display.HalconWindow.SetColor("green");
            foreach(var item in SelectedLine)
            {
                window_display.HalconWindow.DispLine(item.row1, item.col1, item.row2, item.col2);
            }
                
        }
        

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            if (node.ReferenceImage.Value != null)
            {
                if (node.ReferenceImage.Value.IsInitialized())
                {
                    var image = node.ReferenceImage.Value.Clone();
                    window_display.HalconWindow.AttachBackgroundToWindow(image);
                    window_display.SetFullImagePart(image);
                }
                
            }
            DispOverlay();
        }

        private void btn_add_model_click(object sender, RoutedEventArgs e)
        {
            if (SelectedLine.Count == 2)
            {
                var newItem = new LineMeasure() { MeasureName = "unknow" + node.LineMeasures.Count.ToString(), 
                    Index1 = Lines.IndexOf(SelectedLine.ElementAt(0)), 
                    Index2 = Lines.IndexOf(SelectedLine.ElementAt(1)), Mode = Mode };
                node.LineMeasures.Add(newItem);
                lst_view.SelectedItem = newItem;
                lst_view.ScrollIntoView(newItem);
            }
            else
            {
                DXMessageBox.Show(this,"Please click 2 lines on images first!","Info",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as LineMeasure;
            if (vm != null)
            {
                try
                {
                    if(DXMessageBox.Show(this,"Do you want to remove " + vm.MeasureName + "?", "warning", MessageBoxButton.YesNo, MessageBoxImage.Warning)== MessageBoxResult.Yes)
                    {
                        node.LineMeasures.Remove(vm);
                    }
                    
                }
                catch (Exception ex)
                {

                }

            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = (sender as Control).DataContext as LineMeasure;
            if (vm != null)
            {
                try
                {
                    DispOverlay();
                    if (node.LastContext == null)
                    {
                        return;
                    }
                    if (vm.Index1 >= Lines.Length| vm.Index2 >= Lines.Length)
                    {
                        return;
                    }
                    var scale_x = (node.LastContext as InspectionContext).inspection_result.scale_x;
                    var scale_y = (node.LastContext as InspectionContext).inspection_result.scale_y;
                    var context = new InspectionContext(null,null, scale_x, scale_y, "");

                    vm.Run(Lines[vm.Index1], Lines[vm.Index2], context, scale_ratio);
                    context.inspection_result.Display(window_display);
                }
                catch (Exception ex)
                {

                }

            }
            
            
        }

        private void btn_add_input_Click(object sender, RoutedEventArgs e)
        {
            IOMeasurementLinesEditor wd = new IOMeasurementLinesEditor(node);
            wd.ShowDialog();
        }
    }
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
