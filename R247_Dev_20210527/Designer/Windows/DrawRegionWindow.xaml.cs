using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for DrawRegionWindow.xaml
    /// </summary>
    public partial class DrawRegionWindow : Window, INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);
        const uint MAPVK_VK_TO_VSC = 0x00;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        bool _is_draw;
        public bool IsDraw
        {
            get
            {
                return _is_draw;
            }
            set
            {
                if (_is_draw != value)
                {
                    _is_draw = value;
                    if (value)
                        HOperatorSet.SetSystem("cancel_draw_result", "exception");
                    else
                        HOperatorSet.SetSystem("cancel_draw_result", "true");
                    RaisePropertyChanged("IsDraw");

                }
            }
        }

        HImage image;
        public HRegion region = new HRegion();
        public HRegion region_select = new HRegion();
        public CollectionOfregion regions = new CollectionOfregion();
        public void AdapImageSize(HImage image, HWindowControlWPF window)
        {

            HTuple w, h;
            image.GetImageSize(out w, out h);
            window.ImagePart = new Rect(0, 0, w, h);
            if (window.ActualWidth > window.ActualHeight * w / h)
            {
                window.Width = window.ActualHeight * w / h;
            }
            else
            {
                window.Height = window.ActualWidth * h / w;
            }

        }
        HHomMat2D transform;
        public DrawRegionWindow(HImage image, CollectionOfregion regions, HHomMat2D transform)
        {
            image.GetImageSize(out w, out h);
            this.image = image;
            this.transform = transform;
            InitializeComponent();
            window.DataContext = this;
            txt_name.DataContext = regions;

            region = regions.Region;
            this.regions = regions;
            lst_region.ItemsSource = regions.regions;
            color_picker.DataContext = regions;



            IsDraw = false;

        }
        HWindow display;
        public void ChangeRegion()
        {
            if (display == null)
                return;
            region.Dispose();
            display.ClearWindow();
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
            DispOverlay();
            regions.Region = region;


        }
        public void ChangeRegionSource(CollectionOfregion regions)
        {
            this.regions = regions;
            lst_region.ItemsSource = regions.regions;
            region = regions.Region;

            //change region without dispose
            try
            {
                display.ClearWindow();

                DispOverlay();
            }
            catch (Exception ex)
            {
            }

        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("margin");
            display.SetColor("green");
            display.AttachBackgroundToWindow(image);

        }
        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_region.SelectedIndex != -1)
            {
                HOperatorSet.ClearWindow(display);
                region_select = (lst_region.SelectedItem as Region).region;
                DispOverlay();
                display.SetDraw("margin");
                display.SetColor("blue");
                if (transform != null)
                    display.DispObj(region_select.AffineTransRegion(transform, "nearest_neighbor"));
                else
                    display.DispObj(region_select);
                //  display.DispObj((lst_region.SelectedItem as Region).region );
                display.SetDraw("fill");
            }
        }
        public void DispOverlay()
        {
            display.SetDraw("fill");
            display.SetColor(regions.Color);
            if (transform != null)
                display.DispObj(region.AffineTransRegion(transform, "nearest_neighbor"));
            else
                display.DispObj(region);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  HImage image = new HImage();
            // window_display.HalconWindow.DispImage(image);
            AdapImageSize(image, window_display);
            HOperatorSet.SetSystem("cancel_draw_result", "true");
            HalconAPI.CancelDraw();
        }


        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        HDrawingObject obj = new HDrawingObject();
        private void btn_draw_nurbs_Click(object sender, RoutedEventArgs e)
        {
            DrawNewRegion(new Nurbs(false));
            //HalconAPI.CancelDraw();
        }
        public int w, h;
        private void DrawNewRegion(Region item)
        {
            IsDraw = true;
            is_cancel = false;
            try
            {
                item.Draw(display, transform, w, h);
                if (is_cancel)
                {
                    IsDraw = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                //regions.regions.Add(item);
                //lst_region.SelectedItem = item;
                //ChangeRegion();
                IsDraw = false;
                ////MessageBox.Show(ex.Message);
                return;
            }
            regions.regions.Add(item);
            lst_region.SelectedItem = item;
            ChangeRegion();
            IsDraw = false;
        }

        private void window_display_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btn_draw_rectangle1_Click(object sender, RoutedEventArgs e)
        {
            // HalconAPI.CancelDraw();

            DrawNewRegion(new Rectange1(false));

        }

        private void btn_draw_rectangle2_Click(object sender, RoutedEventArgs e)
        {
            DrawNewRegion(new Rectange2(false));
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);

            if (lst_region.Items[index] != null)
            {
                regions.regions.RemoveAt(index);
                ChangeRegion();
            }
        }

        private void btn_edit_Click(object sender, RoutedEventArgs e)
        {
            // HalconAPI.CancelDraw();
            display.ClearWindow();
            IsDraw = true;
            HOperatorSet.SetSystem("cancel_draw_result", "true");
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);

            if (lst_region.Items[index] != null)
            {
                (lst_region.Items[index] as Region).Edit(display, transform);
                ChangeRegion();
            }
            IsDraw = false;
        }



        private void btn_close_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void btn_draw_nurbs_minus_Click(object sender, RoutedEventArgs e)
        {
            DrawNewRegion(new Nurbs(true));
        }

        private void btn_draw_rectangle_minus_Click(object sender, RoutedEventArgs e)
        {
            // HalconAPI.CancelDraw();
            DrawNewRegion(new Rectange1(true));
        }

        private void btn_draw_rectangle2_minus_Click(object sender, RoutedEventArgs e)
        {
            DrawNewRegion(new Rectange2(true));
        }
        bool is_cancel = false;
        private void btn_cancel_region_Click(object sender, RoutedEventArgs e)
        {
            is_cancel = true;
            HalconAPI.CancelDraw();

        }

        private void btn_ok_region_Click(object sender, RoutedEventArgs e)
        {
            HOperatorSet.SetSystem("cancel_draw_result", "true");
            HalconAPI.CancelDraw();
        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sav = new System.Windows.Forms.SaveFileDialog();
            if (sav.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                regions.Save(new HFile(sav.FileName, "output_binary"));
            }
        }

        private void btn_import_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog op = new System.Windows.Forms.OpenFileDialog();
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    regions.Load(new DeserializeFactory(new HFile(op.FileName, "input_binary"), new HSerializedItem(), op.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wrong type of file!!");
                }
            }
        }

        private void btn_edit_point_Click(object sender, RoutedEventArgs e)
        {
            IntPtr handle;
            window_display.HalconWindow.GetOsWindowHandle(out handle);
            PostMessage(handle, 0x0100, 0x10, 1);
            Thread.Sleep(100);
            PostMessage(handle, 0x0101, 0x10, 1);

        }
        IntPtr windowHandle;
        private void window_Initialized(object sender, EventArgs e)
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            HOperatorSet.SetSystem("cancel_draw_result", "exception");
        }

        private void window_Closed(object sender, EventArgs e)
        {


            HalconAPI.CancelDraw();
        }

        private void btn_shift_Click(object sender, RoutedEventArgs e)
        {
            // System.Windows.Forms.SendKeys.SendWait("+");
        }
    }
}
