using DevExpress.Xpf.Core;
using HalconDotNet;
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

namespace NOVisionDesigner.Designer.Deeplearning.Windows
{
    /// <summary>
    /// Interaction logic for SmartLabelWindow.xaml
    /// </summary>
    public partial class SmartLabelWindow : ThemedWindow,INotifyPropertyChanged
    {
        bool IsRunning = true;
        public CollectionOfregion Regions = new CollectionOfregion();
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _is_adding=true;
        public bool IsAdding
        {
            get
            {
                return _is_adding;
            }
            set
            {
                if (_is_adding != value)
                {
                    _is_adding = value;
                    if (!_is_adding)
                    {
                        window_display.Cursor = Cursors.No;
                    }
                    else
                    {
                        window_display.Cursor = Cursors.Cross;
                    }
                    
                    RaisePropertyChanged("IsAdding");
                }
            }
        }
        bool _is_loading;
        public bool IsLoading
        {
            get
            {
                return _is_loading;
            }
            set
            {
                if (_is_loading != value)
                {
                    _is_loading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }

        HImage image;
        SegmentAnything.SegmentAnythingRuntime runtime = SegmentAnything.SegmentAnythingRuntime.Instance;
        public SmartLabelWindow(HImage image)
        {
            this.image = image;
            InitializeComponent();
            this.DataContext = this;
            lst_region.ItemsSource = Regions.regions;
            Task.Run(() =>
            {
                IsLoading = true;
                LoadImageEmbededing();
                IsLoading = false;
            });
        }
        void LoadImageEmbededing()
        {
            if (image != null)
            {
                runtime.SetImage(image);
                window_display.HMouseMove += window_display_HMouseMove;
                window_display.HMouseDown += window_display_HMouseDown;
            }
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            if (image != null)
            {
                window_display.HalconWindow.AttachBackgroundToWindow(image);
                window_display.HalconWindow.SetColor("#0000ff30");
                window_display.HalconWindow.SetDraw("margin");
            }
        }
        Task segmentTask = null;
        public double last_row,last_column;
        bool waitToRun = false;
        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!IsRunning)
            {
                return;
            }
            last_row = e.Row;
            last_column = e.Column;
            waitToRun = true;
            if (runtime.Points.Count > 0)
            {
                return;
            }
            if (segmentTask == null)
            {
                segmentTask = Task.Run(() =>
                {
                    var region = runtime.RunDecoder((float)e.Column, (float)e.Row);
                    Render(region);
                });
            }
            else
            {
                if (!segmentTask.IsCompleted)
                {
                    
                }
                else
                {
                    if (segmentTask.IsCompleted)
                    {
                        segmentTask = Task.Run(() =>
                        {
                            var region = runtime.RunDecoder((float)e.Column, (float)e.Row);
                            Render(region);
                            while (waitToRun)
                            {
                                waitToRun = false;
                            region = runtime.RunDecoder((float)last_column, (float)last_row
                                
                                );
                                Render(region);

                            }
                        });
                    }
                    
                }
            }
        }

