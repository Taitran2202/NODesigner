using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Editors
{
    public class DoubleValueEditorViewModel : ValueEditorViewModel<double>,IHalconDeserializable
    {
        static DoubleValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new DoubleValueEditorView(), typeof(IViewFor<DoubleValueEditorViewModel>));
        }
        public double DefaultStep { get; set; }
        public DoubleValueEditorViewModel(double defaultValue = 1,double defaultStep =1)
        {
            Value = defaultValue;
            DefaultStep = defaultStep;
        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
