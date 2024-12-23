using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Display","Display Graphics")]
    public class DispGraphicsNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static DispGraphicsNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<DispGraphicsNode>));
        }

        public ValueNodeInputViewModel<HImage> Image { get; }
        public ValueNodeInputViewModel<HHomMat2D> Fixture { get; }
        public ObservableCollection<IGraphic> ListGraphics { get; set; } = new ObservableCollection<IGraphic>();
        [HMIProperty]
        public ValueNodeOutputViewModel<double> Mean { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<double> Deviation { get; }

        #region Properties
        [HMIProperty("Graphic Editor")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender));
            }
        }
        #endregion
        
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    var image = Extensions.Functions.GetNoneEmptyHImage(Image);
                    if(image != null)
                    {
                        DispGraphicsEditorWindow wd = new DispGraphicsEditorWindow(ListGraphics, image, Fixture.Value);
                        wd.Owner = Window.GetWindow(sender);
                        wd.Show();
                        
                    }
                    break;

            }
        }
        
        public override void Run(object context)
        {
            //var result = RunInside(Image.Value, Fixture.Value);
            var InspectionContext = context as InspectionContext;
            foreach (IGraphic item in ListGraphics)
            {
                if (item.IsEnabled)
                    item.OnRun(Fixture.Value, InspectionContext);

            }

        }
        public DispGraphicsNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Display graphics";
            Image = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(Image);
            

            Fixture = new ValueNodeInputViewModel<HHomMat2D>()
            {
                Name = "Fixture",
                PortType = "HHomMat2D"
            };
            this.Inputs.Add(Fixture);
        }

        
        
    }

    public interface IGraphic
    {
        string Name { get; set; }
        string Type { get; }
        IGraphic Duplicate();
        bool IsEnabled { get; set; }
        void Initial(int row, int col);
        HDrawingObject CreateDrawingObject(HHomMat2D transform);
        void ClearDrawingData(HWindow display);
        HDrawingObject CurrentDraw { get; set; }
        UpdateGraphicParam UpdateEvent { get; set; }
        void OnResize(HDrawingObject drawid, HWindow window, string type);
        void DrawOnWindow(HWindow window, HHomMat2D transform);
        void OnRun(HHomMat2D transform, InspectionContext e);
        void Edit(object parameters);
        void Remove(object parameters);
        object GetModel();
        void Save(HFile file);
        void Load(DeserializeFactory item);
    }
    public delegate void UpdateGraphicParam(IGraphic sender);
}
