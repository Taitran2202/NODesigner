using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Processing;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Deep Learning", "OCR Match",visible:false)]
    public class OCRValidationNode : BaseNode
    {
        static OCRValidationNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<OCRValidationNode>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        public ValueNodeInputViewModel<HImage> ImageInput { get; }
        public ValueNodeInputViewModel<List<CharacterResult>> TextInput { get; }
        public ValueNodeOutputViewModel<string[]> Result { get; set; }
        public ObservableCollection<OCRGroupModel> ListModel { get; set; } = new ObservableCollection<OCRGroupModel>();
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    OCRValidationWindow wd = new OCRValidationWindow(this);
                    wd.ShowDialog();
                   
                    break;
            }
        }
        [HMIProperty("OCR Match Window")]
        public IReactiveCommand OpenEditor
        {
            get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        #region Properties
        #endregion
        public override void Run(object context)
        {
            var result = RunInside(ImageInput.Value, TextInput.Value, context as InspectionContext);
            Result.OnNext(result);
        }
        public OCRValidationNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.CanBeRemovedByUser = true;
            this.Name = "OCR Match";
            ImageInput = new ValueNodeInputViewModel<HImage>()
            {
                Name = "Image",
                PortType = "Image"
            };
            this.Inputs.Add(ImageInput);
            TextInput = new ValueNodeInputViewModel<List<CharacterResult>>()
            {
                Name = "TextBox",
                PortType = "CharacterResult"
            };
            this.Inputs.Add(TextInput);
            
            Result = new ValueNodeOutputViewModel<string[]>()
            {
                Name = "Text[]",
                PortType = "string[]"
            };
            this.Outputs.Add(Result);
        }
        (double row1,double col1,double row2,double col2) GroupBox(List<CharacterResult> text, int padding)
        {
            var bbbox = text.Select(x => ViewDisplayMode.BoundingBox(x.Box.row,
                    x.Box.col, x.Box.phi, x.Box.length1, x.Box.length2));
            var row1 = bbbox.Min(x => x.row1) - padding;
            var row2 = bbbox.Max(x => x.row2) + padding;
            var col1 = bbbox.Min(x => x.col1) - padding;
            var col2 = bbbox.Max(x => x.col2) + padding;
            return (row1, col1, row2, col2);
        }
        public List<CharacterResult> FindModel(OCRGroupModel model, List<CharacterResult> text)
        {
            List<CharacterResult> group= null;
            if (model.CharactersBox.Count == 0)
                return group;
            var ModelPoint = model.CharactersBox.Select(x => new Point(x.Box.row, x.Box.col)).ToList();
            var obserPoint = text.Select(x => new Point(x.Box.row, x.Box.col)).ToList();
            var result = RANSAC.Match(ModelPoint, obserPoint);
            if (result != null)
            {

                group = new List<CharacterResult>();
                foreach (var item in result)
                {
                    var index = obserPoint.IndexOf(item);
                    group.Add(text[index]);
                }
            }
            return group;
        }
        public string[] RunInside(HImage image, List<CharacterResult> text, InspectionContext e)
        { 
            List<string> result = new List<string>();
            foreach (var model in ListModel)
            {
                try
                {
                    var matchedText = FindModel(model, text);
                    if (matchedText != null)
                    {
                        foreach (var group in model.GroupValidationList)
                        {
                            //Regex re = new Regex(group.ValidationString);
                            var stringResult = "";
                            List<CharacterResult> MatchedGroup = new List<CharacterResult>();
                            foreach (var item in group.CharacterOrder)
                            {
                                if (item.Index < matchedText.Count)
                                {
                                    if (matchedText[item.Index].Box != null)
                                    {
                                        MatchedGroup.Add(matchedText[item.Index]);
                                        stringResult += matchedText[item.Index].ClassName;
                                    }
                                }



                            }
                            result.Add(stringResult);
                            if (ShowDisplay)
                            {
                                var bbbox = GroupBox(MatchedGroup, 6);
                                e.inspection_result.AddRect1("green", bbbox.row1, bbbox.col1, bbbox.row2, bbbox.col2);
                                e.inspection_result.AddText(stringResult, "black", "#ffffffff", bbbox.row1 - 12, bbbox.col1);
                            }



                        }
                    }
                    else
                    {
                        //result = false;
                    }
                }catch (Exception ex)
                {

                }
                
                
            }
            
            return result.ToArray();
        }
    }
}
