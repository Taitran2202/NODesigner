using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.SimpleView
{
    /// <summary>
    /// Interaction logic for DragAndResizeControl.xaml
    /// </summary>
    public partial class DragAndResizeControl : UserControl
    {
        /// <summary>
        /// Gets or sets additional content for the UserControl
        /// </summary>
        public object Title
        {
            get { return (object)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(object), typeof(DragAndResizeControl),
              new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets additional content for the UserControl
        /// </summary>
        public Visibility IsEdit
        {
            get { return (Visibility)GetValue(IsEditProperty); }
            set { SetValue(IsEditProperty, value); }
        }
        public static readonly DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(Visibility), typeof(DragAndResizeControl),
              new PropertyMetadata(null));

        public EventHandler OnSizeAndPositionChanged;
        public DragAndResizeControl()
        {
            InitializeComponent();
            IsEdit = Visibility.Hidden;
        }

        private void Resize(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Width = Math.Max(MinWidth, this.Width +e.HorizontalChange);
            this.Height = Math.Max(MinHeight, this.Height + e.VerticalChange);
            OnSizeAndPositionChanged?.Invoke(null, null);
        }

        private void Resize1(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Margin = new Thickness(Math.Max(0, this.Margin.Left + e.HorizontalChange), Math.Max(0, this.Margin.Top + e.VerticalChange), this.Margin.Right, this.Margin.Bottom);

            if (this.Margin.Left + e.HorizontalChange > 0 )
            {
                this.Width = Math.Max(MinWidth, this.Width - e.HorizontalChange);
               
                
                //OnSizeAndPositionChanged?.Invoke(null, null);
            }
            if (this.Margin.Top + e.VerticalChange > 0)
            {
                this.Height = Math.Max(MinHeight, this.Height - e.VerticalChange);


                
            }
            OnSizeAndPositionChanged?.Invoke(null, null);
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            
            this.Margin = new Thickness(Math.Max(0,this.Margin.Left + e.HorizontalChange), Math.Max(0,this.Margin.Top + e.VerticalChange), this.Margin.Right, this.Margin.Bottom);
            OnSizeAndPositionChanged?.Invoke(null, null);
        }

        private void ResizeBottomLeft(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Margin = new Thickness(Math.Max(0,this.Margin.Left + e.HorizontalChange), this.Margin.Top , this.Margin.Right, this.Margin.Bottom);
            this.Width = Math.Max(MinWidth, this.Width - e.HorizontalChange);
            this.Height = Math.Max(MinHeight, this.Height + e.VerticalChange);
            OnSizeAndPositionChanged?.Invoke(null, null);
        }

        private void ResizeTopRight(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Margin = new Thickness(this.Margin.Left, Math.Max(0,this.Margin.Top+e.VerticalChange), this.Margin.Right, this.Margin.Bottom);
            this.Width = Math.Max(MinWidth, this.Width + e.HorizontalChange);
            this.Height = Math.Max(MinHeight, this.Height - e.VerticalChange);
            OnSizeAndPositionChanged?.Invoke(null, null);
        }

        private void btn_close_edit_Click(object sender, RoutedEventArgs e)
        {
            IsEdit = Visibility.Hidden;
        }

        private void btn_edit_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            IsEdit = Visibility.Visible;
        }
    }
}
