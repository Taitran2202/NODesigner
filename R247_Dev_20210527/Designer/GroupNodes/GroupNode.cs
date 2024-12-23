using HalconDotNet;
using NodeNetwork.Toolkit.Group.AddEndpointDropPanel;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.GroupNodes;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.GroupNodes
{
    public class GroupNode : BaseNode
    {
        static GroupNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new GroupNodeView(), typeof(IViewFor<GroupNode>));
        }
        NetworkViewModel _subnet;
        public NetworkViewModel Subnet
        {
            get
            {
                return _subnet;
            }
            set
            {
                if (_subnet != value)
                {
                    _subnet = value;
                }
            }
        }
        #region IOBinding
        public GroupIOBinding IOBinding
        {
            get => _ioBinding;
            set
            {
                if (_ioBinding != null)
                {
                    throw new InvalidOperationException("IOBinding is already set.");
                }
                _ioBinding = value;
                AddEndpointDropPanelVM = new AddEndpointDropPanelViewModel
                {
                    NodeGroupIOBinding = IOBinding
                };
            }
        }
        private GroupIOBinding _ioBinding;
        #endregion
        public AddEndpointDropPanelViewModel AddEndpointDropPanelVM { get; private set; }
        public override void Run(object context)
        {
            bool isInputHasValue = SetValueInputNode();
            if (!isInputHasValue) { return; }
            foreach (var item in this.IOBinding.EntranceNode.Outputs.Items)
            {
                item.IsUpdated = true;
                foreach (var item2 in item.Connections.Items)
                {
                    item2.Input.IsUpdated = true;
                    //item2.Input.Value
                    item2.Input.Parent.BaseRun(context);
                }
            }
            SetValueOutputNode();
        }
        public GroupNode(NetworkViewModel subnet) : base(NodeType.Group)
        {
            this.Name = "Group";
            this.Subnet = subnet;
            this.ID = Guid.NewGuid().ToString();
        }

        public GroupNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Group";
        }

        #region Method
        public bool SetValueInputNode()
        {
            for (int i = 0; i < this.IOBinding.EntranceNode.Outputs.Items.Count(); i++)
            {
                var target = this.Inputs.Items.ElementAt(i);
                PropertyInfo[] infos = target.GetType().GetProperties();
                foreach (PropertyInfo info in infos)
                {
                    string param_name = info.Name;
                    object value = info.GetValue(target);
                    if (value == null)
                        continue;

                    Type type = value.GetType();
                    if (type == typeof(HImage))
                    {
                        var enntranceNode = this.IOBinding.EntranceNode.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HImage>;
                        var inputNode = this.Inputs.Items.ElementAt(i) as GroupInputViewModel<HImage>;
                        enntranceNode.OnNext(inputNode.Value);
                        return inputNode.Value != null;
                    }
                    if (type == typeof(HRegion))
                    {
                        var enntranceNode = this.IOBinding.EntranceNode.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HRegion>;
                        var inputNode = this.Inputs.Items.ElementAt(i) as GroupInputViewModel<HRegion>;
                        enntranceNode.OnNext(inputNode.Value);
                        return inputNode.Value != null;
                    }
                    if (type == typeof(HHomMat2D))
                    {
                        var enntranceNode = this.IOBinding.EntranceNode.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HHomMat2D>;
                        var inputNode = this.Inputs.Items.ElementAt(i) as GroupInputViewModel<HHomMat2D>;
                        enntranceNode.OnNext(inputNode.Value);
                        return inputNode.Value != null;
                    }
                    //if (type == typeof(bool))
                    //{
                    //    try
                    //    {
                    //        var enntranceNode = this.IOBinding.EntranceNode.Outputs.Items.ElementAt(i) as GroupOutputViewModel<bool>;
                    //        var inputNode = this.Inputs.Items.ElementAt(i) as GroupInputViewModel<bool>;
                    //        enntranceNode.OnNext(inputNode.Value);
                    //    }
                    //    catch (Exception)
                    //    {

                    //        return;
                    //    }

                    //    return;
                    //}
                }
            }
            return false;
        }

        public void SetValueOutputNode()
        {
            for (int i = 0; i < this.IOBinding.ExitNode.Inputs.Items.Count(); i++)
            {
                var target = this.IOBinding.ExitNode.Inputs.Items.ElementAt(i);
                PropertyInfo[] infos = target.GetType().GetProperties();
                foreach (PropertyInfo info in infos)
                {
                    string param_name = info.Name;
                    object value = info.GetValue(target);
                    if (value == null)
                        continue;
                    if(param_name != "Value") { continue; }
                    Type type = value.GetType();
                    if (type == typeof(HImage))
                    {
                        var exitNode = this.IOBinding.ExitNode.Inputs.Items.ElementAt(i) as GroupInputViewModel<HImage>;
                        var outputNode = this.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HImage>;
                        outputNode.OnNext(exitNode.Value);
                        continue;
                    }
                    if (type == typeof(HRegion))
                    {
                        var exitNode = this.IOBinding.ExitNode.Inputs.Items.ElementAt(i) as GroupInputViewModel<HRegion>;
                        var outputNode = this.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HRegion>;
                        outputNode.OnNext(exitNode.Value);
                        continue;
                    }
                    if (type == typeof(HHomMat2D))
                    {
                        var exitNode = this.IOBinding.ExitNode.Inputs.Items.ElementAt(i) as GroupInputViewModel<HHomMat2D>;
                        var outputNode = this.Outputs.Items.ElementAt(i) as GroupOutputViewModel<HHomMat2D>;
                        outputNode.OnNext(exitNode.Value);
                        continue;
                    }
                    if (type == typeof(bool))
                    {
                        var exitNode = this.IOBinding.ExitNode.Inputs.Items.ElementAt(i) as GroupInputViewModel<bool>;
                        var outputNode = this.Outputs.Items.ElementAt(i) as GroupOutputViewModel<bool>;
                        outputNode.OnNext(exitNode.Value);
                        continue;
                    }
                }
            }

        }
        #endregion
    }
}
#region old
//Type t1 = Type.GetType("NOVisionDesigner.Designer.Nodes." + "GroupNode");
//Type t2 = Type.GetType("NOVisionDesigner.Designer.Nodes." + "GroupSubnetInputNode");
//Type t3 = Type.GetType("NOVisionDesigner.Designer.Nodes." + "GroupSubnetOutputNode");
//this.WhenAnyValue(vm => vm.NetworkBreadcrumbBar.ActiveItem).Cast<NetworkBreadcrumb>()
//    .Select(b => b?.Network)
//    .ToProperty(this, vm => vm.designer.Network, out designer._network);
//NetworkBreadcrumbBar.ActivePath.Add(new NetworkBreadcrumb
//{
//    Name = "Main",
//    Network = new NetworkViewModel()
//});
//var grouper = new NodeGrouper
//{
//    GroupNodeFactory = (_) => Activator.CreateInstance(t1, new object[] { designer, designer.BaseDir, Guid.NewGuid().ToString()}) as NodeViewModel,
//    EntranceNodeFactory = () => Activator.CreateInstance(t2, new object[] { designer, designer.BaseDir, Guid.NewGuid().ToString() }) as NodeViewModel,
//    ExitNodeFactory = () => Activator.CreateInstance(t3, new object[] { designer, designer.BaseDir, Guid.NewGuid().ToString() }) as NodeViewModel,
//    SubNetworkFactory = () => new NetworkViewModel(),
//    IOBindingFactory = (groupNode, entranceNode, exitNode) =>
//        new GroupIOBinding(groupNode, entranceNode, exitNode)
//};
//var isGroupNodeSelected = this.WhenAnyValue(vm => vm.designer.Network)
//    .Select(net => net.SelectedNodes.Connect())
//    .Switch()
//    .Select(_ => designer.Network.SelectedNodes.Count == 1 && designer.Network.SelectedNodes.Items.First() is GroupNode);
#endregion