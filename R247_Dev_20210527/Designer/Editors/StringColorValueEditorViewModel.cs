using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NOVisionDesigner.Designer.Editors
{
    public class StringColorValueEditorViewModel : ValueEditorViewModel<string>
    {
        static StringColorValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new StringColorValueEditorView(), typeof(IViewFor<StringColorValueEditorViewModel>));
        }
        public StringColorValueEditorViewModel(string defaultValue)
        {
            Value = defaultValue;
        }
        public StringColorValueEditorViewModel()
        {
            Value = "#00ff00ff";
        }
    }
}
