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
    public class BooleanValueEditorViewModel : ValueEditorViewModel<bool>, IHalconDeserializable
    {
        static BooleanValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new BooleanValueEditorView(), typeof(IViewFor<BooleanValueEditorViewModel>));
        }

        public BooleanValueEditorViewModel()
        {
            Value = true;
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
