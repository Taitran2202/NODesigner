using NOVisionDesigner.Designer.Nodes;
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

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for MeasureLineView.xaml
    /// </summary>
    public partial class MeasureLineView : UserControl
    {
        public MeasureLineView(MeasureLinesNode node)
        {
            InitializeComponent();
            this.DataContext = node;
            lst_view.ItemsSource = node.LineMeasures;
            if (node.LineMeasures.Count() > 0)
            {
                lst_view.SelectedIndex = 0;
            }
        }
         
    }
}
