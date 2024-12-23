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
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Misc;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for DeviceListWindow2.xaml
    /// </summary>
    public partial class MoxaDeviceList : Window
    {
        public MoxaDeviceList(NetworkInterfaceInfo selected_interface)
        {
            InitializeComponent();
            this.DataContext = this;
            var interfaces_list = new List<NetworkInterfaceInfo>();
            try
            {
                ret = MXIO_CS.MXIO_ListIF(wIFCount);
                Nodes.MoxaInput.CheckErr(ret, "MXIO_ListIF");
                if (ret == MXIO_CS.MXIO_OK)
                {
                    szIFInfo = new byte[282 * wIFCount[0]];
                    ret = MXIO_CS.MXIO_GetIFInfo(wIFCount[0], szIFInfo);
                    Nodes.MoxaInput.CheckErr(ret, "MXIO_GetIFInfo");
                    if (ret == MXIO_CS.MXIO_OK)
                    {
                        byte[] index = new byte[4];
                        byte[] ip = new byte[16];
                        byte[] mac = new byte[6];
                        byte[] desc = new byte[256];
                        for (int i = 0; i < wIFCount[0]; i++)
                        {
                            Array.Copy(szIFInfo, 0 + i * 282, index, 0, index.Length);
                            Array.Copy(szIFInfo, 4 + i * 282, ip, 0, ip.Length);
                            Array.Copy(szIFInfo, 20 + i * 282, mac, 0, mac.Length);
                            Array.Copy(szIFInfo, 26 + i * 282, desc, 0, desc.Length);

                            var id = BitConverter.ToUInt32(index, 0);
                            var ip_address = Encoding.UTF8.GetString(ip).Replace("\0", String.Empty);
                            var mac_address = BitConverter.ToString(mac);
                            var description = Encoding.UTF8.GetString(desc).Replace("\0", String.Empty);

                            interfaces_list.Add(new NetworkInterfaceInfo() { Index = id, Description = description, IPAddress = ip_address, MACAddress = mac_address });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (interfaces_list.Any())
                {
                    cmb_interface.Items.Clear();
                    foreach (var device in interfaces_list)
                    {
                        cmb_interface.Items.Add(device);
                        if (device.MACAddress == selected_interface?.MACAddress)
                        {
                            cmb_interface.SelectedItem = device;
                        }
                    }
                }
            }));
            
        }
        object discover_lock = new object();
        int ret;
        ushort[] wIFCount = new ushort[1];
        byte[] szIFInfo;
        private void RefreshDevices()
        {
            loading.DeferedVisibility = true;
            Task.Run(new Action(() =>
            {
                lock (discover_lock)
                {
                    
                    List<E1212Device> devices_list= new List<E1212Device>();
                    try
                    {
                        int MAX_DEVICES = 3;
                        uint nTimeout = 100;
                        uint nRetryCount = 1;
                        uint nType = MXIO_CS.E1200_SERIES;
                        byte[] struML = new byte[47 * MAX_DEVICES];
                        uint[] nNumber = new uint[1];
                        ret = MXIO_CS.MXIO_AutoSearch(nType, nRetryCount, nTimeout, nNumber, struML);
                        Nodes.MoxaInput.CheckErr(ret, "MXIO_AutoSearch");
                        byte[] deviceID = new byte[2];
                        byte[] ip_address = new byte[16];
                        byte[] mac = new byte[6];
                        byte[] ip_address2 = new byte[16];
                        byte[] mac2 = new byte[6];
                        byte[] lan = new byte[1];
                        for (int i = 0; i < nNumber[0]; i++)
                        {
                            Array.Copy(struML, 0 + i * 47, deviceID, 0, deviceID.Length);
                            Array.Copy(struML, 2 + i * 47, ip_address, 0, ip_address.Length);
                            Array.Copy(struML, 18 + i * 47, mac, 0, mac.Length);
                            Array.Copy(struML, 24 + i * 47, ip_address2, 0, ip_address2.Length);
                            Array.Copy(struML, 40 + i * 47, mac2, 0, mac2.Length);
                            Array.Copy(struML, 46 + i * 47, lan, 0, lan.Length);
                            var id = BitConverter.ToString(deviceID.Reverse().ToArray()).Replace("-", String.Empty);
                            var ip = Encoding.UTF8.GetString(ip_address).Replace("\0", String.Empty);
                            var mac_address = BitConverter.ToString(mac);
                            var ip2 = Encoding.UTF8.GetString(ip_address2).Replace("\0", String.Empty);
                            var mac_address2 = BitConverter.ToString(mac2);
                            var lan_use = lan[0].ToString();
                            if (lan_use == "0")
                                devices_list.Add(new E1212Device() { ID = id, IPAddress = ip, MACAddress = mac_address });
                            else
                                devices_list.Add(new E1212Device() { ID = id, IPAddress = ip2, MACAddress = mac_address2 });
                        }
                    }
                    catch (Exception ex)
                    {
                        DXMessageBox.Show("Device hasn't supported");
                        //this.Close();
                    }
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (devices_list.Any())
                        {
                            lst_device.Items.Clear();
                            foreach (var device in devices_list)
                            {
                                lst_device.Items.Add(device);
                            }
                        }
                        loading.DeferedVisibility = false;
                    }
                    ));
                }
            }));
        }

        bool result = false;
        E1212Device selected_device;
        public E1212Device ShowDevice()
        {
            this.ShowDialog();
            if (result)
                return selected_device;
            else
                return null;
        }
        public NetworkInterfaceInfo ShowInterface()
        {
            return cmb_interface.SelectedItem as NetworkInterfaceInfo;
        }
        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            lock (discover_lock)
            {
                if (lst_device.SelectedIndex != -1)
                    selected_device = lst_device.SelectedItem as E1212Device;
                result = true;
            }
            this.Close();

        }

        private void ThemedWindow_ContentRendered(object sender, EventArgs e)
        {
            RefreshDevices();
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void cmb_interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ret = MXIO_CS.MXIO_SelectIF(wIFCount[0], szIFInfo, (cmb_interface.SelectedItem as NetworkInterfaceInfo).Index);
            Nodes.MoxaInput.CheckErr(ret, "MXIO_SelectIF");
            if (ret == MXIO_CS.MXIO_OK)
            {
                RefreshDevices();
            }
        }
    }
    public class NetworkInterfaceInfo:IHalconDeserializable
    {
        public uint Index { get; set; }
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            return Description + " @ " + IPAddress + " @ " + MACAddress;
        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {

            HelperMethods.LoadParam(item, this);
        }
    }
}
