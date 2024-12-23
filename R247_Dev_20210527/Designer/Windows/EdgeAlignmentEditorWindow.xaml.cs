using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using HalconDotNet;
using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Misc;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for EdgeAlignmentEditorWindow.xaml
    /// </summary>
    public partial class EdgeAlignmentEditorWindow : ThemedWindow
    {
        HWindow display;
        HImage image;
        HImage image_gray;
        int w, h;


        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        List<HDrawingObject> draw = new List<HDrawingObject>();
        public EdgeAlignmentEditorWindow(ObservableCollection<EdgeFinder> edges,HImage image)
        {
            this.edges = edges;
            this.image = image;
            image_gray = image.Rgb1ToGray();
            image.GetImageSize(out w, out h);
            InitializeComponent();
            lst_edge.ItemsSource = edges;
            if (edges.Count == 1)
                lst_edge.SelectedIndex = 0;
            series.Points?.Clear();
            series.Points.AddRange(Points);

            // model =  series.CreateModel();

            // plot.Annotations.Add(line_thresh);
            //  plot.Annotations.Add(line_thresh1);

            // plot.ActualModel.MouseMove += ActualModel_MouseMove;

        }
       // Annotation line_thresh = new Annotation() { AnchorPoint = new PaneAnchorPoint( = 0, Type = OxyPlot.Annotations.LineAnnotationType.Horizontal };
       // Annotation line_thresh1 = new Annotation() { Y = 0, Type = OxyPlot.Annotations.LineAnnotationType.Horizontal };
       

        //private void ActualModel_MouseMove(object sender, OxyMouseEventArgs e)
        //{
        //    // throw new NotImplementedException();
        //    // model.PlotModel.
        //    if (Mouse.LeftButton == MouseButtonState.Released )
        //        return;
        //    if (model.GetNearestPoint(e.Position, false) == null)
        //        return;
        //    if (lst_edge.SelectedItem as EdgeFinder == null)
        //        return;
        //    RefreshGraphic();
        //    DataPoint selected = (DataPoint)model.GetNearestPoint(e.Position, false).Item;
        //    display.SetColor("red");
        //    GenLinePlot(selected.X, (lst_edge.SelectedItem as EdgeFinder).range);

        //}

        

       

        //private void Model_MouseDown(object sender, OxyMouseDownEventArgs e)
        //{
            
        //}

     //   OxyPlot.Series.Series model;
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            
            int index = draw.FindIndex(x => x.ID == drawid.ID);
            edges[index].UpdateMeasure(param, w, h, image_gray, display);
            Points = edges[index].GetLineProfile(image_gray);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);
                //line_thresh.Y = edges[index].Threshold;
            }
            ));
           
            RefreshGraphic();
        }
        public void RefreshGraphic()
        {
            display.ClearWindow();
            display.SetColor("green");
            foreach (EdgeFinder edge in edges)
            {
                DisplayLine(edge.last_line);
            }
        }
        public void DisplayLine(LineValue line)
        {
            if (line!=null)
            if (line.row1 != null)
            {
                display.DispLine(line.row1, line.col1, line.row2, line.col2);
            }
        }
        //public void OnSelect(HDrawingObject drawid, HWindow window, string type)
        //{
        //    HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
        //  //  if (draw)
        //    int index = draw.FindIndex(x => x.ID == drawid.ID);
        //    edges[index].UpdateMeasure(param, w, h, image_gray, display);
        //}
        HDrawingObject last_selected = null;
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        long pre_index_selected = -1;
        
        public void OnSelect(HDrawingObject drawid, HWindow window, string type)
        {
            
            int index = draw.FindIndex(x => x.ID == drawid.ID);
            lst_edge.SelectedIndex = index;
            if (last_selected != null)
            {
                pre_index_selected = last_selected.ID;
            }
            if (last_selected!=null)
                last_selected.SetDrawingObjectParams("color", "red");
            drawid.SetDrawingObjectParams("color", "blue");
            last_selected = drawid;
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));

            stack_edge.DataContext = edges[index];
            edges[index].ParameterChanged = Edge_PropertyChanged;
            edges[index].UpdateMeasure(param, w, h, image_gray, display);
            RefreshGraphic();
            Points = edges[index].GetLineProfile(image_gray);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);
                //line_thresh.Y = edges[index].Threshold;
            }
            ));

        }
        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        public ObservableCollection<EdgeFinder> edges;
        EdgeFinder edge;
        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lst_edge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //try
            //{
            //    (e.RemovedItems[0] as EdgeFinder).ParameterChanged -= Edge_PropertyChanged;
            //}
            //catch (Exception ex)
            //{

            //}
            //if (lst_edge.SelectedItem != null)
            //{
            //    edge = lst_edge.SelectedItem as EdgeFinder;

            //    display.AttachDrawingObjectToWindow(draw[lst_edge.SelectedIndex]);
            //    this.DataContext = edge;

            //}
            if (lst_edge.SelectedIndex>-1)
            edges[lst_edge.SelectedIndex].ParameterChanged = Edge_PropertyChanged;
        }

        private void Edge_PropertyChanged(EdgeFinder sender, object e)
        {
            if (e is bool)
            {
                draw[lst_edge.SelectedIndex].SetDrawingObjectParams("color",sender.EdgeColor);
                return;
            }
            sender.UpdateMeasure(w, h, image_gray, display);

            RefreshGraphic();

            Points = sender.GetLineProfile(image_gray);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);
              
            }
            ));
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_new_edge_Click(object sender, RoutedEventArgs e)
        {        
           
            edge = new EdgeFinder(w, h);
            edges.Add(edge);
            HDrawingObject drawobj = new HDrawingObject(edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);
            
            draw.Add(drawobj);
            drawobj.OnResize(OnResize);
            drawobj.OnDrag(OnResize);
            drawobj.OnSelect(OnSelect);
            //drawobj.OnAttach(OnSelect);
            display.AttachDrawingObjectToWindow(drawobj);
            if (edges.Count==1)
            {
                stack_edge.DataContext = edge;
            }
            if (lst_edge.Items.Count == 1)
            {
                lst_edge.SelectedIndex = 0;
                OnSelect(drawobj, display, "new");
            }

           // OnSelect(drawobj, display, "new");
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetLineWidth(1);
            display.AttachBackgroundToWindow(image);
            window_display.SetFullImagePart(null);
            
            foreach (EdgeFinder edge in edges)
            {
                HDrawingObject drawobj = new HDrawingObject(edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);
                draw.Add(drawobj);
                drawobj.OnResize(OnResize);
                drawobj.OnDrag(OnResize);
                drawobj.OnSelect(OnSelect);
              //  drawobj.OnAttach(OnSelect);
                display.AttachDrawingObjectToWindow(drawobj);
            }
            if (edges.Count>0)
            {
                stack_edge.DataContext = edges[0];
                try
                {
                    OnSelect(draw[0], display, "setup");
                }
                catch (Exception ex)
                {

                }
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_edge.Items.IndexOf(button.DataContext);
            if (lst_edge.Items[index] != null)
            {
                edge = lst_edge.Items[index]  as EdgeFinder;

                display.DetachDrawingObjectFromWindow(draw[index]);
                draw.RemoveAt(index);
                edges.Remove(edge);
                
            }
        }

        private void btn_edit_item_Click(object sender, RoutedEventArgs e)
        {

        }

        


        private void window_display_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sav = new System.Windows.Forms.SaveFileDialog();
            if (sav.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                HFile file = new HFile(sav.FileName, "output_binary");
                new HTuple(edges.Count).SerializeTuple().FwriteSerializedItem(file);
                foreach (EdgeFinder ed in edges)
                {
                    ed.Save(file);
                }
            }
        }

        private void btn_load_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog op = new System.Windows.Forms.OpenFileDialog();
            if (op.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                DeserializeFactory item = new DeserializeFactory(new HFile(op.FileName, "input_binary"), new HSerializedItem(), op.FileName);
                int count = item.DeserializeTuple();
               for (int i=0;i<count;i++)
                {
                    EdgeFinder edge = new EdgeFinder(item);
                    edges.Add(edge);
                    HDrawingObject drawobj = new HDrawingObject(edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);
                    draw.Add(drawobj);
                    drawobj.OnResize(OnResize);
                    drawobj.OnDrag(OnResize);
                }
            }
        }

        private void btn_apply_size_Click(object sender, RoutedEventArgs e)
        {
            foreach (EdgeFinder ed in edges)
            {
                ed.AdaptToImageSize(w, h);
            }
        }

        private void btn_remove_edge_Click(object sender, RoutedEventArgs e)
        {
           // Button button = sender as Button;
          //  int index = lst_edge.Items.IndexOf(button.DataContext);
            if (lst_edge.SelectedItem != null)
            {
                EdgeFinder edge = lst_edge.SelectedItem as EdgeFinder;
                if (edge != null & last_selected != null)
                {
                    display.DetachDrawingObjectFromWindow(last_selected);
                    int index = draw.FindIndex(x => x.ID == last_selected.ID);
                    draw.RemoveAt(index);
                    edges.Remove(edge);
                    last_selected = null;
                    this.edge = null;
                    if (draw.Count < 1)
                        return;

                        //long max= draw.Max(x => x.ID );
                       // int max_index = draw.FindIndex(x => x.ID == max);
                   
                    //display.DetachDrawingObjectFromWindow(draw[max_index]);
                    // display.para
                   // var draw2 = window_display.HDisplayCurrentObject;
                    //display.AttachDrawingObjectToWindow(draw[max_index]);
                    
                      //  if (max_index > -1)
                      //  {
                          //  
                      //  }
                      foreach (HDrawingObject obj in draw)
                    {
                        display.DetachDrawingObjectFromWindow(obj);
                    }
                    foreach (HDrawingObject obj in draw)
                    {
                        display.AttachDrawingObjectToWindow(obj);
                    }
                    lst_edge.SelectedIndex = 0;
                     stack_edge.DataContext = edges[0];
                       edges[0].ParameterChanged = Edge_PropertyChanged;
                    draw[0].SetDrawingObjectParams("color", "blue");
                    last_selected = draw[0];
                    this.edge = edges[0];
                    //if (draw.Count > 0)
                    //    draw[draw.Count - 1].SetDrawingObjectParams("color", "blue");
                }

            }
        }

        private void plot_MouseDown(object sender, MouseButtonEventArgs e)
        {
          //  TrackerHitResult result = model.GetNearestPoint(new ScreenPoint(e.GetPosition(plot).X, e.GetPosition(plot).Y), false);
            // result.DataPoint
            
        }

        private void series_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // series.
          
        }
        public void GenLinePlot(double x,double range)
        {
          HTuple param=  draw[lst_edge.SelectedIndex].GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            double translate_x = range / 2 - x;
            double col1 = param[1];
            double row1 = param[0];
            double Phi = param[2];
            double Length1 = param[3], Length2 = param[4];
            double disp_col1 = col1 + Length2;
            double disp_col2 = col1 - Length2;
            HHomMat2D trans = new HHomMat2D();
            HHomMat2D rotate = trans.HomMat2dTranslate( -translate_x,0).HomMat2dRotate((double)Phi + (double)new HTuple(90).TupleRad(), (double)row1, (double)col1);
            HTuple trans_col1, trans_row1, trans_col2, trans_row2;
            rotate.AffineTransPixel(row1, disp_col1, out trans_row1, out trans_col1);
            rotate.AffineTransPixel(row1, disp_col2, out trans_row2, out trans_col2);

            LineValue line = new LineValue();
            line.row1 = trans_row1;
            line.row2 = trans_row2;
            line.col1 = trans_col1;
            line.col2 = trans_col2;
          //  display.SetColor("green");
            DisplayLine(line);

        }

        private void num_threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                line_pos.Value = e.NewValue.Value;
                line_neg.Value = -e.NewValue.Value;
            }
        }

        private void cmb_transtion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (cmb_transtion.SelectedItem!=null)
            //{
            //    switch (cmb_transtion.SelectedItem.ToString())
            //    {
            //        case "positive": line_thresh.Y = num_threshold.Value.Value;break;
            //        case "negative": line_thresh.Y = -num_threshold.Value.Value;break;
            //    }
            //}
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (HDrawingObject obj in draw)
            {
                obj?.Dispose();

            }
            foreach (EdgeFinder edge in edges)
            {
                edge.ParameterChanged = null;

            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            expand_profiler.IsExpanded = !expand_profiler.IsExpanded;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
   
            
            edge = new EdgeFinder(w, h) { Row= m_row,Col=m_col };
            edges.Add(edge);
            HDrawingObject drawobj = new HDrawingObject(edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);

            draw.Add(drawobj);
            drawobj.OnResize(OnResize);
            drawobj.OnDrag(OnResize);
            drawobj.OnSelect(OnSelect);
            //drawobj.OnAttach(OnSelect);
            display.AttachDrawingObjectToWindow(drawobj);
            if (edges.Count == 1)
            {
                stack_edge.DataContext = edge;
            }
            if (lst_edge.Items.Count == 1)
            {
                lst_edge.SelectedIndex = 0;
                OnSelect(drawobj, display, "new");
            }

            //OnSelect(drawobj, display, "new");
        }
        double m_row, m_col;

        private void Sld_brushsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (line_pos != null)
            {
                line_pos.Value = e.NewValue;
                line_neg.Value = -e.NewValue;
            }
                
            
        }

        private void RemoveEdge_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (lst_edge.SelectedItem != null)
            {
                btn_remove_edge_Click(null, null);
            }
        }

        private void MenuItem_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            
            AddNewEdge(m_row,m_col);
        }

        private void AddNewEdge(double row,double col)
        {
            edge = new EdgeFinder(w, h) { Row = row, Col = col };
            edges.Add(edge);
            HDrawingObject drawobj = new HDrawingObject(edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);

            draw.Add(drawobj);
            drawobj.OnResize(OnResize);
            drawobj.OnDrag(OnResize);
            drawobj.OnSelect(OnSelect);
            //drawobj.OnAttach(OnSelect);
            display.AttachDrawingObjectToWindow(drawobj);
            if (edges.Count == 1)
            {
                stack_edge.DataContext = edge;
            }
            if (lst_edge.Items.Count == 1)
            {
                lst_edge.SelectedIndex = 0;
                OnSelect(drawobj, display, "new");
            }
        }

        private void btn_add_new_edge_Click(object sender, RoutedEventArgs e)
        {
            var col = window_display.HImagePart.X + window_display.HImagePart.Width / 2;
            var row = window_display.HImagePart.Y + window_display.HImagePart.Height / 2;
            AddNewEdge(row,col);
        }

        private void Window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            m_row = e.Row;
            m_col = e.Column;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //window_display.HalconWindow.DispObj(image);
        }
    }
}
