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
using Automation.BDaq;

namespace R247_Dev_20210527.Designer.Windows
{
    /// <summary>
    /// Interaction logic for USBIOOutput.xaml
    /// </summary>
    public partial class USBIOInput : Window
    {
        public InstantDiCtrl inputCtrl;
        public string port0data;
        public string port1data;
        public USBIOInput(string selectedDevice)
        {
            InitializeComponent();
            inputCtrl = new InstantDiCtrl();
            GetListDevice();

            if (selectedDevice == null)
            {
                inputCtrl.SelectedDevice = new DeviceInformation(0);
                cmb_listDevices.SelectedIndex = 0;

            }
            else
            {
                inputCtrl.SelectedDevice = new DeviceInformation(selectedDevice);
                cmb_listDevices.SelectedIndex = cmb_listDevices.Items.IndexOf(selectedDevice);
            }



            port0data = ReadDataFromPort0();
            port1data = ReadDataFromPort1();
            UpdateCheckboxPort1(port1data);
            UpdateCheckboxPort0(port0data);


            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,0,100);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            port0data = ReadDataFromPort0();
            port1data = ReadDataFromPort1();
            UpdateCheckboxPort1(port1data);
            UpdateCheckboxPort0(port0data);
        }

        public void UpdateCheckboxPort0(string bin)
        {
            P0D0.IsChecked = (bin[7]) == 49 ? true : false;
            P0D1.IsChecked = (bin[6]) == 49 ? true : false;
            P0D2.IsChecked = (bin[5]) == 49 ? true : false;
            P0D3.IsChecked = (bin[4]) == 49 ? true : false;
            P0D4.IsChecked = (bin[3]) == 49 ? true : false;
            P0D5.IsChecked = (bin[2]) == 49 ? true : false;
            P0D6.IsChecked = (bin[1]) == 49 ? true : false;
            P0D7.IsChecked = (bin[0]) == 49 ? true : false;
        }
        public void UpdateCheckboxPort1(string bin)
        {
            P1D0.IsChecked = (bin[7]) == 49 ? true : false;
            P1D1.IsChecked = (bin[6]) == 49 ? true : false;
            P1D2.IsChecked = (bin[5]) == 49 ? true : false;
            P1D3.IsChecked = (bin[4]) == 49 ? true : false;
            P1D4.IsChecked = (bin[3]) == 49 ? true : false;
            P1D5.IsChecked = (bin[2]) == 49 ? true : false;
            P1D6.IsChecked = (bin[1]) == 49 ? true : false;
            P1D7.IsChecked = (bin[0]) == 49 ? true : false;

        }
        public string ReadDataFromPort0()
        {
            //Do something
            byte data;
            inputCtrl.Read(0, out data);
            string bin = Convert.ToString(data, 2);
            var tempLength = bin.Length;
            if (tempLength < 8)
            {
                for (int i = 0; i < (8 - tempLength); i++)
                {
                    bin = "0" + bin;
                }
            }
            return bin;
        }
        public string ReadDataFromPort1()
        {
            //Do something
            byte data;
            inputCtrl.Read(1, out data);
            string bin = Convert.ToString(data, 2);
            var tempLength = bin.Length;
            if (tempLength < 8)
            {
                for (int i = 0; i < (8 - tempLength); i++)
                {
                    bin = "0" + bin;
                }
            }
            return bin;
        }

        public void GetListDevice()
        {
            cmb_listDevices.Items.Clear();
            for (int i = 0; i < inputCtrl.SupportedDevices.Count; i++)
            {
                cmb_listDevices.Items.Add(inputCtrl.SupportedDevices[i].Description);
            }
        }

        private void ResetListDevicesFunction(object sender, RoutedEventArgs e)
        {
            GetListDevice();
        }

        private void Changed_Devices(object sender, SelectionChangedEventArgs e)
        {
            var index = cmb_listDevices.Items.IndexOf(cmb_listDevices.SelectedItem);
            inputCtrl.SelectedDevice = new DeviceInformation(index);
            port0data = ReadDataFromPort0();
            port1data = ReadDataFromPort1();
            UpdateCheckboxPort0(port0data);
            UpdateCheckboxPort1(port1data);
        }

      

     
        public byte ConvertStringToByteData(string value)
        {
            var result = (byte)Convert.ToInt32(value, 2);
            return result;
        }

    }
}
