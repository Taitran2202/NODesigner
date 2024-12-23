using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDesigner.Core
{
    public class Node
    {
        public Dictionary<string,PortValue> Inputs { get; set; }
        public Dictionary<string,PortValue> Outputs { get; set; }
        public Dictionary<string,object> OutputData { get; set; }
        public bool IsUpdated { get; set; }
        public void BaseRun()
        {
            if (CheckUpdated())
            {
                IsUpdated = true;
                Run();
                UpdateConnections();
            }
            
        }
        public void UpdateConnections()
        {
            foreach(var item in Outputs)
            {
                item.Value.Value = OutputData[item.Key];
                item.Value.IsUpdated = true;
                item.Value.Parrent.BaseRun();
            }
        }
        public virtual void Run()
        {
            
        }
        private bool CheckUpdated()
        {
            if (Inputs.Count > 0)
            {
                foreach (var item in Inputs)
                {
                    if (!item.Value.IsUpdated)
                        return false;
                }
                return true;
            }
            else
            {
                return true;
            }
            
        }
    }
}
