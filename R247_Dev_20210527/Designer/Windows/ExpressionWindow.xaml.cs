using DevExpress.Data.Filtering;
using DevExpress.Xpf.Editors.Settings;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using NOVisionDesigner.Designer.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for ExpressionWindow.xaml
    /// </summary>
    public partial class ExpressionWindow : Window, INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        object source;
        NodeExpression node;
        public ExpressionWindow(object source,NodeExpression node)
        {
            this.source = source;
            this.node = node;
            InitializeComponent();
            //CriteriaOperator.RegisterCustomFunction(new NOVision.Core.IsNaN());
            //this.DataContext = this;
            //textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            //textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            //textEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit("count"));
            //var context = ExpressionEditorContextHelper.GetContext(true, true, true);
            //context.Columns.Add(new DevExpress.Data.Controls.ExpressionEditor.ColumnInfo("Segmentation variables") { Name = "Count", Description = "Number of segment detected", Type = typeof(int) });
            //context.Columns.Add(new DevExpress.Data.Controls.ExpressionEditor.ColumnInfo("Segmentation variables") { Name = "Area", Description = "Size of segments", Type = typeof(int) });
            //editor.Context = context;
            ////editor.Context.Columns.Add(new DevExpress.Data.Controls.ExpressionEditor.ColumnInfo() { Name = "Area" });
            grid_control.ItemsSource = source;
            grid_control.PopulateColumns();
            grid_control.Columns.Add(new DevExpress.Xpf.Grid.GridColumn() { FieldName = "Result", AllowUnboundExpressionEditor = true, ReadOnly = true, UnboundType = DevExpress.Data.UnboundColumnType.Boolean, UnboundExpression = node.ResultExpression, EditSettings = new TextEditSettings() { DisplayTextConverter = new custConverter(), HorizontalContentAlignment = EditSettingsHorizontalAlignment.Right } });
            grid_control.Columns.Add(new DevExpress.Xpf.Grid.GridColumn() { FieldName = "Message", AllowUnboundExpressionEditor = true, ReadOnly = true, UnboundType = DevExpress.Data.UnboundColumnType.String, UnboundExpression = node.MessageExpression, EditSettings = new TextEditSettings() { HorizontalContentAlignment = EditSettingsHorizontalAlignment.Right } });
            foreach (var column in grid_control.Columns)
            {
                column.AllowColumnFiltering = DevExpress.Utils.DefaultBoolean.False;
                column.ReadOnly = true;
            }




        }
        //CompletionWindow completionWindow;

        //void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        //{
        //    if (e.Text == ".")
        //    {
        //        // Open code completion after the user has pressed dot:
        //        completionWindow = new CompletionWindow(textEditor.TextArea);
        //        IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
        //        data.Add(new MyCompletionData("Item1"));
        //        data.Add(new MyCompletionData("Item2"));
        //        data.Add(new MyCompletionData("Item3"));
        //        completionWindow.Show();
        //        completionWindow.Closed += delegate {
        //            completionWindow = null;
        //        };
        //    }
        //}

        //void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        //{
        //    if (e.Text.Length > 0 && completionWindow != null)
        //    {
        //        if (!char.IsLetterOrDigit(e.Text[0]))
        //        {
        //            // Whenever a non-letter is typed while the completion window is open,
        //            // insert the currently selected element.
        //            completionWindow.CompletionList.RequestInsertion(e);
        //        }
        //    }
        //    // Do not set e.Handled=true.
        //    // We still want to insert the character that was typed.
        //}

        private void btn_run_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            node.ResultExpression = grid_control.Columns["Result"].UnboundExpression;
            node.MessageExpression = grid_control.Columns["Message"].UnboundExpression;
            node.BuildExpression();
            this.Close();
        }



        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnUnboundExpressionEditorCreated(object sender, DevExpress.Xpf.Grid.UnboundExpressionEditorEventArgs e)
        {
            var expressionEditorContext = e.AutoCompleteExpressionEditorControl.Context;


            expressionEditorContext.Columns.Remove(expressionEditorContext.Columns.Where(x => x.Name == "Result").FirstOrDefault());
            expressionEditorContext.Columns.Remove(expressionEditorContext.Columns.Where(x => x.Name == "Message").FirstOrDefault());
            //expressionEditorContext.Constants.Add(new DevExpress.Data.Controls.ExpressionEditor.ConstantInfo() { Name = "NaN" });
        }
    }
    public class custConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                bool b = (bool)value;
                if (b)
                    return "TRUE";
                else
                    return "FALSE";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
    public class ExpressionData
    {
        public int Count { get; set; } = 10;
        public int Area { get; set; } = 20;
        public bool Result { get; set; } = true;
        public string Message { get; set; } = "";
    }
    public class Test
    {
        public int Count { get; set; } = 10;
        public int Area { get; set; } = 20;
    }
    public class MyCompletionData : ICompletionData
    {
        public MyCompletionData(string text)
        {
            this.Text = text;
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get { return "Description for " + this.Text; }
        }

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {

        public ColorizeAvalonEdit(string word)
        {
            Word = word;
        }

        public string Word { get; }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            while ((index = text.IndexOf(Word, start)) >= 0)
            {
                base.ChangeLinePart(
                    lineStartOffset + index, // startOffset
                    lineStartOffset + index + Word.Length, // endOffset
                    (VisualLineElement element) => {
                        // This lambda gets called once for every VisualLineElement
                        // between the specified offsets.
                        Typeface tf = element.TextRunProperties.Typeface;
                        // Replace the typeface with a modified version of
                        // the same typeface
                        element.TextRunProperties.SetForegroundBrush(Brushes.Red);
                        element.TextRunProperties.SetTypeface(new Typeface(
                                tf.FontFamily,
                                FontStyles.Italic,
                                FontWeights.Bold,
                                tf.Stretch
                            ));
                    });
                start = index + 1; // search for next occurrence
            }
        }
    }

}
