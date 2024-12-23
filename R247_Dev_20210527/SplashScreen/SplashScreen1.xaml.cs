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

namespace NOVisionDesigner.SplashScreen
{
    /// <summary>
    /// Interaction logic for SplashScreen1.xaml
    /// </summary>
    public partial class SplashScreen1 : Window, INotifyPropertyChanged
    {
        public SplashScreen1()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChanged("Status");
                }
            }
        }
        public void UpdateStatus(string _status)
        {
            this.Dispatcher.Invoke(new Action(() =>
            this.Status = _status
            ));

            // splash.UpdateLayout();
        }
        public void ShowMessageBox(string message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            MessageBox.Show(this, message)
            ));


        }
        public void CloseSplash()
        {
            this.Dispatcher.BeginInvoke(new Action(() => Close()));
        }
       
    }
}
