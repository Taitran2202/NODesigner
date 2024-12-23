using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
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
    /// Interaction logic for AcqussitionView.xaml
    /// </summary>
    public partial class AcqussitionView : UserControl, INotifyPropertyChanged
    {
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        #region Field
        public Accquisition acq { get; set; }
        #endregion
        public AcqussitionView()
        {
            InitializeComponent();
        }
        public AcqussitionView(Accquisition acq)
        {
            InitializeComponent();
            this.acq = acq;
            content.DataContext = acq;
            InitSelectionBox();
        }

        #region Method
        void InitSelectionBox()
        {
            cmb_interface.SelectionChanged -= cmb_interface_SelectionChanged;
            if (acq.Interface == null) 
            { 
                cmb_interface.SelectedItem = null;
                cmb_interface.SelectionChanged += cmb_interface_SelectionChanged;
                return; 
            }
            switch (acq.Interface.Type)
            {
                case "GigEVision2": cmb_interface.SelectedItem = "GigEVision2";break;
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
            
            (acq.Interface as GigeCameraModel)?.Start();
        }

        private void btn_run_setting_1_Click(object sender, RoutedEventArgs e)
        {
            (acq.Interface as GigeCameraModel)?.OpenImage();
        }

        private void btn_save_image_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_simulate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmb_interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cmb_interface.SelectedItem as string)
            {
                case "GigEVision2":
                    acq.Interface = new GigeCameraModel();
                    
                    break;
                default:
                    acq.Interface = null;
                    break;
            }
        }

        #endregion

    }

}
