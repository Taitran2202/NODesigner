using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Editors
{
    public class DefaultOutputValueEditorViewModel<T>:OutputValueEditorViewModel<T>
    {
        static DefaultOutputValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new DefaultOutputValueEditorView<T>(), typeof(IViewFor<DefaultOutputValueEditorViewModel<T>>));
        }
        public DefaultOutputValueEditorViewModel()
        {
            
        }
    }


}
