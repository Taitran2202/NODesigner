using DevExpress.Xpf.Core;
using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for NodeOutputSelectionWindow.xaml
    /// </summary>
    public partial class NodeOutputSelectionWindow : ThemedWindow
    {
        DesignerHost host;
        NodeInputViewModel input;
        public NodeOutputSelectionWindow(DesignerHost host, NodeInputViewModel input)
        {
            InitializeComponent();
            this.host= host;
            this.input = input;
            tv_search.ItemsSource = BuildOutputList(host, input);
            Closing += ContactsWindow_Closing;

        }
        private void ContactsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Owner?.Activate();
        }
        List<NodeInfo> BuildOutputList(DesignerHost host, NodeInputViewModel input)
        {
            List<NodeInfo> lst = new List<NodeInfo>();
            var firstGenericArgumentType = input.GetType().GetGenericArguments().FirstOrDefault();
            var currentConnection = input.Connections.Items.FirstOrDefault();
            
            foreach (var item in host.Network.Nodes.Items)
            {
                if(input.Parent == item)
                {
                    continue;
                }
                ObservableCollection<NodeOutputInfo> Children = new ObservableCollection<NodeOutputInfo>();
                foreach (var output in item.Outputs.Items)
                {
                    var Type = output.GetType().GetGenericArguments().FirstOrDefault();
                    if (firstGenericArgumentType == Type)
                    {
                        var newNodeInfo = new NodeOutputInfo() { Name = output.Name, ID = item.ID };
                        if (currentConnection!=null)
                        {
                           if(currentConnection.Output == output)
                            {
                                newNodeInfo.IsConnected = true;
                            }
                        }
                        Children.Add(newNodeInfo);
                    }
                }
                if (Children.Count > 0)
                {
                    lst.Add(new NodeInfo() { Name = item.Name, ID = item.ID, Members = Children });
                }

            }
            return lst;
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        bool MakeConnection()
        {
            var selected = tv_search.SelectedItem as NodeOutputInfo;
            
            if (selected != null)
            {
                var selectedNode = host.FindNodeByID(selected.ID);
                if (selectedNode != null)
                {
                    var output = selectedNode.Outputs.Items.FirstOrDefault(x => x.Name == selected.Name);
                    if (output != null)
                    {
                        return input.MakeConnection(output);                        
                    }
                }
            }
            return false;
            
        }
        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            if (MakeConnection())
            {
                this.Close();
            }
            else
            {
                DXMessageBox.Show(this,"Cannot make connection","Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
    }
    public class NodeInfo
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public ObservableCollection<NodeOutputInfo> Members { get; set; } = new ObservableCollection<NodeOutputInfo>();
    }
    public class NodeOutputInfo
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public bool IsConnected { get; set; } = false;
    }
}
