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

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for EbusSettingWindow.xaml
    /// </summary>
    public partial class EbusSettingWindow : Window
    {
        public EbusSettingWindow(PvDotNet.PvGenParameterArray model)
        {
            InitializeComponent();
            try
            {
                host.Child = new PvGUIDotNet.PvGenBrowserControl() { GenParameterArray = model };
            }
            catch (Exception ex)
            {

            }

        }
    }
}
