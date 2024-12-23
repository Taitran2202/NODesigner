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
    public class StringValueEditorViewModel : ValueEditorViewModel<string>, IHalconDeserializable
    {
        public bool ShowDirectory { get; set; } = false;
        static StringValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new StringValueEditorView(), typeof(IViewFor<StringValueEditorViewModel>));
        }

        public StringValueEditorViewModel(bool ShowDirectory)
        {
            Value = "0";
            this.ShowDirectory = ShowDirectory;
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
