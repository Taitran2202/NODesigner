using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.ViewModel
{
    public class SynchronizationModel: IDisposable
    {
        public object lock_object = new object();
        public List<FrameResult> listContext;
        public SynchronizationModel()
        {
            listContext = new List<FrameResult>();
        }
        
        public void Dispose()
        {
            
        }
    }
    public class FrameResult
    {
        public bool Result { get; set; }
        public ulong FrameID { get; set; }
    }
}
