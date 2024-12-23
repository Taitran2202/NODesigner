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
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Deeplearning.Windows.PPOCR
{
    /// <summary>
    /// Interaction logic for ModelSelectionWindow.xaml
    /// </summary>
    public partial class ModelSelectionWindow : ThemedWindow
    {
        PPOCRV3TextRecognition runtime;
        public ModelSelectionWindow(PPOCRV3TextRecognition runtime)
        {
            this.runtime = runtime;
            InitializeComponent();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            var item = (PPOCRRecognitionModel)cmb_model.SelectedItem ;
            runtime.CopyModel(item, runtime.ModelDir);
            this.Close();
        }
    }
}
