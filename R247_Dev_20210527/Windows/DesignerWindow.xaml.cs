using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for DesignerWindow.xaml
    /// </summary>
    public partial class DesignerWindow : ThemedWindow
    {
        public DesignerViewModel ViewModel { get; set; }

        public DesignerWindow(VisionModel visionModel)
        {
            InitializeComponent();
            this.DataContext = ViewModel = new DesignerViewModel(visionModel);
            this.Owner = Application.Current.MainWindow as MainWindow;
            lst_node.DataContext = visionModel.Designer.Network.Nodes.Items;
        }

        private void nodeList_LostFocus(object sender, RoutedEventArgs e)
        {
            //togge_toolview.IsChecked = false;
        }

        private void btn_close_tool_view_Click(object sender, RoutedEventArgs e)
        {
            //togge_toolview.IsChecked = false;
        }

        private void togge_camera_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void togge_toolview_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btn_close_camera_view_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateMousePosition(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(networkView);
            ViewModel.mousePosition = pt;
        }

        private void BarButtonItem_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var item = (sender as BarButtonItem).DataContext as BaseLayoutItem;
            dock.DockController.Restore(item);
        }


        private void btn_export_node_click(object sender, ItemClickEventArgs e)
        {
            var item = (sender as BarButtonItem).DataContext as BaseNode;

            if (item != null)
            {
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.FileName = item.Name;
                saveFileDialog.DefaultExt = ".zip";
                if (saveFileDialog.ShowDialog()== System.Windows.Forms.DialogResult.OK)
                {
                    Task.Run(new Action(() =>
                    {
                        ViewModel.IsLoading= true;
                        ViewModel.LoadingMessage = "Exporting node data, please wait...";
                        try
                        {
                            ZipFile.CreateFromDirectory(item.Dir, saveFileDialog.FileName);
                        }catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        
                        ViewModel.IsLoading = false;
                    }));
                    
                }
                
            }
        }
        public void ExtractZip(string src,string dst)
        {
            using (var zipArchive = ZipFile.OpenRead(src))
            {
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    entry.ExtractToFile(System.IO.Path.Combine(dst,entry.FullName), true);
                }
            };
        }
        private void btn_import_node_click(object sender, ItemClickEventArgs e)
        {
            var item = (sender as BarButtonItem).DataContext as BaseNode;

            if (item != null)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Task.Run(new Action(() =>
                    {
                        ViewModel.IsLoading = true;
                        ViewModel.LoadingMessage = "Importing node data, please wait...";
                        try
                        {
                            if (System.IO.Directory.Exists(item.Dir))
                            {
                                Directory.Delete(item.Dir, true);
                                Directory.CreateDirectory(item.Dir);
                            }
                            ZipFile.ExtractToDirectory(openFileDialog.FileName, item.Dir);
                        } catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message); 
                        }
                        
                        ViewModel.IsLoading = false;
                    }));
                    
                }

            }
        }

        private void BarButtonItem_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            Task.Run(new Action(() =>
            {
                ViewModel.IsLoading = true;
                ViewModel.LoadingMessage = "Saving, please wait...";
                try
                {
                    ViewModel?.Designer?.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                ViewModel.IsLoading = false;
                MessageBox.Show("Save successfully!");
            }));
           
        }

        private void btn_clean_unused_node_Click(object sender, ItemClickEventArgs e)
        {
            ViewModel.Designer.CleanUnusedNodes();
        }

        private void btn_set_3d_click(object sender, ItemClickEventArgs e)
        {
            display.HalconWindow.SetPaint("3d_plot");
        }

        private void btn_set_2d_click(object sender, ItemClickEventArgs e)
        {
            display.HalconWindow.SetPaint("default");
        }

        private void btn_reset_position_Click(object sender, ItemClickEventArgs e)
        {
            ViewModel.Designer.ResetNodePosition();
        }
    }
}
