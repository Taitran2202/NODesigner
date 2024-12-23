using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    public class NodeInfo: System.Attribute
    {
        public string TypeNode
        {
            get;internal set;
        }
        public string DisplayName
        {
            get;internal set;
        }
        public string Description
        {
            get;internal set;
        }
        public string Icon
        {
            get; internal set;
        }
        public int SortIndex
        {
            get; internal set;
        }

        public bool Visible { get; set; }
        public NodeInfo(string TypeNode,string DisplayName = "", string Description = "", string Icon = "", int sortIndex = 99999,bool visible = true )
        {
            this.SortIndex = sortIndex;
            this.Icon = Icon;
            this.TypeNode = TypeNode;
            this.DisplayName = DisplayName;
            this.Description = Description;
            Visible = visible;
            SortIndex = sortIndex;
        }
    }
}
