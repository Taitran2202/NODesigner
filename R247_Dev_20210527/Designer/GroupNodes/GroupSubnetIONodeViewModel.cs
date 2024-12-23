using NodeNetwork.Toolkit.Group.AddEndpointDropPanel;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.GroupNodes
{
    public class GroupSubnetIONodeViewModel : BaseNode
    {
        static GroupSubnetIONodeViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new GroupSubnetIONodeView(), typeof(IViewFor<GroupSubnetIONodeViewModel>));
        }

        public NetworkViewModel Subnet { get; }

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
                AddEndpointDropPanelVM = new AddEndpointDropPanelViewModel(_isEntranceNode, _isExitNode)
                {
                    NodeGroupIOBinding = IOBinding
                };
            }
        }
        private GroupIOBinding _ioBinding;
        #endregion

        public AddEndpointDropPanelViewModel AddEndpointDropPanelVM { get; set; }
        //public string ID { get; set; } = Guid.NewGuid().ToString();

        public readonly bool _isEntranceNode, _isExitNode;

        public GroupSubnetIONodeViewModel(NetworkViewModel subnet, bool isEntranceNode, bool isExitNode) : base(NodeType.Group)
        {
            this.Subnet = subnet;
            _isEntranceNode = isEntranceNode;
            _isExitNode = isExitNode;
            this.ID = Guid.NewGuid().ToString();
        }
        public override void Run(object context)
        {

        }

    }
}

