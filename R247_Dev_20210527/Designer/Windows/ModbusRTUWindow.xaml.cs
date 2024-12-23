using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
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
    /// Interaction logic for ModbusRTUWindow.xaml
    /// </summary>
    public partial class ModbusRTUWindow : ThemedWindow
    {
        private List<string> _Lst_com_port = new List<string>();

        public List<string> Lst_com_port
        {
            get { return _Lst_com_port; }
            set
            {
                _Lst_com_port = value;
            }
        }
        public ModbusRTUWindow(string PortName, int BaudRate, Parity Parity, StopBits StopBit)
        {
            InitializeComponent();
            this.DataContext = this;
            _Lst_com_port = SerialPort.GetPortNames().ToList<string>();
            this.PortName = PortName;
            this.BaudRate = BaudRate;
            this.SerialParity = Parity;
            this.SerialStopBits = StopBit;
            /*SelectedBaudrate = BaudRate.ToString();
            SelectedParity = Parity.ToString();
            SelectedStopBits = StopBit.ToString();*/
        }
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        //public string SelectedBaudrate { get; set; }
        public StopBits SerialStopBits { get; set; }
        //public string SelectedStopBits { get; set; }
        //Parity _SerialParity;
        public Parity SerialParity { get; set; }
        //public string SelectedParity { get; set; }
        private void on_ok_click(object sender, RoutedEventArgs e)
        {
            this.PortName = cbm_com_port.SelectedItem as string;
            this.BaudRate = Int32.Parse(cbm_baud.SelectedItem as string);
            switch(cbm_parity.SelectedItem as string)
            {
                case "None":
                    SerialParity = Parity.None;
                    break;
                case "Odd":
                    SerialParity = Parity.Odd;
                    break;
                case "Even":
                    SerialParity = Parity.Even;
                    break;
                default:
                    SerialParity= Parity.None;
                    break;
            }
            switch (cbm_stop_bit.SelectedItem as string)
            {
                case "None":
                    SerialStopBits= StopBits.None;
                    break;
                case "One":
                    SerialStopBits = StopBits.One;
                    break;
                case "Two":
                    SerialStopBits = StopBits.Two;
                    break;
                default:
                    SerialStopBits = StopBits.None;
                    break;
            }
            this.Close();
        }
    }
    public class ParityToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Parity parity)
            {
                return parity.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string parityString && Enum.TryParse(parityString, out Parity parity))
            {
                return parity;
            }

            return Binding.DoNothing;
        }
    }

    public class StopBitsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StopBits stopbit)
            {
                return stopbit.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stopbitString && Enum.TryParse(stopbitString, out StopBits stopbit))
            {
                return stopbit;
            }

            return Binding.DoNothing;
        }
    }
    public class BaudToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int baudrate)
            {
                return baudrate.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string baudrateString && int.TryParse(baudrateString, out int baudrate))
            {
                return baudrate;
            }

            return Binding.DoNothing;
        }
    }
}
