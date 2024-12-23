using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
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
    public partial class DefectBlobView : UserControl
    {
        DefectBlobNode node;
        public DefectBlobView(DefectBlobNode node)
        {
            InitializeComponent();
            this.DataContext = node;
            this.node = node;
            lst_view.ItemsSource = node.ToolList;
            if (node.ToolList.Count() > 0)
            {
                lst_view.SelectedIndex = 0;
            }
        }

        private void btn_add_tool_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            string name = "blob";
            while (node.ToolList.Where(x => x.ToolName == (name + i.ToString())).Any())
            {
                i++;
            }
            node.ToolList.Add(new DefectBlobTool() { ToolName=name+i.ToString()});
        }

        private void btn_edit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (sender as Button).DataContext as DefectBlobTool;
            if (selectedItem != null)
            {
                var image=  Extensions.Functions.GetNoneEmptyHImage(node.ImageInput);
                if(image!= null)
                {
                    BlobDefectWindow wd = new BlobDefectWindow(selectedItem, image, node.FixtureInput.Value);
                    wd.Show();
                }
                
            }
        }
    }
}
