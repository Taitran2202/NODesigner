using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Automation.BDaq;
using NOVisionDesigner.Designer.Controls;
using NOVisionDesigner.Designer.Nodes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for USBIOOutput.xaml
    /// </summary>
    public partial class MoxaOutput : Window, INotifyPropertyChanged
    {
        //InstantDiCtrl IOControl;

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        List<E1212Device> _devices;
        public List<E1212Device> Devices
        {
            get
            {
                return _devices;
            }
            set
            {
                if (_devices != value)
                {
                    _devices = value;
                    RaisePropertyChanged("Devices");
                }
            }
        }
        E1212Device _device;
        public E1212Device Device
        {
            get
            {
                return _device;
            }
            set
            {
                if (_device != value)
                {
                    _device = value;
                    RaisePropertyChanged("Device");
                }
            }
        }
        ObservableCollection<PortState> PortStates = new ObservableCollection<PortState>();
        Nodes.MoxaOutput node;
        int ret;
        public MoxaOutput(Nodes.MoxaOutput node)
        {
            InitializeComponent();
            this.node = node;
            this.DataContext = this;

            port_control.ItemsSource = PortStates;
            Device = node.SelectedDevice;
            //GetListDevice();
            lst_view.ItemsSource = node.PortStates;
            //cmb_listDevices.SelectedItem = Devices.FirstOrDefault(x=>x.MACAddress==node.SelectedDevice.MACAddress);
            if(node.CheckAndReconnect())
                CreatePorts();
        }

        public void GetListDevice()
        {
            var newdevices = new List<E1212Device>();

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
                    newdevices.Add(new E1212Device() { ID = id, IPAddress = ip, MACAddress = mac_address });
                else
                    newdevices.Add(new E1212Device() { ID = id, IPAddress = ip2, MACAddress = mac_address2 });
            }
            Devices = newdevices;
        }

        private void ResetListDevicesFunction(object sender, RoutedEventArgs e)
        {
            MoxaDeviceList wd = new MoxaDeviceList(node.SelectedInterface);
            var device = wd.ShowDevice();
            //GetListDevice();
            if (device != null)
            {
                //node.CheckAndReconnect();
                node.SelectedDevice = device;
                Device = device;
                node.InitDevice();
                CreatePorts();
            }
            node.SelectedInterface = wd.ShowInterface();
        }
        public void CreatePorts()
        {
            PortStates.Clear();
            
            uint[] dwGetDOValue = new uint[1];
            ret = MXIO_CS.E1K_DO_Reads(node.hConnection[0], 0, 8, dwGetDOValue);
            Nodes.MoxaInput.CheckErr(ret, "E1K_DO_Reads");
            BitArray DO_Value = new BitArray(new byte[] { (byte)dwGetDOValue[0] });
            var bits = new List<BitValue>();
            for (int j = 0; j < 8; j++)
            {
                if (!node.DIOPortStatus[j])
                    bits.Add(new BitValue() { Visible = false });
                else
                {
                    bits.Add(new BitValue() { Index = j, State = DO_Value[j], PortIndex = 0 });
                }

            }
            PortStates.Add(new PortState() { BitStates = bits, PortIndex = 0 });

        }

        private void Changed_Devices(object sender, SelectionChangedEventArgs e)
        {
            //var index = cmb_listDevices.SelectedIndex;
            //if (index > -1)
            //{
            //    byte[] status = new byte[1];
            //    if (MXIO_CS.MXEIO_CheckConnection(node.hConnection[0], 100, status) == MXIO_CS.MXIO_OK)
            //        if (status[0] == MXIO_CS.CHECK_CONNECTION_OK)
            //        {
            //            ret = MXIO_CS.MXEIO_Disconnect(node.hConnection[0]);
            //            Nodes.MoxaInput.CheckErr(ret, "MXEIO_Disconnect");
            //        }
                
            //    node.SelectedDevice = Devices[index];
            //    node.InitDevice();
            //    CreatePorts();
            //}


        }

        public byte ConvertStringToByteData(string value)
        {
            var result = (byte)Convert.ToInt32(value, 2);
            return result;
        }

        private void Changed_Modes(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var state = ((sender as CheckBox).DataContext as BitValue);
            if (state != null)
            {
                byte index = (byte)(state.Index + state.PortIndex * 8);
                DO_Write(index, 1);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var state = ((sender as CheckBox).DataContext as BitValue);
            if (state != null)
            {
                byte index = (byte)(state.Index + state.PortIndex * 8);
                DO_Write(index, 0);
            }
        }
        private void DO_Write(byte index, uint value)
        {
            node.CheckAndReconnect();
            ret = MXIO_CS.E1K_DO_Writes(node.hConnection[0], index, 1, value);
            Nodes.MoxaInput.CheckErr(ret, "E1K_DO_Writes");
        }
        private void btn_add_model_click(object sender, RoutedEventArgs e)
        {
            node.PortStates.Add(new BitValue());
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as BitValue;
            if (vm != null)
            {
                try
                {
                    node.PortStates.Remove(vm);
                }
                catch (Exception ex)
                {

                }

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(node.CheckAndReconnect())
                CreatePorts();
        }
    }
}
