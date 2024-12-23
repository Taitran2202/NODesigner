using DevExpress.Xpf.Core;
using DynamicData;
using NodeNetwork.ViewModels;
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
    /// Interaction logic for EditNodeConnectionWindow.xaml
    /// </summary>
    public partial class EditNodeConnectionWindow : ThemedWindow
    {
        BaseNode node;
        public EditNodeConnectionWindow(BaseNode node)
        {
            InitializeComponent();
            lst_inputs.ItemsSource = node.Inputs.Items;
            this.node = node;
            this.Closing += EditNodeConnectionWindow_Closing;
        }

        private void EditNodeConnectionWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Activate();
            }
        }

        private void btn_change_connection_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as NodeInputViewModel;
            if (selected != null)
            {
                NodeOutputSelectionWindow wd = new NodeOutputSelectionWindow(node.designer, selected);
                wd.Owner = Window.GetWindow(this);
                wd.Show();
            }
        }

        private void btn_remove_connection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = (sender as Button).DataContext as NodeInputViewModel;
                if (selected != null)
                {
                    if (selected.Connections.Count > 0)
                    {
                        var connection = selected.Connections.Items.FirstOrDefault();
                        if (connection != null)
                        {
                            selected.Parent.Parent.Connections.Remove(connection);
                        }
                    }

                }
            }catch(Exception ex)
            {

            }
            
        }
    }
}
