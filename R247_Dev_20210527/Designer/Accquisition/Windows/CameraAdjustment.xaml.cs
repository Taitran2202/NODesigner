using DevExpress.Xpf.Core;
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
using HalconDotNet;

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for CameraAdjustment.xaml
    /// </summary>
    public partial class CameraAdjustment : ThemedWindow
    {
        EBUS model;
        int old_height = 1000;
        public CameraAdjustment(EBUS model)
        {
            InitializeComponent();
            this.model = model;
            old = model.ImageAcquired;
            old_height = model.ImageHeight;
            this.DataContext = model;
        }
        long linetrigger;
        long frametrigger;
        OnImageAcquired old;
        public void StartLive()
        {

            if (model.IsRun)
            {
                model.OnCameraDisconnected?.Invoke();
            }
            //connect camera
            if (model.device == null)
            {
                bool result = model.Connect();
                if (!result)
                    return;
            }
            model.ImageHeight = 200;
            model.device.Parameters.SetEnumValue("TriggerSelector", "LineStart");
            linetrigger = model.device.Parameters.GetEnumValueAsInt("TriggerMode");
            model.device.Parameters.SetEnumValue("TriggerMode", 0);
            model.device.Parameters.SetEnumValue("TriggerSelector", "FrameStart");
            frametrigger = model.device.Parameters.GetEnumValueAsInt("TriggerMode");
            model.device.Parameters.SetEnumValue("TriggerMode", 0);




            model.Start();

            //  model.ImageAcquired = old ;
            //start camera
        }
        public void ImageCaptured(HImage image, ulong frame_count ,double fps=0)
        {
            try

            {
                HTuple deviation, intensity;
                intensity = image.Intensity(image, out deviation);
                int percent = (int)((deviation.D - 80) * 100 / 40);
                window_focus.HalconWindow.DispObj(image);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    gauge.Value = percent;
                }));
            }
            catch (Exception ex)
            {

            }
        }

        private void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                if (model.IsRun)
                    return;
                model.ImageAcquired = ImageCaptured;
                StartLive();
            }));
        }

        private void Btn_stop_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                model.Stop();

                //model.device.Parameters.SetEnumValue("TriggerSelector", "LineStart");
                //model.device.Parameters.SetEnumValue("TriggerMode", linetrigger);
                //model.device.Parameters.SetEnumValue("TriggerSelector", "FrameStart");
                //model.device.Parameters.SetEnumValue("TriggerMode", frametrigger);

            }));
        }

        private void ThemedWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (model.IsRun)
            {
                model.Stop();
            }
            model.ImageAcquired = old;
            model.ImageHeight = old_height;
        }

        private void HSmartWindowControlWPF_HInitWindow(object sender, EventArgs e)
        {

        }

        private void Btn_roll_start_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                if (model.IsRun)
                    return;


                model.ImageAcquired = ImageCapturedRoll;
                StartLive();
            }));
        }
        public void ImageCapturedRoll(HImage image, ulong frame_count, double fps = 0)
        {
            try
            {
                HTuple w, h;
                image.GetImageSize(out w, out h);
                HMeasure roll_measurement = new HMeasure(h / 2, w / 2, 0, w * 0.9 / 2, h * 0.9 / 2, w, h, "nearest_neighbor");
                HTuple row, col, amp, dis;
                HTuple value = 0;
                roll_measurement.MeasurePos(image, 1, 90, "all", "all", out row, out col, out amp, out dis);

                if (row.Type != HTupleType.EMPTY)
                {

                    // check highest value.
                    HTuple max = dis.TupleMax();
                    HTuple index_max = dis.TupleFind(max);
                    HTuple num_right = dis.Length - index_max;
                    HTuple num_even_element = index_max;
                    if (num_right < index_max)
                    {
                        num_even_element = num_right;
                    }
                    HTuple total_left = 0, total_right = 0;
                    if (num_even_element < 5)
                    {
                        value = 0;
                    }
                    if (num_even_element > 8)
                        num_even_element = 8;

                    if (num_even_element > 5)
                    {
                        for (int i = 0; i < num_even_element - 1; i++)
                        {
                            total_left = total_left + dis[index_max - i - 1] * (1 + i * 0.1);
                            total_right = total_right + dis[index_max + i + 1] * (1 + i * 0.1);
                        }
                        value = (total_left * 50 / total_right);
                    }
                }


                window_aligment.HalconWindow.DispObj(image);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    gauge_1.Value = value.D - 50;
                }));
            }
            catch (Exception ex)
            {

            }
        }
        private void Btn_roll_stop_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                model.Stop();
            }));
        }
    }
}
