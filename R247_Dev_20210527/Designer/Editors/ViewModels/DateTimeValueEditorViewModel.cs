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
    public class DateTimeValueEditorViewModel : ValueEditorViewModel<DateTime>, IHalconDeserializable
    {
        static DateTimeValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new DateTimeValueEditorView(), typeof(IViewFor<DateTimeValueEditorViewModel>));
        }

        public DateTimeValueEditorViewModel()
        {
            Value = DateTime.Now;
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
