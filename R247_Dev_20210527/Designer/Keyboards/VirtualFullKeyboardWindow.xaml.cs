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

namespace NOVisionDesigner.Designer.Keyboards
{
    /// <summary>
    /// Interaction logic for VirtualFullKeyboardWindow.xaml
    /// </summary>
    public partial class VirtualFullKeyboardWindow : Window
    {
        public string result;
        public VirtualFullKeyboardWindow(string text,bool is_password=false)
        {

            InitializeComponent();
            if (is_password)
            {
                txt_input.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#password");
            }
            txt_input.Text = text;
            txt_input.Focus();
            txt_input.SelectAll();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            result = txt_input.Text;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
