using NodeNetwork.Toolkit.ValueNode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;
using ReactiveUI;

namespace NOVisionDesigner.Designer.Editors
{
    public class HHomMat2DValueEditorViewModel: ValueEditorViewModel<HHomMat2D>
    {
        static HHomMat2DValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new HHomMat2DValueEditorView(), typeof(IViewFor<HHomMat2DValueEditorViewModel>));
        }

        public HHomMat2DValueEditorViewModel()
        {
            Value = new HHomMat2D();
        }
    }
}
