using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Accquisition
{
    public  class CameraInterfaceBase
    {
        public virtual void OnLoadComplete()
        {

        }
        public  void LoadParam(DeserializeFactory item, object target)
        {
            HelperMethods.LoadParam(item, target);
        }
        public  void SaveParam(HFile file, object target)
        {
            HelperMethods.SaveParam(file, target);
        }
        public  void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
            OnLoadComplete();
        }
        
        public  void Save(HFile file)
        {
            SaveParam(file, this);
        }
        
    }
}
