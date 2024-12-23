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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window, INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #region Field
        public bool UpdateAll { get; set; } = false;
        MultiImageFilter filter;
        IMultiImageFilter selected_filter;
        HImage image;
        HWindow display = null;
        HHomMat2D hom;
        public HRegion region = new HRegion();
        CollectionOfregion region_collection;
        double row, col;
        Region selected_region = null;

        public Region Selected_region
        {
            get => selected_region;
            set
            {
                if (value != selected_region)
                {
                    // stack_parameter.DataContext = value;
                    selected_region = value;
                }
            }
        }
        #endregion


        public FilterWindow(HImage image, HHomMat2D hom, MultiImageFilter filter)
        {
            this.image = image;
            this.hom = hom;
            this.filter = filter;
            InitializeComponent();
            properties.SelectedObject = selected_filter;
            this.hom = hom != null ? hom : new HHomMat2D();
            lst_region.ItemsSource = filter.filters;
            region_collection = filter.Regions;
            region = region_collection.Region;
        }


        #region Method
        public void MoveItem(int direction)
        {
            // Checking selected item
            if (lst_region.SelectedItem == null || lst_region.SelectedIndex < 0)
                return; // No selected item - nothing to do

            // Calculate new index using move direction
            int newIndex = lst_region.SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= lst_region.Items.Count)
                return; // Index out of range - nothing to do

            IMultiImageFilter selected = lst_region.SelectedItem as IMultiImageFilter;
            if (selected != null)
            {
                // Removing removable element
                filter.filters.Remove(selected);
                // Insert it in new position
                filter.filters.Insert(newIndex, selected);
                // Restore selection
                lst_region.SelectedIndex = newIndex;
            }
        }
        public void DispOverlay()
        {
            display.ClearWindow();
            display.SetDraw("fill");
            display.SetColor(region_collection.Color);
            if (hom != null)
                display.DispObj(region.AffineTransRegion(hom, "nearest_neighbor"));
            else
                display.DispObj(region);
        }
        public void ChangeRegion()
        {
            if (display == null)
                return;
            region.Dispose();

            region.GenEmptyRegion();
            foreach (Region item in region_collection.regions)
            {
                if (!item.Minus)
                {
                    region = region.Union2(item.region);
                }
            }
            foreach (Region item in region_collection.regions)
            {
                if (item.Minus)
                {
                    region = region.Difference(item.region);
                }
            }
            //DispOverlay();
            region_collection.Region = region;


        }
        private void AddRegion(Region region)
        {
            Region region_add = region;
            // regions.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(hom);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
        }
        private void AddRegionNew(Region region)
        {

            region.Initial((int)row, (int)col);

            Region region_add = region;
            region_collection.regions.Add(region_add);
            region_add.OnUpdated = Update;
            HDrawingObject draw = region_add.CreateDrawingObject(hom);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(region_add.OnResize);
                draw.OnDrag(region_add.OnResize);
                draw.OnSelect(region_add.OnResize);
            }
            if (region_collection.regions.Count == 1)
            {
                Selected_region = region_collection.regions[0];
            }
            DispOverlay();
        }

        #endregion

        #region Event Handler
        public void Update(Region sender)
        {


            display.ClearWindow();
            Selected_region = sender;
            ChangeRegion();
            //  DispOverlay();
            //   display.SetDraw("margin");

            try
            {
                if (UpdateAll)
                    display.DispObj(filter.Update(image, hom));
                else
                    display.DispObj(filter.UpdateSingle(image, hom, selected_filter));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        #endregion
        #region View
        private void btn_add_gauss_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new GaussImage());
        }

        private void btn_dilation_rec_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new DilationRectImage());
        }

        private void btn_binominal_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new BinomialImage());
        }

        private void btn_canny_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new CannyImage());
        }

        private void btn_laplace_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new LaplaceImage());
        }

        private void btn_median_rect_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new MedianRectangleImage());
        }

        private void btn_deriche1_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new Deriche1());
        }

        private void btn_deriche2_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new Deriche2());
        }

        private void btn_shen_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new Shen());
        }

        private void btn_invert_image_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new InvertImage());
        }

        private void Btn_laplace_2_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new Laplace());
        }

        private void btn_select_channel_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new SelectChannel());
        }

        private void Btn_extract_color_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAll = true;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateAll = false;
        }

        private void btn_up_Click(object sender, RoutedEventArgs e)
        {
            MoveItem(-1);
        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {
            MoveItem(1);
        }

        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_region.SelectedItem != null)
                properties.SelectedObject = lst_region.SelectedItem;
            selected_filter = lst_region.SelectedItem as IMultiImageFilter;
        }

        #endregion

        private void Setting_Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Tab_Grid.Focus();
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            if (image == null) { return; }
            display.AttachBackgroundToWindow(image);
            display.SetColor("#ff0000aa");

            foreach (Region region in region_collection.regions)
            {
                AddRegion(region);
            }
            if (region_collection.regions.Count > 0)
            {
                Selected_region = region_collection.regions[0];
            }
        }

        private void window_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);
            if (index > -1)
                if (lst_region.Items[index] != null)
                {
                    filter.filters.RemoveAt(index);
                    //  ChangeRegion();
                    try
                    {
                        display.ClearWindow();
                        if (UpdateAll)
                            display.DispObj(filter.Update(image, hom));
                        else
                            display.DispObj(filter.UpdateSingle(image, hom, selected_filter));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
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

        private void btn_add_erode_Click(object sender, RoutedEventArgs e)
        {
            filter.filters.Add(new ErodeRectImage());
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            if (Selected_region != null)
            {
                Selected_region.ClearDrawingData(display);
                region_collection.regions.Remove(Selected_region);
                ChangeRegion();
                DispOverlay();
                Selected_region = null;
            }
        }
    }
}
