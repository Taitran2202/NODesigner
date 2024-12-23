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
    public partial class FindLinesWindow : ThemedWindow
    {
        HWindow display;
        HImage image;
        HImage image_gray;
        int w, h;


        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        List<Rectange2> draw = new List<Rectange2>();
        HHomMat2D fixture;
        public FindLinesWindow(ObservableCollection<LineFinder> edges,HImage image,HHomMat2D fixture)
        {
            this.edges = edges;
            
            this.image = image;
            if (image.CountChannels() == 3)
            {
                image_gray = image.Rgb1ToGray();
            }
            else
            {
                image_gray = image.Clone();
            }
            
            image.GetImageSize(out w, out h);
            if (fixture != null)
            {
                this.fixture = fixture;
            }
            else
            {
                this.fixture = new HHomMat2D();
            }
            InitializeComponent();
            lst_edge.ItemsSource = edges;
            if (edges.Count == 1)
                lst_edge.SelectedIndex = 0;
            series.Points?.Clear();
            series.Points.AddRange(Points);

        }
        int currentIndex = -1;
        
        public void RefreshGraphic()
        {
            display.ClearWindow();
            display.SetColor("green");
            foreach (LineFinder edge in edges)
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
        public void OnUpdate(Region rect)
        {
            var rec2 = rect as Rectange2;
            if (currentIndex == -1 | currentIndex>=edges.Count)
            {
                currentIndex = draw.IndexOf(rec2);
                lst_edge.SelectedIndex = currentIndex;
                stack_edge.DataContext = edges[currentIndex];
                edges[currentIndex].ParameterChanged = Edge_PropertyChanged;
            }
            var param = rec2.current_draw.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            edges[currentIndex].UpdateMeasureWithResult(param, fixture, w, h, image_gray, display );
            RefreshGraphic();
            Points = edges[currentIndex].GetLineProfile(image_gray);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                series.Points.Clear();
                series.Points.AddRange(Points);
            }
            ));
        }
        public void OnSelect(Region rect)
        {
            var rec2 = rect as Rectange2;
            currentIndex = draw.IndexOf(rec2);
            rec2.current_draw.SetDrawingObjectParams("color", "red");
            foreach(var item in draw)
            {
                if (item.current_draw != rec2.current_draw)
                {
                    item.current_draw.SetDrawingObjectParams("color", "green");
                }
                
            }
            lst_edge.SelectedIndex = currentIndex;
            stack_edge.DataContext = edges[currentIndex];
            edges[currentIndex].ParameterChanged = Edge_PropertyChanged;
            if(edges[currentIndex].Width != w | edges[currentIndex].Height != h){
                DXMessageBox.Show(this, String.Format("Current image size ({0},{1}) not match with edge tool size ({2},{3}). Please click Adapt size to update edge tool size!",
                    w, h, edges[currentIndex].Width, edges[currentIndex].Height
                    ), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            OnUpdate(rect);

        }
        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        public ObservableCollection<LineFinder> edges;
        LineFinder edge;
        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lst_edge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_edge.SelectedIndex>-1)
            edges[lst_edge.SelectedIndex].ParameterChanged = Edge_PropertyChanged;
        }

        private void Edge_PropertyChanged(LineFinder sender, object e)
        {
            if (e is bool)
            {
                
                return;
            }
            var line = sender.Run( image_gray, fixture);
            sender.last_line = line.Line;
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
           
            edge = new LineFinder(w, h);
            edges.Add(edge);
            AddLineFinderDrawObject(edge);
        }
        public void AddLineFinderDrawObject(LineFinder edge)
        {
            var new_rect = new Rectange2(false, edge.Row, edge.Col, edge.Phi, edge.Length1, edge.Length2);
            var drawobject = new_rect.CreateDrawingObject(fixture);
            drawobject.OnResize(new_rect.OnResize);
            drawobject.OnSelect(new_rect.OnSelect);
            drawobject.OnDrag(new_rect.OnResize);
            new_rect.OnSelected = OnSelect;
            new_rect.OnUpdated = OnUpdate;
            draw.Add(new_rect);
            display.AttachDrawingObjectToWindow(drawobject);
            var line = edge.Run(image_gray, fixture);
            edge.last_line = line.Line;
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetLineWidth(1);
            display.AttachBackgroundToWindow(image);
            window_display.SetFullImagePart(null);
            
            foreach (LineFinder edge in edges)
            {
                AddLineFinderDrawObject(edge);
            }
            
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedEdge();
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
                foreach (LineFinder ed in edges)
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
                    LineFinder edge = new LineFinder(item);
                    edges.Add(edge);
                    AddLineFinderDrawObject(edge);
                }
            }
        }

        private void btn_apply_size_Click(object sender, RoutedEventArgs e)
        {
            foreach (LineFinder ed in edges)
            {
                ed?.AdaptToImageSize(w, h);
            }
        }

        private void btn_remove_edge_Click(object sender, RoutedEventArgs e)
        {
            if (lst_edge.SelectedItem != null)
            {
                //LineFinder edge = lst_edge.SelectedItem as LineFinder;
                //var index =lst_edge.Items.IndexOf(edge);
                //if (edge != null)
                //{
                //    edges.Remove(edge);
                //    draw[index].ClearDrawingData(display);
                //    draw.RemoveAt(index);
                //    currentIndex = -1;
                //}
                RemoveSelectedEdge();
            }
        }

        private void plot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void series_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var obj in draw)
            {
                obj.ClearDrawingData(display);

            }
            foreach (LineFinder edge in edges)
            {
                edge.ParameterChanged = null;

            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            
        }

        

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
   
            
            edge = new LineFinder(w, h) { Row= m_row,Col=m_col };
            edges.Add(edge);
            AddLineFinderDrawObject(edge);
        }
        double m_row, m_col;

        private void Sld_brushsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
                line_pos.Value = e.NewValue;
                line_neg.Value = -e.NewValue;
            
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
        private void bnt_add_tool_Click(object sender, RoutedEventArgs e)
        {
            window_display.HalconWindow.GetPart(out int r1, out int c1, out int c2, out int r2);
            var cx = (c1 + c2) / 2;
            var cy = (r1 + r2) / 2;
            edge = new LineFinder(w, h) { Row = cy, Col = cx };
            edges.Add(edge);
            AddLineFinderDrawObject(edge);
        }

        private void MenuItem_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            edge = new LineFinder(w, h) { Row = m_row, Col = m_col };
            edges.Add(edge);
            AddLineFinderDrawObject(edge);
        }
        void RemoveSelectedEdge()
        {
            int index = lst_edge.SelectedIndex;
            if(index != -1)
            {
                if (DXMessageBox.Show(this, "Do you want to remove " + (lst_edge.Items[index] as LineFinder).Name + "?", "warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    if (lst_edge.Items[index] != null)
                    {
                        edge = lst_edge.Items[index] as LineFinder;
                        draw[index].ClearDrawingData(display);
                        draw.RemoveAt(index);
                        edges.Remove(edge);

                    }
                }
            }
            
        }
        private void RemoveEdge_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            RemoveSelectedEdge();
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
