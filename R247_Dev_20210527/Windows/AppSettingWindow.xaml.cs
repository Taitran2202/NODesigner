using DevExpress.Xpf.Core;
using NOVisionDesigner.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for AppSettingWindow.xaml
    /// </summary>
    public partial class AppSettingWindow : ThemedWindow,INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        object _selected_page;
        public object SelectedPage
        {
            get
            {
                return _selected_page;
            }
            set
            {
                if (_selected_page != value)
                {
                    _selected_page = value;
                    RaisePropertyChanged("SelectedPage");
                }
            }
        }

        public AppSettingWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void AccordionItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void project_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new GeneralSettingView();
        }

        private void performance_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new PerformanceSettingView();
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            App.SaveSetting();
            this.Close();
        }

        private void licence_view_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new LicenceView();
        }

        private void gpu_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new GPUView();
        }

        private void services_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new AppCommunicationView();
        }

        private void job_selected(object sender, RoutedEventArgs e)
        {
            SelectedPage = new JobSettingView();
        }
    }
}
