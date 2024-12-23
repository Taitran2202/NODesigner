using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDesigner.Core
{
    //public class Port<T>: PortValue
    //{
    //    public T Data { get; set; }
    //}
    public class PortValue
    {
        /// <summary>
        /// True when value was updated
        /// </summary>
        public bool IsUpdated { get; set; }
        public Node Parrent { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public Type DataType { get; set; }
        public PortValue(Type DataType)
        {
            this.DataType = DataType;
        }
    }
}
