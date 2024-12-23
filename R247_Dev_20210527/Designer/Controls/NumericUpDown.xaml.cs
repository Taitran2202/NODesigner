using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        public static readonly  DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown));
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public NumericUpDown()
        {
            InitializeComponent();
            
            txt_value.TextChanged += Txt_value_TextChanged;
            btn_up.Click += Btn_up_Click;
            btn_down.Click += Btn_down_Click;
            layout_root.DataContext = this;
        }

        private void Btn_up_MouseDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Btn_down_Click(object sender, RoutedEventArgs e)
        {
            Value--;
        }

        private void Btn_up_Click(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void Txt_value_TextChanged(object sender, TextChangedEventArgs e)
        {
           
        }
    }
}
