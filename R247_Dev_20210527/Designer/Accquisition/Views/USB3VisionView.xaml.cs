using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
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
    /// Interaction logic for USB3VisionView.xaml
    /// </summary>
    public partial class USB3VisionView : UserControl, IViewFor<USB3Vision>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(USB3Vision), typeof(USB3VisionView), new PropertyMetadata(null));

        public USB3Vision ViewModel
        {
            get => (USB3Vision)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (USB3Vision)value;
        }
        public USB3VisionView()
        {

            InitializeComponent();
            //this.DataContext = model;
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
        }
        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            //try
            //{

            //    if (model.is_run == false)
            //    {
            //        EditUSB3CameraWindow wd = new EditUSB3CameraWindow(model);
            //        wd.ShowDialog();
            //    }
            //    else
            //    {
            //        MessageBox.Show("Offline Camera");
            //    }

            //}
            //catch (Exception)
            //{

            //}

        }

        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow wd = new DeviceListWindow("USB3Vision");
            string selected = wd.ShowDevice();
            if (selected != "")

            {
                ViewModel.Device = selected;
                //model.Connect();
            }

        }


    }
}
