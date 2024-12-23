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

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for ProgressControl.xaml
    /// </summary>
    public partial class ProgressControl : UserControl
    {
        public ProgressControl()
        {
            InitializeComponent();
        }
        public void SetProgress(double value)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var width = container.ActualWidth;
                var grid_width = Math.Min(width * (value / 100), width);
                grid_progress.Width = grid_width;
                txt_percent.Text = String.Format(value.ToString(), "P");
            }));
            
        }
    }
}
