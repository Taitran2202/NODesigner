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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for TrainCheckDifferenceModelWindow.xaml
    /// </summary>
    public partial class TrainCheckDifferenceModelWindow : Window
    {
        public TrainCheckDifferenceModelWindow()
        {
            InitializeComponent();
        }
        HWindow display;
        CheckDifferenceModel model;
        HDrawingObject draw_obj_search;
        HDrawingObject draw_obj_model;
        EventHandler on_draw_complete;
        HDrawingObject draw_obj_check;
        HImage GrayImage;
        public TrainCheckDifferenceModelWindow(CheckDifferenceModel model, HImage image)
        {
            DataContext = this;

            InitializeComponent();

            this.model = model;
            this.GrayImage = image;


            // UI Update
            confirm_box.Visibility = Visibility.Hidden;

            image.GetImageSize(out int w, out int h);
            window_display.HImagePart = new Rect(0, 0, w, h);
        }
        #region BindingBase
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion //BindingBase
        private void display_window_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetLineWidth(1);
            display.AttachBackgroundToWindow(GrayImage);
        }

        public void Draw_search_finish(object sender, EventArgs e)
        {
            display.DetachDrawingObjectFromWindow(draw_obj_search);
            if (!(bool)sender)
                return;
            HRegion region = new HRegion();
            HTuple param = draw_obj_search.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            region.GenRectangle1((double)param[0], param[1], param[2], param[3]);
            model.SearchRegion = region;
            display.SetColor("#00ff0055");
            display.DispRegion(region);
            on_draw_complete = null;

            btn_draw_search.IsEnabled = true;
            btn_draw_model.IsEnabled = true;
            btn_draw_check.IsEnabled = true;

        }
        public void Draw_model_finish(object sender, EventArgs e)
        {
            display.DetachDrawingObjectFromWindow(draw_obj_model);
            if (!(bool)sender)
                return;
            HRegion region = new HRegion();
            HTuple param = draw_obj_model.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            region.GenRectangle2((double)param[0], param[1], param[2], param[3], param[4]);
            HTuple row_base, col_base;
            region.AreaCenter(out row_base, out col_base);

            model.ModelRegion = region;

            display.SetColor("#0000ffaa");
            display.DispRegion(region);
            on_draw_complete = null;



            btn_draw_search.IsEnabled = true;
            btn_draw_model.IsEnabled = true;
            btn_draw_check.IsEnabled = true;
        }
        public void Draw_check_finish(object sender, EventArgs e)
        {
            display.DetachDrawingObjectFromWindow(draw_obj_check);
            if (!(bool)sender)
                return;
            HRegion region = new HRegion();
            HTuple param = draw_obj_check.GetDrawingObjectParams(new HTuple("row", "column", "phi", "length1", "length2"));
            region.GenRectangle2((double)param[0], param[1], param[2], param[3], param[4]);
            HTuple row_base, col_base;
            region.AreaCenter(out row_base, out col_base);

            model.CheckingRegion = region;
            display.SetColor("#0000ffaa");
            display.DispRegion(region);
            on_draw_complete = null;


            btn_draw_search.IsEnabled = true;
            btn_draw_model.IsEnabled = true;
            btn_draw_check.IsEnabled = true;
        }
        private void btn_draw_check_Click(object sender, RoutedEventArgs e)
        {
            btn_draw_search.IsEnabled = false;
            btn_draw_model.IsEnabled = false;
            btn_draw_check.IsEnabled = false;
            confirm_box.Visibility = Visibility.Visible;
            if (draw_obj_check == null)
            {
                if (model.CheckingRegion != null)
                {
                    HTuple row, col, length1, length2, phi;
                    model.CheckingRegion.SmallestRectangle2(out row, out col, out phi, out length1, out length2);
                    draw_obj_check = new HDrawingObject(row, col, phi, length1, length2);
                }
                else
                {
                    HTuple r1, c1, r2, c2;
                    r1 = window_display.HImagePart.Y;
                    r2 = window_display.HImagePart.Y + window_display.HImagePart.Height;
                    c1 = window_display.HImagePart.X;
                    c2 = window_display.HImagePart.X + window_display.HImagePart.Width;
                    draw_obj_check = new HDrawingObject((r1 + r2) / 2, (c1 + c2) / 2, 0, 200, 200);
                }
                draw_obj_check.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                draw_obj_check.SetDrawingObjectParams("color", "blue");
                display.AttachDrawingObjectToWindow(draw_obj_check);

            }
            else
            {
                display.AttachDrawingObjectToWindow(draw_obj_check);

            }
            on_draw_complete += Draw_check_finish;
        }

        private void btn_draw_model_Click(object sender, RoutedEventArgs e)
        {
            btn_draw_search.IsEnabled = false;
            btn_draw_model.IsEnabled = false;
            btn_draw_check.IsEnabled = false;
            confirm_box.Visibility = Visibility.Visible;
            if (draw_obj_model == null)
            {
                if (model.ModelRegion != null)
                {
                    HTuple row, col, length1, length2, phi;
                    model.ModelRegion.SmallestRectangle2(out row, out col, out phi, out length1, out length2);
                    draw_obj_model = new HDrawingObject(row, col, phi, length1, length2);
                }
                else
                {
                    HTuple r1, c1, r2, c2;
                    r1 = window_display.HImagePart.Y;
                    r2 = window_display.HImagePart.Y + window_display.HImagePart.Height;
                    c1 = window_display.HImagePart.X;
                    c2 = window_display.HImagePart.X + window_display.HImagePart.Width;
                    draw_obj_model = new HDrawingObject((r1 + r2) / 2, (c1 + c2) / 2, 0, 200, 200);
                }
                draw_obj_model.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                draw_obj_model.SetDrawingObjectParams("color", "blue");
                display.AttachDrawingObjectToWindow(draw_obj_model);

            }
            else
            {
                display.AttachDrawingObjectToWindow(draw_obj_model);

            }
            on_draw_complete += Draw_model_finish;
            //grid_verify.Visibility = Visibility.Visible;
            //grid_control.Visibility = Visibility.Hidden;
            //lb_message.Content = "Draw the PATTERN area";

        }

        private void btn_draw_search_Click(object sender, RoutedEventArgs e)
        {

            btn_draw_search.IsEnabled = false;
            btn_draw_model.IsEnabled = false;
            btn_draw_check.IsEnabled = false;
            confirm_box.Visibility = Visibility.Visible;
            try
            {

                if (draw_obj_search == null)
                {


                    if (model.SearchRegion.Area != 0)
                    {
                        HTuple row1, col1, row2, col2;
                        model.SearchRegion.SmallestRectangle1(out row1, out col1, out row2, out col2);
                        draw_obj_search = new HDrawingObject(row1, col1, row2, col2);
                    }
                    else
                    {
                        HTuple r1, c1, r2, c2;
                        r1 = window_display.HImagePart.Y;
                        r2 = window_display.HImagePart.Y + window_display.HImagePart.Height;
                        c1 = window_display.HImagePart.X;
                        c2 = window_display.HImagePart.X + window_display.HImagePart.Width;
                        //draw_obj_search = new HDrawingObject((r1 + r2) / 2 - 100, (r1 + r2) / 2 + 100, (c1 + c2) / 2 - 100, (c1 + c2) / 2 + 100);
                        draw_obj_search = new HDrawingObject(200, 200, 1000, 1000);
                    }
                    draw_obj_search.SetDrawingObjectParams((HTuple)"line_width", new HTuple(1));
                    draw_obj_search.SetDrawingObjectParams("color", "green");
                    display.AttachDrawingObjectToWindow(draw_obj_search);


                }
                else
                {
                    display.AttachDrawingObjectToWindow(draw_obj_search);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            on_draw_complete += Draw_search_finish;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            on_draw_complete?.Invoke(true, null);
            confirm_box.Visibility = Visibility.Hidden;
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            on_draw_complete?.Invoke(false, null);
            confirm_box.Visibility = Visibility.Hidden;
            btn_draw_search.IsEnabled = true;
            btn_draw_model.IsEnabled = true;
            btn_draw_check.IsEnabled = true;
        }

        private void btn_select_sample_path_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();

            // OK button was pressed.
            System.Windows.Forms.DialogResult result = folderBrowserDialog1.ShowDialog();

        }

        private void btn_start_train_Click(object sender, RoutedEventArgs e)

        {
            model.TrainModel(GrayImage, (model.ModelRegion));



        }


        private void btn_clear_window_Click(object sender, RoutedEventArgs e)
        {
            display.ClearWindow();

        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
