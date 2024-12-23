using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Camera","Image File",visible:false)]
    public class LoadImagePathNode : BaseNode
    {
        static LoadImagePathNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<LoadImagePathNode>));
        }
        public ValueNodeInputViewModel<string> NameInput { get; }
        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        public override void Run(object context)
        {
            
        }

            public LoadImagePathNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "Load Image";
            NameInput = new ValueNodeInputViewModel<string>(ValueNodeInputViewModel<string>.ValidationAction.PushDefaultValue, ValueNodeInputViewModel<string>.ValidationAction.DontValidate)
            {
                Name = "Name",
                PortType = "String",
                Editor = new StringValueEditorViewModel(true)
                
            };
            this.Inputs.Add(NameInput);

            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
                //Value = this.WhenAnyObservable(vm => vm.NameInput.ValueChanged)
                //    .Select(name =>
                //    {


                //        if (System.IO.File.Exists(name)){

                //            var loaded = new HImage(name);
                //            if (ShowDisplay)
                //            {
                //                designer.display.HalconWindow.DispObj(loaded);
                //            }
                //            return loaded;
                //        }
                //        else
                //        {
                //            return null;
                //        }
                //    })

            };
            this.Outputs.Add(ImageOutput);
            NameInput.ValueChanged.Subscribe((x) =>
            {
                //Task.Run(new Action(() =>
                //{
                    var context = new InspectionContext(null,null, 1, 1,ID);
                    if (System.IO.File.Exists(x))
                    {

                        var loaded = new HImage(x);
                        if (ShowDisplay)
                        {
                            designer.display?.HalconWindow.DispObj(loaded);
                            designer.displayMainWindow?.HalconWindow.DispObj(loaded);
                        }
                    ImageOutput.OnNext(loaded);

                    }
                    else
                    {
                        ImageOutput.OnNext((HImage)null);

                    }

                
                    BaseRun(context);
                    context.inspection_result.Display(designer.display);
                    context.inspection_result.Display(designer.displayMainWindow);

                Reset();
                    if (context.result)
                    {
                        //pass++;
                    }
                    else
                    {
                        context.inspection_result.image = ImageOutput.CurrentValue.CopyImage();
                        designer.recorder.Add(context.inspection_result);
                        //fail++;
                    }
                // }));

            });
           
            
        }
    }
}
