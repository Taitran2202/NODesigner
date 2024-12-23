using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Editors.Windows;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Input", "Enum")]
    public class InputEnumNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static InputEnumNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<InputEnumNode>));
        }
        [HMIProperty]
        public ValueNodeOutputViewModel<string> Output { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<EnumStringSelection> Selection { get; }
        public ValueNodeInputViewModel<bool> Enable { get; }

        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    StringListEditorWindow wd = new StringListEditorWindow(Selection.Value.Items);
                    wd.Show();
                    break;
            }
        }

        public override void Run(object context)
        {
            if (Selection.Value.SelectedItem() != null)
            {
                Output.OnNext(Selection.Value.SelectedItem().Value);
            }
            else
            {
                Output.OnNext(String.Empty);
            }
            
        }
        public InputEnumNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "String Selection Box";

            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                PortType = "boolean"
            };
            this.Inputs.Add(Enable);

            Selection = new ValueNodeInputViewModel<EnumStringSelection>()
            {
                Name = "Selection",
                PortType = "EnumString",
                Editor = new StringEnumValueEditorViewModel()
            };
            this.Inputs.Add(Selection);


            Output = new ValueNodeOutputViewModel<string>()
            {
                Name = "SelectedItem",
                PortType = "string"
            };
            this.Outputs.Add(Output);
            Enable.ValueChanged.Subscribe(x =>
            {
                if (this.NodeType == NodeType.Event)
                {
                    this.Run(null);
                }
            });
        }



    }
    public class EnumStringSelection: IHalconDeserializable
    {
        public EnumString SelectedItem()
        {
            if(SelectedIndex< Items.Count & SelectedIndex>-1)
                return Items[SelectedIndex];
            else
                return null;

        }
        public int SelectedIndex { get; set; }
        public ObservableCollection<EnumString> Items { get; set; } = new ObservableCollection<EnumString>();

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class EnumString:IHalconDeserializable
    {
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public string Value { get; set; }
    }
}
