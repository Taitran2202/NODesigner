using HalconDotNet;
using NOVisionDesigner.Designer.Keyboards;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using System;
using System.Collections.Generic;
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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for RecordViewWindow.xaml
    /// </summary>
    public partial class RecordViewWindow : Window
    {
        public HWindow display;
        RecorderView recorder_view;
        InspectionResult defect;
        
        public RecordViewWindow(RecorderView recorderView)
        {
            InitializeComponent();
            this.recorder_view = recorderView;
        }
        public void ShowResult(InspectionResult defect)
        {
            try
            {
                this.defect = defect;
                display.AttachBackgroundToWindow(defect.image);
                display.ClearWindow();
                //defect.image. (display);


                foreach (IDisplayable disp in defect.lst_display)
                {
                    disp?.Display(display);
                }
                
                display.FlushBuffer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HImage image = new HImage();
            image.GenEmptyObj();
            window_display.HalconWindow.DispImage(image);

        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("margin");
        }

        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            foreach (RegionInfo region in defect.regions)
            {
                HRegion selected = region.region.SelectRegionPoint((int)e.Row, (int)e.Column);
                if (selected.CountObj() > 0)
                {
                    display.SetWindowParam("flush", "false");
                    display.ClearWindow();
                    for (int index = 0; index < defect.regions.Count; index++)
                    {
                        display.SetColor(defect.ColorCodes[index]);
                        display.DispRegion(defect.regions[index].region);
                    }
                    lb_area.Content = Math.Round(((selected.Area.D) / (defect.scale_x * defect.scale_y)), 2).ToString();
                    display.SetDraw("fill");
                    selected.DispObj(display);
                    display.SetDraw("margin");
                    display.FlushBuffer();
                    display.SetWindowParam("flush", "true");
                }

            }
        }

        private void btn_save_image_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            //diag.InitialDirectory = MainWindow.path_record_ram;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    string record_path = diag.FileName;


                    defect.image.WriteImage("tiff", 0, record_path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void Btn_save_all_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            VirtualNumericKeyboard wd_input = new VirtualNumericKeyboard(20);
            int image_number = 20;
            if (wd_input.ShowDialog() == true)
            {
                image_number = (int)wd_input.result;
                try
                {

                    System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                    if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        string record_path = diag.SelectedPath;
                        recorder_view.Recorder.IsSaving = true;
                        int max = recorder_view.Recorder.ResultRecoderQueue.Count;
                        if (max >= image_number)
                        {
                            max = image_number;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            try
                            {
                                recorder_view.Recorder.ResultRecoderQueue.ElementAt(max - i - 1).image.WriteImage("tiff", 0, record_path + "\\" + i.ToString());
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        recorder_view.Recorder.IsSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_previous_Click(object sender, RoutedEventArgs e)
        {
            recorder_view.SelectRadPrevious();
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            recorder_view.SelectRadNext();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            recorder_view.Close();
        }

        private void btn_set_current_image_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    if (acq == null)
            //        return;
            //    acq.CurrentImage = defect.image;
            //    acq.DisplaygImage();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}
          
        }
    }
}
