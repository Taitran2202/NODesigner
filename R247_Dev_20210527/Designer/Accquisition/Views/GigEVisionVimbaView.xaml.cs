using AVT.VmbAPINET;
using DevExpress.Data.Helpers;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using NOVisionDesigner.Designer.Accquisition.Windows;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for GigEVisionVimbaView.xaml
    /// </summary>
    public partial class GigEVisionVimbaView : System.Windows.Controls.UserControl, IViewFor<BaseVimbaInterface>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(BaseVimbaInterface), typeof(GigEVisionVimbaView), new PropertyMetadata(null));

        public BaseVimbaInterface ViewModel
        {
            get => (BaseVimbaInterface)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (BaseVimbaInterface)value;
        }
        public GigEVisionVimbaView()
        {
            InitializeComponent();
            //this.DataContext = ViewModel;
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
            

        }
        ICollectionView view1;
        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            GigEVisionVimbaSettingWindow wd = new GigEVisionVimbaSettingWindow(ViewModel);
            wd.ShowDialog();
        }

        private void btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow2 wd = new DeviceListWindow2(BaseVimbaInterface.VimbaSystem, ViewModel.Type);
            string selected = wd.ShowDevice();
            if (selected != "")
            {
                ViewModel.SetDevice(selected);
                //ViewModel.Connect();
            }
                

        }

        private void btn_sofware_trigger_click(object sender, RoutedEventArgs e)
        {
            ViewModel?.Trigger();
        }

        private void btn_disconnect_camera_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Connected = ViewModel.Disconnect();
        }
        
        

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lst_features_control.ViewModel = ViewModel;
            lst_features_control.lst_features1.ItemsSource = ViewModel.ListCommonFeatures;
            view1 = CollectionViewSource.GetDefaultView(ViewModel.ListCommonFeatures);
            BindingOperations.EnableCollectionSynchronization(ViewModel.ListCommonFeatures, ViewModel.featureCommonLock);
            BindingOperations.EnableCollectionSynchronization(ViewModel.ListFeatures, ViewModel.featureLock);
            //BindingOperations.EnableCollectionSynchronization(ViewModel.ListCommonFeatures, ViewModel.featureCommonLock);
            //view1.Filter = CustomFilter;
            //view1.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));


        }

        private void btn_reload_camera_setting_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadCameraSetting();
        }

        private void btn_save_camera_setting_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCameraSetting();
        }

        private void btn_add_features_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Connected)
            {
                AddFeaturesWindow wd = new AddFeaturesWindow(ViewModel);
                wd.ShowDialog();
            }
            
        }

        private void btn_connect_camera_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Connected = ViewModel.Connect();
        }

        private void btn_show_live_view_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsRun)
            {
                DXMessageBox.Show("You must stop camera in other window first.", "Warning");
                return;
            }
            var wd = new VimbaLiveViewWindow(ViewModel);
            wd.ShowDialog();    
        }
    }
}
