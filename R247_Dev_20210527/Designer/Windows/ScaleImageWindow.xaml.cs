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
    /// Interaction logic for ScaleImageWindow.xaml`
    /// </summary>
    public partial class ScaleImageWindow : ThemedWindow
    {
        HWindow display;
        int w, h;
        ObservableCollection<ScaleImage> ListScaleImage;
        HImage image;
        HHomMat2D fixture;
        ScaleImage SelectedItem;
        public List<SeriesPoint> Points { get; private set; } = new List<SeriesPoint>();
        List<HDrawingObject> draw = new List<HDrawingObject>();
        public ScaleImageWindow(ObservableCollection<ScaleImage> ListScaleImage,HImage image,HHomMat2D fixture)
        {
            this.image = image;
            image.GetImageSize(out w, out h);
            this.ListScaleImage = ListScaleImage;
            InitializeComponent();
            lst_edge.ItemsSource = ListScaleImage;
            if (ListScaleImage.Count == 1)
                lst_edge.SelectedIndex = 0;
        }

       
        public void RefreshGraphic()
        {
            if(display != null)
            {
                display.ClearWindow();
                display.SetDraw("margin");

                display.SetLineStyle(new HTuple(1,5,7));
                display.SetColor("green");
                foreach (var item in ListScaleImage)
                {
                    if(item != SelectedItem)
                    {
                        if (fixture != null)
                        {
                            display.DispObj(item.Regions.Region.AffineTransRegion(fixture,"constant"));
                        }
                        else
                        {
                            display.DispObj(item.Regions.Region);
                        }
                    }
                        
                    
                }
                if (SelectedItem != null)
                {
                    //display.SetColor("blue");
                    display.SetLineStyle(new HTuple(0));
                    if (fixture != null)
                    {
                        display.DispObj(SelectedItem.Regions.Region.AffineTransRegion(fixture,"constant"));
                    }
                    else
                    {
                        display.DispObj(SelectedItem.Regions.Region);
                    }
                }
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

        
        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lst_edge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_edge.SelectedItem != null)
            {
                SelectedItem = lst_edge.SelectedItem as ScaleImage;
                RefreshGraphic();
            }
        }

        

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var selected = button.DataContext as ScaleImage;
            if (selected != null)
            {
                ListScaleImage.Remove(selected);

            }
        }

        private void btn_new_edge_Click(object sender, RoutedEventArgs e)
        {
            ListScaleImage.Add(new ScaleImage() { Name="ScaleImage"});
            

            // OnSelect(drawobj, display, "new");
        }

        private void window_display_HInitWindow(object  sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetLineWidth(2);
            display.AttachBackgroundToWindow(image);
            window_display.SetFullImagePart(null);

            RefreshGraphic();
            //if (ListScaleImage.Count > 0)
            //{
            //    stack_edge.DataContext = ListScaleImage[0];
            //    try
            //    {
            //        OnSelect(draw[0], display, "setup");
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //}

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var selected = button.DataContext as ScaleImage;
            if (selected != null)
            {
                ListScaleImage.Remove(selected);

            }
        }

        private void btn_edit_item_Click(object sender, RoutedEventArgs e)
        {

        }




        private void window_display_Loaded(object sender, RoutedEventArgs e)
        {

        }

       

        

       

        private void btn_remove_edge_Click(object sender, RoutedEventArgs e)
        {
            
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
           
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        double m_row, m_col;


        private void RemoveEdge_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var selected = lst_edge.SelectedItem as ScaleImage;
            if (selected != null)
            {
                ListScaleImage.Remove(selected);

            }
        }

        private void MenuItem_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var selected = lst_edge.SelectedItem as ScaleImage;
            if (selected != null)
            {
                DrawRegionWindowV2 wd = new DrawRegionWindowV2(image, selected.Regions, fixture);
                wd.Owner = this;
                wd.ShowDialog();
                RefreshGraphic();
            }
        }

        private void AddNewEdge(double row, double col)
        {
           
        }

        private void btn_add_new_edge_Click(object sender, RoutedEventArgs e)
        {
            ListScaleImage.Add(new ScaleImage() { Name="ScaleImage"});
        }

        private void btn_edit_region_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_edge.SelectedItem as ScaleImage;
            if (selected != null)
            {
                DrawRegionWindowV2 wd = new DrawRegionWindowV2(image, selected.Regions, fixture);
                wd.Owner = this;
                wd.ShowDialog();
                RefreshGraphic();
            }
            
        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            foreach(var item in ListScaleImage)
            {
                if (item.Regions.Region.TestRegionPoint(e.Row, e.Column) == 1)
                {
                    lst_edge.SelectedItem = item;
                    break;
                }
            }
        }

        private void Window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            m_row = e.Row;
            m_col = e.Column;
            try
            {

                if (m_row > 0 & m_col > 0 & m_row <= h & m_col <= w)
                    lb_gray_value.Content = image?.GetGrayval((int)m_row, (int)m_col);
                else
                    lb_gray_value.Content = 0;
            }
            catch (Exception ex)
            {

            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //window_display.HalconWindow.DispObj(image);
        }
    }
}
