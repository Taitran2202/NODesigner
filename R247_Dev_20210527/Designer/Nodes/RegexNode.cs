using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows.RegexEditorWindow;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning","Regex",visible:false)]
    public class RegexNode : BaseNode
    {
        static RegexNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<RegexNode>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            new HTuple(regex.Count).SerializeTuple().FwriteSerializedItem(file);
            for (int i = 0; i < regex.Count; i++)
            {
                regex[i].Save(file);
            }
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            int count = item.DeserializeTuple();
            for (int i = 0; i < count; i++)
            {
                RegexItem r = new RegexItem();
                r.Load(item);
                regex.Add(r);
            }
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<HRegion> RegionInput { get; }
        public ValueNodeInputViewModel<HTuple> Text { get; }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    RegexWindow wd = new RegexWindow(ImageInput.Value, RegionInput.Value, Text.Value, regex);
                    wd.ShowDialog();
                    regex = wd.ListRegex;
                    break;
            }
        }
        #region Properties
        public InspectionContext context;
        public ObservableCollection<RegexItem> regex = new ObservableCollection<RegexItem>();
        #endregion
        public override void Run(object context)
        {
            var result = RunInside(ImageInput.Value, RegionInput.Value, Text.Value, context as InspectionContext);
            Result.OnNext(result);
        }
        public RegexNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.CanBeRemovedByUser = true;
            this.Name = "Regex";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);
            RegionInput = new ValueNodeInputViewModel<HRegion>()
            {
                Name = "TextBox",
                PortType = "Region"
            };
            this.Inputs.Add(RegionInput);
            Text = new ValueNodeInputViewModel<HTuple>()
            {
                Name = "Text",
                PortType = "HTuple"
            };
            this.Inputs.Add(Text);
            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Bool"
            };
            this.Outputs.Add(Result);
        }
        public bool RunInside(HImage image, HRegion textbox, HTuple text, InspectionContext e)
        {
            bool result = true;
            if (text != null)
            {
                for (int i = 0; i < text.SArr.Length; i++)
                {
                    Regex r;
                    if (regex.ElementAtOrDefault(i) != null) 
                    {
                        if (regex[i].IsValid) { r = new Regex(regex[i].Pattern); }
                        else
                        {
                            if (image != null && textbox != null)
                            {
                                HRegion box = textbox.SelectObj(i + 1);
                                box.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                                e.inspection_result.AddDisplay(box, "red");
                                e.inspection_result.AddText("Regex Syntax Error", "black", "red", row1, col1);
                            }
                            result = false;
                            continue;
                        }
                    }
                    else { r = new Regex(""); }
                    var match = r.Match(text.SArr[i]);
                    if (match.Success)
                    {
                        if (image != null && textbox != null)
                        {
                            HRegion box = textbox.SelectObj(i+1);
                            box.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                            e.inspection_result.AddDisplay(box, "green");
                            e.inspection_result.AddText(text.SArr[i], "black", "green", row1, col1);
                        }
                        result = true;
                    }
                    else
                    {
                        if (image != null && textbox != null)
                        {
                            HRegion box = textbox.SelectObj(i+1);
                            box.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                            e.inspection_result.AddDisplay(box, "red");
                            e.inspection_result.AddText(text.SArr[i], "black", "red", row1, col1);
                        }
                        result = false;
                    }
                }
            }

            return result;
        }
    }
    public class RegexItem : INotifyPropertyChanged
    {
        public RegexItem(string value)
        {
            _pattern = value;
        }
        public RegexItem() { }
        private string _pattern;
        public string Pattern
        {
            get { return _pattern; }
            set
            {
                if (_pattern != value)
                {
                    _pattern = value;
                    try { new Regex(_pattern).IsMatch(""); IsValid = true; } 
                   catch { IsValid = false; }
                    OnPropertyChanged("Pattern");
                }
            }
        }
        public bool IsValid { get; set; } = true;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
            new HTuple(Pattern).SerializeTuple().FwriteSerializedItem(file);
            new HTuple(IsValid).SerializeTuple().FwriteSerializedItem(file);
        }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
            Pattern = item.DeserializeTuple();
            IsValid = item.DeserializeTuple();
        }
    }
}
