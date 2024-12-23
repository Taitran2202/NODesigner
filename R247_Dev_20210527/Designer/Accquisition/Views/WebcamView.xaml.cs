using HalconDotNet;
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
    /// Interaction logic for WebcamView.xaml
    /// </summary>
    public partial class WebcamView : UserControl, IViewFor<Webcam>
    {
        public WebcamView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
            this.DataContext = ViewModel;
        }
        public static readonly DependencyProperty ViewModelProperty = 
            DependencyProperty.Register(nameof(ViewModel), typeof(Webcam), typeof(WebcamView), new PropertyMetadata(null));
        public Webcam ViewModel
        {
            get => (Webcam)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (Webcam)value;
        }

        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.TriggerSoftwareCommand == null)
            {
                Console.WriteLine("FSDFSD");
            }    

        }


        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow2 wd = new DeviceListWindow2("DirectShow");
            string selected = wd.ShowDevice();
            if (selected != "")

            {
                ViewModel.Device = selected;
                //model.Connect();
            }
        }
    }
}
