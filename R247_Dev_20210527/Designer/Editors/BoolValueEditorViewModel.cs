using NodeNetwork.Toolkit.ValueNode;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Editors
{
    public class BoolValueEditorViewModel : ValueEditorViewModel<bool>
    {
        static BoolValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new BoolValueEditorView(), typeof(IViewFor<BoolValueEditorViewModel>));
        }

        public BoolValueEditorViewModel()
        {
            Value = false;
        }
    }
}
