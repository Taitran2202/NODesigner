using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer
{
    public interface IHalconDeserializable
    {
        void Load(DeserializeFactory item);
        void Save(HFile file);
    }
}
