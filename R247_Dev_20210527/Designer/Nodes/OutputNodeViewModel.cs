using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace NOVisionDesigner.Designer.Nodes
{
    public class OutputNodeViewModel : NodeViewModel
    {
        static OutputNodeViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<OutputNodeViewModel>));
        }

        public ValueNodeInputViewModel<HImage> ResultInput { get; }

        public OutputNodeViewModel()
        {
            Name = "Output";

            this.CanBeRemovedByUser = false;

            ResultInput = new ValueNodeInputViewModel<HImage>
            {
                Name = "Value",
            };
            ResultInput.ValueChanged.Do((v) =>
            {
                Console.WriteLine(v);
            });
            ResultInput.ValueChanged.Subscribe(x => Console.WriteLine(x));
            this.Inputs.Add(ResultInput);
        }
    }
}
