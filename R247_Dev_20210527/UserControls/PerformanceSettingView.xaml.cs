using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for PerformanceSettingView.xaml
    /// </summary>
    public partial class PerformanceSettingView : UserControl
    {
        public PerformanceSettingView()
        {
            InitializeComponent();
            this.DataContext = App.AppSetting;
            txt_num_core.Text ="This CPU have " +Environment.ProcessorCount.ToString()+" cores.";
        }
    }
}
