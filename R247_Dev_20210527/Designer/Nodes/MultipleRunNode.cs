using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Multiple Run", "Multitple Run")]
    public class MultipleRunNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static MultipleRunNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MultipleRunNode>));
        }
        [HMIProperty]
        public ValueNodeInputViewModel<HRegion> InputRegions { get; }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }

        public ValueNodeOutputViewModel<HImage> ImageOutput { get; }
        [HMIProperty("Open Editor")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        }
        public MultipleRunNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Multiple Run Control";
            this.CanBeRemovedByUser = true;
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);
            ImageOutput = new ValueNodeOutputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Outputs.Add(ImageOutput);
            InputRegions = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "Input Regions",
                PortType = "HRegion",
                Editor = new Editors.RegionValueEditorViewModel()
            };
            this.Inputs.Add(InputRegions);
            
        }

        public override void Run(object context)
        {
            var regions =InputRegions.Value;
            var image = ImageInput.Value;
            if (regions != null)
            {
                var connection = regions;
                for(int i = 1; i < connection.CountObj()+1; i++)
                {
                    
                    connection[i].SmallestRectangle1(out int row1,out int col1,out int row2,out int col2);
                    if (row1 < 0)
                    {
                        row1 = 0;
                    }
                    if(col1 < 0)
                    {
                        col1 = 0;
                    }
                    var imageCroped = image.CropRectangle1(row1,col1,row2,col2);
                    HHomMat2D tranform = new HHomMat2D();
                    tranform = tranform.HomMat2dTranslate((double)row1, col1);
                    (context as InspectionContext).inspection_result.Transform = null;
                    (context as InspectionContext).inspection_result.ImageID = i-1;
                    if (ShowDisplay)
                    {
                        (context as InspectionContext).inspection_result.AddRect1("green", row1, col1, row2, col2);
                    }
                    (context as InspectionContext).inspection_result.Transform = tranform;
                    if (ShowDisplay)
                    {
                        
                        (context as InspectionContext).inspection_result.AddText(i.ToString(), "black", "#ffffffff", 10, 10);
                    }
                    ImageOutput.OnNext(imageCroped);
                    if(i>1 & i== connection.CountObj())
                    {
                        return;
                    }
                    UpdateConnections(context);
                    Reset();
                }
                
            }
            else
            {
                ImageOutput.OnNext(image);
            }
            
            
        }

        
    }
}
