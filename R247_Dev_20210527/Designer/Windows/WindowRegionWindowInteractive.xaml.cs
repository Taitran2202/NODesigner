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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for WindowRegionWindowInteractive.xaml
    /// </summary>
    public partial class WindowRegionWindowInteractive : ThemedWindow,INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        int _roi_x;
        public int ROIX
        {
            get
            {
                return _roi_x;
            }
            set
            {
                if (_roi_x != value)
                {
                    _roi_x = value;
                    RaisePropertyChanged("ROIX");
                }
            }
        }
        int _roi_y;
        public int ROIY
        {
            get
            {
                return _roi_y;
            }
            set
            {
                if (_roi_y != value)
                {
                    _roi_y = value;
                    RaisePropertyChanged("ROIY");
                }
            }
        }
        int _roi_w;
        public int ROIW
        {
            get
            {
                return _roi_w;
            }
            set
            {
                if (_roi_w != value)
                {
                    _roi_w = value;
                    RaisePropertyChanged("ROIW");
                }
            }
        }
        int roi_h;
        public int ROIH
        {
            get
            {
                return roi_h;
            }
            set
            {
                if (roi_h != value)
                {
                    roi_h = value;
                    RaisePropertyChanged("ROIH");
                }
            }
        }
        //public HRegion region = new HRegion();
        HWindow display;
        public CollectionOfregion regions;
        HImage image;
        HHomMat2D transform;
        public WindowRegionWindowInteractive(HImage image, CollectionOfregion regions, HHomMat2D transform,bool showFooter=false)
        {

            //region = regions.Region;
            InitializeComponent();
            try
            {
                this.regions = regions;
                this.image = image; this.transform = transform;
                lst_region.ItemsSource = regions.regions;
                //this.Owner = Application.Current.MainWindow;
                this.DataContext = this;
                if (!showFooter)
                {
                    grid_footer.Visibility = Visibility.Collapsed;
                }
            }catch(Exception ex)
            {
                DXMessageBox.Show(this,ex.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            
        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("margin");
                display.SetColor("green");
                if (image != null)
                {
                    display.AttachBackgroundToWindow(image);
                    window_display.SetFullImagePart(null);
                }
                
                foreach (Region region in regions.regions)
                {
                    AddRegion(region);
                }
                if (regions.regions.Count > 0)
                {
                    Selected_region = regions.regions[0];
                    lst_region.SelectedItem = regions.regions[0];
                }
                color_background.DataContext = regions;
                ChangeRegion();

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                //this.Close();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  HImage image = new HImage();
            // window_display.HalconWindow.DispImage(image);
            //try
            //{
            //    window_display.HalconWindow.DispObj(image);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error at Window_Loaded: "+ex.Message);
            //    this.Close();
            //}
            //window_display.HalconWindow.DispImage(image);  HOperatorSet.SetSystem("cancel_draw_result", "true");

        }
        private void btn_add_rectangle_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange1(false));

        }

        private void AddRegionNew(Region region)
        {
            region.Initial((int)row, (int)col);
            Region region_add = region;
            regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            if (regions.regions.Count == 1)
            {
                Selected_region = regions.regions[0];
            }
            lst_region.SelectedItem = region_add;
            ChangeRegion();
            //ChangeRegion();
        }

        private void AddRegion(Region region)
        {
            Region region_add = region;
            // regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
        }
        Region selected_region = null;

        public Region Selected_region
        {
            get => selected_region;
            set
            {
                if (value != selected_region)
                {
                    //stack_parameter.DataContext = value;
                    selected_region = value;
                }
            }
        }

        public void Update(Region sender)
        {
            Selected_region = sender;
            ChangeRegion();
            DispOverlay();
        }
        public void DispOverlay()
        {
            if (display == null )
                return;
            display.ClearWindow();
            display.SetDraw("fill");
            display.SetColor("#00ff0025");
            if (transform != null )
            {
                display.DispObj(regions.Region.AffineTransRegion(transform, "nearest_neighbor"));
                display.SetDraw("margin");
                display.SetColor(regions.Color);
                display.DispObj(regions.Region.AffineTransRegion(transform, "nearest_neighbor"));
                if (selectedRegion != null)
                {
                    display.SetDraw("margin");
                    display.SetLineWidth(2);
                    display.SetColor("blue");
                    display.DispObj(selectedRegion.AffineTransRegion(transform, "nearest_neighbor"));
                    display.SetLineWidth(1);
                }
            }
            else
            {
                display.DispObj(regions.Region);
                display.SetDraw("margin");
                display.SetColor(regions.Color);
                display.DispObj(regions.Region);
                if (selectedRegion != null)
                {
                    display.SetDraw("margin");
                    display.SetLineWidth(2);
                    display.SetColor("blue");
                    display.DispObj(selectedRegion);
                    display.SetLineWidth(1);
                }
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            if(selected_region == null)
            {
                return;
            }
            if(selected_region is Rectange1)
            {
                var data= selected_region as Rectange1;
                ROIX =(int) data.col1;
                ROIY = (int)data.row1;
                ROIW = (int)(data.col2 - data.col1);
                ROIH = (int)(data.row2 - data.row1);
            }
            else
            {
                selected_region.region.SmallestRectangle1(out int r1, out int c1, out int r2, out int c2);
                ROIX = c1;
                ROIY = r1;
                ROIW = c2 - c1;
                ROIH = r2 - r1;
            }
            
            regions.MergeRegion();       
            DispOverlay();

        }
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            //RefreshGraphic();
        }

        private void btn_add_rec2_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);

            if (lst_region.Items[index] != null)
            {
                regions.regions[index].ClearDrawingData(display);
                regions.regions.RemoveAt(index);
                ChangeRegion();
                DispOverlay();
            }
        }

        private void OnRectangle1_Click(object sender, RoutedEventArgs e)
        {

            AddRegionNew(new Rectange1(false));
        }

        private void OnRectangle2_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            if (Selected_region != null)
            {
                Selected_region.ClearDrawingData(display);
                regions.regions.Remove(Selected_region);
                ChangeRegion();
                DispOverlay();
                Selected_region = null;
            }
        }

        private void color_background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            DispOverlay();
        }
        double row, col;

        private void btn_export_region_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sav = new System.Windows.Forms.SaveFileDialog();
            if (sav.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                regions.Region.WriteRegion(sav.FileName);
            }
        }
        HRegion selectedRegion = null;
        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Region selected = lst_region.SelectedItem as Region;
            if (selected == null)
            {
                stack_parameter.DataContext = null;
                foreach (Region region in regions.regions)
                {
                    if (region.current_draw != null)
                        display.DetachDrawingObjectFromWindow(region.current_draw);
                }
                selectedRegion = null;
            }
            else
            {
                stack_parameter.DataContext = selected;

                foreach (Region region in regions.regions)
                {
                    if (region.current_draw != null)
                        display.DetachDrawingObjectFromWindow(region.current_draw);
                }
                if (selected.current_draw != null)
                    display.AttachDrawingObjectToWindow(selected.current_draw);
                if(selected is BrushRegion)
                {
                    selectedRegion = selected.region;
                }
                else
                {
                    selectedRegion = null;
                }
            }
            DispOverlay();
        }

        private void btn_add_curve_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nurbs(false));
        }

        private void OnCurve_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nurbs(false));
        }


        private void window_display_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selected = lst_region.SelectedItem as Region;
            if (selected != null)
            {
                if (selected is Nurbs)
                {
                    lst_region.SelectedIndex = -1;
                }
            }
        }

        private void btn_brush_region_Click(object sender, RoutedEventArgs e)
        {
            var added = new BrushRegion(false);
            AddRegionNew(added);
            added.Edit(display, transform);
            ChangeRegion();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_region.SelectedItem as Region;
            if (selected != null)
            {
                if (selected is BrushRegion)
                {
                    selected.Edit(display, transform);
                    ChangeRegion();
                }
                else
                {
                    DXMessageBox.Show("Edit brush is only avaliable for brush region!!");
                }
            }
        }
        public double speed = 1;
        CubicEase calc = new CubicEase();
        double counter = 1;
        void IncreateSpeed()
        {

            calc.EasingMode = EasingMode.EaseIn;

            if (counter > 10)
            {
                counter = 10;
            }
            else
            {
                speed = calc.Ease(counter / 10) * 20;
                if (speed < 1)
                    speed = 1;
                counter++;
            }
        }
        private void btn_up_Click(object sender, RoutedEventArgs e)
        {

            IncreateSpeed();

            lst_region.SelectedIndex = -1;
            HHomMat2D mat = new HHomMat2D();
            mat.HomMat2dIdentity();
            mat = mat.HomMat2dTranslate(-speed, 0.0);
            foreach (Region item in regions.regions)
            {
                item.TransformRegion(mat);
            }
            ChangeRegion();
        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {
            IncreateSpeed();
            lst_region.SelectedIndex = -1;
            HHomMat2D mat = new HHomMat2D();
            mat.HomMat2dIdentity();
            mat = mat.HomMat2dTranslate(speed, 0.0);
            foreach (Region item in regions.regions)
            {
                item.TransformRegion(mat);
            }
            ChangeRegion();
        }

        private void btn_right_Click(object sender, RoutedEventArgs e)
        {
            IncreateSpeed();
            lst_region.SelectedIndex = -1;
            HHomMat2D mat = new HHomMat2D();
            mat.HomMat2dIdentity();
            mat = mat.HomMat2dTranslate(0.0, speed);
            foreach (Region item in regions.regions)
            {
                item.TransformRegion(mat);
            }
            ChangeRegion();
        }

        private void btn_left_Click(object sender, RoutedEventArgs e)
        {
            IncreateSpeed();
            lst_region.SelectedIndex = -1;
            HHomMat2D mat = new HHomMat2D();
            mat.HomMat2dIdentity();
            mat = mat.HomMat2dTranslate(0.0, -speed);
            foreach (Region item in regions.regions)
            {
                item.TransformRegion(mat);
            }
            ChangeRegion();
        }

        private void btn_up_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            speed = 1;
            counter = 1;
        }

        private void btn_add_curve_old_Click(object sender, RoutedEventArgs e)
        {


            Nurbs region_add = new Nurbs(false);


            WindowNurbs wd = null;
            try
            {
                wd = new WindowNurbs(image, region_add, transform, this, false);
                wd.ShowDialog();
            }
            catch (Exception ex)
            {
                wd?.Close();
                return;
            }

            if (region_add.rows.Length < 3)
            {
                MessageBox.Show("This region require atleast 3 point!!!");
                return;
            }

            regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            if (regions.regions.Count == 1)
            {
                Selected_region = regions.regions[0];
            }
            lst_region.SelectedItem = region_add;
            ChangeRegion();
        }

        private void ColorEdit_ColorChanged(object sender, RoutedEventArgs e)
        {
            DispOverlay();
        }

        private void Btn_circle_region_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Circle(false));
        }

        private void Btn_ellipse_region_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nodes.Ellipse(false));
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            //this.regions.GenRegionBorder();
            this.DialogResult = true;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            try
            {
                row = e.Row;
                col = e.Column;
            }
            catch (Exception ex)
            {

            }
        }
    }
}
