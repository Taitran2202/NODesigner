using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.Native;
using NOVisionDesigner.Designer.Nodes;
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
    public partial class ONNXModelLoaderWindow : ThemedWindow
    {
        ONNXModel model;
        ViewModel viewModel;
        bool Continuous = false;
        public ONNXModelLoaderWindow(ONNXModel model,bool Continuous = false)
        {
            this.model = model;
            InitializeComponent();
            this.DataContext = viewModel = new ViewModel();
            cmb_provider.DataContext = model;
            this.Continuous = Continuous;
        }

        private void btn_load_Click(object sender, RoutedEventArgs e)
        {
           
            viewModel.IsLoading = true;
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    viewModel.Logs.Clear();
                });
                
                
                try
                {
                    var loadResult = false;
                    if (!Continuous)
                    {
                        model.State = ModelState.Unloaded;

                        loadResult = model.LoadRecipe();
                    }
                    else
                    {
                        loadResult = model.ContinuousLoadRecipe();
                    }
                   
                    viewModel.IsLoading = false;
                    if (loadResult)
                    {
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Load model successfully", "Load model", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                    else
                    {
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Cannot load model!", "Load model", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }


                }
                catch (Exception ex)
                {
                    viewModel.IsLoading = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show(this, "Cannot load model!", "Load model", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                viewModel.IsLoading = false;
            });
            return;
            Task.Run(() =>
            {
                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
                while (viewModel.IsLoading)
                {
                    string line;
                    if ((line = Console.ReadLine()) != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            viewModel.Logs.Add(line);
                        });
                    }
                   
                    
                    Thread.Sleep(500);
                }
            });
            


        }
        public class ViewModel:INotifyPropertyChanged
        {
            void RaisePropertyChanged(string prop)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
            }
            public event PropertyChangedEventHandler PropertyChanged;

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
            ObservableCollection<string> _logs = new ObservableCollection<string>();
            public ObservableCollection<string> Logs
            {
                get
                {
                    return _logs;
                }
                set
                {
                    if (_logs != value)
                    {
                        _logs = value;
                        RaisePropertyChanged("Logs");
                    }
                }
            }


        }
    }
    
}
