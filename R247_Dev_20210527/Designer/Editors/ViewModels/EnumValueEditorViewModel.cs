using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NOVisionDesigner.Designer.Editors
{
    public class EnumValueEditorViewModel<T>:ValueEditorViewModel<T>, IHalconDeserializable
    {
        static EnumValueEditorViewModel()
        {
            Splat.Locator.CurrentMutable.Register(() => new EnumValueEditorViewGeneric<T>(), typeof(IViewFor<EnumValueEditorViewModel<T>>));
        }
        
        public List<string> EnumList { get; set; }
        public string StringValue 
        { 
            get            
            {
                return Value.ToString();
            }
            set
            {
                if (Value.ToString() != value)
                {
                    Value = (T)Enum.Parse(typeof(T), value);
                }
            } 
        }
        public EnumValueEditorViewModel(T defaultValue)
        {
            EnumList  = Enum.GetValues(typeof(T)).Cast<System.Enum>().Select(x=>x.ToString()).ToList();
            Value = defaultValue;
        }
        public EnumValueEditorViewModel()
        {
            EnumList = Enum.GetValues(typeof(T)).Cast<System.Enum>().Select(x => x.ToString()).ToList();
            Value = default(T);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);

        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
}
