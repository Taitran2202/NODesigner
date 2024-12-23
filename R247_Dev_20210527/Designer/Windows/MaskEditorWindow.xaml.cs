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
using System.Windows.Resources;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for MaskEditorWindow.xaml
    /// </summary>
    public partial class MaskEditorWindow : Window, INotifyPropertyChanged
    {
        Stack<HRegion> region_undo = new Stack<HRegion>(10);
        Stack<HRegion> region_redo = new Stack<HRegion>(10);
        Cursor eraser;
        bool _is_margin = true;
        public bool IsMargin
        {
            get
            {
                return _is_margin;
            }
            set
            {
                if (_is_margin != value)
                {
                    _is_margin = value;
                    if (value)
                    {
                        display?.SetDraw("fill");
                    }
                    else
                    {

                        display?.SetDraw("margin");
                    }

                    RaisePropertyChanged("IsMargin");
                }
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public HRegion region_modified = new HRegion();
        public HRegion region_base = new HRegion();
        HWindow display;
        HImage image_mask;
        HImage image;
        HHomMat2D transform;
        State current_state = State.Pan;
        HTuple w, h;
        double _scale = 1;
        public double Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    RaisePropertyChanged("Scale");
                }
            }
        }
        double _smooth = 1;
        public double Smooth
        {
            get
            {
                return _smooth;
            }
            set
            {
                if (_smooth != value)
                {
                    _smooth = value;
                    RaisePropertyChanged("Smooth");
                }
            }
        }

        int _brush_size = 10;
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
        BrushRegion mask;
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
                    display?.SetColor(value);
                    RaisePropertyChanged("ColorDraw");
                }
            }
        }
        public MaskEditorWindow(HImage image, HHomMat2D transform, BrushRegion mask)
        {
            StreamResourceInfo sriCurs = Application.GetResourceStream(
       new Uri("Cursors/eraser.cur", UriKind.Relative));
            eraser = new Cursor(sriCurs.Stream);
            this.mask = mask;
            // this.image_mask = image_mask;
            this.image = image;
            region_modified.GenEmptyObj();
            this.transform = transform;
            this.region_base = mask.region;
            if (transform == null)
            {
                region_modified = region_base;
            }
            else
            {
                region_modified = region_base.AffineTransRegion(transform, "constant");
            }
            image.GetImageSize(out w, out h);
            InitializeComponent();
            this.DataContext = this;
            radioButton.DataContext = mask;

        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                display = window_display.HalconWindow;
                display.SetDraw("fill");
                display.SetColor(ColorDraw);
                display.AttachBackgroundToWindow(image);
                DispOverlay();
                // display.DispObj(image_mask);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
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
            //    MessageBox.Show("Error at Window_Loaded: " + ex.Message);
            //    this.Close();
            //}
            //window_display.HalconWindow.DispImage(image);  HOperatorSet.SetSystem("cancel_draw_result", "true");

        }
        public void Update(Region sender)
        {


            DispOverlay();
        }
        public void DispOverlay()
        {
            if (display == null)
                return;
            display.ClearWindow();
            // display.SetDraw("fill");
            //display.SetColor(regions.Color);
            display.DispObj(region_modified);
        }

        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            //RefreshGraphic();
        }
        private void color_background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            DispOverlay();
        }
        double row, col;
        double row_start, col_start;
        bool enter_state = false;
        State last_state = State.Pan;
        Cursor last_cursor = null;
        bool last_move_state = false;
        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
            {
                //window_display.HMouseUp += window_display_HMouseUp;
                //window_display.HMouseUp -= Window_display_HMouseUp_pan;
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
                        display.ClearWindow();
                        display.DispObj(region_modified);
                        display.DispObj(region_gen);

                        break;
                    case State.Eraser:


                        region_undo.Push(region_modified);
                        region_gen.GenCircle(e.Row, e.Column, BrushSize);
                        region_modified = region_modified.Difference(region_gen);
                        display.ClearWindow();
                        display.DispObj(region_modified);


                        display.DispObj(region_gen);
                        break;
                    default:
                        region_undo.Push(region_modified);
                        break;
                }
            }
            //else
            //{
            //    last_cursor = window_display.Cursor;
            //    last_move_state = window_display.HMoveContent;
            //    window_display.Cursor = Cursors.Hand;
            //    window_display.HMoveContent = true;
            //    last_state = current_state;
            //    current_state = State.Pan;
                
            //    window_display.HMouseUp -= window_display_HMouseUp;
            //    window_display.HMouseUp += Window_display_HMouseUp_pan;
            //}


            row_start = e.Row;
            col_start = e.Column;
        }

        private void Window_display_HMouseUp_pan(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            current_state = last_state;
            if (last_cursor!=null)
            {
                window_display.Cursor = last_cursor;
            }
            window_display.HMoveContent = last_move_state;
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

        private void btn_gen_mask_Click(object sender, RoutedEventArgs e)
        {
           HImage chan1= region_modified.DistanceTransform("city-block", "true", w, h).ConvertImageType("byte");
            image_mask = chan1.Compose3(chan1, chan1).SmoothImage("deriche1", Smooth).ScaleImage(Scale, 0);
            HImage result = image.AddImage(image_mask, 1.0, 0);
            display.DispObj(result);
        }

        private void window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (transform!=null)
                {
                    mask.region = region_modified.AffineTransRegion(transform.HomMat2dInvert(), "constant");
                }
                else
                {
                    mask.region = region_modified.Clone();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {
            if (region_undo.Count > 0)
            {
                region_redo.Push(region_modified.Clone());
                region_modified = region_undo.Pop();
                DispOverlay();
            }
        }

        private void btn_redo_Click(object sender, RoutedEventArgs e)
        {
            if (region_redo.Count > 0)
            {
                region_undo.Push(region_modified);
                region_modified = region_redo.Pop();
                DispOverlay();
            }

        }

        private void window_display_MouseLeave(object sender, MouseEventArgs e)
        {
            DispOverlay();
        }

        private void btn_load_region_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog op = new System.Windows.Forms.OpenFileDialog();
            if (op.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    HRegion region = new HRegion();
                    region.ReadRegion(op.FileName);
                    //regions.Load(new DeserializeFactory(new HFile(op.FileName, "input_binary"), new HSerializedItem(), op.FileName));
                    region_undo.Push(region_modified.Clone());
                    region_modified = region_modified.Union2(region);
                    DispOverlay();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wrong type of file!!");
                }
            }
        }

        private void window_display_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {

           

            enter_state = false;
            
        }

        private void btn_eraser_Checked(object sender, RoutedEventArgs e)
        {
            current_state = State.Eraser;
            window_display.Cursor = eraser;
            window_display.HMoveContent = false;
        }
        
        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
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
                            display.ClearWindow();
                            display.DispObj(region_modified);

                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            region_modified = region_modified.Union2(region_gen.DilationCircle((double)BrushSize));
                            display.ClearWindow();
                            display.DispObj(region_modified);

                            
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
                            display.ClearWindow();
                            display.DispObj(region_modified);
                        }
                        else
                        {
                            HRegion region_gen = new HRegion();
                            region_gen.GenRegionPolygon(new HTuple(row, e.Row), new HTuple(col, e.Column));
                            region_modified = region_modified.Difference(region_gen.DilationCircle((double)BrushSize));
                            display.ClearWindow();
                            display.DispObj(region_modified);
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
                    case State.Move:break;
                    case State.Pan:
                        break;
                    default:



                        display.ClearWindow();
                        display.DispObj(region_modified);
                        HRegion region_disp = new HRegion();
                        region_disp.GenCircle(e.Row, e.Column, BrushSize);
                        display.DispObj(region_disp);
                        break;
                   
                }
            }
            
            row = e.Row;
            col = e.Column;
        }
    }
}
