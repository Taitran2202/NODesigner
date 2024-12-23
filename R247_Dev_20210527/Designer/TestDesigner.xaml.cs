using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.NodeList;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Nodes;
using ReactiveUI;
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

namespace NOVisionDesigner.Designer
{
    /// <summary>
    /// Interaction logic for TestDesigner.xaml
    /// </summary>
    public partial class TestDesigner : Window
    {
        public NodeListViewModel ListViewModel { get; } = new NodeListViewModel();
        public NetworkViewModel network { get; } = new NetworkViewModel();
        public TestDesigner()
        {
            InitializeComponent();
            
            
            nodeList.ViewModel = ListViewModel;
            // Create a new viewmodel for the NetworkView
            DesignerHost designer = new DesignerHost(Workspace.WorkspaceManager.Instance.AppDataPath );
            foreach (var item in designer.AddNodeFunction)
            {
                ListViewModel.AddNodeType(item.AddFunction);
            }
            designer.AddNode("LoadImagePathNode");
            var displayNode=designer.AddNode("HImageOutputNodeViewModel") as DisplayImage;
           
            networkView.ViewModel = designer.Network;
            
            //designer.SetDisplay(display);
        }

       
    }
}
