using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.Views;
using ReactiveUI;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.GroupNodes
{
    public class GroupOutputViewModel <T> : ValueNodeOutputViewModel<T>
    {
        static GroupOutputViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeOutputView(), typeof(IViewFor<GroupOutputViewModel<T>>));
        }

        public GroupOutputViewModel(PortType type)
        {
            this.Port = new GroupPortViewModel { PortType = type };

            if (type == GroupNodes.PortType.Execution)
            {
                this.PortPosition = PortPosition.Left;
            }
        }
    }
}