        private void btn_reset_Click(object sender, RoutedEventArgs e)
        {
            runtime.Reset();
            window_display.HalconWindow.ClearWindow();
            //window_display.HalconWindow.DetachDrawingObjectFromWindow(CurrentEditingRegion.current_draw);
        }
        HRegion region;
        void Render(HRegion region)
        {
            window_display.HalconWindow.ClearWindow();
            window_display.HalconWindow.SetColor("#0000ff90");
            window_display.HalconWindow.SetDraw("margin");
            region.DispRegion(window_display.HalconWindow);
            window_display.HalconWindow.SetColor("#0000ff30");
            window_display.HalconWindow.SetDraw("fill");
            region.DispRegion(window_display.HalconWindow);
            //render adding points
            foreach(var item in runtime.Points)
            {
                if (item.Class == 0)
                {
                    window_display.HalconWindow.SetColor("red");
                    window_display.HalconWindow.DispCross((double)item.Y, item.X, 20, 3.14 / 4);

                }
                else
                {
                    window_display.HalconWindow.SetColor("blue");
                    window_display.HalconWindow.DispCross((double)item.Y, item.X, 20,0);
                }
                
            }
        }
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!IsRunning)
            {
                return;
            }
            if (IsAdding)
            {
                region= runtime.AddPoint((float)e.Column,(float) e.Row);
                Render(region);
            }
            else
            {
                region = runtime.RemovePoint((float)e.Column, (float)e.Row);
                Render(region);

            }
        }
        public void Update(Region sender)
        {
            Render(sender.region);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_polygon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                region.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
                HRegion hRegion = new HRegion();
                hRegion.GenRegionPolygonFilled(rows,cols);
                region = hRegion;
                Render(region);
            }
            catch (Exception ex)
            {

            }
        }
        private void btn_accept_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = false;
            panel_fit_region.Visibility = Visibility.Visible;
        }

        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (lst_region.SelectedItem != null)
                {
                    Render((lst_region.SelectedItem as Region).region);
                }
                
            }catch(Exception ex)
            {

            }
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);

            if (lst_region.Items[index] != null)
            {
                Regions.regions[index].ClearDrawingData(window_display.HalconWindow);
                Regions.regions.RemoveAt(index);
            }
        }
        private void AddRegion(Region region)
        {
            if (CurrentEditingRegion != null)
            {
                CurrentEditingRegion.ClearDrawingData(window_display.HalconWindow);
            }
            Region region_add = region;
            // regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(new HHomMat2D());
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            CurrentEditingRegion = region;
            region.CreateRegion();
            
        }
        void ClearEditingObject()
        {
            if (CurrentEditingRegion != null)
            {
                CurrentEditingRegion.ClearDrawingData(window_display.HalconWindow);
            }
        }
        Region CurrentEditingRegion;
        private void rad_curve_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                region.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
                HXLDCont cont = new HXLDCont();
                cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
                var newRegion = cont.GenRegionContourXld("filled");
                var newRegionDraw = new Nurbs(false) { cols = cols, rows = rows };
                AddRegion(newRegionDraw);
                
                Render(newRegion);
            }
            catch (Exception ex)
            {

            }
        }

        private void rad_rec_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                region.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                var newRegion = new Rectange1(false) { row1 = r1, row2 = r2, col1 = c1, col2 = c2 };
                AddRegion(newRegion); ;

                Render(newRegion.region);
            }
            catch (Exception ex)
            {

            }
        }

        private void rad_poly_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                region.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
                HRegion hRegion = new HRegion();
                hRegion.GenRegionPolygonFilled(rows, cols);

                AddRegion(new BrushRegion(false) { region = hRegion }); ;

                Render(hRegion);
            }
            catch (Exception ex)
            {

            }
        }

        private void rad_rec2_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                region.SmallestRectangle2(out double r, out double c, out double phi, out double l1,out double l2);
                var newRegion = new Rectange2(false) { row = r, col = c, phi = phi, length1 = l1,length2=l2 };
                AddRegion(newRegion); ;

                Render(newRegion.region);
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_accept_region_Clicked(object sender, RoutedEventArgs e)
        {
            panel_fit_region.Visibility = Visibility.Hidden;
            ClearEditingObject();
            IsRunning = true;
            Regions.regions.Add(CurrentEditingRegion);
            runtime.Reset();
            window_display.HalconWindow.ClearWindow();
            window_display.Cursor = Cursors.Arrow;
        }

        private void btn_cancel_region_Clicked(object sender, RoutedEventArgs e)
        {
            panel_fit_region.Visibility = Visibility.Hidden;
            ClearEditingObject();
            IsRunning = true;
            runtime.Reset();
            window_display.HalconWindow.ClearWindow();
        }

        private void btn_intant_accept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                panel_fit_region.Visibility = Visibility.Hidden;
                ClearEditingObject();
                IsRunning = true;
                Regions.regions.Add(new BrushRegion(false) { region = region });
                runtime.Reset();
                window_display.HalconWindow.ClearWindow();
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_curve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                region.GetRegionPolygon(2.5, out HTuple rows, out HTuple cols);
                HXLDCont cont = new HXLDCont();
                cont.GenContourNurbsXld(rows, cols, "auto", "auto", 3, 1, 5);
                region = cont.GenRegionContourXld("filled");
                Render(region);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
