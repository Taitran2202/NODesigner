using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOVisionDesigner.Designer.Misc
{
    public class ZoomBorder : Border
    {

        private UIElement child = null;
        private Point origin;
        private Point start;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);

            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(child);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }

        }
        public bool CanMove = false;
        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!CanMove)
                return;
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }
            }
        }

        #endregion
    }
    
    public class DrawingRectangle : DrawingObject
    {

        string _class_name;
        public string ClassName
        {
            get
            {
                return _class_name;
            }
            set
            {
                if (_class_name != value)
                {
                    _class_name = value;
                    RaisePropertyChanged("ClassName");
                }
            }
        }


        Thumb thumb_1;
        Thumb thumb_2;
        Thumb thumb_3;
        Thumb thumb_4;
        Thumb thumb_move;
        System.Windows.Shapes.Rectangle rectangle;
        protected override void IsSelectedChanged()
        {
            if (IsSelected)
            {
                rectangle.StrokeThickness = 2;
            }
            else
            {
                rectangle.StrokeThickness = 1;

            }
            base.IsSelectedChanged();
        }
        public DrawingRectangle(object datacontext, GridItemControl drag, double x, double y, double w, double h)
        {
            Canvas.SetTop(this, y);
            Canvas.SetLeft(this, x);
            Width = w;
            Height = h;

            rectangle = new System.Windows.Shapes.Rectangle() { StrokeThickness = 1 };
            rectangle.Fill = new SolidColorBrush(new Color() { A = 100, R = 255, G = 255, B = 255 });

            if (datacontext != null)
            {
                Binding borderbinding = new Binding("SelectedColor");
                borderbinding.Converter = new ColorToBrush();
                borderbinding.Source = datacontext;
                rectangle.SetBinding(System.Windows.Shapes.Rectangle.StrokeProperty, borderbinding);

            }
            else
                rectangle.Stroke = Brushes.DeepSkyBlue;



            thumb_1 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, -5.5, 0, 0) };
            thumb_2 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -5.5, -5.5) };
            thumb_3 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, -5.5, -5.5, 0) };
            thumb_4 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, 0, 0, -5.5) };
            thumb_move = new MoveThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = 10, Height = 10 };

            Content = new Grid();
            (Content as Grid).Children.Add(rectangle);
            (Content as Grid).Children.Add(thumb_1);
            (Content as Grid).Children.Add(thumb_2);
            (Content as Grid).Children.Add(thumb_3);
            (Content as Grid).Children.Add(thumb_4);
            (Content as Grid).Children.Add(thumb_move);

            //InitializeThumb();
        }
    }
    public class DrawingCirle : DrawingObject
    {

        string _class_name;
        public string ClassName
        {
            get
            {
                return _class_name;
            }
            set
            {
                if (_class_name != value)
                {
                    _class_name = value;
                    RaisePropertyChanged("ClassName");
                }
            }
        }


        Thumb thumb_1;
        Thumb thumb_2;
        Thumb thumb_3;
        Thumb thumb_4;
        Thumb thumb_move;
        System.Windows.Shapes.Ellipse cirlce;
        protected override void IsSelectedChanged()
        {
            if (IsSelected)
            {
                cirlce.StrokeThickness = 2;
            }
            else
            {
                cirlce.StrokeThickness = 1;

            }
            base.IsSelectedChanged();
        }
        public DrawingCirle(object datacontext, double x, double y, double radius)
        {
            Canvas.SetTop(this, y);
            Canvas.SetLeft(this, x);
            Width = Height = radius;


            cirlce = new System.Windows.Shapes.Ellipse() { StrokeThickness = 1 };
            cirlce.Fill = new SolidColorBrush(new Color() { A = 100, R = 255, G = 255, B = 255 });

            if (datacontext != null)
            {
                Binding borderbinding = new Binding("SelectedColor");
                borderbinding.Converter = new ColorToBrush();
                borderbinding.Source = datacontext;
                cirlce.SetBinding(System.Windows.Shapes.Rectangle.StrokeProperty, borderbinding);

            }
            else
                cirlce.Stroke = Brushes.DeepSkyBlue;



            thumb_1 = new ResizeRatioThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, -5.5, 0, 0) };
            thumb_2 = new ResizeRatioThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -5.5, -5.5) };
            thumb_3 = new ResizeRatioThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, -5.5, -5.5, 0) };
            thumb_4 = new ResizeRatioThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, 0, 0, -5.5) };
            thumb_move = new MoveThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = 10, Height = 10 };

            Content = new Grid();
            (Content as Grid).Children.Add(cirlce);
            (Content as Grid).Children.Add(thumb_1);
            (Content as Grid).Children.Add(thumb_2);
            (Content as Grid).Children.Add(thumb_3);
            (Content as Grid).Children.Add(thumb_4);
            (Content as Grid).Children.Add(thumb_move);

            //InitializeThumb();
        }
    }
    public class DrawingEllipse : DrawingObject
    {

        string _class_name;
        public string ClassName
        {
            get
            {
                return _class_name;
            }
            set
            {
                if (_class_name != value)
                {
                    _class_name = value;
                    RaisePropertyChanged("ClassName");
                }
            }
        }


        Thumb thumb_1;
        Thumb thumb_2;
        Thumb thumb_3;
        Thumb thumb_4;
        Thumb thumb_move;
        System.Windows.Shapes.Ellipse ellipse;
        protected override void IsSelectedChanged()
        {
            if (IsSelected)
            {
                ellipse.StrokeThickness = 2;
            }
            else
            {
                ellipse.StrokeThickness = 1;

            }
            base.IsSelectedChanged();
        }
        public DrawingEllipse(object datacontext, double x, double y, double radius)
        {
            Canvas.SetTop(this, y);
            Canvas.SetLeft(this, x);
            Width = Height = radius;


            ellipse = new System.Windows.Shapes.Ellipse() { StrokeThickness = 1 };
            ellipse.Fill = new SolidColorBrush(new Color() { A = 100, R = 255, G = 255, B = 255 });

            if (datacontext != null)
            {
                Binding borderbinding = new Binding("SelectedColor");
                borderbinding.Converter = new ColorToBrush();
                borderbinding.Source = datacontext;
                ellipse.SetBinding(System.Windows.Shapes.Rectangle.StrokeProperty, borderbinding);

            }
            else
                ellipse.Stroke = Brushes.DeepSkyBlue;



            thumb_1 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, -5.5, 0, 0) };
            thumb_2 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -5.5, -5.5) };
            thumb_3 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, -5.5, -5.5, 0) };
            thumb_4 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, 0, 0, -5.5) };
            var thumb_rotate = new RotateThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -10, 0) };

            thumb_move = new MoveThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = 10, Height = 10 };

            Content = new Grid();
            (Content as Grid).Children.Add(ellipse);
            (Content as Grid).Children.Add(thumb_1);
            (Content as Grid).Children.Add(thumb_2);
            (Content as Grid).Children.Add(thumb_3);
            (Content as Grid).Children.Add(thumb_4);
            (Content as Grid).Children.Add(thumb_move);
            (Content as Grid).Children.Add(thumb_rotate);

            //InitializeThumb();
        }
    }
    public class RotateThumb : Thumb
    {
        private Point centerPoint;
        private Vector startVector;
        private double initialAngle;
        private Canvas designerCanvas;
        private FrameworkElement designerItem;
        private RotateTransform rotateTransform;

        public RotateThumb(FrameworkElement designerItem)
        {
            this.designerItem = designerItem;
            this.BorderThickness = new Thickness(2);
            DragDelta += new DragDeltaEventHandler(this.RotateThumb_DragDelta);
            DragStarted += new DragStartedEventHandler(this.RotateThumb_DragStarted);
        }

        private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {


            if (this.designerItem != null)
            {
                this.designerCanvas = VisualTreeHelper.GetParent(this.designerItem) as Canvas;

                if (this.designerCanvas != null)
                {
                    this.centerPoint = this.designerItem.TranslatePoint(
                        new Point(this.designerItem.Width * this.designerItem.RenderTransformOrigin.X,
                                  this.designerItem.Height * this.designerItem.RenderTransformOrigin.Y),
                                  this.designerCanvas);

                    Point startPoint = Mouse.GetPosition(this.designerCanvas);
                    this.startVector = Point.Subtract(startPoint, this.centerPoint);

                    this.rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                    if (this.rotateTransform == null)
                    {
                        this.designerItem.RenderTransform = new RotateTransform(0);
                        this.initialAngle = 0;
                    }
                    else
                    {
                        this.initialAngle = this.rotateTransform.Angle;
                    }
                }
            }
        }

        private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.designerItem != null && this.designerCanvas != null)
            {
                Point currentPoint = Mouse.GetPosition(this.designerCanvas);
                Vector deltaVector = Point.Subtract(currentPoint, this.centerPoint);

                double angle = Vector.AngleBetween(this.startVector, deltaVector);

                RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;
                rotateTransform.Angle = this.initialAngle + Math.Round(angle, 0);
                this.designerItem.InvalidateMeasure();
            }
        }
    }
    public class ResizeThumb : Thumb
    {
        private double angle;
        private Point transformOrigin;
        private FrameworkElement designerItem;

        public ResizeThumb(FrameworkElement designerItem)
        {
            this.designerItem = designerItem;
            this.BorderThickness = new Thickness(2);
            DragStarted += new DragStartedEventHandler(this.ResizeThumb_DragStarted);
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {


            if (this.designerItem != null)
            {
                this.transformOrigin = this.designerItem.RenderTransformOrigin;
                RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;

                if (rotateTransform != null)
                {
                    this.angle = rotateTransform.Angle * Math.PI / 180.0;
                }
                else
                {
                    this.angle = 0;
                }
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.designerItem != null)
            {
                double deltaVertical, deltaHorizontal;

                switch (VerticalAlignment)
                {
                    case System.Windows.VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange, this.designerItem.Height - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) - deltaVertical * this.transformOrigin.Y * Math.Sin(-this.angle));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    case System.Windows.VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange, this.designerItem.Height - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaVertical * Math.Cos(-this.angle) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaVertical * Math.Sin(-this.angle) - (this.transformOrigin.Y * deltaVertical * Math.Sin(-this.angle)));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    default:
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case System.Windows.HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange, this.designerItem.Width - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaHorizontal * Math.Sin(this.angle) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaHorizontal * Math.Cos(this.angle) + (this.transformOrigin.X * deltaHorizontal * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    case System.Windows.HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-e.HorizontalChange, this.designerItem.Width - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + (deltaHorizontal * this.transformOrigin.X * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    default:
                        break;
                }
            }

            // e.Handled = true;
        }
    }
    public class ResizeRatioThumb : Thumb
    {
        private double angle;
        private Point transformOrigin;
        private FrameworkElement designerItem;

        public ResizeRatioThumb(FrameworkElement designerItem)
        {
            this.designerItem = designerItem;
            this.BorderThickness = new Thickness(2);
            DragStarted += new DragStartedEventHandler(this.ResizeThumb_DragStarted);
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);

        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {


            if (this.designerItem != null)
            {
                this.transformOrigin = this.designerItem.RenderTransformOrigin;
                RotateTransform rotateTransform = this.designerItem.RenderTransform as RotateTransform;

                if (rotateTransform != null)
                {
                    this.angle = rotateTransform.Angle * Math.PI / 180.0;
                }
                else
                {
                    this.angle = 0;
                }
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.designerItem != null)
            {
                double vertical_change;
                double horizonal_change;
                if ((VerticalAlignment == VerticalAlignment.Top & HorizontalAlignment == HorizontalAlignment.Left) |
                        (VerticalAlignment == VerticalAlignment.Bottom & HorizontalAlignment == HorizontalAlignment.Right))
                {
                    horizonal_change = vertical_change = e.HorizontalChange * Math.Sqrt(2) / 2 + (e.VerticalChange * Math.Sqrt(2) / 2);
                }
                else
                {
                    horizonal_change = e.HorizontalChange * Math.Sqrt(2) / 2 - (e.VerticalChange * Math.Sqrt(2) / 2);
                    vertical_change = -horizonal_change;
                }

                double deltaVertical, deltaHorizontal;

                switch (VerticalAlignment)
                {
                    case System.Windows.VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-vertical_change, this.designerItem.Height - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) - deltaVertical * this.transformOrigin.Y * Math.Sin(-this.angle));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    case System.Windows.VerticalAlignment.Top:
                        deltaVertical = Math.Min(vertical_change, this.designerItem.Height - this.designerItem.MinHeight);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaVertical * Math.Cos(-this.angle) + (this.transformOrigin.Y * deltaVertical * (1 - Math.Cos(-this.angle))));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaVertical * Math.Sin(-this.angle) - (this.transformOrigin.Y * deltaVertical * Math.Sin(-this.angle)));
                        this.designerItem.Height -= deltaVertical;
                        break;
                    default:
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case System.Windows.HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(horizonal_change, this.designerItem.Width - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) + deltaHorizontal * Math.Sin(this.angle) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + deltaHorizontal * Math.Cos(this.angle) + (this.transformOrigin.X * deltaHorizontal * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    case System.Windows.HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-horizonal_change, this.designerItem.Width - this.designerItem.MinWidth);
                        Canvas.SetTop(this.designerItem, Canvas.GetTop(this.designerItem) - this.transformOrigin.X * deltaHorizontal * Math.Sin(this.angle));
                        Canvas.SetLeft(this.designerItem, Canvas.GetLeft(this.designerItem) + (deltaHorizontal * this.transformOrigin.X * (1 - Math.Cos(this.angle))));
                        this.designerItem.Width -= deltaHorizontal;
                        break;
                    default:
                        break;
                }
            }
            Width = Height;
            // e.Handled = true;
        }
    }
    public class MoveThumb : Thumb
    {
        FrameworkElement designerItem;
        public MoveThumb(FrameworkElement designerItem)
        {
            this.designerItem = designerItem;
            this.BorderThickness = new Thickness(2);
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (designerItem != null)
            {
                Point dragDelta = new Point(e.HorizontalChange, e.VerticalChange);

                RotateTransform rotateTransform = designerItem.RenderTransform as RotateTransform;
                if (rotateTransform != null)
                {
                    dragDelta = rotateTransform.Transform(dragDelta);
                }

                Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + dragDelta.X);
                Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + dragDelta.Y);
            }
        }
    }
    public class DrawingRectangle2 : DrawingObject
    {
        string _class_name;
        public string ClassName
        {
            get
            {
                return _class_name;
            }
            set
            {
                if (_class_name != value)
                {
                    _class_name = value;
                    RaisePropertyChanged("ClassName");
                }
            }
        }
        Thumb thumb_1;
        Thumb thumb_2;
        Thumb thumb_3;
        Thumb thumb_4;
        Thumb thumb_move;
        Thumb thumb_rotate;
        protected override void IsSelectedChanged()
        {
            if (IsSelected)
            {
                rectangle.StrokeThickness = 2;
            }
            else
            {
                rectangle.StrokeThickness = 1;
            }
            base.IsSelectedChanged();
        }
        System.Windows.Shapes.Rectangle rectangle;
        public DrawingRectangle2(double X, double Y, double Angle, double Width, double Height)
        {
            this.Width = Width;
            this.Height = Height;
            Canvas.SetLeft(this, X - Width / 2);
            Canvas.SetTop(this, Y - Height / 2);

            InitializeDefault(null);


        }
        public DrawingRectangle2(object datacontext)
        {
            InitializeDefault(datacontext);
        }

        private void InitializeDefault(object datacontext)
        {
            rectangle = new System.Windows.Shapes.Rectangle() { StrokeThickness = 1 };
            if (datacontext != null)
            {
                Binding borderbinding = new Binding("SelectedColor");
                borderbinding.Converter = new ColorToBrush();
                borderbinding.Source = datacontext;
                rectangle.SetBinding(System.Windows.Shapes.Rectangle.StrokeProperty, borderbinding);

            }
            else
                rectangle.Stroke = Brushes.DeepSkyBlue;
            thumb_1 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, -5.5, 0, 0) };
            thumb_2 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -5.5, -5.5) };
            thumb_3 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, -5.5, -5.5, 0) };
            thumb_4 = new ResizeThumb(this) { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Width = 10, Height = 10, Margin = new Thickness(-5.5, 0, 0, -5.5) };
            thumb_move = new MoveThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = 10, Height = 10 };
            thumb_rotate = new RotateThumb(this) { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width = 10, Height = 10, Margin = new Thickness(0, 0, -10, 0) };


            Content = new Grid();
            (Content as Grid).Children.Add(rectangle);
            (Content as Grid).Children.Add(thumb_1);
            (Content as Grid).Children.Add(thumb_2);
            (Content as Grid).Children.Add(thumb_3);
            (Content as Grid).Children.Add(thumb_4);
            (Content as Grid).Children.Add(thumb_move);
            (Content as Grid).Children.Add(thumb_rotate);

            rectangle.Fill = new SolidColorBrush(new Color() { A = 100, R = 255, G = 255, B = 255 });
        }
    }

    public class ImageHelper
    {
        public enum ImageFormat
        {
            JPEG, BMP, PNG
        }
        public static void SaveImage(string src_filename, string dst_filename, ImageFormat format)
        {
            System.Drawing.Bitmap image = new System.Drawing.Bitmap(src_filename);

            if (src_filename != string.Empty)
            {
                using (FileStream stream = new FileStream(dst_filename, FileMode.Create))
                {
                    System.Drawing.Imaging.ImageFormat img_format;
                    switch (format)
                    {
                        case ImageFormat.BMP: img_format = System.Drawing.Imaging.ImageFormat.Bmp; break;
                        case ImageFormat.JPEG: img_format = System.Drawing.Imaging.ImageFormat.Jpeg; break;
                        case ImageFormat.PNG: img_format = System.Drawing.Imaging.ImageFormat.Png; break;
                        default: img_format = System.Drawing.Imaging.ImageFormat.Png; break;
                    }
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Close();
                }
            }
        }
    }
    public delegate void OnDrawingParamChanged(DrawingObject sender, EventArgs e);
    public class DrawingObject : ContentControl, INotifyPropertyChanged
    {

        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        Color _thumb_color;
        public Color ThumbColor
        {
            get
            {
                return _thumb_color;
            }
            set
            {
                if (_thumb_color != value)
                {
                    _thumb_color = value;
                    RaisePropertyChanged("ThumbColor");
                }
            }
        }

        public GridItemControl Container { get; set; }
        public DrawingObject()
        {
            ClipToBounds = false;
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 0);
            this.MinHeight = 10;
            this.MinWidth = 10;
            this.RenderTransform = new RotateTransform(0);
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            MouseUp += DrawingRectangle_MouseUp;
            MouseDown += DrawingRectangle_MouseDown;
            MinWidth = 10;
            MinHeight = 10;
        }
        public void InitializeThumb(GridItemControl container)
        {
            this.Container = container;
            var panel = Content as Panel;
            if (panel != null)
            {
                foreach (UIElement item in panel.Children)
                {
                    var thumb = item as Thumb;
                    if (thumb != null)
                    {
                        thumb.DragStarted += Thumb_DragStarted;
                        thumb.DragCompleted += Thumb_DragCompleted;
                        thumb.DragDelta += Thumb_DragDelta;
                    }
                }
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            OnDrawingParamChanged?.Invoke(this);
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Container.IsDrag = false;
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Container.IsDrag = true;
        }

        bool _is_selected = false;
        public bool IsSelected
        {
            get
            {
                return _is_selected;
            }
            set
            {

                if (value != _is_selected)
                {

                    _is_selected = value;
                    IsSelectedChanged();

                }
            }
        }
        protected virtual void IsSelectedChanged()
        {
            if (IsSelected)
                Panel.SetZIndex(this, 10);
            else
                Panel.SetZIndex(this, 0);
            var parrent = Content as Panel;
            if (parrent != null)
            {
                foreach (UIElement item in parrent.Children)
                {
                    var item_thumb = item as Thumb;
                    if (item_thumb != null)
                    {
                        if (IsSelected)
                            item_thumb.Visibility = Visibility.Visible;
                        else
                            item_thumb.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
        public DrawingParamChanged OnDrawingParamChanged { get; set; }
        bool is_mouse_down = false;
        private void DrawingRectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (is_mouse_down)
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    if (click1 == e.GetPosition(this))
                    {
                        IsSelected = true;
                        //OnSelectionChangeEvent?.Invoke(this, null);
                    }
                }
            }
        }
        Point click1;
        private void DrawingRectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                is_mouse_down = true;
                click1 = e.GetPosition(this);
            }
        }
    }
    public delegate void DrawingParamChanged(DrawingObject sender);

    public class GridItemControl : Canvas
    {
        public void SetMultiSelectArea(double x, double y, double w, double h)
        {
            foreach (UIElement child in Children)
            {
                var item = child as DrawingObject;
                if (item != null)
                {
                    var top = GetTop(item);
                    var left = GetLeft(item);
                    if (top > y & left > x & item.Width + left < w + x & item.Height + top < h + x)
                    {
                        item.IsSelected = true;
                    }
                    else
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }
        public GridItemControl()
        {
            this.PreviewKeyDown += Grid_main_PreviewKeyDown;
            this.PreviewKeyUp += Grid_main_PreviewKeyUp;

        }
        public DrawingObject SelectedItem { get; set; }
        public bool IsDrag { get; set; }
        public void SetSingleSelectedItem(DrawingObject item)
        {
            foreach (UIElement element in this.Children)
            {
                if (element != item)
                {
                    if (element is DrawingObject)
                        (element as DrawingObject).IsSelected = false;
                }
                else
                {
                    SelectedItem = item;
                    SelectedItem.IsSelected = true;
                }

            }
        }
        public void RemoveDrawingObject(DrawingObject item)
        {
            Children.Remove(item);
        }
        public void AddDrawingObject(DrawingObject item)
        {
            Children.Add(item);
            item.InitializeThumb(this);
            SetSingleSelectedItem(item);
        }
        bool is_key_down = false;
        private void Grid_main_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                is_key_down = true;
        }

        private void Grid_main_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (is_key_down)
            {
                if (e.Key == Key.Delete)
                {
                    Children.Remove(SelectedItem as UIElement);
                    SelectedItem = null;
                }
            }
            is_key_down = false;
        }
        public void ClearDrawingobject()
        {
            List<UIElement> item_to_remove = new List<UIElement>();

            foreach (UIElement item in Children)
            {
                if (item is DrawingObject)
                {
                    item_to_remove.Add(item);
                }
            }
            foreach (UIElement item in item_to_remove)
            {
                Children.Remove(item);
            }
        }
        public int GetDrawingObjectCount()
        {
            int count = 0;
            foreach (var item in Children)
            {
                if (item is DrawingObject)
                    count++;
            }
            return count;
        }
        public DrawingObject LastGetDrawingObject()
        {
            foreach (var item in Children)
            {
                if (item is DrawingObject)
                    return item as DrawingObject;
            }
            return null;
        }
    }
}
