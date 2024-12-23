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
using System.Windows.Shapes;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for DetailStatisticsWindow.xaml
    /// </summary>
    public partial class DetailStatisticsWindow : Window
    {
        public DetailStatisticsViewModel ViewModel { get; set; }
        public MainWindow main;
        //public int rowIndex { get; set; }
        //public int colIndex { get; set; }
        public DetailStatisticsWindow(UserControls.StatisticsUC StatisticsUC)
        {
            InitializeComponent();
            
            //this.rowIndex = detailStatistics.rowIndex;
            //this.colIndex = detailStatistics.colIndex;
            this.DataContext = ViewModel = new DetailStatisticsViewModel(StatisticsUC);
            
            main = Application.Current.MainWindow as MainWindow;
            this.Owner = main;
        }
    }
}
