using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Python;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for ONNXModelLoaderWindow.xaml
    /// </summary>
    public partial class LoadingWindow : ThemedWindow, INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        bool _is_loading;
        public bool IsLoading
        {
            get
            {
                return _is_loading;
            }
            set
            {
                if (_is_loading != value)
                {
                    _is_loading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }
        PadInspection node;
        public LoadingWindow(PadInspection node)
        {
            InitializeComponent();
            this.DataContext = this;
            this.node = node;
            anomaly_config.DataContext = node.AnomalyConfig;
            fapm_config.DataContext = node.AnomalyConfig.FAPMConfig;
            
            

        }
        TrainAnomalyV3 train = new TrainAnomalyV3();
        private void btn_load_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                IsLoading = true;
                node.AnomalyConfig.ModelName = "DeepKNNAD";
                node.AnomalyConfig.FAPMConfig.UPDATE_THRESHOLD = false;
                node.AnomalyConfig.Save();
                train.TrainPython(node.AnomalyConfig.ConfigDir, node.AnomalyConfig.ModelName, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsLoading = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Training was cancel because of error!", "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });

                    }
                    if (trainargs.State == TrainState.OnGoing)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Logs.Add(trainargs.Log);
                            if (log_box.VerticalOffset == log_box.ScrollableHeight)
                            { log_box.ScrollToEnd(); }
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsLoading = false;
                        node.AnomalyRuntime.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
                IsLoading = false;
            }));
           
        
        }

        private void btn_update_Click(object sender, RoutedEventArgs e)
        {
            return;
            Task.Run(new Action(() =>
            {
                IsLoading = true;
                node.AnomalyConfig.ModelName = "FAPM_Pytorch";
                node.AnomalyConfig.FAPMConfig.UPDATE_THRESHOLD =true;
                node.AnomalyConfig.Save();
                train.TrainPython(node.AnomalyConfig.ConfigDir, node.AnomalyConfig.ModelName, (trainargs) =>
                {
                    if (trainargs.State == Python.TrainState.Cancel | trainargs.State == Python.TrainState.Error)
                    {
                        IsLoading = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Training was cancel because of error!", "Warning", MessageBoxButton.OK, icon: MessageBoxImage.Warning);
                        });

                    }
                    if (trainargs.State == TrainState.OnGoing)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Logs.Add(trainargs.Log);
                            if (log_box.VerticalOffset == log_box.ScrollableHeight)
                            { log_box.ScrollToEnd(); }
                        });

                    }
                    if (trainargs.State == Python.TrainState.Completed)
                    {
                        IsLoading = false;
                        node.AnomalyRuntime.State = ModelState.Unloaded;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Train complete!", "Info", MessageBoxButton.OK, icon: MessageBoxImage.Information);
                        });

                    }
                });
                IsLoading = false;
            }));
        }
    }

}
