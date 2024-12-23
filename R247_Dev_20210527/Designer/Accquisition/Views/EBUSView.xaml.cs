using NOVisionDesigner.Designer.Accquisition.Windows;
using PvGUIDotNet;
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
    /// Interaction logic for EBUSView.xaml
    /// </summary>
    public partial class EBUSView : UserControl, IViewFor<EBUS>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(EBUS), typeof(EBUSView), new PropertyMetadata(null));
        private EBUS _view_model;
        public EBUS ViewModel
        {
            get => _view_model;
            set => _view_model = value;
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (EBUS)value;
        }

        public EBUSView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).Subscribe(x =>
                {
                    this.DataContext = x;
                }).DisposeWith(d);
            });
            //this.DataContext = ViewModel;
            //this.ViewModel = ViewModel;
        }

        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            PvDeviceFinderForm lFinder = new PvDeviceFinderForm();

            // Show device finder
            if ((lFinder.ShowDialog() != System.Windows.Forms.DialogResult.OK) ||
                (lFinder.Selected == null))
            {
                return;
            }
            ViewModel.Device = lFinder.Selected.ConnectionID;

        }

        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    if (ViewModel.device == null)

                        ViewModel.Connect();
                }
                catch (Exception ex)
                {

                }
                if (ViewModel.device != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            EbusSettingWindow wd = new EbusSettingWindow(ViewModel.device.Parameters);
                            wd.Owner = Window.GetWindow(this);
                            wd.Show(); 
                        }catch(Exception ex)
                        {

                        }
                        
                    }));

                }
                //else
                //{
                //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //    {
                //        DXMessageBox.Show("Cannot connect to camera!");
                //    }));
                //}
            }));

        }

        private void Btn_test_output_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    if (ViewModel.device == null)

                        ViewModel.Connect();
                }
                catch (Exception ex)
                {

                }
                if (ViewModel.device != null)
                {
                    ViewModel.Reject();

                }
                //else
                //{

                //        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //        {
                //            DXMessageBox.Show("Cannot connect to camera!");
                //        }));

                //}
            }));
        }

        private void Btn_communication_setting_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    if (ViewModel.device == null)

                        ViewModel.Connect();
                }
                catch (Exception ex)
                {

                }
                if (ViewModel.device != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        EbusSettingWindow wd = new EbusSettingWindow(ViewModel.device.CommunicationParameters);
                        wd.ShowDialog(); ;
                    }));

                }
                //else
                //{
                //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //    {
                //        DXMessageBox.Show("Cannot connect to camera!");
                //    }));
                //}
            }));
        }

        private void Btn_stream_setting_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                try
                {
                    if (ViewModel.device == null)

                        ViewModel.Connect();
                }
                catch (Exception ex)
                {

                }
                if (ViewModel.mStream != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {

                        EbusSettingWindow wd = new EbusSettingWindow(ViewModel.mStream.Parameters);
                        wd.ShowDialog(); ;
                    }));

                }
                //else
                //{
                //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //    {
                //        DXMessageBox.Show("Cannot connect to camera!");
                //    }));
                //}
            }));
        }
        private void Btn_adjustment_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsRun)
            {
                MessageBox.Show(Window.GetWindow(this),"Please offline camera first!!");
                return;
            }
            CameraAdjustment wd = new CameraAdjustment(ViewModel);
            wd.ShowDialog();
        }

        private void Btn_quick_setting_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsRun)
            {
                MessageBox.Show(Window.GetWindow(this), "Please offline camera first!!");
                return;
            }
            var old_callback = ViewModel.ImageAcquired;
            var owner = Window.GetWindow(this);
            if (owner != null)
            {
                UniquaEbusWindow wd = new UniquaEbusWindow(ViewModel);
                wd.Owner = owner;
                wd.ShowDialog();
            }
            else
            {
                UniquaEbusWindow wd = new UniquaEbusWindow(ViewModel);
                wd.ShowDialog();
            }
            
            ViewModel.ImageAcquired = old_callback;
        }
    }
}
