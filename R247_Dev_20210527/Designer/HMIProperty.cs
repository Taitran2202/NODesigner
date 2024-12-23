using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer
{
    public class HMIProperty: System.Attribute
    {
        public string Description { get; set; }
        public bool IsCustomClass { get; set; }
        public HMIProperty()
        {

        }
        public HMIProperty(string description)
        {
            this.Description = description;
        }
        public HMIProperty(string description, bool isCustomClass)
        {
            this.IsCustomClass = isCustomClass;
            this.Description = description;
        }
    }
    public class HMIMethod : System.Attribute
    { 
    }
}
