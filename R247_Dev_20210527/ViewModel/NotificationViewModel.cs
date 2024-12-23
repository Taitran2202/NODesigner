using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;

namespace NOVisionDesigner.ViewModel
{
    [POCOViewModel]
    public class NotificationViewModel
    {
        public virtual string Caption { get; set; }
        public virtual string Content { get; set; }
    }
}
