using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for TrainColorMlpWindow.xaml
    /// </summary>
    public partial class TrainColorMlpWindow : ThemedWindow
    {
        public string current_color = "#0000ffaa";
        bool class_changed = false;
        HClassMlp class_mlp;
        HWindow display;
        HRegion seam_region = new HRegion();
        HRegion colored_region = new HRegion();
        HImage image;
        HClassLUT lut;
        ColorDetection stains;
        Cursor eraser;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public TrainColorMlpWindow(HClassMlp class_mlp, HClassLUT lut, HImage image, ColorDetection stains)
        {
            region_modified.GenEmptyRegion();
            StreamResourceInfo sriCurs = Application.GetResourceStream(
      new Uri("Cursors/eraser.cur", UriKind.Relative));
            eraser = new Cursor(sriCurs.Stream);
            if (stains != null)
                this.stains = stains;
            this.class_mlp = class_mlp;
            this.image = image;
            // this.image = image;
            this.lut = lut;
            InitializeComponent();

            seam_region.GenEmptyRegion();
            colored_region.GenEmptyRegion();
            lb_sample.Content = class_mlp.GetSampleNumClassMlp().ToString();
            grid_parameters.DataContext = stains;
            this.DataContext = this;
            sld_sensitive.IsEnabled = true;
        }

        #region properties
        int _brush_size = 10;
        string _color = "#000000ff";
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    //if (add_region == false)
                    display?.SetColor(AddOpacity(value, ColorOpacity/100));
                    _color = value;
                    RaisePropertyChanged("Color");
                }
            }
        }
        public int BrushSize
        {
            get
            {
                return _brush_size;
            }
            set
            {
                if (_brush_size != value)
                {
                    _brush_size = value;
                    RaisePropertyChanged("BrushSize");
                }
            }
        }
        //MaskingFilter mask;
        string _color_draw = "#00ff00aa";
        public string ColorDraw
        {
            get
            {
                return _color_draw;
            }
            set
            {
                if (_color_draw != value)
                {
                    _color_draw = value;
                    display?.SetColor(AddOpacity(value,ColorOpacity/100));
                    RaisePropertyChanged("ColorDraw");
                }
            }
        }
        State current_state = State.Pan;
        HDrawingObject draw_obj;
        public EventHandler OnDrawComplete;
        public HRegion CurrentRegion;
        #endregion
        private void btn_add_seam_Click(object sender, RoutedEventArgs e)
        {
            _is_paint = false;
            btn_gradient.IsChecked = true;
            display?.SetColor(AddOpacity(_color_draw, ColorOpacity/100));
            OnDrawComplete = DrawSeamComplete;
            current_color = "#0000ffaa";
            bd_verify.Visibility = Visibility.Visible;
            grid_control.Visibility = Visibility.Hidden;
            add_region = true;
            CurrentRegion = seam_region;
            return;
            try
            {
                if (draw_obj == null)
                {
                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    AdjustDrawingObjectSize(ref x, ref y, ref h, ref w);
                    draw_obj = new HDrawingObject(y + h / 4, x + w / 4, y + h * 3 / 4, x + w * 3 / 4);
                    //  draw_obj.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                    draw_obj.SetDrawingObjectParams("color", "blue");
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                else
                {
                    draw_obj.SetDrawingObjectParams("color", "blue");

                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    AdjustDrawingObjectSize(ref x, ref y, ref h, ref w);
                    draw_obj.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple(y + h / 4, x + w / 4, y + h * 3 / 4, x + w * 3 / 4));
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                display.SetColor(AddOpacity("#0000ffaa", ColorOpacity/100));
                display.DispRegion(seam_region);
                OnDrawComplete += DrawSeamComplete;
                bd_verify.Visibility = Visibility.Visible;
                grid_control.Visibility = Visibility.Hidden;
                // lb_message.Content = "Draw the region that contain the normal region";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "Please Close Training window and open again");
                display.DetachDrawingObjectFromWindow(draw_obj);
            }

        }
        private void btn_eraser_Checked(object sender, RoutedEventArgs e)
        {
            current_state = State.Eraser;
            window_display.Cursor = eraser;
        }

        private void rad_move_Checked(object sender, RoutedEventArgs e)
        {
            window_display.HMoveContent = false;
            current_state = State.Move;
            window_display.Cursor = Cursors.Cross;
        }
        private void rad_move_Unchecked(object sender, RoutedEventArgs e)
        {
            window_display.Cursor = Cursors.Arrow;
        }
        private void btn_solid_Checked(object sender, RoutedEventArgs e)
        {
            current_state = State.Pan;
            window_display.Cursor = Cursors.Hand;
            window_display.HMoveContent = true;
        }

        private void btn_gradient_Checked(object sender, RoutedEventArgs e)
        {
            current_state = State.GradientBrush;
            window_display.HMoveContent = false;
            window_display.Cursor = Cursors.Pen;
        }

        private void btn_solid_Unchecked(object sender, RoutedEventArgs e)
        {
            window_display.Cursor = Cursors.Arrow;
        }
        public void DrawSeamComplete(object sender, EventArgs e)
        {
            // HTuple param = draw_obj.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            seam_region = seam_region.Union2(region_modified);
            //seam_region.Union2(new HRegion((HTuple)param[0], param[1], param[2], param[3]));

            display.SetColor(AddOpacity("#0000ffaa", ColorOpacity/100));
            display.DispRegion(seam_region);
            region_modified.GenEmptyRegion();
            // display.DetachDrawingObjectFromWindow(draw_obj);
            OnDrawComplete = null;
        }
        public void DrawColoredComplete(object sender, EventArgs e)
        {
            // HTuple param = draw_obj.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            colored_region = colored_region.Union2(region_modified);
            //colored_region.Union2(new HRegion((HTuple)param[0], param[1], param[2], param[3]));
            display.SetColor(AddOpacity("#ff0000aa", ColorOpacity/100));
            display.DispRegion(colored_region);
            region_modified.GenEmptyRegion();
            // display.DetachDrawingObjectFromWindow(draw_obj);
            OnDrawComplete = null;
        }
        public void DrawColoredSampleComplete(object sender, EventArgs e)
        {
            //HTuple param = draw_obj.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            //HRegion color_sample = new HRegion((HTuple)param[0], param[1], param[2], param[3]);
            HImage image_base = image.ScaleImage((double)(1 - (double)(color_selected.Color.A) / 255), 0);
            HImage image_overlay = image.PaintRegion(region_modified, new HTuple((int)color_selected.Color.R, color_selected.Color.G, color_selected.Color.B), "fill");
            image_overlay = image_overlay.ScaleImage((double)color_selected.Color.A / 255, 0.0);
            image_overlay = image_base.AddImage(image_overlay, 1, 0.0).ReduceDomain(region_modified);

            image.OverpaintGray(image_overlay);
            //   image.OverpaintRegion(region_modified, new HTuple((int)color_selected.SelectedColor.Value.R, color_selected.SelectedColor.Value.G, color_selected.SelectedColor.Value.B), "fill");
            region_modified.GenEmptyRegion();
            add_region = false;
            //  display.DetachDrawingObjectFromWindow(draw_obj);
            display.AttachBackgroundToWindow(image);
            OnDrawComplete = null;
            class_new_train = true;
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetLineWidth(1);
            display.AttachBackgroundToWindow(image);
            //display.SetDraw("margin");
            display.SetDraw("fill");
            window_display.SetFullImagePart(null);
        }

        public void AdjustDrawingObjectSize(ref HTuple x, ref HTuple y, ref HTuple h, ref HTuple w)
        {
            if (image != null)
            {
                int width, height;
                image.GetImageSize(out width, out height);
                if (x < 0)
                {
                    x = 0;
                }
                if (y < 0)
                {
                    y = 0;
                }
                if (h > height)
                {
                    h = height;
                }
                if (w > width)
                {
                    w = width;
                }
                if (x > width)
                {
                    x = width / 2;
                }
                if (y > height)
                {
                    y = height / 2;
                }
            }
        }
        private void btn_add_color_Click(object sender, RoutedEventArgs e)
        {
            _is_paint = false;
            display?.SetColor(AddOpacity(_color_draw, ColorOpacity/100));
            btn_gradient.IsChecked = true;
            current_color = "#ff0000aa";
            OnDrawComplete = DrawColoredComplete;
            bd_verify.Visibility = Visibility.Visible;
            grid_control.Visibility = Visibility.Hidden;
            add_region = true;
            CurrentRegion = colored_region;
            return;
            try
            {
                if (draw_obj == null)
                {


                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    AdjustDrawingObjectSize(ref x, ref y, ref h, ref w);
                    draw_obj = new HDrawingObject(y + h / 4, x + w / 4, y + h * 3 / 4, x + w * 3 / 4);
                    // draw_obj.SetDrawingObjectParams((HTuple)"line_width",new HTuple(1));
                    draw_obj.SetDrawingObjectParams("color", "red");
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                else
                {
                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    AdjustDrawingObjectSize(ref x, ref y, ref h, ref w);
                    draw_obj.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple((y + h / 4), (x + w / 4), (y + h * 3 / 4), (x + w * 3 / 4)));
                    draw_obj.SetDrawingObjectParams("color", "red");
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                display.SetColor(AddOpacity("#ff0000aa",ColorOpacity/100));
                display.DispRegion(colored_region);
                OnDrawComplete += DrawColoredComplete;


                // lb_message.Content = "Draw the region that contain the defect region";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "Please Close Training window and open again");
                display.DetachDrawingObjectFromWindow(draw_obj);
                draw_obj = null;
            }
        }
        public bool _is_paint = false;
        public void DisplayRegionDraw()
        {
            display.ClearWindow();
            if (CurrentRegion != null)
            {
                display.SetColor(AddOpacity(current_color, ColorOpacity/100));
                display.DispRegion(CurrentRegion);
            }
            //display.SetColor("#ff0000aa");
            //display.DispRegion(colored_region);
            if (_is_paint)
                display.SetColor(AddOpacity(_color,ColorOpacity/100));
            else
                display.SetColor(AddOpacity(_color_draw, ColorOpacity/100));
            display.DispObj(region_modified);
        }
        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {

            OnDrawComplete?.Invoke(null, null);

            bd_verify.Visibility = Visibility.Hidden;
            grid_control.Visibility = Visibility.Visible;
            add_region = false;
            btn_solid.IsChecked = true;
            CurrentRegion = null;
            //  DisplayRegionDraw();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            //if (draw_obj!=null)
            //  display.DetachDrawingObjectFromWindow(draw_obj);

            bd_verify.Visibility = Visibility.Hidden;
            grid_control.Visibility = Visibility.Visible;
            add_region = false;
            btn_solid.IsChecked = true;
            region_modified.GenEmptyRegion();
            display.ClearWindow();
            if (CurrentRegion != null)
            {
                display.SetColor(AddOpacity(current_color, ColorOpacity/100));
                display.DispRegion(CurrentRegion);
            }
            CurrentRegion = null;
            // DisplayRegionDraw();
        }
        bool class_new_train = true;
        CancellationToken cancel_token;
        CancellationTokenSource source = new CancellationTokenSource();
        private void btn_train_Click(object sender, RoutedEventArgs e)
        {
            //this.IsEnabled = false;
            //loading.Content = "Training, Please wait!!";
            //loading.DeferedVisibility = true;
            training_grid.Visibility = Visibility.Visible;
            cancel_token = source.Token;
            Task.Run(new Action(() =>
            {

                try
                {
                    class_mlp.SetRejectionParamsClassMlp("sampling_strategy", "no_rejection_class");

                    //  class_mlp.SetRejectionParamsClassMlp("rejection_sample_factor", .3);
                    if (stains != null)
                    {
                        //MainWindow.WriteActionDatabase(stains.Name, "Color Data", "N/A", "N/A", "Soft Training new color");
                    }
                    class_mlp.AddSamplesImageClassMlp(image, seam_region.ConcatObj(colored_region));
                    seam_region.GenEmptyRegion();
                    colored_region.GenEmptyRegion();
                    HTuple error;
                    class_mlp.TrainClassMlp(200, 1, 0.01, out error);
                    class_changed = true;
                    class_new_train = true;
                }
                catch (Exception ex)
                {

                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>

                {

                    this.IsEnabled = true;
                    //loading.DeferedVisibility = false;
                    training_grid.Visibility=Visibility.Hidden;
                    lb_sample.Content = class_mlp.GetSampleNumClassMlp().ToString();
                }
                ));

            }
            ), cancel_token);
        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //window_display.HalconWindow.DispObj(image);
        }

        private void btn_zoom_in_Click(object sender, RoutedEventArgs e)
        {

            window_display.HZoomWindowContents((int)(window_display.ActualWidth / 2), (int)(window_display.ActualHeight / 2), 120);
        }

        private void btn_zoom_out_Click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, -120);
        }
        HRegion classes;
        private void btn_view_segmen_Click(object sender, RoutedEventArgs e)
        {
            if (class_new_train)
            {
                if (stains != null)
                    classes = class_mlp.ClassifyImageClassMlp(image, stains.RejectionThreshold);
                else
                    classes = class_mlp.ClassifyImageClassMlp(image, 0.5);
                class_new_train = false;
            }
            display.SetColor(AddOpacity("#0000ffaa", ColorOpacity/100));
            classes.SelectObj(1).DispObj(display);
            display.SetColor(AddOpacity("#ff0000aa", ColorOpacity/100));
            classes.SelectObj(2).DispObj(display);
        }

        private void btn_fit_Click(object sender, RoutedEventArgs e)
        {
            display.ClearWindow();
            //window_display.SetFullImagePart(null);
        }

        private void btn_clear_train_data_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = DXMessageBox.Show(this, "Warning !!!" + Environment.NewLine + "Are you sure want to CLEAR train data? " + Environment.NewLine + "(need to train again)", "Clear Sample Data", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                class_mlp.ClearSamplesClassMlp();
                class_mlp.CreateClassMlp(3, 10, 2, "softmax", "normalization", 10, 42);
                lb_sample.Content = class_mlp.GetSampleNumClassMlp().ToString();
                class_changed = true;
                class_new_train = true;
            }
        }

        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_view_train_data_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (class_changed)
            {
                MainWindow.ShowLoading("Creating LUT to speedup classification task!");
                Task.Run(new Action(() =>
                {
                    if (stains != null)
                        lut?.CreateClassLutMlp(class_mlp, new HTuple("rejection_threshold"), stains.RejectionThreshold);
                    else
                        lut?.CreateClassLutMlp(class_mlp, new HTuple(), new HTuple());
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => MainWindow.CloseLoading()));
                }
               ));

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

        private void btn_pen_Checked(object sender, RoutedEventArgs e)
        {

        }
        double _color_opacity = 50;
        public double ColorOpacity
        {
            get
            {
                return _color_opacity;
            }
            set
            {
                if (_color_opacity != value)
                {
                    _color_opacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        private void btn_pen_Click(object sender, RoutedEventArgs e)
        {
            _is_paint = true;
            btn_gradient.IsChecked = true;
            display.SetDraw("fill");
            display?.SetColor(AddOpacity(_color, ColorOpacity/100));
        
            add_region = true;
            CurrentRegion = null;
            bd_verify.Visibility = Visibility.Visible;
            grid_control.Visibility = Visibility.Hidden;
            OnDrawComplete = DrawColoredSampleComplete;
            return;
            try
            {
                if (draw_obj == null)
                {


                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    draw_obj = new HDrawingObject(y + h / 4, x + w / 4, y + h * 3 / 4, x + w * 3 / 4);
                    //  draw_obj.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                    draw_obj.SetDrawingObjectParams("color", "red");
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                else
                {
                    HTuple x, y, w, h;
                    y = window_display.HImagePart.Y;
                    h = window_display.HImagePart.Height;
                    x = window_display.HImagePart.X;
                    w = window_display.HImagePart.Width;
                    draw_obj.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple(y + h / 4, x + w / 4, y + h * 3 / 4, x + w * 3 / 4));
                    draw_obj.SetDrawingObjectParams("color", "red");
                    display.AttachDrawingObjectToWindow(draw_obj);
                }
                display.SetColor(AddOpacity("#00FF00aa", ColorOpacity/100));
                display.DispRegion(colored_region);
                OnDrawComplete += DrawColoredSampleComplete;
                bd_verify.Visibility = Visibility.Visible;
                grid_control.Visibility = Visibility.Hidden;
                //  lb_message.Content = "Draw the region that contain the defect region";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "Please Close Training window and open again");
                display.DetachDrawingObjectFromWindow(draw_obj);
            }
        }



        private void btn_load_train_data_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog diag = new System.Windows.Forms.OpenFileDialog();
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                HFile file = new HFile(diag.FileName, "input_binary");
                HSerializedItem item = new HSerializedItem();
                item.FreadSerializedItem(file);
                class_mlp.DeserializeClassMlp(item);
                file.Dispose();
                btn_train_Click(null, null);
            }
        }

        private void btn_hard_train_Click(object sender, RoutedEventArgs e)
        {

            //this.IsEnabled = false;
            //loading.Content = "Training, Please wait!!";
            //loading.DeferedVisibility = true;
            training_grid.Visibility = Visibility.Visible;
            cancel_token = source.Token;
            Task.Run(new Action(() =>
            {

                try
                {
                    class_mlp.SetRejectionParamsClassMlp("sampling_strategy", "hyperbox_ring_around_each_class");
                    class_mlp.SetRejectionParamsClassMlp("rejection_sample_factor", .3);
                    if (stains != null)
                    {
                        class_mlp.SetRejectionParamsClassMlp("hyperbox_tolerance", stains.Sensitive / 100);
                        //MainWindow.WriteActionDatabase(stains.Name, "Color Data", "N/A", "N/A", "Hard Training new color");
                    }

                    class_mlp.AddSamplesImageClassMlp(image, seam_region.ConcatObj(colored_region));
                    seam_region.GenEmptyRegion();
                    colored_region.GenEmptyRegion();
                    CurrentRegion = null;
                    HTuple error;
                    class_mlp.TrainClassMlp(200, 1, 0.01, out error);
                    class_changed = true;
                    class_new_train = true;

                }
                catch (Exception ex)
                {

                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>

                {
                    //loading.DeferedVisibility = false;
                    training_grid.Visibility = Visibility.Hidden;
                    this.IsEnabled = true;
                    lb_sample.Content = class_mlp.GetSampleNumClassMlp().ToString();
                }
                ));

            }
            ),cancel_token);

        }

        private void btn_save_train_data_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                HFile file = new HFile(diag.FileName, "output_binary");

                class_mlp.SerializeClassMlp().FwriteSerializedItem(file);
                file.Dispose();
            }
        }
        bool enter_state = false;
        Stack<HRegion> region_undo = new Stack<HRegion>(10);
        Stack<HRegion> region_redo = new Stack<HRegion>(10);
        public HRegion region_modified = new HRegion();
        double row_start, col_start;
        double row, col;
        bool add_region = false;
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!add_region)
                return;
            enter_state = true;
            HRegion region_gen = new HRegion();
            switch (current_state)
            {
                case State.Pan: break;
                case State.SolidBrush: break;
                case State.GradientBrush:

                    enter_state = false;
                    region_undo.Push(region_modified);
                    region_gen.GenCircle(e.Row, e.Column, BrushSize);
                    region_modified = region_modified.Union2(region_gen);
                    DisplayRegionDraw();
                    display.DispObj(region_gen);

                    break;
                case State.Eraser:

                    enter_state = true;
                    region_undo.Push(region_modified);
                    region_gen.GenCircle(e.Row, e.Column, BrushSize);
                    region_modified = region_modified.Difference(region_gen);
                    DisplayRegionDraw();


                    display.DispObj(region_gen);
                    break;
                default:
                    region_undo.Push(region_modified);
                    break;
            }



            row_start = e.Row;
            col_start = e.Column;
        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            region_modified.GenEmptyRegion();
            display.ClearWindow();

        }

        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {
            enter_state = false;
            if (add_region)
            {

                DisplayRegionDraw();
            }
        }



        private void sld_closing_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {

        }

        private void NumericUpDownWithKeyboard_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.OldValue != null)
            {
                class_new_train = true;
                class_changed = true;
            }

        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.ShowInTaskbar = false;

                this.Owner = Application.Current.MainWindow;
            }
            catch (Exception ex)
            {

            }
        }

        private void Btn_parameter_Click(object sender, RoutedEventArgs e)
        {
            expander.IsExpanded = !expander.IsExpanded;
        }

        private void sld_closing_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (class_new_train)
            {
                classes = class_mlp.ClassifyImageClassMlp(image, 0.5);
                class_new_train = false;
            }
            if (display == null)
                return;

            display.ClearWindow();
            display.SetColor(AddOpacity("#0000ffaa", ColorOpacity/100));
            classes.SelectObj(1).DispObj(display);
            display.SetColor(AddOpacity("#ff0000aa", ColorOpacity/100));
            classes.SelectObj(2).ClosingCircle(sld_closing.Value).DispObj(display);
        }

        private void btn_cancel_train_Click(object sender, RoutedEventArgs e)
        {
            if (source != null)
            {
                source.Cancel();
            }
            training_grid.Visibility = Visibility.Hidden;
            this.IsEnabled = true;
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!add_region)
                return;
            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
                switch (current_state)
                {
                    case State.Move:

                        region_modified = region_modified.MoveRegion((int)(e.Row - row_start), (int)(e.Column - col_start));
                        row_start = e.Row;
                        col_start = e.Column;
                        display.ClearWindow();
                        display.DispObj(region_modified);

                        break;
                    case State.Pan: break;
                    case State.SolidBrush: break;
                    case State.GradientBrush:
                        if (enter_state)
                        {
                            enter_state = false;
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row_start, e.Row), new HTuple(col_start, e.Column));
                            region_modified = region_modified.Union2(region_gen.DilationCircle((double)BrushSize));
                            DisplayRegionDraw();

                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            region_modified = region_modified.Union2(region_gen.DilationCircle((double)BrushSize));
                            DisplayRegionDraw();


                        }

                        HRegion region_disp = new HRegion();
                        region_disp.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_disp);

                        break;
                    case State.Eraser:
                        if (enter_state)
                        {
                            enter_state = false;
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row_start, e.Row), new HTuple(col_start, e.Column));
                            region_modified = region_modified.Difference(region_gen.DilationCircle((double)BrushSize));
                            DisplayRegionDraw();
                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            region_modified = region_modified.Difference(region_gen.DilationCircle((double)BrushSize));
                            DisplayRegionDraw();
                        }

                        HRegion region_erase = new HRegion();
                        region_erase.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_erase);
                        break;
                }
            else
            {
                switch (current_state)
                {
                    case State.Move: break;
                    case State.Pan:
                        break;
                    default:



                        DisplayRegionDraw();
                        HRegion region_disp = new HRegion();
                        region_disp.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_disp);
                        break;

                }
            }

            row = e.Row;
            col = e.Column;
        }

        private void window_display_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

            enter_state = false;
        }
    }
}
