using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using NOVisionDesigner.Designer.Nodes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for USBIOOutput.xaml
    /// </summary>
    public partial class USBIOOutput : Window,INotifyPropertyChanged
    {
        InstantDoCtrl IOControl;

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        List<string> _devices;
        public List<string> Devices
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
        ObservableCollection<PortState> PortStates = new ObservableCollection<PortState>();
        USBOutput node;
        public USBIOOutput( Nodes.USBOutput NodeDo)
        {
            InitializeComponent();
            this.node = NodeDo;
            this.DataContext = this;
            cmb_listDevices.SelectedValue = NodeDo.SelectedDevice;
            port_control.ItemsSource = PortStates;
            IOControl = NodeDo.IOControl;
            GetListDevice();
            lst_view.ItemsSource = node.PortStates;
        }

        public void GetListDevice()
        {
            var newdevices = new List<string>();
            foreach(var item in IOControl.SupportedDevices)
            {
                newdevices.Add(item.Description);
            }
            Devices = newdevices;
        }

        private void ResetListDevicesFunction(object sender, RoutedEventArgs e)
        {
            GetListDevice();
        }
        public void CreatePorts()
        {
            PortStates.Clear();
            if (IOControl.SelectedDevice.DeviceNumber > -1)
            {
                for(int i = 0; i < IOControl.PortCount; i++)
                {
                    var bits = new List<BitValue>();
                    
                    IOControl.Read(i, out byte data);
                    
                    for (int j = 0; j < 8; j++)
                    {
                        bits.Add(new BitValue() { Index = j, State = ((data>>j)&1)==1 ,PortIndex=i});
                    }
                    PortStates.Add(new PortState() { BitStates = bits, PortIndex = i });
                }
            }
        }
        private void Changed_Devices(object sender, SelectionChangedEventArgs e)
        {
            var index = cmb_listDevices.SelectedIndex;
            if (index>-1)
            {
                IOControl.SelectedDevice = new DeviceInformation(Devices[index]);
                node.SelectedDevice = Devices[index];
                CreatePorts();
            }
            
    
        }

        public byte ConvertStringToByteData(string value)
        {
            var result =(byte)Convert.ToInt32(value,2);
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
                //IOControl.WriteBit(state.po)
                IOControl.WriteBit(state.PortIndex, state.Index, 1);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var state = ((sender as CheckBox).DataContext as BitValue);
            if (state != null)
            {
                //IOControl.WriteBit(state.po)
                IOControl.WriteBit(state.PortIndex, state.Index, 0);
            }
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
    }
}
