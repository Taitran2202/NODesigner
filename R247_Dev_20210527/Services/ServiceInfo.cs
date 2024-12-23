using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Services
{
    public class ServiceInfo : System.Attribute
    {
        public string Catergory
        {
            get; internal set;
        }
        public string DisplayName
        {
            get; internal set;
        }
        public string Description
        {
            get; internal set;
        }
        public bool Visible { get; set; }
        public ServiceInfo(string Catergory, string DisplayName = "", string Description = "", bool visible = true)
        {
            this.Catergory = Catergory;
            this.DisplayName = DisplayName;
            this.Description = Description;
            Visible = visible;
        }
    }
}
