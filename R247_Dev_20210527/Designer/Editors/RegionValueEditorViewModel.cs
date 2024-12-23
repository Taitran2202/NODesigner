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
    public class RegionValueEditorViewModel : ValueEditorViewModel<HRegion>,IHalconDeserializable
    {
        public Nodes.CollectionOfregion regions { get; set; } = new Nodes.CollectionOfregion();
        static RegionValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new RegionValueEditorView(), typeof(IViewFor<RegionValueEditorViewModel>));
        }

        public RegionValueEditorViewModel()
        {
            HRegion region = new HRegion();
            region.GenEmptyRegion();
            Value = region;
        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            Value = regions.Region;
            
        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
