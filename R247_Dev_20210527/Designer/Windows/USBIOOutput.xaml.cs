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
    public partial class USBIOOutput : Window
    {
        public InstantDoCtrl outputCtrl;
        public string port0data;
        public string port1data;
        public USBIOOutput(string selectedDevice, Nodes.USBIOOutput NodeDo)
        {
            InitializeComponent();
            outputCtrl = new InstantDoCtrl();
            GetListDevice();

            if (selectedDevice == null)
            {
                outputCtrl.SelectedDevice = new DeviceInformation(0);
                cmb_listDevices.SelectedIndex = 0;

            }
            else
            {
                outputCtrl.SelectedDevice = new DeviceInformation(selectedDevice);
                cmb_listDevices.SelectedIndex = cmb_listDevices.Items.IndexOf(selectedDevice);
            }
            if (NodeDo.Port0.Connections.Count !=0)
            {
                outputCtrl.Write(0, NodeDo.Port0.Value);
            }
            if (NodeDo.Port1.Connections.Count != 0)
            {
                outputCtrl.Write(1, NodeDo.Port1.Value);
            }


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
            outputCtrl.Read(0, out data);
            string bin = Convert.ToString(data, 2);
            var tempLength = bin.Length;
            if (tempLength < 8)
            {
                for (int i=0; i< (8- tempLength); i++)
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
            outputCtrl.Read(1, out data);
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
            for (int i = 0; i < outputCtrl.SupportedDevices.Count; i++)
            {
                cmb_listDevices.Items.Add(outputCtrl.SupportedDevices[i].Description);      
            }
        }

        private void ResetListDevicesFunction(object sender, RoutedEventArgs e)
        {
            GetListDevice();
        }

        private void Changed_Devices(object sender, SelectionChangedEventArgs e)
        {
            var index = cmb_listDevices.Items.IndexOf(cmb_listDevices.SelectedItem);
            outputCtrl.SelectedDevice = new DeviceInformation(index);
            port0data = ReadDataFromPort0();
            port1data = ReadDataFromPort1();

            UpdateCheckboxPort0(port0data);
            UpdateCheckboxPort1(port1data);
        }

        //Checked for port 0
        private void P0D7_Checked(object sender, RoutedEventArgs e)
        {
            //outputCtrl.WriteBit(0,1,);
            if (P0D7.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 7, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 7, 0);
            }
            
        }

        private void P0D6_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D6.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 6, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 6, 0);
            }
        }

        private void P0D5_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D5.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 5, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 5, 0);
            }
        }

        private void P0D4_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D4.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 4, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 4, 0);
            }
        }

        private void P0D3_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D3.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 3, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 3, 0);
            }
        }

        private void P0D2_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D2.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 2, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 2, 0);
            }
        }

        private void P0D1_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D1.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 1, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 1, 0);
            }
        }

        private void P0D0_Checked(object sender, RoutedEventArgs e)
        {
            if (P0D0.IsChecked == true)
            {
                outputCtrl.WriteBit(0, 0, 1);
            }
            else
            {
                outputCtrl.WriteBit(0, 0, 0);
            }
        }

        //Checked for port 1
        private void P1D7_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D7.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 7, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 7, 0);
            }

        }

        private void P1D6_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D6.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 6, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 6, 0);
            }
        }

        private void P1D5_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D5.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 5, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 5, 0);
            }
        }

        private void P1D4_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D4.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 4, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 4, 0);
            }
        }

        private void P1D3_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D3.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 3, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 3, 0);
            }
        }

        private void P1D2_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D2.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 2, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 2, 0);
            }
        }

        private void P1D1_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D1.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 1, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 1, 0);
            }
        }

        private void P1D0_Checked(object sender, RoutedEventArgs e)
        {
            if (P1D0.IsChecked == true)
            {
                outputCtrl.WriteBit(1, 0, 1);
            }
            else
            {
                outputCtrl.WriteBit(1, 0, 0);
            }
        }

        private void WriteDataToPort0(object sender, RoutedEventArgs e)
        {
            
        }
        public byte ConvertStringToByteData(string value)
        {
            var result =(byte)Convert.ToInt32(value,2);
            return result;
        }

    }
}
