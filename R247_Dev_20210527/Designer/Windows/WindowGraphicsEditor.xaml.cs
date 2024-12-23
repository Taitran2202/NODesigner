using DevExpress.Xpf.Core;
using HalconDotNet;
using NOVisionDesigner.Designer.Accquisition;
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
    public partial class WindowGraphicsEditor : ThemedWindow
    {
        HWindow display;
        public HDrawingObject CurrentDraw { get; set; }
        public ObservableCollection<WindowGraphic> graphics;
        HImage image;
        //HHomMat2D transform;
        public WindowGraphicsEditor(ObservableCollection<WindowGraphic> graphics, HImage image)
        {
            //if (transform == null)
            //    this.transform = new HHomMat2D();
            //else
            //    this.transform = transform.Clone();
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
            foreach (WindowGraphic graphic in graphics)
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
            window_display.HalconWindow.DispObj(image);

        }
        private void btn_add_rectangle_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new ResultGraphic());

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
        private void AddGraphicNew(WindowGraphic graphic)
        {
            graphic.Name = GenListItemName("Item", 100);
            WindowGraphic graphic_add = graphic;
            graphics.Add(graphic_add);
            lst_region.SelectedItem = graphic_add;
            //graphic_add.UpdateEvent = Update;
            //HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            //if (draw != null)
            //{
            //    window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
            //    draw.OnResize(graphic_add.OnResize);
            //    draw.OnDrag(graphic_add.OnResize);
            //    draw.OnSelect(graphic_add.OnResize);
            //}
            //if (graphics.Count == 1)
            //{
            //    SelectedGraphic = graphics[0];
            //}
        }
        private void AddGraphicDuplicate(WindowGraphic graphic)
        {
            //  graphic.Initial((int)row, (int)col);
            graphic.Name = GenListItemName("Item", 100);
            WindowGraphic graphic_add = graphic;
            graphics.Add(graphic_add);
            //graphic_add.UpdateEvent = Update;
            //HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            //if (draw != null)
            //{
            //    window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
            //    draw.OnResize(graphic_add.OnResize);
            //    draw.OnDrag(graphic_add.OnResize);
            //    draw.OnSelect(graphic_add.OnResize);
            //}
            //if (graphics.Count == 1)
            //{
            //    SelectedGraphic = graphics[0];
            //}
        }


        private void AddGraphic(WindowGraphic graphic)
        {
            //IGraphic graphic_add = graphic;
            // regions.regions.Add(region_add);
            //graphic_add.UpdateEvent = Update;
            //HDrawingObject draw = graphic_add.CreateDrawingObject(transform);
            //if (draw != null)
            //{
            //    window_display.HalconWindow.AttachDrawingObjectToWindow(draw);
            //    draw.OnResize(graphic_add.OnResize);
            //    draw.OnDrag(graphic_add.OnResize);
            //    draw.OnSelect(graphic_add.OnResize);
            //}
        }
        WindowGraphic selected_graphic = null;

        public WindowGraphic SelectedGraphic
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

        public void Update(WindowGraphic sender)
        {
            SelectedGraphic = sender;
            DispOverlay();
        }
        InspectionContext sampleContext = new InspectionContext(null,null,1,1,"");
        public void DispOverlay()
        {
            display.ClearWindow();
            foreach (WindowGraphic graphic in graphics)
            {
                graphic.Display(display, sampleContext);
            }
        }
       
        public void OnResize(HDrawingObject drawid, HWindow window, string type)
        {
        }

        private void btn_add_rec2_Click(object sender, RoutedEventArgs e)
        {
            AddGraphicNew(new FPSGraphic());
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = lst_region.Items.IndexOf(button.DataContext);
            if (index > -1)
                if (lst_region.Items[index] != null)
                {
                    //graphics[index].ClearDrawingData(display);
                    graphics.RemoveAt(index);
                    //  ChangeRegion();
                    DispOverlay();
                }
        }

        private void OnRectangle1_Click(object sender, RoutedEventArgs e)
        {

            //AddGraphicNew(new HalconGraphicText());
        }

        private void OnRectangle2_Click(object sender, RoutedEventArgs e)
        {
            //AddGraphicNew(new HalconGraphicLine());
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            if (SelectedGraphic != null)
            {
                //SelectedGraphic.ClearDrawingData(display);
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
                //AddGraphicDuplicate(SelectedGraphic.Duplicate());

            }
        }

        private void OnCircle_Click(object sender, RoutedEventArgs e)
        {
            //AddGraphicNew(new HalconGraphicCircle());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //AddGraphicNew(new HalconGraphicCircle());
        }
        public void OnDrag(HDrawingObject drawid, HWindow window, string type)
        {
            if (SelectedGraphic != null)
            {
                var data = drawid.GetDrawingObjectParams(new HTuple("row1", "column1"));
                SelectedGraphic.Row = data[0];
                SelectedGraphic.Column = data[1];
                DispOverlay();
            }
        }
        private void lst_region_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lst_region.SelectedItem as WindowGraphic;
            SelectedGraphic = selectedItem;
            if (CurrentDraw == null & selectedItem!=null)
            {
                CurrentDraw = new HDrawingObject();
                CurrentDraw.CreateDrawingObjectRectangle1(selectedItem.Row, selectedItem.Column, selectedItem.Row + 50, selectedItem.Column + 50);
                CurrentDraw.OnDrag(OnDrag);
                window_display.HalconWindow.AttachDrawingObjectToWindow(CurrentDraw);
            }
            else
            {
                if (selectedItem == null)
                {
                    window_display.HalconWindow.DetachDrawingObjectFromWindow(CurrentDraw);
                    CurrentDraw = null;
                }
                else
                {
                    CurrentDraw.SetDrawingObjectParams(new HTuple("row1", "column1", "row2", "column2"), new HTuple(selectedItem.Row, selectedItem.Column, selectedItem.Row + 50, selectedItem.Column + 50));
                }
                
            }
        }

        private void window_display_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            row = e.Row;
            col = e.Column;
        }
    }

}
