using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for GigEVisionHIKView.xaml
    /// </summary>
    public partial class GigEVisionHIKView : UserControl, IViewFor<GigEVisionHIK>
    {
        public GigEVisionHIKView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
        }
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(GigEVisionHIK), typeof(GigEVisionHIKView), new PropertyMetadata(null));

        public GigEVisionHIK ViewModel
        {
            get => (GigEVisionHIK)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (GigEVisionHIK)value;
        }
       
        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow2 wd = new DeviceListWindow2("GigEVisionHIK");
            string selected = wd.ShowDevice();
            if (selected != "")
                ViewModel.Device = selected;
        }

        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {

        }

        private void SoftwareTriggerClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Trigger();
        }

        private void btn_reload_camera_setting_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadCameraSetting();
        }

        private void btn_save_camera_setting_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
