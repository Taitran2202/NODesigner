using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.Views;
using ReactiveUI;
using NodeNetwork.ViewModels;

namespace NOVisionDesigner.Designer.GroupNodes
{
    public class GroupInputViewModel<T> : ValueNodeInputViewModel<T>
    {
        static GroupInputViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeInputView(), typeof(IViewFor<GroupInputViewModel<T>>));
        }

        public GroupInputViewModel(PortType type)
        {
            this.Port = new GroupPortViewModel { PortType = type };

            if (type == GroupNodes.PortType.Execution)
            {
                this.PortPosition = PortPosition.Left;
            }
        }
    }
}
