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
using WindowsInput;
using WindowsInput.Native;

namespace NOVisionDesigner.Designer.Keyboards
{
    /// <summary>
    /// Interaction logic for VirtualNumbericKeyboard.xaml
    /// </summary>
    public partial class VirtualNumericKeyboard : Window
    {
        InputSimulator sim = new InputSimulator();
        public double result;
        public VirtualNumericKeyboard(double? value)
        {

            InitializeComponent();

            if (value == null)
            {
                txt_num.Text = "";
            }
            else
            {
                txt_num.Text = value.ToString();
            }
            txt_num.Focus();
            txt_num.SelectAll();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.TextEntry((sender as Button).Content.ToString());
        }

        private void btn_backspace_Click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
        }

        private void btn_delete_Click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.DELETE);
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            bool is_double = double.TryParse(txt_num.Text, out result);
            if (is_double)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid number");
            }
        }

        private void btn_backspace_KeyDown(object sender, KeyEventArgs e)
        {
            sim.Keyboard.KeyDown(VirtualKeyCode.BACK);
        }

        private void btn_backspace_KeyUp(object sender, KeyEventArgs e)
        {
            sim.Keyboard.KeyUp(VirtualKeyCode.BACK);
        }

        private void btn_backspace_MouseDown(object sender, MouseButtonEventArgs e)
        {
            sim.Keyboard.KeyDown(VirtualKeyCode.BACK);
        }

        private void btn_backspace_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // sim.Keyboard.KeyUp(VirtualKeyCode.BACK);
        }
    }
}
