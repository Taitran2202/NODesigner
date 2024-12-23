using DevExpress.Xpf.Core;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for EditUserControl.xaml
    /// </summary>
    public partial class EditUserControl : UserControl
    {
        public bool IsSubmit { get; set; } = false;
        public EditUserControl()
        {
            InitializeComponent();
        }

        private void btn_submit_Click(object sender, RoutedEventArgs e)
        {
            IsSubmit= true;
            Visibility = Visibility.Collapsed;
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            IsSubmit= false;
            Visibility= Visibility.Collapsed;
        }
    }
}
