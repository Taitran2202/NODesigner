using MoreLinq;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Editors
{
    public interface IGroupEndpointEditorViewModel
    {
        Endpoint Endpoint { get; }
        ReactiveCommand<Unit, Unit> MoveUp { get; }
        ReactiveCommand<Unit, Unit> MoveDown { get; }
        ReactiveCommand<Unit, Unit> Delete { get; }
    }
    public class GroupEndpointEditorViewModel<T> : ValueEditorViewModel<T>, IGroupEndpointEditorViewModel
    {
        public Endpoint Endpoint => Parent;
        public ReactiveCommand<Unit, Unit> MoveUp { get; }
        public ReactiveCommand<Unit, Unit> MoveDown { get; }
        public ReactiveCommand<Unit, Unit> Delete { get; }
        static GroupEndpointEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new GroupEndpointEditorView(), typeof(IViewFor<GroupEndpointEditorViewModel<T>>));
        }
        public GroupEndpointEditorViewModel(GroupIOBinding nodeGroupBinding)
        {
            MoveUp = ReactiveCommand.Create(() =>
            {
                bool isInput = Parent is NodeInputViewModel;
                IEnumerable<Endpoint> endpoints = isInput ? (IEnumerable<Endpoint>)Parent.Parent.Inputs.Items : Parent.Parent.Outputs.Items;

                // Swap SortIndex of this endpoint with the SortIndex of the previous endpoint in the list, if any.
                var prevElement = endpoints
                    .Where(e => e.SortIndex < Parent.SortIndex)
                    .MaxBy(e => e.SortIndex)
                    .FirstOrDefault();
                if (prevElement != null)
                {
                    var idx = prevElement.SortIndex;
                    prevElement.SortIndex = Parent.SortIndex;
                    Parent.SortIndex = idx;
                }
            });
            MoveDown = ReactiveCommand.Create(() =>
            {
                bool isInput = Parent is NodeInputViewModel;
                IEnumerable<Endpoint> endpoints = isInput ? (IEnumerable<Endpoint>)Parent.Parent.Inputs.Items : Parent.Parent.Outputs.Items;

                var nextElement = endpoints
                    .Where(e => e.SortIndex > Parent.SortIndex)
                    .MinBy(e => e.SortIndex)
                    .FirstOrDefault();
                if (nextElement != null)
                {
                    var idx = nextElement.SortIndex;
                    nextElement.SortIndex = Parent.SortIndex;
                    Parent.SortIndex = idx;
                }
            });
            Delete = ReactiveCommand.Create(() =>
            {
                nodeGroupBinding.DeleteEndpoint(Parent);
            });
        }

    }
}
