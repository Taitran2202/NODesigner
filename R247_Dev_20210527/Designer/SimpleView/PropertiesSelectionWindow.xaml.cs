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

namespace NOVisionDesigner.Designer.SimpleView
{
    /// <summary>
    /// Interaction logic for PropertiesSelectionWindow.xaml
    /// </summary>
    public partial class PropertiesSelectionWindow : Window
    {
        DesignerHost designerHost;
        public PropertiesSelectionWindow(DesignerHost designerHost)
        {
            InitializeComponent();
            this.designerHost = designerHost;
            cmb_nodes.ItemsSource = designerHost.Network.Nodes.Items;
        }

        private void cmb_props_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count > 0)
            {
                txt_label.Text = e.AddedItems[0].ToString();
            }
            
        }

        private void cmb_nodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = e.AddedItems[0];
            if (item != null)
            {
                var selected = item as NodeViewModel;
                if (selected != null)
                {
                    var proplist = selected.GetType().GetProperties().Where(x=>x.IsDefined(typeof(HMIProperty),false)).Select(x => x.Name);
                    cmb_props.ItemsSource = proplist;
                }
            }
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectednode = cmb_nodes.SelectedItem as NodeViewModel;
                var selectedProp = cmb_props.SelectedItem as string;
                if (selectednode != null & selectedProp != null)
                {
                    var listRole = new List<string>() { ViewModel.UserViewModel.Instance.CurrentUser.Role.Name };


                    designerHost.HMI.BindingList.Add(new NodeBinding(selectednode) { Label = txt_label.Text, NodeName = selectednode.Name, PropName = selectedProp, Role = listRole,IsEditable=true });
                    //designerHost.CreateSimpleView();
                    this.Close();
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
