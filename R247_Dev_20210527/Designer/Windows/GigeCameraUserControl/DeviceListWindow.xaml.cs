using DevExpress.Xpf.Core;
using HalconDotNet;
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

namespace NOVisionDesigner.Designer.Windows.GigeCameraUserControl
{
    /// <summary>
    /// Interaction logic for DeviceListWindow.xaml
    /// </summary>
    public partial class DeviceListWindow : ThemedWindow
    {
        public DeviceListWindow(string interface_type = "GigEVision2")
        {
            InitializeComponent();
            this.interface_type = interface_type;
        }
        #region Method
        string interface_type="";
        object discover_lock = new object();
        private void RefreshCamera()
        {

            loading.DeferedVisibility = true;
            Task.Run(new Action(() =>
            {
                lock (discover_lock)
                {

                    HTuple device, device_values;
                    device_values = new HTuple();
                    try
                    {
                        HOperatorSet.InfoFramegrabber(interface_type, "device", out device, out device_values);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Acquisition interface hasn't supported");
                        //this.Close();
                    }


                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {


                        if (device_values.Type != HTupleType.EMPTY)
                        {

                            lst_device.Items.Clear();
                            foreach (string camera_name in device_values.SArr)
                            {
                                lst_device.Items.Add(camera_name);

                            }
                        }



                        loading.DeferedVisibility = false;
                    }
                    ));
                }

            }));




        }

        bool result = false;
        string selected_device = "";
        public string ShowDevice()
        {
            this.ShowDialog();
            if (result)
                return selected_device;
            else
                return "";
        }
        #endregion
        #region View
        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            lock (discover_lock)
            {
                if (lst_device.SelectedIndex != -1)
                    selected_device = lst_device.SelectedItem.ToString();
                result = true;
            }
            this.Close();
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCamera();
        }

        private void ThemedWindow_ContentRendered(object sender, EventArgs e)
        {
            RefreshCamera();
        }
        #endregion
    }
}
