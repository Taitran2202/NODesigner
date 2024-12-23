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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for TrainModeSelectionWindow.xaml
    /// </summary>
    public partial class TrainModeSelectionWindow : Window
    {
        public TrainModeSelectionWindow(BaseCheckpointInfo latest, BaseCheckpointInfo best)
        {
            InitializeComponent();
            btn_best.DataContext = best;
            btn_latest.DataContext = latest;
            grid_latest_index.SelectedObject = latest;
            grid_best_index.SelectedObject = best;
            btn_best.Click += Btn_best_Click;
            btn_latest.Click += Btn_latest_Click;
            btn_new.Click += Btn_new_Click;
        }
        public TrainModeSelectionWindow(ClassifierCheckpointInfo best)
        {
            InitializeComponent();
            btn_best.DataContext = best;
            btn_latest.IsEnabled = false;
            btn_best.Click += Btn_best_Click;
            
            btn_new.Click += Btn_new_Click;
        }

        private void Btn_new_Click(object sender, RoutedEventArgs e)
        {
            TrainType = TrainResume.New;
            this.DialogResult = true;
            this.Close();
        }

        private void Btn_latest_Click(object sender, RoutedEventArgs e)
        {
            TrainType = TrainResume.Resume;
            CheckPoint = CheckPointEnum.Latest;
            this.DialogResult = true;
            this.Close();
        }

        private void Btn_best_Click(object sender, RoutedEventArgs e)
        {
            TrainType = TrainResume.Resume;
            CheckPoint = CheckPointEnum.Best;
            this.DialogResult = true;
            this.Close();
        }

        public TrainResume TrainType { get; set; }
        public CheckPointEnum CheckPoint { get; set; }
    }
    public class BaseCheckpointInfo
    {

    }
    public class AnomalyCheckpointInfo:BaseCheckpointInfo
    {
        public int Epoch { get; set; }
        public double AUROCImage { get; set; }
        public double AUROCPixel { get; set; }
        public double AUPROPixel { get; set; }
    }
    public class EdgeDetectionCheckpointInfo : BaseCheckpointInfo
    {
        public int Epoch { get; set; }
        public double Loss { get; set; }
    }
    public class ClassifierCheckpointInfo
    {
        public int Epoch { get; set; }
        public double ValidationAccuracy { get; set; }
        public double ValidationLoss { get; set; }
    }
}
