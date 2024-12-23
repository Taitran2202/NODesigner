using DevExpress.Xpf.Core;
using HalconDotNet;
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
using Xceed.Wpf.Toolkit.Primitives;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for PadInspectionView.xaml
    /// </summary>
    public partial class PadInspectionView : UserControl
    {
        PadInspection node;
        public PadInspectionView(PadInspection node)
        {
            InitializeComponent();
            this.DataContext = node;
            this.node = node;
            lst_view.ItemsSource = node.PadDefectTools;
            if (node.PadDefectTools.Count() > 0)
            {
                lst_view.SelectedIndex = 0;
            }
        }

        private void btn_add_tool_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            string name = "blob";
            while (node.PadDefectTools.Where(x => x.ToolName == (name + i.ToString())).Any())
            {
                i++;
            }
            node.PadDefectTools.Add(new PadDefectTool() { ToolName=name+i.ToString()});
        }

        private void btn_edit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (sender as Button).DataContext as PadDefectTool;
            if (selectedItem != null)
            {
                var image=  Extensions.Functions.GetNoneEmptyHImage(node.Image);
                if(image != null)
                {
                    var data = node.GetAnomalyMap(image);
                    if (image != null)
                    {
                        PadDefectWindow wd = new PadDefectWindow(selectedItem, data.map, data.pad, new HHomMat2D());
                        try
                        {
                            wd.Owner = Window.GetWindow(this);
                        }
                        catch(Exception ex)
                        {

                        }
                        wd.Show();
                    }
                }
                
                
            }
        }

        private void btn_edit_region_Click(object sender, RoutedEventArgs e)
        {
            var image = Extensions.Functions.GetNoneEmptyHImage(node.Image);
            if (image != null)
            {
                
                DrawRegionWindowV2 wd = new DrawRegionWindowV2(image, node.Regions, new HHomMat2D());
                
                wd.Owner= Window.GetWindow(this); 
                wd.Show();
            }
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            if(DXMessageBox.Show(this,"Do you want to remove this tool?","Remove tool",
                MessageBoxButton.YesNo,MessageBoxImage.Warning)== MessageBoxResult.Yes)
            {
                var selected = (sender as Button).DataContext as PadDefectTool;
                if (selected != null)
                {
                    node.PadDefectTools.Remove(selected);
                }
            }
            
        }
    }
}
