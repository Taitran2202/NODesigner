using Newtonsoft.Json;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace NOVisionDesigner.Designer.SimpleView
{
    public class NodeBinding:INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        bool _is_editable;
        [JsonIgnore]
        public bool IsEditable
        {
            get
            {
                return _is_editable;
            }
            set
            {
                if (_is_editable != value)
                {
                    _is_editable = value;
                    RaisePropertyChanged("IsEditable");
                }
            }
        }

        [JsonIgnore]
        public NodeViewModel Node { get; set; }
        public string Label { get; set; }
        public string NodeName { get; set; }
        public string NodeID { get; set; }
        
        public List<string> Role { get; set; } = new List<string>();
        public string PropName { get; set; }
        [JsonIgnore]
        public TagManagerModel TagManager { get; set; }
        public int TagIndex { get; set; }
        public NodeBinding(NodeViewModel node)
        {
            this.Node = node;
            this.NodeID = node.ID;
           
        }
        public NodeBinding()
        {
            
        }
    }
}
