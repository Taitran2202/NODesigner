using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Diagram
{
    /// <summary>
    /// Interaction logic for DiagramDesigner.xaml
    /// </summary>
    public partial class DiagramDesigner : UserControl
    {
        public static readonly DependencyProperty SetModelProperty =
         DependencyProperty.Register("Model", typeof(OCRGroupModel), typeof(DiagramDesigner), new
            PropertyMetadata(null, new PropertyChangedCallback(OnSetModelChanged)));

        public OCRGroupModel Model
        {
            get { return (OCRGroupModel)GetValue(SetModelProperty); }
            set { SetValue(SetModelProperty, value); }
        }
        Style contentStyle;
        public DiagramDesigner()
        {
            InitializeComponent();
            contentStyle =this.TryFindResource("DesignerItemStyle") as Style;
            
        }
        public void SetEditMode(bool Mode)
        {
            
            if (Mode)
            {
                foreach (Control child in DesignerCanvas.Children)
                {
                    Selector.SetIsSelected(child, true);
                }
            }
            else
            {
                foreach (Control child in DesignerCanvas.Children)
                {
                    Selector.SetIsSelected(child, false);
                }
            }
        }
        double offetx, offsety;
        double scale = 1;
        private static void OnSetModelChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            var designer = d as DiagramDesigner;
            if (designer != null)
            {
                designer.Render(e.NewValue as OCRGroupModel);
            }
        }
        bool _mouse_down = false;
        private void canvas_mouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_mouse_down)
            {
                _mouse_down = true;
                var source = e.Source as Rect1Box;
                if(Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (source != null)
                    {
                        //foreach (var item in DesignerCanvas.Children)
                        //{
                        //    var rec = item as Rect1Box;
                        //    if (rec != null)
                        //    {
                        //        rec.IsSelected = false;
                        //    }
                        //}
                        source.IsSelected = true;
                    }
                }
                else
                {
                    if (source != null)
                    {
                        foreach (var item in DesignerCanvas.Children)
                        {
                            var rec = item as Rect1Box;
                            if (rec != null)
                            {
                                rec.IsSelected = false;
                            }
                        }
                        source.IsSelected = true;
                    }
                }
                
            }
        }

        private void DesignerCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _mouse_down = false;
        }

        private void DesignerCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouse_down = false;
        }
        public void ReRender()
        {
            Render(Model);
        }
        double ScaleRate = 2;
        private void DesignerCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                st.ScaleX *= ScaleRate;
                st.ScaleY *= ScaleRate;
            }
            else
            {
                st.ScaleX /= ScaleRate;
                st.ScaleY /= ScaleRate;
            }
        }

        private void Render(OCRGroupModel model)
        {
            
            DesignerCanvas.Children.Clear();
            if (model == null)
            {
                return;
            }
            if (model.CharactersBox.Count == 0)
            {
                return;
            }
            int i = 0;
            var bbbox = model.CharactersBox.Select(x => ViewDisplayMode.BoundingBox(x.Box.row,
                    x.Box.col, x.Box.phi, x.Box.length1, x.Box.length2));

            var row1 = bbbox.Min(x => x.row1);
            var row2 = bbbox.Max(x => x.row2);
            var col1 = bbbox.Min(x => x.col1);
            var col2 = bbbox.Max(x => x.col2);
            
            //fit up to 50% of canvas
            var width = (col2 - col1);
            var height = (row2 - row1);
            var scalex = DesignerCanvas.ActualWidth / width;
            var scaley = DesignerCanvas.ActualHeight / height;
            scale = Math.Min(scalex, scaley)* ScaleRate;
            var bbcenterx = (col1 + col2) / 2;
            var bbcentery = (row1 + row2) / 2;
            offsety = row1;
            offetx = col1;
            foreach (var item in model.CharactersBox)
            {
                var newbox = new Rect1Box() {
                    IsDragable = false,
                    Width = item.Box.length1 * 2 * scale,
                    Height = item.Box.length2 * 2 * scale,
                    Style = contentStyle,
                    Index = i
                };
                var text = new TextBlock() { 
                    Text = i.ToString(),
                    FontSize= 14,
                    FontWeight = FontWeights.Bold,
                    Foreground= Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsHitTestVisible=false
                };
                var border = new Grid() { 
                    IsHitTestVisible=false
                
                };

                border.Children.Add(text);
                newbox.Content = border;
                DesignerCanvas.Children.Add(
                    newbox
                );
                Canvas.SetLeft(newbox, (item.Box.col-item.Box.length1 -offetx)*scale);
                Canvas.SetTop(newbox, (item.Box.row- item.Box.length2 - offsety)*scale);
                i++;
            }
        } 
    }
}
