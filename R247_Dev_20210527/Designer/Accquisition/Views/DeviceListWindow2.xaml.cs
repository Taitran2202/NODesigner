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
using AVT.VmbAPINET;
using Basler.Pylon;
using MvCamCtrl.NET;//HIK

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for DeviceListWindow2.xaml
    /// </summary>
    public partial class DeviceListWindow2 : ThemedWindow
    {
        public DeviceListWindow2()
        {
            InitializeComponent();
            RefreshCamera();
        }
        Vimba sys;
        string type;
        public DeviceListWindow2(Vimba sys, string type)
        {
            InitializeComponent();
            this.sys = sys;
            this.type = type;
            this.interfaceType = "GigEVisionVimba";
        }
        private string interfaceType;
        public DeviceListWindow2(string interface_type)
        {
            InitializeComponent();
            interfaceType = interface_type;
        }
        object discover_lock = new object();
        private void RefreshCamera2()
        {

            loading.DeferedVisibility = true;
            
            
            Task.Run(new Action(() =>
            {
                lock (discover_lock)
                {

                    HTuple device, device_values;
                    device_values = new HTuple();
                    if(interfaceType == "GigEVisionVimba")
                    {
                        Vimba sys = new Vimba();
                        try
                        {
                            sys.Startup();
                            foreach (AVT.VmbAPINET.Camera c in sys.Cameras)
                            {
                                device_values.Append(c.Id);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        finally
                        {
                            sys.Shutdown();
                        }
                    }
                    else if(interfaceType == "GigEVisionBasler")
                    {
                        List<ICameraInfo> cameras = CameraFinder.Enumerate();
                        foreach(ICameraInfo cam in cameras)
                        {
                            device_values.Append(cam[CameraInfoKey.FullName]);
                        }
                    }
                    else if (interfaceType == "GigEVisionHIK")
                    {
                        List<CCameraInfo> m_ltDeviceList = new List<CCameraInfo>();
                        m_ltDeviceList.Clear();
                        int nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref m_ltDeviceList);
                        for (int i = 0; i < m_ltDeviceList.Count; i++)
                        {
                            CGigECameraInfo gigeInfo = (CGigECameraInfo)m_ltDeviceList[i];
                            device_values.Append(gigeInfo.chModelName + "(" + gigeInfo.chSerialNumber + ")");
                        }
                    }
                    else if(interfaceType == "DirectShow")
                    {
                        try
                        {
                            HOperatorSet.InfoFramegrabber("DirectShow", "device", out device, out device_values);
                        }
                        catch (Exception ex)
                        {
                            DXMessageBox.Show("Acquisition interface hasn't supported");
                            //this.Close();
                        }
                    }    
                    else
                    {
                        try
                        {
                            HOperatorSet.InfoFramegrabber("USB3Vision", "device", out device, out device_values);
                        }
                        catch (Exception ex)
                        {
                            DXMessageBox.Show("Acquisition interface hasn't supported");
                            //this.Close();
                        }
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
        private void RefreshCamera()
        {
            if(sys==null){
              RefreshCamera1();
            }else{
              RefreshCamera2();
            }
        }
        private void RefreshCamera1()
        {

            loading.DeferedVisibility = true;
            
            
            Task.Run(new Action(() =>
            {
                lock (discover_lock)
                {

                    HTuple device, device_values;
                    device_values = new HTuple();
                    if(interfaceType == "GigEVisionBasler")
                    {
                        List<ICameraInfo> cameras = CameraFinder.Enumerate();
                        foreach(ICameraInfo cam in cameras)
                        {
                            device_values.Append(cam[CameraInfoKey.FullName]);
                        }
                    }
                    else if (interfaceType == "GigEVisionHIK")
                    {
                        List<CCameraInfo> m_ltDeviceList = new List<CCameraInfo>();
                        m_ltDeviceList.Clear();
                        int nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref m_ltDeviceList);
                        for (int i = 0; i < m_ltDeviceList.Count; i++)
                        {
                            CGigECameraInfo gigeInfo = (CGigECameraInfo)m_ltDeviceList[i];
                            device_values.Append(gigeInfo.chModelName + "(" + gigeInfo.chSerialNumber + ")");
                        }
                    }
                    else if(interfaceType == "DirectShow")
                    {
                        try
                        {
                            HOperatorSet.InfoFramegrabber("DirectShow", "device", out device, out device_values);
                        }
                        catch (Exception ex)
                        {
                            DXMessageBox.Show("Acquisition interface hasn't supported");
                            //this.Close();
                        }
                    }    
                    else
                    {
                        try
                        {
                            HOperatorSet.InfoFramegrabber("USB3Vision", "device", out device, out device_values);
                        }
                        catch (Exception ex)
                        {
                            DXMessageBox.Show("Acquisition interface hasn't supported");
                            //this.Close();
                        }
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

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            RefreshCamera();
        }
    }
}
