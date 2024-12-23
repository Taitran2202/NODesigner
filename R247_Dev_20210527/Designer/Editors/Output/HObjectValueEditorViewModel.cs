using HalconDotNet;
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
    public class HObjectOutputValueEditorViewModel<T>:OutputValueEditorViewModel<T> where T:HObject
    {
        static HObjectOutputValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new HObjectOutputValueEditorView<T>(), typeof(IViewFor<HObjectOutputValueEditorViewModel<T>>));
        }
        public HObjectOutputValueEditorViewModel()
        {
            
        }
    }


}
