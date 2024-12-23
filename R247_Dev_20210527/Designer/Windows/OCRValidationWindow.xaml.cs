using HalconDotNet;
using NOVisionDesigner.Designer.Diagram;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Processing;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = NOVisionDesigner.Designer.Processing.Point;

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for OCRValidationWindow.xaml
    /// </summary>
    public partial class OCRValidationWindow : Window,INotifyPropertyChanged
    {
        OCRValidationNode node;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        int _font_size=14;
        public int DisplayFontSize
        {
            get
            {
                return _font_size;
            }
            set
            {
                if (_font_size != value)
                {
                    _font_size = value;
                    RaisePropertyChanged("DisplayFontSize");
                    window_display.HalconWindow?.SetFont("default-Normal-"+value.ToString());
                }
            }
        }

        int _color_opacity=20;
        public int ColorOpacity
        {
            get
            {
                return _color_opacity;
            }
            set
            {
                if (_color_opacity != value)
                {
                    _color_opacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        public ObservableCollection<OCRGroupModel> listModel;
        public OCRValidationDisplayMode displayMode;
        HImage image;
        public List<CharacterResult> charBox;
        public OCRValidationWindow(OCRValidationNode node)
        {
            InitializeComponent();
            this.DataContext = this;
            this.node = node;
            this.listModel = node.ListModel;
            lst_view.ItemsSource = listModel;
            if (node.ImageInput.Value != null)
            {
                image = node.ImageInput.Value.Clone();
            }
            if (node.TextInput.Value != null)
            {
                //charBox = node.TextInput.Value;
                charBox = new List<CharacterResult>();
                foreach (var item in node.TextInput.Value)
                {
                    charBox.Add(new CharacterResult(item.Box, item.ClassName));
                }
            }
            displayMode = new OCRValidationDisplayMode(window_display, this);
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as OCRGroupModel;
            if (vm != null)
            {
                try
                {
                    listModel.Remove(vm);
                }
                catch (Exception ex)
                {

                }

            }
        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            if (image != null)
            {
                window_display.HalconWindow.AttachBackgroundToWindow(image);
            }
        }
        private void btn_add_model_click(object sender, RoutedEventArgs e)
        {
            displayMode.CreateModel();
        }

        private void btn_edit_group_model_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as OCRGroupModel;
            if (selected != null)
            {
                OCRGroupModelWindow wd = new OCRGroupModelWindow(selected);
                wd.Show();
            }
            
        }

        

        private void btn_sort_left_to_right(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as OCRGroupModel;
            if (selected != null)
            {
                double max = selected.CharactersBox.Max(x => x.Box.row);
                selected.CharactersBox.Sort((x,y)=>(x.Box.col + max - x.Box.row).CompareTo(y.Box.col + max - y.Box.row));
                model_view.ReRender();
            }
        }

        private void btn_sort_right_to_left(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as OCRGroupModel;
            if (selected != null)
            {
                double max = selected.CharactersBox.Max(x => x.Box.row);
                selected.CharactersBox.Sort((y, x) => (x.Box.col + max - x.Box.row).CompareTo(y.Box.col + max - y.Box.row));
                model_view.ReRender();
            }
        }

        private void btn_add_group_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_view.SelectedItem as OCRGroupModel;
            if (selected != null)
            {
                GroupValidation groupValidation = new GroupValidation();
                foreach (var item in model_view.DesignerCanvas.Children)
                {
                    var box = item as Rect1Box;
                    if (box != null)
                    {
                        if (box.IsSelected)
                        {
                            groupValidation.CharacterOrder.Add(new CharIndex() { Index = box.Index });
                        }
                        
                    }
                }
                selected.GroupValidationList.Add(groupValidation);
            }
        }

        private void btn_remove_group_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as GroupValidation;
            var selected = lst_view.SelectedItem as OCRGroupModel;
            if (vm != null)
            {
                try
                {
                    selected.GroupValidationList.Remove(vm);
                }
                catch (Exception ex)
                {

                }

            }
        }

        private void btn_add_char_order_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as GroupValidation;
            if (vm != null)
            {
                vm.CharacterOrder.Add(new CharIndex());
            }
        }

        private void btn_remove_char_order_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as Button).DataContext as GroupValidation;
            if (vm != null)
            {
                if(vm.CharacterOrder.Count > 0)
                {
                    vm.CharacterOrder.RemoveAt(vm.CharacterOrder.Count - 1);
                }
                
            }
        }
    }
    public class CharacterGroup2
    {
        public int GroupId { get; set; }
        public List<RegionMaker2> Items = new List<RegionMaker2>();
    }
    public class GroupValidation:IHalconDeserializable
    {
        public string Name { get; set; } = "group 1";
        public ObservableCollection<CharIndex> CharacterOrder { get; set; } = new ObservableCollection<CharIndex>();
        public string ValidationString { get; set; }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class CharIndex:IHalconDeserializable
    {
        public int Index { get; set; }
        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
    }
    public class OCRGroupModel:IHalconDeserializable
    {
        public ObservableCollection<GroupValidation> GroupValidationList { get; set; } = new ObservableCollection<GroupValidation>();
        public string Name { get; set; } = "model 1";
        public string RegexString { get; set; }
        public List<CharacterResult> CharactersBox { get; set; } = new List<CharacterResult>(); 
        static double Distance(Point x, Point y)
        {
            return (x.X - y.X) * (x.X - y.X) + (x.Y - y.Y) * (x.Y - y.Y);
        }
        static IEnumerable<Point> Translate(IOrderedEnumerable<Point> input, double tx, double ty)
        {
            
            return input.Select(x => new Point(x.X + tx, x.Y + ty));
        }
        public static List<Point> Match(List<Point> modelPoint, List<Point> obserPoint)
        {
            var sortedModelPoint = modelPoint.OrderBy(x => x.Y).OrderBy(x => x.X);
            var sortedObserPoint = obserPoint.OrderBy(x => x.Y).OrderBy(x => x.X);
            var firstPoint = sortedModelPoint.First();
            double min_error = 999999999;
            List<Point> BestMatchedPoint = null;
            foreach (var item in sortedObserPoint)
            {
                //get translate
                double tx = item.X - firstPoint.X;
                double ty = item.X - firstPoint.Y;
                //translate
                var translatedModelPoint = Translate(sortedModelPoint, tx, ty);
                List<Point> MatchedPoint = new List<Point>();
                double error = 0;
                foreach (var item2 in translatedModelPoint)
                {
                    var clostedPoint = sortedObserPoint.OrderBy(x => Distance(item2, x)).First();
                    MatchedPoint.Add(clostedPoint);
                    error += Distance(item2, clostedPoint);
                }
                if (error < min_error)
                {
                    min_error = error;
                    BestMatchedPoint = MatchedPoint;
                }

            }
            return BestMatchedPoint;


        }

        public void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }

        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file,this); 
        }
    }
    public class OCRValidationDisplayMode : INotifyPropertyChanged, DisplayMode
    {
        OCRValidationWindow window;
        bool ctrl_keydown = false;
        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelectBoxPosition();
            }
        }

        private void window_display_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            UpdateSelectBoxPosition();
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        RegionMaker2 _selected_marker;
        public RegionMaker2 SelectedMarker
        {
            get
            {
                return _selected_marker;
            }
            set
            {
                if (_selected_marker != value)
                {
                    _selected_marker = value;
                    RaisePropertyChanged("SelectedMarker");
                }
            }
        }

        public ICommand copyAnnotation { get; set; }
        public ICommand pasteAnnotation { get; set; }
        public List<RegionMaker2> CurrentRegionList { get; set; } = new List<RegionMaker2>();
        HWindow Display
        {
            get { return window.window_display.HalconWindow; }
        }

        public void Dispose()
        {
            window.window_display.ContextMenu.Items.Remove(newannotation_menu);
            foreach (var item in CurrentRegionList)
            {
                Display.DetachDrawingObjectFromWindow(item.Region.current_draw);
                item.Region.current_draw = null;
            }
            //remove input behavior
            window.InputBindings.Remove(copyBinding);
            window.InputBindings.Remove(pasteBinding);



        }
        KeyBinding copyBinding, pasteBinding;
        MenuItem newannotation_menu, group_menu;
        public OCRValidationDisplayMode(HSmartWindowControlWPF window_display, OCRValidationWindow ocrwindow)
        {
            //window_display.HInitWindow += Window_display_HInitWindow;
            this.window = ocrwindow;
            ocrwindow.lst_view.SelectionChanged += Lst_view_SelectionChanged;
            ocrwindow.window_display.ContextMenu = new ContextMenu();
            newannotation_menu = new MenuItem() { Header = "Add new Annotation" };
            newannotation_menu.Click += new_annotation_menu_Click;
            ocrwindow.window_display.ContextMenu.Items.Add(newannotation_menu);
            group_menu = new MenuItem() { Header = "Group" };
            group_menu.Click += group_menu_Click;
            ocrwindow.window_display.ContextMenu.Items.Add(group_menu);

            ocrwindow.window_display.HMouseMove += window_display_HMouseMove;
            ocrwindow.window_display.HMouseWheel += window_display_HMouseWheel;
            ocrwindow.window_display.HMouseDown += window_display_HMouseDown;
            ocrwindow.window_display.KeyDown += window_display_KeyDown;
            copyAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                copied = new RegionMaker2() { Annotation = SelectedMarker.Annotation, Region = SelectedMarker.Region };

            });
            pasteAnnotation = new RelayCommand<object>((p) => { return true; }, (p) => {

                if (copied != null)
                {
                    AddRegion(new RegionMaker2() { Annotation = copied.Annotation, Region = new Rectange2(false) { 
                        row = copied.Region.row - 20, 
                        col = copied.Region.col + 20,
                        phi = copied.Region.phi, 
                        length1 = copied.Region.length1, 
                        length2 = copied.Region.length2 }});
                }

            });
            copyBinding = new KeyBinding(copyAnnotation, Key.C, ModifierKeys.Control);
            pasteBinding = new KeyBinding(pasteAnnotation, Key.V, ModifierKeys.Control);
            ocrwindow.InputBindings.Add(copyBinding);
            ocrwindow.InputBindings.Add(pasteBinding);
            if (ocrwindow.window_display.HalconWindow == null)
            {
                ocrwindow.window_display.HInitWindow += Window_display_HInitWindow; ;
            }
            else
            {
                LoadCharaterBox();
            }
        }

        private void Lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = window.lst_view.SelectedItem as OCRGroupModel;
            if (selected != null)
            {
                FindModel(selected);
            }
        }
        public void FindModel(OCRGroupModel model)
        {
            if (model.CharactersBox.Count == 0)
                return;
            var ModelPoint = model.CharactersBox.Select(x => new Point(x.Box.row, x.Box.col)).ToList();
            var obserPoint = CurrentRegionList.Select(x => new Point(x.Region.row, x.Region.col)).ToList();
            var result =RANSAC.Match(ModelPoint, obserPoint);
            if (result != null)
            {
                List<RegionMaker2> group = new List<RegionMaker2>();
                foreach(var item in result)
                {
                    var index = obserPoint.IndexOf(item);
                    group.Add(CurrentRegionList[index]);
                }
                CurrentResultGroupp = new CharacterGroup2() { GroupId = -1, Items = group };
                DispOverlay();
            }

        }
        private void Window_display_HInitWindow(object sender, EventArgs e)
        {
            LoadCharaterBox();
        }

        public void LoadCharaterBox()
        {
            CurrentRegionList.Clear();
            Groups.Clear();
            if (window.charBox == null)
            {
                return;
            }
            foreach(var item in window.charBox)
            {
                AddRegion(new RegionMaker2()
                {
                    Annotation = new ClassifierClass1() { Name = item.ClassName },
                    Region = new Rectange2(false, item.Box.row, item.Box.col, item.Box.phi, item.Box.length1, item.Box.length2)
                });
            }
        }

        public void ChangeRegion()
        {
            if (Display == null)
                return;
            DispOverlay();
        }
        int row, col;

        private void AddRegion(RegionMaker2 region)
        {
            RegionMaker2 region_add = region;
            CurrentRegionList.Add(region_add);
            region_add.Attach(Display, null, Update, Selected);
            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }
            ChangeRegion();
        }

        private void AddRegionNew(RegionMaker2 region)
        {
            //region.Region.Initial((int)row, (int)col);
            //region.Region.row2 = region.Region.
            RegionMaker2 region_add = region;
            CurrentRegionList.Add(region_add);

            region_add.Attach(Display, null, Update, Selected);

            if (CurrentRegionList.Count == 1)
            {
                SelectedMarker = CurrentRegionList[0];
            }

            //lst_region.SelectedItem = region_add;
            ChangeRegion();
        }

        RegionMaker2 copied;
        public void Update(RegionMaker2 sender, Region region)
        {
            //Selected_region = sender;
            //UpdateSelectBoxPosition();
            ChangeRegion();
            //DispOverlay();
        }

        public void Selected(RegionMaker2 sender, Region region)
        {
            //Selected_region = sender;          

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    //group selection
                    if (sender != null)
                    {
                        sender.IsSelected = !sender.IsSelected;
                    }
                }
                else
                {
                    sender.IsSelected = true;
                    //reset selection to single object
                    foreach (var item in CurrentRegionList)
                    {
                        if (item == sender)
                        {

                        }
                        else
                        {
                            item.IsSelected = false;
                        }

                    }
                }
            }
            else
            {
                if (sender != null)
                {
                    //right click on a selected rect will not reset group selection
                    if (sender.IsSelected)
                    {

                    }
                    else
                    {
                        //reset selection to single object
                        foreach (var item in CurrentRegionList)
                        {
                            if (item == sender)
                            {

                            }
                            else
                            {
                                item.IsSelected = false;
                            }

                        }
                    }
                }


            }



            //DispOverlay();
            SelectedMarker = sender;
            //SelectedMarker.IsSelected = true;
            ChangeRegion();
            UpdateSelectBoxPosition();
            //update box label
            if (SelectedMarker != null)
            {
                window.cmb_select_class.SelectedItem = SelectedMarker.Annotation;
            }
        }
        void DrawGroupBoundingBox()
        {
            int padding = 5;
            foreach (var item in Groups)
            {
                DrawGroupBox(padding, item);

            }
        }

        private void DrawGroupBox(int padding, CharacterGroup2 item)
        {
            var charlist = item.Items;
            if (charlist.Count > 0)
            {
                var bbbox = charlist.Select(x => ViewDisplayMode.BoundingBox(x.Region.row,
                    x.Region.col, x.Region.phi, x.Region.length1, x.Region.length2));

                var row1 = bbbox.Min(x => x.row1) - padding;
                var row2 = bbbox.Max(x => x.row2) + padding;
                var col1 = bbbox.Min(x => x.col1) - padding;
                var col2 = bbbox.Max(x => x.col2) + padding;
                foreach (RegionMaker2 item2 in charlist)
                {
                    Display.SetColor(OCRWindow.AddOpacity(item2.Annotation.Color, window.ColorOpacity / 100.0));
                    Display.DispRegion(item2.Region.region);
                }
                Display.SetDraw("margin");
                Display.SetColor(OCRWindow.AddOpacity("#0000ffff", 100 / 100));
                Display.DispRectangle1(row1, col1, row2, col2);
                Display.SetDraw("fill");
                Display.SetColor(OCRWindow.AddOpacity("#0000ffff", window.ColorOpacity / 100));
                Display.DispRectangle1(row1, col1, row2, col2);
                Display.DispText(string.Join("", item.Items.Select(x => x.Annotation.Name)), "image", row1 - 10, col1, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
                
            }
        }

        public void DispOverlay()
        {
            if (Display == null)
                return;
            Display.ClearWindow();
            Display.SetDraw("fill");
            //display.SetColor("#00ff0025");
            foreach (var item in CurrentRegionList)
            {
                //display.SetColor(item.Color);
                if (item.Annotation != null)
                {
                    //Display.SetColor(OCRWindow.AddOpacity(item.Annotation.Color, window.ColorOpacity / 100));
                    //Display.DispObj(item.Region.region);
                    Display.DispText(item.Annotation.Name, "image", item.Region.row, item.Region.col, "black", new HTuple("box_color"), new HTuple("#ffffffff"));
                }

            }
            DrawGroupBoundingBox();
            if (CurrentResultGroupp != null)
            {
               
                DrawGroupBox(5, CurrentResultGroupp);
                
            }

        }
        public void UpdateSelectBoxPosition()
        {
            if (SelectedMarker != null)
            {
                double winposx, winposy;
                Display.ConvertCoordinatesImageToWindow(SelectedMarker.Region.row, SelectedMarker.Region.col, out winposx, out winposy);
                window.cmb_select_class.Margin = new Thickness(winposy, winposx - 25, 0, 0);
            }

        }
        public void ClearAnnotation()
        {
            foreach (var item in CurrentRegionList)
            {
                item.Region.ClearDrawingData(Display);
            }
            CurrentRegionList.Clear();
            Groups.Clear();
        }


        
        
        private void Lst_class_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (SelectedMarker != null)
            //{
            //    SelectedMarker.Annotation = window.lst_class.SelectedItem as ClassifierClass1;
            //    window.cmb_select_class.SelectedItem = window.lst_class.SelectedItem as ClassifierClass1;
            //    DispOverlay();
            //}
        }
        public void OnImageChanging(string imagepath, string annotation_path)
        {
            
        }

        private void window_display_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            //window_display.Focus();
            if (e.Button == MouseButton.Right)
            {
                row = (int)e.Row;
                col = (int)e.Column;
            }
            Keyboard.Focus(window.window_display);
        }
        public List<CharacterGroup2> Groups = new List<CharacterGroup2>();
        CharacterGroup2 CurrentResultGroupp;
        public void CreateModel()
        {
            OCRGroupModel oCRGroupModel = new OCRGroupModel() { CharactersBox = new List<CharacterResult>()};
            foreach (var item in CurrentRegionList)
            {
                if (item.IsSelected)
                {
                    //assign char to new group
                    oCRGroupModel.CharactersBox.Add(
                        new CharacterResult(new Rect2(item.Region.row, item.Region.col, item.Region.phi, item.Region.length1, item.Region.length2), item.Annotation.Name));
                    

                }
            }
            window.listModel.Add(oCRGroupModel);

        }
        private void group_menu_Click(object sender, RoutedEventArgs e)
        {
            int newGroupId = -1;
            for (int i = 0; i < 100; i++)
            {
                if (Groups.FirstOrDefault(x => x.GroupId == i) == null)
                {
                    newGroupId = i;
                    break;
                }
            }
            if (newGroupId != -1)
            {
                var newgroup = new CharacterGroup2() { GroupId = newGroupId };
                foreach (var item in CurrentRegionList)
                {
                    if (item.IsSelected)
                    {
                        //remove char from an existing group
                        foreach (var item2 in Groups)
                        {
                            if (item2.Items.Contains(item))
                            {
                                item2.Items.Remove(item);
                            }
                        }
                        //assign char to new group
                        newgroup.Items.Add(item);
                        item.GroupId = newgroup.GroupId;

                    }
                }



                Groups.Add(newgroup);
            }

        }
        private void new_annotation_menu_Click(object sender, RoutedEventArgs e)
        {

            var width = window.window_display.HImagePart.Width / 4;
            var height = window.window_display.HImagePart.Height / 4;
            AddRegionNew(new RegionMaker2() { Annotation = new ClassifierClass1() { Name = "0" }, Region = new Rectange2(false,row,col,0,width,height) }); ;

        }
        private void window_display_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrl_keydown = true;
                return;
            }

            if (ctrl_keydown)
            {
                if (e.Key == Key.C)
                {
                    ctrl_keydown = true;
                    return;
                }

                ctrl_keydown = false;
                return;
            }

            if (e.Key == Key.Delete)
            {
                if (SelectedMarker != null)
                {
                    SelectedMarker.Region.ClearDrawingData(Display);
                    CurrentRegionList.Remove(SelectedMarker);
                    if (CurrentRegionList.Count > 0)
                    {
                        SelectedMarker = CurrentRegionList[CurrentRegionList.Count - 1];
                    }
                    else
                    {
                        SelectedMarker = null;
                    }

                    DispOverlay();
                }
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                if (SelectedMarker != null)
                {
                    //ClassifierClass1 item = window.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == e.Key.ToString().ToLower()).First();
                    //SelectedMarker.Annotation = item;
                    //window.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                if (SelectedMarker != null)
                {
                    //ClassifierClass1 item = window.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == ((int)e.Key - 34).ToString()).First();
                    //SelectedMarker.Annotation = item;
                    //window.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                if (SelectedMarker != null)
                {
                    //ClassifierClass1 item = window.lst_class.Items.OfType<ClassifierClass1>().Where(p => p.Name == ((int)e.Key - 74).ToString()).First();
                    //SelectedMarker.Annotation = item;
                    //window.cmb_select_class.SelectedItem = item;
                    DispOverlay();
                }
            }
        }

        public void OnImageChanged(string imagepath, string annotation_path)
        {
            
        }
    }
}
