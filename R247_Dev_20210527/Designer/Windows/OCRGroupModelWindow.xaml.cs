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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for OCRGroupModelWindow.xaml
    /// </summary>
    public partial class OCRGroupModelWindow : Window
    {
        OCRGroupModel group;
        public OCRGroupModelWindow(OCRGroupModel group)
        {
            InitializeComponent();
            this.group = group;
            //diagram.Model = group;
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            diagram.Model=group;
        }
    }
}
