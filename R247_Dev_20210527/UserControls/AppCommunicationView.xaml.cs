using DevExpress.Xpf.Core;
using NOVisionDesigner.Services;
using NOVisionDesigner.ViewModel;
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
    /// Interaction logic for AppCommunicationView.xaml
    /// </summary>
    public partial class AppCommunicationView : UserControl
    {
        public AppCommunicationView()
        {
            InitializeComponent();
            lst_view.ItemsSource = MainViewModel.Instance.ServiceManager.Services;
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as ApplicationService;
            if (selected != null)
            {
                if (DXMessageBox.Show(this, "Do you want to remove the selected service?", "Warning",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    MainViewModel.Instance.ServiceManager.Services.Remove(selected);
                    selected.Dispose();
                }
            }
            
        }

        private void btn_add_new_service_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ServiceManager.AddService("RSLogixServiceViewModel");
        }

        private void btn_add_host_link_udp_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ServiceManager.AddService("HostLinkUDPViewModel");
        }
    }
}
