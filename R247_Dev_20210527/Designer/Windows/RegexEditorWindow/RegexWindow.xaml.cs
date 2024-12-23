using DevExpress.Xpf.Editors;
using HalconDotNet;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Windows.RegexEditorWindow
{
    /// <summary>
    /// Interaction logic for RegexWindow.xaml
    /// </summary>
    public partial class RegexWindow : Window
    {
        public RegexWindow(HImage image, HRegion textbox, HTuple text, ObservableCollection<RegexItem> regex_list)
        {
            InitializeComponent();
            this.image = image;
            this.textbox = textbox;
            this.text = text;
            ListRegex = regex_list;
            lst_regex.ItemsSource = ListRegex;
            window_display.HMoveContent = true;
            _result = new ObservableCollection<RegexResult>();
            _refItemsDisplay = new ObservableCollection<ReferenceItem>();
            _refTypesDisplay = new ObservableCollection<ReferenceType>();
            lst_ref_type.ItemsSource = RefTypesDisplay;
            lst_ref.ItemsSource = RefItemsDisplay;
            LoadQuickRef();
        }
        HWindow display;
        HImage image;
        HRegion textbox;
        HTuple text;
        List<ReferenceItem> refItems = QuickRefItems.ReferenceItems;
        List<ReferenceItem> refItemsSearch = new List<ReferenceItem>();
        List<ReferenceType> refTypes = QuickRefItems.ReferenceTypes;
        ObservableCollection<ReferenceItem> _refItemsDisplay;
        public ObservableCollection<ReferenceItem> RefItemsDisplay
        {
            get
            {
                return _refItemsDisplay;
            }
            set
            {
                if (_refItemsDisplay != value)
                {
                    _refItemsDisplay = value;
                    RaisePropertyChanged("RefItemsDisplay");
                }
            }
        }
        ObservableCollection<ReferenceType> _refTypesDisplay;
        public ObservableCollection<ReferenceType> RefTypesDisplay
        {
            get
            {
                return _refTypesDisplay;
            }
            set
            {
                if (_refTypesDisplay != value)
                {
                    _refTypesDisplay = value;
                    RaisePropertyChanged("RefTypesDisplay");
                }
            }
        }
        ObservableCollection<RegexResult> _result;
        public ObservableCollection<RegexResult> Result
        {
            get
            {
                return _result;
            }
            set
            {
                if (_result != value)
                {
                    _result = value;
                    RaisePropertyChanged("Result");
                }
            }
        }
        ObservableCollection<RegexItem> _list_regex;
        public ObservableCollection<RegexItem> ListRegex
        {
            get
            {
                return _list_regex;
            }
            set
            {
                if (_list_regex != value)
                {
                    _list_regex = value;
                    RaisePropertyChanged("ListRegex");
                }
            }
        }
        
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            if (image != null)
            {
                window_display.HalconWindow.AttachBackgroundToWindow(image);
            }
            if (text != null)
            {
                for (int i = 0; i < text.SArr.Count(); i++)
                {
                    Run(i);
                }
                UpdateDisplay();
            }
        }
        private void Run(int selected_index)
        {
            if (text != null)
            {
                if (text.SArr.ElementAtOrDefault(selected_index) != null)
                {
                    HRegion box = textbox.SelectObj(selected_index + 1);
                    Regex r;
                    if (ListRegex.ElementAtOrDefault(selected_index) != null)
                    {
                        if (ListRegex[selected_index].IsValid)
                        {
                            r = new Regex(ListRegex[selected_index].Pattern);
                        }
                        else
                        {
                            if (Result.ElementAtOrDefault(selected_index) == null)
                            {
                                Result.Add(new RegexResult() { Message = "Regex Syntax Error", Box = box, Status = "Error" });
                            }
                            else
                            {
                                Result[selected_index] = new RegexResult() { Message = "Regex Syntax Error", Box = box, Status = "Error", MatchResults = Result[selected_index].MatchResults };
                                Result[selected_index].MatchResults.Clear();
                            }
                            return;
                        }

                    }
                    else { r = new Regex(""); }
                    var match = r.Matches(text.SArr[selected_index]);
                    if (match.Count!=0)
                    {
                        var match_result_list = match.Cast<Match>().ToList();
                        ObservableCollection<MatchResult> match_result = new ObservableCollection<MatchResult>(match_result_list.Select(x => new MatchResult() { Index = x.Index.ToString(), Length = x.Length.ToString(), Text = x.Value, BackColor= match_result_list.IndexOf(x)%2==0? "#d5ebff": "#9fcfff" }));

                        if (Result.ElementAtOrDefault(selected_index) == null)
                        {
                            Result.Add(new RegexResult() { Message = text.SArr[selected_index], Box = box, Status = "Success", MatchResults = match_result });
                        }
                        else
                        {
                            Result[selected_index] = new RegexResult() { Message = text.SArr[selected_index], Box = box, Status = "Success", MatchResults = Result[selected_index].MatchResults };
                            if (Result[selected_index].MatchResults.Count > 0)
                            {
                                Result[selected_index].MatchResults.Clear();
                            }
                            foreach (var x in match_result_list)
                            {
                              Result[selected_index].MatchResults.Add(new MatchResult() { Index = x.Index.ToString(), Length = x.Length.ToString(), Text = x.Value, BackColor=match_result_list.IndexOf(x)%2==0? "#d5ebff" : "#9fcfff" });
                            }
                            //Result[selected_index].MatchResults = match.Cast<Match>().Select(x => new MatchResult() { Index = x.Index.ToString(), Length = x.Length.ToString(), Text = x.Value }) as ObservableCollection<MatchResult>;
                        }

                        //Result[selected_index].MatchResults[0].Text = "ga` xe'";
                    }
                    else
                    {
                        if (Result.ElementAtOrDefault(selected_index) == null)
                        {
                            Result.Add(new RegexResult() { Message = text.SArr[selected_index], Box = box, Status = "Fail" });
                        }
                        else
                        {
                            Result[selected_index] = new RegexResult() { Message = text.SArr[selected_index], Box = box, Status = "Fail", MatchResults = Result[selected_index].MatchResults };
                            Result[selected_index].MatchResults.Clear();
                        }
                    }
                }
            }
        }

        private void UpdateDisplay()
        {
            if (display == null)
            {
                return;
            }
            display.ClearWindow();
            if (!Result.Any()) { return; }
            foreach (RegexResult item in Result)
            {
                HTuple box_color;
                item.Box.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                if (item.Status == "Success")
                {
                    display.SetColor("green");
                    box_color = new HTuple("green");
                }
                else
                {
                    display.SetColor("red");
                    box_color = new HTuple("red");
                }
                if (Result.IndexOf(item) == lst_regex.SelectedIndex) { display.SetColor("blue"); }
                display.SetDraw("margin");
                display.DispRegion(item.Box);
                display.DispText(item.Message, "image", row1, col1, "black", new HTuple("box_color"), box_color);
            }
            if (lst_regex.SelectedIndex == -1) { return; }
            var p = rtb_match.Document.Blocks.OfType<Paragraph>().Where(x => x.Name == "prgp_match").First();
            p.Inlines.Clear();
            var selected_result = Result.ElementAtOrDefault(lst_regex.SelectedIndex);
            var result_list = selected_result.MatchResults.GetEnumerator();
            result_list.MoveNext();
            for (int i = 0; i < selected_result.Message.Length; i++)
            {
                Run txt = new Run();
                txt.Text = selected_result.Message[i].ToString();
                if (result_list.Current != null)
                {
                    if (Int32.Parse(result_list.Current.Index) == i)
                    {
                        txt.Text = result_list.Current.Text;
                        txt.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(result_list.Current.BackColor);
                        i += Int32.Parse(result_list.Current.Length) - 1;
                        result_list.MoveNext();
                    }
                }
                p.Inlines.Add(txt);
            }
        }
        private void Lst_regex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Result.ElementAtOrDefault(lst_regex.SelectedIndex) != null)
            {
                Binding binding = new Binding
                {
                    Source = Result.ElementAtOrDefault(lst_regex.SelectedIndex).MatchResults,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                lst_result.SetBinding(ListView.ItemsSourceProperty, binding);
                
            }
            UpdateDisplay();
        }

        private void Btn_add_regex_Click(object sender, RoutedEventArgs e)
        {
            ListRegex.Add(new RegexItem(""));
            int selected = ListRegex.Count - 1;
            if (text != null)
            {
                if (selected < text.SArr.Length)
                {
                    Run(selected);
                    UpdateDisplay();
                }
            }
        }

        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            RegexItem selected = (sender as TextBox).DataContext as RegexItem;
            int index = ListRegex.IndexOf(selected);
            Run(index);
            UpdateDisplay();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RegexItem selected = (sender as Button).DataContext as RegexItem;
            int index = ListRegex.IndexOf(selected);
            if (selected != null) 
            {
                if (ListRegex.ElementAtOrDefault(index)!=null)
                {
                    ListRegex.RemoveAt(index);
                }
                if (text != null)
                {
                    for (int i = 0; i < text.SArr.Length; i++)
                    {
                        Run(i);
                    }
                    UpdateDisplay();
                }
                
            }
            if (index != 0)
            {
                lst_regex.SelectedItem = index - 1;
            }
        }

        private void UpdateHeight()
        {
            if (xpd_regex_list == null || xpd_match_result == null || xpd_quick_ref == null) { return; }
            int count = 0;
            double total_height = (stack_workspace.ActualHeight != 0 ? stack_workspace.ActualHeight : stack_workspace.Height) - 24 * 3 + 5;
            if (xpd_regex_list.IsExpanded) { count++; }
            if (xpd_match_result.IsExpanded) { count++; }
            if (xpd_quick_ref.IsExpanded) { count++; }
            xpd_regex_list.Height = 18;
            xpd_match_result.Height = 18;
            xpd_quick_ref.Height = 18;
            if (count != 0)
            {
                xpd_regex_list.Height += xpd_regex_list.IsExpanded ? total_height / count : 0;
                xpd_match_result.Height += xpd_match_result.IsExpanded ? total_height / count : 0;
                xpd_quick_ref.Height += xpd_quick_ref.IsExpanded ? total_height / count : 0;
            }
        }

        private void xpd_regex_list_Expanded(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }

        private void xpd_regex_list_Collapsed(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }

        private void xpd_match_result_Collapsed(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }

        private void xpd_match_result_Expanded(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }

        private void xpd_quick_ref_Collapsed(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }

        private void xpd_quick_ref_Expanded(object sender, RoutedEventArgs e)
        {
            UpdateHeight();
        }
        private void LoadQuickRef()
        {
            //foreach (var item in refItems)
            //{
            //    RefItemsDisplay.Add(item);
            //    refItemsSearch.Add(item);
            //}
            //RefTypesDisplay.Add(refTypes.Find(x => x.Name == "All Tokens"));
            //var types = RefItemsDisplay.Select(x => x.Type);
            //foreach (var item in types)
            //{
            //    if (!RefTypesDisplay.Contains(refTypes.Find(x => x.Name == item)))
            //    {
            //        RefTypesDisplay.Add(refTypes.Find(x => x.Name == item));
            //    }
            //}
            //lst_ref_type.SelectedIndex = 0;
            //tb_search_TextChanged(null, null);
            txt_search_EditValueChanged(null, null);
        }

        private void lst_ref_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_ref_type.SelectedItem != null)
            {
                RefItemsDisplay.Clear();
                ReferenceType selected = (lst_ref_type.SelectedItem as ReferenceType);
                if ( selected.Name == "All Tokens"||selected.Name=="Full Search Result")
                {                  
                    foreach (var item in refItemsSearch)
                    {
                        RefItemsDisplay.Add(item);
                    }
                }
                else
                {
                    var items = refItemsSearch.Where(x => (selected.IsSubType ? x.SubType : x.Type) == selected.Name);
                    foreach(var item in items)
                    {
                        RefItemsDisplay.Add(item);
                    }
                }
            }
            
        }

        //private void tb_search_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    var selected_type = lst_ref_type.SelectedItem;
        //    var items = refItems.Where(x => x.Title.ToLower().Contains(tb_search.Text.ToLower()));
        //    RefItemsDisplay.Clear();
        //    RefTypesDisplay.Clear();
        //    refItemsSearch.Clear();
        //    if (items.Any())
        //    {
        //        foreach(var item in items)
        //        {
        //            RefItemsDisplay.Add(item);
        //            refItemsSearch.Add(item);
        //        }

        //        var types = RefItemsDisplay.Where(x => x.Type != null).Select(x => x.Type);
        //        var subtypes = RefItemsDisplay.Where(x => x.SubType!=null).Select(x=>x.SubType);
        //        if (types.Any())
        //        {
        //            RefTypesDisplay.Add(refTypes.Find(x => x.Name == (tb_search.Text == "" ? "All Tokens" : "Full Search Result")));
        //            if (subtypes.Any())
        //            {
        //                foreach(var item in subtypes)
        //                {
        //                    if (!RefTypesDisplay.Contains(refTypes.Find(x => x.Name == item)))
        //                    {
        //                        RefTypesDisplay.Add(refTypes.Find(x => x.Name == item));
        //                    }
        //                }
        //            }
        //            foreach (var item in types)
        //            {
        //                if(!RefTypesDisplay.Contains(refTypes.Find(x => x.Name == item)))
        //                {
        //                    RefTypesDisplay.Add(refTypes.Find(x => x.Name == item));
        //                }
        //            }
        //            if (RefTypesDisplay.Contains(selected_type))
        //            {
        //                lst_ref_type.SelectedItem = selected_type;
        //            }
        //            else
        //            {
        //                lst_ref_type.SelectedIndex = 0;
        //            }
        //        }
        //    }
        //}
        private void Btn_close_ref_Click(object sender, RoutedEventArgs e)
        {
            stp_ref_item.Visibility = Visibility.Hidden;
            grid_home_quick_ref.Visibility = Visibility.Visible;
            lst_ref.SelectedIndex = -1;
        }

        private void btn_item_Click(object sender, RoutedEventArgs e)
        {
            grid_home_quick_ref.Visibility = Visibility.Hidden;
            stp_ref_item.Visibility = Visibility.Visible;
            var item = (sender as Button).DataContext as ReferenceItem;
            lst_ref.SelectedItem = item;
            var p = rtb_example_string.Document.Blocks.OfType<Paragraph>().Where(x => x.Name == "prgp_example_string").First();
            p.Inlines.Clear();
            if (item.ExamplePattern == "") { return; }
            try
            {
                var result = new Regex(item.ExamplePattern).Matches(item.ExampleString);
                Match[] result_arr = new Match[result.Count];
                result.CopyTo(result_arr, 0);
                var result_list = result_arr.ToList();
                bool swap = true;
                for (int i = 0; i < item.ExampleString.Length; i++)
                {
                    Run txt = new Run();
                    txt.Text = item.ExampleString[i].ToString();
                    if (result_list.Count>0)
                    {
                        if (result_list[0].Index == i)
                        {
                            txt.Text = result_list[0].Value;
                            txt.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(swap ? "#d5ebff" : "#9fcfff");
                            i += result_list[0].Length - 1;
                            result_list.RemoveAt(0);
                            swap = !swap;
                        }
                    }
                    p.Inlines.Add(txt);
                }
            }
            catch (Exception ex) { return; }
            
            
        }

        private void bd_item_result_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Border).Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#f3db90");
        }

        private void bd_item_result_MouseLeave(object sender, MouseEventArgs e)
        {
            var data_context = (sender as Border).DataContext as MatchResult;
            (sender as Border).Background = (SolidColorBrush)new BrushConverter().ConvertFrom(data_context.BackColor);
        }

        private void lst_result_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var p = rtb_match.Document.Blocks.OfType<Paragraph>().Where(x => x.Name == "prgp_match").First();
            p.Inlines.Clear();
            var selected_result = Result.ElementAtOrDefault(lst_regex.SelectedIndex);
            var result_list = selected_result.MatchResults.GetEnumerator();
            result_list.MoveNext();
            for (int i = 0; i < selected_result.Message.Length; i++)
            {
                Run txt = new Run();
                txt.Text = selected_result.Message[i].ToString();
                if (result_list.Current != null)
                {
                    if (Int32.Parse(result_list.Current.Index) == i)
                    {
                        txt.Text = result_list.Current.Text;
                        txt.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(result_list.Current.BackColor);
                        if (lst_result.SelectedIndex != -1)
                        {
                            if (Int32.Parse((lst_result.SelectedItem as MatchResult).Index) == i)
                            {
                                txt.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#f3db90");
                            }
                        }
                        i += Int32.Parse(result_list.Current.Length) - 1;
                        result_list.MoveNext();
                    }
                }
                p.Inlines.Add(txt);
            }
        }

        private void txt_search_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var selected_type = lst_ref_type.SelectedItem;
            var search_text = (sender as TextEdit) != null ? (sender as TextEdit).EditText.ToLower() : "";
            var items = refItems.Where(x => x.Title.ToLower().Contains(search_text));
            RefItemsDisplay.Clear();
            RefTypesDisplay.Clear();
            refItemsSearch.Clear();
            if (items.Any())
            {
                foreach (var item in items)
                {
                    RefItemsDisplay.Add(item);
                    refItemsSearch.Add(item);
                }

                var types = RefItemsDisplay.Where(x => x.Type != null).Select(x => x.Type);
                var subtypes = RefItemsDisplay.Where(x => x.SubType != null).Select(x => x.SubType);
                if (types.Any())
                {
                    RefTypesDisplay.Add(refTypes.Find(x => x.Name == (search_text == "" ? "All Tokens" : "Full Search Result")));
                    if (subtypes.Any())
                    {
                        foreach (var item in subtypes)
                        {
                            if (!RefTypesDisplay.Contains(refTypes.Find(x => x.Name == item)))
                            {
                                RefTypesDisplay.Add(refTypes.Find(x => x.Name == item));
                            }
                        }
                    }
                    foreach (var item in types)
                    {
                        if (!RefTypesDisplay.Contains(refTypes.Find(x => x.Name == item)))
                        {
                            RefTypesDisplay.Add(refTypes.Find(x => x.Name == item));
                        }
                    }
                    if (RefTypesDisplay.Contains(selected_type))
                    {
                        lst_ref_type.SelectedItem = selected_type;
                    }
                    else
                    {
                        lst_ref_type.SelectedIndex = 0;
                    }
                }
            }
        }

        private void xpd_regex_list_LayoutUpdated(object sender, EventArgs e)
        {
            //Console.WriteLine(xpd_regex_list.ActualHeight.ToString());
        }

        private void stack_workspace_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateHeight();
        }
    }
    public class RegexResult : INotifyPropertyChanged
    {
        string message;
        HRegion box;
        string status;
        ObservableCollection<MatchResult> match_results = new ObservableCollection<MatchResult>();
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                if (message != value) { message = value; }
            }
        }
        public HRegion Box
        {
            get
            {
                return box;
            }
            set
            {
                if (box != value) { box = value; }
            }
        }
        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                if (status != value) { status = value; }
            }
        }
        public ObservableCollection<MatchResult> MatchResults
        {
            get { return match_results; }
            set
            {
                if (match_results != value)
                {
                    match_results = value;
                     OnPropertyChanged("MatchResults");
                }  
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public RegexResult()
        {
            
        }
    }
    public class MatchResult:INotifyPropertyChanged
    {
        string index;
        public string Index
        {
            get { return index; }
            set
            {
                if (index != value)
                {
                    index = value;
                    OnPropertyChanged("Index");
                }
            }
        }
        string length;
        public string Length
        {
            get { return length; }
            set
            {
                if (length != value)
                {
                    length = value;
                    OnPropertyChanged("Length");
                }
            }
        }
        string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged("Text");
                }
            }
        }
        string _backColor;
        public string BackColor
        {
            get { return _backColor; }
            set
            {
                if (_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged("BackColor");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class ReferenceItem : INotifyPropertyChanged
    {
        string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }
        string _pattern;
        public string Pattern
        {
            get { return _pattern; }
            set
            {
                if (_pattern != value)
                {
                    _pattern = value;
                    OnPropertyChanged("Pattern");
                }
            }
        }
        string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged("Description");
                }
            }
        }
        string _examplePattern;
        public string ExamplePattern
        {
            get { return _examplePattern; }
            set
            {
                if (_examplePattern != value)
                {
                    _examplePattern = value;
                    OnPropertyChanged("ExamplePattern");
                }
            }
        }
        string _exampleString;
        public string ExampleString
        {
            get { return _exampleString; }
            set
            {
                if (_exampleString != value)
                {
                    _exampleString = value;
                    OnPropertyChanged("ExampleString");
                }
            }
        }
        string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged("Type");
                }
            }
        }
        string _subtype;
        public string SubType
        {
            get { return _subtype; }
            set
            {
                if (_subtype != value)
                {
                    _subtype = value;
                    OnPropertyChanged("SubType");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class ReferenceType : INotifyPropertyChanged
    {
        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        string _icon;
        public string Icon
        {
            get { return _icon; }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged("Icon");
                }
            }
        }
        bool _isSubType = false;
        public bool IsSubType
        {
            get { return _isSubType; }
            set
            {
                if (_isSubType != value)
                {
                    _isSubType = value;
                    OnPropertyChanged("IsSubType");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
