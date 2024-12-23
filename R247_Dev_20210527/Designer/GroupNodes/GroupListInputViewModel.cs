using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
namespace NOVisionDesigner.Designer.GroupNodes
{
    public class GroupListInputViewModel<T> : ValueListNodeInputViewModel<T>
    {
        static GroupListInputViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeInputView(), typeof(IViewFor<GroupListInputViewModel<T>>));
        }

        public GroupListInputViewModel(PortType type)
        {
            this.Port = new GroupPortViewModel { PortType = type };

            if (type == GroupNodes.PortType.Execution)
            {
                this.PortPosition = PortPosition.Right;
            }
        }
    }
}
