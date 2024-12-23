using DevExpress.Xpf.Core;
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

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for PerformanceSettingView.xaml
    /// </summary>
    public partial class GeneralSettingView : UserControl
    {
        public GeneralSettingView()
        {
            InitializeComponent();
            this.DataContext = App.AppSetting;
        }

        private void btn_select_job_dir_Click(object sender, RoutedEventArgs e)
        {
            if (DXMessageBox.Show(this, "Are you sure to change job directory?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    App.AppSetting.JobDirectory = dialog.SelectedPath;
                    
                }
            }

        }
    }
    public enum DisplayQuality
    {
        Normal,High,Low
    }
}
