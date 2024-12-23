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

namespace NOVisionDesigner.Designer.Windows.GigeCameraUserControl
{
    /// <summary>
    /// Interaction logic for GigEVision2View.xaml
    /// </summary>
    public partial class GigEVision2View : UserControl
    {
        GigeCameraModel model;
        public GigEVision2View(GigeCameraModel model)
        {
            this.model = model;
            InitializeComponent();
            this.DataContext = model;
        }

        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            if(!model.is_run)
            {
                model.Start();
            }
            else
            {
                model.Stop();
            }
            
        }

        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow wd = new DeviceListWindow();
            string selected = wd.ShowDevice();
            if (selected != "")
                model.Device = selected;
                model.Connect();
                
        }

        private void btn_sofware_trigger_click(object sender, RoutedEventArgs e)
        {
            model?.Trigger();
        }
        private void Btn_UserSetLoad_Click(object sender, RoutedEventArgs e)
        {
            model.UserSetLoadEvent();
        }

        private void Btn_UserSetSave_Click(object sender, RoutedEventArgs e)
        {
            model.UserSetSaveEvent();
        }

        private void btn_live_view_Click(object sender, RoutedEventArgs e)
        {
            //model?.LiveView();
        }
    }
}
