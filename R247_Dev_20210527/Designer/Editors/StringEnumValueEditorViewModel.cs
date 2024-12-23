using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NOVisionDesigner.Designer.Editors
{
    public class StringEnumValueEditorViewModel : ValueEditorViewModel<EnumStringSelection>, IHalconDeserializable
    {
        static StringEnumValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new StringEnumValueEditorView(), typeof(IViewFor<StringEnumValueEditorViewModel>));
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }
        public StringEnumValueEditorViewModel()
        {
            Value = new EnumStringSelection();
        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
