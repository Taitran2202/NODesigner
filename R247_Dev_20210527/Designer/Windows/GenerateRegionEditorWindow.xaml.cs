using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Interaction logic for GenerateRegionEditorWindow.xaml
    /// </summary>
    public partial class GenerateRegionEditorWindow : Window
    {
        public HRegion region = new HRegion();
        public HWindow display;
        public CollectionOfregion regions;
        public HImage image;
        public HHomMat2D transform;

        public GenerateRegionEditorWindow(HImage image, CollectionOfregion regions, HHomMat2D transform)
        {
            InitializeComponent();
            this.image = image;
            this.regions = regions;
            this.transform = transform;
           
            region = regions.Region;
            lst_region.ItemsSource = regions.regions;
                
            //this.Owner = Application.Current.MainWindow;
        }
       
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("margin");
                display.SetColor("green");
                display.AttachBackgroundToWindow(image);
                window_display.SetFullImagePart(null);
               
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
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private void Window_display_HMouseMove(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
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

        private void window_display_Loaded(object sender, RoutedEventArgs e)
        {
              //Do something
        }

        private void btn_add_curve_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nurbs(false));
        }

        private void btn_add_curve_old_Click(object sender, RoutedEventArgs e)
        {
            //Nurbs region_add = new Nurbs(false);


            //WindowNurbs wd = null;
            //try
            //{
            //    wd = new WindowNurbs(image, region_add, transform, this, false);
            //    wd.ShowDialog();
            //}
            //catch (Exception ex)
            //{
            //    wd?.Close();
            //    return;
            //}

            //if (region_add.rows.Length < 3)
            //{
            //    MessageBox.Show("This region require atleast 3 point!!!");
            //    return;
            //}

            //regions.regions.Add(region_add);
            //region_add.OnUpdated = Update;
            //HDrawingObject draw = region_add.CreateDrawingObject(transform);
            //if (draw != null)
            //{
            //    //window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
            //    draw.OnResize(region_add.OnResize);
            //    draw.OnDrag(region_add.OnResize);
            //    draw.OnSelect(region_add.OnResize);
            //}
            //if (regions.regions.Count == 1)
            //{
            //    Selected_region = regions.regions[0];
            //}
            //lst_region.SelectedItem = region_add;
            //ChangeRegion();
        }

        private void btn_add_rec2_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange2(false));
        }

        private void btn_add_rectangle_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Rectange1(false));
        }

        private void btn_brush_region_Click(object sender, RoutedEventArgs e)
        {
            var added = new BrushRegion(false);
            AddRegionNew(added);
            added.Edit(display, transform);
            ChangeRegion();
        }

        private void Btn_circle_region_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Circle(false));
        }

        private void Btn_ellipse_region_Click(object sender, RoutedEventArgs e)
        {
            AddRegionNew(new Nodes.Ellipse(false));
        }

        private void btn_export_region_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sav = new System.Windows.Forms.SaveFileDialog();
            if (sav.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                regions.Region.WriteRegion(sav.FileName);
            }
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
        private void ColorEdit_ColorChanged(object sender, RoutedEventArgs e)
        {

            DispOverlay();
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

        private void btn_left_Click(object sender, RoutedEventArgs e)
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

        private void btn_right_Click(object sender, RoutedEventArgs e)
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

            }
        }
        double row, col;
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
            ChangeRegion();
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
            if (display == null)
                return;
            display.ClearWindow();
            display.SetDraw("fill");
            display.SetColor("#00ff0025");
            if (transform != null)
            {
                display.DispObj(region.AffineTransRegion(transform, "nearest_neighbor"));
                display.SetDraw("margin");
                display.SetColor(regions.Color);
                display.DispObj(region.AffineTransRegion(transform, "nearest_neighbor"));
            }
            else
            {
                display.DispObj(region);
                display.SetDraw("margin");
                display.SetColor(regions.Color);
                display.DispObj(region);
            }
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            region.Dispose();

            region.GenEmptyRegion();
            foreach (Region item in regions.regions)
            {
                if (!item.Minus)
                {
                    region = region.Union2(item.region);
                }
            }
            foreach (Region item in regions.regions)
            {
                if (item.Minus)
                {
                    region = region.Difference(item.region);
                }
            }

            regions.Region = region;
            DispOverlay();

        }
    }
}
