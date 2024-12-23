using Microsoft.Win32;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Designer.Windows.Calibration;
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
    /// Interaction logic for AccquisitionView.xaml
    /// </summary>
    public partial class AccquisitionView : UserControl,IViewFor<Accquisition>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(Accquisition), 
           typeof(AccquisitionView), new PropertyMetadata(null));

        public Accquisition ViewModel
        {
            get => (Accquisition)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (Accquisition)value;
        }
        public AccquisitionView()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                {
                    
                    InitSelectionBox();
                    this.WhenAnyValue(x => x.ViewModel).BindTo(stack_preprocessing, x => x.DataContext).DisposeWith(d);
                    this.WhenAnyValue(v => v.ViewModel.Interface).BindTo(this, v => v.viewmodelhost.ViewModel).DisposeWith(d);
                    this.WhenAnyValue(v => v.ViewModel.IsRun).Subscribe(x=>
                    {
                        if (x)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action( () =>
                            {
                                btn_live_image.Visibility = Visibility.Hidden;
                                btn_stop_image.Visibility = Visibility.Visible;
                            }));
                            
                        }
                        else
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                btn_live_image.Visibility = Visibility.Visible;
                                btn_stop_image.Visibility = Visibility.Hidden;
                            }));
                        }
                    }
                    ).DisposeWith(d);
                }
            });

        }
        

        #region Method
        void InitSelectionBox()
        {
            cmb_interface.SelectionChanged -= cmb_interface_SelectionChanged;
            if (ViewModel.Interface == null)
            {
                cmb_interface.SelectedItem = null;
                cmb_interface.SelectionChanged += cmb_interface_SelectionChanged;
                return;
            }
            switch (ViewModel.Interface.Type)
            {
                case "GigEVision2": cmb_interface.SelectedItem = "GigEVision2"; break;
                case "GigEVisionVimba": cmb_interface.SelectedItem = "GigEVisionVimba"; break;
                case "GigEVisionBasler": cmb_interface.SelectedItem = "GigEVisionBasler"; break;
                case "GigEVisionHIK": cmb_interface.SelectedItem = "GigEVisionHIK"; break;
                case "Sapera": cmb_interface.SelectedItem = "Sapera"; break;
                case "EBUS": cmb_interface.SelectedItem = "EBUS"; break;
                case "USB3Vision": cmb_interface.SelectedItem = "USB3Vision"; break;
                case "USB3VisionVimba": cmb_interface.SelectedItem = "USB3VisionVimba"; break;
                default:
                    cmb_interface.SelectedItem = null;
                    break;
            }
            cmb_interface.SelectionChanged += cmb_interface_SelectionChanged;
            
        }
        #endregion

        #region View

        private void btn_calib_Click(object sender, RoutedEventArgs e)
        {
            CalibrationView wd;
            lock (ViewModel.image_lock)
            {
                wd = new CalibrationView(ViewModel.Calib, ViewModel.CurrentImage.Image.CopyImage());
            }
            wd.ShowDialog();
        }

        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to clear this interface?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                cmb_interface.SelectedItem = null;
        }

        private void btn_live_image_Click(object sender, RoutedEventArgs e)
        {
            if(!ViewModel.IsRun)
            {
                ViewModel.Start();
            }
        }

        private void btn_run_setting_1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenImage();
           
        }

        private void btn_save_image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.CurrentImage != null)
                {
                    if (ViewModel.CurrentImage.Image != null)
                    {
                        if (ViewModel.CurrentImage.Image.IsInitialized())
                        {
                            SaveFileDialog saveDialog = new SaveFileDialog();
                            if (saveDialog.ShowDialog() == true)
                            {
                                ViewModel.CurrentImage.Image.WriteImage("png", 0, saveDialog.FileName);
                            }

                        }
                    }
                }
            }catch(Exception ex)
            {

            }
            
        }

        private void btn_simulate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmb_interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmb_interface.SelectedItem as string)
            {
                case "GigEVision2":
                    ViewModel.Interface = new GigEVision2();
                    break;
                case "GigEVision2Script":
                    ViewModel.Interface = new GigEVision2Script();

                    break;
                case "GigEVisionVimba":
                    ViewModel.Interface = new GigEVisionVimba(ViewModel.basedir);
                    break;
                case "Sapera":
                    ViewModel.Interface = new Sapera();
                    break;
                case "EBUS":
                    ViewModel.Interface = new EBUS();
                    break;
                case "USB3Vision":
                    ViewModel.Interface = new USB3Vision();
                    break;
                case "Webcam":
                    ViewModel.Interface = new Webcam();
                    break;
                case "GigEVisionBasler":
                    ViewModel.Interface = new GigEVisionBasler();
                    break;
                case "GigEVisionHIK":
                    ViewModel.Interface = new GigEVisionHIK();
                    break;
                case "USB3VisionVimba":
                    ViewModel.Interface = new USB3VisionVimba(ViewModel.basedir);
                    break;
                default:
                    ViewModel.Interface = null;
                    break;
            }
        }

        #endregion

        private void btn_stop_image_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsRun)
            {
                ViewModel.Stop();
            }
            
        }

        private void btn_record_setting_Click(object sender, RoutedEventArgs e)
        {
            RecordSettingWindow wd = new RecordSettingWindow(ViewModel.Record);
            wd.ShowDialog();
        }

        private void btn_filmstrip_setting_Click(object sender, RoutedEventArgs e)
        {
            
            ViewModel.ShowFilmstrip(this);
            
        }
    }
}
