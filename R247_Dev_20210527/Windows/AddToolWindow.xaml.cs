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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for AddToolWindow.xaml
    /// </summary>
    public partial class AddToolWindow : ThemedWindow
    {

        public AddToolWindow()
        {
            InitializeComponent();
        }
        public void SetDesigner(Designer.DesignerHost designer)
        {
            nodeGrid.SetAddNodeFactory(designer.AddNodeFunction);
            btn_add_node.Click += (o, e) =>
            {
                
                NodeViewModel newNodeVm = nodeGrid.GetSelectedNode().AddFunction();
                if (newNodeVm != null)
                {
                   
                    designer.Network.Nodes.Add(newNodeVm);
                }
                //designer.add
            };
        }

       
    }
}
