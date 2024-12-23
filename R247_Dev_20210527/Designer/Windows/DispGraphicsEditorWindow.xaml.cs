using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Controls;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for DispGraphicsEditorWindow.xaml
    /// </summary>
    public partial class DispGraphicsEditorWindow : ThemedWindow
    {
        HWindow display;
        public ObservableCollection<IGraphic> graphics;
        HImage image;
        HHomMat2D transform;
        public DispGraphicsEditorWindow(ObservableCollection<IGraphic> graphics, HImage image, HHomMat2D transform)
        {
            if (transform == null)
                this.transform = new HHomMat2D();
            else
                this.transform = transform.Clone();
            if (image != null)
            {
                this.image = image;
            }
            else
            {
                this.image = new HImage("byte", 1000, 1000);
            }

            //this.transform = transform;
            this.graphics = graphics;
            InitializeComponent();
            lst_region.ItemsSource = graphics;

        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("margin");
            display.SetColor("green");
            display.AttachBackgroundToWindow(image);
            foreach (IGraphic graphic in graphics)
            {
                AddGraphic(graphic);
            }
            if (graphics.Count > 0)
            {
                SelectedGraphic = graphics[0];
            }
            // radioButton.DataContext = SelectedGraphic;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //  HImage image = new HImage();
            // window_display.HalconWindow.DispImage(image);
            window_display.HalconWindow.DispObj(image);
            //window_display.HalconWindow.DispImage(image);  HOperatorSet.SetSystem("cancel_draw_result", "true");

        }
        private void btn_add_rectangle_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new HalconGraphicText());

        }
       
        string GenListItemName(string prefix="item",int max=100)
        {
            var NameList = graphics.Select(x => x.Name);
            for(int i = 0; i < 100; i++)
            {
                if (!NameList.Contains(prefix + i.ToString()))
                    return prefix + i.ToString();
            }
            return "item";
        }
        private void AddGraphicNew(IGraphic graphic)
        {
            graphic.Name = GenListItemName("Item", 100);
            graphic.Initial((int)row, (int)col);

            IGraphic graphic_add = graphic;
            graphics.Add(graphic_add);
            graphic_add.UpdateEvent = Update;
            HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(graphic_add.OnResize);
                draw.OnDrag(graphic_add.OnResize);
                draw.OnSelect(graphic_add.OnResize);
            }
            if (graphics.Count == 1)
            {
                SelectedGraphic = graphics[0];
            }
        }
        private void AddGraphicDuplicate(IGraphic graphic)
        {

            //  graphic.Initial((int)row, (int)col);
            graphic.Name = GenListItemName("Item", 100);
            IGraphic graphic_add = graphic;
            graphics.Add(graphic_add);
            graphic_add.UpdateEvent = Update;
            HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(graphic_add.OnResize);
                draw.OnDrag(graphic_add.OnResize);
                draw.OnSelect(graphic_add.OnResize);
            }
            if (graphics.Count == 1)
            {
                SelectedGraphic = graphics[0];
            }
        }


        private void AddGraphic(IGraphic graphic)
        {
            IGraphic graphic_add = graphic;
            // regions.regions.Add(region_add);
            graphic_add.UpdateEvent = Update;
            HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            if (draw != null)
            {
                window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
                draw.OnResize(graphic_add.OnResize);
                draw.OnDrag(graphic_add.OnResize);
                draw.OnSelect(graphic_add.OnResize);
            }
        }
        IGraphic selected_graphic = null;

        public IGraphic SelectedGraphic
        {
            get => selected_graphic;
            set
            {
                if (value != selected_graphic)
                {
                    // stack_parameter.DataContext = value;
                    // radioButton.DataContext = value;
                    selected_graphic = value;
                    //properties.SelectedObject = selected_graphic;
                    lst_region.SelectedItem = selected_graphic;
                }
            }
        }

        public void Update(IGraphic sender)
        {
            SelectedGraphic = sender;

            // ChangeRegion();
            DispOverlay();
        }
        public void DispOverlay()
        {
            display.ClearWindow();
            foreach (IGraphic graphic in graphics)
            {
                graphic.DrawOnWindow(display, transform);
            }
            //display.SetDraw("fill");
            //display.SetColor(regions.Color);
            //if (transform != null)
            //    display.DispObj(region.AffineTransRegion(transform, "nearest_neighbor"));
            //else
            //    display.DispObj(region);
        }
        //public void ChangeRegion()
        //{
        //    if (display == null)
        //        return;
        //    region.Dispose();

        //    region.GenEmptyRegion();
        //    foreach (Region item in regions.regions)
        //    {
        //        if (!item.Minus)
        //        {
        //            region = region.Union2(item.region);
        //        }
        //    }
        //    foreach (Region item in regions.regions)
        //    {
        //        if (item.Minus)
        //        {
        //            region = region.Difference(item.region);
        //        }
        //    }
        //    DispOverlay();
        //    regions.region = region;


        //}
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()

            //RefreshGraphic();
        }

        private void btn_add_rec2_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new HalconGraphicLine());
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);
            if (index > -1)
                if (lst_region.Items[index] != null)
                {
                    graphics[index].ClearDrawingData(display);
                    graphics.RemoveAt(index);
                    //  ChangeRegion();
                    DispOverlay();
                }
        }

        private void OnRectangle1_Click(object sender, RoutedEventArgs e)
        {

            AddGraphicNew(new HalconGraphicText());
        }

        private void OnRectangle2_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new HalconGraphicLine());
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            if (SelectedGraphic != null)
            {
                SelectedGraphic.ClearDrawingData(display);
                graphics.Remove(SelectedGraphic);
                //  ChangeRegion();
                DispOverlay();
                SelectedGraphic = null;
            }
        }

        private void color_background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            DispOverlay();
        }
        double row, col;

        private void OnDuplicate(object sender, RoutedEventArgs e)
        {
            if (SelectedGraphic != null)
            {
                AddGraphicDuplicate(SelectedGraphic.Duplicate());

            }
        }

        private void OnCircle_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new HalconGraphicCircle());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new HalconGraphicCircle());
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            row = e.Row;
            col = e.Column;
        }
    }
    public class HalconGraphicText : HelperMethods, INotifyPropertyChanged, IGraphic
    {
        [ReadOnly(true)]
        public string Type { get; set; } = "Text";
        bool _is_enabled = true;
        public bool IsEnabled
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }
        #region Interactive Interface
        public void ClearDrawingData(HWindow display)
        {
            display.DetachDrawingObjectFromWindow(CurrentDraw);
            CurrentDraw = null;
        }
        public void Initial(int row, int col)
        {
            this.Row = row;
            this.Col = col;
            this.Row2 = row + 100;
            this.Col2 = col + 100;
        }
        public int MarginTop { get; set; }
        public int MarginLeft { get; set; }
        public void OnRun(HHomMat2D transform, InspectionContext e)
        {
            if (transform == null)
            {
                e.inspection_result.AddText(Text, Foreground, Background, Row, Col, Size);
            }
            else
            {
                double rows_t, cols_t;
                double rows_t2, cols_t2;
                transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                transform.AffineTransPixel(Row2, Col2, out rows_t2, out cols_t2);
                e.inspection_result.AddText(Text, Foreground, Background, rows_t, cols_t, Size);
            }

        }
        public void DrawOnWindow(HWindow display, HHomMat2D transform)
        {
            if (transform == null)
            {
                // display.DrawLine(out _row, out _col, out _row2, out _col2);
                //display.SetFont("default-Normal-" + Size.ToString());
                //display.SetTposition((int)Row+ MarginTop    , (int) Col+ MarginLeft);
                display.SetFont("default-Normal-" + Size.ToString());
                //display.SetColor(Background);
                //display.SetDraw("fill");
                display.DispText(Text, "image", Row, Col, Foreground, "box_color", Background);
                //HTuple decents, w, h;
                //display.GetStringExtents(Text, out decents, out w, out h);

                //display.DispRectangle1(Row, Col,(double)(Row+decents+h+4*MarginTop),(double)( Col+w+4*MarginLeft));

                //display.SetDraw("margin");
                //display.SetColor(Foreground);

                //display.WriteString(Text);
                display.SetFont("default-Normal-12");
                //display.DispText(Text, "image", Row, Col, "white", "box_color", "blue");
                // display.SetFont("default-Normal-12");
            }
            else
            {
                double rows_t, cols_t;
                double rows_t2, cols_t2;
                transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                transform.AffineTransPixel(Row2, Col2, out rows_t2, out cols_t2);

                //display.SetTposition((int)rows_t+ MarginTop, (int)cols_t+ MarginLeft);
                //display.SetDraw("fill");
                //display.SetColor(Background);

                //display.DispRectangle1(rows_t, cols_t, rows_t2, cols_t2);
                //display.SetDraw("margin");
                //display.SetColor(Foreground);
                display.SetFont("default-Normal-" + Size.ToString());
                display.DispText(Text, "image", rows_t, cols_t, Foreground, "box_color", Background);
                display.SetFont("default-Normal-12");
                //display.DrawLine(out rows_t, out cols_t, out row_t2, out col_t2);
                //transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
                //transform.HomMat2dInvert().AffineTransPixel(row_t2, col_t2, out _row2, out _col2);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            // drawid.SetDrawingObjectParams(new HTuple("row2", "column2"), new HTuple(param[0] + 20, param[1] + 20));
            UpdateDrawingObject(param, transform);
            UpdateEvent?.Invoke(this);
            //RefreshGraphic();
        }
        [Browsable(false)]
        public UpdateGraphicParam UpdateEvent { get; set; }
        public HHomMat2D transform;

        [Browsable(false)]
        public HDrawingObject CurrentDraw { get; set; }
        public HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            CurrentDraw = null;
            this.transform = transform;
            double row_t, col_t, row2_t, col2_t;

            if (transform != null)
            {


                transform.AffineTransPixel(Row, Col, out row_t, out col_t);
                transform.AffineTransPixel(Row2, Col2, out row2_t, out col2_t);

                CurrentDraw = new HDrawingObject(row_t, col_t, row2_t, col2_t);
            }
            else
            {
                CurrentDraw = new HDrawingObject(Row, Col, Row2, Col2);
            }
            return CurrentDraw;


        }
        public void UpdateDrawingObject(HTuple param, HHomMat2D transform)
        {

            if (transform != null)
            {


                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out _row, out _col);
                transform.HomMat2dInvert().AffineTransPixel(param[2], param[3], out _row2, out _col2);


            }
            else
            {
                Row = param[0];
                Col = param[1];
                Row2 = param[2];
                Col2 = param[3];
            }
            // region.GenRectangle1(row1, col1, row2, col2);

        }
        #endregion

        #region Duplicate
        public IGraphic Duplicate()
        {
            HalconGraphicText dup = new HalconGraphicText();
            dup.Foreground = this.Foreground;
            dup.Background = this.Background;
            dup.Row = this.Row;
            dup.Col = this.Col;
            dup.Row2 = this.Row2;
            dup.Col2 = this.Col2;
            dup.Text = this.Text;
            dup.transform = this.transform;
            dup.Size = this.Size;


            return dup;
        }
        #endregion
        string _foreground = "#000000ff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Foreground
        {
            get
            {
                return _foreground;
            }
            set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    RaisePropertyChanged("Foreground");
                }
            }
        }
        string _background = "#ffffffff";
        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Background
        {
            get
            {
                return _background;
            }
            set
            {
                if (_background != value)
                {
                    _background = value;
                    RaisePropertyChanged("Background");
                }
            }
        }


        public object GetModel()
        {
            // return draw;
            return null;
        }


        public void Draw(HWindow window)
        {
            window.SetFont("default-Normal-" + Size.ToString());
            window.DispText(Text, "image", Row, Col, "white", "box_color", "blue");
            window.SetFont("default-Normal-12");
            //window.SetTposition(Row, Col);
            //window.WriteString(Text);
        }
        public void Edit(object parameters)
        {
            HWindow target = parameters as HWindow;
            // target?.AttachDrawingObjectToWindow(draw);
            Edit(target, null);
            target.ClearWindow();
            Draw(target);
            //  target.draw
        }
        public void Remove(object parameters)
        {
            // HWindow target = parameters as HWindow;
            // target?.DetachDrawingObjectFromWindow(draw);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Draw(HWindow display, HHomMat2D transform)
        {
            if (transform == null)

                display.DrawPoint(out _row, out _col);

            else
            {
                double rows_t, cols_t;
                display.DrawPoint(out rows_t, out cols_t);
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void Edit(HWindow display, HHomMat2D transform)
        {
            if (transform == null)
                display.DrawPointMod(_row, _col, out _row, out _col);
            else
            {
                double r_t, c_t, rows_t, cols_t;
                transform.AffineTransPixel(_row, _col, out r_t, out c_t);
                display.DrawPointMod(r_t, c_t, out rows_t, out cols_t);
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public HalconGraphicText()
        {
            Text = "not defined";
            Name = "no name";
            Size = 20;
            MarginTop = 10;
            MarginLeft = 20;
        }
        public void OnMoving(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column"));
            Row = param[0];
            Col = param[1];
        }
        double _row2;
        [ReadOnly(true)]
        public double Row2
        {
            get
            {
                return _row2;
            }
            set
            {
                if (_row2 != value)
                {
                    _row2 = value;
                    RaisePropertyChanged("Row2");
                }
            }
        }
        double _col2;
        [ReadOnly(true)]
        public double Col2
        {
            get
            {
                return _col2;
            }
            set
            {
                if (_col2 != value)
                {
                    _col2 = value;
                    RaisePropertyChanged("Col2");
                }
            }
        }

        double _row = 0;
        [ReadOnly(true)]
        public double Row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row != value)
                {
                    _row = value;
                    //draw?.SetDrawingObjectParams("row", value);
                    RaisePropertyChanged("Row");
                }
            }
        }
        double _col = 0;
        [ReadOnly(true)]
        public double Col
        {
            get
            {
                return _col;
            }
            set
            {
                if (_col != value)
                {
                    _col = value;
                    //draw?.SetDrawingObjectParams("column", value);
                    RaisePropertyChanged("Col");
                }
            }
        }

        string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    //draw?.SetDrawingObjectParams("string", value);
                    UpdateEvent?.Invoke(this);
                    RaisePropertyChanged("Text");
                }
            }
        }
        int _size = 12;
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    //draw?.SetDrawingObjectParams("font", "default-Normal-" + value.ToString());
                    RaisePropertyChanged("Size");
                }
            }
        }
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public void Save(HFile file)
        {
            new HTuple("Text").SerializeTuple().FwriteSerializedItem(file);

            SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
    }

    public class HalconGraphicLine : HelperMethods, INotifyPropertyChanged, IGraphic
    {
        [ReadOnly(true)]
        public string Type { get; set; } = "Line";
        bool _is_enabled = true;
        public bool IsEnabled
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }
        #region Interactive Interface
        public void ClearDrawingData(HWindow display)
        {
            display.DetachDrawingObjectFromWindow(CurrentDraw);
            CurrentDraw = null;
        }
        public void Initial(int row, int col)
        {
            this.Row = row;
            this.Col = col;
            this.Row2 = row + 100;
            this.Col2 = col + 100;
        }
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"));
            UpdateDrawingObject(param, transform);
            UpdateEvent?.Invoke(this);
            //RefreshGraphic();
        }
        [Browsable(false)]
        public UpdateGraphicParam UpdateEvent { get; set; }
        public HHomMat2D transform;
        [Browsable(false)]
        public HDrawingObject CurrentDraw { get; set; }
        public HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            CurrentDraw = null;
            this.transform = transform;
            HTuple row_t, col_t, row2_t, col2_t;

            if (transform != null)
            {


                transform.AffineTransPixel(Row, Col, out row_t, out col_t);
                transform.AffineTransPixel(Row2, Col2, out row2_t, out col2_t);

                CurrentDraw = new HDrawingObject();
                CurrentDraw.CreateDrawingObjectLine(row_t, col_t, row2_t, col2_t);
            }
            else
            {
                CurrentDraw = new HDrawingObject();
                CurrentDraw.CreateDrawingObjectLine(Row, Col, Row2, Col2);
            }
            return CurrentDraw;


        }
        public void UpdateDrawingObject(HTuple param, HHomMat2D transform)
        {

            if (transform != null)
            {


                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out _row, out _col);
                transform.HomMat2dInvert().AffineTransPixel(param[2], param[3], out _row2, out _col2);


            }
            else
            {
                Row = param[0];
                Col = param[1];
                Row2 = param[2];
                Col2 = param[3];
            }
            // region.GenRectangle1(row1, col1, row2, col2);

        }
        #endregion
        #region Duplicate
        public IGraphic Duplicate()
        {
            HalconGraphicLine dup = new HalconGraphicLine();
            dup.Color = this.Color;
            // dup.Background = this.Background;
            dup.Row = this.Row;
            dup.Col = this.Col;
            dup.Row2 = this.Row2;
            dup.Col2 = this.Col2;
            //dup.Text = this.Text;
            dup.transform = this.transform;
            dup.Size = this.Size;


            return dup;
        }
        #endregion
        public object GetModel()
        {
            // return draw;
            return null;
        }
        string _color = "#00ff00ff";

        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    //draw?.SetDrawingObjectParams("color", value);
                    RaisePropertyChanged("Color");
                }
            }
        }
        public void OnRun(HHomMat2D transform, InspectionContext e)
        {
            // DrawOnWindow(display, transform);
            if (transform == null)
            {
                // display.DrawLine(out _row, out _col, out _row2, out _col2);
                e.inspection_result.AddLine(Row, Col, Row2, Col2, Color, Size);
            }
            else
            {
                double rows_t, cols_t, row_t2, col_t2;

                transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                transform.AffineTransPixel(Row2, Col2, out row_t2, out col_t2);
                e.inspection_result.AddLine(rows_t, cols_t, row_t2, col_t2, Color, Size);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void Draw(HWindow window)
        {
            window.SetColor(Color);
            window.DispLine(Row, Col, Row2, Col2);
            //window.SetTposition(Row, Col);
            //window.WriteString(Text);
        }
        public void Edit(object parameters)
        {
            HWindow target = parameters as HWindow;
            // target?.AttachDrawingObjectToWindow(draw);
            Edit(target, null);
            target.ClearWindow();
            Draw(target);
            //  target.draw
        }
        public void Remove(object parameters)
        {
            // HWindow target = parameters as HWindow;
            // target?.DetachDrawingObjectFromWindow(draw);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Draw(HWindow display, HHomMat2D transform)
        {
            if (transform == null)

                display.DrawLine(out _row, out _col, out _row2, out _col2);

            else
            {
                double rows_t, cols_t, row_t2, col_t2;
                display.DrawLine(out rows_t, out cols_t, out row_t2, out col_t2);
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
                transform.HomMat2dInvert().AffineTransPixel(row_t2, col_t2, out _row2, out _col2);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void DrawOnWindow(HWindow display, HHomMat2D transform)
        {
            display.SetColor(Color);

            if (transform == null)
            {
                // display.DrawLine(out _row, out _col, out _row2, out _col2);
                display.SetLineWidth(Size);
                display.DispLine(Row, Col, Row2, Col2);
                display.SetLineWidth(1);
            }
            else
            {
                double rows_t, cols_t, row_t2, col_t2;

                transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                transform.AffineTransPixel(Row2, Col2, out row_t2, out col_t2);
                display.SetLineWidth(Size);
                display.DispLine(rows_t, cols_t, row_t2, col_t2);
                display.SetLineWidth(1);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void Edit(HWindow display, HHomMat2D transform)
        {
            if (transform == null)
                display.DrawLineMod(_row, _col, _row2, _col2, out _row, out _col, out _row2, out _col2);
            else
            {
                double r_t, c_t, rows_t, cols_t;
                double r_t2, c_t2, rows_t2, cols_t2;
                transform.AffineTransPixel(_row, _col, out r_t, out c_t);
                transform.AffineTransPixel(_row2, _col2, out r_t2, out c_t2);
                display.DrawLineMod(r_t, c_t, r_t2, c_t2, out rows_t, out cols_t, out rows_t2, out cols_t2);
                transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
                transform.HomMat2dInvert().AffineTransPixel(rows_t2, cols_t2, out _row2, out _col2);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public HalconGraphicLine()
        {
            //draw = new HDrawingObject();
            //draw.CreateDrawingObjectText(Row, Col, "no_text");
            //draw.OnDrag(OnMoving);
            //Text = "not defined";
            Name = "no name";
            Size = 2;
        }
        public void OnMoving(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column"));
            Row = param[0];
            Col = param[1];
        }
        double _row = 0;
        [ReadOnly(true)]
        public double Row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row != value)
                {
                    _row = value;
                    //draw?.SetDrawingObjectParams("row", value);
                    RaisePropertyChanged("Row");
                }
            }
        }
        double _col = 0;
        [ReadOnly(true)]
        public double Col
        {
            get
            {
                return _col;
            }
            set
            {
                if (_col != value)
                {
                    _col = value;
                    //draw?.SetDrawingObjectParams("column", value);
                    RaisePropertyChanged("Col");
                }
            }
        }
        double _col2 = 50;
        [ReadOnly(true)]
        public double Col2
        {
            get
            {
                return _col2;
            }
            set
            {
                if (_col2 != value)
                {
                    _col2 = value;
                    RaisePropertyChanged("Col2");
                }
            }
        }
        double _row2 = 50;
        [ReadOnly(true)]
        public double Row2
        {
            get
            {
                return _row2;
            }
            set
            {
                if (_row2 != value)
                {
                    _row2 = value;
                    RaisePropertyChanged("Row2");
                }
            }
        }
        int _size;
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    //draw?.SetDrawingObjectParams("font", "default-Normal-" + value.ToString());
                    RaisePropertyChanged("Size");
                }
            }
        }
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public void Save(HFile file)
        {
            new HTuple("Line").SerializeTuple().FwriteSerializedItem(file);

            SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
    }
    public class HalconGraphicCircle : HelperMethods, INotifyPropertyChanged, IGraphic
    {
        bool _is_enabled = true;
        public bool IsEnabled
        {
            get
            {
                return _is_enabled;
            }
            set
            {
                if (_is_enabled != value)
                {
                    _is_enabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }

        #region Interactive Interface
        public void ClearDrawingData(HWindow display)
        {
            display.DetachDrawingObjectFromWindow(CurrentDraw);
            CurrentDraw = null;
        }
        public void Initial(int row, int col)
        {
            this.Row = row;
            this.Col = col;

        }
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {

            //   drawid.GetDrawingObjectParams()
            HTuple param;
            param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "radius"));

            UpdateDrawingObject(param, transform);
            UpdateEvent?.Invoke(this);
            //RefreshGraphic();
        }
        [Browsable(false)]
        public UpdateGraphicParam UpdateEvent { get; set; }
        public HHomMat2D transform;
        [Browsable(false)]
        public HDrawingObject CurrentDraw { get; set; }
        public HDrawingObject CreateDrawingObject(HHomMat2D transform)
        {
            CurrentDraw = null;
            this.transform = transform;
            HTuple row_t, col_t, row2_t, col2_t;

            if (transform != null)
            {


                transform.AffineTransPixel(Row, Col, out row_t, out col_t);
                // transform.AffineTransPixel(Row2, Col2, out row2_t, out col2_t);

                CurrentDraw = new HDrawingObject();
                CurrentDraw.CreateDrawingObjectCircle(row_t, col_t, Radius);
            }
            else
            {
                CurrentDraw = new HDrawingObject();
                CurrentDraw.CreateDrawingObjectCircle(Row, Col, Radius);
            }
            return CurrentDraw;


        }
        public void UpdateDrawingObject(HTuple param, HHomMat2D transform)
        {
            Radius = param[2];
            if (transform != null)
            {


                transform.HomMat2dInvert().AffineTransPixel(param[0], param[1], out _row, out _col);
                // transform.HomMat2dInvert().AffineTransPixel(param[2], param[3], out _row2, out _col2);


            }
            else
            {
                Row = param[0];
                Col = param[1];

            }
            // region.GenRectangle1(row1, col1, row2, col2);

        }
        #endregion
        #region Duplicate
        public IGraphic Duplicate()
        {
            HalconGraphicCircle dup = new HalconGraphicCircle();
            dup.Color = this.Color;
            // dup.Background = this.Background;
            dup.Row = this.Row;
            dup.Col = this.Col;
            dup.Radius = this.Radius;
            //dup.Text = this.Text;
            dup.transform = this.transform;
            dup.Size = this.Size;


            return dup;
        }
        #endregion
        public object GetModel()
        {
            // return draw;
            return null;
        }
        string _color = "#00ff00ff";

        [Category("Appearance")]
        [Editor(typeof(ColorToStringEditor), typeof(ColorToStringEditor))]
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    //draw?.SetDrawingObjectParams("color", value);
                    RaisePropertyChanged("Color");
                }
            }
        }

        public void Draw(HWindow window)
        {
            window.SetColor(Color);
            window.DispCircle(Row, Col, Radius);
            //window.SetTposition(Row, Col);
            //window.WriteString(Text);
        }
        public void Edit(object parameters)
        {
            HWindow target = parameters as HWindow;
            // target?.AttachDrawingObjectToWindow(draw);
            Edit(target, null);
            target.ClearWindow();
            Draw(target);
            //  target.draw
        }
        public void Remove(object parameters)
        {
            // HWindow target = parameters as HWindow;
            // target?.DetachDrawingObjectFromWindow(draw);
        }
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Draw(HWindow display, HHomMat2D transform)
        {
            //if (transform == null)

            //    display.DrawLine(out _row, out _col, out _row2, out _col2);

            //else
            //{
            //    double rows_t, cols_t, row_t2, col_t2;
            //    display.DrawLine(out rows_t, out cols_t, out row_t2, out col_t2);
            //    transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
            //    transform.HomMat2dInvert().AffineTransPixel(row_t2, col_t2, out _row2, out _col2);
            //    //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            //}
        }
        public double Radius { get; set; } = 20;
        public void OnRun(HHomMat2D transform, InspectionContext e)
        {
            if (transform == null)
            {
                // display.DrawLine(out _row, out _col, out _row2, out _col2);
                //e.inspection_result.
                //display.SetLineWidth(Size);
                //display.DispCircle(Row, Col, Radius);
                //display.SetLineWidth(1);
            }
            else
            {
                //double rows_t, cols_t, row_t2, col_t2;

                //transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                //// transform.AffineTransPixel(Row2, Col2, out row_t2, out col_t2);
                //display.SetLineWidth(Size);
                //display.DispCircle(rows_t, cols_t, Radius);
                //display.SetLineWidth(1);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void DrawOnWindow(HWindow display, HHomMat2D transform)
        {
            display.SetColor(Color);

            if (transform == null)
            {
                // display.DrawLine(out _row, out _col, out _row2, out _col2);
                display.SetLineWidth(Size);
                display.DispCircle(Row, Col, Radius);
                display.SetLineWidth(1);
            }
            else
            {
                double rows_t, cols_t, row_t2, col_t2;

                transform.AffineTransPixel(Row, Col, out rows_t, out cols_t);
                // transform.AffineTransPixel(Row2, Col2, out row_t2, out col_t2);
                display.SetLineWidth(Size);
                display.DispCircle(rows_t, cols_t, Radius);
                display.SetLineWidth(1);
                //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            }
        }
        public void Edit(HWindow display, HHomMat2D transform)
        {
            //if (transform == null)
            //    display.DrawLineMod(_row, _col, _row2, _col2, out _row, out _col, out _row2, out _col2);
            //else
            //{
            //    double r_t, c_t, rows_t, cols_t;
            //    double r_t2, c_t2, rows_t2, cols_t2;
            //    transform.AffineTransPixel(_row, _col, out r_t, out c_t);
            //  //  transform.AffineTransPixel(_row2, _col2, out r_t2, out c_t2);
            //    display.DrawLineMod(r_t, c_t, r_t2, c_t2, out rows_t, out cols_t, out rows_t2, out cols_t2);
            //    transform.HomMat2dInvert().AffineTransPixel(rows_t, cols_t, out _row, out _col);
            //   // transform.HomMat2dInvert().AffineTransPixel(rows_t2, cols_t2, out _row2, out _col2);
            //    //region = transform.HomMat2dInvert().AffineTransRegion(temp, "nearest_neighbor");
            //}
        }
        public HalconGraphicCircle()
        {
            //draw = new HDrawingObject();
            //draw.CreateDrawingObjectText(Row, Col, "no_text");
            //draw.OnDrag(OnMoving);
            //Text = "not defined";
            Name = "no name";
            Size = 2;
        }
        public void OnMoving(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple param = drawid.GetDrawingObjectParams(new HTuple("row", "column", "radius"));
            Row = param[0];
            Col = param[1];
            Radius = param[2];
        }
        double _row = 0;
        [ReadOnly(true)]
        public double Row
        {
            get
            {
                return _row;
            }
            set
            {
                if (_row != value)
                {
                    _row = value;
                    //draw?.SetDrawingObjectParams("row", value);
                    RaisePropertyChanged("Row");
                }
            }
        }
        double _col = 0;
        [ReadOnly(true)]
        public double Col
        {
            get
            {
                return _col;
            }
            set
            {
                if (_col != value)
                {
                    _col = value;
                    //draw?.SetDrawingObjectParams("column", value);
                    RaisePropertyChanged("Col");
                }
            }
        }

        int _size;
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    //draw?.SetDrawingObjectParams("font", "default-Normal-" + value.ToString());
                    RaisePropertyChanged("Size");
                }
            }
        }
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public void Save(HFile file)
        {
            new HTuple("Circle").SerializeTuple().FwriteSerializedItem(file);

            SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {
            LoadParam(item, this);
        }
        [ReadOnly(true)]
        public string Type { get; set; } = "Circle";
    }

}
