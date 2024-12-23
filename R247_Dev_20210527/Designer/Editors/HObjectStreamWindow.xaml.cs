using HalconDotNet;
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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Editors
{
    /// <summary>
    /// Interaction logic for HObjectStreamWindow.xaml
    /// </summary>
    public partial class HObjectStreamWindow : Window
    {
        public HObjectStreamWindow(IObservable<HObject> ValueChanged)
        {
            InitializeComponent();
            ValueChanged.Subscribe((x) =>
            {
                window_display.HalconWindow?.ClearWindow();
                window_display.HalconWindow?.DispObj(x);
            });
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            window_display.HDoubleClickToFitContent = true;
        }
    }
}
