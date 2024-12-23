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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for RecordImageView.xaml
    /// </summary>
    public partial class RecordImageView : System.Windows.Controls.UserControl
    {
        public RecordImageView(RecordImageNode node)
        {
            InitializeComponent();
            this.DataContext = node.recordImage;
        }

        private void record_folder_click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    (this.DataContext as RecordImageTool).RecordPath = fbd.SelectedPath;

                }
            }
        }
    }
}