using NOVisionDesigner.ViewModel;
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
    /// Interaction logic for StatisticsUC.xaml
    /// </summary>
    public partial class StatisticsUC : UserControl
    {
        public StatisticsUCViewModel ViewModel { get; set; }
        public int rowIndex { get; set; }
        public int colIndex { get; set; }

        public StatisticsUC(int rowIndex, int colIndex)
        {
            InitializeComponent();
            this.DataContext = ViewModel = new StatisticsUCViewModel();
            this.rowIndex = rowIndex;
            this.colIndex = colIndex;
        }
    }
}
