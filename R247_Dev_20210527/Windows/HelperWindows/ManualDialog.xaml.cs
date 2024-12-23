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

namespace NOVisionDesigner.Windows.HelperWindows
{
    /// <summary>
    /// Interaction logic for ManualDialog.xaml
    /// </summary>
    public partial class ManualDialog : Window
    {
        public ManualDialog()
        {
            InitializeComponent();
        }
        public void ManualShow(int type, string title, string content)
        {
            switch (type)
            {
                case 0:
                    content_lbl.Background = Brushes.Green;
                    break;
                case 1:
                    content_lbl.Background = Brushes.Red;
                    break;
                case 2:
                    content_lbl.Background = Brushes.Yellow;
                    break;
            }
            content_lbl.Content = title;
            content_tbl.Text = content;
            this.ShowDialog();
        }
        private void ok_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ok_btn_Click(null, null);
            }
        }
    }
}
