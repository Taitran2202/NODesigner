using DynamicData;
using NodeNetwork.Toolkit.Group;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.GroupNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Misc
{
    public class GroupIOBinding : ValueNodeGroupIOBinding
    {
        public GroupIOBinding(NodeViewModel groupNode, NodeViewModel entranceNode, NodeViewModel exitNode) : base(groupNode, entranceNode, exitNode)
        {
        }

        #region Endpoint Create
        public override ValueNodeOutputViewModel<T> CreateCompatibleOutput<T>(ValueNodeInputViewModel<T> input)
        {
            return new GroupOutputViewModel<T>(PortType.Integer)
            {
                Name = input.Name,
                Editor = new GroupEndpointEditorViewModel<T>(this),
                PortType = input.PortType
            };
        }

        public override ValueNodeOutputViewModel<IObservableList<T>> CreateCompatibleOutput<T>(ValueListNodeInputViewModel<T> input)
        {
            return new GroupOutputViewModel<IObservableList<T>>(((GroupPortViewModel)input.Port).PortType)
            {
                Editor = new GroupEndpointEditorViewModel<IObservableList<T>>(this),
                PortType = input.PortType
            };
        }

        public override ValueNodeInputViewModel<T> CreateCompatibleInput<T>(ValueNodeOutputViewModel<T> output)
        {
            return new GroupInputViewModel<T>(PortType.Integer)
            {
                Name = output.Name,
                Editor = new GroupEndpointEditorViewModel<T>(this),
                HideEditorIfConnected = false,
                PortType = output.PortType
            };
        }

        public override ValueListNodeInputViewModel<T> CreateCompatibleInput<T>(ValueNodeOutputViewModel<IObservableList<T>> output)
        {
            return new GroupListInputViewModel<T>(((GroupPortViewModel)output.Port).PortType)
            {
                Name = output.Name,
                Editor = new GroupEndpointEditorViewModel<T>(this),
                HideEditorIfConnected = false,
                PortType = output.PortType
            };
        }
        #endregion
    }
}
